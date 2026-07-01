using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a set of values with no duplicate elements. Backed by <see cref="WideArray{T}"/>-based
/// storage so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideHashSet<T> : IWideSet<T>, IWideReadOnlySet<T>, ICompactable {
    private const long Lower31BitMask = 0x7FFFFFFF;

    private WideArray<long> _buckets = new();
    private WideArray<Entry> _entries = new();
    private readonly IEqualityComparer<T> _comparer;
    private long _count, _freeList = -1, _freeCount, _version;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    private struct Entry {
        /// <summary>
        /// Stores the cached hash code for the entry, or a negative value when the entry is unused.
        /// </summary>
        public int HashCode;
        /// <summary>
        /// Stores the index of the next entry in the same bucket chain, or -1 when there is none.
        /// </summary>
        public long Next;
        /// <summary>
        /// Stores the value contained by the entry.
        /// </summary>
        public T Value;
    }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="WideHashSet{T}"/> class that uses the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use, or <see langword="null"/> to use the default comparer.</param>
    public WideHashSet(IEqualityComparer<T> comparer) : this(0, comparer) { }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="WideHashSet{T}"/> class with the specified initial capacity and equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of elements the set can contain.</param>
    /// <param name="comparer">The equality comparer to use, or <see langword="null"/> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public WideHashSet(long capacity = 0, IEqualityComparer<T> comparer = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _comparer = comparer ?? EqualityComparer<T>.Default;
        if (capacity > 0)
            Initialize(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideHashSet{T}"/> class that contains the elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the set.</param>
    /// <param name="comparer">The equality comparer to use, or <see langword="null"/> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public WideHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = null) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is ICollection<T> genericCollection)
            Initialize(genericCollection.Count);

        UnionWith(collection);
    }

    /// <inheritdoc />
    public long Count => _count - _freeCount;
    /// <inheritdoc />
    public bool IsReadOnly => false;
    /// <summary>
    /// Gets the equality comparer used to compare values in the set.
    /// </summary>
    public IEqualityComparer<T> Comparer => _comparer;
    internal long InternalEntriesLength => _entries.Length;

    /// <inheritdoc />
    public bool Add(T item) => AddIfNotPresent(item);

    void IWideCollection<T>.Add(T item) => Add(item);

    /// <inheritdoc />
    public void Clear() {
        if (_count == 0)
            return;

        _buckets.Clear();
        if (ContainsReferences) {
            for (long i = 0; i < _count; i++) {
                Entry entry = _entries[i];
                entry.Value = default!;
                entry.HashCode = -1;
                entry.Next = -1;
                _entries[i] = entry;
            }
        } else {
            for (long i = 0; i < _count; i++) {
                Entry entry = _entries[i];
                entry.HashCode = -1;
                entry.Next = -1;
                _entries[i] = entry;
            }
        }

        _count = 0;
        _freeList = -1;
        _freeCount = 0;
        _version++;
    }

    /// <inheritdoc />
    public bool Contains(T item) {
        if (_buckets.Length == 0)
            return false;

        int hashCode = InternalGetHashCode(item);
        long bucket = hashCode % _buckets.Length;

        for (long i = _buckets[bucket] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];
            if (entry.HashCode == hashCode && _comparer.Equals(entry.Value, item))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Count, array.Length - arrayIndex);

        long copied = 0;
        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            if (entry.HashCode >= 0) {
                array[arrayIndex + copied] = entry.Value;
                copied++;
            }
        }
    }

    /// <inheritdoc />
    public bool Remove(T item) {
        if (_buckets.Length == 0)
            return false;

        int hashCode = InternalGetHashCode(item);
        long bucket = hashCode % _buckets.Length;
        long last = -1;

        for (long i = _buckets[bucket] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];
            if (entry.HashCode == hashCode && _comparer.Equals(entry.Value, item)) {
                if (last < 0)
                    _buckets[bucket] = entry.Next + 1;
                else {
                    Entry lastEntry = _entries[last];
                    lastEntry.Next = entry.Next;
                    _entries[last] = lastEntry;
                }

                entry.HashCode = -1;
                entry.Next = _freeList;
                entry.Value = default!;
                _entries[i] = entry;

                _freeList = i;
                _freeCount++;
                _version++;
                return true;
            }

            last = i;
        }

        return false;
    }

    /// <inheritdoc />
    public void UnionWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        foreach (T item in other)
            AddIfNotPresent(item);
    }

    /// <inheritdoc />
    public void IntersectWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (Count == 0)
            return;

        if (ReferenceEquals(other, this))
            return;

        HashSet<T> otherSet = new(other, _comparer);
        if (otherSet.Count == 0) {
            Clear();
            return;
        }

        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            if (entry.HashCode >= 0 && !otherSet.Contains(entry.Value))
                Remove(entry.Value);
        }
    }

    /// <inheritdoc />
    public void ExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (Count == 0)
            return;

        if (ReferenceEquals(other, this)) {
            Clear();
            return;
        }

        foreach (T item in other)
            Remove(item);
    }

    /// <inheritdoc />
    public void SymmetricExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this)) {
            Clear();
            return;
        }

        HashSet<T> uniqueOther = new(other, _comparer);
        foreach (T item in uniqueOther) {
            if (!Remove(item))
                AddIfNotPresent(item);
        }
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (Count == 0)
            return true;

        if (ReferenceEquals(other, this))
            return true;

        HashSet<T> otherSet = new(other, _comparer);
        if (Count > otherSet.Count)
            return false;

        foreach (T item in this) {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        foreach (T item in other) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> otherSet = new(other, _comparer);
        if (Count <= otherSet.Count)
            return false;

        foreach (T item in otherSet) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> otherSet = new(other, _comparer);
        if (Count >= otherSet.Count)
            return false;

        foreach (T item in this) {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (Count == 0)
            return false;

        foreach (T item in other) {
            if (Contains(item))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return true;

        HashSet<T> otherSet = new(other, _comparer);
        if (Count != otherSet.Count)
            return false;

        foreach (T item in this) {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Compact() {
        long liveCount = Count;
        if (liveCount == 0) {
            _buckets = new WideArray<long>();
            _entries = new WideArray<Entry>();
            _count = 0;
            _freeList = -1;
            _freeCount = 0;
            _version++;
            return;
        }

        long newSize = PrimeHelper.GetPrime(liveCount);
        WideArray<long> newBuckets = new(newSize);
        WideArray<Entry> newEntries = new(newSize);

        long newIndex = 0;
        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            if (entry.HashCode < 0)
                continue;

            long bucket = entry.HashCode % newSize;
            entry.Next = newBuckets[bucket] - 1;
            newEntries[newIndex] = entry;
            newBuckets[bucket] = newIndex + 1;
            newIndex++;
        }

        _buckets = newBuckets;
        _entries = newEntries;
        _count = newIndex;
        _freeCount = 0;
        _freeList = -1;
        _version++;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool AddIfNotPresent(T item) {
        if (_buckets.Length == 0)
            Initialize(0);

        int hashCode = InternalGetHashCode(item);
        long bucket = hashCode % _buckets.Length;

        for (long i = _buckets[bucket] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];
            if (entry.HashCode == hashCode && _comparer.Equals(entry.Value, item))
                return false;
        }

        long index;
        if (_freeCount > 0) {
            index = _freeList;
            _freeList = _entries[index].Next;
            _freeCount--;
        } else {
            if (_count == _entries.Length) {
                Resize();
                bucket = hashCode % _buckets.Length;
            }

            index = _count;
            _count++;
        }

        Entry newEntry = new() {
            HashCode = hashCode,
            Next = _buckets[bucket] - 1,
            Value = item
        };
        _entries[index] = newEntry;
        _buckets[bucket] = index + 1;
        _version++;
        return true;
    }

    private void Initialize(long capacity) {
        long size = PrimeHelper.GetPrime(capacity <= 0 ? 3 : capacity);
        _buckets = new WideArray<long>(size);
        _entries = new WideArray<Entry>(size);
        _freeList = -1;
    }

    private void Resize() {
        long newSize = PrimeHelper.GetPrime(_count * 2);
        WideArray<long> newBuckets = new(newSize);
        WideArray<Entry> newEntries = new(newSize);

        WideArray<Entry>.BulkCopy(_entries, 0, newEntries, 0, _count);

        for (long i = 0; i < _count; i++) {
            Entry entry = newEntries[i];
            if (entry.HashCode >= 0) {
                long bucket = entry.HashCode % newSize;
                entry.Next = newBuckets[bucket] - 1;
                newEntries[i] = entry;
                newBuckets[bucket] = i + 1;
            }
        }

        _buckets = newBuckets;
        _entries = newEntries;
    }

    private int InternalGetHashCode(T item) {
        if (item is null)
            return 0;

        return _comparer.GetHashCode(item) & (int)Lower31BitMask;
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="WideHashSet{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T> {
        private readonly WideHashSet<T> _set;
        private readonly long _version;
        private long _index;
        private T _current;

        internal Enumerator(WideHashSet<T> set) {
            _set = set;
            _version = set._version;
            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public T Current => _current;
        object IEnumerator.Current => _current!;

        /// <inheritdoc />
        public bool MoveNext() {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            while (_index < _set._count) {
                Entry entry = _set._entries[_index];
                _index++;
                if (entry.HashCode >= 0) {
                    _current = entry.Value;
                    return true;
                }
            }

            _index = _set._count + 1;
            _current = default!;
            return false;
        }

        /// <inheritdoc />
        public void Reset() {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}

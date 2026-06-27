using System.Collections;

namespace WideCollections;

public class WideHashSet<T> : IWideSet<T>, IWideReadOnlySet<T> {
    private const long Lower31BitMask = 0x7FFFFFFF;

    private static readonly long[] Primes = [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103,
        1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229,
        30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449,
        389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249,
        3471899, 4166287, 4999559, 5999471, 7199369
    ];

    private WideArray<long> _buckets = new();
    private WideArray<Entry> _entries = new();
    private readonly IEqualityComparer<T> _comparer;
    private long _count;
    private long _freeList = -1;
    private long _freeCount;
    private long _version;

    private struct Entry {
        public int HashCode;
        public long Next;
        public T Value;
    }

    public WideHashSet() : this(0, null) { }

    public WideHashSet(IEqualityComparer<T> comparer) : this(0, comparer) { }

    public WideHashSet(long capacity) : this(capacity, null) { }

    public WideHashSet(long capacity, IEqualityComparer<T> comparer) {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

        _comparer = comparer ?? EqualityComparer<T>.Default;
        if (capacity > 0)
            Initialize(capacity);
    }

    public WideHashSet(IEnumerable<T> collection) : this(collection, null) { }

    public WideHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is ICollection<T> genericCollection)
            Initialize(genericCollection.Count);

        UnionWith(collection);
    }

    public long Count => _count - _freeCount;
    public bool IsReadOnly => false;
    public IEqualityComparer<T> Comparer => _comparer;

    public bool Add(T item) => AddIfNotPresent(item);

    void IWideCollection<T>.Add(T item) => Add(item);

    public void Clear() {
        if (_count == 0)
            return;

        _buckets.Clear();
        if (!typeof(T).IsValueType) {
            for (long i = 0; i < _count; i++) {
                Entry entry = _entries[i];
                entry.Value = default!;
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

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");

        if (arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        long copied = 0;
        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            if (entry.HashCode >= 0) {
                array[arrayIndex + copied] = entry.Value;
                copied++;
            }
        }
    }

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

    public void UnionWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        foreach (T item in other)
            AddIfNotPresent(item);
    }

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

    public bool IsSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        foreach (T item in other) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

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
        long size = GetPrime(capacity <= 0 ? 3 : capacity);
        _buckets = new WideArray<long>(size);
        _entries = new WideArray<Entry>(size);
        _freeList = -1;
    }

    private void Resize() {
        long newSize = GetPrime(_count * 2);
        WideArray<long> newBuckets = new(newSize);
        WideArray<Entry> newEntries = new(newSize);

        for (long i = 0; i < _count; i++)
            newEntries[i] = _entries[i];

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

    private static long GetPrime(long min) {
        for (int i = 0; i < Primes.Length; i++) {
            long prime = Primes[i];
            if (prime >= min)
                return prime;
        }

        if (min <= 2)
            return 3;

        for (long candidate = (min | 1); candidate < long.MaxValue; candidate += 2) {
            if (IsPrime(candidate) && ((candidate - 1) % 101 != 0))
                return candidate;
        }

        return min;
    }

    private static bool IsPrime(long candidate) {
        if ((candidate & 1) == 0)
            return candidate == 2;

        long limit = (long)Math.Sqrt(candidate);
        for (long divisor = 3; divisor <= limit; divisor += 2) {
            if (candidate % divisor == 0)
                return false;
        }

        return true;
    }

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

        public T Current => _current;
        object IEnumerator.Current => _current!;

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

        public void Reset() {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _current = default!;
        }

        public void Dispose() { }
    }
}
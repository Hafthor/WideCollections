using System.Collections;

namespace WideCollections;

public class WideDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, IWideReadOnlyDictionary<TKey, TValue>
    where TKey : notnull {
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
    private long _count;
    private long _freeList = -1;
    private long _freeCount;
    private long _version;
    private readonly IEqualityComparer<TKey> _comparer;
    private KeyCollection _keys;
    private ValueCollection _values;

    private struct Entry {
        public int HashCode;
        public long Next;
        public TKey Key;
        public TValue Value;
    }

    public WideDictionary() : this(0, null) { }

    public WideDictionary(long capacity) : this(capacity, null) { }

    public WideDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

    public WideDictionary(long capacity, IEqualityComparer<TKey> comparer) {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        SyncRoot = new object();

        if (capacity > 0)
            Initialize(capacity);
    }

    public WideDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

    public WideDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is ICollection<KeyValuePair<TKey, TValue>> genericCollection)
            Initialize(genericCollection.Count);

        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    public long Count => _count - _freeCount;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;

    public TValue this[TKey key] {
        get {
            long index = FindEntryIndex(key);
            if (index >= 0)
                return _entries[index].Value;

            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }
        set => TryInsert(key, value, InsertionBehavior.OverwriteExisting);
    }

    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    public IWideCollection<TValue> Values => _values ??= new ValueCollection(this);
    IEnumerable<TKey> IWideReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IWideReadOnlyDictionary<TKey, TValue>.Values => Values;
    IWideCollection IWideDictionary.Keys => (IWideCollection)Keys;
    IWideCollection IWideDictionary.Values => (IWideCollection)Values;

    object IWideDictionary.this[object key] {
        get {
            ValidateObjectKey(key);
            return this[(TKey)key]!;
        }
        set {
            ValidateObjectKey(key);
            ValidateObjectValue(value);
            this[(TKey)key] = (TValue)value;
        }
    }

    public void Add(TKey key, TValue value) => TryInsert(key, value, InsertionBehavior.ThrowOnExisting);

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    public bool ContainsKey(TKey key) => FindEntryIndex(key) >= 0;

    public bool TryGetValue(TKey key, out TValue value) {
        long index = FindEntryIndex(key);
        if (index >= 0) {
            value = _entries[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool Remove(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.Length == 0)
            return false;

        int hashCode = InternalGetHashCode(key);
        long bucket = hashCode % _buckets.Length;
        long last = -1;

        for (long i = _buckets[bucket] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];

            if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key)) {
                if (last < 0)
                    _buckets[bucket] = entry.Next + 1;
                else {
                    Entry previous = _entries[last];
                    previous.Next = entry.Next;
                    _entries[last] = previous;
                }

                entry.HashCode = -1;
                entry.Next = _freeList;
                entry.Key = default!;
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

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        long index = FindEntryIndex(item.Key);
        if (index < 0)
            return false;

        return EqualityComparer<TValue>.Default.Equals(_entries[index].Value, item.Value);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        long index = FindEntryIndex(item.Key);
        if (index < 0)
            return false;

        if (!EqualityComparer<TValue>.Default.Equals(_entries[index].Value, item.Value))
            return false;

        return Remove(item.Key);
    }

    void IWideDictionary.Remove(object key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key is TKey typedKey)
            Remove(typedKey);
    }

    public void Clear() {
        if (_count == 0)
            return;

        _buckets.Clear();
        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            entry.HashCode = -1;
            entry.Next = -1;
            entry.Key = default!;
            entry.Value = default!;
            _entries[i] = entry;
        }

        _count = 0;
        _freeList = -1;
        _freeCount = 0;
        _version++;
    }

    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
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
                array[arrayIndex + copied] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                copied++;
            }
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IWideDictionary.GetEnumerator() => new DictionaryEnumerator(this);

    private long FindEntryIndex(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.Length == 0)
            return -1;

        int hashCode = InternalGetHashCode(key);
        for (long i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];
            if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                return i;
        }

        return -1;
    }

    private void TryInsert(TKey key, TValue value, InsertionBehavior behavior) {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.Length == 0)
            Initialize(0);

        int hashCode = InternalGetHashCode(key);
        long bucket = hashCode % _buckets.Length;

        for (long i = _buckets[bucket] - 1; i >= 0; i = _entries[i].Next) {
            Entry entry = _entries[i];
            if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key)) {
                if (behavior == InsertionBehavior.OverwriteExisting) {
                    entry.Value = value;
                    _entries[i] = entry;
                    _version++;
                    return;
                }

                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }
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

        _entries[index] = new Entry {
            HashCode = hashCode,
            Next = _buckets[bucket] - 1,
            Key = key,
            Value = value
        };
        _buckets[bucket] = index + 1;
        _version++;
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

    private int InternalGetHashCode(TKey key) => _comparer.GetHashCode(key) & (int)Lower31BitMask;

    private static void ValidateObjectValue(object value) {
        if (value is null && default(TValue) is not null)
            throw new ArgumentNullException(nameof(value));

        if (value is not TValue && (value is not null || default(TValue) is not null))
            throw new ArgumentException($"Value must be of type {typeof(TValue)}.", nameof(value));
    }

    private static void ValidateObjectKey(object key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key is not TKey)
            throw new ArgumentException($"Key must be of type {typeof(TKey)}.", nameof(key));
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

    private enum InsertionBehavior {
        OverwriteExisting,
        ThrowOnExisting
    }

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideDictionary<TKey, TValue> _dictionary;
        private readonly long _version;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _version = dictionary._version;
            _index = 0;
            _current = default;
        }

        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext() {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            while (_index < _dictionary._count) {
                Entry entry = _dictionary._entries[_index];
                _index++;
                if (entry.HashCode >= 0) {
                    _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                    return true;
                }
            }

            _index = _dictionary._count + 1;
            _current = default;
            return false;
        }

        public void Reset() {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _current = default;
        }

        public void Dispose() { }
    }

    private sealed class DictionaryEnumerator : IDictionaryEnumerator {
        private readonly WideDictionary<TKey, TValue> _dictionary;
        private readonly long _version;
        private long _index;
        private bool _valid;
        private DictionaryEntry _current;

        public DictionaryEnumerator(WideDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _version = dictionary._version;
            _index = 0;
            _valid = false;
            _current = default;
        }

        public DictionaryEntry Entry => _valid ? _current : throw new InvalidOperationException("Enumerator is not on a valid element.");
        public object Key => Entry.Key;
        public object Value => Entry.Value;
        public object Current => Entry;

        public bool MoveNext() {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            while (_index < _dictionary._count) {
                Entry entry = _dictionary._entries[_index];
                _index++;
                if (entry.HashCode >= 0) {
                    _current = new DictionaryEntry(entry.Key!, entry.Value);
                    _valid = true;
                    return true;
                }
            }

            _valid = false;
            return false;
        }

        public void Reset() {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _valid = false;
            _current = default;
        }
    }

    public sealed class KeyCollection : IWideCollection<TKey>, IWideCollection {
        private readonly WideDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

        public long Count => _dictionary.Count;
        public bool IsReadOnly => true;
        public object SyncRoot => _dictionary.SyncRoot;
        public bool IsSynchronized => false;

        public bool Contains(TKey item) => _dictionary.ContainsKey(item);
        public void CopyTo(WideArray<TKey> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
            if (arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
            if (array.Length - arrayIndex < _dictionary.Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    array[arrayIndex + copied] = entry.Key;
                    copied++;
                }
            }
        }

        public void Add(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TKey> GetEnumerator() {
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0)
                    yield return entry.Key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ValueCollection : IWideCollection<TValue>, IWideCollection {
        private readonly WideDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

        public long Count => _dictionary.Count;
        public bool IsReadOnly => true;
        public object SyncRoot => _dictionary.SyncRoot;
        public bool IsSynchronized => false;

        public bool Contains(TValue item) {
            var comparer = EqualityComparer<TValue>.Default;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0 && comparer.Equals(entry.Value, item))
                    return true;
            }

            return false;
        }

        public void CopyTo(WideArray<TValue> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
            if (arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
            if (array.Length - arrayIndex < _dictionary.Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    array[arrayIndex + copied] = entry.Value;
                    copied++;
                }
            }
        }

        public void Add(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TValue> GetEnumerator() {
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0)
                    yield return entry.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
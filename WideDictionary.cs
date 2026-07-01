using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of keys and values. Backed by <see cref="WideArray{T}"/>-based storage so it
/// can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, 
    IWideReadOnlyDictionary<TKey, TValue>, ICompactable where TKey : notnull {
    private const long Lower31BitMask = 0x7FFFFFFF;

    private WideArray<long> _buckets = new();
    private WideArray<Entry> _entries = new();
    private long _count, _freeList = -1, _freeCount, _version;
    private readonly IEqualityComparer<TKey> _comparer;
    private KeyCollection _keys;
    private ValueCollection _values;

    private struct Entry {
        public int HashCode;
        public long Next;
        public TKey Key;
        public TValue Value;
    }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideDictionary{TKey, TValue}"/> class
    /// that uses the default equality comparer for the key type.
    /// </summary>
    public WideDictionary() : this(0, null) { }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideDictionary{TKey, TValue}"/> class
    /// with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements the dictionary can hold before resizing.</param>
    public WideDictionary(long capacity) : this(capacity, null) { }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideDictionary{TKey, TValue}"/> class
    /// that uses the specified key equality comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideDictionary{TKey, TValue}"/> class
    /// with the specified initial capacity and key equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of elements the dictionary can hold before resizing.</param>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideDictionary(long capacity, IEqualityComparer<TKey> comparer) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        if (capacity > 0)
            Initialize(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideDictionary{TKey, TValue}"/> class that contains
    /// the key/value pairs copied from the specified collection, using the default key comparer.
    /// </summary>
    /// <param name="collection">The collection whose key/value pairs are copied into the dictionary.</param>
    public WideDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideDictionary{TKey, TValue}"/> class that contains
    /// the key/value pairs copied from the specified collection, using the specified key comparer.
    /// </summary>
    /// <param name="collection">The collection whose key/value pairs are copied into the dictionary.</param>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is ICollection<KeyValuePair<TKey, TValue>> genericCollection)
            Initialize(genericCollection.Count);

        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    /// <inheritdoc />
    public long Count => _count - _freeCount;
    /// <inheritdoc />
    public bool IsReadOnly => false;
    /// <inheritdoc />
    public bool IsFixedSize => false;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;
    internal long InternalEntriesLength => _entries.Length;

    /// <inheritdoc />
    public TValue this[TKey key] {
        get {
            long index = FindEntryIndex(key);
            if (index >= 0)
                return _entries[index].Value;

            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }
        set => TryInsert(key, value, InsertionBehavior.OverwriteExisting);
    }

    /// <inheritdoc />
    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => TryInsert(key, value, InsertionBehavior.ThrowOnExisting);

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary. Does nothing if the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added; <see langword="false"/> if the key already exists.</returns>
    public bool TryAdd(TKey key, TValue value) => TryInsert(key, value, InsertionBehavior.None);

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => FindEntryIndex(key) >= 0;

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value) {
        long index = FindEntryIndex(key);
        if (index >= 0) {
            value = _entries[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) {
        long index = FindEntryIndex(item.Key);
        if (index < 0)
            return false;

        return EqualityComparer<TValue>.Default.Equals(_entries[index].Value, item.Value);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) {
        long index = FindEntryIndex(item.Key);
        if (index < 0)
            return false;

        if (!EqualityComparer<TValue>.Default.Equals(_entries[index].Value, item.Value))
            return false;

        return Remove(item.Key);
    }

    void IWideDictionary.Remove(object key) {
        ArgumentNullException.ThrowIfNull(key);

        if (key is TKey typedKey)
            Remove(typedKey);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        long copied = 0;
        for (long i = 0; i < _count; i++) {
            Entry entry = _entries[i];
            if (entry.HashCode >= 0) {
                array[arrayIndex + copied] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                copied++;
            }
        }
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

    private bool TryInsert(TKey key, TValue value, InsertionBehavior behavior) {
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
                    return true;
                }

                if (behavior == InsertionBehavior.None)
                    return false;

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

    private int InternalGetHashCode(TKey key) => _comparer.GetHashCode(key) & (int)Lower31BitMask;

    private static void ValidateObjectValue(object value) {
        if (default(TValue) is not null)
            ArgumentNullException.ThrowIfNull(value);

        if (value is not TValue && value is not null)
            throw new ArgumentException($"Value must be of type {typeof(TValue)}.", nameof(value));
    }

    private static void ValidateObjectKey(object key) {
        ArgumentNullException.ThrowIfNull(key);

        if (key is not TKey)
            throw new ArgumentException($"Key must be of type {typeof(TKey)}.", nameof(key));
    }

    private enum InsertionBehavior {
        None,
        OverwriteExisting,
        ThrowOnExisting
    }

    /// <summary>
    /// Enumerates the key/value pairs of a <see cref="WideDictionary{TKey, TValue}"/>.
    /// </summary>
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

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Reset() {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }

    private sealed class DictionaryEnumerator(WideDictionary<TKey, TValue> dictionary) : IDictionaryEnumerator {
        private readonly long _version = dictionary._version;
        private long _index;
        private bool _valid;
        private DictionaryEntry _current;

        /// <summary>
        /// Gets the <see cref="DictionaryEntry"/> at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a valid element.</exception>
        public DictionaryEntry Entry => _valid ? _current : throw new InvalidOperationException("Enumerator is not on a valid element.");
        /// <summary>
        /// Gets the key of the current dictionary entry.
        /// </summary>
        public object Key => Entry.Key;
        /// <summary>
        /// Gets the value of the current dictionary entry.
        /// </summary>
        public object Value => Entry.Value;
        /// <summary>
        /// Gets the current dictionary entry.
        /// </summary>
        public object Current => Entry;

        /// <inheritdoc />
        public bool MoveNext() {
            if (_version != dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            while (_index < dictionary._count) {
                Entry entry = dictionary._entries[_index];
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

        /// <inheritdoc />
        public void Reset() {
            if (_version != dictionary._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            _index = 0;
            _valid = false;
            _current = default;
        }
    }

    private sealed class KeyCollection : WideKeyValueCollectionBase<TKey> {
        private readonly WideDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot) 
            => _dictionary = dictionary;

        /// <summary>
        /// Gets the number of keys in the dictionary.
        /// </summary>
        public override long Count => _dictionary.Count;
        /// <summary>
        /// Determines whether the collection contains the specified key.
        /// </summary>
        /// <param name="item">The key to locate.</param>
        /// <returns><see langword="true"/> if the key is found; otherwise <see langword="false"/>.</returns>
        public override bool Contains(TKey item) => _dictionary.ContainsKey(item);

        protected override TKey GetElementAt(long index) {
            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    if (copied == index)
                        return entry.Key;
                    copied++;
                }
            }
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Copies the keys of the dictionary into the specified array, starting at the given index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public override void CopyTo(WideArray<TKey> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
            ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, _dictionary.Count);

            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    array[arrayIndex + copied] = entry.Key;
                    copied++;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates over the keys of the dictionary.
        /// </summary>
        /// <returns>An enumerator for the keys.</returns>
        public override IEnumerator<TKey> GetEnumerator() {
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0)
                    yield return entry.Key;
            }
        }
    }

    private sealed class ValueCollection : WideKeyValueCollectionBase<TValue> {
        private readonly WideDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot) 
            => _dictionary = dictionary;

        /// <summary>
        /// Gets the number of values in the dictionary.
        /// </summary>
        public override long Count => _dictionary.Count;
        /// <summary>
        /// Determines whether the collection contains the specified value.
        /// </summary>
        /// <param name="item">The value to locate.</param>
        /// <returns><see langword="true"/> if the value is found; otherwise <see langword="false"/>.</returns>
        public override bool Contains(TValue item) {
            var comparer = EqualityComparer<TValue>.Default;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0 && comparer.Equals(entry.Value, item))
                    return true;
            }
            return false;
        }

        protected override TValue GetElementAt(long index) {
            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    if (copied == index)
                        return entry.Value;
                    copied++;
                }
            }
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Copies the values of the dictionary into the specified array, starting at the given index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public override void CopyTo(WideArray<TValue> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
            ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, _dictionary.Count);

            long copied = 0;
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0) {
                    array[arrayIndex + copied] = entry.Value;
                    copied++;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates over the values of the dictionary.
        /// </summary>
        /// <returns>An enumerator for the values.</returns>
        public override IEnumerator<TValue> GetEnumerator() {
            for (long i = 0; i < _dictionary._count; i++) {
                Entry entry = _dictionary._entries[i];
                if (entry.HashCode >= 0)
                    yield return entry.Value;
            }
        }
    }
}

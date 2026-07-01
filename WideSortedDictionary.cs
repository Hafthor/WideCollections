using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of key/value pairs that are sorted by key. Backed by a
/// <see cref="WideList{T}"/> so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideSortedDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, 
    IWideReadOnlyDictionary<TKey, TValue>, ICompactable where TKey : notnull {
    private readonly WideList<KeyValuePair<TKey, TValue>> _items;
    private KeyCollection _keys;
    private ValueCollection _values;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideSortedDictionary{TKey, TValue}"/> class
    /// that orders keys using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedDictionary(IComparer<TKey> comparer = null) {
        _items = new WideList<KeyValuePair<TKey, TValue>>();
        Comparer = comparer ?? Comparer<TKey>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideSortedDictionary{TKey, TValue}"/> class that contains
    /// the entries copied from the specified dictionary, ordered by the specified comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary whose entries are copied.</param>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer = null) : this(comparer) {
        ArgumentNullException.ThrowIfNull(dictionary);
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            Add(pair.Key, pair.Value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideSortedDictionary{TKey, TValue}"/> class that contains
    /// the key/value pairs copied from the specified collection, ordered by the specified comparer.
    /// </summary>
    /// <param name="collection">The collection whose key/value pairs are copied.</param>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey> comparer = null) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    /// <inheritdoc />
    public long Count => _items.Count;
    /// <inheritdoc />
    public bool IsReadOnly => false;
    /// <inheritdoc />
    public bool IsFixedSize => false;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;
    /// <summary>
    /// Gets the comparer used to order the keys of the dictionary.
    /// </summary>
    public IComparer<TKey> Comparer { get; }

    internal long InternalItemsCapacity => _items.Capacity;

    /// <inheritdoc />
    public TValue this[TKey key] {
        get {
            long index = FindIndex(key);
            if (index >= 0)
                return _items[index].Value;

            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }
        set {
            long index = FindIndex(key);
            if (index >= 0) {
                _items[index] = new KeyValuePair<TKey, TValue>(key, value);
                return;
            }

            _items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
        }
    }

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
    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    /// <inheritdoc />
    public IWideCollection<TValue> Values => _values ??= new ValueCollection(this);
    IEnumerable<TKey> IWideReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IWideReadOnlyDictionary<TKey, TValue>.Values => Values;
    IWideCollection IWideDictionary.Keys => (IWideCollection)Keys;
    IWideCollection IWideDictionary.Values => (IWideCollection)Values;

    /// <inheritdoc />
    public void Add(TKey key, TValue value) {
        long index = FindIndex(key);
        if (index >= 0)
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));

        _items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary. Does nothing if the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added; <see langword="false"/> if the key already exists.</returns>
    public bool TryAdd(TKey key, TValue value) {
        long index = FindIndex(key);
        if (index >= 0)
            return false;

        _items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
        return true;
    }

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => FindIndex(key) >= 0;

    /// <summary>
    /// Determines whether the dictionary contains an entry with the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        for (long i = 0; i < _items.Count; i++)
            if (comparer.Equals(_items[i].Value, value))
                return true;

        return false;
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value) {
        long index = FindIndex(key);
        if (index >= 0) {
            value = _items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool Remove(TKey key) {
        long index = FindIndex(key);
        if (index < 0)
            return false;

        _items.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the entry with the specified key and returns its value.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">When this method returns, contains the value of the removed entry if found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an entry was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(TKey key, out TValue value) {
        long index = FindIndex(key);
        if (index < 0) {
            value = default!;
            return false;
        }

        value = _items[index].Value;
        _items.RemoveAt(index);
        return true;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) {
        long index = FindIndex(item.Key);
        if (index < 0)
            return false;

        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;

        return Remove(item.Key);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    void IWideDictionary.Remove(object key) {
        ArgumentNullException.ThrowIfNull(key);

        if (key is TKey typedKey)
            Remove(typedKey);
    }

    /// <inheritdoc />
    public void Clear() => _items.Clear();

    /// <inheritdoc />
    public void Compact() => _items.Compact();

    /// <inheritdoc />
    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IWideDictionary.GetEnumerator() => new DictionaryEnumerator(this);

    private long FindIndex(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        long lo = 0, hi = _items.Count - 1;
        while (lo <= hi) {
            long mid = lo + ((hi - lo) >> 1);
            int cmp = Comparer.Compare(_items[mid].Key, key);
            if (cmp == 0)
                return mid;
            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return ~lo;
    }

    private static void ValidateObjectKey(object key) {
        ArgumentNullException.ThrowIfNull(key);
        if (key is not TKey)
            throw new ArgumentException($"Key must be of type {typeof(TKey)}.", nameof(key));
    }

    private static void ValidateObjectValue(object value) {
        if (default(TValue) is not null)
            ArgumentNullException.ThrowIfNull(value);
        if (value is not TValue && value is not null)
            throw new ArgumentException($"Value must be of type {typeof(TValue)}.", nameof(value));
    }

    /// <summary>
    /// Enumerates the key/value pairs of a <see cref="WideSortedDictionary{TKey, TValue}"/> in key order.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideSortedDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        /// <inheritdoc />
        public bool MoveNext() {
            if (_index < _dictionary.Count) {
                _current = _dictionary._items[_index];
                _index++;
                return true;
            }

            _current = default;
            _index = _dictionary.Count + 1;
            return false;
        }

        /// <inheritdoc />
        public void Reset() {
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }

    private sealed class DictionaryEnumerator(WideSortedDictionary<TKey, TValue> dictionary) : IDictionaryEnumerator {
        private Enumerator _enumerator = dictionary.GetEnumerator();
        private bool _valid;

        /// <summary>
        /// Gets the <see cref="DictionaryEntry"/> at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a valid element.</exception>
        public DictionaryEntry Entry {
            get {
                if (!_valid)
                    throw new InvalidOperationException("Enumerator is not on a valid element.");

                KeyValuePair<TKey, TValue> current = _enumerator.Current;
                return new DictionaryEntry(current.Key!, current.Value);
            }
        }

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
            _valid = _enumerator.MoveNext();
            return _valid;
        }

        /// <inheritdoc />
        public void Reset() {
            _enumerator.Reset();
            _valid = false;
        }
    }

    private sealed class KeyCollection : WideKeyValueCollectionBase<TKey> {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideSortedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
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

        protected override TKey GetElementAt(long index) => _dictionary._items[(int)index].Key;
    }

    private sealed class ValueCollection : WideKeyValueCollectionBase<TValue> {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideSortedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
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
        public override bool Contains(TValue item) => _dictionary.ContainsValue(item);

        protected override TValue GetElementAt(long index) => _dictionary._items[(int)index].Value;
    }
}

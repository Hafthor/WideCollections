using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of key/value pairs that are accessible by key or by index and preserve
/// insertion order. Backed by <see cref="WideList{T}"/> and <see cref="WideDictionary{TKey, TValue}"/>
/// so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideOrderedDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideList<KeyValuePair<TKey, TValue>>,
    IWideReadOnlyDictionary<TKey, TValue>, IWideReadOnlyList<KeyValuePair<TKey, TValue>>, IWideDictionary, IWideList,
    ICompactable where TKey : notnull {
    private readonly WideList<KeyValuePair<TKey, TValue>> _items;
    private readonly WideDictionary<TKey, long> _indexByKey;
    private KeyCollection _keys;
    private ValueCollection _values;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideOrderedDictionary{TKey, TValue}"/> class
    /// that uses the specified key equality comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideOrderedDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideOrderedDictionary{TKey, TValue}"/> class
    /// with the specified initial capacity and key equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of elements the dictionary can hold before resizing.</param>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideOrderedDictionary(long capacity = 0, IEqualityComparer<TKey> comparer = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideList<KeyValuePair<TKey, TValue>>(capacity);
        Comparer = comparer ?? EqualityComparer<TKey>.Default;
        _indexByKey = new WideDictionary<TKey, long>(capacity, Comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideOrderedDictionary{TKey, TValue}"/> class that contains
    /// the key/value pairs copied from the specified collection, preserving enumeration order.
    /// </summary>
    /// <param name="collection">The collection whose key/value pairs are copied.</param>
    /// <param name="comparer">The comparer used to compare keys, or <see langword="null"/> to use the default comparer.</param>
    public WideOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer = null) : this(0, comparer) {
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
    /// Gets the comparer used to compare the keys of the dictionary.
    /// </summary>
    public IEqualityComparer<TKey> Comparer { get; }

    /// <summary>
    /// Gets or sets the number of elements the dictionary can hold before it must resize its
    /// internal storage. When set, the value must not be less than <see cref="Count"/>.
    /// </summary>
    public long Capacity {
        get => _items.Capacity;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);
            _items.Capacity = value;
        }
    }

    /// <inheritdoc />
    public TValue this[TKey key] {
        get => _items[FindIndexByKey(key)].Value;
        set => SetValueByKey(key, value);
    }

    /// <inheritdoc />
    public KeyValuePair<TKey, TValue> this[long index] {
        get => _items[index];
        set => SetItemAtIndex(index, value);
    }

    KeyValuePair<TKey, TValue> IWideReadOnlyList<KeyValuePair<TKey, TValue>>.this[long index] => this[index];

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

    object IWideList.this[long index] {
        get => this[index];
        set {
            if (value is not KeyValuePair<TKey, TValue> pair)
                throw new ArgumentException($"Value must be of type {typeof(KeyValuePair<TKey, TValue>)}.", nameof(value));
            this[index] = pair;
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
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.ContainsKey(key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));

        long index = _items.Count;
        _items.Add(new KeyValuePair<TKey, TValue>(key, value));
        _indexByKey.Add(key, index);
    }

    /// <summary>
    /// Attempts to add the specified key and value to the end of the dictionary. Does nothing if the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added; <see langword="false"/> if the key already exists.</returns>
    public bool TryAdd(TKey key, TValue value) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.ContainsKey(key))
            return false;

        Add(key, value);
        return true;
    }

    /// <summary>
    /// Inserts a key/value pair at the specified position, shifting subsequent entries.
    /// </summary>
    /// <param name="index">The zero-based position at which to insert the entry.</param>
    /// <param name="key">The key of the element to insert.</param>
    /// <param name="value">The value of the element to insert.</param>
    public void Insert(long index, TKey key, TValue value) => Insert(index, new KeyValuePair<TKey, TValue>(key, value));

    /// <inheritdoc />
    public void Insert(long index, KeyValuePair<TKey, TValue> item) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        if (_indexByKey.ContainsKey(item.Key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(item));

        _items.Insert(index, item);
        ReindexFrom(index);
    }

    /// <summary>
    /// Returns the key/value pair at the specified position in the dictionary's order.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to retrieve.</param>
    /// <returns>The key/value pair at <paramref name="index"/>.</returns>
    public KeyValuePair<TKey, TValue> GetAt(long index) => _items[index];

    /// <summary>
    /// Replaces the value of the entry at the specified position, leaving its key unchanged.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to update.</param>
    /// <param name="value">The new value to store.</param>
    public void SetAt(long index, TValue value) {
        KeyValuePair<TKey, TValue> pair = _items[index];
        _items[index] = new KeyValuePair<TKey, TValue>(pair.Key, value);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => key is not null && _indexByKey.ContainsKey(key);

    /// <summary>
    /// Determines whether the dictionary contains an entry with the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        for (long i = 0; i < _items.Count; i++) {
            if (comparer.Equals(_items[i].Value, value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the position of the entry with the specified key in the dictionary's order.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The zero-based index of the key, or -1 if the key is not found.</returns>
    public long IndexOf(TKey key) {
        if (key is null)
            return -1;
        return _indexByKey.TryGetValue(key, out long index) ? index : -1;
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value) {
        if (key is not null && _indexByKey.TryGetValue(key, out long index)) {
            value = _items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool Remove(TKey key) {
        if (key is null || !_indexByKey.TryGetValue(key, out long index))
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the entry with the specified key and returns its value.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">When this method returns, contains the value of the removed entry if found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an entry was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(TKey key, out TValue value) {
        if (TryGetValue(key, out value))
            return Remove(key);
        return false;
    }

    /// <inheritdoc />
    public void RemoveAt(long index) {
        KeyValuePair<TKey, TValue> pair = _items[index];
        _items.RemoveAt(index);
        _indexByKey.Remove(pair.Key);
        ReindexFrom(index);
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out long index))
            return false;
        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;
        return Remove(item.Key);
    }

    /// <inheritdoc />
    public long IndexOf(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out long index))
            return -1;
        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value) ? index : -1;
    }

    /// <inheritdoc />
    public void Clear() {
        _items.Clear();
        _indexByKey.Clear();
    }

    /// <inheritdoc />
    public void Compact() {
        _items.Compact();
        _indexByKey.Compact();
    }

    /// <inheritdoc />
    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        for (long i = 0; i < _items.Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    /// <summary>
    /// Ensures that the dictionary can hold at least the specified number of elements without resizing.
    /// </summary>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <returns>The capacity of the dictionary after the call.</returns>
    public long EnsureCapacity(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (_items.Capacity < capacity)
            _items.Capacity = capacity;
        return _items.Capacity;
    }

    /// <summary>
    /// Reduces the capacity of the dictionary to match the current number of elements.
    /// </summary>
    public void TrimExcess() => _items.Capacity = _items.Count;

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    void IWideDictionary.Remove(object key) {
        ArgumentNullException.ThrowIfNull(key);
        if (key is TKey typedKey)
            Remove(typedKey);
    }

    long IWideList.Add(object value) {
        if (value is not KeyValuePair<TKey, TValue> pair)
            throw new ArgumentException($"Value must be of type {typeof(KeyValuePair<TKey, TValue>)}.", nameof(value));
        Add(pair.Key, pair.Value);
        return Count - 1;
    }

    bool IWideList.Contains(object value) => value is KeyValuePair<TKey, TValue> pair && Contains(pair);

    long IWideList.IndexOf(object value) => value is KeyValuePair<TKey, TValue> pair ? IndexOf(pair) : -1;

    void IWideList.Insert(long index, object value) {
        if (value is not KeyValuePair<TKey, TValue> pair)
            throw new ArgumentException($"Value must be of type {typeof(KeyValuePair<TKey, TValue>)}.", nameof(value));
        Insert(index, pair);
    }

    void IWideList.Remove(object value) {
        if (value is KeyValuePair<TKey, TValue> pair)
            Remove(pair);
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IWideDictionary.GetEnumerator() => new DictionaryEnumerator(this);

    private void SetValueByKey(TKey key, TValue value) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.TryGetValue(key, out long index)) {
            _items[index] = new KeyValuePair<TKey, TValue>(key, value);
            return;
        }

        Add(key, value);
    }

    private void SetItemAtIndex(long index, KeyValuePair<TKey, TValue> item) {
        KeyValuePair<TKey, TValue> existing = _items[index];
        if (!Comparer.Equals(existing.Key, item.Key) && _indexByKey.ContainsKey(item.Key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(item));

        _items[index] = item;
        if (!Comparer.Equals(existing.Key, item.Key)) {
            _indexByKey.Remove(existing.Key);
            _indexByKey[item.Key] = index;
        }
    }

    private long FindIndexByKey(TKey key) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.TryGetValue(key, out long index))
            return index;
        throw new KeyNotFoundException("The given key was not present in the dictionary.");
    }

    private void ReindexFrom(long startIndex) {
        for (long i = startIndex; i < _items.Count; i++)
            _indexByKey[_items[i].Key] = i;
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
    /// Enumerates the key/value pairs of a <see cref="WideOrderedDictionary{TKey, TValue}"/> in insertion order.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideOrderedDictionary<TKey, TValue> dictionary) {
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

    private sealed class DictionaryEnumerator(WideOrderedDictionary<TKey, TValue> dictionary) : IDictionaryEnumerator {
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
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideOrderedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
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
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideOrderedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
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

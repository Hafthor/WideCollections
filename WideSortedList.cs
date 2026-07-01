using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of key/value pairs that are sorted by key and accessible by key and by index.
/// Backed by a <see cref="WideList{T}"/> so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideSortedList<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, 
    IWideReadOnlyDictionary<TKey, TValue>, ICompactable where TKey : notnull {
    private readonly WideList<KeyValuePair<TKey, TValue>> _items;
    private KeyCollection _keys;
    private ValueCollection _values;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideSortedList{TKey, TValue}"/> class
    /// with the specified initial capacity and key comparer.
    /// </summary>
    /// <param name="capacity">The initial number of elements the list can hold before resizing.</param>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedList(long capacity = 0, IComparer<TKey> comparer = null) : this(comparer) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items.Capacity = capacity;
    }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideSortedList{TKey, TValue}"/> class
    /// that orders keys using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedList(IComparer<TKey> comparer) {
        _items = new WideList<KeyValuePair<TKey, TValue>>();
        Comparer = comparer ?? Comparer<TKey>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideSortedList{TKey, TValue}"/> class that contains
    /// the key/value pairs copied from the specified collection, ordered by the specified comparer.
    /// </summary>
    /// <param name="collection">The collection whose key/value pairs are copied.</param>
    /// <param name="comparer">The comparer used to order keys, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedList(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey> comparer = null) : this(comparer) {
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
    /// Gets the comparer used to order the keys of the list.
    /// </summary>
    public IComparer<TKey> Comparer { get; }

    /// <summary>
    /// Gets or sets the number of elements the list can hold before its internal storage must resize.
    /// </summary>
    public long Capacity {
        get => _items.Capacity;
        set => _items.Capacity = value;
    }

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
            KeyValuePair<TKey, TValue> pair = new(key, value);
            if (index >= 0)
                _items[index] = pair;
            else
                _items.Insert(~index, pair);
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

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => FindIndex(key) >= 0;

    /// <summary>
    /// Determines whether the list contains an entry with the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value) => IndexOfValue(value) >= 0;

    /// <summary>
    /// Attempts to add the specified key and value to the list. Does nothing if the key already exists.
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
    /// Returns the key at the specified index in the sorted order.
    /// </summary>
    /// <param name="index">The zero-based index of the key to retrieve.</param>
    /// <returns>The key at <paramref name="index"/>.</returns>
    public TKey GetKeyAtIndex(long index) => _items[index].Key;

    /// <summary>
    /// Returns the value at the specified index in the sorted order.
    /// </summary>
    /// <param name="index">The zero-based index of the value to retrieve.</param>
    /// <returns>The value at <paramref name="index"/>.</returns>
    public TValue GetValueAtIndex(long index) => _items[index].Value;

    /// <summary>
    /// Replaces the value stored at the specified index, leaving its key unchanged.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to update.</param>
    /// <param name="value">The new value to store.</param>
    public void SetValueAtIndex(long index, TValue value) => 
        _items[index] = new KeyValuePair<TKey, TValue>(_items[index].Key, value);

    /// <summary>
    /// Removes the entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to remove.</param>
    public void RemoveAt(long index) => _items.RemoveAt(index);

    /// <summary>
    /// Returns the index of the specified key in the sorted order.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The zero-based index of the key, or -1 if the key is not found.</returns>
    public long IndexOfKey(TKey key) {
        long index = FindIndex(key);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Returns the index of the first entry that has the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The zero-based index of the first matching value, or -1 if no entry has the value.</returns>
    public long IndexOfValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        for (long i = 0; i < Count; i++)
            if (comparer.Equals(_items[i].Value, value))
                return i;

        return -1;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) {
        long index = FindIndex(item.Key);
        if (index < 0)
            return false;
        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;
        return Remove(item.Key);
    }

    void IWideDictionary.Remove(object key) {
        ArgumentNullException.ThrowIfNull(key);

        if (key is TKey typedKey)
            Remove(typedKey);
    }

    /// <inheritdoc />
    public void Clear() => _items.Clear();

    /// <inheritdoc />
    public void Compact() => _items.Compact();

    /// <summary>
    /// Ensures that the list can hold at least the specified number of elements without resizing.
    /// </summary>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <returns>The capacity of the list after the call.</returns>
    public long EnsureCapacity(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (_items.Capacity < capacity)
            _items.Capacity = capacity;
        return _items.Capacity;
    }

    /// <summary>
    /// Reduces the capacity of the list to match the current number of elements.
    /// </summary>
    public void TrimExcess() => _items.Capacity = _items.Count;

    /// <inheritdoc />
    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex + Count, array.Length);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IWideDictionary.GetEnumerator() => new DictionaryEnumerator(this);

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

    private long FindIndex(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        long lo = 0, hi = Count - 1;
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

    /// <summary>
    /// Enumerates the key/value pairs of a <see cref="WideSortedList{TKey, TValue}"/> in key order.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideSortedList<TKey, TValue> _list;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideSortedList<TKey, TValue> list) {
            _list = list;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        /// <inheritdoc />
        public bool MoveNext() {
            if (_index < _list.Count) {
                _current = _list._items[_index];
                _index++;
                return true;
            }

            _current = default;
            _index = _list.Count + 1;
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

    private sealed class DictionaryEnumerator(WideSortedList<TKey, TValue> list) : IDictionaryEnumerator {
        private Enumerator _enumerator = list.GetEnumerator();
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
        private readonly WideSortedList<TKey, TValue> _list;

        internal KeyCollection(WideSortedList<TKey, TValue> list) : base(list.SyncRoot)
            => _list = list;

        /// <summary>
        /// Gets the number of keys in the list.
        /// </summary>
        public override long Count => _list.Count;
        /// <summary>
        /// Determines whether the collection contains the specified key.
        /// </summary>
        /// <param name="item">The key to locate.</param>
        /// <returns><see langword="true"/> if the key is found; otherwise <see langword="false"/>.</returns>
        public override bool Contains(TKey item) => _list.ContainsKey(item);

        protected override TKey GetElementAt(long index) => _list._items[(int)index].Key;
    }

    private sealed class ValueCollection : WideKeyValueCollectionBase<TValue> {
        private readonly WideSortedList<TKey, TValue> _list;

        internal ValueCollection(WideSortedList<TKey, TValue> list) : base(list.SyncRoot)
            => _list = list;

        /// <summary>
        /// Gets the number of values in the list.
        /// </summary>
        public override long Count => _list.Count;
        /// <summary>
        /// Determines whether the collection contains the specified value.
        /// </summary>
        /// <param name="item">The value to locate.</param>
        /// <returns><see langword="true"/> if the value is found; otherwise <see langword="false"/>.</returns>
        public override bool Contains(TValue item) {
            var comparer = EqualityComparer<TValue>.Default;
            for (long i = 0; i < _list.Count; i++) {
                if (comparer.Equals(_list._items[i].Value, item))
                    return true;
            }
            return false;
        }

        protected override TValue GetElementAt(long index) => _list._items[(int)index].Value;
    }
}

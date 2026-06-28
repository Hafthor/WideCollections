using System.Collections;

namespace WideCollections;

public class WideOrderedDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideList<KeyValuePair<TKey, TValue>>,
    IWideReadOnlyDictionary<TKey, TValue>, IWideReadOnlyList<KeyValuePair<TKey, TValue>>, IWideDictionary, IWideList,
    ICompactable where TKey : notnull {
    private readonly WideList<KeyValuePair<TKey, TValue>> _items;
    private readonly WideDictionary<TKey, long> _indexByKey;
    private KeyCollection _keys;
    private ValueCollection _values;

    public WideOrderedDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

    public WideOrderedDictionary(long capacity = 0, IEqualityComparer<TKey> comparer = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideList<KeyValuePair<TKey, TValue>>(capacity);
        Comparer = comparer ?? EqualityComparer<TKey>.Default;
        _indexByKey = new WideDictionary<TKey, long>(capacity, Comparer);
    }

    public WideOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer = null) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    public long Count => _items.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;
    public IEqualityComparer<TKey> Comparer { get; }

    public long Capacity {
        get => _items.Capacity;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);
            _items.Capacity = value;
        }
    }

    public TValue this[TKey key] {
        get => _items[FindIndexByKey(key)].Value;
        set => SetValueByKey(key, value);
    }

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

    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    public IWideCollection<TValue> Values => _values ??= new ValueCollection(this);
    IEnumerable<TKey> IWideReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IWideReadOnlyDictionary<TKey, TValue>.Values => Values;
    IWideCollection IWideDictionary.Keys => (IWideCollection)Keys;
    IWideCollection IWideDictionary.Values => (IWideCollection)Values;

    public void Add(TKey key, TValue value) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.ContainsKey(key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));

        long index = _items.Count;
        _items.Add(new KeyValuePair<TKey, TValue>(key, value));
        _indexByKey.Add(key, index);
    }

    public bool TryAdd(TKey key, TValue value) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.ContainsKey(key))
            return false;

        Add(key, value);
        return true;
    }

    public void Insert(long index, TKey key, TValue value) => Insert(index, new KeyValuePair<TKey, TValue>(key, value));

    public void Insert(long index, KeyValuePair<TKey, TValue> item) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        if (_indexByKey.ContainsKey(item.Key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(item));

        _items.Insert(index, item);
        ReindexFrom(index);
    }

    public KeyValuePair<TKey, TValue> GetAt(long index) => _items[index];

    public void SetAt(long index, TValue value) {
        KeyValuePair<TKey, TValue> pair = _items[index];
        _items[index] = new KeyValuePair<TKey, TValue>(pair.Key, value);
    }

    public bool ContainsKey(TKey key) => key is not null && _indexByKey.ContainsKey(key);

    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        for (long i = 0; i < _items.Count; i++) {
            if (comparer.Equals(_items[i].Value, value))
                return true;
        }

        return false;
    }

    public long IndexOf(TKey key) {
        if (key is null)
            return -1;
        return _indexByKey.TryGetValue(key, out long index) ? index : -1;
    }

    public bool TryGetValue(TKey key, out TValue value) {
        if (key is not null && _indexByKey.TryGetValue(key, out long index)) {
            value = _items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool Remove(TKey key) {
        if (key is null || !_indexByKey.TryGetValue(key, out long index))
            return false;

        RemoveAt(index);
        return true;
    }

    public bool Remove(TKey key, out TValue value) {
        if (TryGetValue(key, out value))
            return Remove(key);
        return false;
    }

    public void RemoveAt(long index) {
        KeyValuePair<TKey, TValue> pair = _items[index];
        _items.RemoveAt(index);
        _indexByKey.Remove(pair.Key);
        ReindexFrom(index);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out long index))
            return false;
        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;
        return Remove(item.Key);
    }

    public long IndexOf(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out long index))
            return -1;
        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value) ? index : -1;
    }

    public void Clear() {
        _items.Clear();
        _indexByKey.Clear();
    }

    public void Compact() {
        _items.Compact();
        _indexByKey.Compact();
    }

    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        for (long i = 0; i < _items.Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    public long EnsureCapacity(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (_items.Capacity < capacity)
            _items.Capacity = capacity;
        return _items.Capacity;
    }

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

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideOrderedDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _index = 0;
            _current = default;
        }

        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

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

        public void Reset() {
            _index = 0;
            _current = default;
        }

        public void Dispose() { }
    }

    private sealed class DictionaryEnumerator(WideOrderedDictionary<TKey, TValue> dictionary) : IDictionaryEnumerator {
        private Enumerator _enumerator = dictionary.GetEnumerator();
        private bool _valid;

        public DictionaryEntry Entry {
            get {
                if (!_valid)
                    throw new InvalidOperationException("Enumerator is not on a valid element.");

                KeyValuePair<TKey, TValue> current = _enumerator.Current;
                return new DictionaryEntry(current.Key!, current.Value);
            }
        }

        public object Key => Entry.Key;
        public object Value => Entry.Value;
        public object Current => Entry;

        public bool MoveNext() {
            _valid = _enumerator.MoveNext();
            return _valid;
        }

        public void Reset() {
            _enumerator.Reset();
            _valid = false;
        }
    }

    private sealed class KeyCollection : WideKeyValueCollectionBase<TKey> {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideOrderedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
            => _dictionary = dictionary;

        public override long Count => _dictionary.Count;
        public override bool Contains(TKey item) => _dictionary.ContainsKey(item);

        protected override TKey GetElementAt(long index) => _dictionary._items[(int)index].Key;
    }

    private sealed class ValueCollection : WideKeyValueCollectionBase<TValue> {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideOrderedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
            => _dictionary = dictionary;

        public override long Count => _dictionary.Count;
        public override bool Contains(TValue item) => _dictionary.ContainsValue(item);

        protected override TValue GetElementAt(long index) => _dictionary._items[(int)index].Value;
    }
}

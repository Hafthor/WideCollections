using System.Collections;

namespace WideCollections;

public class WideOrderedDictionary<TKey, TValue> : IWideCollection<KeyValuePair<TKey, TValue>>,
    IWideDictionary<TKey, TValue>,
    IWideList<KeyValuePair<TKey, TValue>>,
    IWideReadOnlyCollection<KeyValuePair<TKey, TValue>>,
    IWideReadOnlyDictionary<TKey, TValue>,
    IWideReadOnlyList<KeyValuePair<TKey, TValue>>,
    IWideCollection,
    IWideDictionary,
    IWideList
    where TKey : notnull {
    private readonly List<KeyValuePair<TKey, TValue>> _items;
    private readonly Dictionary<TKey, int> _indexByKey;
    private KeyCollection _keys;
    private ValueCollection _values;

    public WideOrderedDictionary() : this(0, null) { }

    public WideOrderedDictionary(long capacity) : this(capacity, null) { }

    public WideOrderedDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

    public WideOrderedDictionary(long capacity, IEqualityComparer<TKey> comparer) {
        if (capacity < 0 || capacity > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 0 and Int32.MaxValue.");

        _items = new List<KeyValuePair<TKey, TValue>>((int)capacity);
        _indexByKey = new Dictionary<TKey, int>((int)capacity, comparer ?? EqualityComparer<TKey>.Default);
        SyncRoot = new object();
    }

    public WideOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

    public WideOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    public long Count => _items.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;
    public IEqualityComparer<TKey> Comparer => _indexByKey.Comparer;

    public long Capacity {
        get => _items.Capacity;
        set {
            if (value < Count || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be less than Count and must fit in Int32.");
            _items.Capacity = (int)value;
        }
    }

    public TValue this[TKey key] {
        get => _items[FindIndexByKey(key)].Value;
        set => SetValueByKey(key, value);
    }

    public KeyValuePair<TKey, TValue> this[long index] {
        get => _items[ValidateIndex(index)];
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

        int index = _items.Count;
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
        int insertIndex = ValidateInsertIndex(index);
        if (_indexByKey.ContainsKey(item.Key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(item));

        _items.Insert(insertIndex, item);
        ReindexFrom(insertIndex);
    }

    public KeyValuePair<TKey, TValue> GetAt(long index) => this[index];

    public void SetAt(long index, TValue value) {
        int itemIndex = ValidateIndex(index);
        KeyValuePair<TKey, TValue> existing = _items[itemIndex];
        _items[itemIndex] = new KeyValuePair<TKey, TValue>(existing.Key, value);
    }

    public bool ContainsKey(TKey key) => key is not null && _indexByKey.ContainsKey(key);

    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        foreach (KeyValuePair<TKey, TValue> pair in _items) {
            if (comparer.Equals(pair.Value, value))
                return true;
        }

        return false;
    }

    public long IndexOf(TKey key) {
        if (key is null)
            return -1;

        return _indexByKey.TryGetValue(key, out int index) ? index : -1;
    }

    public bool TryGetValue(TKey key, out TValue value) {
        if (key is not null && _indexByKey.TryGetValue(key, out int index)) {
            value = _items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool Remove(TKey key) {
        if (key is null || !_indexByKey.TryGetValue(key, out int index))
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
        int itemIndex = ValidateIndex(index);
        KeyValuePair<TKey, TValue> existing = _items[itemIndex];
        _items.RemoveAt(itemIndex);
        _indexByKey.Remove(existing.Key);
        ReindexFrom(itemIndex);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out int index))
            return false;

        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;

        return Remove(item.Key);
    }

    public long IndexOf(KeyValuePair<TKey, TValue> item) {
        if (!_indexByKey.TryGetValue(item.Key, out int index))
            return -1;

        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value) ? index : -1;
    }

    public void Clear() {
        _items.Clear();
        _indexByKey.Clear();
    }

    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
        if (arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        for (int i = 0; i < _items.Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    public long EnsureCapacity(long capacity) {
        if (capacity < 0 || capacity > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 0 and Int32.MaxValue.");

        if (_items.Capacity < (int)capacity)
            _items.Capacity = (int)capacity;

        return _items.Capacity;
    }

    public void TrimExcess() => _items.TrimExcess();

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    void IWideDictionary.Remove(object key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

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
        if (_indexByKey.TryGetValue(key, out int index)) {
            _items[index] = new KeyValuePair<TKey, TValue>(key, value);
            return;
        }

        Add(key, value);
    }

    private void SetItemAtIndex(long index, KeyValuePair<TKey, TValue> item) {
        int itemIndex = ValidateIndex(index);
        KeyValuePair<TKey, TValue> existing = _items[itemIndex];

        if (!Comparer.Equals(existing.Key, item.Key) && _indexByKey.ContainsKey(item.Key))
            throw new ArgumentException("An item with the same key has already been added.", nameof(item));

        _items[itemIndex] = item;
        if (!Comparer.Equals(existing.Key, item.Key)) {
            _indexByKey.Remove(existing.Key);
            _indexByKey[item.Key] = itemIndex;
        }
    }

    private int FindIndexByKey(TKey key) {
        ArgumentNullException.ThrowIfNull(key);
        if (_indexByKey.TryGetValue(key, out int index))
            return index;

        throw new KeyNotFoundException("The given key was not present in the dictionary.");
    }

    private void ReindexFrom(int startIndex) {
        for (int i = startIndex; i < _items.Count; i++)
            _indexByKey[_items[i].Key] = i;
    }

    private int ValidateIndex(long index) {
        if (index < 0 || index >= Count || index > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        return (int)index;
    }

    private int ValidateInsertIndex(long index) {
        if (index < 0 || index > Count || index > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        return (int)index;
    }

    private static void ValidateObjectKey(object key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (key is not TKey)
            throw new ArgumentException($"Key must be of type {typeof(TKey)}.", nameof(key));
    }

    private static void ValidateObjectValue(object value) {
        if (value is null && default(TValue) is not null)
            throw new ArgumentNullException(nameof(value));
        if (value is not TValue && (value is not null || default(TValue) is not null))
            throw new ArgumentException($"Value must be of type {typeof(TValue)}.", nameof(value));
    }

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private List<KeyValuePair<TKey, TValue>>.Enumerator _enumerator;

        internal Enumerator(WideOrderedDictionary<TKey, TValue> dictionary) {
            _enumerator = dictionary._items.GetEnumerator();
        }

        public KeyValuePair<TKey, TValue> Current => _enumerator.Current;
        object IEnumerator.Current => _enumerator.Current;

        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => ((IEnumerator)_enumerator).Reset();
        public void Dispose() => _enumerator.Dispose();
    }

    private sealed class DictionaryEnumerator : IDictionaryEnumerator {
        private List<KeyValuePair<TKey, TValue>>.Enumerator _enumerator;

        public DictionaryEnumerator(WideOrderedDictionary<TKey, TValue> dictionary) {
            _enumerator = dictionary._items.GetEnumerator();
        }

        public DictionaryEntry Entry {
            get {
                KeyValuePair<TKey, TValue> current = _enumerator.Current;
                return new DictionaryEntry(current.Key!, current.Value);
            }
        }

        public object Key => _enumerator.Current.Key!;
        public object Value => _enumerator.Current.Value!;
        public object Current => Entry;

        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => ((IEnumerator)_enumerator).Reset();
    }

    public sealed class KeyCollection : IWideCollection<TKey>, IWideCollection {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideOrderedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

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
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            for (int i = 0; i < _dictionary._items.Count; i++)
                array[arrayIndex + i] = _dictionary._items[i].Key;
        }

        public void Add(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TKey> GetEnumerator() {
            foreach (KeyValuePair<TKey, TValue> pair in _dictionary._items)
                yield return pair.Key;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ValueCollection : IWideCollection<TValue>, IWideCollection {
        private readonly WideOrderedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideOrderedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

        public long Count => _dictionary.Count;
        public bool IsReadOnly => true;
        public object SyncRoot => _dictionary.SyncRoot;
        public bool IsSynchronized => false;

        public bool Contains(TValue item) => _dictionary.ContainsValue(item);

        public void CopyTo(WideArray<TValue> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
            if (arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            for (int i = 0; i < _dictionary._items.Count; i++)
                array[arrayIndex + i] = _dictionary._items[i].Value;
        }

        public void Add(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TValue> GetEnumerator() {
            foreach (KeyValuePair<TKey, TValue> pair in _dictionary._items)
                yield return pair.Value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

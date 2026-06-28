using System.Collections;

namespace WideCollections;

public class WideSortedDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, 
    IWideReadOnlyDictionary<TKey, TValue>, ICompactable where TKey : notnull {
    private readonly WideList<KeyValuePair<TKey, TValue>> _items;
    private KeyCollection _keys;
    private ValueCollection _values;

    public WideSortedDictionary(IComparer<TKey> comparer = null) {
        _items = new WideList<KeyValuePair<TKey, TValue>>();
        Comparer = comparer ?? Comparer<TKey>.Default;
    }

    public WideSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer = null) : this(comparer) {
        ArgumentNullException.ThrowIfNull(dictionary);
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            Add(pair.Key, pair.Value);
    }

    public WideSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey> comparer = null) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (KeyValuePair<TKey, TValue> pair in collection)
            Add(pair.Key, pair.Value);
    }

    public long Count => _items.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;
    public IComparer<TKey> Comparer { get; }

    internal long InternalItemsCapacity => _items.Capacity;

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

    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    public IWideCollection<TValue> Values => _values ??= new ValueCollection(this);
    IEnumerable<TKey> IWideReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IWideReadOnlyDictionary<TKey, TValue>.Values => Values;
    IWideCollection IWideDictionary.Keys => (IWideCollection)Keys;
    IWideCollection IWideDictionary.Values => (IWideCollection)Values;

    public void Add(TKey key, TValue value) {
        long index = FindIndex(key);
        if (index >= 0)
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));

        _items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
    }

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

    public bool ContainsKey(TKey key) => FindIndex(key) >= 0;

    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        for (long i = 0; i < _items.Count; i++)
            if (comparer.Equals(_items[i].Value, value))
                return true;

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) {
        long index = FindIndex(key);
        if (index >= 0) {
            value = _items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool Remove(TKey key) {
        long index = FindIndex(key);
        if (index < 0)
            return false;

        _items.RemoveAt(index);
        return true;
    }

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

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        long index = FindIndex(item.Key);
        if (index < 0)
            return false;

        return EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

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

    public void Clear() => _items.Clear();

    public void Compact() => _items.Compact();

    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[i];
    }

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

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;
        private long _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(WideSortedDictionary<TKey, TValue> dictionary) {
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

    private sealed class DictionaryEnumerator(WideSortedDictionary<TKey, TValue> dictionary) : IDictionaryEnumerator {
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
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideSortedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
            => _dictionary = dictionary;

        public override long Count => _dictionary.Count;
        public override bool Contains(TKey item) => _dictionary.ContainsKey(item);

        protected override TKey GetElementAt(long index) => _dictionary._items[(int)index].Key;
    }

    private sealed class ValueCollection : WideKeyValueCollectionBase<TValue> {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideSortedDictionary<TKey, TValue> dictionary) : base(dictionary.SyncRoot)
            => _dictionary = dictionary;

        public override long Count => _dictionary.Count;
        public override bool Contains(TValue item) => _dictionary.ContainsValue(item);

        protected override TValue GetElementAt(long index) => _dictionary._items[(int)index].Value;
    }
}

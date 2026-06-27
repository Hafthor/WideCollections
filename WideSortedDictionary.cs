using System.Collections;

namespace WideCollections;

public class WideSortedDictionary<TKey, TValue> : IWideDictionary<TKey, TValue>, IWideDictionary, IWideReadOnlyDictionary<TKey, TValue>
    where TKey : notnull {
    private readonly SortedDictionary<TKey, TValue> _dictionary;
    private KeyCollection _keys;
    private ValueCollection _values;

    public WideSortedDictionary() : this((IComparer<TKey>)null) { }

    public WideSortedDictionary(IComparer<TKey> comparer) {
        _dictionary = comparer is null ? new SortedDictionary<TKey, TValue>() : new SortedDictionary<TKey, TValue>(comparer);
        SyncRoot = new object();
    }

    public WideSortedDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) { }

    public WideSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer) : this(comparer) {
        ArgumentNullException.ThrowIfNull(dictionary);
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            _dictionary.Add(pair.Key, pair.Value);
    }

    public WideSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

    public WideSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey> comparer) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (KeyValuePair<TKey, TValue> pair in collection)
            _dictionary.Add(pair.Key, pair.Value);
    }

    public long Count => _dictionary.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;
    public IComparer<TKey> Comparer => _dictionary.Comparer;

    public TValue this[TKey key] {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    object IWideDictionary.this[object key] {
        get {
            ValidateObjectKey(key);
            return _dictionary[(TKey)key]!;
        }
        set {
            ValidateObjectKey(key);
            ValidateObjectValue(value);
            _dictionary[(TKey)key] = (TValue)value;
        }
    }

    public IWideCollection<TKey> Keys => _keys ??= new KeyCollection(this);
    public IWideCollection<TValue> Values => _values ??= new ValueCollection(this);
    IEnumerable<TKey> IWideReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IWideReadOnlyDictionary<TKey, TValue>.Values => Values;
    IWideCollection IWideDictionary.Keys => (IWideCollection)Keys;
    IWideCollection IWideDictionary.Values => (IWideCollection)Values;

    public void Add(TKey key, TValue value) => _dictionary.Add(key, value);

    public bool TryAdd(TKey key, TValue value) {
        if (_dictionary.ContainsKey(key))
            return false;

        _dictionary.Add(key, value);
        return true;
    }

    void IWideCollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    void IWideDictionary.Add(object key, object value) {
        ValidateObjectKey(key);
        ValidateObjectValue(value);
        Add((TKey)key, (TValue)value);
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) {
        var comparer = EqualityComparer<TValue>.Default;
        foreach (TValue current in _dictionary.Values) {
            if (comparer.Equals(current, value))
                return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    public bool Remove(TKey key) => _dictionary.Remove(key);

    public bool Remove(TKey key, out TValue value) {
        if (_dictionary.TryGetValue(key, out value))
            return _dictionary.Remove(key);

        return false;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        _dictionary.TryGetValue(item.Key, out TValue value) &&
        EqualityComparer<TValue>.Default.Equals(value, item.Value);

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;

        return _dictionary.Remove(item.Key);
    }

    bool IWideDictionary.Contains(object key) => key is TKey typedKey && _dictionary.ContainsKey(typedKey);

    void IWideDictionary.Remove(object key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key is TKey typedKey)
            _dictionary.Remove(typedKey);
    }

    public void Clear() => _dictionary.Clear();

    public void CopyTo(WideArray<KeyValuePair<TKey, TValue>> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
        if (arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        long i = arrayIndex;
        foreach (KeyValuePair<TKey, TValue> pair in _dictionary) {
            array[i] = pair;
            i++;
        }
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IWideDictionary.GetEnumerator() => new DictionaryEnumerator(_dictionary.GetEnumerator());

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
        private SortedDictionary<TKey, TValue>.Enumerator _enumerator;

        internal Enumerator(WideSortedDictionary<TKey, TValue> dictionary) {
            _enumerator = dictionary._dictionary.GetEnumerator();
        }

        public KeyValuePair<TKey, TValue> Current => _enumerator.Current;
        object IEnumerator.Current => _enumerator.Current;

        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => ((IEnumerator)_enumerator).Reset();
        public void Dispose() => _enumerator.Dispose();
    }

    private sealed class DictionaryEnumerator : IDictionaryEnumerator {
        private SortedDictionary<TKey, TValue>.Enumerator _enumerator;

        public DictionaryEnumerator(SortedDictionary<TKey, TValue>.Enumerator enumerator) {
            _enumerator = enumerator;
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
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(WideSortedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

        public long Count => _dictionary.Count;
        public bool IsReadOnly => true;
        public object SyncRoot => _dictionary.SyncRoot;
        public bool IsSynchronized => false;

        public bool Contains(TKey item) => _dictionary._dictionary.ContainsKey(item);

        public void CopyTo(WideArray<TKey> array, long arrayIndex) {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
            if (arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            long i = arrayIndex;
            foreach (TKey key in _dictionary._dictionary.Keys) {
                array[i] = key;
                i++;
            }
        }

        public void Add(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TKey item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TKey> GetEnumerator() => _dictionary._dictionary.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ValueCollection : IWideCollection<TValue>, IWideCollection {
        private readonly WideSortedDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(WideSortedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

        public long Count => _dictionary.Count;
        public bool IsReadOnly => true;
        public object SyncRoot => _dictionary.SyncRoot;
        public bool IsSynchronized => false;

        public bool Contains(TValue item) {
            var comparer = EqualityComparer<TValue>.Default;
            foreach (TValue value in _dictionary._dictionary.Values) {
                if (comparer.Equals(value, item))
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
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination does not have enough space.", nameof(array));

            long i = arrayIndex;
            foreach (TValue value in _dictionary._dictionary.Values) {
                array[i] = value;
                i++;
            }
        }

        public void Add(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public bool Remove(TValue item) => throw new NotSupportedException("Collection is read-only.");
        public void Clear() => throw new NotSupportedException("Collection is read-only.");

        public IEnumerator<TValue> GetEnumerator() => _dictionary._dictionary.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

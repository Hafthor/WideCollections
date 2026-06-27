using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WideCollections;

public interface IWideDictionary : IWideCollection {
    object this[object key] { get; set; }
    IWideCollection Keys { get; }
    IWideCollection Values { get; }
    bool Contains(object key);
    void Add(object key, object value);
    void Clear();
    bool IsReadOnly { get; }
    bool IsFixedSize { get; }
    new IDictionaryEnumerator GetEnumerator();
    void Remove(object key);
}

public interface IWideDictionary<TKey, TValue> : IWideCollection<KeyValuePair<TKey, TValue>> {
    TValue this[TKey key] { get; set; }
    IWideCollection<TKey> Keys { get; }
    IWideCollection<TValue> Values { get; }
    bool ContainsKey(TKey key);
    void Add(TKey key, TValue value);
    bool Remove(TKey key);
    bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
}
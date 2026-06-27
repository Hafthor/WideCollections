using System.Diagnostics.CodeAnalysis;

namespace WideCollections;

public interface IWideReadOnlyDictionary<TKey, TValue> : IWideReadOnlyCollection<KeyValuePair<TKey, TValue>> {
    bool ContainsKey(TKey key);
    bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
    TValue this[TKey key] { get; }
    IEnumerable<TKey> Keys { get; }
    IEnumerable<TValue> Values { get; }
}
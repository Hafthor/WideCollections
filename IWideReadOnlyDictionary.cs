using System.Diagnostics.CodeAnalysis;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only dictionary with wide collection semantics.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IWideReadOnlyDictionary<TKey, TValue> : IWideReadOnlyCollection<KeyValuePair<TKey, TValue>> {
    /// <summary>
    /// Determines whether the dictionary contains an element with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    TValue this[TKey key] { get; }

    /// <summary>
    /// Gets an enumerable collection of dictionary keys.
    /// </summary>
    IEnumerable<TKey> Keys { get; }

    /// <summary>
    /// Gets an enumerable collection of dictionary values.
    /// </summary>
    IEnumerable<TValue> Values { get; }
}
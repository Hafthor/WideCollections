using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a non-generic mutable dictionary with wide collection semantics.
/// </summary>
public interface IWideDictionary : IWideCollection {
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get or set.</param>
    object this[object key] { get; set; }

    /// <summary>
    /// Gets the collection of keys.
    /// </summary>
    IWideCollection Keys { get; }

    /// <summary>
    /// Gets the collection of values.
    /// </summary>
    IWideCollection Values { get; }

    /// <summary>
    /// Determines whether the dictionary contains an element with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool Contains(object key);

    /// <summary>
    /// Adds an element with the specified key and value.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    void Add(object key, object value);

    /// <summary>
    /// Removes all elements from the dictionary.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether the dictionary has a fixed size.
    /// </summary>
    bool IsFixedSize { get; }

    /// <summary>
    /// Returns an enumerator that iterates through dictionary entries.
    /// </summary>
    new IDictionaryEnumerator GetEnumerator();

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    void Remove(object key);
}

/// <summary>
/// Represents a mutable dictionary with wide collection semantics.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IWideDictionary<TKey, TValue> : IWideCollection<KeyValuePair<TKey, TValue>> {
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get or set.</param>
    TValue this[TKey key] { get; set; }

    /// <summary>
    /// Gets the collection of keys.
    /// </summary>
    IWideCollection<TKey> Keys { get; }

    /// <summary>
    /// Gets the collection of values.
    /// </summary>
    IWideCollection<TValue> Values { get; }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Adds an element with the specified key and value.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    void Add(TKey key, TValue value);

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><see langword="true"/> if removed; otherwise, <see langword="false"/>.</returns>
    bool Remove(TKey key);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
}
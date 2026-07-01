namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a mutable list with long-based indexing.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideList<T> : IWideCollection<T>, IWideEnumerable<T> {
    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    T this[long index] { get; set; }

    /// <summary>
    /// Finds the index of the first occurrence of a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index if found; otherwise, -1.</returns>
    long IndexOf(T item);

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="item">The item to insert.</param>
    void Insert(long index, T item);

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    void RemoveAt(long index);
}

/// <summary>
/// Represents a non-generic mutable list with long-based indexing.
/// </summary>
public interface IWideList : IWideCollection {
    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    object this[long index] { get; set; }

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    /// <param name="value">The item to add.</param>
    /// <returns>The index where the item was inserted.</returns>
    long Add(object value);

    /// <summary>
    /// Determines whether the list contains a specific value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool Contains(object value);

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a value indicating whether the list is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether the list has a fixed size.
    /// </summary>
    bool IsFixedSize { get; }

    /// <summary>
    /// Finds the index of the first occurrence of a specific value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The zero-based index if found; otherwise, -1.</returns>
    long IndexOf(object value);

    /// <summary>
    /// Inserts a value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="value">The value to insert.</param>
    void Insert(long index, object value);

    /// <summary>
    /// Removes the first occurrence of a specific value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    void Remove(object value);

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    void RemoveAt(long index);
}
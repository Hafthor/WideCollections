using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// A generic list backed by WideArray that can grow beyond Array.MaxLength.
/// </summary>
public class WideList<T> : IWideList<T>, IWideList, IWideReadOnlyList<T>, IWideIndexable<T>, ICompactable {
    private long _count;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    /// <inheritdoc />
    public long Count => _count;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;
    /// <inheritdoc />
    public bool IsReadOnly => false;
    /// <inheritdoc />
    public bool IsFixedSize => false;
    internal WideArray<T> Items { get; }

    /// <inheritdoc />
    public long IndexOf(object value) => value is T item ? IndexOf(item) : -1;

    /// <inheritdoc />
    public void Insert(long index, object value) => Insert(index, (T)value);

    /// <inheritdoc />
    public void Remove(object value) {
        if (value is T item)
            Remove(item);
    }

    /// <summary>
    /// Gets or sets the number of elements the list can contain before resizing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than <see cref="Count"/>.</exception>
    public long Capacity {
        get => Items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            if (value != Items.Length)
                Items.Resize(value);
        }
    }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="WideList{T}"/> class.
    /// </summary>
    public WideList() => Items = new();

    /// <summary>
    /// Initializes a new empty instance of the <see cref="WideList{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of elements the list can initially store.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public WideList(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        Items = new WideArray<T>(capacity);
    }

    /// <summary>
    /// Gets the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the list.</exception>
    public T Get(long index) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        return Items[index];
    }

    /// <summary>
    /// Replaces the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="value">The value to store at <paramref name="index"/>.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the list.</exception>
    public void Set(long index, T value) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        Items[index] = value;
    }

    /// <inheritdoc />
    public T this[long index] {
        get => Get(index);
        set => Set(index, value);
    }

    /// <inheritdoc />
    public void Add(T item) {
        if (_count >= Items.Length)
            EnsureCapacity(_count + 1);

        Items[_count] = item;
        _count++;
    }

    /// <summary>
    /// Adds the elements of a collection to the end of the list.
    /// </summary>
    /// <param name="collection">The collection whose elements are added to the list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public void AddRange(IEnumerable<T> collection) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is IWideReadOnlyCollection<T> wide)
            EnsureCapacity(_count + wide.Count);
        else if (collection is ICollection<T> coll)
            EnsureCapacity(_count + coll.Count);

        foreach (T item in collection)
            Add(item);
    }

    /// <summary>
    /// Inserts the elements of a collection into the list at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the first element.</param>
    /// <param name="collection">The collection whose elements are inserted into the list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than <see cref="Count"/>.</exception>
    public void InsertRange(long index, IEnumerable<T> collection) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);
        ArgumentNullException.ThrowIfNull(collection);

        long offset = 0;
        foreach (T item in collection)
            Insert(index + offset++, item);
    }

    /// <summary>
    /// Removes all elements that match the specified predicate.
    /// </summary>
    /// <param name="match">The predicate that identifies elements to remove.</param>
    /// <returns>The number of elements removed from the list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
    public long RemoveAll(Predicate<T> match) {
        ArgumentNullException.ThrowIfNull(match);

        long removed = 0;
        for (long i = _count - 1; i >= 0; i--)
            if (match(Items[i])) {
                RemoveAt(i);
                removed++;
            }

        return removed;
    }

    /// <summary>
    /// Reverses the order of the elements in the list.
    /// </summary>
    public void Reverse() {
        for (long lo = 0, hi = _count - 1; lo < hi; lo++, hi--)
            (Items[lo], Items[hi]) = (Items[hi], Items[lo]);
    }

    /// <inheritdoc />
    public void Insert(long index, T item) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);

        if (_count >= Items.Length)
            EnsureCapacity(_count + 1);

        // Shift elements to the right
        for (long i = _count; i > index; i--)
            Items[i] = Items[i - 1];

        Items[index] = item;
        _count++;
    }

    /// <inheritdoc />
    public void RemoveAt(long index) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        // Shift elements to the left
        for (long i = index; i < _count - 1; i++)
            Items[i] = Items[i + 1];

        _count--;
        if (ContainsReferences)
            Items[_count] = default!;
    }

    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length - _count);

        WideArray<T>.BulkCopy(Items, 0, array, arrayIndex, _count);
    }

    /// <inheritdoc />
    public bool Remove(T item) {
        long index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    /// <inheritdoc />
    public long IndexOf(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = 0; i < _count; i++)
            if (comparer.Equals(Items[i], item))
                return i;

        return -1;
    }

    /// <inheritdoc />
    public bool Contains(T item) => IndexOf(item) >= 0;

    object IWideList.this[long index] {
        get => this[index];
        set => this[index] = (T)value;
    }

    /// <inheritdoc />
    public long Add(object value) {
        Add((T)value);
        return _count - 1;
    }

    /// <inheritdoc />
    public bool Contains(object value) => Contains((T)value);

    /// <inheritdoc />
    public void Clear() {
        if (_count > 0) {
            // Clear references for reference types
            if (ContainsReferences)
                for (long i = 0; i < _count; i++)
                    Items[i] = default!;

            _count = 0;
        }
    }

    /// <inheritdoc />
    public void Compact() => Capacity = _count;

    /// <summary>
    /// Creates a writable memory view over the elements in the list.
    /// </summary>
    /// <returns>A writable memory view over the entire list.</returns>
    public WideMemory<T> AsMemory() => new(this);
    /// <summary>
    /// Creates a writable memory view over a range of elements in the list.
    /// </summary>
    /// <param name="start">The zero-based index at which the view begins.</param>
    /// <param name="length">The number of elements in the view.</param>
    /// <returns>A writable memory view over the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="length"/> is negative, or the range extends past <see cref="Count"/>.</exception>
    public WideMemory<T> AsMemory(long start, long length) => new WideMemory<T>(this).Slice(start, length);
    /// <summary>
    /// Creates a read-only memory view over the elements in the list.
    /// </summary>
    /// <returns>A read-only memory view over the entire list.</returns>
    public WideReadOnlyMemory<T> AsReadOnlyMemory() => new(this);
    /// <summary>
    /// Creates a read-only memory view over a range of elements in the list.
    /// </summary>
    /// <param name="start">The zero-based index at which the view begins.</param>
    /// <param name="length">The number of elements in the view.</param>
    /// <returns>A read-only memory view over the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="length"/> is negative, or the range extends past <see cref="Count"/>.</exception>
    public WideReadOnlyMemory<T> AsReadOnlyMemory(long start, long length) => new WideReadOnlyMemory<T>(this).Slice(start, length);

    private void EnsureCapacity(long min) {
        long newCapacity = Items.Length == 0 ? 4 : Items.Length;

        while (newCapacity < min) {
            long growth = newCapacity >> 1;
            if (growth == 0)
                growth = 1;

            newCapacity += growth;
        }

        Capacity = newCapacity;
    }

    /// <summary>
    /// Performs a binary search for the given item in the sorted list.
    /// Returns the index of the item if found; otherwise, returns a negative value.
    /// The negative value is the bitwise complement of the index where the item should be inserted.
    /// </summary>
    /// <param name="item">The item to locate in the sorted list.</param>
    /// <returns>The zero-based index of <paramref name="item"/> if found; otherwise, the bitwise complement of the insertion index.</returns>
    public long BinarySearch(T item) {
        var comparer = Comparer<T>.Default;
        long left = 0;
        long right = _count - 1;

        while (left <= right) {
            long mid = left + ((right - left) >> 1);
            int cmp = comparer.Compare(Items[mid], item);

            if (cmp == 0)
                return mid;
            else if (cmp < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left;
    }

    /// <summary>
    /// Performs a binary search for the given item using a custom comparer.
    /// Returns the index of the item if found; otherwise, returns a negative value.
    /// The negative value is the bitwise complement of the index where the item should be inserted.
    /// </summary>
    /// <param name="item">The item to locate in the sorted list.</param>
    /// <param name="comparer">The comparer used to order and compare elements.</param>
    /// <returns>The zero-based index of <paramref name="item"/> if found; otherwise, the bitwise complement of the insertion index.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
    public long BinarySearch(T item, IComparer<T> comparer) {
        ArgumentNullException.ThrowIfNull(comparer);

        long left = 0;
        long right = _count - 1;

        while (left <= right) {
            long mid = left + ((right - left) >> 1);
            int cmp = comparer.Compare(Items[mid], item);

            if (cmp == 0)
                return mid;
            else if (cmp < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < _count; i++)
            yield return Items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

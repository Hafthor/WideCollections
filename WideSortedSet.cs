using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of objects that is maintained in sorted order. Backed by a
/// <see cref="WideList{T}"/> so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideSortedSet<T> : IWideSet<T>, IWideCollection, IWideReadOnlySet<T>, ICompactable {
    private readonly WideList<T> _items;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideSortedSet{T}"/> class
    /// that uses the default comparer for the element type.
    /// </summary>
    public WideSortedSet() : this(Comparer<T>.Default) { }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideSortedSet{T}"/> class
    /// that orders elements using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to order elements, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedSet(IComparer<T> comparer) {
        _items = new WideList<T>();
        Comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideSortedSet{T}"/> class that contains the
    /// distinct elements copied from the specified collection, ordered by the default comparer.
    /// </summary>
    /// <param name="collection">The collection whose distinct elements are copied into the set.</param>
    public WideSortedSet(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideSortedSet{T}"/> class that contains the
    /// distinct elements copied from the specified collection, ordered by the specified comparer.
    /// </summary>
    /// <param name="collection">The collection whose distinct elements are copied into the set.</param>
    /// <param name="comparer">The comparer used to order elements, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WideSortedSet(IEnumerable<T> collection, IComparer<T> comparer) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        UnionWith(collection);
    }

    /// <inheritdoc />
    public long Count => _items.Count;
    /// <inheritdoc />
    public bool IsReadOnly => false;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;
    /// <summary>
    /// Gets the comparer used to order the elements of the set.
    /// </summary>
    public IComparer<T> Comparer { get; }

    /// <summary>
    /// Gets the smallest element in the set, or the default value of <typeparamref name="T"/> if the set is empty.
    /// </summary>
    public T Min => Count == 0 ? default! : _items[0];
    /// <summary>
    /// Gets the largest element in the set, or the default value of <typeparamref name="T"/> if the set is empty.
    /// </summary>
    public T Max => Count == 0 ? default! : _items[Count - 1];
    internal long InternalItemsCapacity => _items.Capacity;

    /// <inheritdoc />
    public bool Add(T item) {
        long index = FindIndex(item);
        if (index >= 0)
            return false;

        _items.Insert(~index, item);
        return true;
    }

    void IWideCollection<T>.Add(T item) => Add(item);

    /// <inheritdoc />
    public void Clear() => _items.Clear();

    /// <inheritdoc />
    public void Compact() => _items.Compact();

    /// <inheritdoc />
    public bool Contains(T item) => FindIndex(item) >= 0;

    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex + Count, array.Length);

        _items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(T item) {
        long index = FindIndex(item);
        if (index < 0)
            return false;

        _items.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes all elements that match the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The predicate that defines which elements to remove.</param>
    /// <returns>The number of elements removed from the set.</returns>
    public int RemoveWhere(Predicate<T> match) {
        ArgumentNullException.ThrowIfNull(match);

        int removed = 0;
        for (long i = Count - 1; i >= 0; i--) {
            if (match(_items[i])) {
                _items.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// Searches the set for an element equal to <paramref name="equalValue"/> and, if found, returns the stored instance.
    /// </summary>
    /// <param name="equalValue">The value to search for.</param>
    /// <param name="actualValue">When this method returns, contains the stored element equal to <paramref name="equalValue"/> if found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if a matching element was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue(T equalValue, out T actualValue) {
        long index = FindIndex(equalValue);
        if (index >= 0) {
            actualValue = _items[index];
            return true;
        }

        actualValue = default!;
        return false;
    }

    /// <summary>
    /// Returns a new set containing the elements in the inclusive range between the specified bounds.
    /// </summary>
    /// <param name="lowerValue">The inclusive lower bound of the range.</param>
    /// <param name="upperValue">The inclusive upper bound of the range.</param>
    /// <returns>A new <see cref="WideSortedSet{T}"/> containing the elements in range.</returns>
    public WideSortedSet<T> GetViewBetween(T lowerValue, T upperValue) {
        WideMemory<T> viewMemory = GetViewMemoryBetween(lowerValue, upperValue);

        WideSortedSet<T> view = new(Comparer);
        foreach (T item in viewMemory)
            view._items.Add(item);

        return view;
    }
    
    /// <summary>
    /// Returns a <see cref="WideMemory{T}"/> that views the elements in the inclusive range between the specified bounds.
    /// </summary>
    /// <param name="lowerValue">The inclusive lower bound of the range.</param>
    /// <param name="upperValue">The inclusive upper bound of the range.</param>
    /// <returns>A memory region over the in-range elements of the underlying storage.</returns>
    /// <exception cref="ArgumentException"><paramref name="lowerValue"/> is greater than <paramref name="upperValue"/>.</exception>
    public WideMemory<T> GetViewMemoryBetween(T lowerValue, T upperValue) {
        if (Comparer.Compare(lowerValue, upperValue) > 0)
            throw new ArgumentException("Lower value must be less than or equal to upper value.");

        long lowerIndex = FindIndex(lowerValue), upperIndex = FindIndex(upperValue);
        if (lowerIndex < 0)
            lowerIndex = ~lowerIndex;
        if (upperIndex < 0)
            upperIndex = ~upperIndex;

        return _items.AsMemory(lowerIndex, upperIndex - lowerIndex + 1);
    }

    /// <summary>
    /// Enumerates the elements of the set in descending order.
    /// </summary>
    /// <returns>An enumerable that yields the elements from largest to smallest.</returns>
    public IEnumerable<T> Reverse() {
        for (long i = Count - 1; i >= 0; i--)
            yield return _items[i];
    }

    /// <summary>
    /// Copies all elements of the set, in order, to the specified array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <exception cref="ArgumentException">The number of elements exceeds the array indexing range.</exception>
    public void CopyTo(T[] array) {
        if (Count > int.MaxValue)
            throw new ArgumentException("Count exceeds array indexing range.", nameof(array));

        CopyTo(array, 0, (int)Count);
    }

    /// <summary>
    /// Copies all elements of the set, in order, to the specified array starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentException">The number of elements exceeds the array indexing range.</exception>
    public void CopyTo(T[] array, int index) {
        if (Count > int.MaxValue)
            throw new ArgumentException("Count exceeds array indexing range.", nameof(array));

        CopyTo(array, index, (int)Count);
    }

    /// <summary>
    /// Copies the specified number of elements of the set, in order, to the given array starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <exception cref="ArgumentException">The destination does not have enough space for the elements.</exception>
    public void CopyTo(T[] array, int index, int count) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length - count);
        if (count > Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        for (int i = 0; i < count; i++)
            array[index + i] = _items[i];
    }

    /// <inheritdoc />
    public void UnionWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other)
            Add(item);
    }

    /// <inheritdoc />
    public void IntersectWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> otherSet = new(other, new ComparerEqualityAdapter<T>(Comparer));
        for (long i = Count - 1; i >= 0; i--) {
            if (!otherSet.Contains(_items[i]))
                _items.RemoveAt(i);
        }
    }

    /// <inheritdoc />
    public void ExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other)
            Remove(item);
    }

    /// <inheritdoc />
    public void SymmetricExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> unique = new(other, new ComparerEqualityAdapter<T>(Comparer));
        foreach (T item in unique) {
            if (!Remove(item))
                Add(item);
        }
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
        if (Count <= set.Count)
            return false;
        foreach (T item in set) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
        if (Count >= set.Count)
            return false;
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other) {
            if (Contains(item))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
        if (set.Count != Count)
            return false;
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private long FindIndex(T item) {
        long lo = 0;
        long hi = Count - 1;
        while (lo <= hi) {
            long mid = lo + ((hi - lo) >> 1);
            int cmp = Comparer.Compare(_items[mid], item);
            if (cmp == 0)
                return mid;
            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return ~lo;
    }

    private sealed class ComparerEqualityAdapter<TItem> : IEqualityComparer<TItem> {
        private readonly IComparer<TItem> _cmp;

        /// <summary>
        /// Initializes a new instance of the adapter that treats two elements as equal when the
        /// underlying comparer reports them as equivalent.
        /// </summary>
        /// <param name="cmp">The ordering comparer to adapt.</param>
        public ComparerEqualityAdapter(IComparer<TItem> cmp) => _cmp = cmp;

        /// <summary>
        /// Determines whether two elements are equal according to the underlying comparer.
        /// </summary>
        /// <param name="x">The first element to compare.</param>
        /// <param name="y">The second element to compare.</param>
        /// <returns><see langword="true"/> if the comparer reports the elements as equivalent; otherwise <see langword="false"/>.</returns>
        public bool Equals(TItem x, TItem y) => _cmp.Compare(x, y) == 0;

        /// <summary>
        /// Returns a constant hash code so that equality is decided solely by the underlying comparer.
        /// </summary>
        /// <param name="obj">The element to hash.</param>
        /// <returns>Always zero.</returns>
        public int GetHashCode(TItem obj) => 0;
    }
}

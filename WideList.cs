using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// A generic list backed by WideArray that can grow beyond Array.MaxLength.
/// </summary>
public class WideList<T> : IWideList<T>, IWideList, IWideReadOnlyList<T>, IWideIndexable<T>, ICompactable {
    private long _count;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public long Count => _count;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    internal WideArray<T> Items { get; }

    public long IndexOf(object value) => value is T item ? IndexOf(item) : -1;

    public void Insert(long index, object value) => Insert(index, (T)value);

    public void Remove(object value) {
        if (value is T item)
            Remove(item);
    }

    public long Capacity {
        get => Items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            if (value != Items.Length)
                Items.Resize(value);
        }
    }

    public WideList() => Items = new();

    public WideList(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        Items = new WideArray<T>(capacity);
    }

    public T Get(long index) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        return Items[index];
    }

    public void Set(long index, T value) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        Items[index] = value;
    }

    public T this[long index] {
        get => Get(index);
        set => Set(index, value);
    }

    public void Add(T item) {
        if (_count >= Items.Length)
            EnsureCapacity(_count + 1);

        Items[_count] = item;
        _count++;
    }

    public void AddRange(IEnumerable<T> collection) {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is IWideReadOnlyCollection<T> wide)
            EnsureCapacity(_count + wide.Count);
        else if (collection is ICollection<T> coll)
            EnsureCapacity(_count + coll.Count);

        foreach (T item in collection)
            Add(item);
    }

    public void InsertRange(long index, IEnumerable<T> collection) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);
        ArgumentNullException.ThrowIfNull(collection);

        long offset = 0;
        foreach (T item in collection)
            Insert(index + offset++, item);
    }

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

    public void Reverse() {
        for (long lo = 0, hi = _count - 1; lo < hi; lo++, hi--)
            (Items[lo], Items[hi]) = (Items[hi], Items[lo]);
    }

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

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length - _count);

        WideArray<T>.BulkCopy(Items, 0, array, arrayIndex, _count);
    }

    public bool Remove(T item) {
        long index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    public long IndexOf(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = 0; i < _count; i++)
            if (comparer.Equals(Items[i], item))
                return i;

        return -1;
    }

    public bool Contains(T item) => IndexOf(item) >= 0;

    object IWideList.this[long index] {
        get => this[index];
        set => this[index] = (T)value;
    }

    public long Add(object value) {
        Add((T)value);
        return _count - 1;
    }

    public bool Contains(object value) => Contains((T)value);

    public void Clear() {
        if (_count > 0) {
            // Clear references for reference types
            if (ContainsReferences)
                for (long i = 0; i < _count; i++)
                    Items[i] = default!;

            _count = 0;
        }
    }

    public void Compact() => Capacity = _count;

    public WideMemory<T> AsMemory() => new(this);
    public WideMemory<T> AsMemory(long start, long length) => new WideMemory<T>(this).Slice(start, length);
    public WideReadOnlyMemory<T> AsReadOnlyMemory() => new(this);
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

    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < _count; i++)
            yield return Items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
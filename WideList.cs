using System.Collections;
using System.Runtime.CompilerServices;

namespace WideCollections;

/// <summary>
/// A generic list backed by WideArray that can grow beyond Array.MaxLength.
/// </summary>
public class WideList<T> : IWideList<T>, IWideList, IWideReadOnlyList<T>, ICompactable {
    private WideArray<T> _items;
    private long _count = 0;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public long Count => _count;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public long IndexOf(object value) {
        throw new NotImplementedException();
    }
    public void Insert(long index, object value) {
        throw new NotImplementedException();
    }
    public void Remove(object value) {
        throw new NotImplementedException();
    }

    public long Capacity {
        get => _items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            if (value != _items.Length)
                _items.Resize(value);
        }
    }

    public WideList() {
        _items = new WideArray<T>();
        SyncRoot = new object();
    }

    public WideList(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideArray<T>(capacity);
        SyncRoot = new object();
    }

    public T Get(long index) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        return _items[index];
    }

    public void Set(long index, T value) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        _items[index] = value;
    }

    public T this[long index] {
        get => Get(index);
        set => Set(index, value);
    }

    public void Add(T item) {
        if (_count >= _items.Length)
            EnsureCapacity(_count + 1);

        _items[_count] = item;
        _count++;
    }

    public void Insert(long index, T item) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);

        if (_count >= _items.Length)
            EnsureCapacity(_count + 1);

        // Shift elements to the right
        for (long i = _count; i > index; i--)
            _items[i] = _items[i - 1];

        _items[index] = item;
        _count++;
    }

    public void RemoveAt(long index) {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideList of count {_count}.");

        // Shift elements to the left
        for (long i = index; i < _count - 1; i++)
            _items[i] = _items[i + 1];

        _count--;
        if (ContainsReferences)
            _items[_count] = default!;
    }

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length - _count);

        for (long i = 0; i < _count; i++)
            array[arrayIndex + i] = _items[i];
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
        for (long i = 0; i < _count; i++) {
            if (comparer.Equals(_items[i], item))
                return i;
        }

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
                    _items[i] = default!;

            _count = 0;
        }
    }

    public void Compact() => Capacity = _count;

    private void EnsureCapacity(long min) {
        long newCapacity = _items.Length == 0 ? 4 : _items.Length;

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
            int cmp = comparer.Compare(_items[mid], item);

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
            int cmp = comparer.Compare(_items[mid], item);

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
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
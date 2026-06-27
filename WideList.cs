using System.Collections;

namespace WideCollections;

/// <summary>
/// A generic list backed by WideArray that can grow beyond Array.MaxLength.
/// </summary>
public class WideList<T> : IWideList<T>, IWideList, IWideReadOnlyList<T>, ICompactable {
    private WideArray<T> _items;
    private long _count = 0;

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
            if (value < _count)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be less than Count.");

            if (value != _items.Length)
                _items.Resize(value);
        }
    }

    public WideList() {
        _items = new WideArray<T>();
        SyncRoot = new object();
    }

    public WideList(long capacity) {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

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
        if (index < 0 || index > _count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

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
    }

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");

        if (arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");

        if (array.Length - arrayIndex < _count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

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
            if (!typeof(T).IsValueType)
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
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));

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
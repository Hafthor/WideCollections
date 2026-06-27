using System.Collections;

namespace WideCollections;

public class WideStack<T> : IEnumerable<T>, IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private WideArray<T> _items;
    private long _count;

    public WideStack() {
        _items = new WideArray<T>();
        SyncRoot = new object();
    }

    public WideStack(long capacity) {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

        _items = new WideArray<T>(capacity);
        SyncRoot = new object();
    }

    public WideStack(IEnumerable<T> collection) : this() {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (T item in collection)
            Push(item);
    }

    public long Count => _count;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;

    public long Capacity {
        get => _items.Length;
        set {
            if (value < _count)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be less than Count.");

            if (value != _items.Length)
                _items.Resize(value);
        }
    }

    public void Push(T item) {
        if (_count >= _items.Length)
            EnsureCapacity(_count + 1);

        _items[_count] = item;
        _count++;
    }

    public void Clear() {
        if (_count == 0)
            return;

        if (!typeof(T).IsValueType) {
            for (long i = 0; i < _count; i++)
                _items[i] = default!;
        }

        _count = 0;
    }

    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = _count - 1; i >= 0; i--) {
            if (comparer.Equals(_items[i], item))
                return true;
        }

        return false;
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
            array[arrayIndex + i] = _items[_count - 1 - i];
    }

    public T Peek() {
        if (_count == 0)
            throw new InvalidOperationException("Stack is empty.");

        return _items[_count - 1];
    }

    public bool TryPeek(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        result = _items[_count - 1];
        return true;
    }

    public T Pop() {
        if (_count == 0)
            throw new InvalidOperationException("Stack is empty.");

        long topIndex = _count - 1;
        T item = _items[topIndex];

        if (!typeof(T).IsValueType)
            _items[topIndex] = default!;

        _count = topIndex;
        return item;
    }

    public bool TryPop(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        long topIndex = _count - 1;
        result = _items[topIndex];

        if (!typeof(T).IsValueType)
            _items[topIndex] = default!;

        _count = topIndex;
        return true;
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

    public IEnumerator<T> GetEnumerator() {
        for (long i = _count - 1; i >= 0; i--)
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
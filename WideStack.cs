using System.Collections;
using System.Runtime.CompilerServices;

namespace WideCollections;

public class WideStack<T> : IEnumerable<T>, IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private readonly WideArray<T> _items;
    private long _count;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public WideStack() {
        _items = new WideArray<T>();
        SyncRoot = new object();
    }

    public WideStack(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

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
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

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

        if (ContainsReferences) {
            for (long i = 0; i < _count; i++)
                _items[i] = default!;
        }

        _count = 0;
    }

    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = _count - 1; i >= 0; i--)
            if (comparer.Equals(_items[i], item))
                return true;

        return false;
    }

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, array.Length - arrayIndex);

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

        if (ContainsReferences)
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

        if (ContainsReferences)
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
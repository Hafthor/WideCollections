using System.Collections;
using System.Runtime.CompilerServices;

namespace WideCollections;

public class WideQueue<T> : IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private WideArray<T> _items;
    private long _head, _tail, _count;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public WideQueue(long capacity = 0) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideArray<T>(capacity);
    }

    public WideQueue(IEnumerable<T> collection) : this() {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (T item in collection)
            Enqueue(item);
    }

    public long Count => _count;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;

    public long Capacity {
        get => _items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            if (value != _items.Length)
                SetCapacity(value);
        }
    }

    public void Enqueue(T item) {
        if (_count == _items.Length)
            EnsureCapacity(_count + 1);

        _items[_tail++] = item;
        if (_tail == _items.Length)
            _tail = 0;

        _count++;
    }

    public T Dequeue() {
        if (_count == 0)
            throw new InvalidOperationException("Queue is empty.");

        T item = _items[_head];

        if (ContainsReferences)
            _items[_head] = default!;

        if (++_head == _items.Length)
            _head = 0;

        if (--_count == 0)
            _head = _tail = 0;

        return item;
    }

    public bool TryDequeue(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        result = Dequeue();
        return true;
    }

    public T Peek() {
        if (_count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _items[_head];
    }

    public bool TryPeek(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        result = _items[_head];
        return true;
    }

    public void Clear() {
        if (_count == 0)
            return;

        if (ContainsReferences)
            _items.Fill(default!);

        _count = _head = _tail = 0;
    }

    public void Compact() => SetCapacity(_count);

    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = 0; i < _count; i++)
            if (comparer.Equals(_items[GetIndex(i)], item))
                return true;

        return false;
    }

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, array.Length - arrayIndex);

        for (long i = 0; i < _count; i++)
            array[arrayIndex + i] = _items[GetIndex(i)];
    }

    private long GetIndex(long offset) {
        long index = _head + offset;
        if (index >= _items.Length)
            index -= _items.Length;

        return index;
    }

    private void SetCapacity(long capacity) {
        WideArray<T> newItems = new(capacity);

        for (long i = 0; i < _count; i++)
            newItems[i] = _items[GetIndex(i)];

        _items = newItems;
        _head = 0;
        _tail = _count == capacity ? 0 : _count;
    }

    private void EnsureCapacity(long min) {
        long newCapacity = _items.Length == 0 ? 4 : _items.Length;

        while (newCapacity < min) {
            long growth = newCapacity >> 1;
            if (growth == 0)
                growth = 1;

            newCapacity += growth;
        }

        SetCapacity(newCapacity);
    }

    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < _count; i++)
            yield return _items[GetIndex(i)];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
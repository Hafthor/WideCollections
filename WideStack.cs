using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

public class WideStack<T> : IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private readonly WideArray<T> _items;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public WideStack() => _items = new WideArray<T>();

    public WideStack(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideArray<T>(capacity);
    }

    public WideStack(IEnumerable<T> collection) : this() {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (T item in collection)
            Push(item);
    }

    public long Count { get; private set; }

    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;

    public long Capacity {
        get => _items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);

            if (value != _items.Length)
                _items.Resize(value);
        }
    }

    public void Push(T item) {
        if (Count >= _items.Length)
            EnsureCapacity(Count + 1);

        _items[Count++] = item;
    }

    public void Clear() {
        if (Count == 0)
            return;

        if (ContainsReferences)
            _items.Fill(default!);

        Count = 0;
    }

    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = Count - 1; i >= 0; i--)
            if (comparer.Equals(_items[i], item))
                return true;

        return false;
    }

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Count, array.Length - arrayIndex);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[Count - 1 - i];
    }

    public T Peek() {
        if (Count == 0)
            throw new InvalidOperationException("Stack is empty.");

        return _items[Count - 1];
    }

    public bool TryPeek(out T result) {
        if (Count == 0) {
            result = default!;
            return false;
        }

        result = _items[Count - 1];
        return true;
    }

    public T Pop() {
        if (Count == 0)
            throw new InvalidOperationException("Stack is empty.");

        long topIndex = Count - 1;
        T item = _items[topIndex];

        if (ContainsReferences)
            _items[topIndex] = default!;

        Count = topIndex;
        return item;
    }

    public bool TryPop(out T result) {
        if (Count == 0) {
            result = default!;
            return false;
        }

        long topIndex = Count - 1;
        result = _items[topIndex];

        if (ContainsReferences)
            _items[topIndex] = default!;

        Count = topIndex;
        return true;
    }

    public void Compact() => Capacity = Count;

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
        for (long i = Count - 1; i >= 0; i--)
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
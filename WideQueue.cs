using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a first-in, first-out (FIFO) collection of instances of the same specified type.
/// Backed by a <see cref="WideArray{T}"/> so it can hold more than <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideQueue<T> : IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private WideArray<T> _items;
    private long _head, _tail, _count;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideQueue{T}"/> class,
    /// optionally with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements the queue can hold before resizing.</param>
    public WideQueue(long capacity = 0) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideArray<T>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideQueue{T}"/> class that contains
    /// the elements copied from the specified collection, enqueued in enumeration order.
    /// </summary>
    /// <param name="collection">The collection whose elements are enqueued.</param>
    public WideQueue(IEnumerable<T> collection) : this() {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (T item in collection)
            Enqueue(item);
    }

    /// <inheritdoc />
    public long Count => _count;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;

    /// <summary>
    /// Gets or sets the number of elements the queue can hold before it must resize its
    /// internal storage. When set, the value must not be less than <see cref="Count"/>.
    /// </summary>
    public long Capacity {
        get => _items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _count);

            if (value != _items.Length)
                SetCapacity(value);
        }
    }

    /// <summary>
    /// Adds an element to the end of the queue, growing capacity if required.
    /// </summary>
    /// <param name="item">The element to add to the queue.</param>
    public void Enqueue(T item) {
        if (_count == _items.Length)
            EnsureCapacity(_count + 1);

        _items[_tail++] = item;
        if (_tail == _items.Length)
            _tail = 0;

        _count++;
    }

    /// <summary>
    /// Removes and returns the element at the beginning of the queue.
    /// </summary>
    /// <returns>The element removed from the beginning of the queue.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
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

    /// <summary>
    /// Removes and returns the element at the beginning of the queue, if the queue is not empty.
    /// </summary>
    /// <param name="result">When this method returns, contains the removed element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
    public bool TryDequeue(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        result = Dequeue();
        return true;
    }

    /// <summary>
    /// Returns the element at the beginning of the queue without removing it.
    /// </summary>
    /// <returns>The element at the beginning of the queue.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public T Peek() {
        if (_count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _items[_head];
    }

    /// <summary>
    /// Returns the element at the beginning of the queue without removing it, if the queue is not empty.
    /// </summary>
    /// <param name="result">When this method returns, contains the front element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was returned; otherwise <see langword="false"/>.</returns>
    public bool TryPeek(out T result) {
        if (_count == 0) {
            result = default!;
            return false;
        }

        result = _items[_head];
        return true;
    }

    /// <summary>
    /// Removes all elements from the queue, clearing references so they can be garbage collected.
    /// </summary>
    public void Clear() {
        if (_count == 0)
            return;

        if (ContainsReferences)
            _items.Fill(default!);

        _count = _head = _tail = 0;
    }

    /// <inheritdoc />
    public void Compact() => SetCapacity(_count);

    /// <inheritdoc />
    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = 0; i < _count; i++)
            if (comparer.Equals(_items[GetIndex(i)], item))
                return true;

        return false;
    }

    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_count, array.Length - arrayIndex);

        long firstLen = Math.Min(_count, _items.Length - _head);
        WideArray<T>.BulkCopy(_items, _head, array, arrayIndex, firstLen);
        if (firstLen < _count)
            WideArray<T>.BulkCopy(_items, 0, array, arrayIndex + firstLen, _count - firstLen);
    }

    private long GetIndex(long offset) {
        long index = _head + offset;
        if (index >= _items.Length)
            index -= _items.Length;

        return index;
    }

    private void SetCapacity(long capacity) {
        WideArray<T> newItems = new(capacity);

        long firstLen = Math.Min(_count, _items.Length - _head);
        WideArray<T>.BulkCopy(_items, _head, newItems, 0, firstLen);
        if (firstLen < _count)
            WideArray<T>.BulkCopy(_items, 0, newItems, firstLen, _count - firstLen);

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

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < _count; i++)
            yield return _items[GetIndex(i)];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

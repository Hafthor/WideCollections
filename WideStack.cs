using System.Collections;
using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a variable-size last-in, first-out (LIFO) collection of instances of the same
/// specified type. Backed by a <see cref="WideArray{T}"/> so it can hold more than
/// <see cref="int.MaxValue"/> elements.
/// </summary>
public class WideStack<T> : IWideCollection, IWideReadOnlyCollection<T>, ICompactable {
    private readonly WideArray<T> _items;
    private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideStack{T}"/> class.
    /// </summary>
    public WideStack() => _items = new WideArray<T>();

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideStack{T}"/> class
    /// with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements the stack can hold before resizing.</param>
    public WideStack(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _items = new WideArray<T>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideStack{T}"/> class that contains
    /// the elements copied from the specified collection, pushed in enumeration order.
    /// </summary>
    /// <param name="collection">The collection whose elements are pushed onto the stack.</param>
    public WideStack(IEnumerable<T> collection) : this() {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (T item in collection)
            Push(item);
    }

    /// <inheritdoc />
    public long Count { get; private set; }

    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;

    /// <summary>
    /// Gets or sets the number of elements the stack can hold before it must resize its
    /// internal storage. When set, the value must not be less than <see cref="Count"/>.
    /// </summary>
    public long Capacity {
        get => _items.Length;
        set {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);

            if (value != _items.Length)
                _items.Resize(value);
        }
    }

    /// <summary>
    /// Inserts an element at the top of the stack, growing capacity if required.
    /// </summary>
    /// <param name="item">The element to push onto the stack.</param>
    public void Push(T item) {
        if (Count >= _items.Length)
            EnsureCapacity(Count + 1);

        _items[Count++] = item;
    }

    /// <summary>
    /// Removes all elements from the stack, clearing references so they can be garbage collected.
    /// </summary>
    public void Clear() {
        if (Count == 0)
            return;

        if (ContainsReferences)
            _items.Fill(default!);

        Count = 0;
    }

    /// <inheritdoc />
    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (long i = Count - 1; i >= 0; i--)
            if (comparer.Equals(_items[i], item))
                return true;

        return false;
    }

    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Count, array.Length - arrayIndex);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[Count - 1 - i];
    }

    /// <summary>
    /// Returns the element at the top of the stack without removing it.
    /// </summary>
    /// <returns>The element at the top of the stack.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    public T Peek() {
        if (Count == 0)
            throw new InvalidOperationException("Stack is empty.");

        return _items[Count - 1];
    }

    /// <summary>
    /// Returns the element at the top of the stack without removing it, if the stack is not empty.
    /// </summary>
    /// <param name="result">When this method returns, contains the top element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was returned; otherwise <see langword="false"/>.</returns>
    public bool TryPeek(out T result) {
        if (Count == 0) {
            result = default!;
            return false;
        }

        result = _items[Count - 1];
        return true;
    }

    /// <summary>
    /// Removes and returns the element at the top of the stack.
    /// </summary>
    /// <returns>The element removed from the top of the stack.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
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

    /// <summary>
    /// Removes and returns the element at the top of the stack, if the stack is not empty.
    /// </summary>
    /// <param name="result">When this method returns, contains the removed element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
        for (long i = Count - 1; i >= 0; i--)
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

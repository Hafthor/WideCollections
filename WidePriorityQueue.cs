using System.Runtime.CompilerServices;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a collection of items that have a value and a priority. On dequeue, the item with the
/// lowest priority value is removed. Backed by a <see cref="WideArray{T}"/> so it can hold more than
/// <see cref="int.MaxValue"/> elements.
/// </summary>
public class WidePriorityQueue<TElement, TPriority> : ICompactable {
    private readonly WideArray<(TElement Element, TPriority Priority)> _nodes;

    private static readonly bool ContainsReferences =
        RuntimeHelpers.IsReferenceOrContainsReferences<(TElement Element, TPriority Priority)>();
    
    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WidePriorityQueue{TElement, TPriority}"/> class
    /// that uses the specified priority comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to determine the relative priority of elements.</param>
    public WidePriorityQueue(IComparer<TPriority> comparer) : this(0, comparer) { }
    
    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WidePriorityQueue{TElement, TPriority}"/> class
    /// with the specified initial capacity and priority comparer.
    /// </summary>
    /// <param name="initialCapacity">The initial number of elements the queue can hold before resizing.</param>
    /// <param name="comparer">The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WidePriorityQueue(long initialCapacity = 0, IComparer<TPriority> comparer = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _nodes = new WideArray<(TElement Element, TPriority Priority)>(initialCapacity);
        Comparer = comparer ?? Comparer<TPriority>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidePriorityQueue{TElement, TPriority}"/> class that
    /// contains the specified element/priority pairs and uses the specified priority comparer.
    /// </summary>
    /// <param name="items">The element/priority pairs to enqueue.</param>
    /// <param name="comparer">The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
    public WidePriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority> comparer = null) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(items);
        foreach ((TElement Element, TPriority Priority) item in items)
            Enqueue(item.Element, item.Priority);
    }

    /// <summary>
    /// Gets the number of elements currently in the queue.
    /// </summary>
    public long Count { get; private set; }

    /// <summary>
    /// Gets the comparer used to determine the relative priority of elements.
    /// </summary>
    public IComparer<TPriority> Comparer { get; }

    /// <summary>
    /// Gets the number of elements the queue can hold before its internal storage must resize.
    /// </summary>
    public long Capacity => _nodes.Length;

    /// <summary>
    /// Gets an unordered view of items currently stored in the queue.
    /// </summary>
    public IEnumerable<(TElement Element, TPriority Priority)> UnorderedItems {
        get {
            for (long i = 0; i < Count; i++)
                yield return _nodes[i];
        }
    }

    /// <summary>
    /// Adds an element with the specified priority to the queue.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <param name="priority">The priority associated with the element.</param>
    public void Enqueue(TElement element, TPriority priority) {
        if (Count == _nodes.Length)
            Grow(Count + 1);

        long index = Count++;
        MoveUp((element, priority), index);
    }

    /// <summary>
    /// Removes and returns the element with the lowest priority (the root of the heap).
    /// </summary>
    /// <returns>The element with the lowest priority.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public TElement Dequeue() {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        (TElement Element, TPriority Priority) root = _nodes[0];
        RemoveRoot();
        return root.Element;
    }

    /// <summary>
    /// Removes and returns the element with the lowest priority, if the queue is not empty.
    /// </summary>
    /// <param name="element">When this method returns, contains the removed element if present; otherwise the default value.</param>
    /// <param name="priority">When this method returns, contains the priority of the removed element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
    public bool TryDequeue(out TElement element, out TPriority priority) {
        if (Count == 0) {
            element = default!;
            priority = default!;
            return false;
        }

        (TElement Element, TPriority Priority) root = _nodes[0];
        element = root.Element;
        priority = root.Priority;
        RemoveRoot();
        return true;
    }

    /// <summary>
    /// Returns the element with the lowest priority without removing it.
    /// </summary>
    /// <returns>The element with the lowest priority.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public TElement Peek() {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _nodes[0].Element;
    }

    /// <summary>
    /// Returns the element with the lowest priority without removing it, if the queue is not empty.
    /// </summary>
    /// <param name="element">When this method returns, contains the lowest-priority element if present; otherwise the default value.</param>
    /// <param name="priority">When this method returns, contains the priority of that element if present; otherwise the default value.</param>
    /// <returns><see langword="true"/> if an element was returned; otherwise <see langword="false"/>.</returns>
    public bool TryPeek(out TElement element, out TPriority priority) {
        if (Count == 0) {
            element = default!;
            priority = default!;
            return false;
        }

        (TElement Element, TPriority Priority) root = _nodes[0];
        element = root.Element;
        priority = root.Priority;
        return true;
    }

    /// <summary>
    /// Adds an element with the specified priority and immediately removes and returns the lowest-priority
    /// element. More efficient than an <see cref="Enqueue"/> followed by a <see cref="Dequeue"/>.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <param name="priority">The priority associated with <paramref name="element"/>.</param>
    /// <returns>The element that has the lowest priority after <paramref name="element"/> is considered.</returns>
    public TElement EnqueueDequeue(TElement element, TPriority priority) {
        if (Count != 0 && Comparer.Compare(priority, _nodes[0].Priority) > 0) {
            (TElement Element, TPriority Priority) root = _nodes[0];
            MoveDown((element, priority), 0);
            return root.Element;
        }

        return element;
    }

    /// <summary>
    /// Removes and returns the lowest-priority element, then adds a new element with the specified priority.
    /// </summary>
    /// <param name="element">The element to add after removing the current root.</param>
    /// <param name="priority">The priority associated with <paramref name="element"/>.</param>
    /// <returns>The element that was removed from the root of the queue.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public TElement DequeueEnqueue(TElement element, TPriority priority) {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        TElement removed = _nodes[0].Element;
        MoveDown((element, priority), 0);
        return removed;
    }

    /// <summary>
    /// Adds each element in the specified sequence to the queue with the same shared priority.
    /// </summary>
    /// <param name="elements">The elements to add.</param>
    /// <param name="priority">The priority applied to every element in <paramref name="elements"/>.</param>
    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority) {
        ArgumentNullException.ThrowIfNull(elements);

        foreach (TElement element in elements)
            Enqueue(element, priority);
    }

    /// <summary>
    /// Adds each element/priority pair in the specified sequence to the queue.
    /// </summary>
    /// <param name="items">The element/priority pairs to add.</param>
    public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items) {
        ArgumentNullException.ThrowIfNull(items);

        foreach ((TElement Element, TPriority Priority) item in items)
            Enqueue(item.Element, item.Priority);
    }

    /// <summary>
    /// Ensures that the queue can hold at least the specified number of elements without resizing.
    /// </summary>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <returns>The capacity of the queue after the call.</returns>
    public long EnsureCapacity(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (_nodes.Length < capacity)
            Grow(capacity);

        return _nodes.Length;
    }

    /// <summary>
    /// Reduces the capacity of the queue to match the current number of elements.
    /// </summary>
    public void TrimExcess() {
        if (_nodes.Length != Count)
            _nodes.Resize(Count);
    }

    /// <summary>
    /// Determines whether the queue contains the specified element.
    /// </summary>
    /// <param name="element">The element to locate.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise <see langword="false"/>.</returns>
    public bool Contains(TElement element) => FindIndex(element) >= 0;

    /// <summary>
    /// Removes the first occurrence of the specified element from the queue and restores the heap order.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(TElement element) {
        long index = FindIndex(element);
        if (index < 0)
            return false;

        long lastIndex = Count - 1;
        (TElement Element, TPriority Priority) last = _nodes[lastIndex];
        Count = lastIndex;

        if (index < Count) {
            MoveDown(last, index);
            if (Comparer.Compare(_nodes[index].Priority, last.Priority) == 0)
                MoveUp(last, index);
        }

        if (ContainsReferences)
            _nodes[lastIndex] = default!;

        return true;
    }

    private long FindIndex(TElement element) {
        EqualityComparer<TElement> cmp = EqualityComparer<TElement>.Default;
        for (long i = 0; i < Count; i++)
            if (cmp.Equals(_nodes[i].Element, element))
                return i;
        return -1;
    }

    /// <summary>
    /// Removes all elements from the queue, clearing references so they can be garbage collected.
    /// </summary>
    public void Clear() {
        if (Count == 0)
            return;

        if (ContainsReferences)
            _nodes.Fill(default!);

        Count = 0;
    }

    /// <inheritdoc />
    public void Compact() => TrimExcess();

    private void RemoveRoot() {
        long lastIndex = Count - 1;
        (TElement Element, TPriority Priority) last = _nodes[lastIndex];
        Count = lastIndex;
        if (Count > 0)
            MoveDown(last, 0);

        if (ContainsReferences)
            _nodes[lastIndex] = default!;
    }

    private void MoveUp((TElement Element, TPriority Priority) node, long index) {
        while (index > 0) {
            long parentIndex = (index - 1) / 2;
            (TElement Element, TPriority Priority) parent = _nodes[parentIndex];

            if (Comparer.Compare(node.Priority, parent.Priority) >= 0)
                break;

            _nodes[index] = parent;
            index = parentIndex;
        }

        _nodes[index] = node;
    }

    private void MoveDown((TElement Element, TPriority Priority) node, long index) {
        long half = Count / 2;
        while (index < half) {
            long leftChild = (index * 2) + 1;
            long rightChild = leftChild + 1;
            long bestChild = leftChild;
            (TElement Element, TPriority Priority) best = _nodes[leftChild];

            if (rightChild < Count) {
                (TElement Element, TPriority Priority) right = _nodes[rightChild];
                if (Comparer.Compare(right.Priority, best.Priority) < 0) {
                    bestChild = rightChild;
                    best = right;
                }
            }

            if (Comparer.Compare(node.Priority, best.Priority) <= 0)
                break;

            _nodes[index] = best;
            index = bestChild;
        }

        _nodes[index] = node;
    }

    private void Grow(long minCapacity) {
        long newCapacity = _nodes.Length == 0 ? 4 : _nodes.Length * 2;
        if (newCapacity < minCapacity)
            newCapacity = minCapacity;

        _nodes.Resize(newCapacity);
    }
}

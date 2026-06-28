using System.Runtime.CompilerServices;

namespace WideCollections;

public class WidePriorityQueue<TElement, TPriority> : ICompactable {
    private readonly WideArray<(TElement Element, TPriority Priority)> _nodes;

    private static readonly bool ContainsReferences =
        RuntimeHelpers.IsReferenceOrContainsReferences<(TElement Element, TPriority Priority)>();
    
    public WidePriorityQueue(IComparer<TPriority> comparer) : this(0, comparer) { }
    
    public WidePriorityQueue(long initialCapacity = 0, IComparer<TPriority> comparer = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _nodes = new WideArray<(TElement Element, TPriority Priority)>(initialCapacity);
        Comparer = comparer ?? Comparer<TPriority>.Default;
    }

    public WidePriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority> comparer = null) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(items);
        foreach ((TElement Element, TPriority Priority) item in items)
            Enqueue(item.Element, item.Priority);
    }

    public long Count { get; private set; }

    public IComparer<TPriority> Comparer { get; }

    public long Capacity => _nodes.Length;

    public IEnumerable<(TElement Element, TPriority Priority)> UnorderedItems {
        get {
            for (long i = 0; i < Count; i++)
                yield return _nodes[i];
        }
    }

    public void Enqueue(TElement element, TPriority priority) {
        if (Count == _nodes.Length)
            Grow(Count + 1);

        long index = Count++;
        MoveUp((element, priority), index);
    }

    public TElement Dequeue() {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        (TElement Element, TPriority Priority) root = _nodes[0];
        RemoveRoot();
        return root.Element;
    }

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

    public TElement Peek() {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _nodes[0].Element;
    }

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

    public TElement EnqueueDequeue(TElement element, TPriority priority) {
        if (Count != 0 && Comparer.Compare(priority, _nodes[0].Priority) > 0) {
            (TElement Element, TPriority Priority) root = _nodes[0];
            MoveDown((element, priority), 0);
            return root.Element;
        }

        return element;
    }

    public TElement DequeueEnqueue(TElement element, TPriority priority) {
        if (Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        TElement removed = _nodes[0].Element;
        MoveDown((element, priority), 0);
        return removed;
    }

    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority) {
        ArgumentNullException.ThrowIfNull(elements);

        foreach (TElement element in elements)
            Enqueue(element, priority);
    }

    public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items) {
        ArgumentNullException.ThrowIfNull(items);

        foreach ((TElement Element, TPriority Priority) item in items)
            Enqueue(item.Element, item.Priority);
    }

    public long EnsureCapacity(long capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (_nodes.Length < capacity)
            Grow(capacity);

        return _nodes.Length;
    }

    public void TrimExcess() {
        if (_nodes.Length != Count)
            _nodes.Resize(Count);
    }

    public void Clear() {
        if (Count == 0)
            return;

        if (ContainsReferences)
            _nodes.Fill(default!);

        Count = 0;
    }

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
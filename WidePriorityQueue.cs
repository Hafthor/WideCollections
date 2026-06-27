using System.Runtime.CompilerServices;

namespace WideCollections;

public class WidePriorityQueue<TElement, TPriority> : ICompactable {
    private WideArray<(TElement Element, TPriority Priority)> _nodes;
    private long _size;
    private readonly IComparer<TPriority> _comparer;
    private static readonly bool ContainsReferences =
        RuntimeHelpers.IsReferenceOrContainsReferences<(TElement Element, TPriority Priority)>();

    public WidePriorityQueue() : this(0, null) { }

    public WidePriorityQueue(IComparer<TPriority> comparer) : this(0, comparer) { }

    public WidePriorityQueue(long initialCapacity) : this(initialCapacity, null) { }

    public WidePriorityQueue(long initialCapacity, IComparer<TPriority> comparer) {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _nodes = new WideArray<(TElement Element, TPriority Priority)>(initialCapacity);
        _comparer = comparer ?? Comparer<TPriority>.Default;
    }

    public WidePriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items) : this(items, null) { }

    public WidePriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority> comparer) : this(0, comparer) {
        ArgumentNullException.ThrowIfNull(items);
        foreach ((TElement Element, TPriority Priority) item in items)
            Enqueue(item.Element, item.Priority);
    }

    public long Count => _size;
    public IComparer<TPriority> Comparer => _comparer;
    public long Capacity => _nodes.Length;

    public IEnumerable<(TElement Element, TPriority Priority)> UnorderedItems {
        get {
            for (long i = 0; i < _size; i++)
                yield return _nodes[i];
        }
    }

    public void Enqueue(TElement element, TPriority priority) {
        if (_size == _nodes.Length)
            Grow(_size + 1);

        long index = _size;
        _size++;
        MoveUp((element, priority), index);
    }

    public TElement Dequeue() {
        if (_size == 0)
            throw new InvalidOperationException("Queue is empty.");

        (TElement Element, TPriority Priority) root = _nodes[0];
        RemoveRoot();
        return root.Element;
    }

    public bool TryDequeue(out TElement element, out TPriority priority) {
        if (_size == 0) {
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
        if (_size == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _nodes[0].Element;
    }

    public bool TryPeek(out TElement element, out TPriority priority) {
        if (_size == 0) {
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
        if (_size != 0 && _comparer.Compare(priority, _nodes[0].Priority) > 0) {
            (TElement Element, TPriority Priority) root = _nodes[0];
            MoveDown((element, priority), 0);
            return root.Element;
        }

        return element;
    }

    public TElement DequeueEnqueue(TElement element, TPriority priority) {
        if (_size == 0)
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
        if (_nodes.Length != _size)
            _nodes.Resize(_size);
    }

    public void Clear() {
        if (_size == 0)
            return;

        if (ContainsReferences) {
            for (long i = 0; i < _size; i++)
                _nodes[i] = default!;
        }

        _size = 0;
    }

    public void Compact() => TrimExcess();

    private void RemoveRoot() {
        long lastIndex = _size - 1;
        (TElement Element, TPriority Priority) last = _nodes[lastIndex];
        _size = lastIndex;
        if (_size > 0)
            MoveDown(last, 0);

        if (ContainsReferences)
            _nodes[lastIndex] = default!;
    }

    private void MoveUp((TElement Element, TPriority Priority) node, long index) {
        while (index > 0) {
            long parentIndex = (index - 1) / 2;
            (TElement Element, TPriority Priority) parent = _nodes[parentIndex];

            if (_comparer.Compare(node.Priority, parent.Priority) >= 0)
                break;

            _nodes[index] = parent;
            index = parentIndex;
        }

        _nodes[index] = node;
    }

    private void MoveDown((TElement Element, TPriority Priority) node, long index) {
        long half = _size / 2;
        while (index < half) {
            long leftChild = (index * 2) + 1;
            long rightChild = leftChild + 1;
            long bestChild = leftChild;
            (TElement Element, TPriority Priority) best = _nodes[leftChild];

            if (rightChild < _size) {
                (TElement Element, TPriority Priority) right = _nodes[rightChild];
                if (_comparer.Compare(right.Priority, best.Priority) < 0) {
                    bestChild = rightChild;
                    best = right;
                }
            }

            if (_comparer.Compare(node.Priority, best.Priority) <= 0)
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
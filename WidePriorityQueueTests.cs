namespace WideCollections;

[TestClass]
public sealed class WidePriorityQueueTests {
    [TestMethod]
    public void EnqueueDequeue_ReturnsElementsByPriority() {
        WidePriorityQueue<string, int> queue = new();
        queue.Enqueue("low", 30);
        queue.Enqueue("high", 10);
        queue.Enqueue("mid", 20);

        Assert.AreEqual(3L, queue.Count);
        Assert.AreEqual("high", queue.Dequeue());
        Assert.AreEqual("mid", queue.Dequeue());
        Assert.AreEqual("low", queue.Dequeue());
    }

    [TestMethod]
    public void PeekAndTryMethods_WorkForEmptyAndNonEmptyQueue() {
        WidePriorityQueue<int, int> queue = new();
        Assert.IsFalse(queue.TryPeek(out int emptyPeekElement, out int emptyPeekPriority));
        Assert.AreEqual(0, emptyPeekElement);
        Assert.AreEqual(0, emptyPeekPriority);
        Assert.IsFalse(queue.TryDequeue(out int emptyDequeueElement, out int emptyDequeuePriority));
        Assert.AreEqual(0, emptyDequeueElement);
        Assert.AreEqual(0, emptyDequeuePriority);

        queue.Enqueue(42, 5);
        Assert.IsTrue(queue.TryPeek(out int element, out int priority));
        Assert.AreEqual(42, element);
        Assert.AreEqual(5, priority);
        Assert.AreEqual(42, queue.Peek());
    }

    [TestMethod]
    public void EnqueueDequeue_MethodBehavesLikeDotNet() {
        WidePriorityQueue<string, int> queue = new();
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 20);

        string returnedHigh = queue.EnqueueDequeue("c", 30);
        Assert.AreEqual("a", returnedHigh);
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, queue.UnorderedItems.Select(x => x.Element).ToArray());

        string returnedLow = queue.EnqueueDequeue("d", 5);
        Assert.AreEqual("d", returnedLow);
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, queue.UnorderedItems.Select(x => x.Element).ToArray());
    }

    [TestMethod]
    public void DequeueEnqueue_ReplacesRootAndReturnsRemovedElement() {
        WidePriorityQueue<string, int> queue = new();
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 20);
        queue.Enqueue("c", 30);

        string removed = queue.DequeueEnqueue("x", 25);

        Assert.AreEqual("a", removed);
        Assert.AreEqual(3L, queue.Count);
        Assert.AreEqual("b", queue.Dequeue());
        Assert.AreEqual("x", queue.Dequeue());
        Assert.AreEqual("c", queue.Dequeue());
    }

    [TestMethod]
    public void EnqueueRangeAndCustomComparer_WorkAsExpected() {
        WidePriorityQueue<int, int> queue = new(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        queue.EnqueueRange([1, 2, 3], 5);
        queue.EnqueueRange([(10, 10), (20, 20), (30, 30)]);

        Assert.AreEqual(6L, queue.Count);
        Assert.AreEqual(30, queue.Dequeue());
        Assert.AreEqual(20, queue.Dequeue());
        Assert.AreEqual(10, queue.Dequeue());
    }

    [TestMethod]
    public void EnsureCapacityAndTrimExcess_AdjustCapacity() {
        WidePriorityQueue<int, int> queue = new();
        long ensured = queue.EnsureCapacity(50);
        Assert.IsTrue(ensured >= 50);

        queue.Enqueue(1, 1);
        queue.Enqueue(2, 2);
        queue.TrimExcess();

        Assert.AreEqual(2L, queue.Capacity);
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WidePriorityQueue<int, int> queue = new(64);
        for (int i = 0; i < 20; i++)
            queue.Enqueue(i, i);

        for (int i = 0; i < 17; i++)
            queue.Dequeue();

        Assert.IsTrue(queue.Capacity > queue.Count);
        queue.Compact();

        Assert.AreEqual(queue.Count, queue.Capacity);
        CollectionAssert.AreEqual(new[] { 17, 18, 19 }, new[] { queue.Dequeue(), queue.Dequeue(), queue.Dequeue() });
    }

    [TestMethod]
    public void Dequeue_ClearsRemovedReferenceForGarbageCollection() {
        WidePriorityQueue<object, int> queue = new(4);
        WeakReference weak = EnqueueAndDequeueReference(queue);

        ForceGc();

        Assert.IsFalse(weak.IsAlive);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference EnqueueAndDequeueReference(WidePriorityQueue<object, int> queue) {
        object payload = new();
        WeakReference weak = new(payload);
        queue.Enqueue(payload, 1);
        queue.Dequeue();
        return weak;
    }

    private static void ForceGc() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

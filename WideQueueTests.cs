namespace WideCollections;

[TestClass]
public sealed class WideQueueTests {
    [TestMethod]
    public void EnqueueDequeuePeek_UsesFifoOrder() {
        WideQueue<int> queue = new();
        queue.Enqueue(10);
        queue.Enqueue(20);
        queue.Enqueue(30);

        Assert.AreEqual(3L, queue.Count);
        Assert.AreEqual(10, queue.Peek());
        Assert.AreEqual(10, queue.Dequeue());
        Assert.AreEqual(20, queue.Dequeue());
        Assert.AreEqual(30, queue.Dequeue());
        Assert.AreEqual(0L, queue.Count);
    }

    [TestMethod]
    public void TryPeekAndTryDequeue_ReturnFalseForEmptyQueue() {
        WideQueue<int> queue = new();

        Assert.IsFalse(queue.TryPeek(out int peeked));
        Assert.AreEqual(0, peeked);
        Assert.IsFalse(queue.TryDequeue(out int dequeued));
        Assert.AreEqual(0, dequeued);
    }

    [TestMethod]
    public void ConstructorFromCollection_EnumeratesInInsertionOrder() {
        WideQueue<int> queue = new([1, 2, 3, 4]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, queue.ToArray());
    }

    [TestMethod]
    public void CopyTo_CopiesInFifoOrderStartingAtOffset() {
        WideQueue<int> queue = new();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        WideArray<int> destination = new(6);
        queue.CopyTo(destination, 1);

        Assert.AreEqual(0, destination[0]);
        Assert.AreEqual(1, destination[1]);
        Assert.AreEqual(2, destination[2]);
        Assert.AreEqual(3, destination[3]);
        Assert.AreEqual(0, destination[4]);
        Assert.AreEqual(0, destination[5]);
    }

    [TestMethod]
    public void WrapAround_PreservesOrderAcrossHeadAndTailBoundary() {
        WideQueue<int> queue = new(4);
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        queue.Enqueue(4);
        Assert.AreEqual(1, queue.Dequeue());
        Assert.AreEqual(2, queue.Dequeue());

        queue.Enqueue(5);
        queue.Enqueue(6);

        CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, queue.ToArray());
        Assert.AreEqual(3, queue.Dequeue());
        Assert.AreEqual(4, queue.Dequeue());
        Assert.AreEqual(5, queue.Dequeue());
        Assert.AreEqual(6, queue.Dequeue());
    }

    [TestMethod]
    public void CapacitySetter_ThrowsWhenValueIsLessThanCount() {
        WideQueue<int> queue = new();
        queue.Enqueue(1);
        queue.Enqueue(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.Capacity = 1);
    }

    [TestMethod]
    public void Clear_RemovesAllItems() {
        WideQueue<string> queue = new();
        queue.Enqueue("a");
        queue.Enqueue("b");

        queue.Clear();

        Assert.AreEqual(0L, queue.Count);
        Assert.IsFalse(queue.Contains("a"));
        Assert.IsFalse(queue.Contains("b"));
        Assert.Throws<InvalidOperationException>(() => queue.Peek());
    }
}

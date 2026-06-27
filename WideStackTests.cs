namespace WideCollections;

[TestClass]
public sealed class WideStackTests {
    [TestMethod]
    public void PushPopPeek_UsesLifoOrder() {
        WideStack<int> stack = new();
        stack.Push(10);
        stack.Push(20);
        stack.Push(30);

        Assert.AreEqual(3L, stack.Count);
        Assert.AreEqual(30, stack.Peek());
        Assert.AreEqual(30, stack.Pop());
        Assert.AreEqual(20, stack.Pop());
        Assert.AreEqual(10, stack.Pop());
        Assert.AreEqual(0L, stack.Count);
    }

    [TestMethod]
    public void TryPeekAndTryPop_ReturnFalseForEmptyStack() {
        WideStack<int> stack = new();

        Assert.IsFalse(stack.TryPeek(out int peeked));
        Assert.AreEqual(0, peeked);
        Assert.IsFalse(stack.TryPop(out int popped));
        Assert.AreEqual(0, popped);
    }

    [TestMethod]
    public void ConstructorFromCollection_EnumeratesTopToBottom() {
        WideStack<int> stack = new([1, 2, 3, 4]);

        CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
    }

    [TestMethod]
    public void CopyTo_CopiesInLifoOrderStartingAtOffset() {
        WideStack<int> stack = new();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        WideArray<int> destination = new(6);
        stack.CopyTo(destination, 2);

        Assert.AreEqual(0, destination[0]);
        Assert.AreEqual(0, destination[1]);
        Assert.AreEqual(3, destination[2]);
        Assert.AreEqual(2, destination[3]);
        Assert.AreEqual(1, destination[4]);
        Assert.AreEqual(0, destination[5]);
    }

    [TestMethod]
    public void CapacitySetter_ThrowsWhenValueIsLessThanCount() {
        WideStack<int> stack = new();
        stack.Push(1);
        stack.Push(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.Capacity = 1);
    }

    [TestMethod]
    public void Clear_RemovesAllItems() {
        WideStack<string> stack = new();
        stack.Push("a");
        stack.Push("b");

        stack.Clear();

        Assert.AreEqual(0L, stack.Count);
        Assert.IsFalse(stack.Contains("a"));
        Assert.IsFalse(stack.Contains("b"));
        Assert.Throws<InvalidOperationException>(() => stack.Peek());
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WideStack<int> stack = new(64);
        for (int i = 0; i < 20; i++)
            stack.Push(i);

        for (int i = 0; i < 17; i++)
            stack.Pop();

        Assert.IsTrue(stack.Capacity > stack.Count);
        stack.Compact();

        Assert.AreEqual(stack.Count, stack.Capacity);
        Assert.AreEqual(2, stack.Peek());
    }
}

namespace WideCollections;

[TestClass]
public sealed class WideListTests {
    [TestMethod]
    public void AddInsertRemoveAt_ManipulatesOrder() {
        WideList<int> list = new();
        list.Add(1);
        list.Add(3);
        list.Insert(1, 2);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());

        list.RemoveAt(1);
        CollectionAssert.AreEqual(new[] { 1, 3 }, list.ToArray());
    }

    [TestMethod]
    public void RemoveContainsIndexOf_WorkAsExpected() {
        WideList<string> list = new();
        list.Add("a");
        list.Add("b");
        list.Add("c");

        Assert.IsTrue(list.Contains("b"));
        Assert.AreEqual(1L, list.IndexOf("b"));
        Assert.IsTrue(list.Remove("b"));
        Assert.IsFalse(list.Contains("b"));
        Assert.AreEqual(-1L, list.IndexOf("b"));
        Assert.IsFalse(list.Remove("x"));
    }

    [TestMethod]
    public void BinarySearch_ReturnsIndexOrInsertionComplement() {
        WideList<int> list = new();
        list.Add(2);
        list.Add(4);
        list.Add(6);
        list.Add(8);

        Assert.AreEqual(2L, list.BinarySearch(6));
        Assert.AreEqual(~2L, list.BinarySearch(5));
    }

    [TestMethod]
    public void BinarySearch_WithCustomComparer_AndNullComparerBehavior() {
        WideList<int> descending = new();
        descending.Add(9);
        descending.Add(7);
        descending.Add(5);
        descending.Add(3);
        IComparer<int> comparer = Comparer<int>.Create((x, y) => y.CompareTo(x));

        Assert.AreEqual(2L, descending.BinarySearch(5, comparer));
        Assert.AreEqual(~2L, descending.BinarySearch(6, comparer));
        Assert.Throws<ArgumentNullException>(() => descending.BinarySearch(5, null!));
    }

    [TestMethod]
    public void Enumerator_OnlyReturnsCountItems() {
        WideList<int> list = new(10);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, list.ToArray());
    }

    [TestMethod]
    public void CopyTo_CopiesAtOffset() {
        WideList<int> list = new();
        list.Add(4);
        list.Add(5);
        list.Add(6);

        WideArray<int> destination = new(5);
        list.CopyTo(destination, 1);

        Assert.AreEqual(0, destination[0]);
        Assert.AreEqual(4, destination[1]);
        Assert.AreEqual(5, destination[2]);
        Assert.AreEqual(6, destination[3]);
    }

    [TestMethod]
    public void GetSetAndIndexer_WorkAsExpected() {
        WideList<string> list = new();
        list.Add("a");
        list.Add("b");

        Assert.AreEqual("a", list.Get(0));
        list.Set(1, "B");
        Assert.AreEqual("B", list[1]);

        list[0] = "A";
        Assert.AreEqual("A", list.Get(0));
    }

    [TestMethod]
    public void ExplicitIWideListIndexer_WorksForGetAndSet() {
        IWideList list = new WideList<int>();
        list.Add(1);
        list.Add(2);

        Assert.AreEqual(1, list[0]);
        list[1] = 20;
        Assert.AreEqual(20, list[1]);
    }

    [TestMethod]
    public void ConstructorAndRangeChecks_ThrowForInvalidArguments() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WideList<int>(-1));

        WideList<int> list = new();
        list.Add(1);
        list.Add(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(3, 0));
        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(2));
        Assert.Throws<IndexOutOfRangeException>(() => list.Get(-1));
        Assert.Throws<IndexOutOfRangeException>(() => list.Set(2, 3));
    }

    [TestMethod]
    public void CapacitySetterAndCopyTo_ValidateArguments() {
        WideList<int> list = new(2);
        list.Add(1);
        list.Add(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 1);
        list.Capacity = 10;
        Assert.AreEqual(10L, list.Capacity);

        WideArray<int> destination = new(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(destination, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(destination, 1));
    }

    [TestMethod]
    public void NonGenericAdd_ReturnsInsertedIndex() {
        IWideList list = new WideList<int>();

        long firstIndex = list.Add(10);
        long secondIndex = list.Add(20);

        Assert.AreEqual(0L, firstIndex);
        Assert.AreEqual(1L, secondIndex);
    }

    [TestMethod]
    public void ObjectBasedMembers_ThrowNotImplemented() {
        IWideList list = new WideList<int>();
        Assert.Throws<NotImplementedException>(() => list.IndexOf(1));
        Assert.Throws<NotImplementedException>(() => list.Insert(0, 1));
        Assert.Throws<NotImplementedException>(() => list.Remove(1));
    }

    [TestMethod]
    public void ObjectContains_UsesGenericContainsSemantics() {
        WideList<int> list = new();
        list.Add(10);
        Assert.IsTrue(list.Contains((object)10));
        Assert.IsFalse(list.Contains((object)99));
    }

    [TestMethod]
    public void Clear_ResetsCountAndRemovesItems() {
        WideList<string> list = new();
        list.Add("x");
        list.Add("y");
        list.Clear();

        Assert.AreEqual(0L, list.Count);
        Assert.IsFalse(list.Contains("x"));
        list.Add("z");
        CollectionAssert.AreEqual(new[] { "z" }, list.ToArray());
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WideList<int> list = new(64);
        for (int i = 0; i < 20; i++)
            list.Add(i);

        for (int i = 0; i < 17; i++)
            list.RemoveAt(list.Count - 1);

        Assert.IsGreaterThan(list.Count, list.Capacity);
        list.Compact();

        Assert.AreEqual(list.Count, list.Capacity);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, list.ToArray());
    }

    [TestMethod]
    public void RemoveAt_ClearsRemovedReferenceForGarbageCollection() {
        WideList<object> list = new(4);
        WeakReference weak = AddAndRemoveReference(list);

        ForceGc();

        Assert.IsFalse(weak.IsAlive);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference AddAndRemoveReference(WideList<object> list) {
        object payload = new();
        WeakReference weak = new(payload);
        list.Add(payload);
        list.RemoveAt(0);
        return weak;
    }

    private static void ForceGc() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

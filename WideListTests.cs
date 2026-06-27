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
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WideList<int> list = new(64);
        for (int i = 0; i < 20; i++)
            list.Add(i);

        for (int i = 0; i < 17; i++)
            list.RemoveAt(list.Count - 1);

        Assert.IsTrue(list.Capacity > list.Count);
        list.Compact();

        Assert.AreEqual(list.Count, list.Capacity);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, list.ToArray());
    }
}

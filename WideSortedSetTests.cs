namespace WideCollections;

[TestClass]
public sealed class WideSortedSetTests {
    [TestMethod]
    public void AddAndEnumeration_AreSortedAndDistinct() {
        WideSortedSet<int> set = new();
        Assert.IsTrue(set.Add(3));
        Assert.IsTrue(set.Add(1));
        Assert.IsTrue(set.Add(2));
        Assert.IsFalse(set.Add(2));

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, set.ToArray());
    }

    [TestMethod]
    public void SetOperations_MatchDotNetSemantics() {
        WideSortedSet<int> set = new([1, 2, 3]);

        set.UnionWith([3, 4, 5]);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, set.ToArray());

        set.IntersectWith([2, 4, 9]);
        CollectionAssert.AreEqual(new[] { 2, 4 }, set.ToArray());

        set.ExceptWith([4]);
        CollectionAssert.AreEqual(new[] { 2 }, set.ToArray());

        set.SymmetricExceptWith([2, 3, 4]);
        CollectionAssert.AreEqual(new[] { 3, 4 }, set.ToArray());
    }

    [TestMethod]
    public void SubsetSupersetAndSetEquals_WorkAsExpected() {
        WideSortedSet<int> set = new([1, 2, 3]);

        Assert.IsTrue(set.IsSubsetOf([1, 2, 3, 4]));
        Assert.IsTrue(set.IsProperSubsetOf([1, 2, 3, 4]));
        Assert.IsTrue(set.IsSupersetOf([1, 3]));
        Assert.IsTrue(set.IsProperSupersetOf([1, 3]));
        Assert.IsTrue(set.SetEquals([3, 2, 1]));
        Assert.IsFalse(set.Overlaps([9, 8]));
    }

    [TestMethod]
    public void CopyTo_CopiesInSortedOrderAtOffset() {
        WideSortedSet<int> set = new([4, 2, 3]);
        WideArray<int> array = new(5);

        set.CopyTo(array, 1);

        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(2, array[1]);
        Assert.AreEqual(3, array[2]);
        Assert.AreEqual(4, array[3]);
    }

    [TestMethod]
    public void Comparer_IsRespected() {
        WideSortedSet<string> set = new(StringComparer.OrdinalIgnoreCase);
        set.Add("b");
        set.Add("A");

        CollectionAssert.AreEqual(new[] { "A", "b" }, set.ToArray());
        Assert.IsTrue(set.Contains("a"));
    }

    [TestMethod]
    public void MinMaxReverseAndTryGetValue_WorkAsExpected() {
        WideSortedSet<int> set = new([10, 20, 30]);

        Assert.AreEqual(10, set.Min);
        Assert.AreEqual(30, set.Max);
        CollectionAssert.AreEqual(new[] { 30, 20, 10 }, set.Reverse().ToArray());
        Assert.IsTrue(set.TryGetValue(20, out int actual));
        Assert.AreEqual(20, actual);
    }

    [TestMethod]
    public void GetViewBetweenAndRemoveWhere_WorkAsExpected() {
        WideSortedSet<int> set = new([1, 2, 3, 4, 5]);
        WideSortedSet<int> view = set.GetViewBetween(2, 4);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, view.ToArray());
        int removed = set.RemoveWhere(x => x % 2 == 0);
        Assert.AreEqual(2, removed);
        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, set.ToArray());
    }

    [TestMethod]
    public void ArrayCopyOverloads_CopyAsExpected() {
        WideSortedSet<int> set = new([3, 1, 2]);
        int[] all = new int[3];
        int[] offset = new int[5];
        int[] partial = new int[4];

        set.CopyTo(all);
        set.CopyTo(offset, 1);
        set.CopyTo(partial, 1, 2);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, all);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, offset);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, partial);
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksBackingStorage() {
        WideSortedSet<int> set = new();
        for (int i = 0; i < 30; i++)
            set.Add(i);

        for (int i = 0; i < 25; i++)
            set.Remove(i);

        long before = set.InternalItemsCapacity;
        set.Compact();
        long after = set.InternalItemsCapacity;

        Assert.IsTrue(after < before);
        CollectionAssert.AreEqual(new[] { 25, 26, 27, 28, 29 }, set.ToArray());
    }
}

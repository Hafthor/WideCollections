namespace WideCollections;

[TestClass]
public sealed class WideHashSetTests {
    private readonly struct RefHolder {
        public RefHolder(object value) => Value = value;
        public object Value { get; }
    }

    [TestMethod]
    public void Add_DeduplicatesValues() {
        WideHashSet<int> set = new();

        Assert.IsTrue(set.Add(1));
        Assert.IsFalse(set.Add(1));
        Assert.IsTrue(set.Add(2));
        Assert.AreEqual(2L, set.Count);
    }

    [TestMethod]
    public void RemoveAndContains_WorkAsExpected() {
        WideHashSet<int> set = new();
        set.Add(10);
        set.Add(20);

        Assert.IsTrue(set.Contains(10));
        Assert.IsTrue(set.Remove(10));
        Assert.IsFalse(set.Contains(10));
        Assert.IsFalse(set.Remove(10));
        Assert.AreEqual(1L, set.Count);
    }

    [TestMethod]
    public void CopyTo_CopiesAllElements() {
        WideHashSet<int> set = new();
        set.Add(3);
        set.Add(5);
        set.Add(7);
        WideArray<int> destination = new(5);

        set.CopyTo(destination, 1);

        int[] copied = [destination[1], destination[2], destination[3]];
        CollectionAssert.AreEquivalent(new[] { 3, 5, 7 }, copied);
    }

    [TestMethod]
    public void UnionIntersectExceptAndSymmetricExcept_WorkLikeHashSet() {
        WideHashSet<int> set = new([1, 2, 3]);

        set.UnionWith([3, 4, 5]);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, set.ToArray());

        set.IntersectWith([2, 4, 9]);
        CollectionAssert.AreEquivalent(new[] { 2, 4 }, set.ToArray());

        set.ExceptWith([4]);
        CollectionAssert.AreEquivalent(new[] { 2 }, set.ToArray());

        set.SymmetricExceptWith([2, 3, 3, 4]);
        CollectionAssert.AreEquivalent(new[] { 3, 4 }, set.ToArray());
    }

    [TestMethod]
    public void SubsetSupersetAndSetEquals_UseSetSemantics() {
        WideHashSet<int> set = new([1, 2, 3]);

        Assert.IsTrue(set.IsSubsetOf([1, 2, 3, 4]));
        Assert.IsTrue(set.IsProperSubsetOf([1, 2, 3, 4]));
        Assert.IsTrue(set.IsSupersetOf([1, 3]));
        Assert.IsTrue(set.IsProperSupersetOf([1, 3]));
        Assert.IsTrue(set.SetEquals([3, 2, 1, 1]));
        Assert.IsFalse(set.SetEquals([1, 2]));
    }

    [TestMethod]
    public void Overlaps_ReturnsTrueWhenAnyElementMatches() {
        WideHashSet<int> set = new([8, 9]);

        Assert.IsTrue(set.Overlaps([9, 10]));
        Assert.IsFalse(set.Overlaps([10, 11]));
    }

    [TestMethod]
    public void Constructor_UsesComparerForEquality() {
        WideHashSet<string> set = new(StringComparer.OrdinalIgnoreCase);
        set.Add("abc");

        Assert.IsTrue(set.Contains("ABC"));
        Assert.IsFalse(set.Add("AbC"));
        Assert.AreEqual(1L, set.Count);
    }

    [TestMethod]
    public void Enumerator_ThrowsWhenModifiedDuringEnumeration() {
        WideHashSet<int> set = new([1, 2, 3]);
        using var enumerator = set.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        set.Add(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksBackingStorage() {
        WideHashSet<int> set = new();
        for (int i = 0; i < 30; i++)
            set.Add(i);

        for (int i = 0; i < 25; i++)
            set.Remove(i);

        long before = set.InternalEntriesLength;
        set.Compact();
        long after = set.InternalEntriesLength;

        Assert.IsTrue(after < before);
        CollectionAssert.AreEquivalent(new[] { 25, 26, 27, 28, 29 }, set.OrderBy(x => x).ToArray());
    }

    [TestMethod]
    public void Clear_ClearsStructContainedReferencesForGarbageCollection() {
        WideHashSet<RefHolder> set = new();
        WeakReference weak = AddStructReferenceAndClear(set);

        ForceGc();

        Assert.IsFalse(weak.IsAlive);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference AddStructReferenceAndClear(WideHashSet<RefHolder> set) {
        object payload = new();
        WeakReference weak = new(payload);
        set.Add(new RefHolder(payload));
        set.Clear();
        return weak;
    }

    private static void ForceGc() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

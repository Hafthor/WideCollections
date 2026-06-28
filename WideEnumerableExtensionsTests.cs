namespace WideCollections;

[TestClass]
public sealed class WideEnumerableExtensionsTests {
    [TestMethod]
    public void AsWide_ReturnsSameInstanceForWideSource() {
        WideList<int> list = new();
        list.Add(1);

        IWideEnumerable<int> wide = list.AsWide();

        Assert.AreSame(list, (object)wide);
    }

    [TestMethod]
    public void AsWide_AdaptsNonWideEnumerable() {
        IEnumerable<int> source = new[] { 1, 2, 3 };

        IWideEnumerable<int> wide = source.AsWide();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, wide.ToArray());
    }

    [TestMethod]
    public void WhereAndSelectWide_WorkAsExpected() {
        WideList<int> list = new();
        for (int i = 0; i < 6; i++)
            list.Add(i);

        int[] values = list.AsWide()
            .WhereWide<int>(x => x % 2 == 0)
            .SelectWide<int, int>(x => x * 10)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 20, 40 }, values);
    }

    [TestMethod]
    public void LongCount_UsesWideAndPredicateVariants() {
        WideList<int> list = new();
        for (int i = 0; i < 10; i++)
            list.Add(i);

        Assert.AreEqual(10L, list.LongCount());
        Assert.AreEqual(4L, list.LongCount(x => x > 5));
    }

    [TestMethod]
    public void ElementAtLong_ReturnsExpectedItemAndThrowsWhenOutOfRange() {
        WideList<string> list = new();
        list.Add("a");
        list.Add("b");
        list.Add("c");

        Assert.AreEqual("b", list.AsWide().ElementAtLong<string>(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsWide().ElementAtLong<string>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsWide().ElementAtLong<string>(3));
    }

    [TestMethod]
    public void SkipLongAndTakeLong_WorkAsExpected() {
        WideList<int> list = new();
        for (int i = 1; i <= 6; i++)
            list.Add(i);

        int[] skipped = list.AsWide().SkipLong<int>(3).ToArray();
        int[] taken = list.AsWide().TakeLong<int>(3).ToArray();
        int[] chained = list.AsWide().SkipLong<int>(1).TakeLong<int>(3).ToArray();

        CollectionAssert.AreEqual(new[] { 4, 5, 6 }, skipped);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, taken);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, chained);
    }

    [TestMethod]
    public void SkipTake_ThrowForNegativeCount() {
        WideList<int> list = new();
        list.Add(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsWide().SkipLong<int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsWide().TakeLong<int>(-1));
    }

    [TestMethod]
    public void ToWideListArrayAndHashSet_MaterializeCorrectly() {
        WideList<int> source = new();
        source.Add(3);
        source.Add(1);
        source.Add(2);
        source.Add(1);

        WideList<int> list = source.AsWide().WhereWide<int>(x => x > 1).ToWideList();
        WideArray<int> array = source.AsWide().WhereWide<int>(x => x > 1).ToWideArray();
        WideHashSet<int> set = source.AsWide().ToWideHashSet<int>();

        CollectionAssert.AreEqual(new[] { 3, 2 }, list.ToArray());
        CollectionAssert.AreEqual(new[] { 3, 2 }, array.ToArray());
        Assert.AreEqual(3L, set.Count);
        Assert.IsTrue(set.Contains(1));
    }
}

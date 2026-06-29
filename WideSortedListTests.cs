namespace com.hafthor.WideCollections;

[TestClass]
public sealed class WideSortedListTests {
    [TestMethod]
    public void AddAndEnumeration_AreSortedByKey() {
        WideSortedList<int, string> list = new();
        list.Add(3, "c");
        list.Add(1, "a");
        list.Add(2, "b");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Select(p => p.Key).ToArray());
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list.Select(p => p.Value).ToArray());
    }

    [TestMethod]
    public void IndexerContainsTryGetAndRemove_WorkAsExpected() {
        WideSortedList<string, int> list = new();
        list["a"] = 1;
        list["b"] = 2;

        Assert.IsTrue(list.ContainsKey("a"));
        Assert.IsTrue(list.TryGetValue("b", out int value));
        Assert.AreEqual(2, value);
        Assert.IsTrue(list.Remove("a"));
        Assert.IsFalse(list.ContainsKey("a"));
        Assert.IsFalse(list.Remove("a"));
    }

    [TestMethod]
    public void AddDuplicate_ThrowsArgumentException() {
        WideSortedList<int, string> list = new();
        list.Add(1, "a");
        Assert.Throws<ArgumentException>(() => list.Add(1, "b"));
    }

    [TestMethod]
    public void PairContainsAndRemove_UseKeyAndValue() {
        IWideCollection<KeyValuePair<int, string>> list = new WideSortedList<int, string> {
            [1] = "one"
        };

        Assert.IsTrue(list.Contains(new KeyValuePair<int, string>(1, "one")));
        Assert.IsFalse(list.Contains(new KeyValuePair<int, string>(1, "ONE")));
        Assert.IsFalse(list.Remove(new KeyValuePair<int, string>(1, "ONE")));
        Assert.IsTrue(list.Remove(new KeyValuePair<int, string>(1, "one")));
    }

    [TestMethod]
    public void KeysAndValues_AreSortedAndReadOnly() {
        WideSortedList<int, string> list = new() {
            [2] = "two",
            [1] = "one"
        };

        CollectionAssert.AreEqual(new[] { 1, 2 }, list.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { "one", "two" }, list.Values.ToArray());
        Assert.Throws<NotSupportedException>(() => list.Keys.Add(3));
        Assert.Throws<NotSupportedException>(() => list.Values.Clear());
    }

    [TestMethod]
    public void CopyTo_RespectsArrayOffset() {
        WideSortedList<int, string> list = new() {
            [2] = "two",
            [1] = "one"
        };
        WideArray<KeyValuePair<int, string>> array = new(4);

        list.CopyTo(array, 1);

        Assert.AreEqual(new KeyValuePair<int, string>(1, "one"), array[1]);
        Assert.AreEqual(new KeyValuePair<int, string>(2, "two"), array[2]);
    }

    [TestMethod]
    public void NonGenericInterface_WorksWithObjectAccess() {
        IWideDictionary dictionary = new WideSortedList<string, int>();
        dictionary.Add("b", 2);
        dictionary["a"] = 1;

        Assert.AreEqual(2L, dictionary.Count);
        Assert.IsTrue(dictionary.Contains("a"));
        Assert.AreEqual(1, dictionary["a"]);
        dictionary.Remove("a");
        Assert.IsFalse(dictionary.Contains("a"));
    }

    [TestMethod]
    public void IndexAndValueHelpers_WorkAsExpected() {
        WideSortedList<int, string> list = new() {
            [3] = "c",
            [1] = "a",
            [2] = "b"
        };

        Assert.AreEqual(1, list.GetKeyAtIndex(0));
        Assert.AreEqual("b", list.GetValueAtIndex(1));
        Assert.AreEqual(2L, list.IndexOfKey(3));
        Assert.AreEqual(0L, list.IndexOfValue("a"));
        Assert.IsTrue(list.ContainsValue("c"));

        list.SetValueAtIndex(0, "A");
        Assert.AreEqual("A", list[1]);
        list.RemoveAt(1);
        CollectionAssert.AreEqual(new[] { 1, 3 }, list.Keys.ToArray());
    }

    [TestMethod]
    public void TryAddAndCapacityMethods_WorkAsExpected() {
        WideSortedList<int, string> list = new(1);
        Assert.AreEqual(1L, list.Capacity);
        Assert.IsTrue(list.TryAdd(1, "a"));
        Assert.IsFalse(list.TryAdd(1, "b"));

        Assert.AreEqual(-1L, list.IndexOfKey(42));
        Assert.AreEqual(-1L, list.IndexOfValue("missing"));

        Assert.AreEqual(10L, list.EnsureCapacity(10));
        Assert.AreEqual(10L, list.Capacity);
        list.TrimExcess();
        Assert.AreEqual(list.Count, list.Capacity);
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WideSortedList<int, int> list = new(64);
        for (int i = 0; i < 30; i++)
            list.Add(i, i);

        for (int i = 0; i < 25; i++)
            list.Remove(i);

        Assert.IsGreaterThan(list.Count, list.Capacity);
        list.Compact();

        Assert.AreEqual(list.Count, list.Capacity);
        CollectionAssert.AreEqual(new[] { 25, 26, 27, 28, 29 }, list.Keys.ToArray());
    }
}

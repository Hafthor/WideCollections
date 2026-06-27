namespace WideCollections;

[TestClass]
public sealed class WideSortedDictionaryTests {
    [TestMethod]
    public void AddAndEnumeration_AreSortedByKey() {
        WideSortedDictionary<int, string> dictionary = new();
        dictionary.Add(3, "c");
        dictionary.Add(1, "a");
        dictionary.Add(2, "b");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, dictionary.Select(p => p.Key).ToArray());
    }

    [TestMethod]
    public void TryAddContainsValueAndRemoveOut_WorkAsExpected() {
        WideSortedDictionary<string, int> dictionary = new();
        Assert.IsTrue(dictionary.TryAdd("a", 1));
        Assert.IsFalse(dictionary.TryAdd("a", 2));
        Assert.IsTrue(dictionary.ContainsValue(1));

        Assert.IsTrue(dictionary.Remove("a", out int removed));
        Assert.AreEqual(1, removed);
        Assert.IsFalse(dictionary.Remove("a", out _));
    }

    [TestMethod]
    public void CopyToAndReadOnlyViews_WorkAsExpected() {
        WideSortedDictionary<int, string> dictionary = new() {
            [2] = "two",
            [1] = "one"
        };
        WideArray<KeyValuePair<int, string>> pairs = new(3);

        dictionary.CopyTo(pairs, 1);

        Assert.AreEqual(new KeyValuePair<int, string>(1, "one"), pairs[1]);
        Assert.AreEqual(new KeyValuePair<int, string>(2, "two"), pairs[2]);
        CollectionAssert.AreEqual(new[] { 1, 2 }, dictionary.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { "one", "two" }, dictionary.Values.ToArray());
    }

    [TestMethod]
    public void NonGenericInterface_WorksWithObjectAccess() {
        IWideDictionary dictionary = new WideSortedDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary["b"] = 2;

        Assert.AreEqual(2L, dictionary.Count);
        Assert.IsTrue(dictionary.Contains("a"));
        Assert.AreEqual(2, dictionary["b"]);
        dictionary.Remove("a");
        Assert.IsFalse(dictionary.Contains("a"));
    }

    [TestMethod]
    public void Comparer_IsApplied() {
        WideSortedDictionary<string, int> dictionary = new(StringComparer.OrdinalIgnoreCase);
        dictionary.Add("b", 1);
        dictionary.Add("A", 2);

        CollectionAssert.AreEqual(new[] { "A", "b" }, dictionary.Keys.ToArray());
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }
}

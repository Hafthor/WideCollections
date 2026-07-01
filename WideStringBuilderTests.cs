namespace com.hafthor.WideCollections;

[TestClass]
public sealed class WideStringBuilderTests {
    [TestMethod]
    public void Append_Char_And_String_Build() {
        WideStringBuilder sb = new();
        sb.Append('a').Append("bc").Append('d');

        Assert.AreEqual(4L, sb.Length);
        Assert.AreEqual("abcd", sb.ToString());
    }

    [TestMethod]
    public void Append_CharCount_Repeats() {
        WideStringBuilder sb = new();
        sb.Append('z', 3);

        Assert.AreEqual("zzz", sb.ToString());
    }

    [TestMethod]
    public void Constructor_FromString_Seeds() {
        WideStringBuilder sb = new("seed");

        Assert.AreEqual("seed", sb.ToString());
    }

    [TestMethod]
    public void Append_WideString_And_Builder() {
        WideStringBuilder sb = new("a");
        sb.Append(new WideString("bc"));
        WideStringBuilder other = new("de");
        sb.Append(other);

        Assert.AreEqual("abcde", sb.ToString());
    }

    [TestMethod]
    public void Indexer_Get_And_Set() {
        WideStringBuilder sb = new("abc");
        sb[1] = 'X';

        Assert.AreEqual('X', sb[1]);
        Assert.AreEqual("aXc", sb.ToString());
    }

    [TestMethod]
    public void Insert_Char_And_String() {
        WideStringBuilder sb = new("ac");
        sb.Insert(1, 'b');
        Assert.AreEqual("abc", sb.ToString());

        sb.Insert(0, "12");
        Assert.AreEqual("12abc", sb.ToString());
    }

    [TestMethod]
    public void Remove_DeletesRange() {
        WideStringBuilder sb = new("hello world");
        sb.Remove(5, 6);

        Assert.AreEqual("hello", sb.ToString());
    }

    [TestMethod]
    public void Remove_Middle_ShiftsTail() {
        WideStringBuilder sb = new("abcdef");
        sb.Remove(1, 2);

        Assert.AreEqual("adef", sb.ToString());
    }

    [TestMethod]
    public void Remove_OutOfRange_Throws() {
        WideStringBuilder sb = new("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Remove(1, 5));
    }

    [TestMethod]
    public void Length_Set_Truncates() {
        WideStringBuilder sb = new("abcdef");
        sb.Length = 3;

        Assert.AreEqual(3L, sb.Length);
        Assert.AreEqual("abc", sb.ToString());
    }

    [TestMethod]
    public void Length_Set_PadsWithNull() {
        WideStringBuilder sb = new("ab");
        sb.Length = 4;

        Assert.AreEqual(4L, sb.Length);
        Assert.AreEqual('\0', sb[2]);
        Assert.AreEqual('\0', sb[3]);
    }

    [TestMethod]
    public void Clear_EmptiesBuilder() {
        WideStringBuilder sb = new("content");
        sb.Clear();

        Assert.AreEqual(0L, sb.Length);
        Assert.AreEqual("", sb.ToString());
    }

    [TestMethod]
    public void ToWideString_IsIndependentSnapshot() {
        WideStringBuilder sb = new("abc");
        WideString snapshot = sb.ToWideString();

        sb.Append("def");

        Assert.AreEqual("abc", snapshot.ToString());
        Assert.AreEqual("abcdef", sb.ToString());
    }

    [TestMethod]
    public void ToString_Slice_ReturnsRange() {
        WideStringBuilder sb = new("abcdefg");

        Assert.AreEqual("cde", sb.ToString(2, 3));
    }

    [TestMethod]
    public void Enumerator_YieldsAllChars() {
        WideStringBuilder sb = new("xyz");

        List<char> chars = [];
        foreach (char c in sb)
            chars.Add(c);

        CollectionAssert.AreEqual(new List<char> { 'x', 'y', 'z' }, chars);
    }

    [TestMethod]
    public void Capacity_CanBeIncreased() {
        WideStringBuilder sb = new(16);

        Assert.IsGreaterThanOrEqualTo(16L, sb.Capacity);
        sb.Append("hi");
        Assert.AreEqual(2L, sb.Length);
    }
}

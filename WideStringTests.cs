namespace com.hafthor.WideCollections;

[TestClass]
public sealed class WideStringTests {
    [TestMethod]
    public void FromString_HasCorrectLengthAndChars() {
        WideString s = new("hello");

        Assert.AreEqual(5L, s.Length);
        Assert.IsFalse(s.IsEmpty);
        Assert.AreEqual('h', s[0]);
        Assert.AreEqual('o', s[4]);
    }

    [TestMethod]
    public void Empty_IsZeroLength() {
        Assert.AreEqual(0L, WideString.Empty.Length);
        Assert.IsTrue(WideString.Empty.IsEmpty);
        Assert.AreEqual("", WideString.Empty.ToString());
    }

    [TestMethod]
    public void RepeatedChar_Constructor_Fills() {
        WideString s = new('x', 4);

        Assert.AreEqual(4L, s.Length);
        Assert.AreEqual("xxxx", s.ToString());
    }

    [TestMethod]
    public void Substring_StartOnly_ReturnsTail() {
        WideString s = new("hello world");

        Assert.AreEqual("world", s.Substring(6).ToString());
    }

    [TestMethod]
    public void Substring_StartAndLength_ReturnsSlice() {
        WideString s = new("hello world");

        Assert.AreEqual("lo w", s.Substring(3, 4).ToString());
    }

    [TestMethod]
    public void Substring_OutOfRange_Throws() {
        WideString s = new("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => s.Substring(2, 5));
    }

    [TestMethod]
    public void Concat_And_PlusOperator_Combine() {
        WideString a = new("foo");
        WideString b = new("bar");

        Assert.AreEqual("foobar", a.Concat(b).ToString());
        Assert.AreEqual("foobar", (a + b).ToString());
    }

    [TestMethod]
    public void IndexOf_And_Contains_Work() {
        WideString s = new("banana");

        Assert.AreEqual(1L, s.IndexOf('a'));
        Assert.AreEqual(3L, s.IndexOf('a', 2));
        Assert.AreEqual(-1L, s.IndexOf('z'));
        Assert.IsTrue(s.Contains('n'));
        Assert.IsFalse(s.Contains('z'));
    }

    [TestMethod]
    public void StartsWith_And_EndsWith_Work() {
        WideString s = new("hello world");

        Assert.IsTrue(s.StartsWith(new WideString("hello")));
        Assert.IsFalse(s.StartsWith(new WideString("world")));
        Assert.IsTrue(s.EndsWith(new WideString("world")));
        Assert.IsFalse(s.EndsWith(new WideString("hello")));
    }

    [TestMethod]
    public void Equality_And_HashCode_AreConsistent() {
        WideString a = new("test");
        WideString b = new("test");
        WideString c = new("different");

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a == c);
        Assert.IsTrue(a != c);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void CompareTo_OrdersOrdinally() {
        Assert.IsLessThan(0, new WideString("apple").CompareTo(new WideString("banana")));
        Assert.IsGreaterThan(0, new WideString("banana").CompareTo(new WideString("apple")));
        Assert.AreEqual(0, new WideString("same").CompareTo(new WideString("same")));
        Assert.IsLessThan(0, new WideString("ab").CompareTo(new WideString("abc")));
    }

    [TestMethod]
    public void ImplicitFromString_Works() {
        WideString s = "implicit";

        Assert.AreEqual(8L, s.Length);
        Assert.AreEqual("implicit", s.ToString());
    }

    [TestMethod]
    public void ToString_Slice_ReturnsRequestedRange() {
        WideString s = new("abcdefg");

        Assert.AreEqual("cde", s.ToString(2, 3));
    }

    [TestMethod]
    public void ToString_Slice_OutOfRange_Throws() {
        WideString s = new("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => s.ToString(1, 5));
    }

    [TestMethod]
    public void Enumerator_YieldsAllChars() {
        WideString s = new("abc");

        List<char> chars = [];
        foreach (char c in s)
            chars.Add(c);

        CollectionAssert.AreEqual(new List<char> { 'a', 'b', 'c' }, chars);
    }

    [TestMethod]
    public void FromWideArray_CopiesIndependently() {
        WideArray<char> array = new(3);
        array[0] = 'x';
        array[1] = 'y';
        array[2] = 'z';

        WideString s = new(array);
        array[0] = 'Q';

        Assert.AreEqual("xyz", s.ToString());
    }

    [TestMethod]
    public void AsMemory_ReflectsChars() {
        WideString s = new("abc");

        WideReadOnlyMemory<char> mem = s.AsMemory();

        Assert.AreEqual(3L, mem.Length);
        Assert.AreEqual('b', mem[1]);
    }
}

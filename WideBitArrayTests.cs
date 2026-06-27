namespace WideCollections;

[TestClass]
public sealed class WideBitArrayTests {
    [TestMethod]
    public void IndexerSetAndGet_WorkForIndividualBits() {
        WideBitArray bits = new(130);
        bits[0] = true;
        bits[64] = true;
        bits[129] = true;

        Assert.IsTrue(bits.Get(0));
        Assert.IsTrue(bits.Get(64));
        Assert.IsTrue(bits.Get(129));
        Assert.IsFalse(bits.Get(1));
    }

    [TestMethod]
    public void SetAndClear_MethodsToggleBits() {
        WideBitArray bits = new(16);
        bits.Set(3);
        bits.Set(4);
        bits.Clear(3);

        Assert.IsFalse(bits[3]);
        Assert.IsTrue(bits[4]);
    }

    [TestMethod]
    public void SetBitThreadSafe_SetsAndResetsBit() {
        WideBitArray bits = new(10);
        bits.SetBitThreadSafe(7, true);
        Assert.IsTrue(bits[7]);

        bits.SetBitThreadSafe(7, false);
        Assert.IsFalse(bits[7]);
    }

    [TestMethod]
    public void Resize_PreservesExistingBitsWithinRange() {
        WideBitArray bits = new(10);
        bits[2] = true;
        bits[9] = true;

        bits.Resize(80);
        Assert.IsTrue(bits[2]);
        Assert.IsTrue(bits[9]);
        Assert.IsFalse(bits[40]);

        bits.Resize(3);
        Assert.IsTrue(bits[2]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = bits[9]);
    }

    [TestMethod]
    public void Enumerator_IteratesBitValuesInOrder() {
        WideBitArray bits = new(5);
        bits[1] = true;
        bits[4] = true;

        bool[] values = bits.Cast<bool>().ToArray();

        CollectionAssert.AreEqual(new[] { false, true, false, false, true }, values);
    }

    [TestMethod]
    public void Clear_ResetsAllBits() {
        WideBitArray bits = new(70);
        bits[0] = true;
        bits[63] = true;
        bits[69] = true;

        bits.Clear();

        Assert.IsFalse(bits[0]);
        Assert.IsFalse(bits[63]);
        Assert.IsFalse(bits[69]);
    }
}

using SerializerFoundation;

namespace MessagePack.Tests;

// Allocation-bomb guard: ReadArrayHeader/ReadMapHeader reject a claimed count that cannot
// physically fit in the remaining payload (every element occupies at least one byte, a map
// pair at least two) BEFORE any formatter preallocates from it.
// ReadStringHeader/ReadBinHeader/ReadExtHeader carry the same guard in EXACT form: the
// payload itself must occupy the claimed byte count of the remaining data.
public class CollectionHeaderValidationTest
{
    static readonly MessagePackSerializerOptions options = MessagePackSerializerOptions.Default;

    [Fact]
    public void LyingArrayHeader_ThrowsInsteadOfAllocating()
    {
        // array32 claiming 100_000_000 elements in a payload with zero bytes after the header
        var bomb = new byte[] { 0xDD, 0x05, 0xF5, 0xE1, 0x00 };
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[]>(bomb, options));
        Assert.Contains("100000000", ex.Message);

        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<List<int>>(bomb, options));

        // off by one: fixarray(3) with only two elements present
        var shortByOne = new byte[] { 0x93, 0x01, 0x02 };
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[]>(shortByOne, options));
    }

    [Fact]
    public void LyingMapHeader_ThrowsInsteadOfAllocating()
    {
        // map16 claiming 65535 pairs with nothing behind it
        var bomb = new byte[] { 0xDE, 0xFF, 0xFF };
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Dictionary<int, int>>(bomb, options));

        // a pair needs at least two bytes: fixmap(2) with only three bytes behind it
        var shortPair = new byte[] { 0x82, 0x01, 0x02, 0x03 };
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Dictionary<int, int>>(shortPair, options));
    }

    [Fact]
    public void ExactFitHeaders_StillDeserialize()
    {
        // count == remaining bytes is the tightest legal array: fixarray(3) + three fixints
        Assert.Equal(new[] { 1, 2, 3 }, MessagePackSerializer.Deserialize<int[]>(
            new byte[] { 0x93, 0x01, 0x02, 0x03 }, options));

        // 2 * count == remaining bytes is the tightest legal map: fixmap(2) + four fixints
        var map = MessagePackSerializer.Deserialize<Dictionary<int, int>>(
            new byte[] { 0x82, 0x01, 0x02, 0x03, 0x04 }, options);
        Assert.Equal(new Dictionary<int, int> { [1] = 2, [3] = 4 }, map);
    }

    [Fact]
    public void LyingPayloadHeader_ThrowsAtHeader()
    {
        // str8 claiming 255 bytes with nothing behind it, reached through DecimalFormatter
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<decimal>(new byte[] { 0xD9, 0xFF }, options));
        Assert.Contains("255", ex.Message);

        // bin32 claiming int.MaxValue over a 1-byte payload
        Assert.Throws<MessagePackSerializationException>(() => ReadBin([0xC6, 0x7F, 0xFF, 0xFF, 0xFF, 0x00]));

        // ext8 claiming 255 data bytes with nothing behind the type code
        Assert.Throws<MessagePackSerializationException>(() => ReadExt([0xC7, 0xFF, 0x01]));

        static void ReadBin(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            b.ReadBinHeader();
        }

        static void ReadExt(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            b.ReadExtHeader();
        }
    }

    [Fact]
    public void LyingPayloadHeader_ThrowsDuringSkip()
    {
        Assert.Throws<MessagePackSerializationException>(() => SkipOne([0xC6, 0x7F, 0xFF, 0xFF, 0xFF, 0x00])); // bin32
        Assert.Throws<MessagePackSerializationException>(() => SkipOne([0xD9, 0xFF])); // str8
        Assert.Throws<MessagePackSerializationException>(() => SkipOne([0xC7, 0xFF, 0x01])); // ext8

        static void SkipOne(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            b.Skip();
        }
    }

    [Fact]
    public void ExactFitPayloadHeaders_StillRead()
    {
        // byteCount == remaining is the tightest legal claim for str/bin/ext
        Assert.Equal(3, ReadStr([0xD9, 0x03, (byte)'a', (byte)'b', (byte)'c']));
        Assert.Equal(2, ReadBin([0xC4, 0x02, 0x01, 0x02]));
        Assert.Equal((sbyte)7, ReadExtTypeCode([0xC7, 0x01, 0x07, 0x00]));

        static int ReadStr(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            return b.ReadStringHeader();
        }

        static int ReadBin(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            return b.ReadBinHeader();
        }

        static sbyte ReadExtTypeCode(byte[] bytes)
        {
            var b = new ReadOnlySpanReadBuffer(bytes);
            return b.ReadExtHeader().TypeCode;
        }
    }

    [Fact]
    public void LyingNestedHeader_ThrowsDuringSkip()
    {
        // Skip must also reject the lie: array32 bomb nested where a value is skipped.
        // fixmap(1) { "a": <bomb> } deserialized into a type that skips unknown keys is
        // overkill to set up here; going through the reader directly keeps it focused.
        var payload = new byte[] { 0x91, 0xDD, 0x05, 0xF5, 0xE1, 0x00 }; // [ array32(100M) ]
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[][]>(payload, options));
    }
}

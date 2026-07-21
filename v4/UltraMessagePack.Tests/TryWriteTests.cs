using System.Runtime.InteropServices;
using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// TryWrite is an independent simple-cascade implementation with exact-size semantics, so
// the properties to lock are: (1) with exactly the encoded size it succeeds and produces
// the same bytes as the UnsafeWrite implementation (cross-checking the two independent
// implementations against each other), (2) one byte short of the encoded size it fails
// with bytesWritten == 0 and the destination untouched, (3) value-contract violations
// (negative counts, fix range) return false.
public class TryWriteTests
{
    delegate bool TryWriter(Span<byte> destination, out int bytesWritten);
    delegate int UnsafeWriter(ref byte destination);

    static void AssertMirror(TryWriter tryWrite, UnsafeWriter unsafeWrite)
    {
        // Unsafe side needs its worst-case window: for strings that is
        // GetMaxStringByteCount (3 bytes/char + 5 header — the 40-char case needs 125);
        // 256 covers every value used in these tests
        var expected = new byte[256];
        int expectedLen = unsafeWrite(ref MemoryMarshal.GetArrayDataReference(expected));

        // exactly the encoded size succeeds with identical bytes
        var buf = new byte[expectedLen];
        Assert.True(tryWrite(buf, out int written));
        Assert.Equal(expectedLen, written);
        Assert.True(expected.AsSpan(0, expectedLen).SequenceEqual(buf), $"bytes mismatch: unsafe=[{Convert.ToHexString(expected.AsSpan(0, expectedLen))}] try=[{Convert.ToHexString(buf)}]");

        // one byte short fails, reports 0, and writes nothing
        var shortBuf = new byte[expectedLen - 1];
        shortBuf.AsSpan().Fill(0xEE);
        Assert.False(tryWrite(shortBuf, out written));
        Assert.Equal(0, written);
        Assert.True(shortBuf.AsSpan().IndexOfAnyExcept((byte)0xEE) < 0, "failed TryWrite touched the destination");
    }

    [Fact]
    public void Integers_MirrorUnsafe()
    {
        foreach (int v in (int[])[int.MinValue, -32769, -32768, -129, -128, -33, -32, -1, 0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteInt32(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteInt32(ref d, v));
        }
        foreach (long v in (long[])[long.MinValue, int.MinValue - 1L, int.MinValue, -32769, -32, -1, 0, 1, 127, 128, 65536, int.MaxValue, int.MaxValue + 1L, uint.MaxValue, uint.MaxValue + 1L, long.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteInt64(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteInt64(ref d, v));
        }
        foreach (uint v in (uint[])[0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteUInt32(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteUInt32(ref d, v));
        }
        foreach (ulong v in (ulong[])[0, 127, 128, 65535, 65536, uint.MaxValue, uint.MaxValue + 1UL, ulong.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteUInt64(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteUInt64(ref d, v));
        }
        foreach (byte v in (byte[])[0, 127, 128, 255])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteByte(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteByte(ref d, v));
        }
        foreach (sbyte v in (sbyte[])[sbyte.MinValue, -33, -32, 0, sbyte.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteSByte(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteSByte(ref d, v));
        }
        foreach (short v in (short[])[short.MinValue, -129, -128, -33, -32, 0, 127, 128, 255, 256, short.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteInt16(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteInt16(ref d, v));
        }
        foreach (ushort v in (ushort[])[0, 127, 128, 255, 256, ushort.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteUInt16(d, v, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteUInt16(ref d, v));
        }
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteChar(d, 'あ', out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteChar(ref d, 'あ'));
    }

    [Fact]
    public void FixedScalars_MirrorUnsafe()
    {
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteNil(d, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteNil(ref d));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteBoolean(d, true, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteBoolean(ref d, true));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteBoolean(d, false, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteBoolean(ref d, false));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteSingle(d, 1.5f, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteSingle(ref d, 1.5f));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteDouble(d, double.MaxValue, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteDouble(ref d, double.MaxValue));
    }

    [Fact]
    public void Headers_MirrorUnsafe()
    {
        foreach (int count in (int[])[0, 1, 15, 16, 255, 256, 65535, 65536, int.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteArrayHeader(d, count, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteArrayHeader(ref d, count));
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteMapHeader(d, count, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteMapHeader(ref d, count));
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteStringHeader(d, count, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteStringHeader(ref d, count));
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteBinHeader(d, count, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteBinHeader(ref d, count));
        }
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteFixArrayHeader(d, 15, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteFixArrayHeader(ref d, 15));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteFixMapHeader(d, 0, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteFixMapHeader(ref d, 0));
        // ext: fixext1/2/4/8/16, ext8 (0 and 17), ext16, ext32
        foreach (int dataLength in (int[])[0, 1, 2, 3, 4, 8, 16, 17, 255, 256, 65535, 65536, int.MaxValue])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteExtHeader(d, 42, dataLength, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteExtHeader(ref d, 42, dataLength));
        }
    }

    [Fact]
    public void PayloadsAndTimestamp_MirrorUnsafe()
    {
        byte[] bin = [1, 2, 3];
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteBinary(d, bin, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteBinary(ref d, bin));

        foreach (string? s in (string?[])[null, "", "abc", "日本語", "0123456789012345678901234567890123456789"])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteString(d, s, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteString(ref d, s));
        }
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteString(d, "name"u8, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteString(ref d, "name"u8));

        // ts32 (whole seconds in u32 range), ts64 (sub-second precision), ts96 (post-2514)
        var ts32 = new DateTime(2026, 7, 16, 1, 2, 3, DateTimeKind.Utc);
        var ts64 = new DateTime(2026, 7, 16, 1, 2, 3, 456, DateTimeKind.Utc);
        var ts96 = new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var ts in (DateTime[])[ts32, ts64, ts96])
        {
            AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteTimestamp(d, ts, out w),
                (ref byte d) => MessagePackPrimitives.UnsafeWriteTimestamp(ref d, ts));
        }
    }

    [Fact]
    public void ForcedWidth_MirrorUnsafe()
    {
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsInt8(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsInt8(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsUInt8(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsUInt8(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsInt16(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsInt16(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsUInt16(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsUInt16(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsInt32(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsInt32(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsUInt32(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsUInt32(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsInt64(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsInt64(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsUInt64(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsUInt64(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsArray32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsArray32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsMap32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsMap32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsStr32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsStr32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteAsBin32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteAsBin32Header(ref d, 7));
    }

    [Fact]
    public void ValueContractViolations_ReturnFalse()
    {
        var buf = new byte[16];
        Assert.False(MessagePackPrimitives.TryWriteArrayHeader(buf, -1, out int w));
        Assert.Equal(0, w);
        Assert.False(MessagePackPrimitives.TryWriteMapHeader(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteStringHeader(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteBinHeader(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteExtHeader(buf, 42, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteFixArrayHeader(buf, 16, out w));
        Assert.False(MessagePackPrimitives.TryWriteFixArrayHeader(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteFixMapHeader(buf, 16, out w));
        Assert.False(MessagePackPrimitives.TryWriteAsArray32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteAsMap32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteAsStr32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteAsBin32Header(buf, -1, out w));
    }
}

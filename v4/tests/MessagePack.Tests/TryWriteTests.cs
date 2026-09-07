using System.Runtime.InteropServices;
using MessagePack;
using Xunit;

namespace MessagePack.Tests;

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

        // TryWriteString has two internal paths: destination >= GetMaxStringByteCount runs
        // the single-pass speculative writer, [exact, worstCase) runs the two-pass exact
        // count. Sweep every destination size across the crossover: below exact must fail
        // untouched, everything else must produce identical bytes on either path.
        foreach (var s in (string[])["", "a", new string('x', 31), new string('x', 32),
            "あいう", new string('あ', 11), "mix\ud800ed", new string('あ', 100)])
        {
            var worst = MessagePackPrimitives.GetMaxStringByteCount(s);
            var expected = new byte[worst];
            int exact = MessagePackPrimitives.UnsafeWriteString(ref MemoryMarshal.GetArrayDataReference(expected), s);

            for (int size = Math.Max(0, exact - 1); size <= worst + 1; size++)
            {
                var buf = new byte[size];
                buf.AsSpan().Fill(0xEE);
                var ok = MessagePackPrimitives.TryWriteString(buf, s, out var written);
                if (size < exact)
                {
                    Assert.False(ok);
                    Assert.Equal(0, written);
                    Assert.True(buf.AsSpan().IndexOfAnyExcept((byte)0xEE) < 0, $"failed TryWriteString touched the destination (len={s.Length}, size={size})");
                }
                else
                {
                    Assert.True(ok, $"TryWriteString failed with room (len={s.Length}, size={size}, exact={exact})");
                    Assert.Equal(exact, written);
                    Assert.True(expected.AsSpan(0, exact).SequenceEqual(buf.AsSpan(0, exact)), $"path mismatch (len={s.Length}, size={size})");
                }
            }
        }

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
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedInt8(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedInt8(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedUInt8(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedUInt8(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedInt16(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedInt16(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedUInt16(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedUInt16(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedInt32(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedInt32(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedUInt32(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedUInt32(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedInt64(d, -1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedInt64(ref d, -1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedUInt64(d, 1, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedUInt64(ref d, 1));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedArray32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedArray32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedMap32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedMap32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedStr32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedStr32Header(ref d, 7));
        AssertMirror((Span<byte> d, out int w) => MessagePackPrimitives.TryWriteForcedBin32Header(d, 7, out w),
            (ref byte d) => MessagePackPrimitives.UnsafeWriteForcedBin32Header(ref d, 7));
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
        // CA1857: the out-of-range constants are the point, this verifies the fix-header range contract
#pragma warning disable CA1857
        Assert.False(MessagePackPrimitives.TryWriteFixArrayHeader(buf, 16, out w));
        Assert.False(MessagePackPrimitives.TryWriteFixArrayHeader(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteFixMapHeader(buf, 16, out w));
#pragma warning restore CA1857
        Assert.False(MessagePackPrimitives.TryWriteForcedArray32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteForcedMap32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteForcedStr32Header(buf, -1, out w));
        Assert.False(MessagePackPrimitives.TryWriteForcedBin32Header(buf, -1, out w));
    }

    // TryWriteBinary / TryWriteString(utf8) accept a payload that overlaps the destination (payload copied before
    // the header is written), so bytes already sitting in the buffer can be wrapped in place. Sweep every payload
    // placement relative to the header slot, at every header class (fixstr/str8/str16/str32, bin8/bin16/bin32),
    // and compare against encoding from a separate copy.
    [Fact]
    public void PayloadOverlappingDestination_EncodesInPlace()
    {
        foreach (int length in (int[])[0, 1, 31, 32, 255, 256, 65535, 65536])
        {
            var payload = new byte[length];
            for (int i = 0; i < length; i++) payload[i] = (byte)(i * 31 + 7);

            var expectedBin = new byte[length + 5];
            Assert.True(MessagePackPrimitives.TryWriteBinary(expectedBin, payload, out int expectedBinLen));
            var expectedStr = new byte[length + 5];
            Assert.True(MessagePackPrimitives.TryWriteString(expectedStr, payload, out int expectedStrLen));

            int binHeader = expectedBinLen - length;
            int strHeader = expectedStrLen - length;

            // payload starts at every offset from 0 (fully inside the header slot, moved forward) through the
            // header size (already in place, only the header lands) to beyond it (moved backward)
            foreach (int offset in (int[])[0, 1, binHeader - 1, binHeader, binHeader + 1, strHeader, 5, 6, 16])
            {
                if (offset < 0) continue;

                var buf = new byte[offset + length + 5];
                buf.AsSpan().Fill(0xEE);
                payload.CopyTo(buf, offset);
                Assert.True(MessagePackPrimitives.TryWriteBinary(buf, buf.AsSpan(offset, length), out int written),
                    $"bin length={length} offset={offset}");
                Assert.Equal(expectedBinLen, written);
                Assert.True(expectedBin.AsSpan(0, expectedBinLen).SequenceEqual(buf.AsSpan(0, written)),
                    $"bin bytes differ: length={length} offset={offset}");

                buf.AsSpan().Fill(0xEE);
                payload.CopyTo(buf, offset);
                Assert.True(MessagePackPrimitives.TryWriteString(buf, buf.AsSpan(offset, length), out written),
                    $"str length={length} offset={offset}");
                Assert.Equal(expectedStrLen, written);
                Assert.True(expectedStr.AsSpan(0, expectedStrLen).SequenceEqual(buf.AsSpan(0, written)),
                    $"str bytes differ: length={length} offset={offset}");
            }

            // destination that is exactly the payload's own storage plus header room: the tightest in-place wrap
            var tight = new byte[expectedBinLen];
            payload.CopyTo(tight, 0);
            Assert.True(MessagePackPrimitives.TryWriteBinary(tight, tight.AsSpan(0, length), out int tightWritten));
            Assert.Equal(expectedBinLen, tightWritten);
            Assert.True(expectedBin.AsSpan(0, expectedBinLen).SequenceEqual(tight));

            // and one byte short of that still fails without touching the payload
            if (length > 0)
            {
                var tooTight = new byte[expectedBinLen - 1];
                payload.CopyTo(tooTight, 0);
                Assert.False(MessagePackPrimitives.TryWriteBinary(tooTight, tooTight.AsSpan(0, length), out tightWritten));
                Assert.Equal(0, tightWritten);
                Assert.True(payload.AsSpan().SequenceEqual(tooTight.AsSpan(0, length)), "failed overlapping TryWriteBinary touched the payload");
            }
        }
    }
}

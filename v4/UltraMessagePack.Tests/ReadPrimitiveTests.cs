using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// Read primitives verified against the TryWrite layer (independent cascade implementation).
// Properties locked per value: (1) roundtrip returns Success with bytesConsumed == the
// encoded length, (2) a well-formed message whose value doesn't fit the target type folds
// into TokenMismatch (bytesConsumed == 0 — only Success reports a length), (3) every
// strict prefix of a valid message reports InsufficientBuffer with bytesConsumed == 0,
// (4) a non-target token reports TokenMismatch with bytesConsumed == 0.
public class ReadPrimitiveTests
{
    // every strict prefix must report InsufficientBuffer with a tokenSize that (a) demands
    // more than the window it was given — the retry-loop progress guarantee — and (b) never
    // exceeds the true token length. (str/bin report the header requirement first, so exact
    // equality with the full length only holds once the header is visible.)
    static void AssertTruncationFails(byte[] message, TryReader reader)
    {
        for (int cut = 0; cut < message.Length; cut++)
        {
            Assert.Equal(DecodeResult.InsufficientBuffer, reader(message.AsSpan(0, cut), out int tokenSize));
            Assert.True(tokenSize > cut && tokenSize <= message.Length,
                $"prefix {cut} of [{Convert.ToHexString(message)}]: tokenSize {tokenSize} must be in ({cut}, {message.Length}]");
        }
    }

    delegate DecodeResult TryReader(ReadOnlySpan<byte> source, out int tokenSize);

    static DecodeResult FitOr(bool fits) => fits ? DecodeResult.Success : DecodeResult.TokenMismatch;

    static int LenOr(bool fits, int written) => fits ? written : 0;

    [Fact]
    public void SignedIntegers_RoundtripAndFit()
    {
        foreach (long v in (long[])[long.MinValue, int.MinValue - 1L, int.MinValue, -65537, -65536, -32769, -32768, -129, -128, -33, -32, -1,
            0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue, int.MaxValue + 1L, uint.MaxValue, uint.MaxValue + 1L, long.MaxValue])
        {
            var buf = new byte[9];
            Assert.True(MessagePackPrimitives.TryWriteInt64(buf, v, out int written));
            var msg = buf.AsSpan(0, written);

            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadInt64(msg, out long i64, out int consumed));
            Assert.Equal(v, i64);
            Assert.Equal(written, consumed);

            // narrower targets: fit succeeds; non-fit folds to TokenMismatch with consumed 0
            Assert.Equal(FitOr(v is >= int.MinValue and <= int.MaxValue), MessagePackPrimitives.TryReadInt32(msg, out int i32, out consumed));
            Assert.Equal(LenOr(v is >= int.MinValue and <= int.MaxValue, written), consumed);
            if (v is >= int.MinValue and <= int.MaxValue) Assert.Equal((int)v, i32);

            Assert.Equal(FitOr(v is >= short.MinValue and <= short.MaxValue), MessagePackPrimitives.TryReadInt16(msg, out short i16, out consumed));
            Assert.Equal(LenOr(v is >= short.MinValue and <= short.MaxValue, written), consumed);
            if (v is >= short.MinValue and <= short.MaxValue) Assert.Equal((short)v, i16);

            Assert.Equal(FitOr(v is >= sbyte.MinValue and <= sbyte.MaxValue), MessagePackPrimitives.TryReadSByte(msg, out sbyte i8, out consumed));
            Assert.Equal(LenOr(v is >= sbyte.MinValue and <= sbyte.MaxValue, written), consumed);
            if (v is >= sbyte.MinValue and <= sbyte.MaxValue) Assert.Equal((sbyte)v, i8);

            // unsigned targets reject negatives
            Assert.Equal(FitOr(v >= 0), MessagePackPrimitives.TryReadUInt64(msg, out ulong u64, out consumed));
            Assert.Equal(LenOr(v >= 0, written), consumed);
            if (v >= 0) Assert.Equal((ulong)v, u64);

            var msgArray = msg.ToArray();
            AssertTruncationFails(msgArray, (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadInt64(s, out _, out c));
            AssertTruncationFails(msgArray, (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadInt32(s, out _, out c));
            AssertTruncationFails(msgArray, (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadUInt64(s, out _, out c));
        }

        // a non-int token is TokenMismatch with 0 (both fast and careful windows)
        byte[] notInt = [MessagePackCode.MinFixStr, 0, 0, 0, 0, 0, 0, 0, 0];
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadInt64(notInt, out _, out int mc));
        Assert.Equal(0, mc);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadInt64(notInt.AsSpan(0, 2), out _, out mc));
        Assert.Equal(0, mc);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadInt32(notInt, out _, out mc));
        Assert.Equal(0, mc);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadUInt64(notInt, out _, out mc));
        Assert.Equal(0, mc);
    }

    [Fact]
    public void UnsignedIntegers_RoundtripAndFit()
    {
        foreach (ulong v in (ulong[])[0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue, uint.MaxValue + 1UL, long.MaxValue, long.MaxValue + 1UL, ulong.MaxValue])
        {
            var buf = new byte[9];
            Assert.True(MessagePackPrimitives.TryWriteUInt64(buf, v, out int written));
            var msg = buf.AsSpan(0, written);

            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadUInt64(msg, out ulong u64, out int consumed));
            Assert.Equal(v, u64);
            Assert.Equal(written, consumed);

            Assert.Equal(FitOr(v <= uint.MaxValue), MessagePackPrimitives.TryReadUInt32(msg, out uint u32, out consumed));
            Assert.Equal(LenOr(v <= uint.MaxValue, written), consumed);
            if (v <= uint.MaxValue) Assert.Equal((uint)v, u32);

            Assert.Equal(FitOr(v <= ushort.MaxValue), MessagePackPrimitives.TryReadUInt16(msg, out ushort u16, out consumed));
            Assert.Equal(LenOr(v <= ushort.MaxValue, written), consumed);
            if (v <= ushort.MaxValue) Assert.Equal((ushort)v, u16);

            Assert.Equal(FitOr(v <= byte.MaxValue), MessagePackPrimitives.TryReadByte(msg, out byte u8, out consumed));
            Assert.Equal(LenOr(v <= byte.MaxValue, written), consumed);
            if (v <= byte.MaxValue) Assert.Equal((byte)v, u8);

            Assert.Equal(FitOr(v <= char.MaxValue), MessagePackPrimitives.TryReadChar(msg, out char ch, out consumed));
            if (v <= char.MaxValue) Assert.Equal((char)v, ch);

            // signed target: uint64 above long.MaxValue must not alias to a negative long
            Assert.Equal(FitOr(v <= long.MaxValue), MessagePackPrimitives.TryReadInt64(msg, out long i64, out consumed));
            Assert.Equal(LenOr(v <= long.MaxValue, written), consumed);
            if (v <= long.MaxValue) Assert.Equal((long)v, i64);
        }
    }

    [Fact]
    public void NilAndBoolean()
    {
        Assert.True(MessagePackPrimitives.TryReadNil([MessagePackCode.Nil]));
        Assert.False(MessagePackPrimitives.TryReadNil([MessagePackCode.True]));
        Assert.False(MessagePackPrimitives.TryReadNil([]));
        int consumed;

        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadBoolean([MessagePackCode.True], out bool b, out consumed));
        Assert.True(b);
        Assert.Equal(1, consumed);
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadBoolean([MessagePackCode.False], out b, out consumed));
        Assert.False(b);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadBoolean([MessagePackCode.Nil], out _, out consumed));
        Assert.Equal(0, consumed);
        Assert.Equal(DecodeResult.InsufficientBuffer, MessagePackPrimitives.TryReadBoolean([], out _, out _));
    }

    [Fact]
    public void Floats_Roundtrip()
    {
        foreach (double v in (double[])[0, -0.0, 1, -1, 0.5, double.MaxValue, double.MinValue, double.Epsilon, double.PositiveInfinity, double.NegativeInfinity, double.NaN, 3.14159265358979])
        {
            var buf = new byte[9];
            Assert.True(MessagePackPrimitives.TryWriteDouble(buf, v, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadDouble(buf.AsSpan(0, written), out double d, out int consumed));
            Assert.Equal(v, d);
            Assert.Equal(written, consumed);
            AssertTruncationFails(buf.AsSpan(0, written).ToArray(), (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadDouble(s, out _, out c));
        }
        foreach (float v in (float[])[0, 1, -1, float.MaxValue, float.MinValue, float.Epsilon, float.PositiveInfinity, float.NaN, 1.5f])
        {
            var buf = new byte[5];
            Assert.True(MessagePackPrimitives.TryWriteSingle(buf, v, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadSingle(buf, out float f, out int consumed));
            Assert.Equal(v, f);
            Assert.Equal(written, consumed);

            // float32 widens exactly into double
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadDouble(buf, out double d, out consumed));
            Assert.Equal(v, d);
            Assert.Equal(5, consumed);
        }

        // int formats read as float/double (MessagePack-CSharp compatible)
        var intBuf = new byte[9];
        Assert.True(MessagePackPrimitives.TryWriteInt64(intBuf, -300, out int w));
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadDouble(intBuf.AsSpan(0, w), out double dv, out int c2));
        Assert.Equal(-300d, dv);
        Assert.Equal(w, c2);
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadSingle(intBuf.AsSpan(0, w), out float fv, out c2));
        Assert.Equal(-300f, fv);
        Assert.True(MessagePackPrimitives.TryWriteUInt64(intBuf, ulong.MaxValue, out w));
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadDouble(intBuf.AsSpan(0, w), out dv, out c2));
        Assert.Equal((double)ulong.MaxValue, dv);
        Assert.Equal(w, c2);

        // a non-numeric token is TokenMismatch
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadDouble([MessagePackCode.MinFixStr], out _, out int mc));
        Assert.Equal(0, mc);
    }

    [Fact]
    public void Headers_Roundtrip()
    {
        foreach (int count in (int[])[0, 1, 15, 16, 255, 256, 65535, 65536, int.MaxValue])
        {
            var buf = new byte[5];
            Assert.True(MessagePackPrimitives.TryWriteArrayHeader(buf, count, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadArrayHeader(buf.AsSpan(0, written), out int c, out int consumed));
            Assert.Equal(count, c);
            Assert.Equal(written, consumed);
            AssertTruncationFails(buf.AsSpan(0, written).ToArray(), (ReadOnlySpan<byte> s, out int cc) => MessagePackPrimitives.TryReadArrayHeader(s, out _, out cc));

            Assert.True(MessagePackPrimitives.TryWriteMapHeader(buf, count, out written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadMapHeader(buf.AsSpan(0, written), out c, out consumed));
            Assert.Equal(count, c);
            Assert.Equal(written, consumed);
        }
        foreach (int len in (int[])[0, 1, 31, 32, 255, 256, 65535, 65536, int.MaxValue])
        {
            var buf = new byte[5];
            Assert.True(MessagePackPrimitives.TryWriteStringHeader(buf, len, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadStringHeader(buf.AsSpan(0, written), out int c, out int consumed));
            Assert.Equal(len, c);
            Assert.Equal(written, consumed);

            Assert.True(MessagePackPrimitives.TryWriteBinHeader(buf, len, out written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadBinHeader(buf.AsSpan(0, written), out c, out consumed));
            Assert.Equal(len, c);
            Assert.Equal(written, consumed);
        }

        // 32-bit headers with a count above int.MaxValue fold to TokenMismatch (consumed 0)
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadArrayHeader([MessagePackCode.Array32, 0xff, 0xff, 0xff, 0xff], out _, out int over));
        Assert.Equal(0, over);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadMapHeader([MessagePackCode.Map32, 0xff, 0xff, 0xff, 0xff], out _, out over));
        Assert.Equal(0, over);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadStringHeader([MessagePackCode.Str32, 0xff, 0xff, 0xff, 0xff], out _, out over));
        Assert.Equal(0, over);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadBinHeader([MessagePackCode.Bin32, 0xff, 0xff, 0xff, 0xff], out _, out over));
        Assert.Equal(0, over);

        // wrong token kind is TokenMismatch
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadArrayHeader([MessagePackCode.MinFixMap], out _, out int mc));
        Assert.Equal(0, mc);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadMapHeader([MessagePackCode.MinFixArray], out _, out mc));
        Assert.Equal(0, mc);
    }

    [Fact]
    public void Binary_Roundtrip()
    {
        foreach (int len in (int[])[0, 1, 255, 256, 65535, 65536])
        {
            var payload = new byte[len];
            new Random(42).NextBytes(payload);
            var buf = new byte[len + 5];
            Assert.True(MessagePackPrimitives.TryWriteBinary(buf, payload, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadBinary(buf.AsSpan(0, written), out var read, out int consumed));
            Assert.True(read.SequenceEqual(payload));
            Assert.Equal(written, consumed);

            // truncated payload: InsufficientBuffer with tokenSize = header + payload
            Assert.Equal(DecodeResult.InsufficientBuffer, MessagePackPrimitives.TryReadBinary(buf.AsSpan(0, written - 1), out _, out consumed));
            Assert.Equal(written, consumed);
        }
    }

    [Fact]
    public void ExtHeader_Roundtrip()
    {
        foreach (sbyte typeCode in (sbyte[])[sbyte.MinValue, -1, 0, 7, sbyte.MaxValue])
        {
            foreach (int len in (int[])[0, 1, 2, 3, 4, 5, 8, 16, 17, 255, 256, 65535, 65536, int.MaxValue])
            {
                var buf = new byte[6];
                Assert.True(MessagePackPrimitives.TryWriteExtHeader(buf, typeCode, len, out int written));
                Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadExtHeader(buf.AsSpan(0, written), out sbyte t, out int l, out int consumed));
                Assert.Equal(typeCode, t);
                Assert.Equal(len, l);
                Assert.Equal(written, consumed);
                AssertTruncationFails(buf.AsSpan(0, written).ToArray(), (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadExtHeader(s, out _, out _, out c));
            }
        }
    }

    [Fact]
    public void Timestamp_Roundtrip()
    {
        foreach (DateTime v in (DateTime[])
        [
            DateTime.UnixEpoch,
            new DateTime(2026, 7, 17, 12, 34, 56, DateTimeKind.Utc),                       // ts32 (whole seconds)
            new DateTime(2026, 7, 17, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567),     // ts64 (sub-second)
            new DateTime(2106, 2, 7, 6, 28, 16, DateTimeKind.Utc),                         // first second past ts32 range
            new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc),                      // pre-epoch -> ts96
            new DateTime(2600, 1, 1, 0, 0, 0, DateTimeKind.Utc),                           // past ts64 range -> ts96
            DateTime.MinValue,
            DateTime.MaxValue,
        ])
        {
            var buf = new byte[15];
            Assert.True(MessagePackPrimitives.TryWriteTimestamp(buf, v, out int written));
            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadTimestamp(buf.AsSpan(0, written), out DateTime read, out int consumed));
            Assert.Equal(DateTime.SpecifyKind(v, DateTimeKind.Utc), read);
            Assert.Equal(DateTimeKind.Utc, read.Kind);
            Assert.Equal(written, consumed);
            AssertTruncationFails(buf.AsSpan(0, written).ToArray(), (ReadOnlySpan<byte> s, out int c) => MessagePackPrimitives.TryReadTimestamp(s, out _, out c));
        }

        // a well-formed ext of another type folds to TokenMismatch (consumed 0)
        var extBuf = new byte[6];
        Assert.True(MessagePackPrimitives.TryWriteExtHeader(extBuf, 7, 4, out int headerLen));
        var msg = new byte[headerLen + 4];
        extBuf.AsSpan(0, headerLen).CopyTo(msg);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadTimestamp(msg, out _, out int skipLen));
        Assert.Equal(0, skipLen);

        // ts96 below DateTime.MinValue: TokenMismatch (consumed 0)
        var under = new byte[15];
        Assert.True(MessagePackPrimitives.TryWriteExtHeader(under, -1, 12, out int h96));
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(under.AsSpan(h96 + 4), -63000000000000); // < year 1
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadTimestamp(under.AsSpan(0, h96 + 12), out _, out skipLen));
        Assert.Equal(0, skipLen);
    }

    [Fact]
    public void Strings_Roundtrip()
    {
        foreach (string? v in (string?[])[null, "", "a", new string('x', 31), new string('x', 32), "こんにちは世界", "🌍emoji🌍",
            new string('y', 255), new string('y', 256), new string('あ', 100), new string('z', 65535), new string('z', 65536)])
        {
            var buf = new byte[(v?.Length ?? 0) * 3 + 5];
            Assert.True(MessagePackPrimitives.TryWriteString(buf, v, out int written));
            var msg = buf.AsSpan(0, written);

            Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadString(msg, out string? read, out int consumed));
            Assert.Equal(v, read);
            Assert.Equal(written, consumed);

            if (v != null)
            {
                Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadStringSpan(msg, out var utf8, out consumed));
                Assert.Equal(v, System.Text.Encoding.UTF8.GetString(utf8));
                Assert.Equal(written, consumed);

                // truncated payload: InsufficientBuffer with tokenSize = header + payload
                // (the header is fully visible at written - 1, so the report is exact)
                if (written > 1)
                {
                    Assert.Equal(DecodeResult.InsufficientBuffer, MessagePackPrimitives.TryReadString(msg.Slice(0, written - 1), out _, out consumed));
                    Assert.Equal(written, consumed);
                }
            }
        }

        // a non-str token is TokenMismatch
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadString([MessagePackCode.True], out _, out int mc));
        Assert.Equal(0, mc);
    }

    [Fact]
    public void AdversarialHugeLengths_ReportTokenMismatch_NeverNegativeTokenSize()
    {
        // str32/bin32/ext32 headers claiming ~int.MaxValue payloads: headerSize + byteCount
        // would wrap negative, and a negative tokenSize hangs sequence retry loops
        // (required <= BytesRemaining is always true for a negative requirement). The
        // total exceeds any span this platform can hold, so it folds to TokenMismatch.
        Span<byte> str32 = stackalloc byte[6];
        str32[0] = MessagePackCode.Str32;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(str32.Slice(1), int.MaxValue);

        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadString(str32, out _, out int ts));
        Assert.Equal(0, ts);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadStringSpan(str32, out _, out ts));
        Assert.Equal(0, ts);

        Span<byte> bin32 = stackalloc byte[6];
        bin32[0] = MessagePackCode.Bin32;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bin32.Slice(1), int.MaxValue);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadBinary(bin32, out _, out ts));
        Assert.Equal(0, ts);

        // ext32(-1) with a huge length: fatal before any Insufficient report (a
        // non-timestamp length can never become readable, no matter how much arrives)
        Span<byte> ext32 = stackalloc byte[6];
        ext32[0] = MessagePackCode.Ext32;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(ext32.Slice(1), int.MaxValue);
        ext32[5] = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
        Assert.Equal(DecodeResult.TokenMismatch, MessagePackPrimitives.TryReadTimestamp(ext32, out _, out ts));
        Assert.Equal(0, ts);

        // a large-but-representable claim stays InsufficientBuffer with the exact
        // (positive) total — the retry loop's BytesRemaining guard then terminates it
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(str32.Slice(1), int.MaxValue - 10);
        Assert.Equal(DecodeResult.InsufficientBuffer, MessagePackPrimitives.TryReadString(str32, out _, out ts));
        Assert.Equal(5 + (int.MaxValue - 10), ts);
    }
}

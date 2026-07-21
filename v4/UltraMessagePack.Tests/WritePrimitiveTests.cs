using System.Buffers;
using System.Runtime.InteropServices;
using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// every write primitive, byte-compared against MessagePack-CSharp's MessagePackWriter
public class WritePrimitiveTests
{
    static byte[] Ours(Func<byte[], int> write)
    {
        var buf = new byte[16];
        int len = write(buf);
        return buf[..len];
    }

    static readonly int[] HeaderCounts =
        [0, 1, 14, 15, 16, 17, 31, 32, 33, 254, 255, 256, 257, 65534, 65535, 65536, 65537, 1_000_000, int.MaxValue];

    [Fact]
    public void ArrayHeader_MatchOracle()
    {
        foreach (var count in HeaderCounts)
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteArrayHeader(count));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteArrayHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"array header mismatch count={count}: ours=[{Convert.ToHexString(ours)}] oracle=[{Convert.ToHexString(oracle)}]");
        }
    }

    [Fact]
    public void MapHeader_MatchOracle()
    {
        foreach (var count in HeaderCounts)
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteMapHeader(count));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteMapHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"map header mismatch count={count}");
        }
    }

    [Fact]
    public void FixHeaders_MatchOracle()
    {
        for (int count = 0; count <= 15; count++)
        {
            var arrayOracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteArrayHeader(count));
            var arrayOurs = Ours(buf => MessagePackPrimitives.UnsafeWriteFixArrayHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            Assert.True(arrayOracle.AsSpan().SequenceEqual(arrayOurs), $"fixarray header mismatch count={count}");

            var mapOracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteMapHeader(count));
            var mapOurs = Ours(buf => MessagePackPrimitives.UnsafeWriteFixMapHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            Assert.True(mapOracle.AsSpan().SequenceEqual(mapOurs), $"fixmap header mismatch count={count}");
        }
    }

    [Fact]
    public void WriteAs_MatchOracle()
    {
        // the oracle's WriteInt8/WriteUInt16/... are its forced-format writers
        foreach (sbyte v in (sbyte[])[sbyte.MinValue, -32, -1, 0, 1, sbyte.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteInt8(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsInt8(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as int8 mismatch value={v}");
        }
        foreach (byte v in (byte[])[0, 1, 127, 128, byte.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteUInt8(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsUInt8(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as uint8 mismatch value={v}");
        }
        foreach (short v in (short[])[short.MinValue, -129, -32, -1, 0, 1, 127, 128, short.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteInt16(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsInt16(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as int16 mismatch value={v}");
        }
        foreach (ushort v in (ushort[])[0, 1, 127, 128, 255, 256, ushort.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteUInt16(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsUInt16(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as uint16 mismatch value={v}");
        }
        foreach (int v in (int[])[int.MinValue, -32769, -32768, -129, -128, -32, -1, 0, 1, 127, 128, 65535, 65536, int.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteInt32(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsInt32(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as int32 mismatch value={v}");
        }
        foreach (uint v in (uint[])[0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteUInt32(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsUInt32(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as uint32 mismatch value={v}");
        }
        foreach (long v in (long[])[long.MinValue, int.MinValue - 1L, -32769, -32, -1, 0, 1, 127, 65536, int.MaxValue + 1L, long.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteInt64(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsInt64(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as int64 mismatch value={v}");
        }
        foreach (ulong v in (ulong[])[0, 1, 127, 128, 255, 65536, uint.MaxValue, uint.MaxValue + 1UL, ulong.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteUInt64(v));
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteAsUInt64(ref MemoryMarshal.GetArrayDataReference(buf), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"as uint64 mismatch value={v}");
        }
    }

    [Fact]
    public void WriteAsHeaders_SpecBytes()
    {
        // no forced-header oracle API exists; compare against the spec layout directly
        foreach (int count in (int[])[0, 1, 15, 16, 65535, 65536, int.MaxValue])
        {
            byte[] be = [(byte)(count >>> 24), (byte)(count >>> 16), (byte)(count >>> 8), (byte)count];
            Assert.Equal([0xdd, .. be], Ours(buf => MessagePackPrimitives.UnsafeWriteAsArray32Header(ref MemoryMarshal.GetArrayDataReference(buf), count)));
            Assert.Equal([0xdf, .. be], Ours(buf => MessagePackPrimitives.UnsafeWriteAsMap32Header(ref MemoryMarshal.GetArrayDataReference(buf), count)));
            Assert.Equal([0xdb, .. be], Ours(buf => MessagePackPrimitives.UnsafeWriteAsStr32Header(ref MemoryMarshal.GetArrayDataReference(buf), count)));
            Assert.Equal([0xc6, .. be], Ours(buf => MessagePackPrimitives.UnsafeWriteAsBin32Header(ref MemoryMarshal.GetArrayDataReference(buf), count)));
        }
    }

    [Fact]
    public void StringHeader_MatchOracle()
    {
        foreach (var count in HeaderCounts)
        {
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteStringHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            if (count == int.MaxValue)
            {
                // the oracle's WriteStringHeader pre-reserves header + payload and overflows here;
                // compare against the spec bytes directly
                Assert.Equal((byte[])[0xdb, 0x7f, 0xff, 0xff, 0xff], ours);
                continue;
            }
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteStringHeader(count));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"string header mismatch count={count}");
        }
    }

    [Fact]
    public void BinHeader_MatchOracle()
    {
        foreach (var count in HeaderCounts)
        {
            var ours = Ours(buf => MessagePackPrimitives.UnsafeWriteBinHeader(ref MemoryMarshal.GetArrayDataReference(buf), count));
            if (count == int.MaxValue)
            {
                Assert.Equal((byte[])[0xc6, 0x7f, 0xff, 0xff, 0xff], ours);
                continue;
            }
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteBinHeader(count));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"bin header mismatch count={count}");
        }
    }

    [Fact]
    public void FixedScalars_MatchOracle()
    {
        Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteNil()), Ours(b => MessagePackPrimitives.UnsafeWriteNil(ref MemoryMarshal.GetArrayDataReference(b))));
        Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(true)), Ours(b => MessagePackPrimitives.UnsafeWriteBoolean(ref MemoryMarshal.GetArrayDataReference(b), true)));
        Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(false)), Ours(b => MessagePackPrimitives.UnsafeWriteBoolean(ref MemoryMarshal.GetArrayDataReference(b), false)));
        foreach (var f in (float[])[0f, 1.5f, -1.5f, float.NaN, float.PositiveInfinity, float.MaxValue, MathF.PI])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(f)), Ours(b => MessagePackPrimitives.UnsafeWriteSingle(ref MemoryMarshal.GetArrayDataReference(b), f)));
        }
        foreach (var d in (double[])[0, 1.5, -1.5, double.NaN, double.Epsilon, double.MaxValue, Math.PI])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(d)), Ours(b => MessagePackPrimitives.UnsafeWriteDouble(ref MemoryMarshal.GetArrayDataReference(b), d)));
        }
    }

    [Fact]
    public void UInt32AndNarrow_MatchOracle()
    {
        foreach (var v in (uint[])[0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue, (uint)int.MaxValue + 1, uint.MaxValue])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v));
            var ours = Ours(b => MessagePackPrimitives.UnsafeWriteUInt32(ref MemoryMarshal.GetArrayDataReference(b), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"uint32 mismatch {v}");
        }
        foreach (var v in (short[])[0, 1, -1, -32, -33, 127, 128, 255, 256, short.MaxValue, short.MinValue, -128, -129])
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v));
            var ours = Ours(b => MessagePackPrimitives.UnsafeWriteInt16(ref MemoryMarshal.GetArrayDataReference(b), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"int16 mismatch {v}");
        }
        foreach (var v in (ushort[])[0, 127, 128, 255, 256, ushort.MaxValue])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v)), Ours(b => MessagePackPrimitives.UnsafeWriteUInt16(ref MemoryMarshal.GetArrayDataReference(b), v)));
        }
        foreach (var v in (byte[])[0, 127, 128, 255])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v)), Ours(b => MessagePackPrimitives.UnsafeWriteByte(ref MemoryMarshal.GetArrayDataReference(b), v)));
        }
        foreach (var v in (sbyte[])[0, 1, -1, -32, -33, -128, 127])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v)), Ours(b => MessagePackPrimitives.UnsafeWriteSByte(ref MemoryMarshal.GetArrayDataReference(b), v)));
        }
    }

    [Fact]
    public void StringPrimitive_DirectCall_MatchOracle()
    {
        // the primitive standalone (no MessagePackWriter): caller provides the 3L+5 window.
        // includes >= 65536 chars, which the Writer never routes here (it takes the
        // exact-reservation path) but the primitive must handle for direct callers
        string?[] values =
        [
            null, "", "a", "こんにちは", new string('x', 20), new string('あ', 20),
            new string('x', 300), new string('あ', 300), new string('あ', 30000),
            new string('x', 70000), new string('あ', 70000),
        ];
        foreach (var v in values)
        {
            var buf = new byte[(v?.Length ?? 0) * 3 + 5];
            int len = MessagePackPrimitives.UnsafeWriteString(ref MemoryMarshal.GetArrayDataReference(buf), v);
            var oracle = MessagePack.MessagePackSerializer.Serialize(v);
            Assert.True(buf.AsSpan(0, len).SequenceEqual(oracle), $"string primitive mismatch len={v?.Length}");
        }

        var bin = new byte[300];
        new Random(42).NextBytes(bin);
        var binBuf = new byte[bin.Length + 5];
        int binLen = MessagePackPrimitives.UnsafeWriteBinary(ref MemoryMarshal.GetArrayDataReference(binBuf), bin);
        Assert.Equal(MessagePack.MessagePackSerializer.Serialize(bin), binBuf[..binLen]);
    }

    [Fact]
    public void Boolean_NonNormalizedBool_StillWritesValidCode()
    {
        // bools with raw bytes other than 0/1 can only come from unsafe/interop code,
        // but a raw OR would turn them into 196 = Bin8 and corrupt stream framing
        foreach (byte rawByte in (byte[])[0, 1, 2, 3, 127, 128, 254, 255])
        {
            byte raw = rawByte;
            bool corrupt = System.Runtime.CompilerServices.Unsafe.As<byte, bool>(ref raw);
            var buf = new byte[1];
            int len = MessagePackPrimitives.UnsafeWriteBoolean(ref MemoryMarshal.GetArrayDataReference(buf), corrupt);
            Assert.Equal(1, len);
            byte expected = rawByte == 0 ? MessagePackCode.False : MessagePackCode.True;
            Assert.True(buf[0] == expected, $"raw byte {rawByte}: wrote {buf[0]} (expected {expected})");
        }
    }

    [Fact]
    public void ExtHeader_MatchOracle()
    {
        int[] lengths = [0, 1, 2, 3, 4, 5, 7, 8, 9, 15, 16, 17, 32, 254, 255, 256, 257, 65534, 65535, 65536, 65537, 1_000_000, int.MaxValue];
        sbyte[] typeCodes = [0, 1, -1, 42, sbyte.MaxValue, sbyte.MinValue];
        foreach (var length in lengths)
        {
            foreach (var typeCode in typeCodes)
            {
                var ours = Ours(b => MessagePackPrimitives.UnsafeWriteExtHeader(ref MemoryMarshal.GetArrayDataReference(b), typeCode, length));
                if (length == int.MaxValue)
                {
                    // the oracle's WriteExtensionFormatHeader pre-reserves header + payload and
                    // overflows here; compare against the spec bytes directly
                    Assert.Equal((byte[])[0xc9, 0x7f, 0xff, 0xff, 0xff, unchecked((byte)typeCode)], ours);
                    continue;
                }
                var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.WriteExtensionFormatHeader(new MessagePack.ExtensionHeader(typeCode, length)));
                Assert.True(oracle.AsSpan().SequenceEqual(ours), $"ext header mismatch type={typeCode} length={length}: ours=[{Convert.ToHexString(ours)}] oracle=[{Convert.ToHexString(oracle)}]");
            }
        }
    }

    [Fact]
    public void Timestamp_MatchOracle()
    {
        var epoch = DateTime.UnixEpoch;
        DateTime[] values =
        [
            epoch,                                            // timestamp32 zero
            epoch.AddSeconds(1),
            epoch.AddSeconds(uint.MaxValue),                  // timestamp32 upper bound (2106)
            epoch.AddSeconds((long)uint.MaxValue + 1),        // -> timestamp64
            epoch.AddTicks(1),                                // ns != 0 -> timestamp64
            epoch.AddSeconds(uint.MaxValue).AddTicks(9_999_999),
            epoch.AddSeconds(0x3_FFFF_FFFFL),                 // 34-bit seconds upper bound (~2514)
            epoch.AddSeconds(0x4_0000_0000L),                 // -> timestamp96
            epoch.AddSeconds(-1),                             // pre-epoch -> timestamp96
            new DateTime(1912, 7, 30, 12, 34, 56, DateTimeKind.Utc),
            DateTime.MinValue,
            DateTime.MaxValue,
            new DateTime(2026, 7, 15, 1, 2, 3, DateTimeKind.Unspecified),
            DateTime.SpecifyKind(new DateTime(2026, 7, 15, 1, 2, 3), DateTimeKind.Local), // converts to UTC
        ];
        foreach (var v in values)
        {
            var oracle = OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v));
            var ours = Ours(b => MessagePackPrimitives.UnsafeWriteTimestamp(ref MemoryMarshal.GetArrayDataReference(b), v));
            Assert.True(oracle.AsSpan().SequenceEqual(ours), $"timestamp mismatch {v:O} kind={v.Kind}: ours=[{Convert.ToHexString(ours)}] oracle=[{Convert.ToHexString(oracle)}]");
        }
    }

    [Fact]
    public void Char_MatchOracle()
    {
        foreach (var v in (char[])['\0', 'a', '', '', 'ÿ', 'Ā', 'あ', '￿'])
        {
            Assert.Equal(OracleBytes((ref MessagePack.MessagePackWriter w) => w.Write(v)), Ours(b => MessagePackPrimitives.UnsafeWriteChar(ref MemoryMarshal.GetArrayDataReference(b), v)));
        }
    }

    delegate void OracleWrite(ref MessagePack.MessagePackWriter writer);

    static byte[] OracleBytes(OracleWrite write)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePack.MessagePackWriter(output);
        write(ref writer);
        writer.Flush();
        return output.WrittenSpan.ToArray();
    }
}

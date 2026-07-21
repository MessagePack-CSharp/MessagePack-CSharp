using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

public class PrimitiveTests
{
    static IEnumerable<int> Int32TestValues()
    {
        int[] boundaries = [0, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue];
        foreach (var b in boundaries)
        {
            for (int d = -2; d <= 2; d++)
            {
                yield return unchecked(b + d);
            }
        }
        var rand = new Random(42);
        for (int i = 0; i < 100_000; i++)
        {
            yield return rand.Next(int.MinValue, int.MaxValue);
        }
    }

    static IEnumerable<long> Int64TestValues()
    {
        long[] boundaries = [0, 127, 128, 255, 256, 65535, 65536, uint.MaxValue, (long)uint.MaxValue + 1,
            int.MaxValue, long.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue, (long)int.MinValue - 1, long.MinValue];
        foreach (var b in boundaries)
        {
            for (long d = -2; d <= 2; d++)
            {
                yield return unchecked(b + d);
            }
        }
        var rand = new Random(42);
        for (int i = 0; i < 100_000; i++)
        {
            yield return rand.NextInt64(long.MinValue, long.MaxValue);
        }
    }

    [Fact]
    public void Int32_BytesMatchOracle_AndRoundtrip()
    {
        foreach (var v in Int32TestValues())
        {
            var ours = MessagePackSerializer.Default.Serialize(v);
            var oracle = Oracle.Serialize(v);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch for {v}: ours=[{Convert.ToHexString(ours)}] oracle=[{Convert.ToHexString(oracle)}]");
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<int>(ours));
            Assert.Equal(v, Oracle.Deserialize<int>(ours)); // oracle can read ours
        }
    }

    [Fact]
    public void Int64_BytesMatchOracle_AndRoundtrip()
    {
        foreach (var v in Int64TestValues())
        {
            var ours = MessagePackSerializer.Default.Serialize(v);
            var oracle = Oracle.Serialize(v);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch for {v}: ours=[{Convert.ToHexString(ours)}] oracle=[{Convert.ToHexString(oracle)}]");
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<long>(ours));
            Assert.Equal(v, Oracle.Deserialize<long>(ours));
        }
    }

    [Fact]
    public void UInt64_BytesMatchOracle_AndRoundtrip()
    {
        ulong[] values = [0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue, (ulong)uint.MaxValue + 1, long.MaxValue, (ulong)long.MaxValue + 1, ulong.MaxValue];
        foreach (var v in values)
        {
            var ours = MessagePackSerializer.Default.Serialize(v);
            var oracle = Oracle.Serialize(v);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch for {v}");
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<ulong>(ours));
        }
    }

    [Fact]
    public void SmallIntegerTypes_Roundtrip()
    {
        foreach (short v in (short[])[0, 1, -1, short.MaxValue, short.MinValue, 127, -32, 128, -33])
        {
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<short>(MessagePackSerializer.Default.Serialize(v)));
        }
        foreach (byte v in (byte[])[0, 1, 127, 128, 255])
        {
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<byte>(MessagePackSerializer.Default.Serialize(v)));
        }
        foreach (sbyte v in (sbyte[])[0, 1, -1, -32, -33, sbyte.MaxValue, sbyte.MinValue])
        {
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<sbyte>(MessagePackSerializer.Default.Serialize(v)));
        }
    }

    [Fact]
    public void Bool_Double_Single_MatchOracle()
    {
        foreach (var v in (bool[])[true, false])
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Default.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<bool>(MessagePackSerializer.Default.Serialize(v)));
        }
        foreach (var v in (double[])[0, 1.5, -1.5, double.MaxValue, double.MinValue, double.Epsilon, double.NaN, double.PositiveInfinity, Math.PI])
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Default.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<double>(MessagePackSerializer.Default.Serialize(v)));
        }
        foreach (var v in (float[])[0f, 1.5f, -1.5f, float.MaxValue, float.NaN, MathF.PI])
        {
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<float>(MessagePackSerializer.Default.Serialize(v)));
        }
    }

    [Fact]
    public void String_BytesMatchOracle_AndRoundtrip()
    {
        string?[] values =
        [
            null, "", "a", "hello", "1234567890", "12345678901",
            new string('x', 31), new string('x', 32), new string('x', 255), new string('x', 256),
            new string('x', 65535), new string('x', 65536),
            "こんにちは世界", "日本語まじり English text", "絵文字🎉🚀テスト", "サロゲート𠮷野家",
        ];
        foreach (var v in values)
        {
            var ours = MessagePackSerializer.Default.Serialize(v);
            var oracle = Oracle.Serialize(v);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch for '{v?[..Math.Min(v.Length, 20)]}'");
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<string?>(ours));
        }
    }

    [Fact]
    public void String_ClassStraddleZones_MatchOracle()
    {
        // the single-pass writer guesses the header class from char count; non-ASCII in the
        // straddle zones (L in 11..31, 86..255, 21846..65535) triggers the payload-move path.
        // cover every zone edge with 1-byte, 3-byte, and mixed-width chars
        int[] lengths = [10, 11, 20, 31, 32, 85, 86, 100, 255, 256, 300, 21845, 21846, 30000, 65535, 65536, 70000];
        foreach (var len in lengths)
        {
            foreach (var content in (Func<int, string>[])
            [
                n => new string('x', n),                     // ASCII: guess always exact
                n => new string('あ', n),                    // 3-byte chars: worst-case expansion
                n => string.Concat(Enumerable.Repeat("aあ", n / 2 + 1))[..n], // mixed widths
            ])
            {
                var s = content(len);
                var ours = MessagePackSerializer.Default.Serialize(s);
                var oracle = Oracle.Serialize(s);
                Assert.True(ours.AsSpan().SequenceEqual(oracle), $"len={len} bytes mismatch (first char '{s[0]}')");
                Assert.Equal(s, MessagePackSerializer.Default.Deserialize<string>(ours));
            }
        }
    }

    [Fact]
    public void WideFormats_AcceptedWhenInRange()
    {
        // handcrafted non-minimal encodings: int64(d3)/uint64(cf) holding small values
        Span<byte> buf = stackalloc byte[9];
        foreach (var v in (long[])[0, 1, -1, 42, int.MaxValue, int.MinValue])
        {
            buf[0] = 0xd3;
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(buf[1..], v);
            Assert.Equal((int)v, MessagePackSerializer.Default.Deserialize<int>(buf));
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<long>(buf));
        }
        buf[0] = 0xcf;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(buf[1..], 42);
        Assert.Equal(42, MessagePackSerializer.Default.Deserialize<int>(buf));

        // uint64 above long.MaxValue: the sign-alias trap (0xFF.. reinterprets as -1)
        buf[0] = 0xcf;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(buf[1..], ulong.MaxValue);
        var trap = buf.ToArray();
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Default.Deserialize<int>(trap));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Default.Deserialize<long>(trap));
        Assert.Equal(ulong.MaxValue, MessagePackSerializer.Default.Deserialize<ulong>(trap));

        // int64 out of int32 range must fail as int
        buf[0] = 0xd3;
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(buf[1..], 2147483648L);
        var over = buf.ToArray();
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Default.Deserialize<int>(over));
        Assert.Equal(2147483648L, MessagePackSerializer.Default.Deserialize<long>(over));
    }
}

using System.Numerics;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// NumericFormatters.cs (System.Numerics scalars): every case is wire-compared against
// MessagePack v3 AND roundtripped through our own reader.
public class NumericFormatterTests
{
    static void AssertOracleAndRoundtrip<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours));
    }

    [Fact]
    public void BigInteger_MatchOracleAndRoundtrip()
    {
        BigInteger[] values =
        [
            BigInteger.Zero, BigInteger.One, BigInteger.MinusOne,
            new BigInteger(long.MaxValue), new BigInteger(long.MinValue),
            BigInteger.Parse("123456789012345678901234567890"),
            BigInteger.Parse("-98765432109876543210987654321"),
            BigInteger.Pow(2, 3000),  // 376 bytes: exceeds bin8, exercises the array route
            -BigInteger.Pow(2, 3000),
        ];
        foreach (var value in values)
        {
            AssertOracleAndRoundtrip(value);
        }
        AssertOracleAndRoundtrip((BigInteger?)BigInteger.One);
        AssertOracleAndRoundtrip((BigInteger?)null);
    }

    [Fact]
    public void Complex_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(Complex.Zero);
        AssertOracleAndRoundtrip(new Complex(1.5, -2.5));
        AssertOracleAndRoundtrip(new Complex(double.MaxValue, double.Epsilon));
        AssertOracleAndRoundtrip((Complex?)null);
    }

    [Fact]
    public void VectorsAndMatrices_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(new Vector2(1.5f, -2.5f));
        AssertOracleAndRoundtrip(new Vector3(1f, 2f, 3f));
        AssertOracleAndRoundtrip(new Vector4(1f, 2f, 3f, 4f));
        AssertOracleAndRoundtrip(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f));
        // distinct element values catch any ordering mistake
        AssertOracleAndRoundtrip(new Matrix3x2(1f, 2f, 3f, 4f, 5f, 6f));
        AssertOracleAndRoundtrip(new Matrix4x4(
            1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f,
            9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f));
        AssertOracleAndRoundtrip(Matrix4x4.Identity);
        AssertOracleAndRoundtrip((Vector3?)new Vector3(1f, 2f, 3f));
        AssertOracleAndRoundtrip((Vector3?)null);
    }

    [Fact]
    public void SegmentStraddle_FallsBackToElementReaders()
    {
        // the fused fast path needs the whole constant shape in ONE window; a seam in the
        // middle of a float token must fall back to the stitch-aware element readers
        var v3 = new Vector3(1.5f, -2.5f, 3.25f);
        var bytes = MessagePackSerializer.Serialize(v3);
        for (int split = 1; split < bytes.Length; split++)
        {
            var sequence = Chain(bytes.AsSpan(0, split).ToArray(), bytes.AsSpan(split).ToArray());
            Assert.Equal(v3, MessagePackSerializer.Deserialize<Vector3>(in sequence));
        }

        var m = new Matrix4x4(1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f);
        var mBytes = MessagePackSerializer.Serialize(m);
        var mSeq = Chain(mBytes.AsSpan(0, 41).ToArray(), mBytes.AsSpan(41).ToArray()); // split inside a token
        Assert.Equal(m, MessagePackSerializer.Deserialize<Matrix4x4>(in mSeq));
    }

    sealed class Segment : System.Buffers.ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static System.Buffers.ReadOnlySequence<byte> Chain(params byte[][] segments)
    {
        var first = new Segment(segments[0]);
        var last = first;
        for (var i = 1; i < segments.Length; i++)
        {
            last = last.Append(segments[i]);
        }
        return new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Fact]
    public void MalformedCounts_SurfaceAsSerializationException()
    {
        var three = MessagePackSerializer.Serialize((1, 2, 3));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Vector2>(three));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Complex>(three));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Matrix4x4>(three));
    }
}

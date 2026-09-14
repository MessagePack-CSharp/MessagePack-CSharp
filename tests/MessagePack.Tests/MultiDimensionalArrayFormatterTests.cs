extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Multi-dimensional arrays (MultiDimensionalArrayFormatters.cs), v3-parity wire format:
// [len0, ..., len(rank-1), [elements in row-major order]] — wire-compared against the
// oracle and roundtripped with dimension + content assertions.
public class MultiDimensionalArrayFormatterTests
{
    static byte[] AssertOracle<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        return ours;
    }

    [Fact]
    public void TwoDimensional_MatchesOracleAndRoundtrips()
    {
        var value = new int[2, 3] { { 1, 2, 3 }, { 4, 5, -6 } };
        var back = MessagePackSerializer.Deserialize<int[,]>(AssertOracle(value))!;

        Assert.Equal(2, back.GetLength(0));
        Assert.Equal(3, back.GetLength(1));
        Assert.Equal(value, back); // multi-dim arrays compare rank+dims+elements structurally in xunit
    }

    [Fact]
    public void ThreeDimensional_MatchesOracleAndRoundtrips()
    {
        var value = new int[2, 2, 2] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } };
        var back = MessagePackSerializer.Deserialize<int[,,]>(AssertOracle(value))!;
        Assert.Equal(value, back);
    }

    [Fact]
    public void FourDimensional_MatchesOracleAndRoundtrips()
    {
        var value = new string[2, 1, 2, 1];
        value[0, 0, 0, 0] = "a";
        value[0, 0, 1, 0] = "b";
        value[1, 0, 0, 0] = "c";
        value[1, 0, 1, 0] = "d";
        var back = MessagePackSerializer.Deserialize<string[,,,]>(AssertOracle(value))!;
        Assert.Equal(value, back);
    }

    [Fact]
    public void ZeroLengthDimensions_Roundtrip()
    {
        var value = new int[0, 3];
        var back = MessagePackSerializer.Deserialize<int[,]>(AssertOracle(value))!;
        Assert.Equal(0, back.GetLength(0));
        Assert.Equal(3, back.GetLength(1));
    }

    [Fact]
    public void Null_Roundtrips()
    {
        Assert.Null(MessagePackSerializer.Deserialize<int[,]?>(MessagePackSerializer.Serialize<int[,]?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<int[,,]?>(MessagePackSerializer.Serialize<int[,,]?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<int[,,,]?>(MessagePackSerializer.Serialize<int[,,,]?>(null)));
    }

    [Fact]
    public void DimensionMismatch_Throws()
    {
        // craft [2, 2, [1, 2, 3]] — product says 4, element array says 3
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var w = new V3::MessagePack.MessagePackWriter(writer);
        w.WriteArrayHeader(3);
        w.Write(2);
        w.Write(2);
        w.WriteArrayHeader(3);
        w.Write(1);
        w.Write(2);
        w.Write(3);
        w.Flush();

        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[,]>(writer.WrittenSpan.ToArray()));
    }

    [Fact]
    public void JaggedArrays_StillResolveAsNestedRank1()
    {
        int[][] value = [[1, 2], [3]];
        var back = MessagePackSerializer.Deserialize<int[][]>(AssertOracle(value))!;
        Assert.Equal(value, back);
    }

    [Fact]
    public void OverflowingDimensionProduct_ThrowsSerializationException()
    {
        // len0 * len1 overflows int (2^16 * 2^16 == 2^32); the dimension-product check must
        // report a format error, not let a raw OverflowException escape the exception contract.
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var w = new V3::MessagePack.MessagePackWriter(writer);
        w.WriteArrayHeader(3);
        w.Write(1 << 16);
        w.Write(1 << 16);
        w.WriteArrayHeader(0); // any count: the product can never match, and must not throw OverflowException
        w.Flush();

        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[,]>(writer.WrittenSpan.ToArray()));
    }

    [Fact]
    public void OverflowingProductWithZeroDimension_RejectedAsUnconstructible()
    {
        // T[70000, 70000, 0] declares zero elements, but the runtime itself refuses to
        // construct an array whose dimension product overruns its limit ("Array dimensions
        // exceeded supported range"), so the write side could never have produced this shape.
        // It must fail as a format error, not reach `new T[...]` and throw a raw OOM.
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var w = new V3::MessagePack.MessagePackWriter(writer);
        w.WriteArrayHeader(4);
        w.Write(70000);
        w.Write(70000);
        w.Write(0);
        w.WriteArrayHeader(0);
        w.Flush();

        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<int[,,]>(writer.WrittenSpan.ToArray()));
    }

    [Fact]
    public void GenuineZeroDimension_Roundtrips()
    {
        // a real empty array with a zero dimension (small other dims) still round-trips
        foreach (var value in new[] { new int[0, 3], new int[3, 0] })
        {
            var back = MessagePackSerializer.Deserialize<int[,]>(MessagePackSerializer.Serialize(value))!;
            Assert.Equal(value.GetLength(0), back.GetLength(0));
            Assert.Equal(value.GetLength(1), back.GetLength(1));
            Assert.Equal(0, back.Length);
        }
    }
}

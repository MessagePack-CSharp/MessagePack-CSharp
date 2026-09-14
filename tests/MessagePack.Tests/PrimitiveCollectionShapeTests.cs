extern alias V3;
using SerializerFoundation;
using MessagePack.Formatters;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// PrimitiveCollectionFormatters.cs / PrimitiveElementCodecs.cs: every element type is
// wire-compared against MessagePack v3 across all five shapes (array / List / Memory /
// ReadOnlyMemory / ArraySegment) at SIMD chunk-boundary sizes, and roundtripped through
// our own reader. Value pools cross every format-class edge of the type's ladder.
public class PrimitiveCollectionShapeTests
{
    static readonly int[] Counts = [0, 1, 15, 16, 17, 33, 64, 65, 100, 1000];

    static T[] Make<T>(int count, T[] pool)
    {
        var rand = new Random(42);
        var values = new T[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = pool[rand.Next(pool.Length)];
        }
        return values;
    }

    static readonly sbyte[] SBytePool = [0, 1, -1, -32, -33, 127, -128, 100, -100]; // -33..-128 are the 2-byte int8 stragglers
    static readonly int[] IntPool = [0, 1, -1, -32, -33, 127, 128, 255, 256, 65535, 65536, -32768, -32769, int.MaxValue, int.MinValue];
    static readonly short[] ShortPool = [0, 1, -1, -32, 127, -33, 128, 255, 256, -128, -129, short.MinValue, short.MaxValue];
    static readonly ushort[] UShortPool = [0, 1, 127, 128, 255, 256, ushort.MaxValue];
    static readonly uint[] UIntPool = [0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue];
    static readonly long[] LongPool = [0, 1, -1, -32, 127, int.MaxValue, int.MinValue, (long)int.MaxValue + 1, (long)int.MinValue - 1, long.MaxValue, long.MinValue, 5_000_000_000];
    static readonly ulong[] ULongPool = [0, 1, 127, 255, 65535, uint.MaxValue, (ulong)uint.MaxValue + 1, ulong.MaxValue];
    static readonly float[] FloatPool = [0f, -0f, 1.5f, -1.5f, float.MaxValue, float.MinValue, float.Epsilon, float.PositiveInfinity, float.NegativeInfinity, float.NaN, 12345.678f];
    static readonly double[] DoublePool = [0d, -0d, 1.5, -1.5, double.MaxValue, double.MinValue, double.Epsilon, double.PositiveInfinity, double.NegativeInfinity, double.NaN, 12345.6789];
    static readonly bool[] BoolPool = [true, false];

    static void AssertAllShapes<T>(T[] value)
    {
        var name = typeof(T).Name;

        var arrayBytes = MessagePackSerializer.Serialize(value);
        Assert.True(arrayBytes.AsSpan().SequenceEqual(Oracle.Serialize(value)), $"{name}[] bytes mismatch len={value.Length}");
        Assert.Equal(value, MessagePackSerializer.Deserialize<T[]>(arrayBytes));

        var list = new List<T>(value);
        var listBytes = MessagePackSerializer.Serialize(list);
        Assert.True(listBytes.AsSpan().SequenceEqual(Oracle.Serialize(list)), $"List<{name}> bytes mismatch len={value.Length}");
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<T>>(listBytes));

        var memory = new Memory<T>(value);
        var memoryBytes = MessagePackSerializer.Serialize(memory);
        Assert.True(memoryBytes.AsSpan().SequenceEqual(Oracle.Serialize(memory)), $"Memory<{name}> bytes mismatch len={value.Length}");
        Assert.Equal(value, MessagePackSerializer.Deserialize<Memory<T>>(memoryBytes).ToArray());

        var readOnlyMemory = new ReadOnlyMemory<T>(value);
        var romBytes = MessagePackSerializer.Serialize(readOnlyMemory);
        Assert.True(romBytes.AsSpan().SequenceEqual(Oracle.Serialize(readOnlyMemory)), $"ReadOnlyMemory<{name}> bytes mismatch len={value.Length}");
        Assert.Equal(value, MessagePackSerializer.Deserialize<ReadOnlyMemory<T>>(romBytes).ToArray());

        var segment = new ArraySegment<T>(value);
        var segmentBytes = MessagePackSerializer.Serialize(segment);
        Assert.True(segmentBytes.AsSpan().SequenceEqual(Oracle.Serialize(segment)), $"ArraySegment<{name}> bytes mismatch len={value.Length}");
        Assert.Equal(value.AsEnumerable(), MessagePackSerializer.Deserialize<ArraySegment<T>>(segmentBytes));
    }

    [Fact]
    public void SByteShapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, SBytePool));
        // homogeneous runs: all-fixint hits the verbatim-copy superlane end to end,
        // all-int8 keeps the scalar ladder honest
        foreach (var count in Counts) AssertAllShapes(Make(count, [(sbyte)0, 1, -1, -32, 127]));
        foreach (var count in Counts) AssertAllShapes(Make(count, [(sbyte)-33, -100, -128]));
    }

    // for each ladder type the mixed pool exercises the scalar chain, the all-fixint and
    // all-wide pools drive the homogeneous superlanes end to end
    [Fact]
    public void Int16Shapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, ShortPool));
        foreach (var count in Counts) AssertAllShapes(Make<short>(count, [0, 1, -1, -32, 127]));
        foreach (var count in Counts) AssertAllShapes(Make<short>(count, [256, -256, 300, -300, short.MinValue, short.MaxValue]));
    }

    [Fact]
    public void UInt16Shapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, UShortPool));
        foreach (var count in Counts) AssertAllShapes(Make<ushort>(count, [0, 1, 100, 127]));
        foreach (var count in Counts) AssertAllShapes(Make<ushort>(count, [256, 300, 40000, ushort.MaxValue]));
    }

    [Fact]
    public void UInt32Shapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, UIntPool));
        foreach (var count in Counts) AssertAllShapes(Make<uint>(count, [0, 1, 100, 127]));
        foreach (var count in Counts) AssertAllShapes(Make<uint>(count, [65536, 100_000, 3_000_000_000, uint.MaxValue]));
    }

    [Fact]
    public void Int64Shapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, LongPool));
        foreach (var count in Counts) AssertAllShapes(Make<long>(count, [0, 1, -1, -32, 127]));
        foreach (var count in Counts) AssertAllShapes(Make<long>(count, [5_000_000_000, -5_000_000_000, long.MinValue, long.MaxValue, (long)int.MaxValue + 1, (long)int.MinValue - 1]));
    }

    [Fact]
    public void UInt64Shapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, ULongPool));
        foreach (var count in Counts) AssertAllShapes(Make<ulong>(count, [0, 1, 100, 127]));
        foreach (var count in Counts) AssertAllShapes(Make<ulong>(count, [(ulong)uint.MaxValue + 1, 5_000_000_000, ulong.MaxValue]));
    }

    [Fact]
    public void SingleShapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, FloatPool));
    }

    [Fact]
    public void DoubleShapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, DoublePool));
    }

    [Fact]
    public void BooleanShapes_AllSizes_MatchOracleAndRoundtrip()
    {
        foreach (var count in Counts) AssertAllShapes(Make(count, BoolPool));
    }

    // The serialize region loop must survive its own seams: counts straddling each
    // codec's SerializeRegionElements force a second region whose written offset and
    // reservation restart from zero. (Counts above stops at 1000 and never gets there.)
    [Fact]
    public void RegionBoundaries_AllElementTypes_MatchOracleAndRoundtrip()
    {
        AssertRegionBoundaries(Int32ElementCodec.SerializeRegionElements, IntPool);
        AssertRegionBoundaries(SByteElementCodec.SerializeRegionElements, SBytePool);
        AssertRegionBoundaries(Int16ElementCodec.SerializeRegionElements, ShortPool);
        AssertRegionBoundaries(UInt16ElementCodec.SerializeRegionElements, UShortPool);
        AssertRegionBoundaries(UInt32ElementCodec.SerializeRegionElements, UIntPool);
        AssertRegionBoundaries(Int64ElementCodec.SerializeRegionElements, LongPool);
        AssertRegionBoundaries(UInt64ElementCodec.SerializeRegionElements, ULongPool);
        AssertRegionBoundaries(SingleElementCodec.SerializeRegionElements, FloatPool);
        AssertRegionBoundaries(DoubleElementCodec.SerializeRegionElements, DoublePool);
        AssertRegionBoundaries(BooleanElementCodec.SerializeRegionElements, BoolPool);
    }

    static void AssertRegionBoundaries<T>(int regionElements, T[] pool)
    {
        foreach (var count in (int[])[regionElements - 1, regionElements, regionElements + 1])
        {
            AssertAllShapes(Make(count, pool));
        }
    }

    // A fixint run breaking at every offset would only exercise int; for the fused
    // constant-stride decoders (float/double/bool) the analog is "one foreign token at
    // every offset within the 16-lane window" — the gate must fail and the scalar
    // reader must produce identical results.
    [Fact]
    public void FusedDecodeGates_BreakAtEveryOffset()
    {
        for (int breakAt = 0; breakAt < 48; breakAt++)
        {
            // float array with an int (foreign but readable) at one position: our reader
            // and v3 agree element-wise even though the fast gate falls to scalar
            var floats = new object[48];
            for (int i = 0; i < 48; i++) floats[i] = 1.5f;
            floats[breakAt] = 42; // serializes as fixint
            var payload = Oracle.Serialize(floats); // heterogeneous array via object[]
            var backFloats = MessagePackSerializer.Deserialize<float[]>(payload)!;
            Assert.Equal(Oracle.Deserialize<float[]>(payload), backFloats);

            // double analog: the VBMI decode gathers 7 tokens per 64B window (the scalar
            // fallback fuses 16), so a foreign token at any offset must fail the gate
            // and hand these elements to the stitch-aware scalar reader
            var doubles = new object[48];
            for (int i = 0; i < 48; i++) doubles[i] = 1.5;
            doubles[breakAt] = 42; // serializes as fixint
            var doublePayload = Oracle.Serialize(doubles);
            Assert.Equal(Oracle.Deserialize<double[]>(doublePayload), MessagePackSerializer.Deserialize<double[]>(doublePayload));

            var bools = new bool[48];
            var boolBytes = MessagePackSerializer.Serialize(bools);
            var headerLength = boolBytes.Length - bools.Length; // array16 here, but stay robust
            boolBytes[headerLength + breakAt] = 0xc3; // flip one lane true in-place: still canonical
            Assert.Equal(Oracle.Deserialize<bool[]>(boolBytes), MessagePackSerializer.Deserialize<bool[]>(boolBytes));
        }
    }

    sealed class Chunk : System.Buffers.ReadOnlySequenceSegment<byte>
    {
        public Chunk(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Chunk Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Chunk(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static void RoundtripChunked<T>(T[] expected, int chunkSize)
    {
        var bytes = MessagePackSerializer.Serialize(expected);
        var first = new Chunk(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var last = first;
        for (int i = chunkSize; i < bytes.Length; i += chunkSize)
        {
            last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
        }
        var seq = new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

        Assert.Equal(expected, MessagePackSerializer.Deserialize<T[]>(seq));
        Assert.Equal(expected.AsEnumerable(), MessagePackSerializer.Deserialize<List<T>>(seq));
    }

    // fused decoders need contiguous windows (80B float, 144B double, 16B bool); a seam
    // anywhere inside must fall to the stitch-aware scalar readers and then RESUME
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(16)]
    public void SequenceSegmentBoundaries_AllElementTypes_Roundtrip(int chunkSize)
    {
        RoundtripChunked(Make(500, SBytePool), chunkSize);
        RoundtripChunked(Make(500, ShortPool), chunkSize);
        RoundtripChunked(Make(500, UShortPool), chunkSize);
        RoundtripChunked(Make(500, UIntPool), chunkSize);
        RoundtripChunked(Make(500, LongPool), chunkSize);
        RoundtripChunked(Make(500, ULongPool), chunkSize);
        RoundtripChunked(Make(500, FloatPool), chunkSize);
        RoundtripChunked(Make(500, DoublePool), chunkSize);
        RoundtripChunked(Make(500, BoolPool), chunkSize);
    }

    [Fact]
    public void DoubleList_Populate_ReusesInstance()
    {
        var expected = Make(100, DoublePool);
        var bytes = MessagePackSerializer.Serialize(expected);

        var list = new List<double> { 1, 2, 3 };
        var before = list;
        MessagePackSerializer.Deserialize(bytes, ref list);
        Assert.Same(before, list);
        Assert.Equal(expected, list);

        MessagePackSerializer.Deserialize(MessagePackSerializer.Serialize(new[] { 5d }), ref list);
        Assert.Same(before, list);
        Assert.Equal([5d], list);
    }

    [Fact]
    public void Int16Views_WriteThroughOnExactLength()
    {
        var bytes = MessagePackSerializer.Serialize(new short[] { 7, 8 });

        var memoryBacking = new short[2];
        Memory<short> memory = memoryBacking;
        MessagePackSerializer.Deserialize(bytes, ref memory);
        Assert.Equal([7, 8], memoryBacking);

        var segmentBacking = new short[] { 1, 2, 3, 4 };
        var segment = new ArraySegment<short>(segmentBacking, 1, 2);
        MessagePackSerializer.Deserialize(bytes, ref segment);
        Assert.Equal([1, 7, 8, 4], segmentBacking);
    }

    [Fact]
    public void PrimitiveShapes_ResolveToTheSpecializedFormatters()
    {
        // a registration typo would silently fall through to the generic tier and still
        // pass the wire tests — pin the resolved formatter types (array shape per type,
        // spot-check the other shapes on one type)
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default);
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, sbyte, SByteElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, sbyte[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, short, Int16ElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, short[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ushort, UInt16ElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ushort[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, uint, UInt32ElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, uint[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, long, Int64ElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, long[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ulong, UInt64ElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ulong[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float, SingleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, double, DoubleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, double[]>());
        Assert.IsType<PrimitiveArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, bool, BooleanElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, bool[]>());

        Assert.IsType<PrimitiveListFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float, SingleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, List<float>>());
        // Memory/ReadOnlyMemory/ArraySegment have no v3 public names: the internal shape
        // templates are registered directly (visible here via InternalsVisibleTo)
        Assert.IsType<PrimitiveMemoryFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float, SingleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, Memory<float>>());
        Assert.IsType<PrimitiveReadOnlyMemoryFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float, SingleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ReadOnlyMemory<float>>());
        Assert.IsType<PrimitiveArraySegmentFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, float, SingleElementCodec>>(
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ArraySegment<float>>());
    }
}

using MessagePack;
using SerializerFoundation;
using System.Buffers.Binary;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack.Tests;

public class CollectionAndObjectTests
{
    static int[] MakeInts(int count, string distribution)
    {
        var rand = new Random(42);
        var values = new int[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = distribution switch
            {
                "small" => rand.Next(-32, 128),
                "large" => rand.Next(2) == 0 ? rand.Next(65536, int.MaxValue) : rand.Next(int.MinValue, -32768),
                _ => rand.Next(8) switch
                {
                    0 => rand.Next(0, 128),
                    1 => rand.Next(-32, 0),
                    2 => rand.Next(128, 256),
                    3 => rand.Next(-128, -32),
                    4 => rand.Next(256, 65536),
                    5 => rand.Next(-32768, -128),
                    6 => rand.Next(65536, int.MaxValue),
                    _ => rand.Next(int.MinValue, -32768),
                },
            };
        }
        return values;
    }

    // SIMD chunk boundaries: 0/1/15/16/17/31/32/33/63/64/65/100/1000, all three distributions.
    // "mixed" exercises the scalar-chunk fallback inside the SIMD loop.
    [Theory]
    [InlineData("small")]
    [InlineData("mixed")]
    [InlineData("large")]
    public void Int32Array_AllSizes_MatchOracleAndRoundtrip(string distribution)
    {
        foreach (var count in (int[])[0, 1, 15, 16, 17, 31, 32, 33, 63, 64, 65, 100, 1000, 100_000])
        {
            var value = MakeInts(count, distribution);
            var ours = MessagePackSerializer.Default.Serialize(value);
            var oracle = Oracle.Serialize(value);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch count={count} dist={distribution}");
            Assert.Equal(value, MessagePackSerializer.Default.Deserialize<int[]>(ours));
            Assert.Equal(value, Oracle.Deserialize<int[]>(ours));
            Assert.Equal(value, MessagePackSerializer.Default.Deserialize<int[]>(oracle));
        }
    }

    [Fact]
    public void Int32Array_SimdChunkEdges()
    {
        // a fixint run that turns non-fixint exactly at every offset within a 16-lane chunk
        for (int breakAt = 0; breakAt < 48; breakAt++)
        {
            var value = new int[48];
            for (int i = 0; i < value.Length; i++) value[i] = i % 100; // fixint range
            value[breakAt] = 100_000; // force a non-fixint lane
            var ours = MessagePackSerializer.Default.Serialize(value);
            Assert.Equal(Oracle.Serialize(value), ours);
            Assert.Equal(value, MessagePackSerializer.Default.Deserialize<int[]>(ours));
        }
    }

    [Fact]
    public void Int32Array_WideChunkEdges()
    {
        // the wide-superlane mirror of SimdChunkEdges: an all-wide run (5-byte tokens,
        // alternating int32/uint32 so both code bytes appear) broken by one narrow value
        // at every offset, plus boundary values around the wide thresholds
        for (int breakAt = 0; breakAt < 48; breakAt++)
        {
            var value = new int[48];
            for (int i = 0; i < value.Length; i++) value[i] = (i % 2 == 0) ? 100_000 + i : -100_000 - i;
            value[breakAt] = 7; // force a narrow lane
            var ours = MessagePackSerializer.Default.Serialize(value);
            Assert.Equal(Oracle.Serialize(value), ours);
            Assert.Equal(value, MessagePackSerializer.Default.Deserialize<int[]>(ours));
        }

        // deserialize wide-superlane gates: a uint32 token above int.MaxValue inside an
        // otherwise all-wide run must still throw (falls to the scalar reader), and
        // non-minimal int32 encodings of small values must still decode
        {
            var bytes = new byte[3 + 16 * 5];
            bytes[0] = 0xdc; bytes[1] = 0; bytes[2] = 16; // array16 header, 16 elements
            for (int t = 0; t < 16; t++)
            {
                bytes[3 + t * 5] = 0xce; // uint32
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(3 + t * 5 + 1), 100_000);
            }
            var wide = MessagePackSerializer.Default.Deserialize<int[]>(bytes)!;
            Assert.All(wide, v => Assert.Equal(100_000, v));

            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(3 + 7 * 5 + 1), 0x8000_0000); // > int.MaxValue
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Default.Deserialize<int[]>(bytes));

            for (int t = 0; t < 16; t++)
            {
                bytes[3 + t * 5] = 0xd2; // non-minimal int32 holding a small value
                BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(3 + t * 5 + 1), t - 8);
            }
            Assert.Equal(Oracle.Deserialize<int[]>(bytes), MessagePackSerializer.Default.Deserialize<int[]>(bytes));
        }

        // threshold values: wide starts strictly outside [-32768, 65535]
        var edges = new int[16]
        {
            -32769, 65536, int.MinValue, int.MaxValue,
            -32769, 65536, -40000, 70000,
            int.MinValue, int.MaxValue, -32769, 65536,
            -100, 65535, -32768, 0, // last quad narrow: kills the wide superlane
        };
        Assert.Equal(Oracle.Serialize(edges), MessagePackSerializer.Default.Serialize(edges));
        for (int j = 0; j < 12; j++) edges[12 + j % 4] = -32769; // now all wide
        Assert.Equal(Oracle.Serialize(edges), MessagePackSerializer.Default.Serialize(edges));
        Assert.Equal(edges, MessagePackSerializer.Default.Deserialize<int[]>(MessagePackSerializer.Default.Serialize(edges)));
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

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(64)]
    public void Int32Array_SequenceSegmentBoundaries_Roundtrip(int chunkSize)
    {
        // the SIMD deserialize loop needs 16 contiguous bytes; short windows at segment
        // boundaries must fall to the scalar chunk (which stitches) and then RESUME SIMD
        foreach (var distribution in (string[])["small", "mixed", "large"])
        {
            var expected = MakeInts(1000, distribution);
            var bytes = MessagePackSerializer.Default.Serialize(expected);

            var first = new Chunk(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
            var last = first;
            for (int i = chunkSize; i < bytes.Length; i += chunkSize)
            {
                last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
            }
            var seq = new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

            Assert.Equal(expected, MessagePackSerializer.Default.Deserialize<int[]>(seq));
        }
    }

    [Fact]
    public void NullArraysAndLists()
    {
        Assert.Equal(Oracle.Serialize<int[]?>(null), MessagePackSerializer.Default.Serialize<int[]?>(null));
        Assert.Null(MessagePackSerializer.Default.Deserialize<int[]?>(MessagePackSerializer.Default.Serialize<int[]?>(null)));
    }

    [Fact]
    public void GenericCollections_MatchOracleAndRoundtrip()
    {
        var list = new List<int> { 1, -1, 128, -129, 70000, int.MaxValue };
        Assert.Equal(Oracle.Serialize(list), MessagePackSerializer.Default.Serialize(list));
        Assert.Equal(list, MessagePackSerializer.Default.Deserialize<List<int>>(MessagePackSerializer.Default.Serialize(list)));

        var strings = new[] { "a", "こんにちは", "", "longer string value here" };
        Assert.Equal(Oracle.Serialize(strings), MessagePackSerializer.Default.Serialize(strings));
        Assert.Equal(strings, MessagePackSerializer.Default.Deserialize<string[]>(MessagePackSerializer.Default.Serialize(strings)));

        var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["big"] = 100000, ["neg"] = -50 };
        Assert.Equal(Oracle.Serialize(dict), MessagePackSerializer.Default.Serialize(dict));
        Assert.Equal(dict, MessagePackSerializer.Default.Deserialize<Dictionary<string, int>>(MessagePackSerializer.Default.Serialize(dict)));

        var longs = new long[] { 0, -1, long.MaxValue, long.MinValue, 5_000_000_000L };
        Assert.Equal(Oracle.Serialize(longs), MessagePackSerializer.Default.Serialize(longs));
        Assert.Equal(longs, MessagePackSerializer.Default.Deserialize<long[]>(MessagePackSerializer.Default.Serialize(longs)));

        int? nullable = 42;
        int? nothing = null;
        Assert.Equal(Oracle.Serialize(nullable), MessagePackSerializer.Default.Serialize(nullable));
        Assert.Equal(Oracle.Serialize(nothing), MessagePackSerializer.Default.Serialize(nothing));
        Assert.Equal(nullable, MessagePackSerializer.Default.Deserialize<int?>(MessagePackSerializer.Default.Serialize(nullable)));
        Assert.Null(MessagePackSerializer.Default.Deserialize<int?>(MessagePackSerializer.Default.Serialize(nothing)));
    }

    [Fact]
    public void Enums_MatchOracleAndRoundtrip()
    {
        // byte-backed: 255 crosses the fixint→uint8 format edge
        foreach (var v in new[] { ByteEnum.Small, ByteEnum.Edge, ByteEnum.Max })
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Default.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<ByteEnum>(MessagePackSerializer.Default.Serialize(v)));
        }

        // int-backed: negative values take the signed writer's int8/int16/int32 ladder
        foreach (var v in new[] { IntEnum.Negative, IntEnum.Zero, IntEnum.Big })
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Default.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Default.Deserialize<IntEnum>(MessagePackSerializer.Default.Serialize(v)));
        }

        // long-backed: value beyond int range
        Assert.Equal(Oracle.Serialize(LongEnum.Huge), MessagePackSerializer.Default.Serialize(LongEnum.Huge));
        Assert.Equal(LongEnum.Huge, MessagePackSerializer.Default.Deserialize<LongEnum>(MessagePackSerializer.Default.Serialize(LongEnum.Huge)));

        // nullable and collection composition resolve through the same factory chain
        ByteEnum? some = ByteEnum.Edge;
        ByteEnum? none = null;
        Assert.Equal(Oracle.Serialize(some), MessagePackSerializer.Default.Serialize(some));
        Assert.Equal(Oracle.Serialize(none), MessagePackSerializer.Default.Serialize(none));
        Assert.Equal(some, MessagePackSerializer.Default.Deserialize<ByteEnum?>(MessagePackSerializer.Default.Serialize(some)));
        Assert.Null(MessagePackSerializer.Default.Deserialize<ByteEnum?>(MessagePackSerializer.Default.Serialize(none)));

        var list = new List<IntEnum> { IntEnum.Negative, IntEnum.Zero, IntEnum.Big };
        Assert.Equal(Oracle.Serialize(list), MessagePackSerializer.Default.Serialize(list));
        Assert.Equal(list, MessagePackSerializer.Default.Deserialize<List<IntEnum>>(MessagePackSerializer.Default.Serialize(list)));
    }

    public enum ByteEnum : byte { Small = 3, Edge = 128, Max = 255 }
    public enum IntEnum { Negative = -70000, Zero = 0, Big = 1_000_000 }
    public enum LongEnum : long { Huge = 5_000_000_000L }

    [Fact]
    public void FactoryChainConstructor_ExactChainSemantics()
    {
        // no factories = the default chain
        var byDefault = new MessagePackSerializer();
        Assert.Equal(42, byDefault.Deserialize<int>(byDefault.Serialize(42)));

        // explicit chain including defaults works end to end
        var explicitChain = new MessagePackSerializer(PrimitiveFormatterFactory.Instance, GenericFormatterFactory.Instance);
        var list = new List<int> { 1, 2, 3 };
        Assert.Equal(list, explicitChain.Deserialize<List<int>>(explicitChain.Serialize(list)));

        // the chain is EXACT: primitives only, so List<int> resolves to Missing
        var primitivesOnly = new MessagePackSerializer(PrimitiveFormatterFactory.Instance);
        Assert.Equal(7, primitivesOnly.Deserialize<int>(primitivesOnly.Serialize(7)));
        Assert.Throws<InvalidOperationException>(() => primitivesOnly.Serialize(new List<int> { 1 }));
    }

    [Fact]
    public void Poco_MatchOracleAndRoundtrip()
    {
        DynamicFormatterFactory.Instance.RegisterFactory<Person>(new PersonFormatterFactory());

        var person = new Person { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        var ours = MessagePackSerializer.Default.Serialize(person);
        var oracle = Oracle.Serialize(person);
        Assert.Equal(oracle, ours);
        var back = MessagePackSerializer.Default.Deserialize<Person>(ours);
        Assert.Equal(person.Id, back.Id);
        Assert.Equal(person.Name, back.Name);
        Assert.Equal(person.Score, back.Score);
        var fromOracle = Oracle.Deserialize<Person>(ours);
        Assert.Equal(person.Name, fromOracle.Name);
    }
}

[MessagePackObject]
public class Person
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
}


public sealed class PersonFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Person>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref Person value)
    {
        buffer.WriteFixArrayHeader(3);
        buffer.WriteInt32(value.Id);
        buffer.WriteString(value.Name);
        buffer.WriteDouble(value.Score);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Person value)
    {
        var count = buffer.ReadArrayHeader();

        if (value == null)
        {
            value = new Person();
        }

        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0:
                    value.Id = buffer.ReadInt32();
                    break;
                case 1:
                    value.Name = buffer.ReadString();
                    break;
                case 2:
                    value.Score = buffer.ReadDouble();
                    break;
                default:
                    break;
            }
        }
    }
}

public sealed class PersonFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new PersonFormatter<TWriteBuffer, TReadBuffer>();
    }
}
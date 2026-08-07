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
            var ours = MessagePackSerializer.Serialize(value);
            var oracle = Oracle.Serialize(value);
            Assert.True(ours.AsSpan().SequenceEqual(oracle), $"bytes mismatch count={count} dist={distribution}");
            Assert.Equal(value, MessagePackSerializer.Deserialize<int[]>(ours));
            Assert.Equal(value, Oracle.Deserialize<int[]>(ours));
            Assert.Equal(value, MessagePackSerializer.Deserialize<int[]>(oracle));
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
            var ours = MessagePackSerializer.Serialize(value);
            Assert.Equal(Oracle.Serialize(value), ours);
            Assert.Equal(value, MessagePackSerializer.Deserialize<int[]>(ours));
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
            var ours = MessagePackSerializer.Serialize(value);
            Assert.Equal(Oracle.Serialize(value), ours);
            Assert.Equal(value, MessagePackSerializer.Deserialize<int[]>(ours));
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
            var wide = MessagePackSerializer.Deserialize<int[]>(bytes)!;
            Assert.All(wide, v => Assert.Equal(100_000, v));

            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(3 + 7 * 5 + 1), 0x8000_0000); // > int.MaxValue
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<int[]>(bytes));

            for (int t = 0; t < 16; t++)
            {
                bytes[3 + t * 5] = 0xd2; // non-minimal int32 holding a small value
                BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(3 + t * 5 + 1), t - 8);
            }
            Assert.Equal(Oracle.Deserialize<int[]>(bytes), MessagePackSerializer.Deserialize<int[]>(bytes));
        }

        // threshold values: wide starts strictly outside [-32768, 65535]
        var edges = new int[16]
        {
            -32769, 65536, int.MinValue, int.MaxValue,
            -32769, 65536, -40000, 70000,
            int.MinValue, int.MaxValue, -32769, 65536,
            -100, 65535, -32768, 0, // last quad narrow: kills the wide superlane
        };
        Assert.Equal(Oracle.Serialize(edges), MessagePackSerializer.Serialize(edges));
        for (int j = 0; j < 12; j++) edges[12 + j % 4] = -32769; // now all wide
        Assert.Equal(Oracle.Serialize(edges), MessagePackSerializer.Serialize(edges));
        Assert.Equal(edges, MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(edges)));
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
            var bytes = MessagePackSerializer.Serialize(expected);

            var first = new Chunk(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
            var last = first;
            for (int i = chunkSize; i < bytes.Length; i += chunkSize)
            {
                last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
            }
            var seq = new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

            Assert.Equal(expected, MessagePackSerializer.Deserialize<int[]>(seq));
        }
    }

    [Fact]
    public void NullArraysAndLists()
    {
        Assert.Equal(Oracle.Serialize<int[]?>(null), MessagePackSerializer.Serialize<int[]?>(null));
        Assert.Null(MessagePackSerializer.Deserialize<int[]?>(MessagePackSerializer.Serialize<int[]?>(null)));
    }

    [Fact]
    public void GenericCollections_MatchOracleAndRoundtrip()
    {
        var list = new List<int> { 1, -1, 128, -129, 70000, int.MaxValue };
        Assert.Equal(Oracle.Serialize(list), MessagePackSerializer.Serialize(list));
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<int>>(MessagePackSerializer.Serialize(list)));

        var strings = new[] { "a", "こんにちは", "", "longer string value here" };
        Assert.Equal(Oracle.Serialize(strings), MessagePackSerializer.Serialize(strings));
        Assert.Equal(strings, MessagePackSerializer.Deserialize<string[]>(MessagePackSerializer.Serialize(strings)));

        var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["big"] = 100000, ["neg"] = -50 };
        Assert.Equal(Oracle.Serialize(dict), MessagePackSerializer.Serialize(dict));
        Assert.Equal(dict, MessagePackSerializer.Deserialize<Dictionary<string, int>>(MessagePackSerializer.Serialize(dict)));

        var longs = new long[] { 0, -1, long.MaxValue, long.MinValue, 5_000_000_000L };
        Assert.Equal(Oracle.Serialize(longs), MessagePackSerializer.Serialize(longs));
        Assert.Equal(longs, MessagePackSerializer.Deserialize<long[]>(MessagePackSerializer.Serialize(longs)));

        int? nullable = 42;
        int? nothing = null;
        Assert.Equal(Oracle.Serialize(nullable), MessagePackSerializer.Serialize(nullable));
        Assert.Equal(Oracle.Serialize(nothing), MessagePackSerializer.Serialize(nothing));
        Assert.Equal(nullable, MessagePackSerializer.Deserialize<int?>(MessagePackSerializer.Serialize(nullable)));
        Assert.Null(MessagePackSerializer.Deserialize<int?>(MessagePackSerializer.Serialize(nothing)));
    }

    [Fact]
    public void Enums_MatchOracleAndRoundtrip()
    {
        // byte-backed: 255 crosses the fixint→uint8 format edge
        foreach (var v in new[] { ByteEnum.Small, ByteEnum.Edge, ByteEnum.Max })
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Deserialize<ByteEnum>(MessagePackSerializer.Serialize(v)));
        }

        // int-backed: negative values take the signed writer's int8/int16/int32 ladder
        foreach (var v in new[] { IntEnum.Negative, IntEnum.Zero, IntEnum.Big })
        {
            Assert.Equal(Oracle.Serialize(v), MessagePackSerializer.Serialize(v));
            Assert.Equal(v, MessagePackSerializer.Deserialize<IntEnum>(MessagePackSerializer.Serialize(v)));
        }

        // long-backed: value beyond int range
        Assert.Equal(Oracle.Serialize(LongEnum.Huge), MessagePackSerializer.Serialize(LongEnum.Huge));
        Assert.Equal(LongEnum.Huge, MessagePackSerializer.Deserialize<LongEnum>(MessagePackSerializer.Serialize(LongEnum.Huge)));

        // nullable and collection composition resolve through the same factory chain
        ByteEnum? some = ByteEnum.Edge;
        ByteEnum? none = null;
        Assert.Equal(Oracle.Serialize(some), MessagePackSerializer.Serialize(some));
        Assert.Equal(Oracle.Serialize(none), MessagePackSerializer.Serialize(none));
        Assert.Equal(some, MessagePackSerializer.Deserialize<ByteEnum?>(MessagePackSerializer.Serialize(some)));
        Assert.Null(MessagePackSerializer.Deserialize<ByteEnum?>(MessagePackSerializer.Serialize(none)));

        var list = new List<IntEnum> { IntEnum.Negative, IntEnum.Zero, IntEnum.Big };
        Assert.Equal(Oracle.Serialize(list), MessagePackSerializer.Serialize(list));
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<IntEnum>>(MessagePackSerializer.Serialize(list)));
    }

    public enum ByteEnum : byte { Small = 3, Edge = 128, Max = 255 }
    public enum IntEnum { Negative = -70000, Zero = 0, Big = 1_000_000 }
    public enum LongEnum : long { Huge = 5_000_000_000L }

    [Fact]
    public void FactoryChainConstructor_ExactChainSemantics()
    {
        // the shared default options round-trip over the default chain
        var byDefault = MessagePackSerializerOptions.Default;
        Assert.Equal(42, MessagePackSerializer.Deserialize<int>(MessagePackSerializer.Serialize(42, byDefault), byDefault));

        // explicit chain including defaults works end to end
        var explicitChain = new MessagePackSerializerOptions([BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]);
        var list = new List<int> { 1, 2, 3 };
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<int>>(MessagePackSerializer.Serialize(list, explicitChain), explicitChain));

        // the chain is EXACT: primitives only, so List<int> resolves to Missing
        var primitivesOnly = new MessagePackSerializerOptions([BuiltInFormatterFactory.Instance]);
        Assert.Equal(7, MessagePackSerializer.Deserialize<int>(MessagePackSerializer.Serialize(7, primitivesOnly), primitivesOnly));
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize(new List<int> { 1 }, primitivesOnly));
    }

    [Fact]
    public void Poco_MatchOracleAndRoundtrip()
    {
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<Person>(new PersonFormatterFactory());

        var person = new Person { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        var ours = MessagePackSerializer.Serialize(person);
        var oracle = Oracle.Serialize(person);
        Assert.Equal(oracle, ours);
        var back = MessagePackSerializer.Deserialize<Person>(ours);
        Assert.Equal(person.Id, back.Id);
        Assert.Equal(person.Name, back.Name);
        Assert.Equal(person.Score, back.Score);
        var fromOracle = Oracle.Deserialize<Person>(ours);
        Assert.Equal(person.Name, fromOracle.Name);
    }

    [Fact]
    public void Array_Populate_ReusesOnExactLengthOnly()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { "x", "yy" });

        // length match: same instance, elements overwritten
        var target = new[] { "a", "b" };
        var original = target;
        MessagePackSerializer.Deserialize(ref target, bytes);
        Assert.Same(original, target);
        Assert.Equal(new[] { "x", "yy" }, target);

        // length mismatch: fresh exact-size array
        var mismatched = new[] { "a" };
        var before = mismatched;
        MessagePackSerializer.Deserialize(ref mismatched, bytes);
        Assert.NotSame(before, mismatched);
        Assert.Equal(new[] { "x", "yy" }, mismatched);
    }

    [Fact]
    public void List_Populate_ReusesInstance_GrowsAndShrinks()
    {
        var bytes = MessagePackSerializer.Serialize(new List<string> { "x", "yy" });

        // reuse: same instance, shorter incoming grows to payload size
        var target = new List<string> { "a" };
        var original = target;
        MessagePackSerializer.Deserialize(ref target, bytes);
        Assert.Same(original, target);
        Assert.Equal(["x", "yy"], target);

        // longer incoming shrinks to payload size
        var longer = new List<string> { "a", "b", "c" };
        MessagePackSerializer.Deserialize(ref longer, bytes);
        Assert.Equal(["x", "yy"], longer);

        // null incoming: fresh list
        List<string>? fresh = null;
        MessagePackSerializer.Deserialize(ref fresh, bytes);
        Assert.Equal(["x", "yy"], fresh);
    }

    [Fact]
    public void Array_CovariantInstance_SerializeAccepts_PopulateReuseThrows()
    {
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<Person>(new PersonFormatterFactory());

        // Serialize reads elements with plain loads (deliberately NO AsSpan, which would
        // throw here): a U[] behind a T[] must serialize byte-identically to the oracle
        Person[] covariant = new DerivedPerson[] { new() { Id = 1, Name = "n", Score = 2.5 } };
        var bytes = MessagePackSerializer.Serialize(covariant);
        Assert.Equal(Oracle.Serialize(covariant), bytes);

        // Populate into a length-matching covariant array hits AsSpan's exact-type check.
        // Spec'd to throw (populate is new in v4, no compat constraint; see ArrayFormatter)
        Person[] target = new DerivedPerson[1];
        Assert.Throws<ArrayTypeMismatchException>(() => MessagePackSerializer.Deserialize(ref target, bytes));
    }
}

[MessagePackObject]
public class Person
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
}

public class DerivedPerson : Person { }


public sealed class PersonFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Person>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Person value)
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

public sealed partial class PersonFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new PersonFormatter<TWriteBuffer, TReadBuffer>();
    }
}
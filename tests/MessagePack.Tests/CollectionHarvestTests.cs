extern alias V3;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using MessagePack;
using SerializerFoundation;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// BCL collection/wrapper closed forms reachable from the member graph (List<string>,
// string[], Dictionary<K,V>, tuples, custom ICollection<T> shapes, ...) are harvested by
// the source generator and registered as static constructions, so the AOT chain
// (SourceGenerated + BuiltIn, no GenericFormatterFactory) resolves them. The wire must be
// identical to the Default chain's GenericFormatterFactory result and to the v3 oracle.
public class CollectionHarvestTests
{
    static HarvestCollectionPoco CreateValue() => new()
    {
        Tags = ["msgpack", "sandbox"],
        Scores = new Dictionary<string, int> { ["a"] = 1, ["b"] = 300 },
        Names = ["x", "yy"],
        Unique = ["one", "two"],
        Nested = [[1, 2], [300]],
        Pair = new KeyValuePair<int, string>(7, "kv"),
        Point = (42, "tuple"),
        Chars = ['a', 'あ'],
        Ints = [1, -70000, int.MaxValue],
        Custom = new HarvestCustomList { "c1", "c2" },
        Immutable = [10, 20],
    };

    [Fact]
    public void DefaultAotMatchesDefaultAndOracle()
    {
        var value = CreateValue();
        var aot = MessagePack.MessagePackSerializerOptions.DefaultAot;
        var bytes = V4.Serialize(value, aot);
        Assert.Equal(V4.Serialize(value), bytes);
        Assert.Equal(Oracle.Serialize(value), bytes);

        var back = V4.Deserialize<HarvestCollectionPoco>(bytes, aot)!;
        Assert.Equal(value.Tags, back.Tags);
        Assert.Equal(value.Scores, back.Scores);
        Assert.Equal(value.Names, back.Names);
        Assert.Equal(value.Unique, back.Unique);
        Assert.Equal(value.Nested, back.Nested);
        Assert.Equal(value.Pair, back.Pair);
        Assert.Equal(value.Point, back.Point);
        Assert.Equal(value.Chars, back.Chars);
        Assert.Equal(value.Ints, back.Ints);
        Assert.Equal(value.Custom, back.Custom);
        Assert.Equal(value.Immutable, back.Immutable);
    }

    [Fact]
    public void DefaultAotNullMembersRoundtrip()
    {
        var aot = MessagePack.MessagePackSerializerOptions.DefaultAot;
        var back = V4.Deserialize<HarvestCollectionPoco>(V4.Serialize(new HarvestCollectionPoco(), aot), aot)!;
        Assert.Null(back.Tags);
        Assert.Null(back.Scores);
        Assert.Null(back.Names);
        Assert.Null(back.Unique);
        Assert.Null(back.Nested);
        Assert.Null(back.Chars);
        Assert.Null(back.Ints);
        Assert.Null(back.Custom);
    }

    // List<primitive> stays on BuiltInFormatterFactory's codec-backed formatter: the
    // harvest must NOT register a plain ListFormatter over it (that would shadow the
    // faster closed form in every chain, AOT and JIT alike)
    [Fact]
    public void BuiltInServedInstantiationsAreNotShadowed()
    {
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot);
        var formatter = resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, List<int>>();
        Assert.Contains("PrimitiveListFormatter", formatter.GetType().Name);

        // ImmutableArray<primitive> likewise (HarvestCollectionPoco.Immutable reaches it from the member graph), while
        // ImmutableArray<byte> stays on the generic element loop
        Assert.Contains("PrimitiveImmutableArrayFormatter", resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ImmutableArray<int>>().GetType().Name);
        var byteFormatter = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default).GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ImmutableArray<byte>>();
        Assert.StartsWith("ImmutableArrayFormatter", byteFormatter.GetType().Name);
    }

    [Fact]
    public void ImmutableArrayOfPrimitives_MatchesOracle()
    {
        AssertImmutable<sbyte>([0, -1, -32, -33, 127, sbyte.MinValue]);
        AssertImmutable<int>([0, 1, -70000, int.MaxValue, int.MinValue, .. Enumerable.Range(-40, 100)]);
        AssertImmutable<short>([0, 128, -129, short.MaxValue, short.MinValue]);
        AssertImmutable<ushort>([0, 127, 128, ushort.MaxValue]);
        AssertImmutable<uint>([0, 127, 128, uint.MaxValue]);
        AssertImmutable<long>([0, -1, long.MaxValue, long.MinValue, .. Enumerable.Range(0, 40).Select(static i => (long)i << i)]);
        AssertImmutable<ulong>([0, 127, ulong.MaxValue]);
        AssertImmutable<float>([0, 1.5f, float.NaN, float.NegativeInfinity]);
        AssertImmutable<double>([0, 1.5, double.Epsilon, double.MaxValue]);
        AssertImmutable<bool>([true, false, .. Enumerable.Range(0, 40).Select(static i => i % 3 == 0)]);
        // the generic loop: an array of positive fixint/uint8 tokens, not bin; nothing here harvests it, so DefaultAot
        // has no formatter for it
        AssertImmutable<byte>([0, 1, 255], aot: false);

        static void AssertImmutable<T>(T[] items, bool aot = true)
        {
            foreach (var value in (ImmutableArray<T>[])[default, [], [.. items]])
            {
                foreach (var options in aot ? [MessagePackSerializerOptions.Default, MessagePackSerializerOptions.DefaultAot] : (MessagePackSerializerOptions[])[MessagePackSerializerOptions.Default])
                {
                    var bytes = V4.Serialize(value, options);
                    Assert.Equal(Oracle.Serialize(value), bytes);
                    var back = V4.Deserialize<ImmutableArray<T>>(bytes, options);
                    Assert.Equal(value.IsDefault, back.IsDefault);
                    if (!value.IsDefault)
                    {
                        Assert.Equal(value, back);
                    }
                    Assert.Equal(value.IsDefault, Oracle.Deserialize<ImmutableArray<T>>(bytes).IsDefault);
                }
            }
        }
    }
}

// the V3-aliased attributes serve both serializers (matched by full name)
[V3::MessagePack.MessagePackObject]
public class HarvestCollectionPoco
{
    [V3::MessagePack.Key(0)] public List<string>? Tags { get; set; }
    [V3::MessagePack.Key(1)] public Dictionary<string, int>? Scores { get; set; }
    [V3::MessagePack.Key(2)] public string[]? Names { get; set; }
    [V3::MessagePack.Key(3)] public HashSet<string>? Unique { get; set; }
    [V3::MessagePack.Key(4)] public List<List<int>>? Nested { get; set; }
    [V3::MessagePack.Key(5)] public KeyValuePair<int, string> Pair { get; set; }
    [V3::MessagePack.Key(6)] public (int, string) Point { get; set; }
    [V3::MessagePack.Key(7)] public char[]? Chars { get; set; }
    [V3::MessagePack.Key(8)] public List<int>? Ints { get; set; }
    [V3::MessagePack.Key(9)] public HarvestCustomList? Custom { get; set; }
    [V3::MessagePack.Key(10)] public ImmutableArray<int> Immutable { get; set; } = [];
}

public class HarvestCustomList : Collection<string>
{
}

using System.Buffers;
using System.Runtime.CompilerServices;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.NativeAot;

// The AOT-supported configuration is "explicit options over the source-generated factory +
// primitives". Everything below runs through that chain; the [RequiresDynamicCode] surface
// (options-less entries, MessagePackSerializerOptions.Default, GenericFormatterFactory) is
// deliberately never referenced, so `dotnet publish` must complete with zero IL warnings.
public class AotSerializationTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
    [
        MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
        BuiltInFormatterFactory.Instance,
    ]));

    // ---- known-answer vectors (hand-computed msgpack, oracle-compat formats) ----

    [Fact]
    public void IntKeyKnownAnswer()
    {
        // [0x93, 1, uint16 300, fixstr "abc"]
        var bytes = V4.Serialize(new AotIntKeyPoco { Id = 1, Count = 300, Name = "abc" }, Options);
        byte[] expected = [0x93, 0x01, 0xcd, 0x01, 0x2c, 0xa3, (byte)'a', (byte)'b', (byte)'c'];
        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void StringKeyKnownAnswer()
    {
        // fixmap2 {"id":1, "name":"ab"}
        var bytes = V4.Serialize(new AotStringKeyPoco { Id = 1, Name = "ab" }, Options);
        byte[] expected = [0x82, 0xa2, (byte)'i', (byte)'d', 0x01, 0xa4, (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0xa2, (byte)'a', (byte)'b'];
        Assert.Equal(expected, bytes);
    }

    // ---- roundtrips across the member-type matrix ----

    [Fact]
    public void IntFormatClassBoundaryRoundtrip()
    {
        foreach (var v in (int[])[0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue])
        {
            var back = V4.Deserialize<AotIntKeyPoco>(V4.Serialize(new AotIntKeyPoco { Id = v, Count = v, Name = null }, Options), Options)!;
            Assert.Equal(v, back.Id);
            Assert.Equal(v, back.Count);
            Assert.Null(back.Name);
        }
    }

    // nested poco + DateTime + byte[] + int[] + double/bool/long, including the >1KB-scratch
    // string (pooled segment growth) and a Japanese string (safe WriteString path)
    static AotNestedPoco CreateNestedValue() => new()
    {
        Inner = new AotIntKeyPoco { Id = 42, Count = -70000, Name = "山岡士郎" },
        Numbers = [1, 300, -70000, int.MaxValue, int.MinValue, 0],
        Stamp = new DateTime(2026, 7, 28, 1, 2, 3, 456, DateTimeKind.Utc),
        Blob = [0, 1, 2, 254, 255],
        Score = -1.5,
        Flag = true,
        Big = 5_000_000_000L,
        Long = new string('あ', 5000),
    };

    [Fact]
    public void NestedPocoRoundtrip()
    {
        var value = CreateNestedValue();
        var back = V4.Deserialize<AotNestedPoco>(V4.Serialize(value, Options), Options)!;
        Assert.Equal(42, back.Inner!.Id);
        Assert.Equal(-70000, back.Inner.Count);
        Assert.Equal("山岡士郎", back.Inner.Name);
        Assert.Equal(value.Numbers, back.Numbers);
        Assert.Equal(value.Stamp.Ticks, back.Stamp.Ticks);
        Assert.Equal(value.Blob, back.Blob);
        Assert.Equal(value.Score, back.Score);
        Assert.Equal(value.Flag, back.Flag);
        Assert.Equal(value.Big, back.Big);
        Assert.Equal(value.Long, back.Long);
    }

    [Fact]
    public void NullMembersRoundtrip()
    {
        var empty = V4.Deserialize<AotNestedPoco>(V4.Serialize(new AotNestedPoco(), Options), Options)!;
        Assert.Null(empty.Inner);
        Assert.Null(empty.Numbers);
        Assert.Null(empty.Blob);
        Assert.Null(empty.Long);
    }

    [Fact]
    public void UnknownMembersCaptureAndReplay()
    {
        // wide (keys 0,1,2) -> narrow (key 0 + packet) -> byte-identical replay, plus the
        // ToDictionary view: all statically reachable code, so it must survive ILC clean
        var bytes = V4.Serialize(new AotIntKeyPoco { Id = 7, Count = 300, Name = "ab" }, Options);
        var narrow = V4.Deserialize<AotNarrowIntKeyPoco>(bytes, Options)!;
        Assert.Equal(7, narrow.Id);
        Assert.Equal(2, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow, Options));
        Assert.True(narrow.Extra.IsArray);
        Assert.Equal("ab", narrow.Extra.ToArrayDictionary(Options)[2]);
    }

    [Fact]
    public void BufferWriterEntryByteIdentical()
    {
        var value = CreateNestedValue();
        var bytes = V4.Serialize(value, Options);
        var writer = new ArrayBufferWriter<byte>();
        V4.Serialize(writer, value, Options);
        Assert.True(writer.WrittenSpan.SequenceEqual(bytes));
    }

    [Fact]
    public void SequenceEntrySplitAtEveryByte()
    {
        var small = V4.Serialize(new AotIntKeyPoco { Id = 42, Count = -70000, Name = "山岡士郎" }, Options);
        for (int splitAt = 0; splitAt <= small.Length; splitAt++)
        {
            var sequence = splitAt == small.Length
                ? new ReadOnlySequence<byte>(small)
                : SplitSegment.CreateSplit(small, splitAt);
            var fromSequence = V4.Deserialize<AotIntKeyPoco>(sequence, Options)!;
            Assert.Equal(42, fromSequence.Id);
            Assert.Equal(-70000, fromSequence.Count);
            Assert.Equal("山岡士郎", fromSequence.Name);
        }
    }

    [Fact]
    public void PopulateOverloadReusesInstance()
    {
        var small = V4.Serialize(new AotIntKeyPoco { Id = 42, Count = -70000, Name = "山岡士郎" }, Options);
        var reusable = new AotIntKeyPoco();
        V4.Deserialize(small, ref reusable!, Options);
        Assert.Equal(42, reusable.Id);
        Assert.Equal("山岡士郎", reusable.Name);
    }

    // value-type T through the generic entry — the classic AOT instantiation case
    [Fact]
    public void StructPocoRoundtrip()
    {
        var back = V4.Deserialize<AotStructPoco>(V4.Serialize(new AotStructPoco { X = -129, Y = long.MaxValue }, Options), Options);
        Assert.Equal(-129, back.X);
        Assert.Equal(long.MaxValue, back.Y);
    }

    // constructor-driven deserialize: getter-only positional record members reach the
    // instance through the matched primary constructor
    [Fact]
    public void PositionalRecordConstructorRoundtrip()
    {
        var back = V4.Deserialize<AotCtorRecord>(V4.Serialize(new AotCtorRecord(7, "ctor"), Options), Options)!;
        Assert.Equal(7, back.Id);
        Assert.Equal("ctor", back.Name);
    }

    // init-only members through the construction path's object initializer
    [Fact]
    public void InitOnlyMembersRoundtrip()
    {
        var back = V4.Deserialize<AotInitPoco>(V4.Serialize(new AotInitPoco { A = 1, B = "init" }, Options), Options)!;
        Assert.Equal(1, back.A);
        Assert.Equal("init", back.B);
    }

    // [Union] polymorphism (static type-check chain, no reflection)
    [Fact]
    public void UnionRoundtrip()
    {
        var bytes = V4.Serialize<IAotShape>(new AotCircle { Radius = 2.5 }, Options);
        Assert.True(V4.Deserialize<IAotShape>(bytes, Options) is AotCircle { Radius: 2.5 });
        Assert.Equal([0xc0], V4.Serialize<IAotShape?>(null, Options));
    }

    [Fact]
    public void HarvestedEnumAndNullableRoundtrip()
    {
        // enums (own and BCL, byte- and int-backed) and Nullable<T> resolve through the
        // factory's static constructions — GenericFormatterFactory is not in this chain
        var holder = new AotEnumHolder
        {
            Color = AotColor.Green,
            MaybeColor = AotColor.Red,
            Day = DayOfWeek.Friday,
            MaybeCount = 300,
        };
        var back = V4.Deserialize<AotEnumHolder>(V4.Serialize(holder, Options), Options)!;
        Assert.Equal(AotColor.Green, back.Color);
        Assert.Equal(AotColor.Red, back.MaybeColor);
        Assert.Equal(DayOfWeek.Friday, back.Day);
        Assert.Equal(300, back.MaybeCount);

        var empty = V4.Deserialize<AotEnumHolder>(V4.Serialize(new AotEnumHolder(), Options), Options)!;
        Assert.Null(empty.MaybeColor);
        Assert.Null(empty.MaybeCount);
    }

    [Fact]
    public void HarvestedGenericInstantiationsRoundtrip()
    {
        // closed generics reachable from the member graph resolve through the factory's
        // static constructions — no MakeGenericType, so this works under ILC
        var holder = new AotGenericHolder
        {
            Numbers = new AotGenericBox<int> { Item = 42 },
            Words = new AotGenericBox<string> { Item = "aot" },
        };
        var back = V4.Deserialize<AotGenericHolder>(V4.Serialize(holder, Options), Options)!;
        Assert.Equal(42, back.Numbers!.Item);
        Assert.Equal("aot", back.Words!.Item);

        // the same closed instantiation as a serialization root rides the registry's
        // generic-definition fallback into the same static construction
        var root = V4.Deserialize<AotGenericBox<int>>(V4.Serialize(new AotGenericBox<int> { Item = 7 }, Options), Options)!;
        Assert.Equal(7, root.Item);
    }

    [Fact]
    public void HarvestedCollectionsRoundtrip()
    {
        // BCL collection closed forms reachable from the member graph resolve through the
        // factory's static constructions — GenericFormatterFactory is not in this chain
        var holder = new AotCollectionHolder
        {
            Tags = ["msgpack", "aot"],
            Scores = new Dictionary<string, int> { ["a"] = 1, ["b"] = 300 },
            Names = ["x", "yy"],
            Unique = ["one", "two"],
            Nested = [[1, 2], [300]],
            Point = (42, "tuple"),
            Custom = new AotCustomList { "c1", "c2" },
            Ints = [1, -70000, int.MaxValue],
        };
        var back = V4.Deserialize<AotCollectionHolder>(V4.Serialize(holder, Options), Options)!;
        Assert.Equal(holder.Tags, back.Tags);
        Assert.Equal(holder.Scores, back.Scores);
        Assert.Equal(holder.Names, back.Names);
        Assert.Equal(holder.Unique, back.Unique);
        Assert.Equal(holder.Nested, back.Nested);
        Assert.Equal(holder.Point, back.Point);
        Assert.Equal(holder.Custom, back.Custom);
        Assert.Equal(holder.Ints, back.Ints);

        var empty = V4.Deserialize<AotCollectionHolder>(V4.Serialize(new AotCollectionHolder(), Options), Options)!;
        Assert.Null(empty.Tags);
        Assert.Null(empty.Scores);
        Assert.Null(empty.Custom);
    }

    [Fact]
    public void SurrogateRoundtrip()
    {
        // the wire is the surrogate's [value, realm] array, and deserialization flows
        // through the constructor's validation
        var bytes = V4.Serialize(new AotUserId(42, "ilc"), Options);
        Assert.Equal(V4.Serialize(new AotUserIdSurrogate(42, "ilc"), Options), bytes);
        var back = V4.Deserialize<AotUserId>(bytes, Options)!;
        Assert.Equal(42, back.Value);
        Assert.Equal("ilc", back.Realm);
        Assert.Null(V4.Deserialize<AotUserId>(V4.Serialize<AotUserId?>(null, Options), Options));
    }

    [Fact]
    public void SerializableRootsRoundtrip()
    {
        // [MessagePackSerializable] roots resolve two ways under ILC: through the
        // module-initializer registration (DefaultAot options, registry-backed) and
        // through explicit composition of the generated Instance (registry-free)
        AotIntKeyPoco[] array = [new AotIntKeyPoco { Id = 1, Count = 300, Name = "root" }];
        var registryBack = V4.Deserialize<AotIntKeyPoco[]>(
            V4.Serialize(array, MessagePackSerializerOptions.DefaultAot), MessagePackSerializerOptions.DefaultAot)!;
        Assert.Equal(300, registryBack[0].Count);
        Assert.Equal("root", registryBack[0].Name);

        var composed = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            AotRootFactory.Instance,
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
        ]));
        List<AotStructPoco> list = [new AotStructPoco { X = -129, Y = long.MaxValue }];
        var composedBack = V4.Deserialize<List<AotStructPoco>>(V4.Serialize(list, composed), composed)!;
        Assert.Equal(-129, composedBack[0].X);
        Assert.Equal(long.MaxValue, composedBack[0].Y);
    }

    // circular-reference envelope + back-reference under ILC: the tracking tables and
    // ResolveCircularReference<T> are reflection-free, so the whole path must publish clean
    [Fact]
    public void CircularReferenceRoundtrip()
    {
        var node = new AotCircularNode { Value = 42 };
        node.Next = node;
        var back = V4.Deserialize<AotCircularNode>(V4.Serialize(node, Options), Options)!;
        Assert.Equal(42, back.Value);
        Assert.Same(back, back.Next);
    }

    [Fact]
    public void SerializationCallbacksInvoked()
    {
        var back = V4.Deserialize<AotCallbackPoco>(V4.Serialize(new AotCallbackPoco { Raw = 3 }, Options), Options)!;
        Assert.Equal(6, back.Normalized);
        Assert.True(back.Restored);
    }

    // generic [MessagePackObject]: the factory closes the open formatter over the runtime
    // type arguments via MakeGenericType. The buffer type parameters are ref structs, and
    // Native AOT cannot instantiate generics over byref-like types, so on AOT the factory
    // declines and the resolver's formatter-not-found error must surface — never a
    // TypeLoadException out of reflection.
    [Fact]
    public void GenericObjectFollowsDynamicCodeSupport()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            var back = V4.Deserialize<AotWrapper<int>>(V4.Serialize(new AotWrapper<int> { Value = 5, Count = 2 }, Options), Options)!;
            Assert.Equal(5, back.Value);
            Assert.Equal(2, back.Count);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => V4.Serialize(new AotWrapper<int> { Value = 5 }, Options));
        }
    }

    // unregistered type must fail with the resolver's exception, not an AOT crash
    // (List<int> no longer qualifies: primitive Lists are hardwired in BuiltIn since the
    // collection-family work; a List of a user type still needs GenericFormatterFactory)
    [Fact]
    public void UnregisteredTypeThrowsResolverError()
    {
        Assert.Throws<InvalidOperationException>(() => V4.Serialize(new List<AotIntKeyPoco>(), Options));
    }
}

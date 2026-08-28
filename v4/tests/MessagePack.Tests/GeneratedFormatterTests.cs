// MsgPack104: InterningStringFormatter here is itself a resolver-installed string customization, the thing the rule steers toward.
#pragma warning disable MsgPack104

extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Source-generator end-to-end: every type here gets its formatter generated at compile
// time and auto-registered via the emitted module initializer, so the default
// (options-less) V4.Serialize just works. The verification anchor is byte-exact equality against MessagePack-CSharp
// serializing the SAME attributed types with its own resolvers.
public class GeneratedFormatterTests
{
    static readonly DateTime Stamp = new DateTime(2026, 7, 21, 1, 2, 3, 456, DateTimeKind.Utc);

    static void AssertBytesAndRoundtrip<T>(T value)
    {
        var ours = V4.Serialize(value);
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, ours);

        // both directions decode each other's bytes
        var back = V4.Deserialize<T>(ours);
        Assert.Equal(Oracle.Serialize(back), oracle);
        var fromOracle = V4.Deserialize<T>(oracle);
        Assert.Equal(Oracle.Serialize(fromOracle), oracle);
    }

    // regression + the reason String left the generator's direct-write path: the emitted
    // formatter used to call buffer.WriteString / buffer.ReadString inline, so a
    // chain-supplied string formatter (interning, custom encoding) never reached a
    // [MessagePackObject] member — only standalone string serialization saw it
    [Fact]
    public void StringMember_RoutesThroughTheResolvedStringFormatter()
    {
        var options = new MessagePack.MessagePackSerializerOptions(new MessagePack.MessagePackFormatterResolver(
            [InterningStringFactory.Instance, MessagePackFormatterFactory.Default]));

        // built at runtime so the instance is not already the interned one
        var name = string.Concat("gen-", "interned-member-name");
        var value = new GenIntKeyPoco { Id = 1, Name = name, Score = 1.5, Flag = true, Ticks = 2 };

        // the interning formatter writes the default wire: bytes stay oracle-identical
        var bytes = V4.Serialize(value, options);
        Assert.Equal(Oracle.Serialize(value), bytes);

        var back = V4.Deserialize<GenIntKeyPoco>(bytes, options)!;
        Assert.Equal(name, back.Name);
        Assert.Same(string.Intern(name), back.Name); // proof the custom formatter ran

        // the default chain is untouched: it still decodes a fresh, un-interned string
        Assert.NotSame(string.Intern(name), V4.Deserialize<GenIntKeyPoco>(bytes)!.Name);
    }

    [Fact]
    public void IntKey_ContiguousKeys()
    {
        AssertBytesAndRoundtrip(new GenIntKeyPoco { Id = 42, Name = "山岡士郎", Score = 98.5, Flag = true, Ticks = 5_000_000_000L });
        AssertBytesAndRoundtrip(new GenIntKeyPoco { Id = -1, Name = null, Score = double.NaN, Flag = false, Ticks = long.MinValue });
        AssertBytesAndRoundtrip<GenIntKeyPoco?>(null);
    }

    [Fact]
    public void IntKey_HolesSerializeAsNil()
    {
        var value = new GenHolePoco { A = 100000, B = "b", C = 255 };
        AssertBytesAndRoundtrip(value);

        // key holes are nil slots: 8 array entries for max key 7
        var bytes = V4.Serialize(value);
        Assert.Equal(0x98, bytes[0]); // fixarray(8)
        Assert.Equal(0xc0, bytes[1]); // hole at key 0
    }

    [Fact]
    public void StringKey_ExplicitAndAutoKeys()
    {
        AssertBytesAndRoundtrip(new GenStringKeyPoco { Id = 7, Name = "name", F = 1.25f });
        AssertBytesAndRoundtrip(new GenStringKeyPoco { Id = 0, Name = null, F = 0 });
        AssertBytesAndRoundtrip(new GenAutoKeyPoco { X = 9, Y = "auto" });
    }

    [Fact]
    public void StringKey_SharedLengthBuckets_Automata()
    {
        AssertBytesAndRoundtrip(new GenKeyBucketPoco
        {
            A = 1,
            B = 2,
            Abc = 3,
            Abd = 4,
            Name = "n",
            Tags = "t",
            CreatedAt0 = 5,
            CreatedAt1 = 6,
            LongName = "v",
        });
        AssertBytesAndRoundtrip(new GenKeyBucketPoco());
    }

    [Fact]
    public void VersionTolerance_UnknownKeysInsideSharedBuckets_AreSkipped()
    {
        var newer = new GenKeyBucketPocoV2
        {
            A = 1,
            C = -1,
            B = 2,
            Abc = 3,
            Abe = -2,
            Abd = 4,
            Name = "n",
            Nom5 = "skip",
            Tags = "t",
            CreatedAt0 = 5,
            CreatedAt2 = -3,
            CreatedAt1 = 6,
            LongName = "v",
        };
        var bytes = Oracle.Serialize(newer);

        var older = V4.Deserialize<GenKeyBucketPoco>(bytes)!;
        Assert.Equal(1, older.A);
        Assert.Equal(2, older.B);
        Assert.Equal(3, older.Abc);
        Assert.Equal(4, older.Abd);
        Assert.Equal("n", older.Name);
        Assert.Equal("t", older.Tags);
        Assert.Equal(5, older.CreatedAt0);
        Assert.Equal(6, older.CreatedAt1);
        Assert.Equal("v", older.LongName);
    }

    [Fact]
    public void Nested_FormatterFieldsResolveThroughDefaultChain()
    {
        var value = new GenNestedPoco
        {
            Child = new GenIntKeyPoco { Id = 1, Name = "c", Score = 2.5, Flag = true, Ticks = 3 },
            Numbers = [1, -1, 128, -129, 70000, int.MinValue],
            Tags = ["a", "こんにちは", ""],
            Map = new Dictionary<string, int> { ["one"] = 1, ["big"] = 100000 },
            Stamp = Stamp,
        };
        AssertBytesAndRoundtrip(value);
        AssertBytesAndRoundtrip(new GenNestedPoco()); // all members null/default
    }

    [Fact]
    public void Struct_RoundtripsAndRejectsNil()
    {
        AssertBytesAndRoundtrip(new GenStructPoco { A = 42, B = -1.5 });

        // nil cannot populate a non-nullable struct
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<GenStructPoco>([0xc0]));
    }

    [Fact]
    public void VersionTolerance_IntKey_ExtraTrailingMembersAreSkipped()
    {
        // a "newer" writer with extra members (including a nested container) at keys 5/6
        var newer = new GenIntKeyPocoV2
        {
            Id = 42,
            Name = "n",
            Score = 1.5,
            Flag = true,
            Ticks = 123,
            Extra = "extra-value",
            ExtraList = [1, 2, 3],
        };
        var bytes = Oracle.Serialize(newer);

        var older = V4.Deserialize<GenIntKeyPoco>(bytes)!;
        Assert.Equal(42, older.Id);
        Assert.Equal("n", older.Name);
        Assert.Equal(1.5, older.Score);
        Assert.True(older.Flag);
        Assert.Equal(123, older.Ticks);
    }

    [Fact]
    public void VersionTolerance_StringKey_UnknownKeysAreSkipped()
    {
        var newer = new GenStringKeyPocoV2
        {
            Id = 7,
            Name = "name",
            F = 2.5f,
            Extra = new Dictionary<string, int> { ["x"] = 1 },
        };
        var bytes = Oracle.Serialize(newer);

        var older = V4.Deserialize<GenStringKeyPoco>(bytes)!;
        Assert.Equal(7, older.Id);
        Assert.Equal("name", older.Name);
        Assert.Equal(2.5f, older.F);
    }

    [Fact]
    public void VersionTolerance_OlderPayloadLeavesMissingMembersDefault()
    {
        // an "older" writer without keys 3/4: deserializing leaves those at defaults
        var bytes = Oracle.Serialize(new GenIntKeyPocoV0 { Id = 5, Name = "old" });
        var value = V4.Deserialize<GenIntKeyPoco>(bytes)!;
        Assert.Equal(5, value.Id);
        Assert.Equal("old", value.Name);
        Assert.False(value.Flag);
        Assert.Equal(0, value.Ticks);
    }

    [Fact]
    public void ExplicitFactoryChain_WorksWithoutModuleInitializerRegistration()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
        ]));
        var value = new GenNestedPoco { Numbers = [1, 2, 3], Stamp = Stamp };
        var bytes = V4.Serialize(value, options);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(bytes, V4.Serialize(V4.Deserialize<GenNestedPoco>(bytes, options), options));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(16)]
    public void SequenceSegmentBoundaries_StringKeysAndNestedGraphs(int chunkSize)
    {
        // map keys and nested payloads crossing segment boundaries exercise the
        // stitch path inside the generated utf8 key matching (GetSpan(byteCount))
        var stringKey = new GenStringKeyPoco { Id = 7, Name = "こんにちは世界", F = 1.25f };
        var nested = new GenNestedPoco
        {
            Child = new GenIntKeyPoco { Id = 1, Name = "child-name-long-enough", Score = 2.5, Flag = true, Ticks = 3 },
            Numbers = [1, -1, 70000],
            Tags = ["tag-one", "tag-two"],
            Map = new Dictionary<string, int> { ["key-alpha"] = 1 },
            Stamp = Stamp,
        };

        {
            var bytes = V4.Serialize(stringKey);
            var back = V4.Deserialize<GenStringKeyPoco>(SkipTestsChunk(bytes, chunkSize))!;
            Assert.Equal(7, back.Id);
            Assert.Equal("こんにちは世界", back.Name);
            Assert.Equal(1.25f, back.F);
        }
        {
            var bytes = V4.Serialize(nested);
            var back = V4.Deserialize<GenNestedPoco>(SkipTestsChunk(bytes, chunkSize))!;
            Assert.Equal(Oracle.Serialize(nested), Oracle.Serialize(back));
        }
    }

    sealed class Chunked : System.Buffers.ReadOnlySequenceSegment<byte>
    {
        public Chunked(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Chunked Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Chunked(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static System.Buffers.ReadOnlySequence<byte> SkipTestsChunk(byte[] bytes, int chunkSize)
    {
        var first = new Chunked(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var last = first;
        for (int i = chunkSize; i < bytes.Length; i += chunkSize)
        {
            last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
        }
        return new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Fact]
    public void Populate_ReusesExistingInstance()
    {
        var bytes = V4.Serialize(new GenIntKeyPoco { Id = 1, Name = "x", Score = 2, Flag = true, Ticks = 3 });
        var target = new GenIntKeyPoco { Id = 999 };
        var result = target;
        V4.Deserialize(bytes, ref result);
        Assert.Same(target, result);
        Assert.Equal(1, result.Id);
        Assert.Equal("x", result.Name);
    }
}

[V3::MessagePack.MessagePackObject]
public class GenIntKeyPoco
{
    [V3::MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1)] public string? Name { get; set; }
    [V3::MessagePack.Key(2)] public double Score { get; set; }
    [V3::MessagePack.Key(3)] public bool Flag { get; set; }
    [V3::MessagePack.Key(4)] public long Ticks { get; set; }
    [V3::MessagePack.IgnoreMember] public int Ignored { get; set; }
}

// same wire shape as GenIntKeyPoco plus two newer members
[V3::MessagePack.MessagePackObject]
public class GenIntKeyPocoV2
{
    [V3::MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1)] public string? Name { get; set; }
    [V3::MessagePack.Key(2)] public double Score { get; set; }
    [V3::MessagePack.Key(3)] public bool Flag { get; set; }
    [V3::MessagePack.Key(4)] public long Ticks { get; set; }
    [V3::MessagePack.Key(5)] public string? Extra { get; set; }
    [V3::MessagePack.Key(6)] public List<int>? ExtraList { get; set; }
}

// same wire shape as GenIntKeyPoco minus the newer members
[V3::MessagePack.MessagePackObject]
public class GenIntKeyPocoV0
{
    [V3::MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1)] public string? Name { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenHolePoco
{
    [V3::MessagePack.Key(1)] public int A { get; set; }
    [V3::MessagePack.Key(4)] public string? B { get; set; }
    [V3::MessagePack.Key(7)] public byte C { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenStringKeyPoco
{
    [V3::MessagePack.Key("id")] public int Id { get; set; }
    [V3::MessagePack.Key("name")] public string? Name { get; set; }
    [V3::MessagePack.Key("f")] public float F { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenStringKeyPocoV2
{
    [V3::MessagePack.Key("id")] public int Id { get; set; }
    [V3::MessagePack.Key("name")] public string? Name { get; set; }
    [V3::MessagePack.Key("f")] public float F { get; set; }
    [V3::MessagePack.Key("extra")] public Dictionary<string, int>? Extra { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class GenAutoKeyPoco
{
    public int X { get; set; }
    public string? Y { get; set; }
}

// key shapes chosen to force every branch of the emitted length-first automata:
// single-byte keys (byte discriminator switch), a shared length-4 bucket (full-width
// chunk switch), length-3 keys sharing their first read (grouped rest-compare),
// >8-byte keys sharing the first ulong chunk (overlapped tail compare), and a
// multi-chunk 19-byte key
[V3::MessagePack.MessagePackObject]
public class GenKeyBucketPoco
{
    [V3::MessagePack.Key("a")] public int A { get; set; }
    [V3::MessagePack.Key("b")] public int B { get; set; }
    [V3::MessagePack.Key("abc")] public int Abc { get; set; }
    [V3::MessagePack.Key("abd")] public int Abd { get; set; }
    [V3::MessagePack.Key("name")] public string? Name { get; set; }
    [V3::MessagePack.Key("tags")] public string? Tags { get; set; }
    [V3::MessagePack.Key("createdAt0")] public long CreatedAt0 { get; set; }
    [V3::MessagePack.Key("createdAt1")] public long CreatedAt1 { get; set; }
    [V3::MessagePack.Key("aVeryLongMemberName")] public string? LongName { get; set; }
}

// the newer-writer twin: every extra key lands inside an existing bucket or shared
// chunk group of GenKeyBucketPoco, so skipping it exercises the automata's
// fall-through (not the zero-compare length rejection)
[V3::MessagePack.MessagePackObject]
public class GenKeyBucketPocoV2
{
    [V3::MessagePack.Key("a")] public int A { get; set; }
    [V3::MessagePack.Key("c")] public int C { get; set; }
    [V3::MessagePack.Key("b")] public int B { get; set; }
    [V3::MessagePack.Key("abc")] public int Abc { get; set; }
    [V3::MessagePack.Key("abe")] public int Abe { get; set; }
    [V3::MessagePack.Key("abd")] public int Abd { get; set; }
    [V3::MessagePack.Key("name")] public string? Name { get; set; }
    [V3::MessagePack.Key("nom5")] public string? Nom5 { get; set; }
    [V3::MessagePack.Key("tags")] public string? Tags { get; set; }
    [V3::MessagePack.Key("createdAt0")] public long CreatedAt0 { get; set; }
    [V3::MessagePack.Key("createdAt2")] public long CreatedAt2 { get; set; }
    [V3::MessagePack.Key("createdAt1")] public long CreatedAt1 { get; set; }
    [V3::MessagePack.Key("aVeryLongMemberName")] public string? LongName { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenNestedPoco
{
    [V3::MessagePack.Key(0)] public GenIntKeyPoco? Child { get; set; }
    [V3::MessagePack.Key(1)] public int[]? Numbers { get; set; }
    [V3::MessagePack.Key(2)] public List<string>? Tags { get; set; }
    [V3::MessagePack.Key(3)] public Dictionary<string, int>? Map { get; set; }
    [V3::MessagePack.Key(4)] public DateTime Stamp { get; set; }
}

[V3::MessagePack.MessagePackObject]
public struct GenStructPoco
{
    [V3::MessagePack.Key(0)] public int A { get; set; }
    [V3::MessagePack.Key(1)] public double B { get; set; }
}

// the motivating custom string tier: same wire as the default, interned on read
// (partial: FactoryBridgeGenerator supplies the Type-based CreateFormatter tier)
sealed partial class InterningStringFactory : MessagePackFormatterFactory
{
    public static readonly InterningStringFactory Instance = new InterningStringFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(string) ? new InterningStringFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

sealed class InterningStringFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
    where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
    {
        buffer.WriteString(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
    {
        var read = buffer.ReadString();
        value = read == null ? null : string.Intern(read);
    }
}

using MessagePack;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;
using Ultra = UltraMessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// Source-generator end-to-end: every type here gets its formatter generated at compile
// time and auto-registered via the emitted module initializer, so Ultra.Default just
// works. The verification anchor is byte-exact equality against MessagePack-CSharp
// serializing the SAME attributed types with its own resolvers.
public class GeneratedFormatterTests
{
    static readonly DateTime Stamp = new DateTime(2026, 7, 21, 1, 2, 3, 456, DateTimeKind.Utc);

    static void AssertBytesAndRoundtrip<T>(T value)
    {
        var ours = Ultra.Default.Serialize(value);
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, ours);

        // both directions decode each other's bytes
        var back = Ultra.Default.Deserialize<T>(ours);
        Assert.Equal(Oracle.Serialize(back), oracle);
        var fromOracle = Ultra.Default.Deserialize<T>(oracle);
        Assert.Equal(Oracle.Serialize(fromOracle), oracle);
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
        var bytes = Ultra.Default.Serialize(value);
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
        Assert.Throws<MessagePackSerializationException>(() => Ultra.Default.Deserialize<GenStructPoco>([0xc0]));
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

        var older = Ultra.Default.Deserialize<GenIntKeyPoco>(bytes)!;
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

        var older = Ultra.Default.Deserialize<GenStringKeyPoco>(bytes)!;
        Assert.Equal(7, older.Id);
        Assert.Equal("name", older.Name);
        Assert.Equal(2.5f, older.F);
    }

    [Fact]
    public void VersionTolerance_OlderPayloadLeavesMissingMembersDefault()
    {
        // an "older" writer without keys 3/4: deserializing leaves those at defaults
        var bytes = Oracle.Serialize(new GenIntKeyPocoV0 { Id = 5, Name = "old" });
        var value = Ultra.Default.Deserialize<GenIntKeyPoco>(bytes)!;
        Assert.Equal(5, value.Id);
        Assert.Equal("old", value.Name);
        Assert.False(value.Flag);
        Assert.Equal(0, value.Ticks);
    }

    [Fact]
    public void ExplicitFactoryChain_WorksWithoutModuleInitializerRegistration()
    {
        var serializer = new Ultra(
            UltraMessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            PrimitiveFormatterFactory.Instance,
            GenericFormatterFactory.Instance);
        var value = new GenNestedPoco { Numbers = [1, 2, 3], Stamp = Stamp };
        var bytes = serializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(bytes, serializer.Serialize(serializer.Deserialize<GenNestedPoco>(bytes)));
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
            var bytes = Ultra.Default.Serialize(stringKey);
            var back = Ultra.Default.Deserialize<GenStringKeyPoco>(SkipTestsChunk(bytes, chunkSize))!;
            Assert.Equal(7, back.Id);
            Assert.Equal("こんにちは世界", back.Name);
            Assert.Equal(1.25f, back.F);
        }
        {
            var bytes = Ultra.Default.Serialize(nested);
            var back = Ultra.Default.Deserialize<GenNestedPoco>(SkipTestsChunk(bytes, chunkSize))!;
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
        var bytes = Ultra.Default.Serialize(new GenIntKeyPoco { Id = 1, Name = "x", Score = 2, Flag = true, Ticks = 3 });
        var target = new GenIntKeyPoco { Id = 999 };
        var result = target;
        Ultra.Default.Deserialize(ref result, bytes);
        Assert.Same(target, result);
        Assert.Equal(1, result.Id);
        Assert.Equal("x", result.Name);
    }
}

[MessagePackObject]
public class GenIntKeyPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
    [Key(3)] public bool Flag { get; set; }
    [Key(4)] public long Ticks { get; set; }
    [IgnoreMember] public int Ignored { get; set; }
}

// same wire shape as GenIntKeyPoco plus two newer members
[MessagePackObject]
public class GenIntKeyPocoV2
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
    [Key(3)] public bool Flag { get; set; }
    [Key(4)] public long Ticks { get; set; }
    [Key(5)] public string? Extra { get; set; }
    [Key(6)] public List<int>? ExtraList { get; set; }
}

// same wire shape as GenIntKeyPoco minus the newer members
[MessagePackObject]
public class GenIntKeyPocoV0
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
}

[MessagePackObject]
public class GenHolePoco
{
    [Key(1)] public int A { get; set; }
    [Key(4)] public string? B { get; set; }
    [Key(7)] public byte C { get; set; }
}

[MessagePackObject]
public class GenStringKeyPoco
{
    [Key("id")] public int Id { get; set; }
    [Key("name")] public string? Name { get; set; }
    [Key("f")] public float F { get; set; }
}

[MessagePackObject]
public class GenStringKeyPocoV2
{
    [Key("id")] public int Id { get; set; }
    [Key("name")] public string? Name { get; set; }
    [Key("f")] public float F { get; set; }
    [Key("extra")] public Dictionary<string, int>? Extra { get; set; }
}

[MessagePackObject(true)]
public class GenAutoKeyPoco
{
    public int X { get; set; }
    public string? Y { get; set; }
}

[MessagePackObject]
public class GenNestedPoco
{
    [Key(0)] public GenIntKeyPoco? Child { get; set; }
    [Key(1)] public int[]? Numbers { get; set; }
    [Key(2)] public List<string>? Tags { get; set; }
    [Key(3)] public Dictionary<string, int>? Map { get; set; }
    [Key(4)] public DateTime Stamp { get; set; }
}

[MessagePackObject]
public struct GenStructPoco
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public double B { get; set; }
}

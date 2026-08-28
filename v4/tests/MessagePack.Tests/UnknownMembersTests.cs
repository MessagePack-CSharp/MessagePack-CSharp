using MessagePack;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MessagePackUnknownMembers end-to-end: a type declaring one settable member of the packet
// type captures the keys (or trailing array elements) its schema does not declare and
// replays them on serialization, so a narrow schema in the middle of a pipeline no longer
// strips what a wider writer produced. The strongest assertion available is byte identity:
// wide bytes -> narrow deserialize -> narrow serialize must reproduce the wide bytes
// exactly, because the narrow type declares a prefix of the wide member order.
public class UnknownMembersTests
{
    // ---- map (string-key) mode ----

    [Fact]
    public void MapMode_UnknownKeysRoundTripByteExact()
    {
        var wide = new UnkWideMap { A = 42, B = "b", C = "extra", D = true };
        var bytes = V4.Serialize(wide);

        var narrow = V4.Deserialize<UnkNarrowMap>(bytes)!;
        Assert.Equal(42, narrow.A);
        Assert.Equal("b", narrow.B);
        Assert.Equal(2, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));

        // and the wide reader sees its full schema again after the narrow hop
        var wideBack = V4.Deserialize<UnkWideMap>(V4.Serialize(narrow))!;
        Assert.Equal("extra", wideBack.C);
        Assert.True(wideBack.D);
    }

    [Fact]
    public void MapMode_NoUnknownKeys_PacketStaysNull_WireUnchanged()
    {
        var plainBytes = V4.Serialize(new UnkNarrowMapPlain { A = 1, B = "x" });
        var narrow = V4.Deserialize<UnkNarrowMap>(plainBytes)!;
        Assert.Null(narrow.Extra);
        // a packet-less instance writes the exact wire of a twin type without the member
        Assert.Equal(plainBytes, V4.Serialize(narrow));
    }

    [Fact]
    public void MapMode_ValueEncodingsSurviveVerbatim()
    {
        // uint64-coded 5 is NOT the canonical encoding of 5: the round trip keeping the
        // 9-byte form proves values ride raw captured bytes, not a re-encode
        byte[] payload = [0x81, 0xa1, (byte)'x', 0xcf, 0, 0, 0, 0, 0, 0, 0, 5];
        var value = V4.Deserialize<UnkPacketOnly>(payload)!;
        Assert.Equal(1, value.Extra!.Count);
        Assert.Equal(payload, V4.Serialize(value));
    }

    [Fact]
    public void MapMode_DuplicateUnknownKeyThrows()
    {
        // declared keys already reject duplicates; captured keys keep the same policy
        // instead of preserving a payload every strict reader downstream would refuse
        byte[] payload = [0x82, 0xa1, (byte)'x', 0x01, 0xa1, (byte)'x', 0x02];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkPacketOnly>(payload));
    }

    // ---- array (int-key) mode ----

    [Fact]
    public void ArrayMode_TrailingElementsAndHolesRoundTripByteExact()
    {
        // wide keys 0,1,4: nil holes at 2,3. Beyond the narrow type's declared keys they
        // are trailing data and captured verbatim (they are the WIDE schema's padding)
        var wide = new UnkWideArray { A = 7, B = "b", E = 2.5 };
        var bytes = V4.Serialize(wide);

        var narrow = V4.Deserialize<UnkNarrowArray>(bytes)!;
        Assert.Equal(7, narrow.A);
        Assert.Equal(3, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));

        var wideBack = V4.Deserialize<UnkWideArray>(V4.Serialize(narrow))!;
        Assert.Equal(2.5, wideBack.E);
    }

    [Fact]
    public void ArrayMode_NoTrailing_PacketNull_WireUnchanged()
    {
        var plainBytes = V4.Serialize(new UnkNarrowArrayPlain { A = 1, B = "x" });
        var narrow = V4.Deserialize<UnkNarrowArray>(plainBytes)!;
        Assert.Null(narrow.Extra);
        Assert.Equal(plainBytes, V4.Serialize(narrow));
    }

    [Fact]
    public void ArrayMode_DeletedKeyHole_RoundTripsByteExact()
    {
        // the narrow schema deleted key 1 (a hole INSIDE its declared range) while a
        // wider writer still fills it: the value is captured and replayed in place, and
        // key 3 rides as trailing capture, so the round trip stays byte-identical
        var wide = new UnkHoleWideArray { A = 7, B = "b", C = 2.5, D = true };
        var bytes = V4.Serialize(wide);

        var narrow = V4.Deserialize<UnkHoleArray>(bytes)!;
        Assert.Equal(7, narrow.A);
        Assert.Equal(2.5, narrow.C);
        Assert.Equal(2, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));

        var wideBack = V4.Deserialize<UnkHoleWideArray>(V4.Serialize(narrow))!;
        Assert.Equal("b", wideBack.B);
        Assert.True(wideBack.D);

        var view = narrow.Extra.ToArrayDictionary();
        Assert.Equal("b", view[1]);
        Assert.Equal(true, view[3]);
    }

    [Fact]
    public void ArrayMode_NilAtHole_IsRegeneratedPadding_PacketStaysNull()
    {
        // the type's own payloads carry nil at their holes; capturing that padding would
        // allocate a packet on every self round trip, so nil holes are skipped on read
        // and regenerated on write, byte-identically
        var selfBytes = V4.Serialize(new UnkHoleArray { A = 1, C = 2.0 });
        var back = V4.Deserialize<UnkHoleArray>(selfBytes)!;
        Assert.Null(back.Extra);
        Assert.Equal(selfBytes, V4.Serialize(back));
    }

    // ---- construction shape ----

    [Fact]
    public void ConstructionShape_CapturesThroughTheInitializer()
    {
        var bytes = V4.Serialize(new UnkWideMap { A = 5, B = "b", C = "c", D = false });
        var narrow = V4.Deserialize<UnkNarrowCtor>(bytes)!;
        Assert.Equal(5, narrow.A);
        Assert.Equal(3, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));
    }

    // ---- populate ----

    [Fact]
    public void Populate_ReplacesThePacketWholesale()
    {
        var withExtras = V4.Serialize(new UnkWideMap { A = 1, B = "b", C = "c", D = true });
        var withoutExtras = V4.Serialize(new UnkNarrowMapPlain { A = 2, B = "x" });

        var target = new UnkNarrowMap();
        var result = target;
        V4.Deserialize(withExtras, ref result);
        Assert.Same(target, result);
        Assert.Equal(2, target.Extra!.Count);

        // a second deserialization with nothing to capture clears the stale packet
        V4.Deserialize(withoutExtras, ref result);
        Assert.Null(target.Extra);
    }

    // ---- multi-segment sequences (ReadRaw's window escalation) ----

    [Fact]
    public void MultiSegmentSequence_CapturesAcrossSeams()
    {
        var wide = new UnkWideMap { A = 42, B = new string('b', 40), C = new string('c', 60), D = true };
        var bytes = V4.Serialize(wide);
        for (int chunkSize = 1; chunkSize <= 7; chunkSize++)
        {
            var narrow = V4.Deserialize<UnkNarrowMap>(Chunk(bytes, chunkSize))!;
            Assert.Equal(bytes, V4.Serialize(narrow));
        }
    }

    // ---- materialized views ----

    [Fact]
    public void TypedDictionaries_MaterializeReadOnlyViews()
    {
        var narrow = V4.Deserialize<UnkNarrowMap>(V4.Serialize(new UnkWideMap { A = 1, B = "b", C = "see", D = true }))!;
        Assert.True(narrow.Extra!.IsMap);
        Assert.False(narrow.Extra.IsArray);
        var view = narrow.Extra.ToMapDictionary();
        Assert.Equal(2, view.Count);
        Assert.Equal("see", view["C"]);
        Assert.Equal(true, view["D"]);
        Assert.Throws<InvalidOperationException>(() => narrow.Extra.ToArrayDictionary());

        var arrayNarrow = V4.Deserialize<UnkNarrowArray>(V4.Serialize(new UnkWideArray { A = 1, B = "b", E = 2.5 }))!;
        Assert.True(arrayNarrow.Extra!.IsArray);
        Assert.False(arrayNarrow.Extra.IsMap);
        var arrayView = arrayNarrow.Extra.ToArrayDictionary();
        Assert.Equal(2.5, arrayView[4]);
        Assert.Null(arrayView[2]); // the wider schema's nil padding, keyed by array index
        Assert.Throws<InvalidOperationException>(() => arrayNarrow.Extra.ToMapDictionary());
    }

    [Fact]
    public void EmptyPacket_KeepsItsConstructedMode()
    {
        // the mode is a construction-time fact, not derived from entries: an empty packet
        // still knows which wire form it belongs to, and the wrong view throws
        var map = new MessagePackUnknownMembers(isMap: true);
        Assert.True(map.IsMap);
        Assert.False(map.IsArray);
        Assert.Empty(map.ToMapDictionary());
        Assert.Throws<InvalidOperationException>(() => map.ToArrayDictionary());

        var array = new MessagePackUnknownMembers(isMap: false);
        Assert.True(array.IsArray);
        Assert.False(array.IsMap);
        Assert.Empty(array.ToArrayDictionary());
        Assert.Throws<InvalidOperationException>(() => array.ToMapDictionary());
    }

    // ---- refusals ----

    [Fact]
#pragma warning disable MsgPack108 // the loud miss IS the assertion
    public void StandaloneType_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => V4.Serialize(new MessagePackUnknownMembers(isMap: true)));
    }
#pragma warning restore MsgPack108

    [Fact]
    public void ReflectionTier_RefusesUnknownMembersTypes()
    {
        // capture/replay lives only in the source-generated formatter; the reflection tier
        // throws instead of silently dropping the retention the member exists to provide
        Assert.Throws<NotSupportedException>(() => V4.Serialize(new UnkSuppressed { A = 1 }));
    }

    [Fact]
    public void TransplantedPacket_FailsLoudInsteadOfCorruptingTheWire()
    {
        // the member is settable, so a packet can be moved across types; replaying a
        // capture whose entries cannot fit the new host must throw, not emit a broken map/array
        var narrowMap = V4.Deserialize<UnkNarrowMap>(V4.Serialize(new UnkWideMap { A = 1, B = "b", C = "c", D = true }))!;
        var arrayHost = new UnkNarrowArray { A = 1, Extra = narrowMap.Extra };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(arrayHost));

        var narrowArray = V4.Deserialize<UnkNarrowArray>(V4.Serialize(new UnkWideArray { A = 1, B = "b", E = 2.5 }))!;
        var mapHost = new UnkNarrowMap { A = 1, Extra = narrowArray.Extra };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(mapHost));

        // a map-mode packet on a type WITH key holes fails at the first hole's cursor
        // check (string-keyed entries carry index -1), same loud refusal
        var holeMapHost = new UnkHoleArray { A = 1, Extra = narrowMap.Extra };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(holeMapHost));
    }

    [Fact]
    public void TransplantedPacket_EntryAtDeclaredKey_Throws()
    {
        // UnkNarrowArray (keys 0,1) captures indices 2,3,4 as trailing; transplanted
        // into UnkHoleArray, only index 1 is a hole there — index 2 collides with a
        // DECLARED key, so replay throws instead of writing member value and captured
        // entry into the same slot's wire position
        var narrowArray = V4.Deserialize<UnkNarrowArray>(V4.Serialize(new UnkWideArray { A = 1, B = "b", E = 2.5 }))!;
        Assert.Equal(3, narrowArray.Extra!.Count);
        var holeHost = new UnkHoleArray { A = 1, C = 9.0, Extra = narrowArray.Extra };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(holeHost));
    }

    // ---- generator diagnostics (MsgPack020) ----

    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "UnknownMembersProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Fact]
    public void KeyOnPacketMember_ReportsMsgPack020()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            public class Keyed
            {
                [Key(0)] public int A { get; set; }
                [Key(1)] public MessagePackUnknownMembers? Extra { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack020" && d.GetMessage().Contains("[Key]"));
    }

    [Fact]
    public void DuplicatePacketMembers_ReportsMsgPack020()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(true)]
            public class Twice
            {
                public int A { get; set; }
                public MessagePackUnknownMembers? Extra { get; set; }
                public MessagePackUnknownMembers? More { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack020" && d.GetMessage().Contains("more than one"));
    }

    [Fact]
    public void GetOnlyPacketMember_ReportsMsgPack020()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(true)]
            public class GetOnly
            {
                public int A { get; set; }
                public MessagePackUnknownMembers? Extra { get; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack020" && d.GetMessage().Contains("settable"));
    }

    [Fact]
    public void InitOnlyPacketMember_ReportsMsgPack020()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(true)]
            public class InitOnly
            {
                public int A { get; set; }
                public MessagePackUnknownMembers? Extra { get; init; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack020" && d.GetMessage().Contains("settable"));
    }

    [Fact]
    public void ValidPacketMember_GeneratesWithoutDiagnostics()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(true)]
            public class Valid
            {
                public int A { get; set; }
                public MessagePackUnknownMembers? Extra { get; set; }
            }
            """);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack020");
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("ValidFormatter"));
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

    static System.Buffers.ReadOnlySequence<byte> Chunk(byte[] bytes, int chunkSize)
    {
        var first = new Chunked(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var last = first;
        for (int i = chunkSize; i < bytes.Length; i += chunkSize)
        {
            last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
        }
        return new System.Buffers.ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}

// the wide twins are the "newer schema" writers; each narrow twin declares a strict prefix
// of the wide member order, which is what makes the round trips byte-exact

[MessagePackObject(true)]
public class UnkWideMap
{
    public int A { get; set; }
    public string? B { get; set; }
    public string? C { get; set; }
    public bool D { get; set; }
}

[MessagePackObject(true)]
public class UnkNarrowMap
{
    public int A { get; set; }
    public string? B { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject(true)]
public class UnkNarrowMapPlain
{
    public int A { get; set; }
    public string? B { get; set; }
}

[MessagePackObject(true)]
public class UnkPacketOnly
{
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject]
public class UnkWideArray
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public string? B { get; set; }
    [Key(4)] public double E { get; set; }
}

[MessagePackObject]
public class UnkNarrowArray
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public string? B { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject]
public class UnkNarrowArrayPlain
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public string? B { get; set; }
}

// the deleted-member pair: the wide twin is contiguous 0..3; the narrow twin retired key 1
// (hole inside its declared range) and never declared key 3 (trailing)

[MessagePackObject]
public class UnkHoleWideArray
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public string? B { get; set; }
    [Key(2)] public double C { get; set; }
    [Key(3)] public bool D { get; set; }
}

[MessagePackObject]
public class UnkHoleArray
{
    [Key(0)] public int A { get; set; }
    [Key(2)] public double C { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject(true)]
public class UnkNarrowCtor
{
    public UnkNarrowCtor(int a) => A = a;
    public int A { get; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class UnkSuppressed
{
    public int A { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

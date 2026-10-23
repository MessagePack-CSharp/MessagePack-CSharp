using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// constructor path: a plain-set member the payload did not carry (an older writer's shorter array, a map without the
// key) keeps the value its initializer gave it, as the reflection tier and v3 do; an init-only member cannot (object
// initializers are unconditional) and gets its default
[MessagePackObject]
public class CtorWithDefaultedMembers
{
    public CtorWithDefaultedMembers(int id)
    {
        Id = id;
    }

    [Key(0)] public int Id { get; }

    [Key(1)] public int Version { get; set; } = 42;

    [Key(2)] public string? Note { get; set; } = "initial";

    [Key(3)] public int InitOnly { get; init; } = 7;
}

[MessagePackObject(true)]
public class CtorWithDefaultedMembersMap
{
    public CtorWithDefaultedMembersMap(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public int Version { get; set; } = 42;
}

// `required` members the formatter never reads must still satisfy every generated `new` (CS9035)
[MessagePackObject]
public class IgnoredRequiredMember
{
    [Key(0)] public int Id { get; set; }

    [IgnoreMember] public required string Name { get; set; }
}

[MessagePackObject]
public class IgnoredRequiredMemberWithCtor
{
    public IgnoredRequiredMemberWithCtor(int id)
    {
        Id = id;
    }

    [Key(0)] public int Id { get; }

    [IgnoreMember] public required string Name { get; set; }
}

// a parameterless [SetsRequiredMembers] constructor already satisfies the required members: the generated `new`
// must not add `Name = default!`, which would both be redundant and erase what the constructor set
[MessagePackObject]
public class IgnoredRequiredMemberSetByConstructor
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public IgnoredRequiredMemberSetByConstructor()
    {
        Name = "from the constructor";
    }

    [Key(0)] public int Id { get; set; }

    [IgnoreMember] public required string Name { get; set; }
}

[MessagePackObject]
public class RequiredUnknownMembersPacket
{
    [Key(0)] public int Id { get; set; }

    public required MessagePackUnknownMembers? Unknown { get; set; }
}

// a type nested in a generic mentions T through its containing type, not its own arguments
public class GenericOuter<T>
{
    public enum Kind
    {
        A,
        B,
    }

    public class Inner
    {
        public T? Value { get; set; }
    }
}

[MessagePackObject]
public class NestedInGenericMember<T>
{
    [Key(0)] public GenericOuter<T>.Kind Kind { get; set; }

    [Key(1)] public T? Value { get; set; }
}

public class ReviewRound2RegressionTests
{
    [Fact]
    public void ConstructorPath_AbsentMembersKeepTheirInitializers()
    {
        var old = V4.Deserialize<CtorWithDefaultedMembers>([0x91, 0x01])!; // [1]: only the constructor argument
        Assert.Equal(1, old.Id);
        Assert.Equal(42, old.Version);
        Assert.Equal("initial", old.Note);
        Assert.Equal(0, old.InitOnly); // init-only: unconditional object initializer, documented

        var partial = V4.Deserialize<CtorWithDefaultedMembers>([0x92, 0x01, 0x05])!; // [1, 5]
        Assert.Equal(5, partial.Version);
        Assert.Equal("initial", partial.Note);

        var explicitNil = V4.Deserialize<CtorWithDefaultedMembers>([0x93, 0x01, 0x05, 0xC0])!; // [1, 5, nil]
        Assert.Null(explicitNil.Note); // present as nil: assigned

        var full = V4.Deserialize<CtorWithDefaultedMembers>(V4.Serialize(new CtorWithDefaultedMembers(3) { Version = 8, Note = "n", InitOnly = 9 }))!;
        Assert.Equal((3, 8, "n", 9), (full.Id, full.Version, full.Note, full.InitOnly));

        var map = V4.Deserialize<CtorWithDefaultedMembersMap>([0x81, 0xA2, (byte)'I', (byte)'d', 0x01])!; // {"Id": 1}
        Assert.Equal(1, map.Id);
        Assert.Equal(42, map.Version);
    }

    [Fact]
    public void IgnoredRequiredMembers_CompileAndRoundtrip()
    {
        var back = V4.Deserialize<IgnoredRequiredMember>(V4.Serialize(new IgnoredRequiredMember { Id = 5, Name = "ignored" }))!;
        Assert.Equal(5, back.Id);
        Assert.Null(back.Name); // never on the wire

        var ctor = V4.Deserialize<IgnoredRequiredMemberWithCtor>(V4.Serialize(new IgnoredRequiredMemberWithCtor(6) { Name = "ignored" }))!;
        Assert.Equal(6, ctor.Id);
        Assert.Null(ctor.Name);

        var setByCtor = V4.Deserialize<IgnoredRequiredMemberSetByConstructor>(V4.Serialize(new IgnoredRequiredMemberSetByConstructor { Id = 8 }))!;
        Assert.Equal(8, setByCtor.Id);
        Assert.Equal("from the constructor", setByCtor.Name);

        var packet = V4.Deserialize<RequiredUnknownMembersPacket>([0x92, 0x07, 0x08])!; // [7, 8]: key 1 is unknown
        Assert.Equal(7, packet.Id);
        Assert.NotNull(packet.Unknown);
        Assert.Equal(new byte[] { 0x92, 0x07, 0x08 }, V4.Serialize(packet)); // replayed
    }

    [Fact]
    public void MemberNestedInGenericOuter_Roundtrips()
    {
        var back = V4.Deserialize<NestedInGenericMember<int>>(V4.Serialize(new NestedInGenericMember<int> { Kind = GenericOuter<int>.Kind.B, Value = 3 }))!;
        Assert.Equal(GenericOuter<int>.Kind.B, back.Kind);
        Assert.Equal(3, back.Value);
    }
}

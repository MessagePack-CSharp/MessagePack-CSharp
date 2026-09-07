using MessagePack;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.ClosedHierarchyParity;

// The STJ pain points Andrew Lock catalogued for .NET 11 preview 7 unions and closed
// hierarchies (andrewlock.net, "the pain of serializing unions and closed class
// hierarchies with System.Text.Json"), each replayed on v4's explicit-tag wire:
//   - a closed root with an internal intermediate and sealed leaves, dispatched by exact
//     runtime type (STJ: accessibility mismatch, then "Labrador not supported by Pet")
//   - a union of records that all serialize as objects (STJ: "JSON value type 'Object' is
//     ambiguous", needs a hand-written classifier)
//   - a union of primitives (STJ: transparent {"Value":42}, cannot round-trip)
//   - a union nested in a union (STJ: the RC1 structural classifier documents no support)
// The tag is always on the wire ([tag, payload]), so none of these need inference. The
// last fixture shows the base-as-member-carrier shape: a closed base WITHOUT
// [MessagePackObject] still contributes its keyed members to the derived formatter
// (a closed [MessagePackObject] base without [UnionTag] is rejected at compile time, as
// v3 did with MsgPack005, and the message names [UnionTag] rather than the missing ctor).
public class ClosedHierarchyParityTests
{
    [Fact]
    public void ClosedHierarchy_MultiLevel_InternalDerived_Roundtrips()
    {
        Pet lab = new Labrador { Name = "rex", FavouriteToy = "ball", Hungry = true };
        var back = V4.Deserialize<Pet>(V4.Serialize(lab));
        var l = Assert.IsType<Labrador>(back);
        Assert.Equal(("rex", "ball", true), (l.Name, l.FavouriteToy, l.Hungry));

        Pet dog = new Dog { Name = "d", FavouriteToy = "t" };
        Assert.IsType<Dog>(V4.Deserialize<Pet>(V4.Serialize(dog)));
        Pet cat = new Cat { Name = "c" };
        Assert.IsType<Cat>(V4.Deserialize<Pet>(V4.Serialize(cat)));
    }

    [Fact]
    public void RecordUnion_Roundtrips_WithDiscriminator()
    {
        SupportedOS w = new Windows("11");
        SupportedOS l = new Linux("ubuntu", "24");
        var bw = V4.Serialize(w);
        var bl = V4.Serialize(l);
        Assert.IsType<Windows>(V4.Deserialize<SupportedOS>(bw).Value);
        Assert.Equal("ubuntu", Assert.IsType<Linux>(V4.Deserialize<SupportedOS>(bl).Value).Distro);
        Assert.Equal(0x92, bw[0]);
    }

    [Fact]
    public void PrimitiveUnion_Roundtrips_NotTransparent()
    {
        var d1 = new Data { Value = new IntOrString(42) };
        var d2 = new Data { Value = new IntOrString("42") };
        var b1 = V4.Serialize(d1);
        var b2 = V4.Serialize(d2);
        Assert.NotEqual(b1, b2);
        Assert.Equal(42, Assert.IsType<int>(V4.Deserialize<Data>(b1).Value.Value));
        Assert.Equal("42", Assert.IsType<string>(V4.Deserialize<Data>(b2).Value.Value));
    }

    [Fact]
    public void NestedUnion_Roundtrips()
    {
        Outer o = new IntOrString("x");
        var back = V4.Deserialize<Outer>(V4.Serialize(o));
        Assert.Equal("x", Assert.IsType<IntOrString>(back.Value).Value);
        Outer o2 = new Windows("10");
        Assert.Equal("10", Assert.IsType<Windows>(V4.Deserialize<Outer>(V4.Serialize(o2)).Value).Version);
    }
}

// --- article's closed hierarchy, concrete closed base, internal intermediate, sealed leaf ---
[MessagePackObject]
[UnionTag<Dog>(0)]
[UnionTag<Labrador>(1)]
[UnionTag<Collie>(2)]
[UnionTag<Cat>(3)]
public closed class Pet
{
    [Key(0)] public string? Name { get; set; }
}

[MessagePackObject]
internal class Dog : Pet
{
    [Key(1)] public string? FavouriteToy { get; set; }
}

[MessagePackObject]
internal sealed class Labrador : Dog
{
    [Key(2)] public bool Hungry { get; set; }
}

[MessagePackObject]
internal sealed class Collie : Dog
{
    [Key(2)] public int Speed { get; set; }
}

[MessagePackObject]
internal sealed class Cat : Pet
{
    [Key(1)] public int Lives { get; set; }
}

// --- record union ---
[MessagePackObject] public record Windows([property: Key(0)] string Version);
[MessagePackObject] public record Linux([property: Key(0)] string Distro, [property: Key(1)] string Version);
[MessagePackObject] public record MacOS([property: Key(0)] string Name, [property: Key(1)] int Version);

[MessagePackObject]
[UnionTag<Windows>(0)]
[UnionTag<Linux>(1)]
[UnionTag<MacOS>(2)]
public union SupportedOS(Windows, Linux, MacOS);

// --- primitive union ---
[MessagePackObject]
[UnionTag<int>(0)]
[UnionTag<string>(1)]
public union IntOrString(int, string);

[MessagePackObject]
public class Data
{
    [Key(0)] public IntOrString Value { get; set; }
}

// --- nested union ---
[MessagePackObject]
[UnionTag<IntOrString>(0)]
[UnionTag<Windows>(1)]
public union Outer(IntOrString, Windows);

// --- closed base as member carrier only: no [MessagePackObject], keys inherited ---
public closed class Pet2
{
    [Key(0)] public string? Name { get; set; }
}

[MessagePackObject]
public sealed class Cat2 : Pet2
{
    [Key(1)] public int Lives { get; set; }
}

public class ClosedBaseMemberCarrierTests
{
    [Fact]
    public void ClosedBase_WithoutAttribute_ContributesKeyedMembers()
    {
        var c = new Cat2 { Name = "c", Lives = 9 };
        var back = V4.Deserialize<Cat2>(V4.Serialize(c));
        Assert.Equal(("c", 9), (back.Name, back.Lives));
    }

    [Fact]
    public void AbstractRoot_WithoutUnionTag_PointsAtUnionTag_NotAtTheConstructor()
    {
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            [MessagePackObject] public abstract class Root { [Key(0)] public string? Name { get; set; } }
            [MessagePackObject] public sealed class Leaf : Root { [Key(1)] public int Lives { get; set; } }
            """, "AbstractRootProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("[UnionTag]"));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack004");
    }
}

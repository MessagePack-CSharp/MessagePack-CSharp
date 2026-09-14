using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// `new`-shadowed members are separate storage with their own [Key], and every declaration
// serializes (v3's rule; the reflection tier and both V3Compat suites agree). Overrides
// share their base declaration's storage and stay ONE slot. The compat suites exercise the
// populate path; the construction shape (init/ctor present, base slot assigned through a
// declarer cast after construction) and the override side of the dedup rule live here.
public class ShadowedMemberTests
{
    [Fact]
    public void ShadowedProperty_ConstructionShape_KeepsBothValues()
    {
        var value = new ShadowCtorDerived { Name = "derived", Age = 42 };
        ((ShadowCtorBase)value).Name = "base";

        var bin = MessagePackSerializer.Serialize(value);
        Assert.Equal(0x93, bin[0]); // fixarray(3): base Name, derived Name, Age

        var back = MessagePackSerializer.Deserialize<ShadowCtorDerived>(bin)!;
        Assert.Equal("derived", back.Name);
        Assert.Equal("base", ((ShadowCtorBase)back).Name);
        Assert.Equal(42, back.Age);
    }

    [Fact]
    public void ShadowedField_StringKeyMode_KeepsBothValues()
    {
        var value = new ShadowMapDerived { Tag = 2 };
        ((ShadowMapBase)value).Tag = 1;

        var bin = MessagePackSerializer.Serialize(value);
        Assert.Equal(0x82, bin[0]); // fixmap(2): distinct explicit keys

        var back = MessagePackSerializer.Deserialize<ShadowMapDerived>(bin)!;
        Assert.Equal(2, back.Tag);
        Assert.Equal(1, ((ShadowMapBase)back).Tag);
    }

    [Fact]
    public void Override_IsOneSlot_NotTwo()
    {
        var bin = MessagePackSerializer.Serialize(new OverrideDerived { Name = "x" });
        Assert.Equal(0x91, bin[0]); // fixarray(1): the override shares the base slot
        Assert.Equal("x", MessagePackSerializer.Deserialize<OverrideDerived>(bin)!.Name);
    }

    [Fact]
    public void ShadowedInitOnlyBaseMember_IsRejected_MsgPack005()
    {
        // generated C# assigns init-only members through the object initializer, where the
        // derived declaration hides the base one - no route, so the type is skipped loudly
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            [MessagePackObject] public class B { [Key(0)] public string? P { get; init; } }
            [MessagePackObject] public class D : B { [Key(1)] public new string? P { get; set; } }
            """, "ShadowInitProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("init-only"));
    }
}

[MessagePackObject]
public class ShadowCtorBase
{
    [Key(0)]
    public string? Name { get; set; }
}

[MessagePackObject]
public class ShadowCtorDerived : ShadowCtorBase
{
    [Key(1)]
    public new string? Name { get; set; }

    [Key(2)]
    public int Age { get; init; } // forces the construction shape
}

[MessagePackObject]
public class ShadowMapBase
{
    [Key("tag")]
    public int Tag;
}

[MessagePackObject]
public class ShadowMapDerived : ShadowMapBase
{
    [Key("tag2")]
    public new int Tag;
}

[MessagePackObject]
public class OverrideBase
{
    [Key(0)]
    public virtual string? Name { get; set; }
}

[MessagePackObject]
public class OverrideDerived : OverrideBase
{
    public override string? Name { get; set; }
}

extern alias V3;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// IMessagePackSurrogate<TTarget, TSurrogate>: TTarget rides the wire as TSurrogate's shape,
// and every deserialized value flows through ToTarget (constructor validation stays in
// force). The declarative alternative to hand-writing a buffer-generic formatter. Both
// conversions are instance members (ToSurrogate runs on default(TSurrogate)), so the
// feature compiles on every TFM; the downlevel roundtrip lives in Tests.NetFx.
public class SurrogateTests
{
    [Fact]
    public void WireIsExactlyTheSurrogateShape()
    {
        var value = new SurrogateUser(42, "realm");
        var bytes = V4.Serialize(value);
        Assert.Equal(V4.Serialize(new SurrogateUserSurrogate(42, "realm")), bytes);
        Assert.Equal(Oracle.Serialize(new SurrogateUserSurrogate(42, "realm")), bytes);

        var back = V4.Deserialize<SurrogateUser>(bytes)!;
        Assert.Equal(42, back.Value);
        Assert.Equal("realm", back.Realm);
    }

    [Fact]
    public void DeserializationFlowsThroughConstructorValidation()
    {
        // a payload the constructor rejects must throw, not materialize an invalid instance
        var poisoned = V4.Serialize(new SurrogateUserSurrogate(-1, "realm"));
        Assert.ThrowsAny<Exception>(() => V4.Deserialize<SurrogateUser>(poisoned));
    }

    [Fact]
    public void NullAndAotChainRoundtrip()
    {
        Assert.Null(V4.Deserialize<SurrogateUser>(V4.Serialize<SurrogateUser?>(null)));

        // the type-level attribute registers through the generated factory, so the
        // registry-backed AOT chain (SourceGenerated + BuiltIn) serves it too
        var aot = MessagePack.MessagePackSerializerOptions.DefaultAot;
        var back = V4.Deserialize<SurrogateUser>(V4.Serialize(new SurrogateUser(7, "aot"), aot), aot)!;
        Assert.Equal(7, back.Value);
    }

    [Fact]
    public void MemberUsageAndStructTargetRoundtrip()
    {
        var holder = new SurrogateHolder
        {
            User = new SurrogateUser(1, "member"),
            Stamp = new SurrogateTick(123456789),
        };
        var back = V4.Deserialize<SurrogateHolder>(V4.Serialize(holder))!;
        Assert.Equal(1, back.User!.Value);
        Assert.Equal("member", back.User.Realm);
        Assert.Equal(123456789, back.Stamp.Ticks);

        var empty = V4.Deserialize<SurrogateHolder>(V4.Serialize(new SurrogateHolder()))!;
        Assert.Null(empty.User);
        Assert.Equal(0, empty.Stamp.Ticks);
    }

    // a third-party-ish target with no annotation anywhere: implementing the interface on
    // the surrogate is the whole declaration, so the default AND AOT chains serve it
    [Fact]
    public void UnannotatedTargetIsAutoRegistered()
    {
        var back = V4.Deserialize<ThirdPartyPoint>(V4.Serialize(new ThirdPartyPoint(3, 4)))!;
        Assert.Equal(3, back.X);
        Assert.Equal(4, back.Y);

        var aot = MessagePack.MessagePackSerializerOptions.DefaultAot;
        Assert.Equal(5, V4.Deserialize<ThirdPartyPoint>(V4.Serialize(new ThirdPartyPoint(5, 6), aot), aot)!.X);
    }

    // a RECURSIVE target needs no class surrogate: the struct surrogate references the
    // TARGET type in its members and resolution recurses through the registered surrogate
    // formatter one level at a time (this is why the struct constraint loses nothing)
    [Fact]
    public void RecursiveTargetThroughStructSurrogate()
    {
        var tree = new RecursiveNode(1,
            new RecursiveNode(2, new RecursiveNode(4)),
            new RecursiveNode(3));
        foreach (var options in (MessagePack.MessagePackSerializerOptions[])[MessagePack.MessagePackSerializerOptions.Default, MessagePack.MessagePackSerializerOptions.DefaultAot])
        {
            var back = V4.Deserialize<RecursiveNode>(V4.Serialize(tree, options), options)!;
            Assert.Equal(1, back.Value);
            Assert.Equal(2, back.Children.Length);
            Assert.Equal(4, back.Children[0].Children[0].Value);
            Assert.Equal(3, back.Children[1].Value);
            Assert.Empty(back.Children[1].Children);
        }
    }

    // registry-free composition still works (the STJ-style explicit chain)
    [Fact]
    public void FactoryComposesIntoExplicitChain()
    {
        var options = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new SurrogateFormatterFactory<ThirdPartyPoint, ThirdPartyPointSurrogate>(),
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
        ]));
        var back = V4.Deserialize<ThirdPartyPoint>(V4.Serialize(new ThirdPartyPoint(3, 4), options), options)!;
        Assert.Equal(4, back.Y);
    }

    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "SurrogateProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Fact]
    public void DuplicateSurrogates_ReportMsgPack018()
    {
        var result = RunGenerator("""
            using MessagePack;
            public class Target { public int X; }
            [MessagePackObject]
            public record struct SurrogateA([property: Key(0)] int X) : IMessagePackSurrogate<Target, SurrogateA>
            {
                public SurrogateA ToSurrogate(Target value) => new(value.X);
                public Target ToTarget() => new() { X = X };
            }
            [MessagePackObject]
            public record struct SurrogateB([property: Key(0)] int X) : IMessagePackSurrogate<Target, SurrogateB>
            {
                public SurrogateB ToSurrogate(Target value) => new(value.X);
                public Target ToTarget() => new() { X = X };
            }
            """);
        Assert.Equal(2, result.Diagnostics.Count(d => d.Id == "MsgPack018"));
    }

    [Fact]
    public void TargetWithOwnDeclaration_ReportsMsgPack019()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            public class Owned { [Key(0)] public int X { get; set; } }
            [MessagePackObject]
            public record struct OwnedSurrogate([property: Key(0)] int X) : IMessagePackSurrogate<Owned, OwnedSurrogate>
            {
                public OwnedSurrogate ToSurrogate(Owned value) => new(value.X);
                public Owned ToTarget() => new() { X = X };
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack019" && d.GetMessage().Contains("wins"));
    }

    [Fact]
    public void GenericSurrogate_ReportsMsgPack019()
    {
        var result = RunGenerator("""
            using MessagePack;
            public class Box<T> { public T? Item; }
            [MessagePackObject]
            public record struct BoxSurrogate<T>([property: Key(0)] T? Item) : IMessagePackSurrogate<Box<T>, BoxSurrogate<T>>
            {
                public BoxSurrogate<T> ToSurrogate(Box<T> value) => new(value.Item);
                public Box<T> ToTarget() => new() { Item = Item };
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack019" && d.GetMessage().Contains("generic"));
    }
}

// constructor-enforced invariant: the surrogate path must preserve it on deserialize.
// NO attribute anywhere on this type: implementing IMessagePackSurrogate on the surrogate
// below is the whole declaration (auto-discovered and auto-registered)
public class SurrogateUser
{
    public int Value { get; }
    public string Realm { get; }

    public SurrogateUser(int value, string realm)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
        Realm = realm;
    }
}

// the V3-aliased attributes serve both serializers (matched by full name)
[V3::MessagePack.MessagePackObject]
public readonly record struct SurrogateUserSurrogate(
    [property: V3::MessagePack.Key(0)] int Value,
    [property: V3::MessagePack.Key(1)] string Realm)
    : IMessagePackSurrogate<SurrogateUser, SurrogateUserSurrogate>
{
    public SurrogateUserSurrogate ToSurrogate(SurrogateUser value) => new(value.Value, value.Realm);

    public SurrogateUser ToTarget() => new(Value, Realm);
}

// struct T through a surrogate: the nil-means-null branch folds away
public readonly struct SurrogateTick(long ticks)
{
    public long Ticks { get; } = ticks;
}

[MessagePackObject]
public readonly record struct SurrogateTickSurrogate(
    [property: Key(0)] long Ticks)
    : IMessagePackSurrogate<SurrogateTick, SurrogateTickSurrogate>
{
    public SurrogateTickSurrogate ToSurrogate(SurrogateTick value) => new(value.Ticks);

    public SurrogateTick ToTarget() => new(Ticks);
}

[MessagePackObject]
public class SurrogateHolder
{
    [Key(0)] public SurrogateUser? User { get; set; }
    [Key(1)] public SurrogateTick Stamp { get; set; }
}

// recursive, unannotated target: serialized through the struct surrogate below
public class RecursiveNode(int value, params RecursiveNode[] children)
{
    public int Value { get; } = value;
    public RecursiveNode[] Children { get; } = children;
}

[MessagePackObject]
public readonly record struct RecursiveNodeSurrogate(
    [property: Key(0)] int Value,
    [property: Key(1)] RecursiveNode[] Children)
    : IMessagePackSurrogate<RecursiveNode, RecursiveNodeSurrogate>
{
    public RecursiveNodeSurrogate ToSurrogate(RecursiveNode value) => new(value.Value, value.Children);

    public RecursiveNode ToTarget() => new(Value, Children);
}

// stands in for a type from an assembly the user cannot annotate
public class ThirdPartyPoint(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
}

[MessagePackObject]
public readonly record struct ThirdPartyPointSurrogate(
    [property: Key(0)] int X,
    [property: Key(1)] int Y)
    : IMessagePackSurrogate<ThirdPartyPoint, ThirdPartyPointSurrogate>
{
    public ThirdPartyPointSurrogate ToSurrogate(ThirdPartyPoint value) => new(value.X, value.Y);

    public ThirdPartyPoint ToTarget() => new(X, Y);
}

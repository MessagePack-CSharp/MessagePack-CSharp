extern alias V3;
using MessagePack;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SerializerFoundation;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [MessagePackSerializable<T>] on a partial factory class (the JsonSerializerContext
// pattern) declares serialization ROOTS the member-graph harvest cannot see: Person[] or
// List<Person> passed straight to Serialize. The generated half derives
// MessagePackFormatterFactory, statically constructs the harvested closure, and
// auto-registers it, so DefaultAot resolves the roots; Instance also composes into
// explicit chains for registry-free resolution.
public class SerializableRootTests
{
    static readonly MessagePack.MessagePackSerializerOptions aot = MessagePack.MessagePackSerializerOptions.DefaultAot;

    [Fact]
    public void ArrayRootMatchesDefaultAndOracle()
    {
        RootPerson[] value = [new RootPerson { Id = 1, Name = "a" }, new RootPerson { Id = 300, Name = null }];
        var bytes = V4.Serialize(value, aot);
        Assert.Equal(V4.Serialize(value), bytes);
        Assert.Equal(Oracle.Serialize(value), bytes);

        var back = V4.Deserialize<RootPerson[]>(bytes, aot)!;
        Assert.Equal(2, back.Length);
        Assert.Equal(1, back[0].Id);
        Assert.Equal("a", back[0].Name);
        Assert.Equal(300, back[1].Id);
        Assert.Null(back[1].Name);
    }

    [Fact]
    public void ListRootRoundtrip()
    {
        List<RootPerson> value = [new RootPerson { Id = 7, Name = "list" }];
        var bytes = V4.Serialize(value, aot);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(7, V4.Deserialize<List<RootPerson>>(bytes, aot)![0].Id);
    }

    // the typeof attribute form, and a root whose element is a closed generic
    // [MessagePackObject] (exercises the open-model join in the generated factory)
    [Fact]
    public void DictionaryAndGenericElementRootsRoundtrip()
    {
        var dictionary = new Dictionary<string, RootPerson> { ["k"] = new RootPerson { Id = 5, Name = "d" } };
        var dictionaryBytes = V4.Serialize(dictionary, aot);
        Assert.Equal(Oracle.Serialize(dictionary), dictionaryBytes);
        Assert.Equal(5, V4.Deserialize<Dictionary<string, RootPerson>>(dictionaryBytes, aot)!["k"].Id);

        List<RootBox<int>> boxes = [new RootBox<int> { Item = 42 }];
        var boxBytes = V4.Serialize(boxes, aot);
        Assert.Equal(Oracle.Serialize(boxes), boxBytes);
        Assert.Equal(42, V4.Deserialize<List<RootBox<int>>>(boxBytes, aot)![0].Item);
    }

    // STJ-style explicit composition: the factory Instance resolves its roots without the
    // global registry tier in the chain
    [Fact]
    public void InstanceComposesIntoExplicitChain()
    {
        var options = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            SandboxRootFactory.Instance,
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
        ]));
        RootPerson[] value = [new RootPerson { Id = 9, Name = "x" }];
        var back = V4.Deserialize<RootPerson[]>(V4.Serialize(value, options), options)!;
        Assert.Equal(9, back[0].Id);
    }

    // an undeclared collection-shaped root fails with a message that teaches the fix
    [Fact]
    public void UndeclaredRootMissTeachesTheAttribute()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => V4.Serialize(new List<RootPerson[]>(), aot));
        Assert.Contains("MessagePackSerializable", exception.Message);
    }

    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "SerializableRootProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Fact]
    public void NonPartialClass_ReportsMsgPack016()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackSerializable<int[]>]
            public class NotPartial
            {
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack016" && d.GetMessage().Contains("partial"));
    }

    [Fact]
    public void ForeignBaseClass_ReportsMsgPack016()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackSerializable<int[]>]
            public partial class WrongBase : System.Exception
            {
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack016" && d.GetMessage().Contains("derive"));
    }

    [Fact]
    public void OpenGenericRoot_ReportsMsgPack017()
    {
        var result = RunGenerator("""
            using MessagePack;
            using System.Collections.Generic;
            [MessagePackSerializable(typeof(List<>))]
            public partial class OpenRoot
            {
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack017");
    }

    [Fact]
    public void BothAttributeFormsMergeIntoOneFactory()
    {
        var result = RunGenerator("""
            using MessagePack;
            using System.Collections.Generic;
            [MessagePackObject]
            public class Model { [Key(0)] public int X { get; set; } }
            [MessagePackSerializable<Model[]>]
            [MessagePackSerializable(typeof(List<Model>))]
            public partial class MergedRoots
            {
            }
            """);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var factory = Assert.Single(result.GeneratedTrees, t => t.FilePath.EndsWith("MergedRoots.g.cs"));
        var text = factory.ToString();
        Assert.Contains("typeof(global::Model[])", text);
        Assert.Contains("typeof(global::System.Collections.Generic.List<global::Model>)", text);
        Assert.Contains("RegisterMessagePackRoots", text);
    }
}

// the V3-aliased attributes serve both serializers (matched by full name)
[V3::MessagePack.MessagePackObject]
public class RootPerson
{
    [V3::MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1)] public string? Name { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class RootBox<T>
{
    [V3::MessagePack.Key(0)] public T? Item { get; set; }
}

[MessagePackSerializable<RootPerson[]>]
[MessagePackSerializable<List<RootPerson>>]
[MessagePackSerializable(typeof(Dictionary<string, RootPerson>))]
[MessagePackSerializable<List<RootBox<int>>>]
public partial class SandboxRootFactory;

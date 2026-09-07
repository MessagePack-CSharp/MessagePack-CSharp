using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack101 against the REAL MessagePack surface (not stubs): the analyzer's serializable
// set is harvested from the referenced assembly's IMessagePackFormatter implementations,
// so these tests double as a check that the harvest understands the actual formatter
// shapes (closed BCL formatters, generic collection patterns, array ranks, Nullable).
public class SerializableMemberTypeAnalyzerTest
{
    static Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string source) =>
        AnalyzerTestHost.RunAnalyzerAsync(new SerializableMemberTypeAnalyzer(), source);

    [Fact]
    public async Task UnannotatedMemberType_Reports()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Plain Value { get; set; }
            }
            public class Plain { }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SerializableMemberTypeAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("Plain", diagnostic.GetMessage());
        Assert.Contains("Holder.Value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task BuiltInAndCollectionMembers_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int I { get; set; }
                [Key(1)] public string S { get; set; }
                [Key(2)] public DateTime Stamp { get; set; }
                [Key(3)] public Guid Id { get; set; }
                [Key(4)] public int[] Array1 { get; set; }
                [Key(5)] public double[,] Array2 { get; set; }
                [Key(6)] public List<string> List { get; set; }
                [Key(7)] public Dictionary<string, List<int>> Map { get; set; }
                [Key(8)] public DayOfWeek Enum { get; set; }
                [Key(9)] public int? MaybeInt { get; set; }
                [Key(10)] public DayOfWeek? MaybeEnum { get; set; }
                [Key(11)] public (int, string) Pair { get; set; }
                [Key(12)] public KeyValuePair<string, decimal> Kv { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnnotatedAndUnionMemberTypes_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Child Child { get; set; }
                [Key(1)] public IShape Shape { get; set; }
            }
            [MessagePackObject]
            public class Child
            {
                [Key(0)] public int X { get; set; }
            }
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public interface IShape { }
            [MessagePackObject]
            public class Circle : IShape
            {
                [Key(0)] public double R { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GenericBinding_ReportsTheInnerType()
    {
        // List<Plain> is structurally served by ListFormatter; the WILDCARD binding is
        // what fails, and the message must name the inner type
        var diagnostics = await RunAnalyzerAsync("""
            using System.Collections.Generic;
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public List<Plain> Values { get; set; }
            }
            public class Plain { }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("'Plain'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task UnannotatedSerializePayload_ReportsUsageSite()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            public static class Caller
            {
                public static byte[] Run() => MessagePackSerializer.Serialize(new Plain());
            }
            public class Plain { public int X { get; set; } }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SerializableMemberTypeAnalyzer.UsageDiagnosticId, diagnostic.Id);
        Assert.Contains("Plain", diagnostic.GetMessage());
        Assert.Contains("Serialize", diagnostic.GetMessage());
    }

    [Fact]
    public async Task AnnotatedAndCollectionPayloads_SilentAtUsageSites()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using System.Collections.Generic;
            using MessagePack;
            public static class Caller
            {
                public static void Run()
                {
                    MessagePackSerializer.Serialize(new Annotated());
                    MessagePackSerializer.Serialize(new List<int> { 1 });
                    MessagePackSerializer.Deserialize<Annotated>(System.Array.Empty<byte>());
                }
            }
            [MessagePackObject]
            public class Annotated { [Key(0)] public int X { get; set; } }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CustomOptionsPayload_Silent()
    {
        // a custom options argument swaps in its own resolver chain (contractless etc.),
        // which the analyzer cannot see into — the call site is out of MsgPack108's scope
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            public static class Caller
            {
                static MessagePackSerializerOptions custom = null!;
                public static void Run()
                {
                    MessagePackSerializer.Serialize(new Plain(), custom);
                    MessagePackSerializer.Deserialize<Plain>(System.Array.Empty<byte>(), custom);
                }
            }
            public class Plain { public int X { get; set; } }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ExplicitDefaultOptionsPayload_Reports()
    {
        // spelling out MessagePackSerializerOptions.Default IS the default chain — the
        // check the analyzer models — so the warning stays
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            public static class Caller
            {
                public static byte[] Run() => MessagePackSerializer.Serialize(new Plain(), MessagePackSerializerOptions.Default);
            }
            public class Plain { public int X { get; set; } }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SerializableMemberTypeAnalyzer.UsageDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task CollectionPayloadWithUnannotatedElement_ReportsTheElement()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using System.Collections.Generic;
            using MessagePack;
            public static class Caller
            {
                public static byte[] Run() => MessagePackSerializer.Serialize(new List<Plain>());
            }
            public class Plain { public int X { get; set; } }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SerializableMemberTypeAnalyzer.UsageDiagnosticId, diagnostic.Id);
        Assert.Contains("'Plain'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DataContractMemberType_Silent()
    {
        // [DataContract] types are served by the Default chain's reflection tail
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            using System.Runtime.Serialization;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Contracted Value { get; set; }
            }
            [DataContract]
            public class Contracted
            {
                [DataMember(Order = 0)] public int X { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task KnownType_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [assembly: MessagePackKnownType(typeof(Plain))]
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Plain Value { get; set; }
            }
            public class Plain { }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MemberLevelFormatterOverride_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)]
                [MessagePackFormatter(typeof(object))]
                public Plain Value { get; set; }
            }
            public class Plain { }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CustomFormatterInTheSameAssembly_Silent()
    {
        // a hand-written formatter is harvested from the user's own compilation, no
        // declaration needed
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            using SerializerFoundation;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Plain Value { get; set; }
            }
            public class Plain { }
            public sealed class PlainFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Plain>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Plain value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Plain value) { }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnionSubtypeWithoutAnnotation_Reports()
    {
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(BareShape), 0)]
            public interface IShape { }
            public class BareShape : IShape { }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("BareShape", diagnostic.GetMessage());
        Assert.Contains("[UnionTag]", diagnostic.GetMessage());
    }

    [Fact]
    public async Task IgnoredAndUnkeyedMembers_NotChecked()
    {
        // [IgnoreMember] never rides the wire; delegates would otherwise be an easy hit
        var diagnostics = await RunAnalyzerAsync("""
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int X { get; set; }
                [IgnoreMember] public System.Action Callback { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }
}

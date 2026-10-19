using System.Collections;
using System.Collections.Immutable;
using MessagePack.SourceGenerator.Analyzers;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Analyzer and public-API behavior from the preview1 review's minor findings (F66, F67, F68, F71, F95, F97, F103).
public class AnalyzerFindingsTests
{
    static Task<ImmutableArray<Diagnostic>> MemberTypes(string source) =>
        AnalyzerTestHost.RunAnalyzerAsync(new SerializableMemberTypeAnalyzer(), source);

    // ---- F66: MsgPack108 judges the payload type argument, never the buffer ----

    [Fact]
    public async Task EntryCall_BufferTypeArgument_IsNotThePayload()
    {
        var diagnostics = await MemberTypes("""
            using MessagePack;
            using SerializerFoundation;
            public class Plain { }
            public static class Caller
            {
                public static void Run()
                {
                    var buffer = new CompatibleArrayPoolListWriteBuffer();
                    MessagePackSerializer.Serialize(typeof(Plain), ref buffer, new Plain(), MessagePackSerializerOptions.Default); // Type-based: judged nowhere
                    MessagePackSerializer.Serialize<CompatibleArrayPoolListWriteBuffer, Plain>(ref buffer, new Plain(), MessagePackSerializerOptions.Default); // the payload is Plain
                }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SerializableMemberTypeAnalyzer.UsageDiagnosticId, diagnostic.Id);
        Assert.Contains("Plain", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.DoesNotContain("CompatibleArrayPoolListWriteBuffer", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    // ---- F67: an unannotated derived type of a [MessagePackObject] base is served by the Default chain ----

    [Fact]
    public async Task InheritedObjectAttribute_SatisfiesMsgPack101()
    {
        var diagnostics = await MemberTypes("""
            using MessagePack;
            [MessagePackObject] public class Base { [Key(0)] public int A { get; set; } }
            public sealed class Derived : Base { }
            [MessagePackObject] public class Holder { [Key(0)] public Derived? Value { get; set; } }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task InheritedObjectAttribute_KeepsKeysAlive_ForMsgPack110AndMsgPack111()
    {
        var ineffective = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject] public class Base { [Key(0)] public int A { get; set; } }
            public sealed class Derived : Base { [Key(1)] public int B { get; set; } }
            """);
        Assert.Empty(ineffective);

        var sparse = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject] public class Base { [Key(0)] public int A { get; set; } }
            public sealed class Derived : Base { [Key(1000)] public int B { get; set; } }
            """);
        Assert.Contains(sparse, static d => d.Id == SparseIntKeyAnalyzer.DiagnosticId);
    }

    // ---- F68: a struct collection with a collection-accepting constructor ----

    public readonly struct IntBag : IEnumerable<int>
    {
        readonly int[] items;

        public IntBag(IEnumerable<int> items) => this.items = items.ToArray();

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [MessagePackObject]
    public class BagHolder
    {
        [Key(0)]
        public IntBag Bag { get; set; }
    }

    [Fact]
    public async Task StructCollection_WithCollectionConstructor_PassesMsgPack101()
    {
        var diagnostics = await MemberTypes("""
            using System.Collections;
            using System.Collections.Generic;
            using MessagePack;
            public readonly struct IntBag : IEnumerable<int>
            {
                readonly int[] items;
                public IntBag(IEnumerable<int> items) { this.items = System.Linq.Enumerable.ToArray(items); }
                public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)items).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            [MessagePackObject] public class Holder { [Key(0)] public IntBag Bag { get; set; } }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StructCollection_RoundTrips_OnDefaultAot()
    {
        var holder = new BagHolder { Bag = new IntBag([1, 2, 3]) };
        var aot = MessagePackSerializerOptions.DefaultAot;
        var back = V4.Deserialize<BagHolder>(V4.Serialize(holder, aot), aot)!;
        Assert.Equal([1, 2, 3], back.Bag);
        Assert.Equal(V4.Serialize(holder), V4.Serialize(holder, aot));
    }

    // ---- F71: MsgPack115 sees the primitive header decoders; MsgPack113 sees a descent split into a helper ----

    [Fact]
    public async Task TryReadHeaderPrimitive_WithStateInScope_ReportsMsgPack115()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DeclaredElementBudgetAnalyzer(), """
            using MessagePack;
            using SerializerFoundation;
            public sealed class RawFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int[] value) => buffer.WriteNil();
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[] value)
                {
                    MessagePackPrimitives.TryReadArrayHeader(buffer.GetUnreadSpan(), out var count, out var tokenSize);
                    buffer.Advance(tokenSize);
                    value = new int[count];
                }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DeclaredElementBudgetAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("buffer.ReadArrayHeader(ref state)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DescentThroughHelper_WithoutEnter_ReportsMsgPack113()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), """
            using MessagePack;
            using SerializerFoundation;
            public sealed class ItemsFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                IMessagePackFormatter<TWriteBuffer, TReadBuffer, int> inner = null!;
                public void Initialize(MessagePackFormatterResolver resolver) => inner = resolver.GetFormatter<TWriteBuffer, TReadBuffer, int>();
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int[] value)
                {
                    buffer.WriteArrayHeader(value.Length);
                    WriteItems(ref buffer, ref state, value); // the descent lives in the helper
                }
                void WriteItems(ref TWriteBuffer buffer, ref SerializeState state, int[] value)
                {
                    foreach (var item in value) inner.Serialize(ref buffer, ref state, item);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[] value) => buffer.Skip();
            }
            """);
        Assert.Contains(diagnostics, static d => d.Id == DepthTrackingAnalyzer.MissingEnterDiagnosticId && d.GetMessage().Contains(".Serialize", StringComparison.Ordinal));
    }

    // ---- F95: a formatter with its own constraints gets MsgPack028, with the clauses to add ----

    [Fact]
    public void ConstrainedFormatter_GetsMsgPack028_WithTheClauses()
    {
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            using SerializerFoundation;
            public sealed partial class EnumNameFormatter<TWriteBuffer, TReadBuffer, TEnum> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TEnum>
                where TEnum : struct, System.Enum
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TEnum value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TEnum value) { }
            }
            """, "ConstraintProbe");
        var driver = CSharpGeneratorDriver.Create([new BufferConstraintsGenerator().AsSourceGenerator()], parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MsgPack028", diagnostic.Id);
        Assert.Contains("where TWriteBuffer : struct, IWriteBuffer", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("where TReadBuffer : struct, IReadBuffer", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    // ---- F97: the not-found hint spells the type in C# and in the typeof form ----

    public class Unregistered
    {
        public int X { get; set; }
    }

    [Fact]
    public void NotFoundHint_SpellsTheTypeInCSharp()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => V4.Serialize(new List<Unregistered>(), MessagePackSerializerOptions.DefaultAot));
        Assert.Contains("[MessagePackSerializable(typeof(List<AnalyzerFindingsTests.Unregistered>))]", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("[MessagePackSerializable<List`1>]", ex.Message, StringComparison.Ordinal); // the old hint spelling
    }

    // ---- F103: options are required wherever an overload takes them ----

    [Fact]
    public void NullOptions_ThrowArgumentNullException()
    {
        byte[] payload = [0x01];
        Assert.Throws<ArgumentNullException>(() => V4.Deserialize<int>(payload, null!));
        Assert.Throws<ArgumentNullException>(() => V4.Serialize(typeof(int), 1, null!));
        Assert.Throws<ArgumentNullException>(() => V4.Deserialize<int>(new MemoryStream(payload), null!));
        Assert.Throws<ArgumentNullException>(() => new MessagePackSerializerOptions((MessagePackFormatterResolver)null!));
        Assert.Throws<ArgumentNullException>(() => new MessagePackFormatterResolver((MessagePackFormatterFactory)null!));
        Assert.Throws<ArgumentException>(() => MessagePackFormatterFactory.Combine(BuiltInFormatterFactory.Instance, null!));
        Assert.Throws<ArgumentException>(() => TypelessTypeLoader.AllowedTypes(typeof(int), null!));
    }
}

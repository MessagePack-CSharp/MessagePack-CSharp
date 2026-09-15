using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// Regression coverage for the pre-release review findings (September 2026). Each test pins a failure that was
// reproduced against the tree at the time (an infinite header walk, a double pool return, a CPU amplification from
// eight bytes, data no reader accepts, a registration the generator dropped) and the behavior the fix restores.
public class ReviewFindingsRegressionTests
{
    static readonly V4Options ReflectionOptions = new(new MessagePackFormatterResolver(
    [
        BuiltInFormatterFactory.Instance,
        GenericFormatterFactory.Instance,
        new ReflectionFormatterFactory(annotatedOnly: true),
    ]));

    // ---- skippable frames: `8 + uint` is uint arithmetic, and 8 + 0xFFFFFFF8 wrapped the offset to 0 ----

    // skippable magic 0x184D2A50, declared length 0xFFFFFFF8
    static readonly byte[] WrappingSkippable = [0x50, 0x2A, 0x4D, 0x18, 0xF8, 0xFF, 0xFF, 0xFF];

    static bool FinishesWithin(Action action, TimeSpan timeout) => Task.Run(action).Wait(timeout);

    [Fact]
    public void Lz4_SkippableFrameLengthNearUInt32Max_DoesNotLoop()
    {
        var processor = new Lz4FrameProcessor();
        var found = true;
        var length = 0L;
        Assert.True(FinishesWithin(() => found = processor.TryFindMessageEnd(new ReadOnlySequence<byte>(WrappingSkippable), out length), TimeSpan.FromSeconds(10)), "the header walk did not return");
        Assert.False(found);
        Assert.Equal(8L + 0xFFFFFFF8L + 4, length); // the walk asked for the magic after the 4GB skippable payload
    }

    [Fact]
    public void Zstandard_SkippableFrameLengthNearUInt32Max_DoesNotLoop()
    {
        var processor = new ZstandardFrameProcessor();
        var found = true;
        var length = 0L;
        Assert.True(FinishesWithin(() => found = processor.TryFindMessageEnd(new ReadOnlySequence<byte>(WrappingSkippable), out length), TimeSpan.FromSeconds(10)), "the header walk did not return");
        Assert.False(found);
        Assert.Equal(8L + 0xFFFFFFF8L + 4, length);
    }

    // ---- Zstandard unsized frame over the cap: the buffer went back to the pool twice ----

    [Fact]
    public void Zstandard_UnsizedFrameOverCap_ReturnsTheBufferOnce()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i % 100;
        }
        var frame = UnsizedZstandardFrame(V4.Serialize(data, V4Options.Default));
        var small = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: 1024);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(frame, small));

        // the growth buffer is the 1024 bucket; a double return hands one array to the next two renters
        var first = ArrayPool<byte>.Shared.Rent(1024);
        var second = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            Assert.NotSame(first, second);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(second);
            ArrayPool<byte>.Shared.Return(first);
        }
    }

    // a frame without a content size in its header: compress in a non-final call first, so the encoder cannot
    // pledge the total (the way streamed input comes out of the zstd tool)
    static byte[] UnsizedZstandardFrame(byte[] plain)
    {
#if NET11_0_OR_GREATER
        var encoder = new System.IO.Compression.ZstandardEncoder(3);
#else
        var encoder = new NativeCompressions.ZstandardEncoder(3);
#endif
        try
        {
            var destination = new byte[plain.Length + 1024];
            var source = plain.AsSpan();
            encoder.Compress(source.Slice(0, source.Length / 2), destination, out var consumed, out var written, isFinalBlock: false);
            var total = written;
            encoder.Compress(source.Slice(consumed), destination.AsSpan(total), out _, out written, isFinalBlock: true);
            total += written;
            return destination.AsSpan(0, total).ToArray();
        }
        finally
        {
            encoder.Dispose();
        }
    }

    // ---- multi-dimensional arrays: a zero-element array with one huge dimension walked 2^31 empty iterations ----

    static readonly byte[] MaxDimension = [0xD2, 0x7F, 0xFF, 0xFF, 0xC7]; // int32 0x7FFFFFC7 = 2147483591, the CLR's per-dimension limit

    static void AssertZeroElementArrayIsCheap<T>(byte[] payload, int rank) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var result = V4.Deserialize<T>(payload)!;
        stopwatch.Stop();
        var array = (Array)(object)result;
        Assert.Equal(rank, array.Rank);
        Assert.Equal(0, array.Length);
        Assert.Equal(0x7FFFFFC7, array.GetLength(0));
        // the walk took about 1.7 seconds before the early return; nothing is read now
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"{rank}D zero-element array took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void ZeroElementMultiDimensionalArray_DoesNotWalkItsDimensions()
    {
        AssertZeroElementArrayIsCheap<int[,]>([0x93, .. MaxDimension, 0x00, 0x90], 2);
        AssertZeroElementArrayIsCheap<int[,,]>([0x94, .. MaxDimension, 0x00, .. MaxDimension, 0x90], 3);
        AssertZeroElementArrayIsCheap<int[,,,]>([0x95, .. MaxDimension, 0x00, .. MaxDimension, .. MaxDimension, 0x90], 4);
    }

    // ---- SerializeElementsAsync with a container-format processor wrote an array header no reader accepts ----

    static async IAsyncEnumerable<int> ThreeAsync()
    {
        await Task.Yield();
        yield return 1;
        yield return 2;
        yield return 3;
    }

    [Fact]
    public async Task SerializeElementsAsync_ContainerProcessor_IsRefusedBeforeTheHeader()
    {
        foreach (var options in new[] { V4Options.Default.WithLz4Frame(), V4Options.Default.WithZstandardFrame() })
        {
            var pipe = new Pipe();
            await Assert.ThrowsAsync<NotSupportedException>(() => V4.SerializeElementsAsync(pipe.Writer, new[] { 1, 2, 3 }, 3, options));
            await Assert.ThrowsAsync<NotSupportedException>(() => V4.SerializeElementsAsync(pipe.Writer, ThreeAsync(), 3, options));
            Assert.Equal(0, pipe.Writer.UnflushedBytes); // not even the array header
            await pipe.Writer.CompleteAsync();
        }
    }

    // ---- an exposable MemoryStream skipped the cancellation check the pipe path performs ----

    [Fact]
    public async Task DeserializeAsync_ExposableMemoryStream_ObservesACanceledToken()
    {
        var bytes = V4.Serialize(42);
        var stream = new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);
        Assert.True(stream.TryGetBuffer(out _));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await V4.DeserializeAsync<int>(stream, V4Options.Default, new CancellationToken(canceled: true)));
        Assert.Equal(0, stream.Position);
    }

    // ---- the async enumerations observed the token only in ReadAsync, so a buffered batch kept flowing ----

    [Fact]
    public async Task DeserializeMessagesAsync_CancellationBetweenBufferedMessages_Stops()
    {
        var pipe = new Pipe();
        foreach (var value in new[] { 1, 2, 3 })
        {
            V4.Serialize(pipe.Writer, value);
        }
        await pipe.Writer.FlushAsync();
        await pipe.Writer.CompleteAsync();

        using var cancellation = new CancellationTokenSource();
        var enumerator = V4.DeserializeMessagesAsync<int>(pipe.Reader, cancellation.Token).GetAsyncEnumerator();
        await using (enumerator.ConfigureAwait(false))
        {
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(1, enumerator.Current);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
        }
    }

    [Fact]
    public async Task DeserializeElementsAsync_CancellationBetweenBufferedElements_Stops()
    {
        var pipe = new Pipe();
        await V4.SerializeElementsAsync(pipe.Writer, new[] { 1, 2, 3 }, 3);
        await pipe.Writer.CompleteAsync();

        using var cancellation = new CancellationTokenSource();
        var enumerator = V4.DeserializeElementsAsync<int>(pipe.Reader, cancellation.Token).GetAsyncEnumerator();
        await using (enumerator.ConfigureAwait(false))
        {
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(1, enumerator.Current);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
        }
    }

    // ---- the reflection tier replaced nested objects on populate; the generated formatters populate in place ----

    [Fact]
    public void ReflectionTier_PopulatesNestedObjectsInPlace()
    {
        byte[] payload = [0x91, 0x91, 0x07]; // [[7]]: the child array stops after A, so B is not on the wire
        foreach (var options in new[] { V4Options.Default, ReflectionOptions })
        {
            var child = new RfPopulateChild { A = 1, B = 99 };
            var value = new RfPopulateOuter { Child = child };
            V4.Deserialize(payload, ref value, options);
            Assert.Same(child, value.Child);
            Assert.Equal(7, child.A);
            Assert.Equal(99, child.B);
        }
    }

    // ---- a derived class hiding an inherited callback with `new` received the direct call ----

    [Fact]
    public void HiddenCallback_TheInterfaceImplementationRuns()
    {
        foreach (var options in new[] { V4Options.Default, ReflectionOptions })
        {
            var value = new CbHidingDerived { Value = 3 };
            var bytes = V4.Serialize(value, options);
            Assert.Equal("base", value.BeforeRanIn);

            var back = V4.Deserialize<CbHidingDerived>(bytes, options)!;
            Assert.Equal(3, back.Value);
            Assert.Equal("base", back.AfterRanIn);
        }
    }

    // ---- runtime-type dispatch (object, typeless) asked for the fast pair a Compatible-only formatter cannot serve ----

    static V4Options CompatibleOnlyOptions(bool typeless)
    {
        var factory = MessagePackFormatterFactory.Combine(
            new CompatiblePairOnlyFactory(),
            ObjectFallbackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance);
        return new V4Options(new MessagePackFormatterResolver(typeless ? factory.WithTypeless(TypelessTypeLoader.LoadAnyType()) : factory));
    }

    [Fact]
    public void ObjectDispatch_RoutesACompatibleOnlyFormatter()
    {
        var options = CompatibleOnlyOptions(typeless: false);
        var typed = V4.Serialize(new RoutedValue { X = 512 }, options);
        Assert.Equal(typed, V4.Serialize<object>(new RoutedValue { X = 512 }, options));
    }

    [Fact]
    public void TypelessDispatch_RoutesACompatibleOnlyFormatter()
    {
        var options = CompatibleOnlyOptions(typeless: true);
        var bytes = V4.Serialize<object>(new RoutedValue { X = 2048 }, options);
        var back = Assert.IsType<RoutedValue>(V4.Deserialize<object>(bytes, options));
        Assert.Equal(2048, back.X);
        back = Assert.IsType<RoutedValue>(V4.Deserialize<object>(new ReadOnlySequence<byte>(bytes), options));
        Assert.Equal(2048, back.X);
    }

    // ---- generator: closed generic roots harvested only themselves, unions dropped their generic cases, ----
    // ---- and the dynamic-code guard referenced an API netstandard2.0 does not have ----

    static string Generate(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "ReviewFindingsProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        return string.Join("\n", result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));
    }

    [Fact]
    public void ClosedGenericRoot_HarvestsTheSubstitutedMemberGraph()
    {
        var text = Generate("""
            using System.Collections.Generic;
            using MessagePack;
            namespace Probe;
            [MessagePackObject] public class Leaf { [Key(0)] public int Id { get; set; } }
            [MessagePackObject] public class Nested { [Key(0)] public int Id { get; set; } }
            [MessagePackObject] public class Box<T>
            {
                [Key(0)] public List<T>? Items { get; set; }
                [Key(1)] public Box<Nested>? Inner { get; set; }
                [IgnoreMember] public HashSet<T>? NotSerialized { get; set; }
            }
            [MessagePackSerializable<Box<Leaf>>] public partial class ProbeFactory { }
            """);
        Assert.Contains("typeof(global::Probe.Box<global::Probe.Leaf>)", text);
        Assert.Contains("typeof(global::System.Collections.Generic.List<global::Probe.Leaf>)", text); // List<T> under T = Leaf
        Assert.Contains("typeof(global::Probe.Box<global::Probe.Nested>)", text); // a closed member reachable only through the root
        Assert.Contains("typeof(global::System.Collections.Generic.List<global::Probe.Nested>)", text); // and its own members (List<int> would be BuiltIn-served, so unregistered)
        Assert.DoesNotContain("HashSet<global::Probe.Leaf>", text); // opted out
    }

    [Fact]
    public void PatternUnion_RegistersItsClosedGenericCase()
    {
        var text = Generate("""
            using MessagePack;
            namespace System.Runtime.CompilerServices { [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)] public sealed class UnionAttribute : Attribute { } }
            namespace Probe;
            [MessagePackObject] public class Leaf { [Key(0)] public int Id { get; set; } }
            [MessagePackObject] public class Box<T> { [Key(0)] public T? Item { get; set; } }
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Box<Leaf>), 0)]
            [UnionTag(typeof(int), 1)]
            public readonly struct Shape
            {
                readonly object? held;
                public Shape(Box<Leaf> value) => held = value;
                public Shape(int value) => held = value;
                public object? Value => held;
            }
            """);
        Assert.Contains("typeof(global::Probe.Box<global::Probe.Leaf>)", text);
    }

    [Fact]
    public void GenericFormatterFactory_GuardsTheDynamicCodeCheckByTargetFramework()
    {
        var text = Generate("""
            using MessagePack;
            namespace Probe;
            [MessagePackObject] public class Box<T> { [Key(0)] public T? Item { get; set; } }
            """);
        var lines = text.Split('\n').Select(static l => l.Trim()).ToArray();
        var guard = Array.FindIndex(lines, static l => l.Contains("RuntimeFeature.IsDynamicCodeSupported"));
        Assert.True(guard > 0, "the generated factory has no dynamic-code guard");
        Assert.Equal("#if NET5_0_OR_GREATER", lines[guard - 1]); // netstandard2.0 and .NET Framework lack the API
    }
}

[MessagePackObject]
public class RfPopulateOuter
{
    [Key(0)] public RfPopulateChild? Child { get; set; }
}

[MessagePackObject]
public class RfPopulateChild
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public int B { get; set; }
}

[MessagePackObject]
public class CbHidingBase : IMessagePackSerializationCallbackReceiver
{
    [Key(0)] public int Value { get; set; }
    [IgnoreMember] public string? BeforeRanIn { get; set; }
    [IgnoreMember] public string? AfterRanIn { get; set; }

    public void OnBeforeSerialize() => BeforeRanIn = "base";

    public void OnAfterDeserialize() => AfterRanIn = "base";
}

[MessagePackObject]
public class CbHidingDerived : CbHidingBase
{
    public new void OnBeforeSerialize() => BeforeRanIn = "hidden";

    public new void OnAfterDeserialize() => AfterRanIn = "hidden";
}

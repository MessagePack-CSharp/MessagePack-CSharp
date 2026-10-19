using System.Buffers;
using System.IO.Pipelines;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;


namespace MessagePack.Tests;

// Combinations the README describes but nothing exercised end to end: an envelope processor (WithFraming) under the
// element stream entry points, MaxDepth = 0 as "no depth check", and what the LZ4 Block envelope reader and the LZ4
// frame reader do with each other's output.
public class ProcessorCombinationTests
{
    static readonly V4Options Framed = V4Options.Default.WithFraming();
    static readonly V4Options Lz4Block = V4Options.Default.WithLz4Block();
    static readonly V4Options Lz4BlockArray = V4Options.Default.WithLz4BlockArray();
    static readonly V4Options Lz4Frame = V4Options.Default.WithLz4Frame();

    static PipeReader Feed(byte[] bytes) => PipeReader.Create(new ReadOnlySequence<byte>(bytes));

    static async Task<byte[]> ElementsAsync<T>(IEnumerable<T> source, int count, V4Options options)
    {
        var pipe = new Pipe();
        await V4.SerializeElementsAsync(pipe.Writer, source, count, options);
        await pipe.Writer.CompleteAsync();
        var result = await pipe.Reader.ReadAtLeastAsync(int.MaxValue);
        var bytes = result.Buffer.ToArray();
        pipe.Reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }

    static async Task<List<T>> ReadElementsAsync<T>(byte[] bytes, V4Options options)
    {
        var list = new List<T>();
        await foreach (var element in V4.DeserializeElementsAsync<T>(Feed(bytes), options))
        {
            list.Add(element);
        }
        return list;
    }

    // ---- WithFraming under the element stream ----

    [Fact]
    public async Task Framing_ElementStream_RoundTripsElementByElement()
    {
        var values = Enumerable.Range(0, 1000).Select(static i => new CircularNode { Value = i }).ToArray();
        var bytes = await ElementsAsync(values, values.Length, Framed);
        var back = await ReadElementsAsync<CircularNode>(bytes, Framed);
        Assert.Equal(values.Select(static v => v.Value), back.Select(static v => v.Value));

        // the same stream through the Stream entry points
        var stream = new MemoryStream();
        await V4.SerializeElementsAsync(stream, values, values.Length, Framed);
        Assert.Equal(bytes, stream.ToArray());
        stream.Position = 0;
        var fromStream = new List<CircularNode>();
        await foreach (var element in V4.DeserializeElementsAsync<CircularNode>(stream, Framed))
        {
            fromStream.Add(element);
        }
        Assert.Equal(values.Length, fromStream.Count);
    }

    [Fact]
    public async Task Framing_ElementStream_IsNotOneFramedArray_NorTheReverse()
    {
        // under an envelope processor the header and each element are wrapped on their own, so the element stream is
        // not the whole-array message and the whole-array message is not an element stream (README)
        int[] values = [1, 2, 3];
        var elementStream = await ElementsAsync(values, values.Length, Framed);
        var wholeArray = V4.Serialize(values, Framed);
        Assert.NotEqual(wholeArray, elementStream);

        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(elementStream, Framed));
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await ReadElementsAsync<int>(wholeArray, Framed));

        // without a processor the two forms are the same bytes, which is what makes the distinction above worth pinning
        Assert.Equal(V4.Serialize(values, V4Options.Default), await ElementsAsync(values, values.Length, V4Options.Default));
    }

    [Fact]
    public async Task ContainerProcessor_ElementStream_IsNotSupported()
    {
        // the LZ4 frame owns the message boundaries, so an element cannot be a message of its own
        var pipe = new Pipe();
        await Assert.ThrowsAsync<NotSupportedException>(() => V4.SerializeElementsAsync(pipe.Writer, new[] { 1, 2 }, 2, Lz4Frame));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await ReadElementsAsync<int>(V4.Serialize(new[] { 1, 2 }, Lz4Frame), Lz4Frame));
    }

    // ---- MaxDepth = 0 ----

    static byte[] NestedPayload(int levels)
    {
        var bytes = new byte[levels + 1];
        bytes.AsSpan(0, levels).Fill(0x91); // fixarray(1) per level
        bytes[levels] = 0xC0;
        return bytes;
    }

    static DepthNode Chain(int levels)
    {
        DepthNode? head = null;
        for (int i = 0; i < levels; i++)
        {
            head = new DepthNode { Child = head };
        }
        return head!;
    }

    static int Depth(DepthNode? node)
    {
        var depth = 0;
        for (; node is not null; node = node.Child)
        {
            depth++;
        }
        return depth;
    }

    // the depth check off, the stack guard is the only limit left, and the test runner's thread is not generous: the
    // README's advice for such data is a thread with a larger stack, so that is where this runs
    static void OnLargeStack(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, 64 * 1024 * 1024);
        thread.Start();
        thread.Join();
        if (caught is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(caught);
        }
    }

    [Fact]
    public void MaxDepthZero_DisablesTheDepthCheck_BothDirections() => OnLargeStack(() =>
    {
        const int Levels = 2000; // four times the default limit
        var unlimited = V4Options.Default with { MaxDepth = 0 };
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<DepthNode>(NestedPayload(Levels), V4Options.Default));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(Chain(Levels), V4Options.Default));

        Assert.Equal(Levels, Depth(V4.Deserialize<DepthNode>(NestedPayload(Levels), unlimited)));
        Assert.Equal(NestedPayload(Levels), V4.Serialize(Chain(Levels), unlimited));

        // the built-in collection formatters and the object slot take the same option
        var jagged = V4.Deserialize<object>(NestedPayload(Levels), unlimited);
        var depth = 0;
        for (var current = jagged; current is object[] array; current = array.Length == 0 ? null : array[0])
        {
            depth++;
        }
        Assert.Equal(Levels, depth);
    });

    [Fact]
    public void MaxDepthZero_StillStopsACycleBeforeTheStackOverflows()
    {
        // with the depth check off, the stack guard is what turns an unbounded recursion into an exception
        var unlimited = V4Options.Default with { MaxDepth = 0 };
        var cycle = new DepthNode();
        cycle.Child = cycle;
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                V4.Serialize(cycle, unlimited);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, 256 * 1024);
        thread.Start();
        thread.Join();
        var ex = Assert.IsType<MessagePackSerializationException>(caught);
        Assert.Contains("stack", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- LZ4 Block envelope reader vs LZ4 frame reader ----

    static int[] Compressible() => Enumerable.Range(0, 20_000).Select(static i => i % 100).ToArray();

    [Fact]
    public void Lz4Frame_ReadWithTheBlockEnvelopeOptions_IsRefusedForAnArrayRoot()
    {
        // the Block reader passes input that is not its ext envelope through to the plain reader (v3 behaviour, so a
        // v3 writer without compression still reads); an LZ4 frame's magic 0x04 0x22 0x4D 0x18 is then read as msgpack
        // and the array root does not match
        var frame = V4.Serialize(Compressible(), Lz4Frame);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(frame, Lz4Block));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(frame, Lz4BlockArray));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(frame, Lz4Block));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Dictionary<string, int>>(frame, Lz4Block));
    }

    [Fact]
    public void Lz4BlockEnvelope_ReadWithTheFrameOptions_IsRefused()
    {
        // the frame reader accepts only LZ4 frames: an ext envelope is not one, whatever it carries
        var block = V4.Serialize(Compressible(), Lz4Block);
        var blockArray = V4.Serialize(Compressible(), Lz4BlockArray);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(block, Lz4Frame));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(blockArray, Lz4Frame));
        // and plain msgpack is refused too: the frame options never pass input through
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(V4.Serialize(Compressible(), V4Options.Default), Lz4Frame));
    }

    [Fact]
    public void Lz4BlockEnvelopeOptions_ReadPlainMessagePack()
    {
        // the pass-through the two tests above rest on: Block options read uncompressed input (a v3 writer configured
        // without compression, or data written before compression was turned on)
        var plain = V4.Serialize(Compressible(), V4Options.Default);
        Assert.Equal(Compressible(), V4.Deserialize<int[]>(plain, Lz4Block));
        Assert.Equal(Compressible(), V4.Deserialize<int[]>(plain, Lz4BlockArray));
        // the Block and BlockArray readers also read each other's envelopes (both codes are recognized)
        Assert.Equal(Compressible(), V4.Deserialize<int[]>(V4.Serialize(Compressible(), Lz4Block), Lz4BlockArray));
        Assert.Equal(Compressible(), V4.Deserialize<int[]>(V4.Serialize(Compressible(), Lz4BlockArray), Lz4Block));
    }
}

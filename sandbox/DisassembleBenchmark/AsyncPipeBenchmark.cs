extern alias V3;
using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// Async streaming deserialization over a PipeReader, architectures compared:
//   V4           = 2-pass (resumable boundary scan, then sync parse of the slice)
//   MsgPackCSharp   = v3 MessagePackStreamReader (TrySkip framing from scratch per refill,
//                     caller deserializes the returned slice) over PipeReader.AsStream()
//   Nerdbank        = true async (isAsync threaded through converters)
// The writer is throttled (pauseWriterThreshold 256 with 256-byte chunks), so it can
// never run more than ~256 bytes ahead of what the reader examined — every operation
// crosses dozens of genuine await suspensions instead of degenerating into the
// fully-buffered sync fast paths. The sync parse speeds of the three libraries differ,
// so this is NOT a pure comparison of the async mechanisms — a reference, not a verdict.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AsyncPipeBenchmark
{
    const int MessageCount = 1000;
    const int ChunkSize = 256;

    readonly Nerdbank.MessagePack.MessagePackSerializer nbSerializer = new();
    byte[] messagesPayload = null!; // MessageCount concatenated NbPocoAsArray messages
    byte[] arrayPayload = null!;    // one array value holding MessageCount NbPocoAsArray

    [GlobalSetup]
    public void Setup()
    {
        var poco = new NbPocoAsArray { SomeInt = 42, SomeString = "Hello, World!" };
        var single = V3::MessagePack.MessagePackSerializer.Serialize(poco, V3::MessagePack.MessagePackSerializerOptions.Standard);
        messagesPayload = Enumerable.Repeat(single, MessageCount).SelectMany(b => b).ToArray();
        arrayPayload = V3::MessagePack.MessagePackSerializer.Serialize(
            Enumerable.Repeat(poco, MessageCount).ToArray(), V3::MessagePack.MessagePackSerializerOptions.Standard);

        // async paths are outside --verify, so self-check here: every competitor must
        // observe every message
        Check(nameof(Messages_V4), Messages_V4);
        Check(nameof(Messages_MsgPackCSharp), Messages_MsgPackCSharp);
        Check(nameof(Messages_Nerdbank), Messages_Nerdbank);
        Check(nameof(Array_V4), Array_V4);
        Check(nameof(Array_MsgPackCSharp), Array_MsgPackCSharp);
        Check(nameof(Single_V4), Single_V4);
        Check(nameof(Single_MsgPackCSharp), Single_MsgPackCSharp);
        Check(nameof(Single_Nerdbank), Single_Nerdbank);

        static void Check(string name, Func<Task<int>> operation)
        {
            int count;
            try
            {
                count = operation().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{name} failed: {exception.Message}", exception);
            }
            if (count != MessageCount)
            {
                throw new InvalidOperationException($"{name} saw {count} items, expected {MessageCount}");
            }
        }
    }

    #region messages (concatenated top-level values)

    [BenchmarkCategory("messages"), Benchmark(Baseline = true)]
    public async Task<int> Messages_MsgPackCSharp()
    {
        var reader = StartFeed(messagesPayload, out var writer);
        var count = 0;
        using (var streamReader = new V3::MessagePack.MessagePackStreamReader(reader.AsStream()))
        {
            while (await streamReader.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> message)
            {
                V3::MessagePack.MessagePackSerializer.Deserialize<NbPocoAsArray>(message, V3::MessagePack.MessagePackSerializerOptions.Standard);
                count++;
            }
        }
        await writer;
        return count;
    }

    [BenchmarkCategory("messages"), Benchmark]
    public async Task<int> Messages_V4()
    {
        var reader = StartFeed(messagesPayload, out var writer);
        var count = 0;
        await foreach (var item in MessagePack.MessagePackSerializer.DeserializeMessagesAsync<NbPocoAsArray>(reader))
        {
            count++;
        }
        await writer;
        return count;
    }

    [BenchmarkCategory("messages"), Benchmark]
    public async Task<int> Messages_Nerdbank()
    {
        var reader = StartFeed(messagesPayload, out var writer);
        var count = 0;
        await foreach (var item in nbSerializer.DeserializeEnumerableAsync<NbPocoAsArray>(reader, CancellationToken.None))
        {
            count++;
        }
        await writer;
        return count;
    }

    #endregion

    #region array (one top-level array, element streaming)

    [BenchmarkCategory("array"), Benchmark(Baseline = true)]
    public async Task<int> Array_MsgPackCSharp()
    {
        var reader = StartFeed(arrayPayload, out var writer);
        var count = 0;
        using (var streamReader = new V3::MessagePack.MessagePackStreamReader(reader.AsStream()))
        {
            var length = await streamReader.ReadArrayHeaderAsync(CancellationToken.None);
            for (var i = 0; i < length; i++)
            {
                if (await streamReader.ReadAsync(CancellationToken.None) is not ReadOnlySequence<byte> element)
                {
                    throw new InvalidOperationException("array ended early");
                }
                V3::MessagePack.MessagePackSerializer.Deserialize<NbPocoAsArray>(element, V3::MessagePack.MessagePackSerializerOptions.Standard);
                count++;
            }
        }
        await writer;
        return count;
    }

    [BenchmarkCategory("array"), Benchmark]
    public async Task<int> Array_V4()
    {
        var reader = StartFeed(arrayPayload, out var writer);
        var count = 0;
        await foreach (var item in MessagePack.MessagePackSerializer.DeserializeElementsAsync<NbPocoAsArray>(reader))
        {
            count++;
        }
        await writer;
        return count;
    }

    // no Nerdbank entry: 1.2.36's DeserializeEnumerableAsync frames concatenated
    // top-level values only (verified: it reads the whole top-level array as ONE
    // element and fails); element streaming inside an envelope arrived later as
    // DeserializePathEnumerableAsync + StreamingEnumerationOptions

    #endregion

    #region single (one large message)

    [BenchmarkCategory("single"), Benchmark(Baseline = true)]
    public async Task<int> Single_MsgPackCSharp()
    {
        var reader = StartFeed(arrayPayload, out var writer);
        var value = await V3::MessagePack.MessagePackSerializer.DeserializeAsync<NbPocoAsArray[]>(reader.AsStream(), V3::MessagePack.MessagePackSerializerOptions.Standard);
        await writer;
        return value!.Length;
    }

    [BenchmarkCategory("single"), Benchmark]
    public async Task<int> Single_V4()
    {
        var reader = StartFeed(arrayPayload, out var writer);
        var value = await MessagePack.MessagePackSerializer.DeserializeAsync<NbPocoAsArray[]>(reader);
        await writer;
        return value.Length;
    }

    [BenchmarkCategory("single"), Benchmark]
    public async Task<int> Single_Nerdbank()
    {
        var reader = StartFeed(arrayPayload, out var writer);
        var value = await nbSerializer.DeserializeAsync<NbPocoAsArray[], AsyncPipeWitness>(reader, CancellationToken.None);
        await writer;
        return value!.Length;
    }

    #endregion

    static PipeReader StartFeed(byte[] payload, out Task writerTask)
    {
        // resume 128/pause 256: the writer parks whenever ~256 unexamined bytes
        // accumulate, so the reader keeps hitting incomplete data and suspending
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 256, resumeWriterThreshold: 128, useSynchronizationContext: false));
        writerTask = FeedAsync(pipe.Writer, payload);
        return pipe.Reader;

        static async Task FeedAsync(PipeWriter writer, byte[] payload)
        {
            for (var offset = 0; offset < payload.Length; offset += ChunkSize)
            {
                var length = Math.Min(ChunkSize, payload.Length - offset);
                await writer.WriteAsync(payload.AsMemory(offset, length)).ConfigureAwait(false);
            }
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }
}

[PolyType.GenerateShapeFor<NbPocoAsArray[]>]
partial class AsyncPipeWitness;

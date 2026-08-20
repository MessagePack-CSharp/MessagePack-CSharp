using System.Buffers;
using System.IO.Pipelines;
using UltraMessagePack;

namespace UltraMessagePack.Tests;

// Async serialization is sync writing into pipe memory plus awaited flushes: batched by
// threshold, forced before the source suspends (liveness), stopping when the reader
// completes. These tests cover byte identity with the sync entries, streaming round
// trips through the async deserialize APIs, the declared-count contract of
// SerializeElementsAsync, and the flush-before-suspend rule.
public class AsyncSerializeTests
{
    static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Default;

    static async Task<byte[]> DrainAsync(PipeReader reader)
    {
        var result = await reader.ReadAsync();
        while (!result.IsCompleted)
        {
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            result = await reader.ReadAsync();
        }
        var bytes = result.Buffer.ToArray();
        reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }

    [Fact]
    public async Task SerializeAsync_MatchesSyncBytes()
    {
        var value = new object?[] { 1, "two", new object?[] { 3, 4 }, null };
        var pipe = new Pipe();
        await MessagePackSerializer.SerializeAsync(pipe.Writer, value, Options);
        pipe.Writer.Complete();
        Assert.Equal(MessagePackSerializer.Serialize(value, Options), await DrainAsync(pipe.Reader));
    }

    [Fact]
    public async Task SerializeAsync_LargeMessage_RoundtripsWithConcurrentReader()
    {
        // 200KB crosses the default 64KB pause threshold: the final flush commits the
        // bytes and then waits for the concurrent reader to consume them
        var value = new byte[200_000];
        new Random(42).NextBytes(value);
        var pipe = new Pipe();
        var readTask = MessagePackSerializer.DeserializeAsync<byte[]>(pipe.Reader, Options);
        await MessagePackSerializer.SerializeAsync(pipe.Writer, value, Options);
        pipe.Writer.Complete();
        Assert.Equal(value, await readTask);
    }

    [Fact]
    public async Task SerializeMessagesAsync_RoundtripsThroughDeserializeMessagesAsync()
    {
        var values = Enumerable.Range(0, 1000).Select(i => $"message-{i}").ToArray();
        var pipe = new Pipe();
        await MessagePackSerializer.SerializeMessagesAsync(pipe.Writer, values, Options);
        pipe.Writer.Complete();

        var seen = new List<string>();
        await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(pipe.Reader, Options))
        {
            seen.Add(message);
        }
        Assert.Equal(values, seen);
    }

    [Fact]
    public async Task SerializeMessagesAsync_AsyncSource_RoundtripsThroughDeserializeMessagesAsync()
    {
        static async IAsyncEnumerable<int> Source()
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 10 == 0)
                {
                    await Task.Yield(); // periodic genuine suspension exercises the flush-before-suspend path
                }
                yield return i;
            }
        }

        var pipe = new Pipe();
        var writeTask = MessagePackSerializer.SerializeMessagesAsync(pipe.Writer, Source(), Options);
        var seen = new List<int>();
        var readTask = Task.Run(async () =>
        {
            await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<int>(pipe.Reader, Options))
            {
                seen.Add(message);
            }
        });
        await writeTask;
        pipe.Writer.Complete();
        await readTask;
        Assert.Equal(Enumerable.Range(0, 100), seen);
    }

    [Fact]
    public async Task SerializeMessagesAsync_FlushesBeforeSourceSuspends()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async IAsyncEnumerable<string> Source()
        {
            yield return "first";
            await gate.Task;
            yield return "second";
        }

        var pipe = new Pipe();
        var writeTask = MessagePackSerializer.SerializeMessagesAsync(pipe.Writer, Source(), Options);
        var enumerator = MessagePackSerializer.DeserializeMessagesAsync<string>(pipe.Reader, Options).GetAsyncEnumerator();

        // "first" must reach the reader while the source is still parked on the gate
        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Equal("first", enumerator.Current);

        gate.SetResult();
        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Equal("second", enumerator.Current);

        await writeTask;
        pipe.Writer.Complete();
        Assert.False(await enumerator.MoveNextAsync());
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task SerializeElementsAsync_MatchesArraySerialize()
    {
        var values = Enumerable.Range(0, 500).ToArray();
        var pipe = new Pipe();
        await MessagePackSerializer.SerializeElementsAsync(pipe.Writer, values, values.Length, Options);
        pipe.Writer.Complete();
        Assert.Equal(MessagePackSerializer.Serialize(values, Options), await DrainAsync(pipe.Reader));
    }

    [Fact]
    public async Task SerializeElementsAsync_RoundtripsThroughDeserializeElementsAsync()
    {
        static async IAsyncEnumerable<string> Source()
        {
            for (int i = 0; i < 50; i++)
            {
                await Task.Yield();
                yield return $"element-{i}";
            }
        }

        var pipe = new Pipe();
        var writeTask = MessagePackSerializer.SerializeElementsAsync(pipe.Writer, Source(), 50, Options);
        var seen = new List<string>();
        var readTask = Task.Run(async () =>
        {
            await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<string>(pipe.Reader, Options))
            {
                seen.Add(element);
            }
        });
        await writeTask;
        pipe.Writer.Complete();
        await readTask;
        Assert.Equal(Enumerable.Range(0, 50).Select(i => $"element-{i}"), seen);
    }

    [Fact]
    public async Task SerializeElementsAsync_CountMismatch_Throws()
    {
        // fewer than declared
        var pipe = new Pipe();
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.SerializeElementsAsync(pipe.Writer, new[] { 1, 2, 3 }, 5, Options));

        // more than declared
        pipe = new Pipe();
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.SerializeElementsAsync(pipe.Writer, new[] { 1, 2, 3 }, 2, Options));

        // async source, fewer than declared
        static async IAsyncEnumerable<int> Three()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
            yield return 3;
        }
        pipe = new Pipe();
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.SerializeElementsAsync(pipe.Writer, Three(), 5, Options));

        // async source, more than declared
        pipe = new Pipe();
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.SerializeElementsAsync(pipe.Writer, Three(), 2, Options));
    }

    [Fact]
    public async Task SerializeElementsAsync_NegativeCount_Throws()
    {
        var pipe = new Pipe();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => MessagePackSerializer.SerializeElementsAsync(pipe.Writer, Array.Empty<int>(), -1, Options));
    }

    [Fact]
    public async Task SerializeMessagesAsync_ReaderCompleted_StopsSilently()
    {
        // reader walks away after the first message; the producer must stop without
        // throwing instead of serializing the rest into the void
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var produced = 0;
        async IAsyncEnumerable<string> Source()
        {
            for (int i = 0; i < 100; i++)
            {
                await gate.Task;
                produced++;
                yield return new string('x', 1000);
            }
        }

        var pipe = new Pipe();
        var writeTask = MessagePackSerializer.SerializeMessagesAsync(pipe.Writer, Source(), Options);
        pipe.Reader.Complete();
        gate.SetResult();
        await writeTask.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(produced < 100);
    }

    [Fact]
    public async Task SerializeMessagesAsync_Lz4Envelope_Roundtrips()
    {
        var options = Options.WithLz4Block();
        var values = Enumerable.Range(0, 20).Select(i => new string((char)('a' + i), 500)).ToArray();
        var pipe = new Pipe();
        await MessagePackSerializer.SerializeMessagesAsync(pipe.Writer, values, options);
        pipe.Writer.Complete();

        var seen = new List<string>();
        await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(pipe.Reader, options))
        {
            seen.Add(message);
        }
        Assert.Equal(values, seen);
    }
}

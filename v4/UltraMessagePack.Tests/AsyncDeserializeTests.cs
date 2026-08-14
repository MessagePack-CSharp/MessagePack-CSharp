using System.Buffers;
using System.IO.Pipelines;
using UltraMessagePack;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// Async deserialization is 2-pass: MessagePackBoundaryScanner finds where one top-level
// value ends over the PipeReader's buffered bytes (resumable at any byte position), then
// the sync core parses exactly that range. The scanner tests drive the resume machinery
// deterministically (byte-by-byte prefixes, adversarial chunking); the pipe tests cover
// the end-to-end APIs.
public class AsyncDeserializeTests
{
    static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Default;

    #region boundary scanner

    static readonly object?[][] ScannerCases =
    [
        [42], [-5], [128], [-33], [70000], [-70000], [3.5], [1.5f], [true], [null],
        ["short"], [new string('あ', 100)], [new string('x', 300)], [new byte[] { 1, 2, 3 }], [new byte[300]], [new byte[70000]],
        [new object?[] { 1, "two", new object?[] { 3, 4 }, null }],
        [new Dictionary<object, object> { ["a"] = 1, ["b"] = new object?[] { 1, 2 } }],
        [DateTime.UtcNow], // ext (timestamp)
        [new object?[0]], [new Dictionary<object, object>()],
        [Enumerable.Range(0, 20).Cast<object>().ToArray()],     // array16
        [Enumerable.Range(0, 70000).Cast<object>().ToArray()],  // array32
    ];

    public static IEnumerable<object?[]> ScannerData() => ScannerCases;

    static byte[] OraclePayload(object? value)
        => Oracle.Serialize(value, MessagePack.Resolvers.ContractlessStandardResolver.Options);

    [Theory]
    [MemberData(nameof(ScannerData))]
    public void Scanner_FindsExactBoundary(object? value)
    {
        var bytes = OraclePayload(value);

        // whole buffer, single segment
        {
            var scanner = new MessagePackBoundaryScanner();
            Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }

        // adversarially chunked segments
        foreach (var chunkSize in (int[])[1, 3, 16])
        {
            var scanner = new MessagePackBoundaryScanner();
            Assert.True(scanner.TryFindEnd(Chunk(bytes, chunkSize)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }

        // growing prefixes: exercises suspend/resume at every byte position, exactly the
        // PipeReader examined-but-not-consumed pattern
        {
            var scanner = new MessagePackBoundaryScanner();
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.False(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes.AsMemory(0, i))));
            }
            Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }
    }

    // token classes the oracle payloads don't produce (wide ext/map headers, every
    // fixext width), built as raw bytes; exercises TryReadToken's full cascade
    static readonly byte[][] RawTokenCases =
    [
        [0xd4, 0x01, 0xaa],                                              // fixext1
        [0xd5, 0x01, 0xaa, 0xbb],                                        // fixext2
        [0xd6, 0x01, 1, 2, 3, 4],                                        // fixext4
        [0xd7, 0x01, 1, 2, 3, 4, 5, 6, 7, 8],                            // fixext8
        [0xd8, 0x01, .. new byte[16]],                                   // fixext16
        [0xc7, 0x03, 0x01, 1, 2, 3],                                     // ext8
        [0xc8, 0x00, 0x03, 0x01, 1, 2, 3],                               // ext16
        [0xc9, 0x00, 0x00, 0x00, 0x03, 0x01, 1, 2, 3],                   // ext32
        [0xd9, 0x03, (byte)'a', (byte)'b', (byte)'c'],                   // str8
        [0xc5, 0x00, 0x03, 1, 2, 3],                                     // bin16
        [0xde, 0x00, 0x01, 0xa1, (byte)'a', 0x01],                       // map16 { "a": 1 }
        [0xdf, 0x00, 0x00, 0x00, 0x01, 0xa1, (byte)'a', 0x01],           // map32 { "a": 1 }
        [0xdd, 0x00, 0x00, 0x00, 0x01, 0xc0],                            // array32 [nil]
        [0xcd, 0x12, 0x34],                                              // uint16
        [0xcb, 1, 2, 3, 4, 5, 6, 7, 8],                                  // float64
    ];

    public static IEnumerable<object[]> RawTokenData() => RawTokenCases.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(RawTokenData))]
    public void Scanner_RawTokens_FindExactBoundary(byte[] bytes)
    {
        // full + byte-by-byte resume + 1-byte segments (headers straddle everywhere)
        {
            var scanner = new MessagePackBoundaryScanner();
            Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }
        {
            var scanner = new MessagePackBoundaryScanner();
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.False(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes.AsMemory(0, i))));
            }
            Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }
        {
            var scanner = new MessagePackBoundaryScanner();
            Assert.True(scanner.TryFindEnd(Chunk(bytes, 1)));
            Assert.Equal(bytes.Length, scanner.Consumed);
        }
    }

    [Fact]
    public void Scanner_StopsAtFirstValueOfMany()
    {
        var first = OraclePayload(new object?[] { 1, "two", 3.5 });
        var second = OraclePayload("second");
        var scanner = new MessagePackBoundaryScanner();
        Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>([.. first, .. second])));
        Assert.Equal(first.Length, scanner.Consumed);
    }

    [Fact]
    public void Scanner_DeeplyNested_NoStackGrowth()
    {
        // 100_000 nested single-element arrays: recursion would overflow, counting must not
        var bytes = new byte[100_001];
        bytes.AsSpan(0, 100_000).Fill(0x91);
        bytes[100_000] = MessagePackCode.Nil;
        var scanner = new MessagePackBoundaryScanner();
        Assert.True(scanner.TryFindEnd(new ReadOnlySequence<byte>(bytes)));
        Assert.Equal(bytes.Length, scanner.Consumed);
    }

    [Fact]
    public void Scanner_NeverUsedCode_Throws()
    {
        var scanner = new MessagePackBoundaryScanner();
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var s = scanner;
            s.TryFindEnd(new ReadOnlySequence<byte>(new byte[] { 0xc1 }));
        });
    }

    #endregion

    #region DeserializeAsync

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task DeserializeAsync_Roundtrips(int chunkSize)
    {
        var value = new Dictionary<string, List<int>>
        {
            ["a"] = [.. Enumerable.Range(0, 100)],
            [new string('あ', 200)] = [1],
        };
        var bytes = MessagePackSerializer.Serialize(value, Options);
        var result = await MessagePackSerializer.DeserializeAsync<Dictionary<string, List<int>>>(Feed(bytes, chunkSize), Options);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task DeserializeAsync_LargeMessage_MultiSegmentPass2()
    {
        // > one pipe segment, so pass 2 takes the sequence core path
        var value = Enumerable.Range(0, 100_000).ToArray();
        var bytes = MessagePackSerializer.Serialize(value, Options);
        var result = await MessagePackSerializer.DeserializeAsync<int[]>(Feed(bytes, 4096), Options);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task DeserializeAsync_LeavesTrailingDataUnconsumed()
    {
        byte[] bytes = [.. MessagePackSerializer.Serialize(123, Options), .. MessagePackSerializer.Serialize("tail", Options)];
        var reader = Feed(bytes, 2);
        Assert.Equal(123, await MessagePackSerializer.DeserializeAsync<int>(reader, Options));
        Assert.Equal("tail", await MessagePackSerializer.DeserializeAsync<string>(reader, Options));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_Throws()
    {
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<int>(Feed([]), Options));
    }

    [Fact]
    public async Task DeserializeAsync_TruncatedMidValue_Throws()
    {
        var bytes = MessagePackSerializer.Serialize(new string('x', 1000), Options);
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<string>(Feed(bytes.AsSpan(0, bytes.Length - 1).ToArray()), Options));
    }

    [Fact]
    public async Task DeserializeAsync_MessageOverCap_Throws()
    {
        var options = Options with { MaxAsyncMessageSize = 16 };
        var bytes = MessagePackSerializer.Serialize(new string('x', 1000), options);
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<string>(Feed(bytes), options));
    }

    [Fact]
    public async Task DeserializeAsync_ImplausibleHeader_ThrowsBeforeBuffering()
    {
        // bin32 claiming a 100MB payload: the size-cap rejection must fire from the
        // header's lower bound alone — the pipe is never completed, so anything else hangs
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0xc6, 0x06, 0x40, 0x00, 0x00 });
        var options = Options with { MaxAsyncMessageSize = 1024 };
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<byte[]>(pipe.Reader, options).WaitAsync(TimeSpan.FromSeconds(30)));
    }

    #endregion

    #region DeserializeMessagesAsync

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task DeserializeMessagesAsync_StreamsConcatenatedMessages(int chunkSize)
    {
        var messages = new[] { "first", new string('あ', 300), "", "last" };
        var bytes = messages.SelectMany(m => MessagePackSerializer.Serialize(m, Options)).ToArray();

        var seen = new List<string>();
        await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(Feed(bytes, chunkSize), Options))
        {
            seen.Add(message);
        }
        Assert.Equal(messages, seen);
    }

    [Fact]
    public async Task DeserializeMessagesAsync_EmptyStream_YieldsNothing()
    {
        await foreach (var _ in MessagePackSerializer.DeserializeMessagesAsync<int>(Feed([]), Options))
        {
            Assert.Fail("empty stream must not yield");
        }
    }

    [Fact]
    public async Task DeserializeMessagesAsync_TruncatedLastMessage_Throws()
    {
        byte[] bytes = [.. MessagePackSerializer.Serialize("complete", Options), .. MessagePackSerializer.Serialize("truncated!", Options)[..^1]];
        var seen = new List<string>();
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
        {
            await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(Feed(bytes, 3), Options))
            {
                seen.Add(message);
            }
        });
        Assert.Equal(["complete"], seen);
    }

    [Fact]
    public async Task DeserializeMessagesAsync_PerMessageLz4Envelope_Roundtrips()
    {
        // a whole-payload envelope is one ext value, so the boundary scan covers it and
        // TryDecode applies per message
        var options = Options.WithLz4Block();
        var messages = new[] { new string('a', 5000), new string('b', 5000) };
        var bytes = messages.SelectMany(m => MessagePackSerializer.Serialize(m, options)).ToArray();

        var seen = new List<string>();
        await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(Feed(bytes, 512), options))
        {
            seen.Add(message);
        }
        Assert.Equal(messages, seen);
    }

    #endregion

    #region DeserializeElementsAsync

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task DeserializeElementsAsync_StreamsElements(int chunkSize)
    {
        var value = Enumerable.Range(0, 1000).Select(i => $"item{i}").ToArray();
        var bytes = MessagePackSerializer.Serialize(value, Options);

        var seen = new List<string>();
        await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<string>(Feed(bytes, chunkSize), Options))
        {
            seen.Add(element);
        }
        Assert.Equal(value, seen);
    }

    [Theory]
    [InlineData(0)]      // fixarray empty
    [InlineData(15)]     // fixarray max
    [InlineData(16)]     // array16 min
    [InlineData(70000)]  // array32
    public async Task DeserializeElementsAsync_AllHeaderWidths(int count)
    {
        var value = Enumerable.Range(0, count).ToArray();
        var bytes = MessagePackSerializer.Serialize(value, Options);

        var seen = new List<int>();
        await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<int>(Feed(bytes, 4096), Options))
        {
            seen.Add(element);
        }
        Assert.Equal(value, seen);
    }

    [Fact]
    public async Task DeserializeElementsAsync_Nil_YieldsNothing()
    {
        await foreach (var _ in MessagePackSerializer.DeserializeElementsAsync<int>(Feed([MessagePackCode.Nil]), Options))
        {
            Assert.Fail("nil must not yield");
        }
    }

    [Fact]
    public async Task DeserializeElementsAsync_TopLevelNotArray_Throws()
    {
        var bytes = MessagePackSerializer.Serialize("not an array", Options);
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
        {
            await foreach (var _ in MessagePackSerializer.DeserializeElementsAsync<int>(Feed(bytes), Options))
            {
            }
        });
    }

    [Fact]
    public async Task DeserializeElementsAsync_MissingElements_Throws()
    {
        // array header claims 3 but only 2 elements follow
        byte[] bytes = [0x93, 1, 2];
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
        {
            await foreach (var _ in MessagePackSerializer.DeserializeElementsAsync<int>(Feed(bytes), Options))
            {
            }
        });
    }

    [Fact]
    public async Task DeserializeElementsAsync_LeavesTrailingDataUnconsumed()
    {
        byte[] bytes = [.. MessagePackSerializer.Serialize(new[] { 1, 2, 3 }, Options), .. MessagePackSerializer.Serialize("tail", Options)];
        var reader = Feed(bytes, 2);

        var seen = new List<int>();
        await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<int>(reader, Options))
        {
            seen.Add(element);
        }
        Assert.Equal([1, 2, 3], seen);
        Assert.Equal("tail", await MessagePackSerializer.DeserializeAsync<string>(reader, Options));
    }

    [Fact]
    public async Task DeserializeElementsAsync_ElementOverCap_Throws()
    {
        var options = Options with { MaxAsyncMessageSize = 64 };
        var bytes = MessagePackSerializer.Serialize(new[] { "small", new string('x', 1000) }, options);
        var seen = new List<string>();
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
        {
            await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<string>(Feed(bytes), options))
            {
                seen.Add(element);
            }
        });
        Assert.Equal(["small"], seen); // the cap is per element, so the small one got through
    }

    #endregion

    #region completed-reader fast path

    // a pipe that is written and completed before the first read deterministically
    // reports IsCompleted with all data on the first ReadAsync, so these pin the
    // single-pass shortcut (parse directly, advance by the parser's consumed count)

    [Fact]
    public async Task CompletedReader_SinglePass_RoundtripsAndLeavesTrailing()
    {
        byte[] bytes = [.. MessagePackSerializer.Serialize(new[] { 1, 2, 3 }, Options), .. MessagePackSerializer.Serialize("tail", Options)];
        var reader = await CompletedFeed(bytes);
        Assert.Equal(new[] { 1, 2, 3 }, await MessagePackSerializer.DeserializeAsync<int[]>(reader, Options));
        Assert.Equal("tail", await MessagePackSerializer.DeserializeAsync<string>(reader, Options));
    }

    [Fact]
    public async Task CompletedReader_SinglePass_Messages()
    {
        var messages = new[] { "one", "two", "three" };
        var bytes = messages.SelectMany(m => MessagePackSerializer.Serialize(m, Options)).ToArray();
        var seen = new List<string>();
        await foreach (var message in MessagePackSerializer.DeserializeMessagesAsync<string>(await CompletedFeed(bytes), Options))
        {
            seen.Add(message);
        }
        Assert.Equal(messages, seen);
    }

    [Fact]
    public async Task CompletedReader_TruncatedValue_Throws()
    {
        var bytes = MessagePackSerializer.Serialize(new string('x', 1000), Options);
        var reader = await CompletedFeed(bytes.AsSpan(0, bytes.Length - 1).ToArray());
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<string>(reader, Options));
    }

    [Fact]
    public async Task CompletedReader_OverCap_StillEnforced()
    {
        // the shortcut is declined when the buffered data exceeds the cap; the scan path
        // must still reject the oversized value
        var options = Options with { MaxAsyncMessageSize = 16 };
        var bytes = MessagePackSerializer.Serialize(new string('x', 1000), options);
        var reader = await CompletedFeed(bytes);
        await Assert.ThrowsAsync<MessagePackSerializationException>(
            () => MessagePackSerializer.DeserializeAsync<string>(reader, options));
    }

    [Fact]
    public async Task CompletedReader_LargeMultiSegment_Roundtrips()
    {
        // > one pipe segment, so the shortcut's sequence-core path runs
        var value = Enumerable.Range(0, 100_000).ToArray();
        var bytes = MessagePackSerializer.Serialize(value, Options);
        var reader = await CompletedFeed(bytes);
        Assert.Equal(value, await MessagePackSerializer.DeserializeAsync<int[]>(reader, Options));
    }

    static async Task<PipeReader> CompletedFeed(byte[] bytes)
    {
        // pauseWriterThreshold 0 = unlimited, so the whole payload lands before any read
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0));
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();
        return pipe.Reader;
    }

    #endregion

    // writes concurrently with the reader (PipeWriter.WriteAsync flushes each chunk), so
    // small chunk sizes make the deserializer observe incomplete data and resume
    static PipeReader Feed(byte[] bytes, int chunkSize = int.MaxValue, bool complete = true)
    {
        var pipe = new Pipe();
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < bytes.Length; i += chunkSize)
            {
                var length = (int)Math.Min((long)chunkSize, bytes.Length - i);
                await pipe.Writer.WriteAsync(bytes.AsMemory(i, length));
            }
            if (complete)
            {
                await pipe.Writer.CompleteAsync();
            }
        });
        return pipe.Reader;
    }

    static ReadOnlySequence<byte> Chunk(byte[] bytes, int chunkSize)
    {
        var first = new Chunked(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var last = first;
        for (var i = chunkSize; i < bytes.Length; i += chunkSize)
        {
            last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Chunked : ReadOnlySequenceSegment<byte>
    {
        public Chunked(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Chunked Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Chunked(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

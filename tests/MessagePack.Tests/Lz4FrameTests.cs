using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using MessagePack;
using NativeCompressions;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Lz4FrameProcessor: every message is one standard LZ4 frame with no MessagePack envelope. The binding's own frame
// decoder stands in for "any LZ4 implementation" as the interop oracle, and the async entries find frame boundaries
// through the processor's header walk instead of the MessagePack scanner.
public class Lz4FrameTests
{
    static V4Options Options { get; } = V4Options.Default.WithLz4Frame();

    static readonly byte[] Magic = [0x04, 0x22, 0x4D, 0x18];

    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    // the segment-by-segment encode writes the frame the one-shot compress writes: block boundaries follow the block
    // size, not the staging buffer's segments, and the content size is declared in the header
    [Fact]
    public void Encode_StreamsSegmentsIntoTheOneShotFrame()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default); // well past one staging segment
        var options = new LZ4CompressionOptions { ContentSize = (ulong)plain.Length };
        var expected = LZ4.Compress(plain, options);
        Assert.Equal(expected, V4.Serialize(BigCompressible(), Options));
        var writer = new ArrayBufferWriter<byte>();
        V4.Serialize(writer, BigCompressible(), Options);
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
        Assert.Equal(plain, LZ4.Decompress(expected)); // and the binding's own decoder reads it back
    }

    [Fact]
    public void Message_IsAStandardLz4Frame()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var framed = V4.Serialize(big, Options);

        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length < plain.Length / 2, $"compression should pay on this payload: {framed.Length} vs {plain.Length}");
        Assert.True(LZ4.TryGetFrameInfo(framed, out var info));
        Assert.Equal((ulong)plain.Length, info.ContentSize); // the content size travels in the header
        Assert.Equal(plain, LZ4.Decompress(framed)); // the frame decoder reads it without knowing MessagePack exists
        Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));

        // no threshold: a tiny message is a frame too
        var one = V4.Serialize(1, Options);
        Assert.Equal(Magic, one.AsSpan(0, 4).ToArray());
        Assert.Equal(1, V4.Deserialize<int>(one, Options));
    }

    // Frames in the shapes the lz4 command-line tool writes, as a foreign producer would hand them over: generated once
    // with libLZ4 (NativeCompressions 1.1.0) through the streaming frame API the tool uses, for the payload the test
    // rebuilds. `lz4` default: 4MB linked blocks, content checksum, no content size; `lz4 -BI -B5 -BX`: independent
    // 256KB blocks, block checksums, content checksum; `lz4 --content-size`. The FLG / BD bytes assert the shapes, so a
    // regenerated constant cannot silently drift into our own shape.
    public static IEnumerable<object[]> CliShapedFrames() =>
    [
        ["lz4 default", "BCJNGERwHYUAAAD/WNwTiAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZGhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj9AQUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVpbXF1eX2BhYmNkAP////////////////////////8fUF9gYWJjAAAAAJFHkCQ=", (byte)0x44, (byte)0x70],
        ["lz4 -BI -B5 -BX", "BCJNGHRQ/4UAAAD/WNwTiAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZGhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj9AQUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVpbXF1eX2BhYmNkAP////////////////////////8fUF9gYWJjTT09jwAAAACRR5Ak", (byte)0x74, (byte)0x50],
        ["lz4 --content-size", "BCJNGExwixMAAAAAAAC1hQAAAP9Y3BOIAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2QA/////////////////////////x9QX2BhYmMAAAAAkUeQJA==", (byte)0x4C, (byte)0x70],
    ];

    static int[] CliPayload() => Enumerable.Range(0, 5000).Select(static i => i % 100).ToArray();

    [Theory]
    [MemberData(nameof(CliShapedFrames))]
    public void CliShapedForeignFrame_IsRead(string name, string base64, byte flg, byte bd)
    {
        var frame = Convert.FromBase64String(base64);
        Assert.Equal(Magic, frame[..4]);
        Assert.Equal(flg, frame[4]); // version, independence, block checksum, content size, content checksum
        Assert.Equal(bd, frame[5]); // block size id
        var expected = CliPayload();
        Assert.Equal(expected, V4.Deserialize<int[]>(frame, Options));
        Assert.Equal(expected, V4.Deserialize<int[]>(Split(frame, frame.Length / 2), Options));
        Assert.Equal(expected, V4.Deserialize<int[]>(new MemoryStream(frame), Options));
        Assert.True(Options.MessageProcessor!.TryFindMessageEnd(new ReadOnlySequence<byte>(frame), out var length), name);
        Assert.Equal(frame.Length, length);
    }

    [Fact]
    public async Task CliShapedForeignFrames_AreDelimitedOnAPipe()
    {
        // the three shapes concatenated, as `cat a.lz4 b.lz4 c.lz4` would, fed in small pieces
        var frames = CliShapedFrames().Select(static c => Convert.FromBase64String((string)c[1])).ToArray();
        var bytes = frames.SelectMany(static f => f).ToArray();
        var pipe = new Pipe();
        var reading = ReadAll(pipe.Reader);
        for (var offset = 0; offset < bytes.Length; offset += 7)
        {
            await pipe.Writer.WriteAsync(bytes.AsMemory(offset, Math.Min(7, bytes.Length - offset)));
        }
        await pipe.Writer.CompleteAsync();
        var results = await reading;
        Assert.Equal(frames.Length, results.Count);
        Assert.All(results, static r => Assert.Equal(CliPayload(), r));
    }

    [Fact]
    public void ForeignFrame_WithoutContentSize_IsRead()
    {
        // the lz4 tool's default: no content size in the header, so the reader grows under its cap
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var foreign = CompressWithoutContentSize(plain);
        Assert.True(LZ4.TryGetFrameInfo(foreign, out var info));
        Assert.Equal(0UL, info.ContentSize);

        Assert.Equal(big, V4.Deserialize<int[]>(foreign, Options));
        Assert.Equal(big, V4.Deserialize<int[]>(Split(foreign, foreign.Length / 3), Options));
        Assert.Equal(big, V4.Deserialize<int[]>(new MemoryStream(foreign), Options));
    }

    [Fact]
    public void PlainMessagePack_IsRejected()
    {
        // strict: the frame processor never passes anything through, so the magic can never be confused with a
        // MessagePack value that happens to start with the same bytes
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(plain, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(Split(plain, 10), Options));
    }

    [Fact]
    public void Cap_IsEnforced_FromTheHeaderAndWhileGrowing()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        var small = V4Options.Default.WithLz4Frame(maxDecompressedSize: 1024);

        var sized = V4.Serialize(BigCompressible(), Options);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(sized, small)); // from the header

        var unsized = CompressWithoutContentSize(plain);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(unsized, small)); // while growing
    }

    // an unsized frame whose content is exactly the cap: after the last byte of output the decoder still has the
    // end mark to consume, which produces nothing and must not read as "more than the cap"
    [Fact]
    public void Cap_ExactlyMet_UnsizedFrame_IsAccepted()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var exact = V4Options.Default.WithLz4Frame(maxDecompressedSize: plain.Length);
        Assert.Equal(big, V4.Deserialize<int[]>(CompressWithoutContentSize(plain), exact));
        Assert.Equal(big, V4.Deserialize<int[]>(V4.Serialize(big, Options), exact)); // and the sized frame
        var oneShort = V4Options.Default.WithLz4Frame(maxDecompressedSize: plain.Length - 1);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(CompressWithoutContentSize(plain), oneShort));
    }

    // the unknown-members packet holds raw MessagePack the framed deserialization already unwrapped: its dictionary
    // views take the same (framed) options for the resolver without running the frame processor again
    [Fact]
    public void UnknownMembers_DictionaryViews_DoNotReapplyTheFrame()
    {
        var bytes = V4.Serialize(new UnkWideMap { A = 1, B = "b", C = "extra", D = true }, Options);
        var narrow = V4.Deserialize<UnkNarrowMap>(bytes, Options)!;
        var view = narrow.Extra!.ToMapDictionary(Options);
        Assert.Equal("extra", view["C"]);
        Assert.Equal(true, view["D"]);
        Assert.Equal(bytes, V4.Serialize(narrow, Options));
    }

    // see ZstandardFrameTests.BoundaryWalk_ResumesFromItsPosition: the same for the LZ4 walker, with 1-byte stored blocks
    [Fact]
    public void BoundaryWalk_ResumesFromItsPosition()
    {
        // magic, FLG (version 01, independent blocks, no checksums or sizes), BD (64KB blocks), header checksum, then
        // 2,000 stored 1-byte blocks and the end mark
        var frame = new List<byte> { 0x04, 0x22, 0x4D, 0x18, 0x60, 0x40, 0x00 };
        for (var i = 0; i < 2000; i++)
        {
            frame.AddRange([0x01, 0x00, 0x00, 0x80, 0x2A]);
        }
        frame.AddRange([0x00, 0x00, 0x00, 0x00]);
        var bytes = frame.ToArray();
        var processor = V4Options.Default.WithLz4Frame().MessageProcessor!;

        long position = 0;
        var previous = 0L;
        for (var delivered = 1; delivered < bytes.Length; delivered++)
        {
            Assert.False(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes, 0, delivered), ref position, out var lowerBound));
            Assert.True(lowerBound > delivered);
            Assert.True(position >= previous);
            Assert.True(delivered - position <= 5 || delivered < 7, $"position {position} lags {delivered} bytes");
            previous = position;
        }
        Assert.True(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes), ref position, out var length));
        Assert.Equal(bytes.Length, length);
        Assert.True(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes), out var whole));
        Assert.Equal(bytes.Length, whole);
    }

    [Fact]
    public async Task Pipe_FramesAreDelimitedByTheirHeaders()
    {
        var values = new[] { BigCompressible(), new[] { 1, 2, 3 }, Enumerable.Range(0, 5000).ToArray(), Array.Empty<int>() };
        var stream = new MemoryStream();
        foreach (var value in values)
        {
            V4.Serialize(stream, value, Options);
        }
        var bytes = stream.ToArray();

        // the concatenation arrives in odd-sized pieces with the writer never completing until the end: every
        // boundary has to come from the frame headers
        var pipe = new Pipe();
        var reading = ReadAll(pipe.Reader);
        for (var offset = 0; offset < bytes.Length; offset += 777)
        {
            await pipe.Writer.WriteAsync(bytes.AsMemory(offset, Math.Min(777, bytes.Length - offset)));
        }
        await pipe.Writer.CompleteAsync();
        var results = await reading;
        Assert.Equal(values.Length, results.Count);
        for (var i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], results[i]);
        }

        // the single-message entry leaves the following frames unconsumed
        var single = new Pipe();
        await single.Writer.WriteAsync(bytes);
        await single.Writer.CompleteAsync();
        Assert.Equal(values[0], await V4.DeserializeAsync<int[]>(single.Reader, Options));
        var rest = await single.Reader.ReadAsync();
        Assert.Equal(Magic, rest.Buffer.Slice(0, 4).ToArray()); // the next frame starts here
    }

    [Fact]
    public async Task Pipe_BogusBlockSize_IsRejectedFromTheHeader()
    {
        // a frame whose first block claims 1MB on a 64KB-block frame is refused by the header walk, before any of
        // the claimed bytes are waited for
        var frame = V4.Serialize(new[] { 1, 2, 3 }, Options);
        Assert.True(LZ4.TryGetFrameInfo(frame, out var info));
        Assert.Equal(BlockSizeId.Max64KB, info.BlockSizeId);
        var headerLength = 4 + 2 + 8 + 1; // magic, FLG/BD, content size, header checksum
        var corrupt = (byte[])frame.Clone();
        corrupt[headerLength] = 0x00;
        corrupt[headerLength + 1] = 0x00;
        corrupt[headerLength + 2] = 0x10; // 0x00100000 = 1MB
        corrupt[headerLength + 3] = 0x00;

        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(corrupt);
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<int[]>(pipe.Reader, Options));
    }

    [Fact]
    public async Task DeserializeElementsAsync_IsNotSupported()
    {
        var pipe = new Pipe();
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in V4.DeserializeElementsAsync<int>(pipe.Reader, Options))
            {
            }
        });
    }

    // a frame without a content size (the lz4 tool's default): the binding records the size only when the options
    // carry one, so the default options leave the C.Size flag clear
    static byte[] CompressWithoutContentSize(byte[] plain)
    {
        var options = new LZ4CompressionOptions();
        var destination = new byte[LZ4.GetMaxCompressedLength(plain.Length, in options)];
        var length = LZ4.Compress(plain, destination, in options);
        var unsized = destination.AsSpan(0, length).ToArray();
        Assert.Equal(0, unsized[4] & 0x08); // C.Size clear
        return unsized;
    }

    [Fact]
    public async Task SkippableFrame_IsSteppedOver_AtEverySplit()
    {
        // a skippable frame (user data the command-line tool may emit) in front of a frame, then a second frame: the
        // header walk steps over the skippable frame and ends exactly where the first frame ends whatever the buffer
        // segmentation, reports a lower bound while the buffer is still short, and the decoder never sees the
        // skippable bytes
        var firstValue = Enumerable.Range(0, 5000).ToArray();
        var first = V4.Serialize(firstValue, Options);
        var second = V4.Serialize(new[] { 1, 2, 3 }, Options);
        var message = Concat(SkippableFrame(37), first);
        var bytes = Concat(message, second);
        var processor = new Lz4FrameProcessor();
        for (var available = 1; available <= bytes.Length; available++)
        {
            var found = processor.TryFindMessageEnd(Segmented(bytes, available, 64), out var length);
            if (available < message.Length)
            {
                Assert.False(found, $"at {available}");
                Assert.True(length > available && length <= message.Length, $"at {available}: {length}");
            }
            else
            {
                Assert.True(found, $"at {available}");
                Assert.Equal(message.Length, length);
            }
        }

        Assert.Equal(firstValue, V4.Deserialize<int[]>(message, Options));
        Assert.Equal(firstValue, V4.Deserialize<int[]>(Segmented(message, message.Length, 64), Options));
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();
        var results = await ReadAll(pipe.Reader);
        Assert.Equal(2, results.Count);
        Assert.Equal(firstValue, results[0]);
        Assert.Equal(new[] { 1, 2, 3 }, results[1]);
    }

    static byte[] SkippableFrame(int payloadLength)
    {
        var frame = new byte[8 + payloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, 0x184D2A5A);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), (uint)payloadLength);
        frame.AsSpan(8).Fill(0xAB);
        return frame;
    }

    // ---- dictionaries ----

    // A raw-content dictionary: bytes typical of the messages (here the phrases the messages are made of, serialized
    // the way they travel), which the codec matches against as if they preceded every message. Each message uses
    // every phrase once, so the message itself has nothing to repeat and only the dictionary can shorten it.
    static readonly string[] Phrases =
    [
        "the quick brown fox jumps over the lazy dog",
        "MessagePack for C# v4 preview, LZ4 frames with a shared dictionary",
        "a reader without the dictionary cannot decode the frame",
        "writer and reader hold the same bytes and the same id",
        "temperature sensor reading from the warehouse roof",
        "order confirmation sent to the customer by email",
        "scheduled maintenance window for the primary database",
        "the frame header carries the dictionary id",
    ];

    static byte[] DictionaryContent() => V4.Serialize(Phrases, V4Options.Default);

    static string[] DictionaryPayload(int seed) => Phrases.Select((p, i) => $"{seed + i}: {p}").ToArray();

    static V4Options DictionaryOptions { get; } = V4Options.Default.WithLz4Frame(DictionaryContent(), 42);

    [Fact]
    public void Dictionary_RoundTripsAndDeclaresItsId()
    {
        var processor = Assert.IsType<Lz4FrameProcessor>(DictionaryOptions.MessageProcessor);
        Assert.Equal(42u, processor.DictionaryId);
        Assert.Equal(DictionaryContent(), processor.Dictionary.ToArray());

        var first = DictionaryPayload(1);
        var framed = V4.Serialize(first, DictionaryOptions);
        Assert.Equal(Magic, framed[..4]);
        Assert.True(LZ4.TryGetFrameInfo(framed, out var info));
        Assert.Equal(42u, info.DictionaryId); // the header declares the dictionary
        Assert.Equal((ulong)V4.Serialize(first, V4Options.Default).Length, info.ContentSize); // and still the content size
        var withoutDictionary = V4.Serialize(first, Options);
        Assert.True(framed.Length < withoutDictionary.Length * 2 / 3, $"the dictionary should pay on this payload: {framed.Length} vs {withoutDictionary.Length}");
        Assert.Equal(first, V4.Deserialize<string[]>(framed, DictionaryOptions));
        Assert.Equal(first, V4.Deserialize<string[]>(Split(framed, framed.Length / 2), DictionaryOptions));
        Assert.Equal(first, V4.Deserialize<string[]>(new MemoryStream(framed), DictionaryOptions));

        // the second message goes through the cached encoder, which takes the dictionary again with its new frame
        var second = DictionaryPayload(2);
        var again = V4.Serialize(second, DictionaryOptions);
        Assert.True(LZ4.TryGetFrameInfo(again, out info));
        Assert.Equal(42u, info.DictionaryId);
        Assert.Equal(second, V4.Deserialize<string[]>(again, DictionaryOptions));
        Assert.Equal(framed, V4.Serialize(first, DictionaryOptions)); // and writes the same frame as the first time
    }

    // frames another LZ4 implementation compresses with the same dictionary (here the binding: one-shot with the
    // content size, and streamed without it) are read through our options, and ours through its decoder
    [Fact]
    public void Dictionary_ForeignFramesAreRead()
    {
        var payload = DictionaryPayload(3);
        var plain = V4.Serialize(payload, V4Options.Default);
        var dictionary = LZ4Dictionary.Create(DictionaryContent(), 42);
        try
        {
            var sized = LZ4.Compress(plain, new LZ4CompressionOptions { ContentSize = (ulong)plain.Length, Dictionary = dictionary });
            Assert.True(LZ4.TryGetFrameInfo(sized, out var info));
            Assert.Equal(42u, info.DictionaryId);
            Assert.Equal(payload, V4.Deserialize<string[]>(sized, DictionaryOptions));
            Assert.Equal(payload, V4.Deserialize<string[]>(Split(sized, sized.Length / 2), DictionaryOptions));

            var unsized = new byte[plain.Length + 1024];
            var encoder = new LZ4Encoder(new LZ4CompressionOptions { Dictionary = dictionary });
            int written;
            try
            {
                written = encoder.Compress(plain, unsized);
                written += encoder.Close(unsized.AsSpan(written));
            }
            finally
            {
                encoder.Dispose();
            }
            var unsizedFrame = unsized.AsSpan(0, written).ToArray();
            Assert.True(LZ4.TryGetFrameInfo(unsizedFrame, out info));
            Assert.Equal(0ul, info.ContentSize);
            Assert.Equal(42u, info.DictionaryId);
            Assert.Equal(payload, V4.Deserialize<string[]>(unsizedFrame, DictionaryOptions)); // the streaming decoder with the dictionary

            var ours = V4.Serialize(payload, DictionaryOptions);
            var decoded = new byte[plain.Length];
            Assert.Equal(plain.Length, LZ4.Decompress(ours, decoded, new LZ4DecompressionOptions { Dictionary = dictionary }));
            Assert.Equal(plain, decoded);
        }
        finally
        {
            dictionary.Dispose();
        }
    }

    [Fact]
    public void Dictionary_MismatchIsRefused()
    {
        var payload = DictionaryPayload(4);
        var framed = V4.Serialize(payload, DictionaryOptions);

        // a reader with no dictionary, on every entry
        var noDictionary = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(framed, Options));
        Assert.Contains("42", noDictionary.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(Split(framed, framed.Length / 2), Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(new MemoryStream(framed), Options));

        // a reader holding a dictionary under another id: the bytes would decode, the id says they are not meant to
        var otherId = V4Options.Default.WithLz4Frame(DictionaryContent(), 7);
        var mismatch = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(framed, otherId));
        Assert.Contains("42", mismatch.Message);
        Assert.Contains("7", mismatch.Message);

        // a frame declaring no dictionary is read by a reader that holds one: history the frame never references
        var plainFrame = V4.Serialize(payload, Options);
        Assert.Equal(payload, V4.Deserialize<string[]>(plainFrame, DictionaryOptions));

        // an id without a dictionary is meaningless
        Assert.Throws<ArgumentException>(() => new Lz4FrameProcessor(1024, ReadOnlyMemory<byte>.Empty, 1));
    }

    // the boundary walk skips the dictionary id field the header gains, on a pipe fed in small pieces
    [Fact]
    public async Task Dictionary_FramesAreDelimitedOnAPipe()
    {
        var bytes = Enumerable.Range(0, 5).SelectMany(i => V4.Serialize(DictionaryPayload(i), DictionaryOptions)).ToArray();
        var pipe = new Pipe();
        var reading = Task.Run(async () =>
        {
            var results = new List<string[]>();
            await foreach (var value in V4.DeserializeMessagesAsync<string[]>(pipe.Reader, DictionaryOptions))
            {
                results.Add(value);
            }
            return results;
        });
        for (var offset = 0; offset < bytes.Length; offset += 7)
        {
            await pipe.Writer.WriteAsync(bytes.AsMemory(offset, Math.Min(7, bytes.Length - offset)));
        }
        await pipe.Writer.CompleteAsync();
        var results = await reading;
        Assert.Equal(5, results.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(DictionaryPayload(i), results[i]);
        }
    }

    static byte[] Concat(byte[] left, byte[] right) => [.. left, .. right];

    static ReadOnlySequence<byte> Segmented(byte[] bytes, int length, int segmentSize)
    {
        var first = new Segment(bytes.AsMemory(0, Math.Min(segmentSize, length)));
        var last = first;
        for (var offset = first.Memory.Length; offset < length; offset += segmentSize)
        {
            last = last.Append(bytes.AsMemory(offset, Math.Min(segmentSize, length - offset)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    static async Task<List<int[]>> ReadAll(PipeReader reader)
    {
        var results = new List<int[]>();
        await foreach (var value in V4.DeserializeMessagesAsync<int[]>(reader, Options))
        {
            results.Add(value);
        }
        return results;
    }

    static ReadOnlySequence<byte> Split(byte[] bytes, int at)
    {
        var first = new Segment(bytes.AsMemory(0, at));
        var last = first.Append(bytes.AsMemory(at));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

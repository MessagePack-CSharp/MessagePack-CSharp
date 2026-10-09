using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using MessagePack;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;
#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
using FrameDictionary = System.IO.Compression.ZstandardDictionary;
#else
using NativeCompressions;
using FrameEncoder = NativeCompressions.ZstandardEncoder;
using FrameDecoder = NativeCompressions.ZstandardDecoder;
using FrameDictionary = NativeCompressions.ZstandardDictionary;
#endif

namespace MessagePack.Tests;

// ZstandardFrameProcessor: every message is one standard Zstandard frame with no MessagePack envelope. The codec's own
// frame decoder (in-box on .NET 11, NativeCompressions on .NET 10) stands in for "any Zstandard implementation" as the
// interop oracle, and the async entries find frame boundaries through the processor's header walk.
public class ZstandardFrameTests
{
    static V4Options Options { get; } = V4Options.Default.WithZstandardFrame();

    static readonly byte[] Magic = [0x28, 0xB5, 0x2F, 0xFD];

    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    static byte[] OracleDecompress(byte[] frame, int expectedLength)
    {
        var decoder = new FrameDecoder();
        try
        {
            var destination = new byte[expectedLength + 64];
            var status = decoder.Decompress(frame, destination, out var consumed, out var written);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(frame.Length, consumed);
            return destination.AsSpan(0, written).ToArray();
        }
        finally
        {
            decoder.Dispose();
        }
    }

    // a frame with no content size in its header, the way streamed input comes out of the zstd tool: compress in a
    // non-final call first, so the encoder cannot pledge the total
    static byte[] UnsizedFrame(byte[] plain)
    {
        var encoder = new FrameEncoder(3);
        try
        {
            var destination = new byte[plain.Length + 1024];
            var source = plain.AsSpan();
            var written = 0;
            while (!source.IsEmpty)
            {
                var status = encoder.Compress(source, destination.AsSpan(written), out var consumed, out var produced, isFinalBlock: false);
                Assert.NotEqual(OperationStatus.InvalidData, status);
                source = source.Slice(consumed);
                written += produced;
            }
            var final = encoder.Compress(ReadOnlySpan<byte>.Empty, destination.AsSpan(written), out _, out var tail, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, final);
            written += tail;
            return destination.AsSpan(0, written).ToArray();
        }
        finally
        {
            encoder.Dispose();
        }
    }

    static bool CarriesContentSize(byte[] frame)
    {
#if NET11_0_OR_GREATER
        // ZstandardDecoder.TryGetMaxDecompressedLength is ZSTD_decompressBound: it answers with the window-size bound
        // for a frame that carries no content size, so it cannot tell the two apart. Read the Frame_Header_Descriptor
        // instead (RFC 8878 3.1.1.1): a content size field is present when Frame_Content_Size_flag is non-zero or
        // Single_Segment_flag is set
        var descriptor = frame[4];
        return (descriptor >> 6) != 0 || (descriptor & 0x20) != 0;
#else
        return Zstandard.TryGetFrameContentSize(frame, out _);
#endif
    }

    [Fact]
    public void Message_IsAStandardZstandardFrame()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var framed = V4.Serialize(big, Options);

        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length < plain.Length / 2, $"compression should pay on this payload: {framed.Length} vs {plain.Length}");
        Assert.True(CarriesContentSize(framed)); // the content size travels in the header
        Assert.Equal(plain, OracleDecompress(framed, plain.Length)); // the frame decoder reads it without knowing MessagePack exists
        Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));

        // no threshold: a tiny message is a frame too
        var one = V4.Serialize(1, Options);
        Assert.Equal(Magic, one.AsSpan(0, 4).ToArray());
        Assert.Equal(1, V4.Deserialize<int>(one, Options));
    }

    [Fact]
    public void Levels_AllRoundtrip_AndHigherLevelsAreNotLarger()
    {
        var big = BigCompressible();
        int? previous = null;
        foreach (var level in new[] { 1, 3, 9, 19 })
        {
            var options = V4Options.Default.WithZstandardFrame(level);
            var framed = V4.Serialize(big, options);
            Assert.Equal(big, V4.Deserialize<int[]>(framed, options));
            // the read side does not depend on the level the frame was written at
            Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));
            if (previous is { } p)
            {
                Assert.True(framed.Length <= p, $"level {level} produced {framed.Length} bytes, more than the previous level's {p}");
            }
            previous = framed.Length;
        }
    }

    [Fact]
    public void Incompressible_IsStillAFrame()
    {
        // large random bin: zstd stores it as raw blocks (a few bytes of block headers over the input); the frame is
        // written anyway, there is no content-dependent switch
        var rand = new Random(42);
        var blob = new byte[100_000];
        rand.NextBytes(blob);
        var plain = V4.Serialize(blob, V4Options.Default);
        var framed = V4.Serialize(blob, Options);
        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length > plain.Length);
        Assert.Equal(plain, OracleDecompress(framed, plain.Length));
        Assert.Equal(blob, V4.Deserialize<byte[]>(framed, Options));
    }

    // the encode asks the output for a window per segment and keeps going when the encoder has more to emit than a
    // window holds: an output that hands out tiny windows (far below the segment bound) still receives the same frame
    [Fact]
    public void Encode_SurvivesTinyOutputWindows()
    {
        var incompressible = new byte[100_000];
        new Random(17).NextBytes(incompressible); // ~100KB of frame either way, so thousands of 37-byte windows
        var expected = V4.Serialize(incompressible, Options);
        var stingy = new StingyWriter();
        V4.Serialize(stingy, incompressible, Options);
        Assert.Equal(expected, stingy.Written.ToArray());
        Assert.True(stingy.Requests > expected.Length / 37, $"only {stingy.Requests} windows were requested for {expected.Length} bytes");
    }

    // hands out at most 37 bytes per window, whatever the hint (an IBufferWriter that honoured the hint would never
    // make the encoder report DestinationTooSmall)
    sealed class StingyWriter : IBufferWriter<byte>
    {
        readonly ArrayBufferWriter<byte> inner = new();
        public int Requests;
        public ReadOnlyMemory<byte> Written => inner.WrittenMemory;
        public void Advance(int count) => inner.Advance(count);
        public Memory<byte> GetMemory(int sizeHint = 0) => inner.GetMemory(sizeHint).Slice(0, Math.Min(37, inner.GetMemory(sizeHint).Length));
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Requests++;
            return inner.GetSpan(37).Slice(0, 37);
        }
    }

    [Fact]
    public void BufferWriterEntry_MatchesArrayEntry()
    {
        var big = BigCompressible();
        var expected = V4.Serialize(big, Options);
        var writer = new ArrayBufferWriter<byte>();
        V4.Serialize(writer, big, Options);
        Assert.True(writer.WrittenSpan.SequenceEqual(expected));
    }

    [Fact]
    public void SequenceEntry_ReadsAtEverySplit()
    {
        // small-but-compressed payload so the every-position split stays cheap
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        for (int splitAt = 1; splitAt < framed.Length; splitAt++)
        {
            Assert.Equal(text, V4.Deserialize<string>(Split(framed, splitAt), Options));
        }
    }

    [Fact]
    public void CorruptFrame_ThrowsSanctioned()
    {
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        // clobber the frame past its header (magic, descriptor, content size, first block header)
        var corrupt = (byte[])framed.Clone();
        for (int i = 16; i < corrupt.Length - 4; i++) corrupt[i] ^= 0x5a;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(corrupt, Options));
        // and the processor stays usable afterwards (no pooled buffer was leaked or double-returned, no poisoned
        // cached context)
        Assert.Equal(text, V4.Deserialize<string>(framed, Options));
    }

    [Fact]
    public void TruncatedFrame_Rejected()
    {
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        var truncated = framed.AsSpan(0, framed.Length - 8).ToArray();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(truncated, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(Split(truncated, 10), Options));
    }

    // Frames in the shapes the zstd command-line tool writes, as a foreign producer would hand them over: generated once
    // with libzstd (NativeCompressions 1.1.0) through the streaming API, for the payload the test rebuilds. From a pipe
    // the tool cannot know the size: no content size, a window descriptor, and its default checksum; `zstd -19` the
    // same with a larger window; from a file the content size is known (single-segment, no window descriptor);
    // `--no-check` drops the checksum. The frame header descriptor asserts the shape (bit 2: checksum, bit 5:
    // single segment / content size present), so a regenerated constant cannot silently drift into our own shape.
    public static IEnumerable<object[]> CliShapedFrames() =>
    [
        ["zstd (pipe, default)", "KLUv/QRYjQMAdAbcE4gAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjAQBnyJx9iAIEX2Xc", (byte)0x04, false],
        ["zstd -19 (pipe)", "KLUv/QRojQMAdAbcE4gAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjAQBnyJx9iAIEX2Xc", (byte)0x04, false],
        ["zstd (file, default)", "KLUv/WSLEo0DAHQG3BOIAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiYwEAZ8icfYgCBF9l3A==", (byte)0x64, true],
        ["zstd --no-check (pipe)", "KLUv/QBYjQMAdAbcE4gAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl9gYWJjAQBnyJx9iAI=", (byte)0x00, false],
    ];

    static int[] CliPayload() => Enumerable.Range(0, 5000).Select(static i => i % 100).ToArray();

    [Theory]
    [MemberData(nameof(CliShapedFrames))]
    public void CliShapedForeignFrame_IsRead(string name, string base64, byte descriptor, bool sized)
    {
        var frame = Convert.FromBase64String(base64);
        Assert.Equal(Magic, frame[..4]);
        Assert.Equal(descriptor, frame[4]);
        Assert.Equal(sized, CarriesContentSize(frame));
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
        // the four shapes concatenated, as `cat a.zst b.zst ...` would, fed in small pieces
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
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var foreign = UnsizedFrame(plain);
        Assert.False(CarriesContentSize(foreign));

        Assert.Equal(big, V4.Deserialize<int[]>(foreign, Options));
        Assert.Equal(big, V4.Deserialize<int[]>(Split(foreign, foreign.Length / 3), Options));
        Assert.Equal(big, V4.Deserialize<int[]>(new MemoryStream(foreign), Options));
    }

    [Fact]
    public void PlainMessagePack_IsRejected()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(plain, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(Split(plain, 10), Options));
    }

    // a 10-byte frame declaring a 128 MB window: the decoder reserves the declared window before producing a byte
    // (and the cached context would keep it), so the window is capped from MaxDecompressedSize and such a frame is
    // rejected; the same content under a window the cap admits decodes
    [Fact]
    public void WindowLarger_ThanTheCap_IsRejected()
    {
        Assert.Equal(26, new ZstandardFrameProcessor().WindowLogMax); // ceil(log2(64 MB))
        Assert.Equal(10, new ZstandardFrameProcessor(3, maxDecompressedSize: 1024).WindowLogMax); // the codec's minimum
        Assert.Equal(20, new ZstandardFrameProcessor(3, maxDecompressedSize: 1024 * 1024).WindowLogMax);

        // magic, frame header descriptor 0x00 (no content size, no dictionary, no checksum, a window descriptor
        // follows), window descriptor, one raw last block holding a single nil
        static byte[] Frame(byte windowDescriptor) => [0x28, 0xB5, 0x2F, 0xFD, 0x00, windowDescriptor, 0x09, 0x00, 0x00, 0xC0];
        var hugeWindow = Frame(17 << 3); // exponent 17: 2^(10+17) = 128 MB
        var smallWindow = Frame(0 << 3); // exponent 0: 1 KB

        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Nil>(hugeWindow, Options));
        Assert.Equal(Nil.Default, V4.Deserialize<Nil>(smallWindow, Options));

        // the sized path (frame content size flag 2: a 4-byte content size of 1) with the same 128 MB window: a
        // decoder that decodes a complete sized frame in one pass needs no window buffer and accepts it (the output
        // is bounded by the content size), one that streams rejects it; either way nothing is reserved for the window
        // and a rejection surfaces as the serialization exception (with the rented output buffer returned)
        byte[] sizedHugeWindow = [0x28, 0xB5, 0x2F, 0xFD, 0x80, 17 << 3, 0x01, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0xC0];
        var sizedOutcome = Record.Exception(() => Assert.Equal(Nil.Default, V4.Deserialize<Nil>(sizedHugeWindow, Options)));
        Assert.True(sizedOutcome is null or MessagePackSerializationException, sizedOutcome?.ToString());
        byte[] sizedSmallWindow = [0x28, 0xB5, 0x2F, 0xFD, 0x80, 0 << 3, 0x01, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0xC0];
        Assert.Equal(Nil.Default, V4.Deserialize<Nil>(sizedSmallWindow, Options));

        // the window follows the cap: 1 MB admits a 1 MB window (exponent 10), not 2 MB
        var oneMegabyte = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: 1024 * 1024);
        Assert.Equal(Nil.Default, V4.Deserialize<Nil>(Frame(10 << 3), oneMegabyte));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Nil>(Frame(11 << 3), oneMegabyte));
    }

    // the same exact-cap case for an unsized Zstandard frame (its epilogue, the last block flag and any checksum,
    // produces no output)
    [Fact]
    public void Cap_ExactlyMet_UnsizedFrame_IsAccepted()
    {
        // an unsized frame at level 3 declares a 1 MB window, and WindowLogMax follows MaxDecompressedSize: the
        // content has to be at least that large for the exact cap to admit the frame's window at all
        var big = new int[1_100_000];
        for (int i = 0; i < big.Length; i++)
        {
            big[i] = i % 100;
        }
        var plain = V4.Serialize(big, V4Options.Default);
        Assert.True(plain.Length >= 1 << 20);
        var exact = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: plain.Length);
        Assert.Equal(big, V4.Deserialize<int[]>(UnsizedFrame(plain), exact));
        Assert.Equal(big, V4.Deserialize<int[]>(V4.Serialize(big, Options), exact));
        var oneShort = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: plain.Length - 1);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(UnsizedFrame(plain), oneShort));
    }

    // the boundary walk resumes where it stopped: fed one byte at a time, the position handed back never moves
    // backwards and always sits within one block header of the bytes delivered, so a frame of many tiny blocks costs
    // its size once rather than once per read (the async entries keep that position between reads)
    [Fact]
    public void BoundaryWalk_ResumesFromItsPosition()
    {
        // a raw frame of 2,000 empty blocks: magic, single-segment descriptor with a 1-byte content size, then 3-byte
        // block headers (type raw, size 0), the last one flagged
        var frame = new List<byte> { 0x28, 0xB5, 0x2F, 0xFD, 0x20, 0x00 };
        for (var i = 0; i < 1999; i++)
        {
            frame.AddRange([0x00, 0x00, 0x00]);
        }
        frame.AddRange([0x01, 0x00, 0x00]);
        var bytes = frame.ToArray();
        var processor = V4Options.Default.WithZstandardFrame(3).MessageProcessor!;
        Assert.True(processor.DefinesMessageBoundaries);

        long position = 0;
        var previous = 0L;
        for (var delivered = 1; delivered < bytes.Length; delivered++)
        {
            Assert.False(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes, 0, delivered), ref position, out var lowerBound));
            Assert.True(lowerBound > delivered);
            Assert.True(position >= previous);
            Assert.True(delivered - position <= 3 || delivered < 6, $"position {position} lags {delivered} bytes");
            previous = position;
        }
        Assert.True(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes), ref position, out var length));
        Assert.Equal(bytes.Length, length);

        // and the stateless call agrees
        Assert.True(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(bytes), out var whole));
        Assert.Equal(bytes.Length, whole);
    }

    [Fact]
    public void Cap_IsEnforced_FromTheHeaderAndWhileGrowing()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        var small = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: 1024);

        var sized = V4.Serialize(BigCompressible(), Options);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(sized, small)); // from the header
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(UnsizedFrame(plain), small)); // while growing
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

        var single = new Pipe();
        await single.Writer.WriteAsync(bytes);
        await single.Writer.CompleteAsync();
        Assert.Equal(values[0], await V4.DeserializeAsync<int[]>(single.Reader, Options));
        var rest = await single.Reader.ReadAsync();
        Assert.Equal(Magic, rest.Buffer.Slice(0, 4).ToArray()); // the next frame starts here
    }

    [Fact]
    public async Task Pipe_CorruptFrameHeader_IsRejectedFromTheHeader()
    {
        // the reserved bit of the frame header descriptor is refused by the header walk before any payload is waited for
        var corrupt = V4.Serialize(new[] { 1, 2, 3 }, Options);
        corrupt[4] |= 0x08;
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(corrupt);
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<int[]>(pipe.Reader, Options));
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
        var processor = new ZstandardFrameProcessor();
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

    // ---- dictionaries ----

    // The messages: each phrase once, so a message has nothing to repeat and only a dictionary can shorten it.
    static readonly string[] Phrases =
    [
        "the quick brown fox jumps over the lazy dog",
        "MessagePack for C# v4 preview, Zstandard frames with a shared dictionary",
        "a reader without the dictionary cannot decode the frame",
        "writer and reader hold the same bytes and the same id",
        "temperature sensor reading from the warehouse roof",
        "order confirmation sent to the customer by email",
        "scheduled maintenance window for the primary database",
        "the frame header carries the dictionary id",
    ];

    static string[] DictionaryPayload(int seed) => Phrases.Select((p, i) => $"{seed + i}: {p}").ToArray();

    // a dictionary trained on messages like the ones to come, what `zstd --train` writes (magic, id, entropy tables,
    // content), through the codec's own trainer; trained once, the id is random
    static readonly Lazy<byte[]> Trained = new(() =>
    {
        var samples = Enumerable.Range(0, 500).Select(static i => V4.Serialize(DictionaryPayload(i * 13), V4Options.Default)).ToArray();
        var lengths = samples.Select(static s => s.Length).ToArray();
        var all = samples.SelectMany(static s => s).ToArray();
#if NET11_0_OR_GREATER
        using var dictionary = FrameDictionary.Train(all, lengths, 16 * 1024);
        return dictionary.Data.ToArray();
#else
        return FrameDictionary.Train(all, lengths, 16 * 1024);
#endif
    });

    static V4Options DictionaryOptions { get; } = V4Options.Default.WithZstandardFrame(Trained.Value);

    static uint IdOfDictionary(byte[] dictionary)
    {
        Assert.Equal(0xEC30A437u, BinaryPrimitives.ReadUInt32LittleEndian(dictionary)); // a trained dictionary's magic
        return BinaryPrimitives.ReadUInt32LittleEndian(dictionary.AsSpan(4));
    }

    // the Dictionary_ID field of a frame header (RFC 8878 3.1.1.1): after the descriptor and the optional window
    // descriptor, 0/1/2/4 bytes as the descriptor's low two bits select
    static (int Offset, int Width) DictionaryIdField(byte[] frame)
    {
        Assert.Equal(Magic, frame[..4]);
        var descriptor = frame[4];
        return (5 + ((descriptor & 0x20) != 0 ? 0 : 1), (descriptor & 0x03) switch { 0 => 0, 1 => 1, 2 => 2, _ => 4 });
    }

    static uint DeclaredDictionaryId(byte[] frame)
    {
        var (offset, width) = DictionaryIdField(frame);
        return width switch
        {
            0 => 0,
            1 => frame[offset],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(offset)),
            _ => BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(offset)),
        };
    }

    static FrameEncoder EncoderWith(FrameDictionary dictionary)
    {
#if NET11_0_OR_GREATER
        return new FrameEncoder(dictionary); // the level went in at Create
#else
        return new FrameEncoder(new ZstandardCompressionOptions { CompressionLevel = 3, Dictionary = dictionary });
#endif
    }

    static FrameDecoder DecoderWith(FrameDictionary dictionary)
    {
#if NET11_0_OR_GREATER
        return new FrameDecoder(dictionary, 27);
#else
        return new FrameDecoder(new ZstandardDecompressionOptions { WindowLogMax = 27, Dictionary = dictionary });
#endif
    }

    [Fact]
    public void Dictionary_RoundTripsAndDeclaresItsId()
    {
        var id = IdOfDictionary(Trained.Value);
        Assert.NotEqual(0u, id);
        var processor = Assert.IsType<ZstandardFrameProcessor>(DictionaryOptions.MessageProcessor);
        Assert.Equal(id, processor.DictionaryId);
        Assert.Equal(Trained.Value, processor.Dictionary.ToArray());

        var first = DictionaryPayload(1);
        var framed = V4.Serialize(first, DictionaryOptions);
        Assert.Equal(id, DeclaredDictionaryId(framed)); // the header declares the dictionary
        Assert.True(CarriesContentSize(framed)); // and still the content size
        var withoutDictionary = V4.Serialize(first, Options);
        Assert.True(framed.Length < withoutDictionary.Length * 2 / 3, $"the dictionary should pay on this payload: {framed.Length} vs {withoutDictionary.Length}");
        Assert.Equal(first, V4.Deserialize<string[]>(framed, DictionaryOptions));
        Assert.Equal(first, V4.Deserialize<string[]>(Split(framed, framed.Length / 2), DictionaryOptions));
        Assert.Equal(first, V4.Deserialize<string[]>(new MemoryStream(framed), DictionaryOptions));

        // the second message goes through the cached contexts, created with the dictionary once and reset between frames
        var second = DictionaryPayload(2);
        var again = V4.Serialize(second, DictionaryOptions);
        Assert.Equal(id, DeclaredDictionaryId(again));
        Assert.Equal(second, V4.Deserialize<string[]>(again, DictionaryOptions));
        Assert.Equal(framed, V4.Serialize(first, DictionaryOptions)); // and writes the same frame as the first time
    }

    // frames another Zstandard implementation compresses with the same dictionary (here the codec itself: pledged
    // with the content size, and streamed without it) are read through our options, and ours through its decoder
    [Fact]
    public void Dictionary_ForeignFramesAreRead()
    {
        var payload = DictionaryPayload(3);
        var plain = V4.Serialize(payload, V4Options.Default);
        var id = IdOfDictionary(Trained.Value);
        using var dictionary = FrameDictionary.Create(Trained.Value, 3);

        var sized = new byte[plain.Length + 1024];
        var encoder = EncoderWith(dictionary);
        int written;
        try
        {
            encoder.SetSourceLength(plain.Length);
            Assert.Equal(OperationStatus.Done, encoder.Compress(plain, sized, out var consumed, out written, isFinalBlock: true));
            Assert.Equal(plain.Length, consumed);
        }
        finally
        {
            encoder.Dispose();
        }
        var sizedFrame = sized.AsSpan(0, written).ToArray();
        Assert.Equal(id, DeclaredDictionaryId(sizedFrame));
        Assert.True(CarriesContentSize(sizedFrame));
        Assert.Equal(payload, V4.Deserialize<string[]>(sizedFrame, DictionaryOptions));
        Assert.Equal(payload, V4.Deserialize<string[]>(Split(sizedFrame, sizedFrame.Length / 2), DictionaryOptions));

        var unsized = new byte[plain.Length + 1024];
        encoder = EncoderWith(dictionary);
        try
        {
            var status = encoder.Compress(plain, unsized, out var consumed, out written, isFinalBlock: false);
            Assert.NotEqual(OperationStatus.InvalidData, status);
            Assert.Equal(plain.Length, consumed);
            Assert.Equal(OperationStatus.Done, encoder.Compress(ReadOnlySpan<byte>.Empty, unsized.AsSpan(written), out _, out var tail, isFinalBlock: true));
            written += tail;
        }
        finally
        {
            encoder.Dispose();
        }
        var unsizedFrame = unsized.AsSpan(0, written).ToArray();
        Assert.Equal(id, DeclaredDictionaryId(unsizedFrame));
        Assert.False(CarriesContentSize(unsizedFrame));
        Assert.Equal(payload, V4.Deserialize<string[]>(unsizedFrame, DictionaryOptions)); // the growing decode with the dictionary

        var ours = V4.Serialize(payload, DictionaryOptions);
        var decoder = DecoderWith(dictionary);
        try
        {
            var decoded = new byte[plain.Length + 64];
            Assert.Equal(OperationStatus.Done, decoder.Decompress(ours, decoded, out var consumed, out var produced));
            Assert.Equal(ours.Length, consumed);
            Assert.Equal(plain, decoded.AsSpan(0, produced).ToArray());
        }
        finally
        {
            decoder.Dispose();
        }
    }

    [Fact]
    public void Dictionary_MismatchIsRefused()
    {
        var payload = DictionaryPayload(4);
        var id = IdOfDictionary(Trained.Value);
        var framed = V4.Serialize(payload, DictionaryOptions);

        // a reader with no dictionary, on every entry
        var noDictionary = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(framed, Options));
        Assert.Contains(id.ToString(), noDictionary.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(Split(framed, framed.Length / 2), Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(new MemoryStream(framed), Options));

        // a frame declaring another dictionary (the same frame with its id field flipped) against a reader holding ours
        var other = framed.ToArray();
        var (offset, width) = DictionaryIdField(other);
        Assert.NotEqual(0, width);
        other[offset] ^= 0x01;
        var otherId = DeclaredDictionaryId(other);
        var mismatch = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(other, DictionaryOptions));
        Assert.Contains(otherId.ToString(), mismatch.Message);
        Assert.Contains(id.ToString(), mismatch.Message);

        // a frame declaring no dictionary is read by a reader that holds one: history the frame never references
        var plainFrame = V4.Serialize(payload, Options);
        Assert.Equal(0u, DeclaredDictionaryId(plainFrame));
        Assert.Equal(payload, V4.Deserialize<string[]>(plainFrame, DictionaryOptions));
    }

    // raw content (bytes typical of the messages, no training) is a dictionary too: it has no id, so the frames
    // declare none and a reader cannot tell a mismatch, but the matching pays the same way
    [Fact]
    public void RawContentDictionary_RoundTrips()
    {
        var content = V4.Serialize(Phrases, V4Options.Default);
        var options = V4Options.Default.WithZstandardFrame(content);
        var processor = Assert.IsType<ZstandardFrameProcessor>(options.MessageProcessor);
        Assert.Equal(0u, processor.DictionaryId);

        var payload = DictionaryPayload(5);
        var framed = V4.Serialize(payload, options);
        Assert.Equal(0u, DeclaredDictionaryId(framed));
        var withoutDictionary = V4.Serialize(payload, Options);
        Assert.True(framed.Length < withoutDictionary.Length * 2 / 3, $"the dictionary should pay on this payload: {framed.Length} vs {withoutDictionary.Length}");
        Assert.Equal(payload, V4.Deserialize<string[]>(framed, options));
        Assert.Equal(payload, V4.Deserialize<string[]>(Split(framed, framed.Length / 2), options));
        Assert.Equal(payload, V4.Deserialize<string[]>(new MemoryStream(framed), options));
        Assert.Equal(DictionaryPayload(6), V4.Deserialize<string[]>(V4.Serialize(DictionaryPayload(6), options), options)); // through the cached contexts

        // the frame is dictionary-dependent: without it the codec refuses the content
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string[]>(framed, Options));
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

    static byte[] SkippableFrame(int payloadLength)
    {
        var frame = new byte[8 + payloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, 0x184D2A5A);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), (uint)payloadLength);
        frame.AsSpan(8).Fill(0xAB);
        return frame;
    }

    static byte[] Concat(byte[] left, byte[] right) => [.. left, .. right];

    static ReadOnlySequence<byte> Segmented(byte[] bytes, int length, int segmentSize)
    {
        var first = new ZstdSegment(bytes.AsMemory(0, Math.Min(segmentSize, length)));
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
        var first = new ZstdSegment(bytes.AsMemory(0, at));
        var last = first.Append(bytes.AsMemory(at));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class ZstdSegment : ReadOnlySequenceSegment<byte>
    {
        public ZstdSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public ZstdSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ZstdSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

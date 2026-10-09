using System.Buffers;
using MessagePack;
using NativeCompressions;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// The header-only frame walkers (Lz4FrameWalker / ZstandardFrameWalker, reached through the processors'
// TryFindMessageEnd) against the bindings' own encoders and decoders as the oracle: frames written with every header
// flag the format has (checksums, declared / undeclared content size, block sizes and modes, dictionary ids, RLE and
// stored blocks, skippable frames in front), each delivered cut at every interesting point and in several segment
// shapes. At every cut short of the frame the walker must say "more" with a lower bound inside the frame, at the
// full frame (with unrelated bytes behind it) it must report exactly the length the decoder consumes, and the
// resumed walk (position kept across cuts) must agree with the stateless one everywhere.
public class FrameWalkerDifferentialTests
{
    static byte[] Compressible(int length)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i++)
        {
            data[i] = (byte)(i % 100);
        }
        return data;
    }

    static byte[] Incompressible(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);
        return data;
    }

    static byte[] Run(int length) => Enumerable.Repeat((byte)0x7A, length).ToArray();

    static readonly (string Name, byte[] Payload)[] Payloads =
    [
        ("empty", []),
        ("1 byte", [0x2A]),
        ("1000 compressible", Compressible(1_000)),
        ("100000 incompressible (stored blocks over 64KB)", Incompressible(100_000)),
        ("300000 compressible (several blocks)", Compressible(300_000)),
        ("70000 run (RLE / one match)", Run(70_000)),
    ];

    // a skippable frame the lz4 / zstd tools may emit in front of a frame: magic 0x184D2A50, 4-byte size, data
    static byte[] Skippable(int size)
    {
        var data = new byte[8 + size];
        data[0] = 0x50;
        data[1] = 0x2A;
        data[2] = 0x4D;
        data[3] = 0x18;
        BitConverter.TryWriteBytes(data.AsSpan(4), size);
        return data;
    }

    // ---- LZ4 ----

    // options as factories: ContentSize is init-only, so the declared size goes in at construction
    static readonly (string Name, Func<ulong, LZ4CompressionOptions> Options)[] Lz4Flags =
    [
        ("defaults", size => new LZ4CompressionOptions { ContentSize = size }),
        ("block checksum", size => new LZ4CompressionOptions { ContentSize = size, BlockChecksumFlag = BlockChecksum.BlockChecksumEnabled }),
        ("content checksum", size => new LZ4CompressionOptions { ContentSize = size, ContentChecksumFlag = ContentChecksum.ContentChecksumEnabled }),
        ("both checksums, 256KB blocks", size => new LZ4CompressionOptions { ContentSize = size, BlockChecksumFlag = BlockChecksum.BlockChecksumEnabled, ContentChecksumFlag = ContentChecksum.ContentChecksumEnabled, BlockSizeId = BlockSizeId.Max256KB }),
        ("independent 4MB blocks", size => new LZ4CompressionOptions { ContentSize = size, BlockMode = BlockMode.BlockIndependent, BlockSizeId = BlockSizeId.Max4MB }),
        ("1MB blocks, level 9", size => new LZ4CompressionOptions { ContentSize = size, BlockSizeId = BlockSizeId.Max1MB, CompressionLevel = 9 }),
    ];

    public static IEnumerable<object[]> Lz4Cases()
    {
        foreach (var (payloadName, _) in Payloads)
        {
            foreach (var (flagName, _) in Lz4Flags)
            {
                foreach (var declareSize in new[] { true, false })
                {
                    yield return [payloadName, flagName, declareSize];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Lz4Cases))]
    public void Lz4Walker_AgreesWithTheDecoder(string payloadName, string flagName, bool declareSize)
    {
        var payload = Payloads.Single(p => p.Name == payloadName).Payload;
        var options = Lz4Flags.Single(f => f.Name == flagName).Options(declareSize ? (ulong)payload.Length : 0);
        var frame = Lz4Frame(payload, in options);
        var expected = Lz4OracleLength(frame, payload.Length);
        Assert.Equal(frame.Length, expected); // the oracle consumes exactly the frame we built
        Sweep(V4Options.Default.WithLz4Frame().MessageProcessor!, frame, expected, $"LZ4 {payloadName} / {flagName} / size {(declareSize ? "declared" : "undeclared")}");
    }

    [Fact]
    public void Lz4Walker_CountsSkippableFramesIntoTheMessage()
    {
        var payload = Compressible(5_000);
        var options = new LZ4CompressionOptions { ContentSize = (ulong)payload.Length };
        var frame = Lz4Frame(payload, in options);
        var message = Skippable(37).Concat(Skippable(0)).Concat(frame).ToArray();
        Sweep(V4Options.Default.WithLz4Frame().MessageProcessor!, message, message.Length, "LZ4 behind two skippable frames");
    }

    static byte[] Lz4Frame(byte[] payload, in LZ4CompressionOptions options)
    {
        using var encoder = new LZ4Encoder(in options);
        var output = new ArrayBufferWriter<byte>();
        // in pieces, as the processor streams the staging segments
        var piece = Math.Max(1, payload.Length / 3);
        for (var offset = 0; offset < payload.Length; offset += piece)
        {
            var chunk = payload.AsSpan(offset, Math.Min(piece, payload.Length - offset));
            var destination = output.GetSpan(encoder.GetMaxCompressedLength(chunk.Length, includingHeader: true, includingFooter: false));
            output.Advance(encoder.Compress(chunk, destination));
        }
        var tail = output.GetSpan(encoder.GetMaxFlushBufferLength(includingFooter: true));
        output.Advance(encoder.Close(tail));
        return output.WrittenSpan.ToArray();
    }

    static int Lz4OracleLength(byte[] frame, int payloadLength)
    {
        using var decoder = new LZ4Decoder();
        var destination = new byte[payloadLength + 16];
        var source = new ReadOnlySpan<byte>(frame);
        var consumedTotal = 0;
        var writtenTotal = 0;
        while (true)
        {
            var status = decoder.Decompress(source, destination.AsSpan(writtenTotal), out var consumed, out var written);
            consumedTotal += consumed;
            writtenTotal += written;
            source = source.Slice(consumed);
            if (status == OperationStatus.Done)
            {
                Assert.Equal(payloadLength, writtenTotal);
                return consumedTotal;
            }
            Assert.True(consumed > 0 || written > 0, $"decoder made no progress ({status})");
        }
    }

    // ---- Zstandard ----

    static readonly (string Name, ZstandardCompressionOptions Options)[] ZstdFlags =
    [
        ("level 1", new ZstandardCompressionOptions { CompressionLevel = 1 }),
        ("level 3 + checksum", new ZstandardCompressionOptions { CompressionLevel = 3, ChecksumFlag = true }),
        ("level 19", new ZstandardCompressionOptions { CompressionLevel = 19 }),
    ];

    public static IEnumerable<object[]> ZstdCases()
    {
        foreach (var (payloadName, _) in Payloads)
        {
            foreach (var (flagName, _) in ZstdFlags)
            {
                foreach (var declareSize in new[] { true, false })
                {
                    yield return [payloadName, flagName, declareSize];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ZstdCases))]
    public void ZstandardWalker_AgreesWithTheDecoder(string payloadName, string flagName, bool declareSize)
    {
        var payload = Payloads.Single(p => p.Name == payloadName).Payload;
        var options = ZstdFlags.Single(f => f.Name == flagName).Options;
        var frame = ZstdFrame(payload, in options, declareSize);
        var expected = ZstdOracleLength(frame, payload.Length);
        Assert.Equal(frame.Length, expected);
        Sweep(V4Options.Default.WithZstandardFrame(3).MessageProcessor!, frame, expected, $"Zstandard {payloadName} / {flagName} / size {(declareSize ? "declared" : "undeclared")}");
    }

    [Fact]
    public void ZstandardWalker_CountsSkippableFramesIntoTheMessage()
    {
        var payload = Compressible(5_000);
        var frame = ZstdFrame(payload, new ZstandardCompressionOptions { CompressionLevel = 3 }, declareSize: true);
        var message = Skippable(0).Concat(Skippable(41)).Concat(frame).ToArray();
        Sweep(V4Options.Default.WithZstandardFrame(3).MessageProcessor!, message, message.Length, "Zstandard behind two skippable frames");
    }

    static byte[] ZstdFrame(byte[] payload, in ZstandardCompressionOptions options, bool declareSize)
    {
        using var encoder = new ZstandardEncoder(in options);
        if (declareSize)
        {
            encoder.SetSourceLength(payload.Length);
        }
        var output = new byte[Zstandard.GetMaxCompressedLength(payload.Length) + 64];
        var written = 0;
        var piece = Math.Max(1, payload.Length / 3);
        var offset = 0;
        do
        {
            var chunk = payload.AsSpan(offset, Math.Min(piece, payload.Length - offset));
            var last = offset + chunk.Length >= payload.Length;
            while (true)
            {
                var status = encoder.Compress(chunk, output.AsSpan(written), out var consumed, out var produced, last);
                written += produced;
                chunk = chunk.Slice(consumed);
                Assert.NotEqual(OperationStatus.InvalidData, status);
                if (last ? (status == OperationStatus.Done && chunk.IsEmpty) : chunk.IsEmpty)
                {
                    break;
                }
            }
            offset += piece;
        }
        while (offset < payload.Length);
        return output.AsSpan(0, written).ToArray();
    }

    static int ZstdOracleLength(byte[] frame, int payloadLength)
    {
        using var decoder = new ZstandardDecoder();
        var destination = new byte[payloadLength + 16];
        var source = new ReadOnlySpan<byte>(frame);
        var consumedTotal = 0;
        var writtenTotal = 0;
        while (true)
        {
            var status = decoder.Decompress(source, destination.AsSpan(writtenTotal), out var consumed, out var written);
            consumedTotal += consumed;
            writtenTotal += written;
            source = source.Slice(consumed);
            if (status == OperationStatus.Done)
            {
                Assert.Equal(payloadLength, writtenTotal);
                return consumedTotal;
            }
            Assert.True(consumed > 0 || written > 0, $"decoder made no progress ({status})");
        }
    }

    // ---- hand-built headers: the fields and branches no encoder option reaches ----

    // Zstandard: magic, Frame_Header_Descriptor, optional Window_Descriptor, Dictionary_ID of 0/1/2/4 bytes,
    // Frame_Content_Size of 0/1/2/4/8 bytes, raw / RLE blocks, optional checksum (RFC 8878 3.1.1)
    static byte[] ZstdRawFrame(bool singleSegment, int contentSizeBytes, int dictionaryIdBytes, bool checksum, int[] blockSizes, int lastBlockType = 0)
    {
        var frame = new List<byte> { 0x28, 0xB5, 0x2F, 0xFD };
        var contentSizeFlag = contentSizeBytes switch { 0 => 0, 1 => 0, 2 => 1, 4 => 2, _ => 3 };
        var dictionaryFlag = dictionaryIdBytes switch { 0 => 0, 1 => 1, 2 => 2, _ => 3 };
        frame.Add((byte)((contentSizeFlag << 6) | (singleSegment ? 0x20 : 0) | (checksum ? 0x04 : 0) | dictionaryFlag));
        if (!singleSegment)
        {
            frame.Add(0x10); // window descriptor
        }
        frame.AddRange(new byte[dictionaryIdBytes]);
        frame.AddRange(new byte[contentSizeBytes]);
        for (var i = 0; i < blockSizes.Length; i++)
        {
            var last = i == blockSizes.Length - 1;
            var type = last ? lastBlockType : 0;
            var header = (last ? 1 : 0) | (type << 1) | (blockSizes[i] << 3);
            frame.AddRange([(byte)header, (byte)(header >> 8), (byte)(header >> 16)]);
            frame.AddRange(new byte[type == 1 ? 1 : blockSizes[i]]); // an RLE block carries one byte
        }
        if (checksum)
        {
            frame.AddRange(new byte[4]);
        }
        return frame.ToArray();
    }

    // LZ4: magic, FLG, BD, optional content size (8) and dictionary id (4), header checksum, blocks (4-byte size with
    // the stored bit, data, optional block checksum), end mark, optional content checksum
    static byte[] Lz4RawFrame(bool independent, bool blockChecksum, bool contentSize, bool contentChecksum, bool dictionaryId, int blockSizeId, int[] blockSizes, uint? firstBlockSizeField = null, byte? flg = null, byte[]? magic = null)
    {
        var frame = new List<byte>(magic ?? [0x04, 0x22, 0x4D, 0x18]);
        frame.Add(flg ?? (byte)(0x40 | (independent ? 0x20 : 0) | (blockChecksum ? 0x10 : 0) | (contentSize ? 0x08 : 0) | (contentChecksum ? 0x04 : 0) | (dictionaryId ? 0x01 : 0)));
        frame.Add((byte)(blockSizeId << 4));
        if (contentSize)
        {
            frame.AddRange(new byte[8]);
        }
        if (dictionaryId)
        {
            frame.AddRange(new byte[4]);
        }
        frame.Add(0x00); // header checksum (the walk does not verify it)
        for (var i = 0; i < blockSizes.Length; i++)
        {
            var field = i == 0 && firstBlockSizeField is { } first ? first : 0x80000000u | (uint)blockSizes[i];
            frame.AddRange(BitConverter.GetBytes(field));
            frame.AddRange(new byte[blockSizes[i]]);
            if (blockChecksum)
            {
                frame.AddRange(new byte[4]);
            }
        }
        frame.AddRange(new byte[4]); // end mark
        if (contentChecksum)
        {
            frame.AddRange(new byte[4]);
        }
        return frame.ToArray();
    }

    public static IEnumerable<object[]> ZstdHeaderShapes() =>
    [
        ["1-byte dictionary id", false, 0, 1, false, new[] { 100, 200 }, 0],
        ["2-byte dictionary id, checksum", false, 0, 2, true, new[] { 100 }, 0],
        ["4-byte dictionary id, 4-byte content size", false, 4, 4, false, new[] { 300, 5 }, 0],
        ["8-byte content size", false, 8, 0, true, new[] { 1000 }, 0],
        ["single segment, 1-byte content size, 2-byte dictionary id", true, 1, 2, false, new[] { 7 }, 0],
        ["RLE last block", false, 0, 0, true, new[] { 50, 1000 }, 1],
        ["compressed-typed last block (header walk only)", false, 2, 0, false, new[] { 10, 20 }, 2],
        ["128KB block (the maximum)", false, 0, 0, false, new[] { 128 * 1024 }, 0],
        ["empty raw blocks", false, 0, 0, false, new[] { 0, 0, 0 }, 0],
    ];

    [Theory]
    [MemberData(nameof(ZstdHeaderShapes))]
    public void ZstandardWalker_ReadsEveryHeaderField(string name, bool singleSegment, int contentSizeBytes, int dictionaryIdBytes, bool checksum, int[] blockSizes, int lastBlockType)
    {
        var frame = ZstdRawFrame(singleSegment, contentSizeBytes, dictionaryIdBytes, checksum, blockSizes, lastBlockType);
        Sweep(V4Options.Default.WithZstandardFrame(3).MessageProcessor!, frame, frame.Length, "Zstandard " + name);
    }

    public static IEnumerable<object[]> Lz4HeaderShapes() =>
    [
        ["dictionary id", false, false, false, false, true, 4, new[] { 100, 200 }],
        ["content size + dictionary id + both checksums", false, true, true, true, true, 4, new[] { 100 }],
        ["independent, block checksum", true, true, false, false, false, 5, new[] { 1, 2, 3 }],
        ["1MB blocks, a block over 64KB", false, false, false, true, false, 6, new[] { 70_000, 10 }],
        ["4MB blocks, 1-byte blocks", false, false, false, false, false, 7, new[] { 1, 1, 1, 1 }],
        ["no blocks at all", false, false, true, true, false, 4, Array.Empty<int>()],
    ];

    [Theory]
    [MemberData(nameof(Lz4HeaderShapes))]
    public void Lz4Walker_ReadsEveryHeaderField(string name, bool independent, bool blockChecksum, bool contentSize, bool contentChecksum, bool dictionaryId, int blockSizeId, int[] blockSizes)
    {
        var frame = Lz4RawFrame(independent, blockChecksum, contentSize, contentChecksum, dictionaryId, blockSizeId, blockSizes);
        Sweep(V4Options.Default.WithLz4Frame().MessageProcessor!, frame, frame.Length, "LZ4 " + name);
    }

    // what the walks refuse, as a format error, from the header alone
    [Fact]
    public void Walkers_RefuseMalformedHeaders()
    {
        var zstd = V4Options.Default.WithZstandardFrame(3).MessageProcessor!;
        var lz4 = V4Options.Default.WithLz4Frame().MessageProcessor!;
        static void Refuses(MessagePackMessageProcessor processor, byte[] frame)
        {
            Assert.Throws<MessagePackSerializationException>(() => processor.TryFindMessageEnd(new ReadOnlySequence<byte>(frame), out _));
            long position = 0;
            Assert.Throws<MessagePackSerializationException>(() => processor.TryFindMessageEnd(new ReadOnlySequence<byte>(frame), ref position, out _));
        }

        var zstdReserved = ZstdRawFrame(false, 0, 0, false, [10]);
        zstdReserved[4] |= 0x08; // reserved descriptor bit
        Refuses(zstd, zstdReserved);
        Refuses(zstd, ZstdRawFrame(false, 0, 0, false, [10, 5], lastBlockType: 3)); // reserved block type
        Refuses(zstd, ZstdRawFrame(false, 0, 0, false, [128 * 1024 + 1])); // a block over the 128KB maximum
        Refuses(zstd, [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]); // not a frame

        Refuses(lz4, Lz4RawFrame(false, false, false, false, false, 4, [10], magic: [0x02, 0x21, 0x4C, 0x18])); // the legacy frame format
        Refuses(lz4, Lz4RawFrame(false, false, false, false, false, 4, [10], flg: 0x80)); // wrong version
        Refuses(lz4, Lz4RawFrame(false, false, false, false, false, 3, [10])); // block size id below 4
        Refuses(lz4, Lz4RawFrame(false, false, false, false, false, 4, [10], firstBlockSizeField: 0x80010001u)); // a block one byte over the 64KB maximum (65536 itself is legal)
        Refuses(lz4, [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]); // not a frame
    }

    // a prefix that ends inside a skippable frame reports a lower bound that waits for exactly what is needed next
    [Fact]
    public void Walkers_ReportLowerBounds_InsideSkippableFrames()
    {
        var skippable = Skippable(100);
        foreach (var processor in new[] { V4Options.Default.WithZstandardFrame(3).MessageProcessor!, V4Options.Default.WithLz4Frame().MessageProcessor! })
        {
            Assert.False(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(skippable, 0, 3), out var atMagic));
            Assert.Equal(4, atMagic); // the magic itself
            Assert.False(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(skippable, 0, 6), out var atSize));
            Assert.Equal(8, atSize); // the size field
            Assert.False(processor.TryFindMessageEnd(new ReadOnlySequence<byte>(skippable, 0, 8), out var atData));
            Assert.Equal(108 + 4, atData); // the skippable frame and the next magic
        }
    }

    // ---- the sweep ----

    static void Sweep(MessagePackMessageProcessor processor, byte[] message, int expectedLength, string label)
    {
        Assert.True(processor.DefinesMessageBoundaries);
        // unrelated bytes behind the message: the walker must stop at the message's own end
        var behind = new byte[message.Length + 64];
        message.CopyTo(behind, 0);
        new Random(7).NextBytes(behind.AsSpan(message.Length));

        // the cut points: every byte for a small message, else the header region, every walk position the walker
        // reports (the block headers) with a few bytes around, and random cuts
        var cuts = new SortedSet<int>();
        if (message.Length <= 2_048)
        {
            for (var i = 0; i < message.Length; i++)
            {
                cuts.Add(i);
            }
        }
        else
        {
            for (var i = 0; i < 40; i++)
            {
                cuts.Add(i);
            }
            var random = new Random(message.Length);
            for (var i = 0; i < 200; i++)
            {
                cuts.Add(random.Next(message.Length));
            }
            long probe = 0;
            var at = 0;
            while (at < message.Length)
            {
                processor.TryFindMessageEnd(new ReadOnlySequence<byte>(behind, 0, at), ref probe, out _);
                for (var d = -4; d <= 4; d++)
                {
                    var c = (int)probe + d;
                    if (c >= 0 && c < message.Length)
                    {
                        cuts.Add(c);
                    }
                }
                at = (int)Math.Max(at + 1, probe + 5);
            }
        }

        foreach (var segmentSize in new[] { int.MaxValue, 1, 3, 4_097 })
        {
            if (segmentSize == 1 && message.Length > 2_048)
            {
                continue; // one-byte segments only for the small frames
            }
            long position = 0;
            var previousPosition = 0L;
            foreach (var cut in cuts)
            {
                var prefix = Segmented(behind, cut, segmentSize);
                Assert.False(processor.TryFindMessageEnd(in prefix, out var stateless), $"{label}: a {cut}-byte prefix of {expectedLength} reported an end (segments of {segmentSize})");
                Assert.True(stateless > cut && stateless <= expectedLength, $"{label}: lower bound {stateless} at cut {cut} of {expectedLength}");
                Assert.False(processor.TryFindMessageEnd(in prefix, ref position, out var resumed), $"{label}: resumed walk at cut {cut}");
                Assert.Equal(stateless, resumed);
                Assert.True(position >= previousPosition && position <= expectedLength, $"{label}: position {position} after {previousPosition} at cut {cut}");
                previousPosition = position;
            }
            foreach (var length in new[] { expectedLength, expectedLength + 1, behind.Length })
            {
                var whole = Segmented(behind, length, segmentSize);
                Assert.True(processor.TryFindMessageEnd(in whole, out var found), $"{label}: {length} bytes did not end (segments of {segmentSize})");
                Assert.Equal(expectedLength, found);
                Assert.True(processor.TryFindMessageEnd(in whole, ref position, out var foundResumed));
                Assert.Equal(expectedLength, foundResumed);
            }
        }
    }

    static ReadOnlySequence<byte> Segmented(byte[] bytes, int length, int segmentSize)
    {
        if (segmentSize >= length)
        {
            return new ReadOnlySequence<byte>(bytes, 0, length);
        }
        var first = new Segment(bytes.AsMemory(0, Math.Min(segmentSize, length)));
        var last = first;
        for (var offset = first.Memory.Length; offset < length; offset += segmentSize)
        {
            last = last.Append(bytes.AsMemory(offset, Math.Min(segmentSize, length - offset)));
        }
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

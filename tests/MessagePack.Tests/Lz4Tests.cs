extern alias V3;
using System.Buffers;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MessagePack.LZ4 (MessageProcessor envelope) against the MessagePack-CSharp oracle:
// cross-READ compatibility both directions for both codes (ext 99 Lz4Block / 98
// Lz4BlockArray). Byte identity is NOT asserted for compressed payloads — block
// segmentation and encoder version legitimately differ; the format contract is that
// either side reads the other's output.
public class Lz4Tests
{
    static V4Options BlockOptions { get; } = V4Options.Default.WithLz4Block();
    static V4Options BlockArrayOptions { get; } = V4Options.Default.WithLz4BlockArray();

    static readonly V3::MessagePack.MessagePackSerializerOptions OracleBlock =
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithCompression(V3::MessagePack.MessagePackCompression.Lz4Block);
    static readonly V3::MessagePack.MessagePackSerializerOptions OracleBlockArray =
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithCompression(V3::MessagePack.MessagePackCompression.Lz4BlockArray);

    // ~100KB msgpack spanning multiple pooled segments (multi-block for BlockArray).
    // Periodic values, not random: LZ4 is match-based (LZ77), so patternless low-entropy
    // bytes do NOT compress — random fixints actually expanded by ~0.4% in the first
    // version of this test.
    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    [Fact]
    public void Roundtrip_Ours()
    {
        var big = BigCompressible();
        var text = new string('あ', 10_000);

        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            var raw = V4.Serialize(big, V4Options.Default);
            var wrapped = V4.Serialize(big, options);
            Assert.True(wrapped.Length < raw.Length / 2, $"compression should pay on this payload: {wrapped.Length} vs {raw.Length}");
            Assert.Equal(big, V4.Deserialize<int[]>(wrapped, options));

            Assert.Equal(text, V4.Deserialize<string>(V4.Serialize(text, options), options));
        }
    }

    [Fact]
    public void SmallPayload_WrittenUnwrapped()
    {
        int[] small = [1, 2, 3];
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            // below the threshold the bytes are identical to the uncompressed form...
            Assert.Equal(V4.Serialize(small, V4Options.Default), V4.Serialize(small, options));
            // ...and still deserialize through processor options (transparent passthrough)
            Assert.Equal(small, V4.Deserialize<int[]>(V4.Serialize(small, options), options));
        }
    }

    [Fact]
    public void Incompressible_StillWrapsEnvelope()
    {
        // large random bin: LZ4 expands it (~+0.4%). The envelope is written anyway,
        // exactly like mpc 3.1.8: whether the envelope appears depends only on the size
        // threshold, never on the data content (ratified 2026-08-29; a raw fallback
        // shipped briefly and was reverted for v3 parity)
        var rand = new Random(42);
        var blob = new byte[100_000];
        rand.NextBytes(blob);

        var raw = V4.Serialize(blob, V4Options.Default);
        var block = V4.Serialize(blob, BlockOptions);
        Assert.Equal(0xc9, block[0]); // ext32, type 99 envelope
        Assert.True(block.Length > raw.Length); // expanded, and that is the point
        Assert.Equal(blob, V4.Deserialize<byte[]>(block, BlockOptions));

        var blockArray = V4.Serialize(blob, BlockArrayOptions);
        Assert.True(blockArray[0] >= 0x91 && blockArray[0] <= 0x9f); // [array n+1] envelope
        Assert.Equal(blob, V4.Deserialize<byte[]>(blockArray, BlockArrayOptions));

        // both directions of the mpc cross-read hold for the expanded envelopes too
        Assert.Equal(blob, Oracle.Deserialize<byte[]>(block, OracleBlock));
        Assert.Equal(blob, Oracle.Deserialize<byte[]>(blockArray, OracleBlockArray));
        Assert.Equal(blob, V4.Deserialize<byte[]>(Oracle.Serialize(blob, OracleBlock), BlockOptions));
        Assert.Equal(blob, V4.Deserialize<byte[]>(Oracle.Serialize(blob, OracleBlockArray), BlockArrayOptions));
    }

    [Fact]
    public void UncompressedInput_ReadsThroughProcessorOptions()
    {
        var big = BigCompressible();
        var raw = V4.Serialize(big, V4Options.Default);
        Assert.Equal(big, V4.Deserialize<int[]>(raw, BlockOptions));
        Assert.Equal(big, V4.Deserialize<int[]>(raw, BlockArrayOptions));
    }

    [Fact]
    public void CrossCompat_OursReadByMessagePackCSharp()
    {
        var big = BigCompressible();
        Assert.Equal(big, Oracle.Deserialize<int[]>(V4.Serialize(big, BlockOptions), OracleBlock));
        Assert.Equal(big, Oracle.Deserialize<int[]>(V4.Serialize(big, BlockArrayOptions), OracleBlockArray));
    }

    [Fact]
    public void CrossCompat_MessagePackCSharpReadByOurs()
    {
        var big = BigCompressible();
        Assert.Equal(big, V4.Deserialize<int[]>(Oracle.Serialize(big, OracleBlock), BlockOptions));
        Assert.Equal(big, V4.Deserialize<int[]>(Oracle.Serialize(big, OracleBlockArray), BlockArrayOptions));
        // read side is transparent for both codes regardless of the write mode configured
        Assert.Equal(big, V4.Deserialize<int[]>(Oracle.Serialize(big, OracleBlockArray), BlockOptions));
        Assert.Equal(big, V4.Deserialize<int[]>(Oracle.Serialize(big, OracleBlock), BlockArrayOptions));
    }

    [Fact]
    public void BufferWriterEntry_MatchesArrayEntry()
    {
        var big = BigCompressible();
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            var expected = V4.Serialize(big, options);
            var writer = new ArrayBufferWriter<byte>();
            V4.Serialize(writer, big, options);
            Assert.True(writer.WrittenSpan.SequenceEqual(expected));
        }
    }

    [Fact]
    public void SequenceEntry_UnwrapsAcrossSplits()
    {
        // small-but-compressed payload so the every-position split stays cheap
        var text = new string('x', 300);
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            var wrapped = V4.Serialize(text, options);
            Assert.NotEqual(V4.Serialize(text, V4Options.Default), wrapped); // really enveloped
            for (int splitAt = 0; splitAt <= wrapped.Length; splitAt++)
            {
                var sequence = splitAt == wrapped.Length
                    ? new ReadOnlySequence<byte>(wrapped)
                    : Split(wrapped, splitAt);
                Assert.Equal(text, V4.Deserialize<string>(sequence, options));
            }
        }
    }

    [Fact]
    public void SequenceEntry_PassthroughAcrossSplits()
    {
        // non-enveloped input reads as-is through processor options whatever the
        // segmentation, including shapes that enter (and fail) each sniff branch:
        // fixstr (neither ext nor array) and an ordinary array (the ext-98 branch)
        var text = "passthrough";
        int[] array = [1, 2, 3];
        var textRaw = V4.Serialize(text, V4Options.Default);
        var arrayRaw = V4.Serialize(array, V4Options.Default);
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            for (int splitAt = 1; splitAt < textRaw.Length; splitAt++)
            {
                Assert.Equal(text, V4.Deserialize<string>(Split(textRaw, splitAt), options));
            }
            for (int splitAt = 1; splitAt < arrayRaw.Length; splitAt++)
            {
                Assert.Equal(array, V4.Deserialize<int[]>(Split(arrayRaw, splitAt), options));
            }
        }
    }

    static ReadOnlySequence<byte> Split(byte[] data, int splitAt)
    {
        var second = new Segment(data.AsMemory(splitAt), null, splitAt);
        var first = new Segment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? next, long runningIndex)
        {
            Memory = memory;
            Next = next;
            RunningIndex = runningIndex;
        }
    }

    // ---- decompression-bomb guards (CWE-409): both fire BEFORE any allocation ----

    [Fact]
    public void Bomb_TinyPayloadDeclaringHugeLength_Rejected()
    {
        // a wire claim no amount of decompression can honor: LZ4 expands at most 255:1,
        // so 1 compressed byte declaring int.MaxValue is a provable lie
        byte[] blockBomb = [0xC7, 0x06, 0x63, 0xD2, 0x7F, 0xFF, 0xFF, 0xFF, 0x00]; // ext8(6, 99) { int32 int.MaxValue, 1 lz4 byte }
        var ex1 = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<byte[]>(blockBomb, BlockOptions));
        Assert.Contains("cannot produce", ex1.Message);

        byte[] blockArrayBomb = [0x92, 0xC7, 0x05, 0x62, 0xD2, 0x7F, 0xFF, 0xFF, 0xFF, 0xC4, 0x01, 0x00]; // [ext8(5, 98) { int32 }, bin8(1)]
        var ex2 = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<byte[]>(blockArrayBomb, BlockArrayOptions));
        Assert.Contains("cannot produce", ex2.Message);
    }

    [Fact]
    public void CorruptBlock_DecodeFailure_ThrowsSanctioned_AndReturnsBuffer()
    {
        // declared length is plausible (32 <= 1 compressed byte * 255) so the payload passes
        // the bomb guard and reaches LZ4.Block.Decompress, which rents a 32-byte buffer and
        // then fails on the garbage block. The fix returns that rented buffer before throwing
        // (the ext-99 path used to leak it); we assert the sanctioned exception, and run the
        // failure many times so a leak would show as pool churn / eventual pressure rather
        // than a clean repeat.
        byte[] corrupt = [0xC7, 0x06, 0x63, 0xD2, 0x00, 0x00, 0x00, 0x20, 0x00]; // ext8(6, 99) { int32 32, one 0x00 lz4 byte }
        for (int i = 0; i < 10_000; i++)
        {
            var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<byte[]>(corrupt, BlockOptions));
            Assert.Contains("Invalid LZ4 envelope", ex.Message);
        }
    }

    [Fact]
    public void Bomb_DeclaredOverMaxDecompressedSize_Rejected()
    {
        // 65MB of zeros compresses to ~256KB, and the envelope honestly declares 65MB —
        // over the 64MB default cap, under the raised one. BlockArray exercises the
        // per-block running total (each segment is under the cap, the sum is not)
        var big = new byte[65 * 1024 * 1024];
        foreach (var (capped, uncapped) in new[]
        {
            (BlockOptions, V4Options.Default.WithLz4Block(long.MaxValue)),
            (BlockArrayOptions, V4Options.Default.WithLz4BlockArray(long.MaxValue)),
        })
        {
            var payload = V4.Serialize(big, capped);
            var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<byte[]>(payload, capped));
            Assert.Contains("exceeds the configured maximum", ex.Message);
            Assert.Equal(big, V4.Deserialize<byte[]>(payload, uncapped));
        }
    }
}

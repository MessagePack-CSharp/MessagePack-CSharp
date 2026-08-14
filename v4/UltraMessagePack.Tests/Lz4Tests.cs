using System.Buffers;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;
using UltraOptions = UltraMessagePack.MessagePackSerializerOptions;
using Ultra = UltraMessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// UltraMessagePack.LZ4 (MessageProcessor envelope) against the MessagePack-CSharp oracle:
// cross-READ compatibility both directions for both codes (ext -99 Lz4Block / -98
// Lz4BlockArray). Byte identity is NOT asserted for compressed payloads — block
// segmentation and encoder version legitimately differ; the format contract is that
// either side reads the other's output.
public class Lz4Tests
{
    static UltraOptions BlockOptions { get; } = UltraOptions.Default.WithLz4Block();
    static UltraOptions BlockArrayOptions { get; } = UltraOptions.Default.WithLz4BlockArray();

    static readonly MessagePack.MessagePackSerializerOptions OracleBlock =
        MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4Block);
    static readonly MessagePack.MessagePackSerializerOptions OracleBlockArray =
        MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray);

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
            var raw = Ultra.Serialize(big, UltraOptions.Default);
            var wrapped = Ultra.Serialize(big, options);
            Assert.True(wrapped.Length < raw.Length / 2, $"compression should pay on this payload: {wrapped.Length} vs {raw.Length}");
            Assert.Equal(big, Ultra.Deserialize<int[]>(wrapped, options));

            Assert.Equal(text, Ultra.Deserialize<string>(Ultra.Serialize(text, options), options));
        }
    }

    [Fact]
    public void SmallPayload_WrittenUnwrapped()
    {
        int[] small = [1, 2, 3];
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            // below the threshold the bytes are identical to the uncompressed form...
            Assert.Equal(Ultra.Serialize(small, UltraOptions.Default), Ultra.Serialize(small, options));
            // ...and still deserialize through processor options (transparent passthrough)
            Assert.Equal(small, Ultra.Deserialize<int[]>(Ultra.Serialize(small, options), options));
        }
    }

    [Fact]
    public void Incompressible_FallsBackToRaw()
    {
        // large random bin: LZ4 expands it (~+0.4%); mpc 3.1.8 ships the expanded envelope,
        // we fall back to the raw payload (readers on both sides accept raw transparently)
        var rand = new Random(42);
        var blob = new byte[100_000];
        rand.NextBytes(blob);

        var raw = Ultra.Serialize(blob, UltraOptions.Default);
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            var bytes = Ultra.Serialize(blob, options);
            Assert.Equal(raw, bytes); // no envelope, no expansion
            Assert.Equal(blob, Ultra.Deserialize<byte[]>(bytes, options));
        }
        Assert.Equal(blob, Oracle.Deserialize<byte[]>(Ultra.Serialize(blob, BlockOptions), OracleBlock));
        // and mpc's expanded envelope still reads fine on our side
        Assert.Equal(blob, Ultra.Deserialize<byte[]>(Oracle.Serialize(blob, OracleBlock), BlockOptions));
    }

    [Fact]
    public void UncompressedInput_ReadsThroughProcessorOptions()
    {
        var big = BigCompressible();
        var raw = Ultra.Serialize(big, UltraOptions.Default);
        Assert.Equal(big, Ultra.Deserialize<int[]>(raw, BlockOptions));
        Assert.Equal(big, Ultra.Deserialize<int[]>(raw, BlockArrayOptions));
    }

    [Fact]
    public void CrossCompat_OursReadByMessagePackCSharp()
    {
        var big = BigCompressible();
        Assert.Equal(big, Oracle.Deserialize<int[]>(Ultra.Serialize(big, BlockOptions), OracleBlock));
        Assert.Equal(big, Oracle.Deserialize<int[]>(Ultra.Serialize(big, BlockArrayOptions), OracleBlockArray));
    }

    [Fact]
    public void CrossCompat_MessagePackCSharpReadByOurs()
    {
        var big = BigCompressible();
        Assert.Equal(big, Ultra.Deserialize<int[]>(Oracle.Serialize(big, OracleBlock), BlockOptions));
        Assert.Equal(big, Ultra.Deserialize<int[]>(Oracle.Serialize(big, OracleBlockArray), BlockArrayOptions));
        // read side is transparent for both codes regardless of the write mode configured
        Assert.Equal(big, Ultra.Deserialize<int[]>(Oracle.Serialize(big, OracleBlockArray), BlockOptions));
        Assert.Equal(big, Ultra.Deserialize<int[]>(Oracle.Serialize(big, OracleBlock), BlockArrayOptions));
    }

    [Fact]
    public void BufferWriterEntry_MatchesArrayEntry()
    {
        var big = BigCompressible();
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            var expected = Ultra.Serialize(big, options);
            var writer = new ArrayBufferWriter<byte>();
            Ultra.Serialize(writer, big, options);
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
            var wrapped = Ultra.Serialize(text, options);
            Assert.NotEqual(Ultra.Serialize(text, UltraOptions.Default), wrapped); // really enveloped
            for (int splitAt = 0; splitAt <= wrapped.Length; splitAt++)
            {
                var sequence = splitAt == wrapped.Length
                    ? new ReadOnlySequence<byte>(wrapped)
                    : Split(wrapped, splitAt);
                Assert.Equal(text, Ultra.Deserialize<string>(sequence, options));
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
        var textRaw = Ultra.Serialize(text, UltraOptions.Default);
        var arrayRaw = Ultra.Serialize(array, UltraOptions.Default);
        foreach (var options in new[] { BlockOptions, BlockArrayOptions })
        {
            for (int splitAt = 1; splitAt < textRaw.Length; splitAt++)
            {
                Assert.Equal(text, Ultra.Deserialize<string>(Split(textRaw, splitAt), options));
            }
            for (int splitAt = 1; splitAt < arrayRaw.Length; splitAt++)
            {
                Assert.Equal(array, Ultra.Deserialize<int[]>(Split(arrayRaw, splitAt), options));
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
}

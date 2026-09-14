using System.Buffers;
using MessagePack;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MessagePack.Zstandard (MessageProcessor envelope, ext 96): the same behavioral contract as MessagePack.LZ4
// (threshold, always-envelope above it, transparent passthrough, split tolerance, bomb guards) without a
// v3 oracle, since v3 never had a Zstandard mode. The frame inside the envelope is a standard zstd frame.
public class ZstandardTests
{
    static V4Options Options { get; } = V4Options.Default.WithZstandardEnvelope();

    // ~100KB msgpack with a periodic pattern: zstd's LZ77 stage needs matches to compress at all
    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    [Fact]
    public void Roundtrip()
    {
        var big = BigCompressible();
        var text = new string('あ', 10_000);
        var raw = V4.Serialize(big, V4Options.Default);
        var wrapped = V4.Serialize(big, Options);
        // the envelope header is always ext32: a fixed 11-byte slot lets the frame be compressed in place behind it
        Assert.Equal(MessagePackCode.Ext32, wrapped[0]);
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadExtHeader(wrapped, out var typeCode, out _, out _));
        Assert.Equal(ThisLibraryExtensionTypeCodes.Zstandard, typeCode);
        Assert.True(wrapped.Length < raw.Length / 2, $"compression should pay on this payload: {wrapped.Length} vs {raw.Length}");
        Assert.Equal(big, V4.Deserialize<int[]>(wrapped, Options));
        Assert.Equal(text, V4.Deserialize<string>(V4.Serialize(text, Options), Options));
    }

    [Fact]
    public void Levels_AllRoundtrip_AndHigherLevelsAreNotLarger()
    {
        var big = BigCompressible();
        int? previous = null;
        foreach (var level in new[] { 1, 3, 9, 19 })
        {
            var options = V4Options.Default.WithZstandardEnvelope(level);
            var wrapped = V4.Serialize(big, options);
            Assert.Equal(big, V4.Deserialize<int[]>(wrapped, options));
            // the read side does not depend on the level the frame was written at
            Assert.Equal(big, V4.Deserialize<int[]>(wrapped, Options));
            if (previous is { } p)
            {
                Assert.True(wrapped.Length <= p, $"level {level} produced {wrapped.Length} bytes, more than the previous level's {p}");
            }
            previous = wrapped.Length;
        }
    }

    [Fact]
    public void SmallPayload_WrittenUnwrapped()
    {
        int[] small = [1, 2, 3];
        // below the threshold the bytes are identical to the uncompressed form...
        Assert.Equal(V4.Serialize(small, V4Options.Default), V4.Serialize(small, Options));
        // ...and still deserialize through processor options (transparent passthrough)
        Assert.Equal(small, V4.Deserialize<int[]>(V4.Serialize(small, Options), Options));
    }

    [Fact]
    public void Incompressible_StillWrapsEnvelope()
    {
        // large random bin: zstd stores it as raw blocks (a few bytes of block headers over the input);
        // the envelope is written anyway, the threshold is the only switch
        var rand = new Random(42);
        var blob = new byte[100_000];
        rand.NextBytes(blob);
        var raw = V4.Serialize(blob, V4Options.Default);
        var wrapped = V4.Serialize(blob, Options);
        Assert.Equal(0xc9, wrapped[0]);
        Assert.True(wrapped.Length > raw.Length);
        Assert.Equal(blob, V4.Deserialize<byte[]>(wrapped, Options));
    }

    [Fact]
    public void UncompressedInput_ReadsThroughProcessorOptions()
    {
        var big = BigCompressible();
        var raw = V4.Serialize(big, V4Options.Default);
        Assert.Equal(big, V4.Deserialize<int[]>(raw, Options));
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
    public void SequenceEntry_UnwrapsAcrossSplits()
    {
        // small-but-compressed payload so the every-position split stays cheap
        var text = new string('x', 300);
        var wrapped = V4.Serialize(text, Options);
        Assert.NotEqual(V4.Serialize(text, V4Options.Default), wrapped); // really enveloped
        for (int splitAt = 0; splitAt <= wrapped.Length; splitAt++)
        {
            var sequence = splitAt == wrapped.Length
                ? new ReadOnlySequence<byte>(wrapped)
                : Split(wrapped, splitAt);
            Assert.Equal(text, V4.Deserialize<string>(sequence, Options));
        }
    }

    [Fact]
    public void SequenceEntry_PassthroughAcrossSplits()
    {
        // non-enveloped input reads as-is through processor options whatever the segmentation, including a
        // user-data ext that is not ours (the sniff must decline it, not throw)
        var text = "passthrough";
        var textRaw = V4.Serialize(text, V4Options.Default);
        for (int splitAt = 1; splitAt < textRaw.Length; splitAt++)
        {
            Assert.Equal(text, V4.Deserialize<string>(Split(textRaw, splitAt), Options));
        }
        var timestamp = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        var timestampRaw = V4.Serialize(timestamp, V4Options.Default); // ext -1
        for (int splitAt = 1; splitAt < timestampRaw.Length; splitAt++)
        {
            Assert.Equal(timestamp, V4.Deserialize<DateTime>(Split(timestampRaw, splitAt), Options));
        }
    }

    [Fact]
    public void Bomb_DeclaredOverMaxDecompressedSize_Rejected()
    {
        // a legitimate envelope for 100KB of ints, then read through a processor capped below that size:
        // rejected from the declared length before any decompression buffer is rented
        var big = BigCompressible();
        var payload = V4.Serialize(big, Options);
        var capped = V4Options.Default.WithZstandardEnvelope(3, maxDecompressedSize: 1024);
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(payload, capped));
        Assert.Contains("MaxDecompressedSize", ex.Message);
        // the same envelope reads fine under a generous cap
        Assert.Equal(big, V4.Deserialize<int[]>(payload, V4Options.Default.WithZstandardEnvelope(3, long.MaxValue)));
    }

    [Fact]
    public void Envelope_DeclaredLengthDisagreeingWithFrame_Rejected()
    {
        // the frame written by the encoder carries its content size; an envelope whose int32 says otherwise is
        // corruption and must not be trusted for the buffer size
        var text = new string('x', 300);
        var wrapped = V4.Serialize(text, Options);
        // ext header, then 0xd2 + big-endian int32: bump the declared length's low byte by one
        Assert.Equal(DecodeResult.Success, MessagePackPrimitives.TryReadExtHeader(wrapped, out _, out _, out var extHeaderLength));
        Assert.Equal(0xd2, wrapped[extHeaderLength]);
        var tampered = (byte[])wrapped.Clone();
        tampered[extHeaderLength + 4]++;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(tampered, Options));
    }

    [Fact]
    public void CorruptFrame_ThrowsSanctioned()
    {
        var text = new string('x', 300);
        var wrapped = V4.Serialize(text, Options);
        // clobber the middle of the frame (past the ext header, int32 and zstd frame header)
        var corrupt = (byte[])wrapped.Clone();
        for (int i = 24; i < corrupt.Length - 4; i++) corrupt[i] ^= 0x5a;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(corrupt, Options));
        // and the processor stays usable afterwards (no pooled buffer was leaked or double-returned)
        Assert.Equal(text, V4.Deserialize<string>(wrapped, Options));
    }

    [Fact]
    public void TruncatedEnvelope_Rejected()
    {
        var text = new string('x', 300);
        var wrapped = V4.Serialize(text, Options);
        var truncated = wrapped.AsSpan(0, wrapped.Length - 8).ToArray();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(truncated, Options));
    }

    static ReadOnlySequence<byte> Split(byte[] data, int splitAt)
    {
        var first = new Segment(data.AsMemory(0, splitAt));
        var last = first.Append(data.AsMemory(splitAt));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

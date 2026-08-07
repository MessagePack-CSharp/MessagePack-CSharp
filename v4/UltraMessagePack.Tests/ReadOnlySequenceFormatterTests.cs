using System.Buffers;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// ReadOnlySequence<T>: generic = array format (CollectionFormatters.cs), byte = bin
// format via the primitive tier (ByteArrayFormatters.cs) — both v3-parity, wire-compared
// against the oracle. Deserialize materializes a fresh single-segment sequence.
public class ReadOnlySequenceFormatterTests
{
    sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public Segment<T> Append(ReadOnlyMemory<T> memory)
        {
            var next = new Segment<T>(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<T> MultiSegment<T>(params T[][] chunks)
    {
        var first = new Segment<T>(chunks[0]);
        var last = first;
        for (var i = 1; i < chunks.Length; i++)
        {
            last = last.Append(chunks[i]);
        }
        return new ReadOnlySequence<T>(first, 0, last, last.Memory.Length);
    }

    [Fact]
    public void Generic_SingleSegment_MatchesOracleAndRoundtrips()
    {
        var value = new ReadOnlySequence<int>([1, -1, 128, 70000, int.MinValue]);
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);

        var back = MessagePackSerializer.Deserialize<ReadOnlySequence<int>>(ours);
        Assert.True(back.IsSingleSegment);
        Assert.Equal(value.ToArray(), back.ToArray());
    }

    [Fact]
    public void Generic_MultiSegment_FlattensIdenticallyOnTheWire()
    {
        var multi = MultiSegment([1, 2], [3], [4, 5, 6]);
        Assert.False(multi.IsSingleSegment);

        var ours = MessagePackSerializer.Serialize(multi);
        Assert.Equal(Oracle.Serialize(multi), ours);
        // same bytes as the flattened single-segment sequence
        Assert.Equal(MessagePackSerializer.Serialize(new ReadOnlySequence<int>(multi.ToArray())), ours);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, MessagePackSerializer.Deserialize<ReadOnlySequence<int>>(ours).ToArray());
    }

    [Fact]
    public void Generic_DefaultAndEmpty_RoundtripAsEmptyArray()
    {
        var bytes = MessagePackSerializer.Serialize(default(ReadOnlySequence<int>));
        Assert.Equal([0x90], bytes); // fixarray0, not nil (a sequence has no null state)
        Assert.Equal(0, MessagePackSerializer.Deserialize<ReadOnlySequence<int>>(bytes).Length);
    }

    [Fact]
    public void Byte_UsesBinFormat_MatchesOracleAndRoundtrips()
    {
        byte[] payload = [1, 2, 3, 200, 255];
        var value = new ReadOnlySequence<byte>(payload);

        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        Assert.Equal(MessagePackSerializer.Serialize(payload), ours); // same bin framing as byte[]

        Assert.Equal(payload, MessagePackSerializer.Deserialize<ReadOnlySequence<byte>>(ours).ToArray());
    }

    [Fact]
    public void Byte_MultiSegment_FlattensIdenticallyOnTheWire()
    {
        var multi = MultiSegment<byte>([1, 2], [3, 4, 5]);
        Assert.False(multi.IsSingleSegment);

        var ours = MessagePackSerializer.Serialize(multi);
        Assert.Equal(Oracle.Serialize(multi), ours);
        Assert.Equal(MessagePackSerializer.Serialize(new byte[] { 1, 2, 3, 4, 5 }), ours);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, MessagePackSerializer.Deserialize<ReadOnlySequence<byte>>(ours).ToArray());
    }

    [Fact]
    public void Byte_Default_IsEmptyBin()
    {
        var bytes = MessagePackSerializer.Serialize(default(ReadOnlySequence<byte>));
        Assert.Equal([0xc4, 0x00], bytes); // bin8, length 0
        Assert.Equal(0, MessagePackSerializer.Deserialize<ReadOnlySequence<byte>>(bytes).Length);
    }
}

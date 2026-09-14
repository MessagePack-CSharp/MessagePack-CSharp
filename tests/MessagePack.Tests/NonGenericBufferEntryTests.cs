using System.Buffers;
using SerializerFoundation;

namespace MessagePack.Tests;

// The buffer-level Type entries: the non-generic twins of Serialize<TWriteBuffer, T>(ref buffer, ...) and
// Deserialize<TReadBuffer, T>(ref buffer, ...), for protocols that embed values inside their own envelope and only know
// the runtime Type (hub protocols, formatters). One value in, one value out, the surrounding bytes untouched.
public class NonGenericBufferEntryTests
{
    static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Default;

    // [300, "abc"] as fixarray2, uint16, fixstr3
    static readonly byte[] Envelope = [0x92, 0xcd, 0x01, 0x2c, 0xa3, (byte)'a', (byte)'b', (byte)'c'];

    [Fact]
    public void WriteBuffer_TypeEntry_WritesIntoTheOpenBuffer()
    {
        var writer = new ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            buffer.WriteArrayHeader(2);
            MessagePackSerializer.Serialize(typeof(int), ref buffer, 300, Options);
            MessagePackSerializer.Serialize(typeof(string), ref buffer, "abc", Options);
        }
        finally
        {
            buffer.Dispose();
        }
        Assert.Equal(Envelope, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void WriteBuffer_TypeEntry_MatchesTheGenericEntry()
    {
        var value = new List<int> { 1, 300, -70000 };
        var expected = new ArrayBufferWriter<byte>();
        var expectedBuffer = new BufferWriterWriteBuffer(expected);
        MessagePackSerializer.Serialize<BufferWriterWriteBuffer, List<int>>(ref expectedBuffer, value, Options);
        expectedBuffer.Dispose();

        var actual = new ArrayBufferWriter<byte>();
        var actualBuffer = new BufferWriterWriteBuffer(actual);
        MessagePackSerializer.Serialize(typeof(List<int>), ref actualBuffer, value, Options);
        actualBuffer.Dispose();

        Assert.Equal(expected.WrittenSpan.ToArray(), actual.WrittenSpan.ToArray());
    }

    [Fact]
    public void ReadBuffer_TypeEntry_ReadsOneValueAndLeavesTheRest()
    {
        byte[] bytes = [.. Envelope, 0xc3];
        var buffer = new ReadOnlySpanReadBuffer(bytes);
        Assert.Equal(2, buffer.ReadArrayHeader());
        Assert.Equal(300, MessagePackSerializer.Deserialize(typeof(int), ref buffer, Options));
        Assert.Equal("abc", MessagePackSerializer.Deserialize(typeof(string), ref buffer, Options));
        Assert.Equal(1, buffer.BytesRemaining);
        Assert.Equal(0xc3, buffer.GetUnreadSpan()[0]);
    }

    [Fact]
    public void ReadBuffer_TypeEntry_OverASplitSequence()
    {
        byte[] bytes = [.. Envelope, 0xc3];
        var first = new Segment(bytes.AsMemory(0, 3));
        var last = first.Append(bytes.AsMemory(3));
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            Assert.Equal(2, buffer.ReadArrayHeader());
            Assert.Equal(300, MessagePackSerializer.Deserialize(typeof(int), ref buffer, Options)); // straddles the split
            Assert.Equal("abc", MessagePackSerializer.Deserialize(typeof(string), ref buffer, Options));
            Assert.Equal(1, buffer.BytesRemaining);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void ValueMustBeAnInstanceOfTheType()
    {
        var writer = new ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var local = new BufferWriterWriteBuffer(new ArrayBufferWriter<byte>());
                MessagePackSerializer.Serialize(typeof(int), ref local, "not an int", Options);
            });
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void MessageProcessorOptions_AreRejected()
    {
        var options = Options.WithLz4BlockArray();
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new BufferWriterWriteBuffer(new ArrayBufferWriter<byte>());
            MessagePackSerializer.Serialize(typeof(int), ref buffer, 1, options);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new ReadOnlySpanReadBuffer([0x01]);
            MessagePackSerializer.Deserialize(typeof(int), ref buffer, options);
        });
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

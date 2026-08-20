using System.Buffers;
using MessagePack;
using SerializerFoundation;

namespace UltraMessagePack.Tests;

[MessagePackObject]
public class BufferPerson
{
    [Key(0)] public int Age { get; set; }
    [Key(1)] public string? Name { get; set; }
}

// The raw-buffer entries (Serialize/Deserialize over IWriteBuffer/IReadBuffer, net9+
// only): the embed-inside-another-protocol scenario — several values written into one
// caller-owned buffer, read back one by one from one caller-owned read buffer.
public class BufferEntryTests
{

    [Fact]
    public void RawBuffer_SequentialValues_RoundTrip()
    {
        var output = new ArrayBufferWriter<byte>();
        var writeBuffer = new BufferWriterWriteBuffer(output);
        MessagePackSerializer.Serialize(ref writeBuffer, 42);
        MessagePackSerializer.Serialize(ref writeBuffer, "embedded");
        MessagePackSerializer.Serialize(ref writeBuffer, new BufferPerson { Age = 3, Name = "b" });
        writeBuffer.Flush(); // the entry does not flush: the caller owns the buffer

        var readBuffer = new ReadOnlySpanReadBuffer(output.WrittenSpan);
        Assert.Equal(42, MessagePackSerializer.Deserialize<ReadOnlySpanReadBuffer, int>(ref readBuffer));
        Assert.Equal("embedded", MessagePackSerializer.Deserialize<ReadOnlySpanReadBuffer, string>(ref readBuffer));
        var person = MessagePackSerializer.Deserialize<ReadOnlySpanReadBuffer, BufferPerson>(ref readBuffer);
        Assert.Equal(3, person.Age);
        Assert.Equal("b", person.Name);
        Assert.Equal(0, readBuffer.BytesRemaining); // exactly the three values consumed
    }

    [Fact]
    public void RawBuffer_MatchesByteArrayEntryWire()
    {
        var value = new BufferPerson { Age = 7, Name = "wire" };
        var output = new ArrayBufferWriter<byte>();
        var writeBuffer = new BufferWriterWriteBuffer(output);
        MessagePackSerializer.Serialize(ref writeBuffer, value, MessagePackSerializerOptions.Default);
        writeBuffer.Flush();

        Assert.Equal(MessagePackSerializer.Serialize(value), output.WrittenArray());
    }

    [Fact]
    public void RawBuffer_ProcessorOptions_Rejected()
    {
        // the buffer-level entries never see the complete message, so options carrying a
        // MessageProcessor are rejected instead of silently skipping the envelope
        var options = MessagePackSerializerOptions.Default.WithLz4Block();

        // ref struct buffers cannot cross a lambda, so no Assert.Throws here
        var writeBuffer = new BufferWriterWriteBuffer(new ArrayBufferWriter<byte>());
        var writeRejected = false;
        try { MessagePackSerializer.Serialize(ref writeBuffer, 1, options); }
        catch (ArgumentException) { writeRejected = true; }
        Assert.True(writeRejected);

        var readBuffer = new ReadOnlySpanReadBuffer(MessagePackSerializer.Serialize(1));
        var readRejected = false;
        try { MessagePackSerializer.Deserialize<ReadOnlySpanReadBuffer, int>(ref readBuffer, options); }
        catch (ArgumentException) { readRejected = true; }
        Assert.True(readRejected);
    }

    [Fact]
    public void RawBuffer_PopulateForm_ReusesInstance()
    {
        var payload = MessagePackSerializer.Serialize(new BufferPerson { Age = 9, Name = "p" });
        var readBuffer = new ReadOnlySpanReadBuffer(payload);
        var target = new BufferPerson();
        MessagePackSerializer.Deserialize(ref readBuffer, ref target, MessagePackSerializerOptions.Default);
        Assert.Equal(9, target.Age);
        Assert.Equal("p", target.Name);
    }
}

static file class ArrayBufferWriterExtensions
{
    public static byte[] WrittenArray(this ArrayBufferWriter<byte> writer) => writer.WrittenSpan.ToArray();
}

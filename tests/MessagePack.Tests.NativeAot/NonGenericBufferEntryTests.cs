using System.Buffers;
using MessagePack;
using SerializerFoundation;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.NativeAot;

// The buffer-level Type entries close a generic virtual method of the bridge over the buffer type; ILC has to generate
// that instantiation for every registered bridge (reference and value types alike), which is what these exercise.
public class NonGenericBufferEntryTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
    [
        MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
        AotRootFactory.Instance,
        BuiltInFormatterFactory.Instance,
    ]));

    [Fact]
    public void WriteAndRead_EmbeddedValues_ThroughTypeEntries()
    {
        var poco = new AotIntKeyPoco { Id = 7, Count = 300, Name = "abc" };
        var value = new AotStructPoco { X = 1, Y = -2 };

        var writer = new ArrayBufferWriter<byte>();
        var writeBuffer = new BufferWriterWriteBuffer(writer);
        try
        {
            writeBuffer.WriteArrayHeader(4);
            V4.Serialize(typeof(AotIntKeyPoco), ref writeBuffer, poco, Options);
            V4.Serialize(typeof(AotStructPoco), ref writeBuffer, value, Options);
            V4.Serialize(typeof(int), ref writeBuffer, 5, Options);
            V4.Serialize(typeof(AotIntKeyPoco[]), ref writeBuffer, new[] { poco }, Options);
        }
        finally
        {
            writeBuffer.Dispose();
        }

        var readBuffer = new ReadOnlySpanReadBuffer(writer.WrittenSpan);
        Assert.Equal(4, readBuffer.ReadArrayHeader());
        var backPoco = Assert.IsType<AotIntKeyPoco>(V4.Deserialize(typeof(AotIntKeyPoco), ref readBuffer, Options));
        Assert.Equal("abc", backPoco.Name);
        Assert.Equal(value, Assert.IsType<AotStructPoco>(V4.Deserialize(typeof(AotStructPoco), ref readBuffer, Options)));
        Assert.Equal(5, V4.Deserialize(typeof(int), ref readBuffer, Options));
        var array = Assert.IsType<AotIntKeyPoco[]>(V4.Deserialize(typeof(AotIntKeyPoco[]), ref readBuffer, Options));
        Assert.Equal(300, Assert.Single(array).Count);
        Assert.Equal(0, readBuffer.BytesRemaining);

        // the sequence-backed buffer takes the same generic virtual instantiation path
        var sequence = new ReadOnlySequence<byte>(writer.WrittenMemory);
        var sequenceBuffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            Assert.Equal(4, sequenceBuffer.ReadArrayHeader());
            Assert.Equal(7, Assert.IsType<AotIntKeyPoco>(V4.Deserialize(typeof(AotIntKeyPoco), ref sequenceBuffer, Options)).Id);
        }
        finally
        {
            sequenceBuffer.Dispose();
        }
    }
}

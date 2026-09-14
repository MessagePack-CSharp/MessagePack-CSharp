using System.Buffers;
using MessagePack;
using MessagePack.SignalR;
using SerializerFoundation;

namespace MessagePack.Tests.AspNetCore;

public class HubProtocolOptionsTests
{
    [Fact]
    public void MessageProcessorOptions_AreRejectedAtConstruction()
    {
        // arguments live inside the envelope and go through the buffer-level entries, which have no whole message for
        // a processor to encode; the protocol says so up front instead of failing on the first invocation
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default]))
        {
            MessageProcessor = new StubProcessor(),
        };
        var exception = Assert.Throws<ArgumentException>(() => new MessagePackHubProtocol(options));
        Assert.Contains("MessageProcessor", exception.Message);
    }

    [Fact]
    public void PlainOptions_AreAccepted()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default]));
        var protocol = new MessagePackHubProtocol(options);
        Assert.Equal("messagepack", protocol.Name);
    }

    sealed class StubProcessor : MessagePackMessageProcessor
    {
        public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output) => throw new NotSupportedException();

        public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message) => throw new NotSupportedException();

        public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message) => throw new NotSupportedException();
    }
}

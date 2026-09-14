using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace MessagePack.Tests;

// FramingProcessor: ext32 envelope carrying the message verbatim, decoded as a slice of the source (no copy) on every
// entry, and the async entries' boundary scanner skipping the payload from the header.
public class FramingProcessorTests
{
    static readonly MessagePackSerializerOptions Framed = MessagePackSerializerOptions.Default.WithFraming();
    static readonly MessagePackSerializerOptions Plain = MessagePackSerializerOptions.Default;

    static Dictionary<string, int[]> Value() => new()
    {
        ["a"] = [1, 300, -70000, int.MaxValue],
        ["b"] = [],
        [new string('あ', 5000)] = Enumerable.Range(0, 2000).ToArray(),
    };

    [Fact]
    public void Envelope_IsExt32HeaderPlusThePlainBytes()
    {
        var plain = MessagePackSerializer.Serialize(Value(), Plain);
        var framed = MessagePackSerializer.Serialize(Value(), Framed);
        Assert.Equal(plain.Length + 6, framed.Length);
        Assert.Equal(MessagePackCode.Ext32, framed[0]);
        Assert.Equal((uint)plain.Length, BinaryPrimitives.ReadUInt32BigEndian(framed.AsSpan(1)));
        Assert.Equal(unchecked((byte)ThisLibraryExtensionTypeCodes.Framing), framed[5]);
        Assert.Equal(plain, framed.AsSpan(6).ToArray());

        // tiny messages keep the fixed 6-byte form too: the reader recognizes the envelope from a fixed prefix
        var small = MessagePackSerializer.Serialize(1, Framed);
        Assert.Equal(new byte[] { 0xc9, 0, 0, 0, 1, unchecked((byte)ThisLibraryExtensionTypeCodes.Framing), 0x01 }, small);
    }

    [Fact]
    public void Roundtrip_EverySyncEntry()
    {
        var value = Value();
        var framed = MessagePackSerializer.Serialize(value, Framed);

        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(framed, Framed));
        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(new ReadOnlySequence<byte>(framed), Framed));

        // splits inside the header and inside the payload
        foreach (var split in new[] { 1, 5, 6, 7, framed.Length / 2, framed.Length - 1 })
        {
            Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(Split(framed, split), Framed));
        }

        // the pooled stream path and the exposable MemoryStream path
        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(new MemoryStream(framed), Framed));
        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(new MemoryStream(framed, 0, framed.Length, false, publiclyVisible: true), Framed));

        // the stream writer takes the buffer-level TryEncode
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(stream, value, Framed);
        Assert.Equal(framed, stream.ToArray());
    }

    [Fact]
    public async Task Pipe_ScannerFindsTheEndFromTheHeader()
    {
        var value = Value();
        var framed = MessagePackSerializer.Serialize(value, Framed);
        var pipe = new Pipe();

        // the writer never completes and the bytes arrive in two pieces, so the reader must find the boundary itself:
        // with the envelope that is one ext32 header, not a walk over every token
        var read = MessagePackSerializer.DeserializeAsync<Dictionary<string, int[]>>(pipe.Reader, Framed).AsTask();
        await pipe.Writer.WriteAsync(framed.AsMemory(0, 100));
        Assert.False(read.IsCompleted);
        await pipe.Writer.WriteAsync(framed.AsMemory(100));
        Assert.Equal(value, await read);

        // the writer side frames too
        var output = new Pipe();
        await MessagePackSerializer.SerializeAsync(output.Writer, value, Framed);
        await output.Writer.CompleteAsync();
        var result = await output.Reader.ReadAsync();
        Assert.Equal(framed, result.Buffer.ToArray());
    }

    [Fact]
    public async Task Pipe_BogusFrameLength_IsRejectedFromTheHeader()
    {
        // a header declaring more than MaxBufferedMessageSize is refused as soon as the scanner sees it, before any of
        // the payload is waited for or buffered; the writer is never completed here, so waiting would hang
        var small = Framed with { MaxBufferedMessageSize = 1024 };
        var pipe = new Pipe();
        var header = new byte[] { 0xc9, 0, 0, 0x10, 0, unchecked((byte)ThisLibraryExtensionTypeCodes.Framing) }; // 4096 bytes declared
        await pipe.Writer.WriteAsync(header);
        var enumerator = MessagePackSerializer.DeserializeMessagesAsync<int>(pipe.Reader, small).GetAsyncEnumerator();
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await enumerator.MoveNextAsync());

        // a header whose declared length outruns the data the completed writer delivered: truncated, not hung
        var truncated = new Pipe();
        var framed = MessagePackSerializer.Serialize(Value(), Framed);
        await truncated.Writer.WriteAsync(framed.AsMemory(0, framed.Length - 10));
        await truncated.Writer.CompleteAsync();
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
        {
            await foreach (var _ in MessagePackSerializer.DeserializeMessagesAsync<Dictionary<string, int[]>>(truncated.Reader, Framed))
            {
            }
        });

        // a header declaring fewer bytes than the message really has: the frame parses as a truncated value, the
        // reader is advanced past that frame, and the exception names the data error
        var short_ = (byte[])framed.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(short_.AsSpan(1), (uint)(framed.Length - 6 - 100));
        var shortPipe = new Pipe();
        await shortPipe.Writer.WriteAsync(short_);
        var shortEnumerator = MessagePackSerializer.DeserializeMessagesAsync<Dictionary<string, int[]>>(shortPipe.Reader, Framed).GetAsyncEnumerator();
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await shortEnumerator.MoveNextAsync());
        await shortEnumerator.DisposeAsync();
        var rest = await shortPipe.Reader.ReadAsync();
        Assert.Equal(100, rest.Buffer.Length); // the 100 bytes after the declared frame are what remains
    }

    [Fact]
    public void UnframedInput_IsReadAsIs()
    {
        var value = Value();
        var plain = MessagePackSerializer.Serialize(value, Plain);
        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(plain, Framed));
        Assert.Equal(value, MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(Split(plain, 3), Framed));
    }

    [Fact]
    public void TruncatedFrame_Throws()
    {
        var framed = MessagePackSerializer.Serialize(Value(), Framed);
        var truncated = framed.AsSpan(0, framed.Length - 1).ToArray();
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(truncated, Framed));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Dictionary<string, int[]>>(Split(truncated, 8), Framed));
    }

    [Fact]
    public void SourceSlice_OutsideTheSource_IsAProcessorBug()
    {
        var options = Plain with { MessageProcessor = new BadSliceProcessor() };
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Deserialize<int>(new byte[] { 0x01 }, options));
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Deserialize<int>(Split([0x01, 0x02], 1), options));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecodedMessage.SourceSlice(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecodedMessage.SourceSlice(0, -1));
    }

    [Fact]
    public void DecodedMessage_IsAUnionOfTheTwoCases()
    {
        DecodedMessage slice = new DecodedMessage.SourceSlice(6, 10);
        Assert.True(slice is DecodedMessage.SourceSlice { Offset: 6, Length: 10 });
        Assert.False(slice is DecodedMessage.Buffer);
        Assert.IsType<DecodedMessage.SourceSlice>(slice.Value);

        var owner = new Owner();
        DecodedMessage buffer = new DecodedMessage(new ReadOnlySequence<byte>(new byte[3]), owner);
        Assert.True(buffer is DecodedMessage.Buffer { Sequence.Length: 3 });
        Assert.False(buffer is DecodedMessage.SourceSlice);
        buffer.Dispose();
        Assert.True(owner.Disposed);

        Assert.Null(default(DecodedMessage).Value);
        default(DecodedMessage).Dispose(); // nothing to release
    }

    sealed class Owner : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    sealed class BadSliceProcessor : MessagePackMessageProcessor
    {
        public override bool TryEncode(ref SerializerFoundation.BufferSegments message, IBufferWriter<byte> output) => false;

        public override bool TryEncode<TWriteBuffer>(ref SerializerFoundation.BufferSegments message, ref TWriteBuffer output) => false;

        public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
        {
            message = new DecodedMessage.SourceSlice(0, 1000);
            return true;
        }

        public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
        {
            message = new DecodedMessage.SourceSlice(0, 1000);
            return true;
        }
    }

    static ReadOnlySequence<byte> Split(byte[] bytes, int at)
    {
        var first = new Segment(bytes.AsMemory(0, at));
        var last = first.Append(bytes.AsMemory(at));
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

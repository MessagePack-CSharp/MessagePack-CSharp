using System.Buffers;

namespace MessagePack.Tests;

// MessagePackReader, mostly reading MessagePackWriter output back. Also compiled into MessagePack.Tests.Net8 and
// MessagePack.Tests.NetFx (the netstandard assets), where the reader has no ReadOnlySpan<byte> constructor and an array
// is read through ReadOnlyMemory<byte>.
public class MessagePackReaderTests
{
    static readonly DateTime Timestamp = new(2026, 10, 30, 12, 34, 56, 789, DateTimeKind.Utc);

    static byte[] WriteEnvelope()
    {
        using var writer = new MessagePackWriter();
        writer.WriteArrayHeader(9);
        writer.WriteInt32(-12345);
        writer.WriteString("Add");
        writer.Serialize(new ManualEnvelopeBody { Id = 7, Name = "body" }, MessagePackSerializerOptions.Default);
        writer.WriteNil();
        writer.WriteBoolean(true);
        writer.WriteDouble(1.5);
        writer.WriteTimestamp(Timestamp);
        writer.WriteBinary(new byte[] { 1, 2, 3 });
        writer.WriteExtHeader(5, 2);
        writer.WriteRaw(new byte[] { 0xAA, 0xBB });
        return writer.ToArray();
    }

    static void ReadEnvelope(ref MessagePackReader reader)
    {
        Assert.Equal(9, reader.ReadArrayHeader());
        Assert.Equal(-12345, reader.ReadInt32());
        Assert.Equal("Add", reader.ReadString());
        var body = reader.Deserialize<ManualEnvelopeBody>(MessagePackSerializerOptions.Default);
        Assert.Equal(7, body.Id);
        Assert.Equal("body", body.Name);
        Assert.True(reader.TryReadNil());
        Assert.True(reader.ReadBoolean());
        Assert.Equal(1.5, reader.ReadDouble());
        Assert.Equal(Timestamp, reader.ReadTimestamp());
        Assert.Equal(new byte[] { 1, 2, 3 }, reader.ReadBinary());
        var (typeCode, dataLength) = reader.ReadExtHeader();
        Assert.Equal(5, typeCode);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, reader.ReadRaw(dataLength));
        Assert.True(reader.End);
    }

    [Fact]
    public void Envelope_RoundTrips()
    {
        var bytes = WriteEnvelope();
        // a using local is read-only and cannot be passed by ref, so a reader handed to a helper is disposed in a finally
        var reader = new MessagePackReader(bytes);
        try
        {
            ReadEnvelope(ref reader);
            Assert.Equal(bytes.Length, reader.BytesConsumed);
        }
        finally
        {
            reader.Dispose();
        }
    }

    [Fact]
    public void Envelope_RoundTripsAcrossOneByteSegments()
    {
        // every token straddles a seam, the Deserialize<T> value and the ReadRaw payload included
        var reader = new MessagePackReader(OneByteSegments(WriteEnvelope()));
        try
        {
            ReadEnvelope(ref reader);
        }
        finally
        {
            reader.Dispose();
        }
    }

    [Fact]
    public void Envelope_RoundTripsFromMemory()
    {
        var reader = new MessagePackReader(WriteEnvelope().AsMemory());
        try
        {
            ReadEnvelope(ref reader);
        }
        finally
        {
            reader.Dispose();
        }
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void Envelope_RoundTripsFromStackMemory()
    {
        var bytes = WriteEnvelope();
        Span<byte> stack = stackalloc byte[bytes.Length];
        bytes.CopyTo(stack);
        var reader = new MessagePackReader(stack);
        try
        {
            ReadEnvelope(ref reader);
        }
        finally
        {
            reader.Dispose();
        }
    }
#endif

    [Fact]
    public void ReadRaw_ReturnsTheValueForWriteRaw()
    {
        var nested = MessagePackSerializer.Serialize(new Dictionary<string, int[]> { ["a"] = new[] { 1, 2 }, ["b"] = new int[0] });
        using var writer = new MessagePackWriter();
        writer.WriteArrayHeader(2);
        writer.WriteRaw(nested);
        writer.WriteInt32(3);

        using var reader = new MessagePackReader(OneByteSegments(writer.ToArray()));
        Assert.Equal(2, reader.ReadArrayHeader());
        var raw = reader.ReadRaw();
        Assert.Equal(nested, raw);
        Assert.Equal(3, reader.ReadInt32());

        using var copy = new MessagePackWriter();
        copy.WriteRaw(raw);
        Assert.Equal(nested, copy.ToArray());
    }

    [Fact]
    public void ReadRaw_LengthReadsThePayloadAfterAHeader()
    {
        using var writer = new MessagePackWriter();
        writer.WriteBinHeader(3);
        writer.WriteRaw(new byte[] { 7, 8, 9 });
        writer.WriteInt32(1);

        using var reader = new MessagePackReader(OneByteSegments(writer.ToArray()));
        Assert.Equal(new byte[] { 7, 8, 9 }, reader.ReadRaw(reader.ReadBinHeader()));
        Assert.Equal(1, reader.ReadInt32());
        Assert.True(reader.End);
    }

    [Fact]
    public void ReadRaw_IntoASpanReadsThePayloadWithoutAnArray()
    {
        using var writer = new MessagePackWriter();
        writer.WriteExtHeader(5, 4);
        writer.WriteRaw(new byte[] { 1, 2, 3, 4 });
        writer.WriteInt32(1);

        using var reader = new MessagePackReader(OneByteSegments(writer.ToArray()));
        var (_, dataLength) = reader.ReadExtHeader();
        Span<byte> payload = stackalloc byte[dataLength];
        reader.ReadRaw(payload);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, payload.ToArray());
        Assert.Equal(1, reader.ReadInt32());

        var thrown = false;
        try { reader.ReadRaw(stackalloc byte[1]); }
        catch (MessagePackSerializationException) { thrown = true; }
        Assert.True(thrown);
    }

    [Fact]
    public void ReadRaw_LengthPastTheDataThrows()
    {
        using var reader = new MessagePackReader(new byte[] { 0x01, 0x02 });
        var thrown = false;
        try { reader.ReadRaw(3); }
        catch (MessagePackSerializationException) { thrown = true; }
        Assert.True(thrown);
        Assert.Equal(0, reader.BytesConsumed);
    }

    [Fact]
    public void Peek_DoesNotMoveTheReader()
    {
        using var writer = new MessagePackWriter();
        writer.WriteMapHeader(1);
        writer.WriteString("k");
        writer.WriteInt32(1);
        using var reader = new MessagePackReader(writer.ToArray());

        Assert.Equal(MessagePackType.Map, reader.NextMessagePackType);
        Assert.True(reader.TryPeek(out var code));
        Assert.Equal(0x81, code);
        Assert.Equal(0, reader.BytesConsumed);
        Assert.Equal(1, reader.ReadMapHeader());
        reader.Skip();
        reader.Skip();
        Assert.True(reader.End);
        Assert.False(reader.TryPeek(out _));

        var thrown = false;
        try { _ = reader.NextMessagePackType; }
        catch (MessagePackSerializationException) { thrown = true; }
        Assert.True(thrown);
    }

    [Fact]
    public void TypeBasedDeserialize_RoundTrips()
    {
        using var writer = new MessagePackWriter();
        writer.Serialize(typeof(ManualEnvelopeBody), new ManualEnvelopeBody { Id = 3, Name = "t" }, MessagePackSerializerOptions.Default);
        writer.Serialize(typeof(int), 42, MessagePackSerializerOptions.Default);

        using var reader = new MessagePackReader(writer.ToArray());
        var body = (ManualEnvelopeBody)reader.Deserialize(typeof(ManualEnvelopeBody), MessagePackSerializerOptions.Default)!;
        Assert.Equal(3, body.Id);
        Assert.Equal(42, reader.Deserialize(typeof(int), MessagePackSerializerOptions.Default));
        Assert.True(reader.End);
    }

    [Fact]
    public void ProcessorOptions_Rejected()
    {
        var options = MessagePackSerializerOptions.Default with { MessageProcessor = new FramingProcessor() };
        using var reader = new MessagePackReader(new byte[] { 0x01 });
        var rejected = false;
        try { reader.Deserialize<int>(options); }
        catch (ArgumentException) { rejected = true; }
        Assert.True(rejected);
    }

    [Fact]
    public void Reader_IsMarkedNonCopyableAndDisposable()
    {
        Assert.True(typeof(MessagePackReader).IsDefined(typeof(SerializerFoundation.CodeAnalysis.NonCopyableAttribute), inherit: false));
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(MessagePackReader)));
    }

    // the header overloads that charge a DeserializeState are formatter-only, and the payload reads ReadRaw(int) and
    // ReadRaw(Span<byte>) are the reader's own
    [Fact]
    public void TokenMethods_MatchTheBufferExtensions()
    {
        var extensions = BufferExtensionParity.ExtensionSignatures(typeof(ReadBufferExtensions)).Where(s => !s.Contains(nameof(DeserializeState))).ToArray();
        var members = BufferExtensionParity.InstanceSignatures(typeof(MessagePackReader), "Read", "Try", "Skip");
        Assert.Equal(extensions, members.Where(s => s is not ("ReadRaw(Int32)" or "ReadRaw(Span`1)")).ToArray());
    }

#if NET9_0_OR_GREATER
    // a formatter from a netstandard-only library (CompatibilityFallbackRoutingTests' factory) throws, as at the
    // buffer-level entries the two forward to, instead of being rerouted as the whole-message entries do
    [Fact]
    public void CompatiblePairOnlyFormatter_ThrowsAsAtTheBufferLevelEntries()
    {
        var resolver = new MessagePackFormatterResolver(
            MessagePackFormatterFactory.Combine(
                new CompatiblePairOnlyFactory(),
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance));
        var options = new MessagePackSerializerOptions(resolver);

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var writer = new MessagePackWriter();
            writer.Serialize(new RoutedValue { X = 1 }, options);
        });

        var payload = MessagePackSerializer.Serialize(new RoutedValue { X = 1 }, options);
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var reader = new MessagePackReader(payload);
            reader.Deserialize<RoutedValue>(options);
        });
    }
#endif

    static ReadOnlySequence<byte> OneByteSegments(byte[] bytes)
    {
        var first = new Segment(bytes.AsMemory(0, 1), 0);
        var last = first;
        for (var i = 1; i < bytes.Length; i++)
        {
            last = last.Append(bytes.AsMemory(i, 1));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }
}

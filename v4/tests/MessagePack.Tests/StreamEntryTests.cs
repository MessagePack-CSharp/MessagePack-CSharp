namespace MessagePack.Tests;

public class StreamEntryTests
{
    public class StreamPerson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    static readonly MessagePackSerializerOptions contractless = new(new MessagePackFormatterResolver(
        [MessagePackFormatterFactory.Default.WithContractless()]));

    [Fact]
    public void SyncRoundTrip()
    {
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(stream, new StreamPerson { Id = 1, Name = "s" }, contractless);

        stream.Position = 0;
        var back = MessagePackSerializer.Deserialize<StreamPerson>(stream, contractless)!;
        Assert.Equal(1, back.Id);
        Assert.Equal("s", back.Name);
    }

    [Fact]
    public void SyncBytes_MatchByteArrayEntry()
    {
        var value = new[] { 1, 2, 3 };
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(stream, value);
        Assert.Equal(MessagePackSerializer.Serialize(value), stream.ToArray());
    }

    [Fact]
    public void SyncDeserialize_SeekableRewindsToConsumedEnd()
    {
        // two concatenated messages: after the first read the position sits at the
        // boundary, so the second read continues cleanly
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(stream, 111);
        MessagePackSerializer.Serialize(stream, "second");
        stream.Position = 0;

        Assert.Equal(111, MessagePackSerializer.Deserialize<int>(stream));
        Assert.Equal("second", MessagePackSerializer.Deserialize<string>(stream));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void SyncDeserialize_NonSeekable_Works()
    {
        var payload = MessagePackSerializer.Serialize(new StreamPerson { Id = 5, Name = "n" }, contractless);
        using var stream = new NonSeekableStream(payload);
        var back = MessagePackSerializer.Deserialize<StreamPerson>(stream, contractless)!;
        Assert.Equal(5, back.Id);
    }

    [Fact]
    public void SyncDeserialize_SizeCap_Throws()
    {
        var small = new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default]))
        {
            MaxBufferedMessageSize = 8,
        };
        var stream = new MemoryStream(MessagePackSerializer.Serialize(new string('x', 100)));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<string>(stream, small));
    }

    [Fact]
    public async Task AsyncRoundTrip()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(stream, new StreamPerson { Id = 9, Name = "a" }, contractless);

        stream.Position = 0;
        var back = await MessagePackSerializer.DeserializeAsync<StreamPerson>(stream, contractless);
        Assert.Equal(9, back!.Id);
        Assert.Equal("a", back.Name);
    }

    [Fact]
    public async Task Async_LeavesStreamOpen()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(stream, 42);
        stream.Position = 0;
        Assert.Equal(42, await MessagePackSerializer.DeserializeAsync<int>(stream));
        stream.WriteByte(0); // still usable: neither entry closed it
    }

    sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public void NonExposableMemoryStream_FallsBackAndWorks()
    {
        // new MemoryStream(byte[]) hides its buffer (TryGetBuffer false): the pooled
        // fallback must serve it, positioning included
        var payload = MessagePackSerializer.Serialize(123);
        var stream = new MemoryStream(payload);
        Assert.Equal(123, MessagePackSerializer.Deserialize<int>(stream));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ExposableMemoryStream_WithOffsetSegment_ReadsCorrectly()
    {
        // a buffer-backed exposable stream whose segment starts mid-array: the fast
        // path must honor the segment offset
        var payload = MessagePackSerializer.Serialize("offset");
        var backing = new byte[payload.Length + 3];
        payload.CopyTo(backing, 3);
        var stream = new MemoryStream(backing, 3, payload.Length, writable: false, publiclyVisible: true);

        Assert.Equal("offset", MessagePackSerializer.Deserialize<string>(stream));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task AsyncOverExposableMemoryStream_PositionsPastValue()
    {
        // the pipe path discards trailing buffered bytes; the MemoryStream fast path
        // positions exactly, so sequential async reads work
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(stream, 1);
        MessagePackSerializer.Serialize(stream, 2);
        stream.Position = 0;

        Assert.Equal(1, await MessagePackSerializer.DeserializeAsync<int>(stream));
        Assert.Equal(2, await MessagePackSerializer.DeserializeAsync<int>(stream));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task StreamingMessages_OverStream_RoundTrip()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeMessagesAsync(stream, new[] { 1, 2, 3 });

        stream.Position = 0;
        var received = new List<int>();
        await foreach (var value in MessagePackSerializer.DeserializeMessagesAsync<int>(stream))
        {
            received.Add(value);
        }
        Assert.Equal([1, 2, 3], received);
    }

    [Fact]
    public async Task StreamingElements_OverStream_RoundTrip()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeElementsAsync(stream, new[] { "a", "b" }, 2);

        stream.Position = 0;
        var received = new List<string?>();
        await foreach (var value in MessagePackSerializer.DeserializeElementsAsync<string?>(stream))
        {
            received.Add(value);
        }
        Assert.Equal(["a", "b"], received);

        // the wire is one plain msgpack array: readable by the ordinary entry too
        Assert.Equal(["a", "b"], MessagePackSerializer.Deserialize<string[]>(stream.ToArray()));
    }
}

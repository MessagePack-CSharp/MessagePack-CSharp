using System.Buffers;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

public class SkipTests
{
    // [marker, VALUE-TO-SKIP, marker] — Skip must land exactly on the trailing marker
    static byte[] Payload(object? value)
    {
        var seq = new object?[] { 111, value, 222 };
        return Oracle.Serialize(seq, MessagePack.Resolvers.ContractlessStandardResolver.Options);
    }

    static readonly object?[][] Cases =
    [
        [42], [-5], [128], [-33], [70000], [-70000], [3.5], [1.5f], [true], [null],
        ["short"], [new string('あ', 100)], [new byte[] { 1, 2, 3 }], [new byte[300]],
        [new object?[] { 1, "two", new object?[] { 3, 4 }, null }],
        [new Dictionary<object, object> { ["a"] = 1, ["b"] = new object?[] { 1, 2 } }],
        [DateTime.UtcNow], // ext (timestamp)
        [new object?[0]], [new Dictionary<object, object>()],
    ];

    public static IEnumerable<object?[]> SkipCases() => Cases;

    [Theory]
    [MemberData(nameof(SkipCases))]
    public void Skip_LandsExactlyPastOneValue(object? value)
    {
        var bytes = Payload(value);

        // span buffer
        {
            var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer(bytes);
            Assert.Equal(3, buffer.ReadArrayHeader());
            Assert.Equal(111, buffer.ReadInt32());
            buffer.Skip();
            Assert.Equal(222, buffer.ReadInt32());
            Assert.Equal(0, buffer.BytesRemaining);
        }

        // sequence buffer, adversarially chunked
        foreach (var chunkSize in (int[])[1, 3, 16])
        {
            var buffer = new SerializerFoundation.ReadOnlySequenceReadBuffer(Chunk(bytes, chunkSize));
            Assert.Equal(3, buffer.ReadArrayHeader());
            Assert.Equal(111, buffer.ReadInt32());
            buffer.Skip();
            Assert.Equal(222, buffer.ReadInt32());
            Assert.Equal(0, buffer.BytesRemaining);
        }
    }

    [Fact]
    public void Skip_DeeplyNested_NoStackGrowth()
    {
        // 100_000 nested single-element arrays: recursion would overflow, counting must not
        var bytes = new byte[100_000 + 1];
        bytes.AsSpan(0, 100_000).Fill(0x91); // fixarray(1)
        bytes[100_000] = 0x7f;               // innermost value
        var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer(bytes);
        buffer.Skip();
        Assert.Equal(0, buffer.BytesRemaining);
    }

    [Fact]
    public void Skip_TruncatedAndAdversarial_Throws()
    {
        // truncated str payload
        var truncated = new byte[] { 0xa5, (byte)'a', (byte)'b' }; // fixstr(5) but 2 bytes
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer(truncated);
            buffer.Skip();
        });

        // bin32 claiming ~2GB with 3 bytes of data must throw, not skip past the end
        var lying = new byte[] { 0xc6, 0x7f, 0xff, 0xff, 0xff, 1, 2, 3 };
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer(lying);
            buffer.Skip();
        });

        // 0xc1 is never a valid code
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer([0xc1]);
            buffer.Skip();
        });

        // empty buffer
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var buffer = new SerializerFoundation.ReadOnlySpanReadBuffer(ReadOnlySpan<byte>.Empty);
            buffer.Skip();
        });
    }

    sealed class Chunked : ReadOnlySequenceSegment<byte>
    {
        public Chunked(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Chunked Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Chunked(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<byte> Chunk(byte[] bytes, int chunkSize)
    {
        var first = new Chunked(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var last = first;
        for (int i = chunkSize; i < bytes.Length; i += chunkSize)
        {
            last = last.Append(bytes.AsMemory(i, Math.Min(chunkSize, bytes.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}

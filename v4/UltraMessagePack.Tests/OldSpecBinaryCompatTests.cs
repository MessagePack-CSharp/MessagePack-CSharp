using SerializerFoundation;
using System.Buffers;
using System.Numerics;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;
using OracleOptions = MessagePack.MessagePackSerializerOptions;

namespace UltraMessagePack.Tests;

// v3 parity: "Reading always supports both new and old spec" (v3 MessagePackSerializerOptions.OldSpec).
// Old-spec (pre-2013) msgpack has no bin family — binary rides raw (= today's str) headers, so
// binary reads must accept str-coded tokens. Writer-side OldSpec is intentionally NOT carried
// into v4; this reader-side acceptance is what keeps v3-era old-spec data deserializable.
// The fallback lives in ReadBinHeaderSlow (ReadBufferExtensions.cs); ReadBinary routes its
// whole token through the header reader, so accepting str there covers the payload too.
public class OldSpecBinaryCompatTests
{
    static readonly OracleOptions OldSpec = OracleOptions.Standard.WithOldSpec(true);

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

    static ReadOnlySequence<byte> Split(byte[] data, int chunkSize)
    {
        if (data.Length <= chunkSize)
        {
            return new ReadOnlySequence<byte>(data);
        }
        var first = new Segment(data.AsMemory(0, chunkSize));
        var last = first;
        for (int i = chunkSize; i < data.Length; i += chunkSize)
        {
            last = last.Append(data.AsMemory(i, Math.Min(chunkSize, data.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    static byte[] RandomBytes(int length)
    {
        var value = new byte[length];
        new Random(42).NextBytes(value);
        return value;
    }

    [Theory]
    [InlineData(0)]     // fixraw 0xa0
    [InlineData(5)]     // fixraw
    [InlineData(31)]    // fixraw ceiling
    [InlineData(32)]    // raw16 (old spec has no str8)
    [InlineData(300)]   // raw16
    [InlineData(70000)] // raw32
    public void ByteArray_WrittenByV3OldSpec_Deserializes(int length)
    {
        var value = RandomBytes(length);
        var oldSpecBytes = Oracle.Serialize(value, OldSpec);

        // prove the oracle really emitted a str-coded token, not bin (v3's WriteBinHeader
        // detours to WriteStringHeader, which still picks str8 for 32..255 — accept it too)
        byte code = oldSpecBytes[0];
        Assert.True((code >= 0xa0 && code <= 0xbf) || code == 0xd9 || code == 0xda || code == 0xdb);

        Assert.Equal(value, MessagePackSerializer.Deserialize<byte[]>(oldSpecBytes));
    }

    [Fact]
    public void Str8CodedBinary_Deserializes()
    {
        // old spec itself never emits str8, but v3's reader accepts the full str family — match it
        var payload = RandomBytes(100);
        var bytes = new byte[2 + payload.Length];
        bytes[0] = 0xd9; // str8
        bytes[1] = (byte)payload.Length;
        payload.CopyTo(bytes, 2);
        Assert.Equal(payload, MessagePackSerializer.Deserialize<byte[]>(bytes));
    }

    [Fact]
    public void BinaryWrapperTargets_AcceptStrCodedPayload()
    {
        // every byte[]-family target funnels through ReadBinary and gets the fallback
        var value = RandomBytes(300);
        var oldSpecBytes = Oracle.Serialize(value, OldSpec); // raw16-coded

        Assert.Equal(value, MessagePackSerializer.Deserialize<ArraySegment<byte>>(oldSpecBytes).ToArray());
        Assert.Equal(value, MessagePackSerializer.Deserialize<Memory<byte>>(oldSpecBytes).ToArray());
        Assert.Equal(value, MessagePackSerializer.Deserialize<ReadOnlyMemory<byte>>(oldSpecBytes).ToArray());
        Assert.Equal(value, MessagePackSerializer.Deserialize<ReadOnlySequence<byte>>(oldSpecBytes).ToArray());
    }

    [Fact]
    public void BigInteger_WrittenByV3OldSpec_Deserializes()
    {
        // BigInteger deserializes via ReadBinHeader (str fallback in the header path)
        BigInteger[] values = [BigInteger.Zero, BigInteger.MinusOne, BigInteger.Parse("123456789012345678901234567890")];
        foreach (var value in values)
        {
            Assert.Equal(value, MessagePackSerializer.Deserialize<BigInteger>(Oracle.Serialize(value, OldSpec)));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Sequence_TinySegments_StrCodedBinary(int chunkSize)
    {
        // multi-segment: the str fallback must survive header/payload stitching
        var value = RandomBytes(300);
        var oldSpecBytes = Oracle.Serialize(value, OldSpec); // raw16-coded
        var buffer = new ReadOnlySequenceReadBuffer(Split(oldSpecBytes, chunkSize));
        try
        {
            Assert.Equal(value, buffer.ReadBinary());
            Assert.Equal(0, buffer.BytesRemaining);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void ReadBinHeader_AcceptsStrHeader()
    {
        var buffer = new ReadOnlySpanReadBuffer([0xa3, 1, 2, 3]); // fixstr(3)
        Assert.Equal(3, buffer.ReadBinHeader());
        Assert.True(buffer.TryGetSpan(3, out var payload));
        Assert.Equal((byte[])[1, 2, 3], payload.Slice(0, 3).ToArray());
    }

    [Fact]
    public void NonBinNonStrCode_StillThrows()
    {
        // the fallback must not turn binary reads into an accept-anything path
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySpanReadBuffer([0xc3]); // true
            b.ReadBinary();
        });
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySpanReadBuffer([0xc3]);
            b.ReadBinHeader();
        });
    }

    [Fact]
    public void TruncatedStrCodedBinary_StillThrows()
    {
        // str16 header cut mid-token: the fallback must report truncation, not loop or succeed
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySpanReadBuffer([0xda, 0x01]);
            b.ReadBinary();
        });

        // full header claiming more payload than remains: allocation-bomb guard still applies
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySpanReadBuffer([0xda, 0x03, 0xe8, 1, 2, 3]); // str16 claiming 1000
            b.ReadBinHeader();
        });
    }
}

using SerializerFoundation;
using System.Buffers;
using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// ReadBufferExtensions verified over both buffer shapes: ReadOnlySpanReadBuffer (whole
// message always in the current span -> fast path only) and ReadOnlySequenceReadBuffer
// split into tiny segments (every multi-byte message straddles a boundary -> exercises
// the stitched-window slow path and temp-buffer Advance). Both must decode the identical
// value stream. Failure semantics: out-of-range and invalid data throw MessagePackSerializationException.
public class ReadBufferExtensionsTests
{
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

    static readonly DateTime TestTimestamp = new DateTime(2026, 7, 17, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);

    static byte[] BuildStream()
    {
        var buf = new byte[4096];
        int pos = 0;
        void W(bool ok, int written) { Assert.True(ok); pos += written; }

        W(MessagePackPrimitives.TryWriteNil(buf.AsSpan(pos), out int w), w);
        W(MessagePackPrimitives.TryWriteBoolean(buf.AsSpan(pos), true, out w), w);
        W(MessagePackPrimitives.TryWriteBoolean(buf.AsSpan(pos), false, out w), w);
        foreach (int v in (int[])[0, -32, -33, 127, 128, 65536, int.MinValue, int.MaxValue])
        {
            W(MessagePackPrimitives.TryWriteInt32(buf.AsSpan(pos), v, out w), w);
        }
        W(MessagePackPrimitives.TryWriteInt64(buf.AsSpan(pos), long.MinValue, out w), w);
        W(MessagePackPrimitives.TryWriteInt64(buf.AsSpan(pos), long.MaxValue, out w), w);
        W(MessagePackPrimitives.TryWriteUInt64(buf.AsSpan(pos), ulong.MaxValue, out w), w);
        W(MessagePackPrimitives.TryWriteUInt32(buf.AsSpan(pos), uint.MaxValue, out w), w);
        W(MessagePackPrimitives.TryWriteByte(buf.AsSpan(pos), 200, out w), w);
        W(MessagePackPrimitives.TryWriteSByte(buf.AsSpan(pos), -100, out w), w);
        W(MessagePackPrimitives.TryWriteInt16(buf.AsSpan(pos), -30000, out w), w);
        W(MessagePackPrimitives.TryWriteUInt16(buf.AsSpan(pos), 60000, out w), w);
        W(MessagePackPrimitives.TryWriteChar(buf.AsSpan(pos), 'あ', out w), w);
        W(MessagePackPrimitives.TryWriteSingle(buf.AsSpan(pos), 1.5f, out w), w);
        W(MessagePackPrimitives.TryWriteDouble(buf.AsSpan(pos), Math.PI, out w), w);
        W(MessagePackPrimitives.TryWriteArrayHeader(buf.AsSpan(pos), 20, out w), w);
        W(MessagePackPrimitives.TryWriteMapHeader(buf.AsSpan(pos), 70000, out w), w);
        W(MessagePackPrimitives.TryWriteString(buf.AsSpan(pos), (string?)null, out w), w);
        W(MessagePackPrimitives.TryWriteString(buf.AsSpan(pos), "hello", out w), w);
        W(MessagePackPrimitives.TryWriteString(buf.AsSpan(pos), "こんにちは世界🌍", out w), w);
        W(MessagePackPrimitives.TryWriteString(buf.AsSpan(pos), new string('x', 300), out w), w);
        W(MessagePackPrimitives.TryWriteBinary(buf.AsSpan(pos), [1, 2, 3, 4, 5], out w), w);
        W(MessagePackPrimitives.TryWriteExtHeader(buf.AsSpan(pos), 7, 16, out w), w);
        pos += 16; // the ext payload itself (zeros)
        W(MessagePackPrimitives.TryWriteTimestamp(buf.AsSpan(pos), TestTimestamp, out w), w);

        return buf[..pos];
    }

    static void ReadAll<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        Assert.True(buffer.TryReadNil());
        Assert.True(buffer.ReadBoolean());
        Assert.False(buffer.TryReadNil()); // false is not nil, consumes nothing
        Assert.False(buffer.ReadBoolean());
        foreach (int v in (int[])[0, -32, -33, 127, 128, 65536, int.MinValue, int.MaxValue])
        {
            Assert.Equal(v, buffer.ReadInt32());
        }
        Assert.Equal(long.MinValue, buffer.ReadInt64());
        Assert.Equal(long.MaxValue, buffer.ReadInt64());
        Assert.Equal(ulong.MaxValue, buffer.ReadUInt64());
        Assert.Equal(uint.MaxValue, buffer.ReadUInt32());
        Assert.Equal((byte)200, buffer.ReadByte());
        Assert.Equal((sbyte)-100, buffer.ReadSByte());
        Assert.Equal((short)-30000, buffer.ReadInt16());
        Assert.Equal((ushort)60000, buffer.ReadUInt16());
        Assert.Equal('あ', buffer.ReadChar());
        Assert.Equal(1.5f, buffer.ReadSingle());
        Assert.Equal(Math.PI, buffer.ReadDouble());
        Assert.Equal(20, buffer.ReadArrayHeader());
        Assert.Equal(70000, buffer.ReadMapHeader());
        Assert.Null(buffer.ReadString());
        Assert.Equal("hello", buffer.ReadString());
        Assert.Equal("こんにちは世界🌍", buffer.ReadString());
        Assert.Equal(new string('x', 300), buffer.ReadString());
        Assert.Equal((byte[])[1, 2, 3, 4, 5], buffer.ReadBinary());
        var (typeCode, dataLength) = buffer.ReadExtHeader();
        Assert.Equal((sbyte)7, typeCode);
        Assert.Equal(16, dataLength);
        buffer.Advance(16); // skip the ext payload
        Assert.Equal(TestTimestamp, buffer.ReadTimestamp());
        Assert.Equal(0, buffer.BytesRemaining);
    }

    [Fact]
    public void Span_Roundtrip()
    {
        var buffer = new ReadOnlySpanReadBuffer(BuildStream());
        ReadAll(ref buffer);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Sequence_TinySegments_Roundtrip(int chunkSize)
    {
        // no scratch: every stitch goes through the retained rented temp
        var buffer = new ReadOnlySequenceReadBuffer(Split(BuildStream(), chunkSize));
        try
        {
            ReadAll(ref buffer);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Sequence_TinySegments_Roundtrip_WithScratch(int chunkSize)
    {
        // 64B scratch: numeric/header stitches use it, the 300-char string still
        // exceeds it and exercises the retained-temp tier in the same run
        Span<byte> scratch = stackalloc byte[64];
        var buffer = new ReadOnlySequenceReadBuffer(Split(BuildStream(), chunkSize), scratch);
        try
        {
            ReadAll(ref buffer);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void EndOfBuffer_ThrowsMessagePackException_SameAsTruncation()
    {
        // data cut exactly at a token boundary must surface as the SAME domain exception
        // as a mid-token cut: GetSpan() returns an empty window at exhaustion (instead of
        // a foundation throw) and the primitives report InsufficientBuffer
        byte[] one = new byte[8];
        Assert.True(MessagePackPrimitives.TryWriteInt32(one, 42, out int written));

        var span = new ReadOnlySpanReadBuffer(one.AsSpan(0, written));
        Assert.Equal(42, span.ReadInt32());
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySpanReadBuffer(one.AsSpan(0, written));
            b.ReadInt32();
            b.ReadInt32(); // exhausted at a boundary
        });

        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var b = new ReadOnlySequenceReadBuffer(Split(one[..written], 1));
            try
            {
                b.ReadInt32();
                b.ReadInt32(); // exhausted at a boundary (multi-segment)
            }
            finally
            {
                b.Dispose();
            }
        });
    }

    [Fact]
    public void OutOfRangeAndInvalid_Throw()
    {
        // uint64 above long.MaxValue: the sign-alias trap
        byte[] trap = new byte[9];
        trap[0] = MessagePackCode.UInt64;
        trap.AsSpan(1).Fill(0xff);
        AssertThrows(trap, (ref ReadOnlySpanReadBuffer b) => b.ReadInt64());
        AssertThrows(trap, (ref ReadOnlySpanReadBuffer b) => b.ReadInt32());
        var ok = new ReadOnlySpanReadBuffer(trap);
        Assert.Equal(ulong.MaxValue, ok.ReadUInt64());

        // negative into unsigned targets
        byte[] neg = new byte[3];
        Assert.True(MessagePackPrimitives.TryWriteInt32(neg, -1, out _));
        AssertThrows(neg, (ref ReadOnlySpanReadBuffer b) => b.ReadUInt64());
        AssertThrows(neg, (ref ReadOnlySpanReadBuffer b) => b.ReadByte());

        // narrow targets reject wide values
        byte[] wide = new byte[3];
        Assert.True(MessagePackPrimitives.TryWriteInt32(wide, 300, out _));
        AssertThrows(wide, (ref ReadOnlySpanReadBuffer b) => b.ReadByte());
        AssertThrows(wide, (ref ReadOnlySpanReadBuffer b) => b.ReadSByte());

        // wrong-type code
        byte[] str = new byte[6];
        Assert.True(MessagePackPrimitives.TryWriteString(str, "abc", out _));
        AssertThrows(str, (ref ReadOnlySpanReadBuffer b) => b.ReadInt32());
        AssertThrows(str, (ref ReadOnlySpanReadBuffer b) => b.ReadBoolean());
        AssertThrows(str, (ref ReadOnlySpanReadBuffer b) => b.ReadArrayHeader());

        // truncated multi-byte message
        byte[] truncated = [MessagePackCode.UInt32, 0x01];
        AssertThrows(truncated, (ref ReadOnlySpanReadBuffer b) => b.ReadInt32());

        // str header larger than the remaining payload
        byte[] shortStr = [0xa5, (byte)'a', (byte)'b']; // fixstr(5) with 2 payload bytes
        AssertThrows(shortStr, (ref ReadOnlySpanReadBuffer b) => b.ReadString());
    }

    delegate void SpanBufferAction(ref ReadOnlySpanReadBuffer buffer);

    static void AssertThrows(byte[] source, SpanBufferAction action)
    {
        try
        {
            var buffer = new ReadOnlySpanReadBuffer(source);
            action(ref buffer);
        }
        catch (MessagePackSerializationException)
        {
            return;
        }
        Assert.Fail("expected MessagePackSerializationException");
    }
}

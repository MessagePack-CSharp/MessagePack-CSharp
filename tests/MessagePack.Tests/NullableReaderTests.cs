using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using MessagePack;
using SerializerFoundation;
using Xunit;

namespace MessagePack.Tests;

// ReadNullableX is `TryReadNil() ? null : ReadX()` by contract: the same value, the same bytes consumed and the same
// exception for everything else. Int32 and Boolean decode through their own one-window path (ReadInt32OrNil /
// ReadBooleanOrNil), so the contract is checked rather than assumed: every first byte 0x00..0xff in front of a set of
// payload patterns, cut at every length, over the contiguous buffer and over a sequence split into 1, 2 and 3 byte
// segments (where a multi-byte token straddles and the reader has to fall back to the pair's stitched retry).
public class NullableReaderTests
{
    abstract class Kind
    {
        public abstract string Name { get; }

        /// <summary>The reference: the expression the generator emitted before the readers existed.</summary>
        public abstract string Pair<TBuffer>(ref TBuffer buffer)
            where TBuffer : struct, IReadBuffer, allows ref struct;

        public abstract string Reader<TBuffer>(ref TBuffer buffer)
            where TBuffer : struct, IReadBuffer, allows ref struct;

        protected static string Text<T>(T? value)
            where T : struct, IFormattable
            => value is { } v ? v.ToString(null, CultureInfo.InvariantCulture) : "null";

        public override string ToString() => Name;
    }

    sealed class Int32Kind : Kind
    {
        public override string Name => "Int32";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(int?) : buffer.ReadInt32());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableInt32());
    }

    sealed class UInt32Kind : Kind
    {
        public override string Name => "UInt32";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(uint?) : buffer.ReadUInt32());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableUInt32());
    }

    sealed class Int64Kind : Kind
    {
        public override string Name => "Int64";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(long?) : buffer.ReadInt64());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableInt64());
    }

    sealed class UInt64Kind : Kind
    {
        public override string Name => "UInt64";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(ulong?) : buffer.ReadUInt64());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableUInt64());
    }

    sealed class Int16Kind : Kind
    {
        public override string Name => "Int16";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(short?) : buffer.ReadInt16());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableInt16());
    }

    sealed class UInt16Kind : Kind
    {
        public override string Name => "UInt16";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(ushort?) : buffer.ReadUInt16());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableUInt16());
    }

    sealed class ByteKind : Kind
    {
        public override string Name => "Byte";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(byte?) : buffer.ReadByte());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableByte());
    }

    sealed class SByteKind : Kind
    {
        public override string Name => "SByte";
        public override string Pair<TBuffer>(ref TBuffer buffer) => Text(buffer.TryReadNil() ? default(sbyte?) : buffer.ReadSByte());
        public override string Reader<TBuffer>(ref TBuffer buffer) => Text(buffer.ReadNullableSByte());
    }

    sealed class BooleanKind : Kind
    {
        public override string Name => "Boolean";
        public override string Pair<TBuffer>(ref TBuffer buffer) => (buffer.TryReadNil() ? default(bool?) : buffer.ReadBoolean()) is { } v ? v ? "true" : "false" : "null";
        public override string Reader<TBuffer>(ref TBuffer buffer) => buffer.ReadNullableBoolean() is { } v ? v ? "true" : "false" : "null";
    }

    sealed class CharKind : Kind
    {
        public override string Name => "Char";
        public override string Pair<TBuffer>(ref TBuffer buffer) => (buffer.TryReadNil() ? default(char?) : buffer.ReadChar()) is { } v ? ((int)v).ToString(CultureInfo.InvariantCulture) : "null";
        public override string Reader<TBuffer>(ref TBuffer buffer) => buffer.ReadNullableChar() is { } v ? ((int)v).ToString(CultureInfo.InvariantCulture) : "null";
    }

    // floats compare by bit pattern: a NaN payload or a signed zero must come through unchanged
    sealed class SingleKind : Kind
    {
        public override string Name => "Single";
        public override string Pair<TBuffer>(ref TBuffer buffer) => (buffer.TryReadNil() ? default(float?) : buffer.ReadSingle()) is { } v ? BitConverter.SingleToUInt32Bits(v).ToString("x8", CultureInfo.InvariantCulture) : "null";
        public override string Reader<TBuffer>(ref TBuffer buffer) => buffer.ReadNullableSingle() is { } v ? BitConverter.SingleToUInt32Bits(v).ToString("x8", CultureInfo.InvariantCulture) : "null";
    }

    sealed class DoubleKind : Kind
    {
        public override string Name => "Double";
        public override string Pair<TBuffer>(ref TBuffer buffer) => (buffer.TryReadNil() ? default(double?) : buffer.ReadDouble()) is { } v ? BitConverter.DoubleToUInt64Bits(v).ToString("x16", CultureInfo.InvariantCulture) : "null";
        public override string Reader<TBuffer>(ref TBuffer buffer) => buffer.ReadNullableDouble() is { } v ? BitConverter.DoubleToUInt64Bits(v).ToString("x16", CultureInfo.InvariantCulture) : "null";
    }

    static readonly Kind[] Kinds =
    [
        new Int32Kind(), new UInt32Kind(), new Int64Kind(), new UInt64Kind(), new Int16Kind(), new UInt16Kind(),
        new ByteKind(), new SByteKind(), new BooleanKind(), new CharKind(), new SingleKind(), new DoubleKind(),
    ];

    public static TheoryData<string> KindNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var kind in Kinds)
            {
                data.Add(kind.Name);
            }
            return data;
        }
    }

    // big-endian payloads behind the code byte: the edges of every integer width, both signs, and bytes that are
    // themselves codes (nil, false / true) so a reader that looks one byte too far shows up
    static readonly ulong[] Patterns =
    [
        0x0000000000000000, 0x0000000000000001, 0x000000000000007f, 0x0000000000000080, 0x00000000000000ff,
        0x7f00000000000000, 0x8000000000000000, 0xff00000000000000, 0x7fff000000000000, 0xffff000000000000,
        0x7fffffff00000000, 0xffffffff00000000, 0x000000007fffffff, 0x0000000080000000, 0x00000000ffffffff,
        0xffffffff80000000, 0xffffffff7fffffff, 0x7fffffffffffffff, 0xffffffffffffffff, 0x0102030405060708,
        0xc0c0c0c0c0c0c0c0, 0xc2c3c2c3c2c3c2c3, 0x3ff8000000000000, 0x7ff8000000000001,
    ];

    // code + 8 payload bytes + one trailing fixint the read has to leave alone
    static byte[] Payload(int code, ulong pattern)
    {
        var payload = new byte[10];
        payload[0] = (byte)code;
        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(1), pattern);
        payload[9] = 0x2a;
        return payload;
    }

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

    static (string Value, long Remaining, string? Error) OnSpan(Kind kind, byte[] data, bool reader)
    {
        var buffer = new ReadOnlySpanReadBuffer(data);
        try
        {
            var value = reader ? kind.Reader(ref buffer) : kind.Pair(ref buffer);
            return (value, buffer.BytesRemaining, null);
        }
        catch (MessagePackSerializationException ex)
        {
            return ("", buffer.BytesRemaining, ex.Message);
        }
    }

    static (string Value, long Remaining, string? Error) OnSequence(Kind kind, byte[] data, int chunkSize, bool reader)
    {
        var buffer = new ReadOnlySequenceReadBuffer(Split(data, chunkSize));
        try
        {
            var value = reader ? kind.Reader(ref buffer) : kind.Pair(ref buffer);
            return (value, buffer.BytesRemaining, null);
        }
        catch (MessagePackSerializationException ex)
        {
            return ("", buffer.BytesRemaining, ex.Message);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(KindNames))]
    public void Span_MatchesThePair(string kindName)
    {
        var kind = Kinds.Single(k => k.Name == kindName);
        var accepted = 0;
        for (int code = 0; code <= 0xff; code++)
        {
            foreach (var pattern in Patterns)
            {
                var payload = Payload(code, pattern);
                for (int length = payload.Length; length >= 0; length--)
                {
                    var cut = payload.AsSpan(0, length).ToArray();
                    var expected = OnSpan(kind, cut, reader: false);
                    var actual = OnSpan(kind, cut, reader: true);
                    if (expected != actual)
                    {
                        Assert.Fail($"ReadNullable{kind} differs from the pair on {Convert.ToHexString(cut)}: {actual} (the pair: {expected})");
                    }
                    if (expected.Error is null)
                    {
                        accepted++;
                    }
                }
            }
        }
        Assert.True(accepted > 0, "the payload set never produced a readable value");
    }

    [Theory]
    [MemberData(nameof(KindNames))]
    public void Sequence_MatchesThePair(string kindName)
    {
        var kind = Kinds.Single(k => k.Name == kindName);
        for (int code = 0; code <= 0xff; code++)
        {
            // every third pattern: the sequence path is the same decode behind a different window, the full set is the span test's
            for (int p = code % 3; p < Patterns.Length; p += 3)
            {
                var payload = Payload(code, Patterns[p]);
                for (int length = payload.Length; length >= 0; length--)
                {
                    var cut = payload.AsSpan(0, length).ToArray();
                    for (int chunkSize = 1; chunkSize <= 3; chunkSize++)
                    {
                        var expected = OnSequence(kind, cut, chunkSize, reader: false);
                        var actual = OnSequence(kind, cut, chunkSize, reader: true);
                        if (expected != actual)
                        {
                            Assert.Fail($"ReadNullable{kind} differs from the pair on {Convert.ToHexString(cut)} in {chunkSize}-byte segments: {actual} (the pair: {expected})");
                        }
                    }
                }
            }
        }
    }

    // the values themselves, written by the library's own writers: the equivalence tests would pass for a pair and a
    // reader that were wrong in the same way
    [Fact]
    public void ReadsWhatTheWritersWrote()
    {
        var writer = new ArrayBufferWriter<byte>();
        var write = new BufferWriterWriteBuffer(writer);
        try
        {
            foreach (int v in (int[])[0, 1, -1, 127, 128, -32, -33, 255, 256, -129, 65535, 65536, -32769, int.MaxValue, int.MinValue])
            {
                write.WriteInt32(v);
            }
            write.WriteNil();
            write.WriteBoolean(true);
            write.WriteBoolean(false);
            write.WriteNil();
            write.WriteUInt32(uint.MaxValue);
            write.WriteNil();
            write.WriteInt64(long.MinValue);
            write.WriteNil();
            write.WriteUInt64(ulong.MaxValue);
            write.WriteNil();
            write.WriteInt16(short.MinValue);
            write.WriteNil();
            write.WriteUInt16(ushort.MaxValue);
            write.WriteNil();
            write.WriteByte(200);
            write.WriteNil();
            write.WriteSByte(-100);
            write.WriteNil();
            write.WriteChar('あ');
            write.WriteNil();
            write.WriteSingle(1.5f);
            write.WriteNil();
            write.WriteDouble(Math.PI);
            write.WriteNil();
            write.Flush();
        }
        finally
        {
            write.Dispose();
        }

        var buffer = new ReadOnlySpanReadBuffer(writer.WrittenSpan);
        foreach (int v in (int[])[0, 1, -1, 127, 128, -32, -33, 255, 256, -129, 65535, 65536, -32769, int.MaxValue, int.MinValue])
        {
            Assert.Equal(v, buffer.ReadNullableInt32());
        }
        Assert.Null(buffer.ReadNullableInt32());
        Assert.Equal(true, buffer.ReadNullableBoolean());
        Assert.Equal(false, buffer.ReadNullableBoolean());
        Assert.Null(buffer.ReadNullableBoolean());
        Assert.Equal(uint.MaxValue, buffer.ReadNullableUInt32());
        Assert.Null(buffer.ReadNullableUInt32());
        Assert.Equal(long.MinValue, buffer.ReadNullableInt64());
        Assert.Null(buffer.ReadNullableInt64());
        Assert.Equal(ulong.MaxValue, buffer.ReadNullableUInt64());
        Assert.Null(buffer.ReadNullableUInt64());
        Assert.Equal(short.MinValue, buffer.ReadNullableInt16());
        Assert.Null(buffer.ReadNullableInt16());
        Assert.Equal(ushort.MaxValue, buffer.ReadNullableUInt16());
        Assert.Null(buffer.ReadNullableUInt16());
        Assert.Equal((byte)200, buffer.ReadNullableByte());
        Assert.Null(buffer.ReadNullableByte());
        Assert.Equal((sbyte)-100, buffer.ReadNullableSByte());
        Assert.Null(buffer.ReadNullableSByte());
        Assert.Equal('あ', buffer.ReadNullableChar());
        Assert.Null(buffer.ReadNullableChar());
        Assert.Equal(1.5f, buffer.ReadNullableSingle());
        Assert.Null(buffer.ReadNullableSingle());
        Assert.Equal(Math.PI, buffer.ReadNullableDouble());
        Assert.Null(buffer.ReadNullableDouble());
        Assert.Equal(0, buffer.BytesRemaining);
    }
}

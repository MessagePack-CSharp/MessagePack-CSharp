using SerializerFoundation;
using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace MessagePack.Tests;

// the buffer-level ext header writer (the public custom-extension write path, paired with
// ReadExtHeader on the read side): byte-identical to the primitives layer, which is
// oracle-verified in WritePrimitiveTests.ExtHeader_MatchOracle, and round-trips together
// with a caller-written payload across every format class
public class WriteExtHeaderTests
{
    [Theory]
    [InlineData(0)]     // ext8 (zero stays ext8, never fixext)
    [InlineData(1)]     // fixext1
    [InlineData(2)]     // fixext2
    [InlineData(3)]     // ext8
    [InlineData(4)]     // fixext4
    [InlineData(8)]     // fixext8
    [InlineData(16)]    // fixext16
    [InlineData(255)]   // ext8 upper bound
    [InlineData(256)]   // ext16
    [InlineData(65535)] // ext16 upper bound
    [InlineData(65536)] // ext32
    public void RoundtripsWithPayload(int dataLength)
    {
        var payload = new byte[dataLength];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)i;
        }

        // initial capacity below every header reservation, so large cases cross segments
        var inner = new ArrayBufferWriter<byte>(16);
        var b = new BufferWriterWriteBuffer(inner);
        b.WriteExtHeader(42, dataLength);
        payload.CopyTo(b.GetSpan(dataLength));
        b.Advance(dataLength);
        b.Dispose();

        var expected = new byte[MessagePackPrimitives.MaxExtHeaderLength + dataLength];
        int headerSize = MessagePackPrimitives.UnsafeWriteExtHeader(ref MemoryMarshal.GetArrayDataReference(expected), 42, dataLength);
        payload.CopyTo(expected.AsSpan(headerSize));
        Assert.Equal(expected.AsSpan(0, headerSize + dataLength).ToArray(), inner.WrittenSpan.ToArray());

        var reader = new ReadOnlySpanReadBuffer(inner.WrittenSpan);
        var (typeCode, readLength) = reader.ReadExtHeader();
        Assert.Equal((sbyte)42, typeCode);
        Assert.Equal(dataLength, readLength);
        if (readLength > 0)
        {
            var data = new byte[readLength];
            reader.CopyTo(data);
            reader.Advance(readLength);
            Assert.Equal(payload, data);
        }
        Assert.Equal(0, reader.BytesRemaining);
    }
}

using System.Buffers;
using System.IO.Pipelines;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// Entry-point behavior on failure, from the preview1 review's minor findings (F1, F2, F3, F45/G218, F77, F78).
public class EntryPointRobustnessTests
{
    // ---- F1: a failing formatter must not commit its half-written span to the IBufferWriter ----

    [Fact]
    public void IBufferWriter_FailedSerialize_CommitsNothing()
    {
        var writer = new ArrayBufferWriter<byte>();
        var shallow = V4Options.Default with { MaxDepth = 1 };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(writer, new DepthNode { Child = new DepthNode() }, shallow));
        Assert.Equal(0, writer.WrittenCount); // the root's array header was staged, never committed

        // the same writer takes the next message cleanly
        V4.Serialize(writer, new DepthNode(), shallow);
        Assert.Equal(new byte[] { 0x91, 0xC0 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public async Task PipeWriter_FailedSerialize_SendsNothing()
    {
        var pipe = new Pipe();
        var shallow = V4Options.Default with { MaxDepth = 1 };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize((IBufferWriter<byte>)pipe.Writer, new DepthNode { Child = new DepthNode() }, shallow));
        await pipe.Writer.CompleteAsync();

        var read = await pipe.Reader.ReadAsync();
        Assert.True(read.Buffer.IsEmpty);
        Assert.True(read.IsCompleted);
    }

    // ---- F2: a failed PipeReader deserialize hands the read back, so the reader stays usable ----

    [Fact]
    public async Task PipeReader_SizeExceeded_HandsTheReadBack()
    {
        var pipe = new Pipe();
        var payload = V4.Serialize(new string('x', 100));
        await pipe.Writer.WriteAsync(payload);
        await pipe.Writer.CompleteAsync();

        var small = V4Options.Default with { MaxBufferedMessageSize = 16 };
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<string>(pipe.Reader, small));

        // used to be "Reading is already in progress": the bytes are still there, examined, unconsumed
        var read = await pipe.Reader.ReadAsync();
        Assert.Equal(payload.Length, read.Buffer.Length);
        pipe.Reader.AdvanceTo(read.Buffer.End);
    }

    [Fact]
    public async Task PipeReader_MalformedBoundaryScan_HandsTheReadBack()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0xC1 }); // the never-used code: the boundary scanner refuses it
        await pipe.Writer.FlushAsync();                      // not completed, so the scanning path (not the fast path) runs

        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<int>(pipe.Reader));

        await pipe.Writer.CompleteAsync();
        var read = await pipe.Reader.ReadAsync();
        Assert.Equal(1, read.Buffer.Length);
        pipe.Reader.AdvanceTo(read.Buffer.End);
    }

    // ---- F3: a non-seekable stream is read at most one byte past the cap ----

    sealed class CountingNonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public long BytesRead { get; private set; }

        public override bool CanSeek => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = base.Read(buffer);
            BytesRead += read;
            return read;
        }
    }

    [Fact]
    public void NonSeekableStream_OverTheCap_StopsReadingOneBytePastIt()
    {
        var payload = V4.Serialize(new string('x', 5000));
        var stream = new CountingNonSeekableStream(payload);
        var capped = V4Options.Default with { MaxBufferedMessageSize = 1000 }; // not a power of two: pool arrays are 1024, then 2048...

        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(stream, capped));
        Assert.Equal(1001, stream.BytesRead); // the cap plus the one byte that proves the overflow

        // exactly the cap is legal
        var exact = V4.Serialize(new string('y', 997)); // 3-byte str16 header + 997 = 1000
        Assert.Equal(1000, exact.Length);
        Assert.Equal(new string('y', 997), V4.Deserialize<string>(new CountingNonSeekableStream(exact), capped));
    }

    // ---- G218 / F77: a tiny Zstandard frame declaring a huge content size is refused before anything is rented ----

    // magic, FHD (8-byte content size, single segment, no dictionary), content size, one last RLE block of one byte
    static byte[] ForgedZstandardFrame(long declaredContentSize)
    {
        var frame = new List<byte> { 0x28, 0xB5, 0x2F, 0xFD, 0xE0 };
        frame.AddRange(BitConverter.GetBytes(declaredContentSize));
        var blockHeader = 3 | (100 << 3); // Last_Block, Block_Type = RLE, Block_Size = 100
        frame.Add((byte)blockHeader);
        frame.Add((byte)(blockHeader >> 8));
        frame.Add((byte)(blockHeader >> 16));
        frame.Add(0x00);
        return frame.ToArray();
    }

    [Fact]
    public void ZstandardFrame_ImplausibleDeclaredSize_IsRefusedBeforeRenting()
    {
        var processor = new ZstandardFrameProcessor();
        var ex = Assert.Throws<MessagePackSerializationException>(() => processor.TryDecode(ForgedZstandardFrame(60 * 1024 * 1024), out _));
        Assert.Contains("cannot produce", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ZstandardFrame_DeclaredSizePastInt32_WithALargeCap_IsTheDataError()
    {
        // a cap above int.MaxValue let the declared size wrap negative in the int cast and leak ArgumentOutOfRangeException from the pool
        var processor = new ZstandardFrameProcessor(3, 3L << 30);
        Assert.Throws<MessagePackSerializationException>(() => processor.TryDecode(ForgedZstandardFrame((1L << 31) + 16), out _));
    }

    [Fact]
    public void HighlyCompressibleFrames_StillRoundTrip()
    {
        // the plausibility bounds must sit above what the real encoders produce for the most compressible input
        var zeros = new byte[8 * 1024 * 1024];
        var zstd = V4Options.Default.WithZstandardFrame();
        Assert.Equal(zeros.Length, V4.Deserialize<byte[]>(V4.Serialize(zeros, zstd), zstd)!.Length);
        var lz4 = V4Options.Default.WithLz4Frame();
        Assert.Equal(zeros.Length, V4.Deserialize<byte[]>(V4.Serialize(zeros, lz4), lz4)!.Length);
    }

    // ---- F78: an out-of-range compression level fails where it is configured, not on the first Serialize ----

    [Fact]
    public void ZstandardFrame_InvalidCompressionLevel_FailsAtConstruction()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ZstandardFrameProcessor(99));
        Assert.ThrowsAny<ArgumentException>(() => V4Options.Default.WithZstandardFrame(99));
    }
}

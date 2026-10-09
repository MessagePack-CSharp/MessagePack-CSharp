#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
#else
#endif

namespace MessagePack;

public static class ZstandardMessagePackOptionsExtensions
{
    /// <summary>Options writing every message as one standard Zstandard frame at the default compression level (3) and decompression cap, see <see cref="ZstandardFrameProcessor"/>.</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = new ZstandardFrameProcessor() };

    /// <summary>Options writing Zstandard frames at <paramref name="compressionLevel"/> (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options, int compressionLevel)
        => options with { MessageProcessor = new ZstandardFrameProcessor(compressionLevel) };

    /// <summary>Options writing Zstandard frames at <paramref name="compressionLevel"/> with a custom decompression-bomb cap (see <see cref="ZstandardFrameProcessor.MaxDecompressedSize"/>).</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options, int compressionLevel, long maxDecompressedSize)
        => options with { MessageProcessor = new ZstandardFrameProcessor(compressionLevel, maxDecompressedSize) };

    /// <summary>
    /// Zstandard frames compressed with a <paramref name="dictionary"/> (trained, or raw content) that the reader must
    /// hold too; a frame declaring another dictionary is refused. See <see cref="ZstandardFrameProcessor(int, long, ReadOnlyMemory{byte})"/>.
    /// </summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options, ReadOnlyMemory<byte> dictionary, int compressionLevel = ZstandardFrameProcessor.DefaultCompressionLevel, long maxDecompressedSize = ZstandardFrameProcessor.DefaultMaxDecompressedSize)
        => options with { MessageProcessor = new ZstandardFrameProcessor(compressionLevel, maxDecompressedSize, dictionary) };
}

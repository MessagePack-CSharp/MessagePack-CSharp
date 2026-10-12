#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
#else
#endif

namespace MessagePack;

static class ZstandardThrows
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void InvalidFrame() => throw new MessagePackSerializationException("Invalid Zstandard frame.");

    public static MessagePackSerializationException DecoderRejectedFrame(Exception inner) => new("The Zstandard decoder rejected the frame: malformed data, or a frame whose window is larger than the WindowLogMax derived from MaxDecompressedSize.", inner);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void NotAFrame() => throw new MessagePackSerializationException("The message is not a Zstandard frame; the Zstandard frame processor accepts nothing else.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void DeclaredLengthExceedsMaximum(long declared, long maxDecompressedSize) => throw new MessagePackSerializationException($"Zstandard frame declares a {declared} byte decompressed length, which exceeds the configured maximum (MaxDecompressedSize {maxDecompressedSize})");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ImplausibleDeclaredLength(long declared, int compressedLength) => throw new MessagePackSerializationException($"Zstandard frame declares a {declared} byte decompressed length, which {compressedLength} compressed bytes cannot produce (a block of at least 4 bytes decodes to at most 128 KB)");
}

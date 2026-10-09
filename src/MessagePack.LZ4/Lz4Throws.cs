namespace MessagePack;

static class Lz4Throws
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void InvalidEnvelope() => throw new MessagePackSerializationException("Invalid LZ4 envelope.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void NotAFrame() => throw new MessagePackSerializationException("The message is not an LZ4 frame; the LZ4 frame processor accepts nothing else.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ImplausibleDeclaredLength(int declaredLength, int compressedLength) => throw new MessagePackSerializationException($"LZ4 envelope declares a {(uint)declaredLength} byte decompressed length, which {compressedLength} compressed bytes cannot produce (LZ4 expands at most 255:1)");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void DeclaredLengthExceedsMaximum(long totalDeclared, long maxDecompressedSize) => throw new MessagePackSerializationException($"LZ4 envelope declares a {totalDeclared} byte decompressed length, which exceeds the configured maximum (MaxDecompressedSize {maxDecompressedSize})");
}

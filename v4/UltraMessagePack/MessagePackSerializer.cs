using SerializerFoundation;
using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

// Formatter resolution history (FormatterResolutionBenchmark, ~18ns Poco entry): per-call
// dictionary 1.7x; GVM virtual GetFormatter +3ns (1.15x); frozen static generic cache ==
// resolver table read (±1ns, "UniformTable" measurement). Every entry resolves through
// resolver.GetFormatter's table read — one uniform path for every options instance.
//
// The serializer is a STATIC facade: it owns no state at all. Configuration lives in
// MessagePackSerializerOptions (today just the resolver — the formatter cache).
//
// AOT shape (System.Text.Json precedent): each entry is an overload PAIR. The options-less
// overload uses MessagePackSerializerOptions.Default — a chain that closes formatters via
// MakeGenericType — and therefore carries [RequiresDynamicCode]; the options-taking
// overload is unannotated, so Native AOT apps that pass explicit options (source-generated
// factory chain) compile with zero IL warnings. Do not merge the pairs back into an
// optional parameter: that would force the annotation onto the AOT-safe path too.
// The factory chain remains the sole extension point; the resolver holds all cached
// state. Formatters are fixed at first resolution: register factories before first use.
public static class MessagePackSerializer
{
    const int ScratchSize = 1024;

    // stitch scratch for tokens straddling sequence segment boundaries: fixed-size
    // tokens need at most 15 bytes, 64 also catches small strings — anything larger
    // falls to the buffer's retained rented temp. The size is performance-insensitive
    // beyond the fixed-token floor (the retained temp amortizes larger stitches).
    const int StitchScratchSize = 64;

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static byte[] Serialize<T>(T value)
        => Serialize(value, MessagePackSerializerOptions.Default);

    [SkipLocalsInit]
    public static byte[] Serialize<T>(T value, MessagePackSerializerOptions options)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState();
            options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> output, T value)
        => Serialize(output, value, MessagePackSerializerOptions.Default);

    // Takes the interface directly, not a `ref TBufferWriter` generic: real writers
    // (PipeWriter, ArrayBufferWriter) are classes, for which a generic TBufferWriter is
    // __Canon-shared anyway (zero specialization benefit) while forcing awkward ref
    // passing (no readonly fields/properties at call sites). The buffer touches the
    // writer only on segment refill/Flush, so interface dispatch is confined to that
    // cold path. Anyone wrapping custom state in a struct for performance should
    // implement IWriteBuffer instead — that puts them on the fully-specialized
    // formatter path, strictly better than a wrapped struct writer.
    public static void Serialize<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options)
    {
        var buffer = new BufferWriterWriteBuffer(output);
        try
        {
            var state = new SerializeState();
            options.Resolver.GetFormatter<BufferWriterWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> source)
        => Deserialize<T>(source, MessagePackSerializerOptions.Default);

    public static T Deserialize<T>(ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        T result = default!;
        Deserialize(ref result, source, options);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source)
        => Deserialize(ref value, source, MessagePackSerializerOptions.Default);

    /// <inheritdoc cref="Deserialize{T}(ref T, ReadOnlySpan{byte})"/>
    public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        var buffer = new ReadOnlySpanReadBuffer(source);
        try
        {
            var state = new DeserializeState();
            options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(in ReadOnlySequence<byte> source)
        => Deserialize<T>(source, MessagePackSerializerOptions.Default);

    public static T Deserialize<T>(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        T result = default!;
        Deserialize(ref result, source, options);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source)
        => Deserialize(ref value, source, MessagePackSerializerOptions.Default);

    /// <inheritdoc cref="Deserialize{T}(ref T, in ReadOnlySequence{byte})"/>
    [SkipLocalsInit]
    public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        Span<byte> scratch = stackalloc byte[StitchScratchSize];
        var buffer = new ReadOnlySequenceReadBuffer(source, scratch);
        try
        {
            var state = new DeserializeState();
            options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }
    }
}

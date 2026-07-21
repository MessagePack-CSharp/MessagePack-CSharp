using SerializerFoundation;

namespace UltraMessagePack;

// Formatter resolution history (DisasmProbe7, ~18ns Poco entry): per-call dictionary 1.7x;
// GVM virtual GetFormatter +3ns (1.15x); frozen static generic cache == resolver table
// read (±1ns, "UniformTable" measurement). The ReferenceEquals(this, Default) fast path
// and its DefaultCache were therefore deleted: every entry resolves through
// resolver.GetFormatter's table read — one uniform path for every instance.
// The factory chain is the sole extension point; the resolver holds all cached state and
// this class is a stateless facade (resolver + future options). Creating serializer
// instances per call over a SHARED resolver wastes only the allocation, never the
// formatter caches — the params-factories constructor, by contrast, builds a fresh
// resolver (a fresh cache) each time, so hold on to that serializer. Formatters are
// fixed at first resolution: register factories before first use.
public sealed class MessagePackSerializer
{
    public static readonly MessagePackSerializer Default = new MessagePackSerializer(new MessagePackFormatterResolver(DefaultFormatterFactory.Instance));

    const int ScratchSize = 1024;

    // stitch scratch for tokens straddling sequence segment boundaries: fixed-size
    // tokens need at most 15 bytes, 64 also catches small strings — anything larger
    // falls to the buffer's retained rented temp. The size is performance-insensitive
    // beyond the fixed-token floor (the retained temp amortizes larger stitches).
    const int StitchScratchSize = 64;

    readonly MessagePackFormatterResolver resolver;

    public MessagePackFormatterResolver Resolver => resolver;

    public MessagePackSerializer(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }

    /// <summary>
    /// Convenience: builds a private resolver over the given factory chain (first
    /// non-null wins, so put overrides BEFORE defaults). The chain is exactly what you
    /// pass — append <see cref="DefaultFormatterFactory.Instance"/> to keep the standard
    /// primitive/collection support. No factories at all means the default chain.
    /// Each call creates a fresh resolver and therefore a fresh formatter cache: hold on
    /// to the serializer (or share a resolver) instead of constructing per call.
    /// </summary>
    public MessagePackSerializer(params IMessagePackFormatterFactory[] factories)
        : this(new MessagePackFormatterResolver(
            factories.Length == 0 ? DefaultFormatterFactory.Instance
            : factories.Length == 1 ? factories[0]
            : new CompositeFormatterFactory(factories)))
    {
    }

    [SkipLocalsInit]
    public byte[] Serialize<T>(T value)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(); // TODO: Pass actual options if needed
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    // Takes the interface directly, not a `ref TBufferWriter` generic: real writers
    // (PipeWriter, ArrayBufferWriter) are classes, for which a generic TBufferWriter is
    // __Canon-shared anyway (zero specialization benefit) while forcing awkward ref
    // passing (no readonly fields/properties at call sites). The buffer touches the
    // writer only on segment refill/Flush, so interface dispatch is confined to that
    // cold path. Anyone wrapping custom state in a struct for performance should
    // implement IWriteBuffer instead — that puts them on the fully-specialized
    // formatter path, strictly better than a wrapped struct writer.
    public void Serialize<T>(IBufferWriter<byte> output, T value)
    {
        var buffer = new BufferWriterWriteBuffer(output);
        try
        {
            var state = new SerializeState(); // TODO: Pass actual options if needed
            resolver.GetFormatter<BufferWriterWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public T Deserialize<T>(ReadOnlySpan<byte> source)
    {
        T result = default!;
        Deserialize(ref result, source);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    public void Deserialize<T>(ref T value, ReadOnlySpan<byte> source)
    {
        var buffer = new ReadOnlySpanReadBuffer(source);
        try
        {
            var state = new DeserializeState();
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public T Deserialize<T>(in ReadOnlySequence<byte> source)
    {
        T result = default!;
        Deserialize(ref result, source);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    [SkipLocalsInit]
    public void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source)
    {
        Span<byte> scratch = stackalloc byte[StitchScratchSize];
        var buffer = new ReadOnlySequenceReadBuffer(source, scratch);
        try
        {
            var state = new DeserializeState();
            resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }
    }
}

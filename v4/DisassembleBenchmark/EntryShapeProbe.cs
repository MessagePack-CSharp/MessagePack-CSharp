using SerializerFoundation;
using UltraMessagePack;
using System.Runtime.CompilerServices;



// Probe for a runtime-cost question (distinct from the DisasmProbe 1-5 diagnoser-NA
// question): does the try/finally in MessagePackSerializer.Serialize<T> suppress JIT
// optimization enough to matter? Lives in this assembly so the variants stay byte-for-byte
// identical to the real entry shape. Moved out of the library (probe code does not
// belong there); everything it touches is public API, so fidelity is preserved.
// Formatter resolution goes through Default.Resolver.GetFormatter — the same single
// table-read path the real entries use since the DefaultCache deletion.
public static class EntryShapeProbe
{
    const int ScratchSize = 1024;

    // exact copy of MessagePackSerializer.Serialize<T> on the Default path
    [SkipLocalsInit]
    public static byte[] SerializeTryFinally<T>(T value)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState();
            MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    // same body, EH region removed: pooled arrays leak if the formatter throws
    [SkipLocalsInit]
    public static byte[] SerializeNoTryFinally<T>(T value)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        var state = new SerializeState();
        MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    // no Dispose at all: isolates the (straight-line) Dispose cost from the EH cost
    [SkipLocalsInit]
    public static byte[] SerializeNoDispose<T>(T value)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        var state = new SerializeState();
        MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
        return buffer.ToArray();
    }

    // entry taking an explicit serializer (used by DisasmProbe8 to swap formatters);
    // once the "UniformTable" candidate, now simply the standard entry shape
    [SkipLocalsInit]
    public static byte[] SerializeUniformTable<T>(MessagePackSerializer serializer, T value)
    {
        Span<byte> scratch = stackalloc byte[ScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState();
            serializer.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, ref value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

}

namespace MessagePack.Formatters;

// A source whose count is unknown (an iterator, a LINQ query) cannot have its array header written first. Buffering
// the elements (ToArray, a List) and writing them afterwards captures one yielded instance several times when the
// source mutates and re-yields it (a pooled buffer, a reused DTO: the pattern of code that avoids allocating per
// element), and the wire then holds the last value repeated, with no error. v3 encoded each element as it was
// enumerated into a staging buffer and wrote the header and the bytes afterwards, which captures every element at the
// moment it is yielded; this is that. The staging buffer is the Compatible pair's (a plain struct, so the element
// formatter for it exists on every target and the staging nests when the elements stage too); the calling formatter
// resolves that element formatter in Initialize, with its other nested formatters, so Serialize never reaches the
// resolver. The wire is identical to the counted path, and the source is enumerated once.
internal static class StagedEnumerable
{
    public static void Serialize<TWriteBuffer, T>(ref TWriteBuffer buffer, ref SerializeState state, IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T> formatter, IEnumerable<T> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var stage = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var count = 0;
            foreach (var item in source)
            {
                formatter.Serialize(ref stage, ref state, item);
                count++;
            }
            WriteStaged(ref buffer, ref stage, count);
        }
        finally
        {
            stage.Dispose();
        }
    }

    public static void Serialize<TWriteBuffer>(ref TWriteBuffer buffer, ref SerializeState state, IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, object?> formatter, System.Collections.IEnumerable source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var stage = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var count = 0;
            foreach (var item in source)
            {
                formatter.Serialize(ref stage, ref state, item);
                count++;
            }
            WriteStaged(ref buffer, ref stage, count);
        }
        finally
        {
            stage.Dispose();
        }
    }

    static void WriteStaged<TWriteBuffer>(ref TWriteBuffer buffer, ref CompatibleArrayPoolListWriteBuffer stage, int count)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        buffer.WriteArrayHeader(count);
        var segments = stage.GetWrittenSegments();
        while (segments.TryGetNext(out var segment))
        {
            buffer.WriteRaw(segment);
        }
    }
}

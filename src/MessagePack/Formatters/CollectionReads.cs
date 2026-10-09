namespace MessagePack.Formatters;

// Element reads for the array-backed collections. The header count has passed the byte guards (one byte per element,
// and the message-wide declared-element budget), which bounds the COUNT by the bytes of the message; the allocation
// is count * sizeof(T), so a header can still lie by sizeof(T). ReadBufferExtensions.PresizeCapacity caps what is
// allocated before any element is read at 8x the unread bytes, and when the cap bites the array is grown while
// reading, so the allocation follows the bytes actually decoded instead of the claim. Data whose in-memory size is at
// most 8x its wire size (everything but near-all-nil arrays of large nullable structs and Int128) never leaves the
// one-allocation path, so the fast loops below are the ones the formatters had inline.
internal static class CollectionReads<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
{
    /// <summary>Reads <paramref name="count"/> elements into a fresh array of exactly that length.</summary>
    public static T[] ReadArray(ref TReadBuffer buffer, ref DeserializeState state, IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> f, int count)
    {
        var capacity = ReadBufferExtensions.PresizeCapacity<T>(count, buffer.BytesRemaining);
        if (capacity < count)
        {
            return ReadGrowing(ref buffer, ref state, f, count, capacity);
        }

        var result = new T[count];
        var span = result.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }
        return result;
    }

    /// <summary>
    /// Reads <paramref name="count"/> elements into a fresh array in REVERSE stream order: stream order is top-first,
    /// but a stack is rebuilt bottom-first, so the stream head ends at the last index.
    /// </summary>
    public static T[] ReadArrayReversed(ref TReadBuffer buffer, ref DeserializeState state, IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> f, int count)
    {
        var capacity = ReadBufferExtensions.PresizeCapacity<T>(count, buffer.BytesRemaining);
        if (capacity < count)
        {
            var grown = ReadGrowing(ref buffer, ref state, f, count, capacity);
            Array.Reverse(grown);
            return grown;
        }

        var result = new T[count];
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref result[count - 1 - i]);
        }
        return result;
    }

    // the header claimed more memory than its bytes justify: start at what the bytes do justify and double as the
    // elements prove themselves, ending at exactly count (the last resize is clamped to it)
    [MethodImpl(MethodImplOptions.NoInlining)]
    static T[] ReadGrowing(ref TReadBuffer buffer, ref DeserializeState state, IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> f, int count, int capacity)
    {
        var result = new T[Math.Max(capacity, 1)];
        for (int i = 0; i < count; i++)
        {
            if (i == result.Length)
            {
                Array.Resize(ref result, (int)Math.Min(2L * result.Length, count));
            }
            f.Deserialize(ref buffer, ref state, ref result[i]);
        }
        return result;
    }
}

#if !NETSTANDARD2_0 // netstandard2.0 HashSet has no TryGetValue (canonical-instance retrieval), so the formatter is not offered there
namespace MessagePack.Formatters;

// MsgPack104 steers formatters to the resolver-configured string formatter; this IS that customization.
#pragma warning disable MsgPack104

/// <summary>
/// Reads strings through a dedup set so that repeated payload strings share one instance.
/// The set holds strong references and grows for every distinct string it ever sees, so
/// use it where the value space is known to be small (column names, enum-like tags), or
/// pass your own set to the constructor to control the sharing scope and lifetime yourself.
/// </summary>
public sealed partial class StringInterningFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
{
    const int MaxStackallocByteCount = 4096;

    // Every access inside the formatter is lock (internedStrings)
    // external mutation of a caller-supplied set must take the same lock.
    readonly HashSet<string> internedStrings;

    public StringInterningFormatter(HashSet<string> internedStrings)
    {
#if NET9_0_OR_GREATER
        StringInterningFormatterFactory.ThrowIfAlternateLookupUnsupported(internedStrings);
#endif
        this.internedStrings = internedStrings;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
    {
        buffer.WriteString(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

#if NET9_0_OR_GREATER
        var byteCount = buffer.ReadStringHeader();
        if (byteCount == 0)
        {
            value = string.Empty; // already the canonical instance
            return;
        }
        if (!buffer.TryGetSpan(byteCount, out var utf8Value))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading string.");
        }

        // The set is keyed by chars, so transcode first;
        // UTF-16 never needs more chars than UTF-8 bytes, so byteCount chars always fit.
        char[]? rented = byteCount > MaxStackallocByteCount ? ArrayPool<char>.Shared.Rent(byteCount) : null;
        Span<char> chars = rented ?? stackalloc char[byteCount];
        var charCount = System.Text.Encoding.UTF8.GetChars(utf8Value.Slice(0, byteCount), chars);
        buffer.Advance(byteCount); // chars is our own copy, the (possibly stitched) span may die now
        ReadOnlySpan<char> charSpan = chars.Slice(0, charCount);

        lock (internedStrings)
        {
            var lookup = internedStrings.GetAlternateLookup<ReadOnlySpan<char>>();
            if (!lookup.TryGetValue(charSpan, out value!))
            {
                value = new string(charSpan);
                internedStrings.Add(value);
            }
        }

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }
#else
        // netstandard2.1 has no alternate lookup, so a hit allocates the string too,
        // but still hands every caller the one canonical instance.
        var materialized = buffer.ReadString()!; // nil was consumed above
        if (materialized.Length == 0)
        {
            value = string.Empty;
            return;
        }

        lock (internedStrings)
        {
            if (internedStrings.TryGetValue(materialized, out var interned))
            {
                value = interned; // don't set materialized, materialized string will be collected by GC.
                return;
            }
            internedStrings.Add(materialized);
            value = materialized;
        }
#endif
    }
}

#endif

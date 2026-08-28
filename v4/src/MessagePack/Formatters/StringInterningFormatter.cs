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
#if NET
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

#if NET
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

/// <summary>
/// Opt-in by chain composition, before the default chain:
/// <c>new MessagePackFormatterResolver([new StringInterningFormatterFactory(), MessagePackFormatterFactory.Default])</c>.
/// Pass the same set to several factories (or pre-seed / clear it under <c>lock</c>) to
/// choose the intern pool's sharing scope and lifetime yourself; sharing the factory
/// instance itself across resolvers shares the pool the same way.
/// </summary>
public sealed partial class StringInterningFormatterFactory : MessagePackFormatterFactory
{
    // one pool shared by every buffer-pair instantiation, so all entry points (bytes / stream / sequence) intern into the same set.
    readonly HashSet<string> internedStrings;

    // The default set deliberately uses the default comparer, not HashFloodingResistantEqualityComparer.
    // The span fast path requires alternate-lookup support,
    // and the default comparer already defends HashDoS on modern .NET by collision-triggered randomized rehash
    public StringInterningFormatterFactory()
        : this(new HashSet<string>())
    {
    }

    public StringInterningFormatterFactory(HashSet<string> internedStrings)
    {
#if NET
        ThrowIfAlternateLookupUnsupported(internedStrings);
#endif
        this.internedStrings = internedStrings;
    }

#if NET
    // fail fast at composition time instead of surfacing GetAlternateLookup's
    // InvalidOperationException from the middle of a deserialization
    internal static void ThrowIfAlternateLookupUnsupported(HashSet<string> internedStrings)
    {
        if (!internedStrings.TryGetAlternateLookup<ReadOnlySpan<char>>(out _))
        {
            throw new ArgumentException(
                "The comparer of the supplied set must implement IAlternateEqualityComparer<ReadOnlySpan<char>, string> (the built-in string comparers all do).",
                nameof(internedStrings));
        }
    }
#endif

    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(string))
        {
            return new StringInterningFormatter<TWriteBuffer, TReadBuffer>(internedStrings);
        }

        return null;
    }
}

#endif

#if !NETSTANDARD2_0 // netstandard2.0 HashSet has no TryGetValue (canonical-instance retrieval), so the formatter is not offered there
using MessagePack.Formatters;

namespace MessagePack;

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
#if NET9_0_OR_GREATER
        ThrowIfAlternateLookupUnsupported(internedStrings);
#endif
        this.internedStrings = internedStrings;
    }

#if NET9_0_OR_GREATER
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

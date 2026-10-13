using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Opt-in by chain composition, before the default chain:
/// <c>new MessagePackFormatterResolver([new StringInterningFormatterFactory(), MessagePackFormatterFactory.Default])</c>.
/// Pass the same set to several factories (or pre-seed / clear it under <c>lock</c>) to
/// choose the intern pool's sharing scope and lifetime yourself; sharing the factory
/// instance itself across resolvers shares the pool the same way. The netstandard2.0 build
/// (.NET Framework) has the parameterless constructor only: its <c>HashSet&lt;T&gt;</c> cannot
/// hand back the canonical instance, so the pool there is the factory's own.
/// </summary>
public sealed partial class StringInterningFormatterFactory : MessagePackFormatterFactory
{
    // one pool shared by every buffer-pair instantiation, so all entry points (bytes / stream / sequence) intern into the same set.
#if NETSTANDARD2_0
    // netstandard2.0's HashSet<T> has no TryGetValue, so the pool maps each string to itself
    readonly Dictionary<string, string> internedStrings = new(DefaultComparer);

    public StringInterningFormatterFactory()
    {
    }
#else
    readonly HashSet<string> internedStrings;

    public StringInterningFormatterFactory()
        : this(new HashSet<string>(DefaultComparer))
    {
    }

    public StringInterningFormatterFactory(HashSet<string> internedStrings)
    {
#if NET9_0_OR_GREATER
        ThrowIfAlternateLookupUnsupported(internedStrings);
#endif
        this.internedStrings = internedStrings;
    }
#endif

#if NET9_0_OR_GREATER
    // The default comparer, not a flooding-resistant one: the span fast path needs alternate-lookup support,
    // and the runtime's own string hashing already defends HashDoS (randomized rehash once a bucket chain grows).
    static IEqualityComparer<string>? DefaultComparer => null;

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
#else
    // The library's downlevel policy, as HashFloodingResistantEqualityComparer applies it to string keys: the netstandard
    // builds also run on Mono and IL2CPP, whose string hashing is deterministic, so a payload could flood the pool's buckets.
    static IEqualityComparer<string> DefaultComparer => StringHashComparer.Instance;
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

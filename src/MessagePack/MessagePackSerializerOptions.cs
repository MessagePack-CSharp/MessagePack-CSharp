using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

/// <summary>
/// Settings for the <see cref="MessagePackSerializer"/> entry points.
/// The per-call settings here can be varied with a <c>with</c> expression.
/// Settings that shape formatter behavior live on <see cref="MessagePackFormatterResolver"/> and are fixed for its lifetime.
/// </summary>
public sealed record class MessagePackSerializerOptions
{
    // static MessagePackSerializerOptions fields lazy-load for AOT-clean

    /// <summary>Options over <see cref="MessagePackFormatterFactory.Default"/>.</summary>
    public static MessagePackSerializerOptions Default
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
        get => Volatile.Read(ref field) ?? Publish(ref field, MessagePackFormatterFactory.Default);
    }

    /// <summary>Options over <see cref="MessagePackFormatterFactory.DefaultAot"/>, for Native AOT and trimmed applications.</summary>
    public static MessagePackSerializerOptions DefaultAot
    {
        get => Volatile.Read(ref field) ?? Publish(ref field, MessagePackFormatterFactory.DefaultAot);
    }

    /// <summary>
    /// Options over <see cref="MessagePackFormatterFactory.DotNetOptimized"/>.
    /// Guid, decimal, DateTime, DateTimeOffset and BitArray use faster formats meant for .NET-to-.NET exchange, for example Guid as binary instead of a string.
    /// </summary>
    public static MessagePackSerializerOptions DotNetOptimized
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
        get => Volatile.Read(ref field) ?? Publish(ref field, MessagePackFormatterFactory.DotNetOptimized);
    }

    /// <summary>Options over <see cref="MessagePackFormatterFactory.DotNetOptimizedAot"/>, the formats of <see cref="DotNetOptimized"/> for Native AOT and trimmed applications.</summary>
    public static MessagePackSerializerOptions DotNetOptimizedAot
    {
        get => Volatile.Read(ref field) ?? Publish(ref field, MessagePackFormatterFactory.DotNetOptimizedAot);
    }

    // Publishes the first instance to win the race so every caller shares one resolver.
    // A loser is discarded before it resolves any formatter, so the race costs nothing beyond an empty resolver.
    static MessagePackSerializerOptions Publish(ref MessagePackSerializerOptions? slot, MessagePackFormatterFactory factory)
    {
        var created = new MessagePackSerializerOptions(new MessagePackFormatterResolver(factory));
        return Interlocked.CompareExchange(ref slot, created, null) ?? created;
    }

    /// <summary>
    /// Resolver that supplies the formatters. A <c>with</c> expression can point a copy of the options at another resolver.
    /// The resolver's own settings, such as required-member validation, are fixed because its cached formatters capture them: to change one, construct a new resolver.
    /// </summary>
    public MessagePackFormatterResolver Resolver
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>Optional whole-message transform, such as compression, applied after serialization and undone before deserialization.</summary>
    public MessagePackMessageProcessor? MessageProcessor { get; init; }

    /// <summary>
    /// Maximum nesting depth in both directions. A level is a formatter descending into another formatter, such as an object, a collection or a union.
    /// </summary>
    public int MaxDepth { get; init; } = 500;

    /// <summary>
    /// Maximum number of bytes the Stream and PipeReader entries buffer before parsing, per message, or per element when elements are streamed one by one.
    /// It bounds the memory an oversized or hostile stream can pin. The span and sequence entries are unaffected, since the caller owns the buffer there.
    /// Under a compressing <see cref="MessageProcessor"/> this counts the compressed bytes; the decoded size is bounded by that processor's own
    /// MaxDecompressedSize, so set both for untrusted input (a 2 KB frame may legitimately decode to the processor's full cap).
    /// Defaults to 64 MiB.
    /// </summary>
    public long MaxBufferedMessageSize { get; init; } = 64 * 1024 * 1024;

    /// <summary>
    /// Creates options over a new <see cref="MessagePackFormatterResolver"/> for <paramref name="factory"/>, with the resolver's default settings.
    /// To set a resolver property such as <see cref="MessagePackFormatterResolver.HashFloodingResistant"/>, construct the resolver yourself and pass it to the other constructor.
    /// The resolver owns the formatter cache, so build the options once and reuse them: an instance created per call rebuilds every formatter it uses.
    /// </summary>
    public MessagePackSerializerOptions(MessagePackFormatterFactory factory)
        : this(new MessagePackFormatterResolver(factory))
    {
    }

    /// <summary>
    /// Creates options over a new <see cref="MessagePackFormatterResolver"/> for <paramref name="factories"/> combined into one chain
    /// (<see cref="MessagePackFormatterFactory.Combine"/>: the first factory that serves a type wins, so overrides go first), with the resolver's default settings.
    /// </summary>
    public MessagePackSerializerOptions(MessagePackFormatterFactory[] factories)
        : this(new MessagePackFormatterResolver(factories))
    {
    }

    /// <summary>Creates options over <paramref name="resolver"/> with default per-call settings, which a <c>with</c> expression can adjust.</summary>
    public MessagePackSerializerOptions(MessagePackFormatterResolver resolver)
    {
        Resolver = resolver; // the init accessor rejects null
    }
}

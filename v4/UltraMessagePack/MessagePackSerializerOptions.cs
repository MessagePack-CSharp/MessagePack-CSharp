using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

/// <summary>
/// The configuration unit for the static <see cref="MessagePackSerializer"/> entry points.
/// </summary>
public sealed record class MessagePackSerializerOptions
{
    // lazy-load for AOT-clean
    static MessagePackSerializerOptions? defaultOptions;
    static MessagePackSerializerOptions? defaultAotOptions;

    /// <summary>
    /// Options over the default factory chain.
    /// </summary>
    public static MessagePackSerializerOptions Default
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        get => defaultOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default));
    }

    /// <summary>
    /// Options over the AOT safe factory chain.
    /// </summary>
    public static MessagePackSerializerOptions DefaultAot
    {
        get => defaultAotOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot));
    }

    readonly MessagePackFormatterResolver resolver;

    public MessagePackFormatterResolver Resolver => resolver;

    /// <summary>
    /// When true, formatters that build hash-based collections (Dictionary/HashSet/...)
    /// use an EqualityComparer resistant to hash-flooding attacks.
    /// This is the readonly, if you want to change this behavior, you need to create a new resolver with the desired setting.
    /// </summary>
    public bool HashFloodingResistant => resolver.HashFloodingResistant;

    /// <summary>
    /// Optional whole-payload transform (compression etc.), applied at the end of
    /// serialization and transparently undone at the start of deserialization.
    /// </summary>
    public MessagePackPayloadProcessor? PayloadProcessor { get; init; }

    /// <summary>
    /// Maximum container nesting depth for both directions.
    /// </summary>
    public int MaxDepth { get; init; } = 500;

    public MessagePackSerializerOptions(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }

    public MessagePackSerializerOptions(MessagePackFormatterFactory[] factories, bool hashFloodingResistant = true, bool throwOnLegacyFormatter = false)
        : this(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(factories), hashFloodingResistant, throwOnLegacyFormatter))
    {
    }
}

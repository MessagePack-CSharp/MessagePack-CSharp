using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

/// <summary>
/// The configuration unit for the static <see cref="MessagePackSerializer"/> entry points.
/// </summary>
public sealed record class MessagePackSerializerOptions
{
    // lazy-load for AOT-clean
    static MessagePackSerializerOptions? defaultOptions;
    static MessagePackSerializerOptions? defaultAotOptions;
    static MessagePackSerializerOptions? dotNetOptimizedOptions;
    static MessagePackSerializerOptions? dotNetOptimizedAotOptions;

    /// <summary>
    /// Options over the default factory chain(SourceGenerated -> BuiltIn -> Generic -> annotated Reflection).
    /// </summary>
    public static MessagePackSerializerOptions Default
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
        get => defaultOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default));
    }

    /// <summary>
    /// Options over the AOT safe factory chain (SourceGenerated -> BuiltIn).
    /// </summary>
    public static MessagePackSerializerOptions DefaultAot
    {
        get => defaultAotOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot));
    }

    /// <summary>
    /// Options over the .NET Optimzied factory chain(SourceGenerated -> DotNetOptimized -> BuiltIn -> Generic).
    /// Guid, decimal, DateTime, DateTimeOffset, and BitArray are optimized for .NET and become faster.
    /// For example, Guid is serialized as binary instead of a string, and DateTime is serialized as Ticks with the Kind preserved, instead of Timestamp.
    /// </summary>
    public static MessagePackSerializerOptions DotNetOptimized
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
        get => dotNetOptimizedOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DotNetOptimized));
    }

    /// <summary>
    /// Options over the AOT safe .NET Optimized factory chain(SourceGenerated -> DotNetOptimized -> BuiltIn).
    /// Guid, decimal, DateTime, DateTimeOffset, and BitArray are optimized for .NET and become faster.
    /// For example, Guid is serialized as binary instead of a string, and DateTime is serialized as Ticks with the Kind preserved, instead of Timestamp.
    /// </summary>
    public static MessagePackSerializerOptions DotNetOptimizedAot
    {
        get => dotNetOptimizedAotOptions ??= new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DotNetOptimizedAot));
    }

    /// <summary>
    readonly MessagePackFormatterResolver resolver;

    public MessagePackFormatterResolver Resolver => resolver;

    /// <summary>
    /// When true, formatters that build hash-based collections (Dictionary/HashSet/...)
    /// use an EqualityComparer resistant to hash-flooding attacks.
    /// This is the readonly, if you want to change this behavior, you need to create a new resolver with the desired setting.
    /// </summary>
    public bool HashFloodingResistant => resolver.HashFloodingResistant;

    /// <summary>
    /// Optional whole-message transform (compression etc.), applied at the end of
    /// serialization and transparently undone at the start of deserialization.
    /// </summary>
    public MessagePackMessageProcessor? MessageProcessor { get; init; }

    /// <summary>
    /// Maximum container nesting depth for both directions.
    /// </summary>
    public int MaxDepth { get; init; } = 500;

    /// <summary>
    /// Upper bound in bytes for a single message (or a single element of
    /// <c>DeserializeElementsAsync</c>) that a deserialization entry buffers before parsing.
    /// It applies to the Stream and PipeReader entries and bounds the memory an oversized
    /// or adversarial stream can pin. The span and sequence entries are unaffected
    /// because the caller owns the buffer there.
    /// Defaults to 64MiB. Raise it for trusted streams with larger messages, or use
    /// <c>DeserializeElementsAsync</c> to stream large arrays element by element.
    /// </summary>
    public long MaxBufferedMessageSize { get; init; } = 64 * 1024 * 1024;

    // TODO: ctor doc-comment

    public MessagePackSerializerOptions(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }
}

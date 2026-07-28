using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

/// <summary>
/// The configuration unit for the static <see cref="MessagePackSerializer"/> entry
/// points. Today it carries only the resolver (the formatter cache); future concerns
/// that a resolver cannot absorb (security limits, compression, ...) land here as
/// additional immutable properties.
///
/// <see cref="Default"/> is an immutable singleton; there is deliberately NO mutable
/// static default (the v2 DefaultOptions footgun). Customization is always an explicit
/// argument at the call site.
/// </summary>
public sealed class MessagePackSerializerOptions
{
    static MessagePackSerializerOptions? defaultOptions;

    /// <summary>
    /// Options over the default factory chain. Requires dynamic code because that chain
    /// resolves collection/Nullable/enum formatters via MakeGenericType; Native AOT apps
    /// construct options over an explicit chain (source-generated factory + primitives)
    /// and pass them to every call. Lazy so that touching this type stays AOT-clean;
    /// the ??= race is benign (equivalent instances, last write wins).
    /// </summary>
    public static MessagePackSerializerOptions Default
    {
        [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
        get => defaultOptions ??= new MessagePackSerializerOptions();
    }

    readonly MessagePackFormatterResolver resolver;

    public MessagePackFormatterResolver Resolver => resolver;

    /// <summary>Options over the default factory chain (see <see cref="Default"/> for the AOT caveat).</summary>
    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public MessagePackSerializerOptions()
        : this(new MessagePackFormatterResolver(DefaultFormatterFactory.Instance))
    {
    }

    public MessagePackSerializerOptions(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }

    /// <summary>
    /// Convenience: builds a private resolver over the given factory chain (first
    /// non-null wins, so put overrides BEFORE defaults). The chain is exactly what you
    /// pass — append <see cref="DefaultFormatterFactory.Instance"/> to keep the standard
    /// primitive/collection support; for the default chain itself use the parameterless
    /// constructor (which honestly carries its RequiresDynamicCode).
    /// Each call creates a fresh resolver and therefore a fresh formatter cache: hold on
    /// to the options instance (or share a resolver) instead of constructing per call.
    /// </summary>
    public MessagePackSerializerOptions(params IMessagePackFormatterFactory[] factories)
        : this(new MessagePackFormatterResolver(
            factories.Length == 1 ? factories[0]
            : factories.Length > 1 ? new CompositeFormatterFactory(factories)
            : throw new ArgumentException("pass at least one factory; for the default chain use the parameterless constructor", nameof(factories))))
    {
    }
}

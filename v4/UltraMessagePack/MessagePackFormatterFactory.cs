using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

/// <summary>
/// Base for the factory that produces IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, T&gt; instances.
/// </summary>
public abstract class MessagePackFormatterFactory
{
    internal const string RequiresDynamicCodeMessage =
        "The default chain contains GenericFormatterFactory, which closes collection/Nullable/enum formatters " +
        "over runtime element types via MakeGenericType. For Native AOT, use MessagePackFormatterFactory.DefaultAot " +
        "(builtin + source-generated formatters) or compose an explicit chain.";

    // lazy-load for AOT-clean
    static MessagePackFormatterFactory? defaultInstance;
    static MessagePackFormatterFactory? defaultAotInstance;
    static MessagePackFormatterFactory? dotNetOptimizedInstance;
    static MessagePackFormatterFactory? dotNetOptimizedAotInstance;

    /// <summary>
    /// The default chain (SourceGenerated -> BuiltIn -> Generic) for JIT Environment.
    /// </summary>
    public static MessagePackFormatterFactory Default
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        get => defaultInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance);
    }

    /// <summary>
    /// The default chain (SourceGenerated -> BuiltIn) for AOT Environment. This does not include generic formatters so Native AOT / trimming safe.
    /// </summary>
    public static MessagePackFormatterFactory DefaultAot
    {
        get => defaultAotInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance);
    }

    /// <summary>
    /// The .NET optimized chain (SourceGenerated -> DotNetOptimized -> BuiltIn -> Generic) for JIT Environment.
    /// <see cref="Default"/> plus alternate wire formats where the default pays for
    /// cross-language readability — Guid and decimal as 16-byte little-endian binary
    /// images, DateTime as ToBinary (preserving <see cref="DateTimeKind"/>),
    /// DateTimeOffset as Ticks + Offset, BitArray bit-packed.
    /// All reads are validated, so this is as safe for untrusted input as the default chain.
    /// </summary>
    public static MessagePackFormatterFactory DotNetOptimized
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        get => dotNetOptimizedInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            DotNetOptimizedFormatterFactory.Instance, // insert .NET Optimized before BuiltIn
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance);
    }

    /// <summary>
    /// The .NET optimized chain (SourceGenerated -> DotNetOptimized -> BuiltIn) for AOT Environment.
    /// This does not include generic formatters so Native AOT / trimming safe.
    /// <see cref="DefaultAot"/> plus alternate wire formats where the default pays for
    /// cross-language readability — Guid and decimal as 16-byte little-endian binary
    /// images, DateTime as ToBinary (preserving <see cref="DateTimeKind"/>),
    /// DateTimeOffset as Ticks + Offset, BitArray bit-packed.
    /// All reads are validated, so this is as safe for untrusted input as the default chain.
    /// </summary>
    public static MessagePackFormatterFactory DotNetOptimizedAot
    {
        get => dotNetOptimizedAotInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            DotNetOptimizedFormatterFactory.Instance, // insert .NET Optimized before BuiltIn
            BuiltInFormatterFactory.Instance);
    }

    /// <summary>
    /// Composes factories into one chain. First non-null wins, so put overrides before defaults.
    /// </summary>
    public static MessagePackFormatterFactory Combine(params MessagePackFormatterFactory[] factories)
    {
        if (factories.Length == 0)
        {
            throw new ArgumentException("pass at least one factory (e.g. MessagePackFormatterFactory.Default)", nameof(factories));
        }
        if (factories.Length == 1)
        {
            return factories[0];
        }

        var flattened = new List<MessagePackFormatterFactory>(factories.Length);
        foreach (var factory in factories)
        {
            // composites are only ever built here, so their children are already flat
            if (factory is CompositeFormatterFactory composite)
            {
                flattened.AddRange(composite.Factories);
            }
            else
            {
                flattened.Add(factory);
            }
        }
        return new CompositeFormatterFactory([.. flattened]);
    }

#if NET9_0_OR_GREATER

    // This is virtual, not abstract: a downlevel-compiled override cannot emit the `allows ref struct` flag and would fail to load (TypeLoadException).
    // However implementer "must" override this method to support the new ref struct buffers and AOT safety.

    /// <summary>
    /// Creates an IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, T&gt; for the
    /// requested type or null when this factory does not serve the type.
    /// Implementations must return a fresh instance per call, only fully stateless formatters may return a cached singleton.
    /// </summary>
    public virtual object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), type);
    }

#endif

    /// <summary>
    /// Creates an IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, T&gt; for the
    /// requested type or null when this factory does not serve the type.
    /// Implementations must return a fresh instance per call, only fully stateless formatters may return a cached singleton.
    /// This is the compatibility tier for target-framework that can't use "allows ref struct".
    /// </summary>
    public abstract object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType);
}

internal sealed class CompositeFormatterFactory : MessagePackFormatterFactory
{
    readonly MessagePackFormatterFactory[] factories;

    internal MessagePackFormatterFactory[] Factories => factories;

    internal CompositeFormatterFactory(MessagePackFormatterFactory[] factories)
    {
        this.factories = factories;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        foreach (var factory in factories)
        {
            var created = factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            if (created != null)
            {
                return created;
            }
        }
        return null;
    }
#endif

    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        foreach (var factory in factories)
        {
            var created = factory.CreateFormatter(writeBufferType, readBufferType, valueType);
            if (created != null)
            {
                return created;
            }
        }
        return null;
    }
}

/// <summary>
/// Base for the runtime-closing factory tier: an override only pattern-matches the value
/// type to an OPEN generic factory definition plus the arguments to close it with. All
/// reflection (MakeGenericType, Activator, the AOT suppressions) lives here once. Derived
/// constructors must carry [RequiresDynamicCode].
/// </summary>
public abstract class GenericFormatterFactoryBase : MessagePackFormatterFactory
{
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    protected GenericFormatterFactoryBase()
    {
    }

    /// <summary>
    /// Maps a type to the open generic factory definition that serves it (e.g.
    /// <c>typeof(ListFormatterFactory&lt;&gt;)</c>), the type arguments to close it over,
    /// and the constructor arguments (null = parameterless). Return null to decline.
    /// </summary>
    protected abstract Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind the RequiresDynamicCode protected constructor; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "open factory definitions reach here as typeof references from overrides, which roots them; the type arguments derive from the value type being resolved, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "the override's pattern match guarantees the type arguments satisfy the open definition's constraints; factory constructors are bound by the explicit argument array")]
    MessagePackFormatterFactory? CreateElementFactory(Type type)
    {
        var openFactoryType = GetOpenFactoryType(type, out var typeArguments, out var constructorArguments);
        if (openFactoryType == null)
        {
            return null;
        }

        var factoryType = openFactoryType.MakeGenericType(typeArguments);

        // args: must be the object[] overload, a lone bool would bind (Type, bool nonPublic)
        return (MessagePackFormatterFactory)Activator.CreateInstance(factoryType, args: constructorArguments)!;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return CreateElementFactory(type)?.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
    }
#endif

    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return CreateElementFactory(valueType)?.CreateFormatter(writeBufferType, readBufferType, valueType);
    }
}

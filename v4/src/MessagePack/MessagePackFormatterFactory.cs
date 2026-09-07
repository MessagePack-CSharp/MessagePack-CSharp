using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Creates the formatters a <see cref="MessagePackFormatterResolver"/> hands out.
/// Factories compose into a chain with <see cref="Combine"/>, where the first factory that serves a type wins.
/// </summary>
public abstract class MessagePackFormatterFactory
{
    internal const string RequiresDynamicCodeMessage =
        "The default chain contains GenericFormatterFactory, which closes collection/Nullable/enum formatters " +
        "over runtime element types via MakeGenericType. For Native AOT, use MessagePackFormatterFactory.DefaultAot " +
        "(builtin + source-generated formatters) or compose an explicit chain.";

    internal const string RequiresUnreferencedCodeMessage =
        "The default chain ends in a reflection tier that serves [MessagePackObject] types the source generator " +
        "did not cover, discovering their members via reflection at runtime; trimming can remove those members " +
        "silently. For trimmed or Native AOT apps use MessagePackFormatterFactory.DefaultAot.";

    // lazy-load for AOT-clean
    static MessagePackFormatterFactory? defaultInstance;
    static MessagePackFormatterFactory? defaultAotInstance;
    static MessagePackFormatterFactory? dotNetOptimizedInstance;
    static MessagePackFormatterFactory? dotNetOptimizedAotInstance;

    /// <summary>
    /// Default chain for JIT environments.
    /// Serves source-generated formatters, built-in types, generic collections, and <see cref="MessagePackObjectAttribute"/> types the generator did not cover through reflection.
    /// Types without attributes are rejected; add them with <see cref="WithContractless"/>.
    /// </summary>
    public static MessagePackFormatterFactory Default
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get => defaultInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            ObjectFallbackFormatterFactory.Instance, // object by runtime type (v3 fallback); before BuiltIn's closed-table claim
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true));
    }

    /// <summary>Default chain for Native AOT and trimmed applications. Serves source-generated and built-in formatters only.</summary>
    public static MessagePackFormatterFactory DefaultAot
    {
        get => defaultAotInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance);
    }

    /// <summary>
    /// <see cref="Default"/> plus formats that favor .NET-to-.NET exchange over cross-language readability.
    /// Guid and decimal are written as 16-byte binary, DateTime through ToBinary preserving <see cref="DateTimeKind"/>, DateTimeOffset as ticks and offset, and BitArray bit-packed.
    /// Reads are validated, so untrusted input is as safe as with the default chain.
    /// </summary>
    public static MessagePackFormatterFactory DotNetOptimized
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get => dotNetOptimizedInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            ObjectFallbackFormatterFactory.Instance, // object by runtime type (v3 fallback); before BuiltIn's closed-table claim
            DotNetOptimizedFormatterFactory.Instance, // insert .NET Optimized before BuiltIn
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true));
    }

    /// <summary><see cref="DefaultAot"/> plus the .NET-to-.NET formats described on <see cref="DotNetOptimized"/>.</summary>
    public static MessagePackFormatterFactory DotNetOptimizedAot
    {
        get => dotNetOptimizedAotInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            DotNetOptimizedFormatterFactory.Instance, // insert .NET Optimized before BuiltIn
            BuiltInFormatterFactory.Instance);
    }

    // The static presets stop at the four that mirror MessagePackSerializerOptions' presets.
    // Every optional capability composes fluently instead, so the preset surface does not explode combinatorially:
    //   MessagePackFormatterFactory.Default.WithContractless()
    //   MessagePackFormatterFactory.Default.WithContractless(allowPrivate: true)
    //   MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())

    /// <summary>
    /// Appends a tail that serializes objects without attributes as maps of member name to value, the format of v3's ContractlessStandardResolver.
    /// <paramref name="allowPrivate"/> includes non-public members, accessors and constructors.
    /// </summary>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public MessagePackFormatterFactory WithContractless(bool allowPrivate = false)
    {
        // the contractless tier is a catch-all, so it must sit last
        return Combine(this, new ReflectionFormatterFactory(annotatedOnly: false, allowPrivate));
    }

    /// <summary>
    /// Adds typeless handling, where object slots embed the concrete .NET type name, compatible with v3's Typeless format.
    /// <paramref name="typeLoader"/> decides how type names in the payload resolve on read.
    /// <c>Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())</c> is the equivalent of v3's TypelessContractlessStandardResolver.
    /// </summary>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public MessagePackFormatterFactory WithTypeless(TypelessTypeLoader typeLoader, bool omitAssemblyVersion = false)
    {
        // Typeless splits across both ends of the chain. typeof(object) must sit first (before BuiltIn's and the object
        // fallback's claims), while interface/abstract static types sit last so BuiltIn's collection-interface formatters
        // and generated union roots keep their claims. v3 kept its TypelessObjectResolver at the tail for the same reason,
        // and needed no head because nothing earlier claimed object.
        return Combine(new TypelessFormatterFactory(typeLoader, omitAssemblyVersion), this, new ForceTypelessFormatterFactory());
    }

    /// <summary>Composes factories into one chain. The first factory that serves a type wins, so put overrides before defaults.</summary>
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

    // Virtual, not abstract. A downlevel-compiled override cannot emit the `allows ref struct` flag and would fail to load (TypeLoadException).
    // Implementers should still override it to support the ref struct buffers and stay AOT safe.

    /// <summary>
    /// Creates a formatter for <paramref name="type"/>, or returns null when this factory does not serve it.
    /// Return a fresh instance per call unless the formatter is fully stateless.
    /// The default forwards to the <see cref="Type"/>-based overload; overriding this one avoids reflection.
    /// </summary>
    public virtual object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), type);
    }

#endif

    /// <summary>
    /// Creates a formatter for <paramref name="valueType"/> over the given buffer types, or returns null when this factory does not serve it.
    /// Return a fresh instance per call unless the formatter is fully stateless.
    /// This overload is required on every target framework.
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
/// Base for factories that close an open generic factory over the requested type at runtime.
/// Derived classes only map a type to the open definition and its arguments; the reflection lives here.
/// Derived constructors must carry <see cref="RequiresDynamicCodeAttribute"/>.
/// </summary>
public abstract class GenericFormatterFactoryBase : MessagePackFormatterFactory
{
    /// <summary>Initializes the factory. Requires dynamic code, so derived constructors carry the same annotation.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    protected GenericFormatterFactoryBase()
    {
    }

    /// <summary>
    /// Maps a type to the open generic factory definition that serves it, such as <c>typeof(ListFormatterFactory&lt;&gt;)</c>,
    /// the type arguments to close it with, and the constructor arguments (null for parameterless). Return null to decline.
    /// </summary>
    protected abstract Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind the RequiresDynamicCode protected constructor; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "open factory definitions reach here as typeof references from overrides, which roots them; the type arguments derive from the value type being resolved, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "the override's pattern match guarantees the type arguments satisfy the open definition's constraints; factory constructors are bound by the explicit argument array")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "same flow as IL2071 seen from ILC's dataflow: the closed type comes from MakeGenericType over a typeof'd open definition, whose constructors the typeof reference roots")]
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
    /// <inheritdoc/>
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return CreateElementFactory(type)?.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
    }
#endif

    /// <inheritdoc/>
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return CreateElementFactory(valueType)?.CreateFormatter(writeBufferType, readBufferType, valueType);
    }
}

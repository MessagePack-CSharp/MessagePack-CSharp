using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Base for the factory that produces IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, T&gt; instances.
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
    /// The default chain (SourceGenerated -> BuiltIn -> Generic -> annotated Reflection)
    /// for JIT Environment. The SourceGenerated tier serves generated object formatters
    /// AND type-level [MessagePackFormatter] annotations (the generator registers both
    /// through the module initializer — there is no runtime attribute tier, so annotated
    /// assemblies must be compiled with the generator). The reflection tail serves
    /// [MessagePackObject] types the source generator did not cover — v3 StandardResolver's
    /// DynamicObjectResolver fallback, minus the Emit. Attribute-free types still throw;
    /// opt into them with <see cref="WithContractless"/>.
    /// </summary>
    public static MessagePackFormatterFactory Default
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get => defaultInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true));
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
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get => dotNetOptimizedInstance ??= Combine(
            SourceGeneratedFormatterFactory.Instance,
            DotNetOptimizedFormatterFactory.Instance, // insert .NET Optimized before BuiltIn
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true));
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

    // The static presets stop at the four that mirror MessagePackSerializerOptions'
    // presets; every optional capability composes fluently instead, so the preset
    // surface does not explode combinatorially:
    //   MessagePackFormatterFactory.Default.WithContractless()
    //   MessagePackFormatterFactory.Default.WithContractless(allowPrivate: true)
    //   MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())

    /// <summary>
    /// Appends a contractless-object tail to this chain: attribute-free objects
    /// serialize as maps of member name to value (v3 contractless wire format), while
    /// [MessagePackObject] types keep their keyed wire form — the composed result matches
    /// v3's ContractlessStandardResolver. allowPrivate widens discovery to non-public
    /// members, accessors and constructors (the v3 AllowPrivate variants).
    /// </summary>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public MessagePackFormatterFactory WithContractless(bool allowPrivate = false)
    {
        // the contractless tier is a catch-all: it must sit LAST
        return Combine(this, new ReflectionFormatterFactory(annotatedOnly: false, allowPrivate));
    }

    /// <summary>
    /// Prepends a typeless head to this chain: object slots embed the concrete .NET
    /// type name in the payload, v3 Typeless-compatible. typeLoader decides how payload
    /// type names resolve on read (<see cref="TypelessTypeLoader.LoadAnyType"/> /
    /// <see cref="TypelessTypeLoader.AllowedTypes"/> / <see cref="TypelessTypeLoader.Create"/>).
    /// <c>Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())</c>
    /// is the v3 TypelessContractlessStandardResolver equivalent.
    /// </summary>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public MessagePackFormatterFactory WithTypeless(TypelessTypeLoader typeLoader, bool omitAssemblyVersion = false)
    {
        // typeless claims typeof(object): it must sit FIRST
        return Combine(new TypelessFormatterFactory(typeLoader, omitAssemblyVersion), this);
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

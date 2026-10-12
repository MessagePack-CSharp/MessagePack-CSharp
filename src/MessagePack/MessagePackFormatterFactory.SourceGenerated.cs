using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

// The source generator's registration surface. Generated module initializers Register the types they cover
// (object formatters and type-level [MessagePackFormatter] alike), and the built-in chains consult this tier first.
// Process-global and mutable, which is tolerable only because the writers are generated [ModuleInitializer] bodies,
// hence Register and the constructor are hidden from IntelliSense. The type and Instance stay visible: composing an
// explicit chain around Instance is the sanctioned way to put a custom factory in front of the generated tier.
// Humans direct a type at a custom formatter with the [MessagePackFormatter] attribute.

/// <summary>Serves the formatters registered by source-generated code. Consulted first by the built-in chains.</summary>
public sealed class SourceGeneratedFormatterFactory : MessagePackFormatterFactory
{
    /// <summary>Shared instance, which generated code registers into.</summary>
    public static readonly SourceGeneratedFormatterFactory Instance = new SourceGeneratedFormatterFactory(new Registrations(), closeOpenGenerics: true);

    /// <summary>
    /// The same registrations as <see cref="Instance"/> without dynamic closing: a closed instantiation of a generic
    /// <see cref="MessagePackObjectAttribute"/> type is served only when the generator constructed it statically (reached
    /// from a member graph, or declared with <see cref="MessagePackSerializableAttribute"/>). This is what
    /// <see cref="MessagePackFormatterFactory.DefaultAot"/> composes, so a JIT run of the AOT chain fails exactly where
    /// the Native AOT build would, instead of closing the formatter with <see cref="Type.MakeGenericType"/> and passing.
    /// </summary>
    public static readonly SourceGeneratedFormatterFactory StaticInstance = new SourceGeneratedFormatterFactory(Instance.registrations, closeOpenGenerics: false);

    // the two instances share one table: generated module initializers register into Instance, StaticInstance reads
    sealed class Registrations
    {
        public readonly ConcurrentDictionary<Type, MessagePackFormatterFactory> Factories = new();
        // open [MessagePackObject] definition -> open generated formatter definition (TWriteBuffer, TReadBuffer, then
        // the value type's own parameters), closed at run time for instantiations the generator never saw
        public readonly ConcurrentDictionary<Type, Type> OpenGenericFormatters = new();
    }

    readonly Registrations registrations;
    readonly bool closeOpenGenerics;

    /// <summary>Creates an empty registry. Chains use <see cref="Instance"/>, which generated code populates.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SourceGeneratedFormatterFactory()
        : this(new Registrations(), closeOpenGenerics: true)
    {
    }

    SourceGeneratedFormatterFactory(Registrations registrations, bool closeOpenGenerics)
    {
        this.registrations = registrations;
        this.closeOpenGenerics = closeOpenGenerics;
    }

    ConcurrentDictionary<Type, MessagePackFormatterFactory> factories => registrations.Factories;

    /// <summary>
    /// Registers the generated formatter definition for an open generic <see cref="MessagePackObjectAttribute"/> type:
    /// <paramref name="openFormatterType"/> takes the buffer pair and then the value type's own type parameters. An
    /// instantiation the generator did not construct statically is closed here at run time, by <see cref="Instance"/>
    /// only (<see cref="StaticInstance"/> declines it). Called by generated module initializers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterOpenGeneric(Type openValueType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type openFormatterType)
    {
        registrations.OpenGenericFormatters[openValueType] = openFormatterType;
    }

    /// <summary>
    /// Registers the factory that serves <paramref name="type"/>. Called by generated module initializers for open generic
    /// definitions; closed types go through <see cref="Register{T}"/>, which also registers the serializer's Type-based entry.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Register(Type type, MessagePackFormatterFactory factory)
    {
        factories[type] = factory;
    }

    /// <summary>
    /// Registers the factory that serves <typeparamref name="T"/> together with the serializer's Type-based entry for it.
    /// Called by generated module initializers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Register<T>(MessagePackFormatterFactory factory)
    {
        Register(typeof(T), factory);
        MessagePackSerializer.RegisterNonGeneric<T>();
    }

    /// <summary>
    /// Registers a harvested default (a closed built-in instantiation reachable from a member graph) for <paramref name="type"/>.
    /// Never displaces an existing entry: a declaration (type-level attribute, surrogate) registered by another assembly's
    /// initializer keeps its precedence regardless of module load order. Called by generated module initializers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterHarvested(Type type, MessagePackFormatterFactory factory)
    {
        factories.TryAdd(type, factory);
    }

    /// <summary>
    /// The <see cref="RegisterHarvested(Type, MessagePackFormatterFactory)"/> form for a closed type, which also registers
    /// the serializer's Type-based entry for it. Called by generated module initializers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterHarvested<T>(MessagePackFormatterFactory factory)
    {
        RegisterHarvested(typeof(T), factory);
        MessagePackSerializer.RegisterNonGeneric<T>();
    }

#if NET9_0_OR_GREATER
    /// <inheritdoc/>
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return TryFindFactory(type, out var factory)
            ? factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type)
            : CloseOpenGeneric(type, typeof(TWriteBuffer), typeof(TReadBuffer));
    }
#endif

    /// <inheritdoc/>
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return TryFindFactory(valueType, out var factory)
            ? factory.CreateFormatter(writeBufferType, readBufferType, valueType)
            : CloseOpenGeneric(valueType, writeBufferType, readBufferType);
    }

    // The one place generated formatters are closed dynamically. It used to live in every generated factory behind
    // RuntimeFeature.IsDynamicCodeSupported, which told Native AOT apart from the JIT but not a JIT run of the AOT
    // chain from a JIT run of the Default chain: DefaultAot passed every test on the JIT and failed in the published
    // app. Here the chain decides (closeOpenGenerics), and the trimmer sees RegisterOpenGeneric's constructor
    // requirement instead of a suppression.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "only Instance closes; Default and DotNetOptimized, which compose it, carry RequiresDynamicCode, DefaultAot composes StaticInstance, and a Native AOT runtime returns null through the IsDynamicCodeSupported guard")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the open formatter definition arrives through RegisterOpenGeneric, whose parameter requires the public parameterless constructor; its type arguments are the value type's own arguments, rooted by the caller")]
    object? CloseOpenGeneric(Type type, Type writeBufferType, Type readBufferType)
    {
        if (!closeOpenGenerics || !type.IsConstructedGenericType
            || !registrations.OpenGenericFormatters.TryGetValue(type.GetGenericTypeDefinition(), out var openFormatter))
        {
            return null;
        }
#if NET9_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return null; // Native AOT: the resolver's formatter-not-found names the instantiation to declare
        }
#endif
        var valueArguments = type.GetGenericArguments();
        var arguments = new Type[valueArguments.Length + 2];
        arguments[0] = writeBufferType;
        arguments[1] = readBufferType;
        valueArguments.CopyTo(arguments, 2);
        Type closed;
        try
        {
            closed = openFormatter.MakeGenericType(arguments);
        }
        catch (Exception ex) when (ex is TypeLoadException or ArgumentException or NotSupportedException || ex.GetType().Name == "ExecutionEngineException")
        {
            // ref struct buffer arguments on a runtime that cannot instantiate them, a constraint the value arguments
            // do not satisfy, an IL2CPP build without this instantiation: not served, the resolver reports not-found
            return null;
        }
        return Activator.CreateInstance(closed);
    }

    bool TryFindFactory(Type type, [NotNullWhen(true)] out MessagePackFormatterFactory? factory)
    {
        if (TryFindRegistered(type, out factory))
        {
            return true;
        }

        // Module-initializer blind spot. Registrations arrive via [ModuleInitializer], but a module cctor only runs
        // before a method of that module executes or a static field is accessed. An assembly whose first contact is
        // Deserialize<TheirType>() has executed neither (typeof as a generic argument does not count), so its
        // registration has not happened yet. Run the type's module initializers (and its generic arguments' or
        // element type's, for closures like List<TheirType>) and look again. Only the cold per-resolver-per-type
        // miss path gets here; on Native AOT module cctors already ran eagerly before Main and this is a no-op.
        RunModuleInitializers(type);
        return TryFindRegistered(type, out factory);
    }

    // Generic [MessagePackObject] types register their open definition (the generated factory closes the formatter
    // over the runtime type arguments), so a closed type that has no direct entry falls back to its definition.
    bool TryFindRegistered(Type type, [NotNullWhen(true)] out MessagePackFormatterFactory? factory)
    {
        if (factories.TryGetValue(type, out factory))
        {
            return true;
        }
        if (type.IsConstructedGenericType && factories.TryGetValue(type.GetGenericTypeDefinition(), out factory))
        {
            return true;
        }
        factory = null;
        return false;
    }

    static bool runModuleConstructorUnsupported;

    internal static void RunModuleInitializers(Type type)
    {
        if (!runModuleConstructorUnsupported)
        {
            try
            {
                RuntimeHelpers.RunModuleConstructor(type.Module.ModuleHandle);
            }
            catch (Exception ex) when (ex is not TypeInitializationException)
            {
                // Best effort. A runtime without lazy module cctors (initializers there ran at startup) has nothing
                // left for this mitigation to do. A module initializer that itself throws is that one assembly's
                // failure (TypeInitializationException, which its first method call would raise just the same), so
                // it propagates rather than switching the mitigation off for every other assembly in the process.
                runModuleConstructorUnsupported = true;
            }
        }
        if (type.IsConstructedGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                RunModuleInitializers(argument);
            }
        }
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            RunModuleInitializers(element);
        }
    }
}

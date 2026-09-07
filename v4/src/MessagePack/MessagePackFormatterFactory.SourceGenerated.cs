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
    public static readonly SourceGeneratedFormatterFactory Instance = new SourceGeneratedFormatterFactory();

    ConcurrentDictionary<Type, MessagePackFormatterFactory> factories = new ConcurrentDictionary<Type, MessagePackFormatterFactory>();

    /// <summary>Creates an empty registry. Chains use <see cref="Instance"/>, which generated code populates.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SourceGeneratedFormatterFactory()
    {
    }

    /// <summary>Registers the factory that serves <paramref name="type"/>. Called by generated module initializers.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Register(Type type, MessagePackFormatterFactory factory)
    {
        factories[type] = factory;
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

#if NET9_0_OR_GREATER
    /// <inheritdoc/>
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return TryFindFactory(type, out var factory)
            ? factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type)
            : null;
    }
#endif

    /// <inheritdoc/>
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return TryFindFactory(valueType, out var factory)
            ? factory.CreateFormatter(writeBufferType, readBufferType, valueType)
            : null;
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

    static void RunModuleInitializers(Type type)
    {
        if (!runModuleConstructorUnsupported)
        {
            try
            {
                RuntimeHelpers.RunModuleConstructor(type.Module.ModuleHandle);
            }
            catch (Exception)
            {
                // Best effort. A runtime without lazy module cctors (initializers there ran at startup) has nothing
                // left for this mitigation to do.
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

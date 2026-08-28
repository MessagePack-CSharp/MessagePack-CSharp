using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MessagePack;

// The SOURCE GENERATOR's registration surface: generated module initializers Register the
// types they cover (object formatters and type-level [MessagePackFormatter] alike), and the
// built-in chains consult this tier first. Process-global and mutable, which is tolerable
// only because the writers are generated [ModuleInitializer] bodies — hence hidden from
// IntelliSense: humans direct a type at a custom formatter with the [MessagePackFormatter]
// attribute, or compose factories into an explicit chain.

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SourceGeneratedFormatterFactory : MessagePackFormatterFactory
{
    public static readonly SourceGeneratedFormatterFactory Instance = new SourceGeneratedFormatterFactory();

    ConcurrentDictionary<Type, MessagePackFormatterFactory> factories = new ConcurrentDictionary<Type, MessagePackFormatterFactory>();

    public SourceGeneratedFormatterFactory()
    {
    }

    public void Register(Type type, MessagePackFormatterFactory factory)
    {
        factories[type] = factory;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return TryFindFactory(type, out var factory)
            ? factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type)
            : null;
    }
#endif

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

        // Module-initializer blind spot: registrations arrive via [ModuleInitializer], but a
        // module cctor only runs before a METHOD of that module executes or a static field is
        // accessed — an assembly whose first contact is Deserialize<TheirType>() has executed
        // neither (typeof as a generic argument does not count), so its registration has not
        // happened yet. Run the type's module initializers (and its generic arguments' /
        // element type's, for closures like List<TheirType>) and look again. Only the cold
        // per-resolver-per-type miss path gets here; on Native AOT module cctors already ran
        // eagerly before Main and this is a no-op.
        RunModuleInitializers(type);
        return TryFindRegistered(type, out factory);
    }

    // generic [MessagePackObject] types register their OPEN definition (the generated
    // factory closes the formatter over the runtime type arguments), so a closed type
    // that has no direct entry falls back to its definition
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
                // best effort: a runtime without lazy module cctors (initializers there ran
                // at startup) has nothing left for this mitigation to do
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

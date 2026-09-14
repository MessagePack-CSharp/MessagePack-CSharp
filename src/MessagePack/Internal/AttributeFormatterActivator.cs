using System.Diagnostics.CodeAnalysis;
using SerializerFoundation;

namespace MessagePack;

// Runtime interpreter for member-level [MessagePackFormatter], used by ReflectionObjectFormatter's member slots
// (source-generated formatters bind the attribute at compile time instead). Type-level [MessagePackFormatter] has no
// runtime tier. The source generator compiles it into the generated registration, and a type that reaches
// ReflectionFormatterFactory still carrying the annotation gets the targeted diagnostic from TypeLevelNotCompiled.
// The v3 forms are honored, either a fully constructed MessagePackFormatterFactory or a formatter passed as an unbound
// generic over the buffer pair (typeof(MyFormatter<,>)), with literal constructor arguments; the attribute is matched
// by name so v3 annotation assemblies keep working. The source generator's expression-string form
// ("StringComparer.OrdinalIgnoreCase") binds at compile time and cannot be resolved at runtime; it is rejected with a
// pointer at the generator instead of a MissingMethod maze.
internal static class AttributeFormatterActivator
{
    internal static object? FindAttribute(object[] attributes, string fullName)
    {
        foreach (var attribute in attributes)
        {
            // walk the attribute's base chain so a derived annotation still matches, the way typed GetCustomAttribute<TBase> would
            for (var type = attribute.GetType(); type is not null; type = type.BaseType)
            {
                if (type.FullName == fullName)
                {
                    return attribute;
                }
            }
        }
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind the factory's RequiresUnreferencedCode acquisition; attributes applied in user code keep their properties rooted")]
    static object? ReadProperty(object attribute, string propertyName)
    {
        return attribute.GetType().GetProperty(propertyName)?.GetValue(attribute);
    }

    static (Type FormatterType, object?[] Arguments) Read(object attribute, string subject)
    {
        // FactoryType is v4's name; FormatterType is the fallback for v3 annotation assemblies, whose attribute is matched
        // by name like the rest of the contract
        if ((ReadProperty(attribute, "FactoryType") ?? ReadProperty(attribute, "FormatterType")) is not Type formatterType)
        {
            throw new MessagePackSerializationException($"[MessagePackFormatter] on '{subject}' does not carry a factory type.");
        }
        return (formatterType, ReadProperty(attribute, "Arguments") as object?[] ?? []);
    }

#if NET9_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "callers are gated by RequiresDynamicCode at factory acquisition; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the formatter type comes from a [MessagePackFormatter] attribute in user code, which roots it; same RequiresUnreferencedCode gate")]
    internal static object Create<TWriteBuffer, TReadBuffer>(object attribute, Type valueType, string subject)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        var (formatterType, arguments) = Read(attribute, subject);
        try
        {
            if (typeof(MessagePackFormatterFactory).IsAssignableFrom(formatterType))
            {
                var factory = CreateFactory(formatterType, arguments, subject);
                return factory.CreateFormatter<TWriteBuffer, TReadBuffer>(valueType)
                    ?? throw DidNotCreate(formatterType, valueType, subject);
            }
            if (formatterType.IsGenericTypeDefinition && formatterType.GetGenericArguments().Length == 2)
            {
                return CreateInstance(formatterType.MakeGenericType(typeof(TWriteBuffer), typeof(TReadBuffer)), arguments, subject);
            }
        }
        catch (Exception broken) when (broken is TypeLoadException or System.IO.FileNotFoundException or System.IO.FileLoadException)
        {
            throw V3AssemblyBoundFormatter(formatterType, subject, broken);
        }
        throw NeitherForm(formatterType, subject);
    }
#endif

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "callers are gated by RequiresDynamicCode at factory acquisition; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the formatter type comes from a [MessagePackFormatter] attribute in user code, which roots it; same RequiresUnreferencedCode gate")]
    internal static object Create(object attribute, Type valueType, Type writeBufferType, Type readBufferType, string subject)
    {
        var (formatterType, arguments) = Read(attribute, subject);
        try
        {
            if (typeof(MessagePackFormatterFactory).IsAssignableFrom(formatterType))
            {
                var factory = CreateFactory(formatterType, arguments, subject);
                return factory.CreateFormatter(writeBufferType, readBufferType, valueType)
                    ?? throw DidNotCreate(formatterType, valueType, subject);
            }
            if (formatterType.IsGenericTypeDefinition && formatterType.GetGenericArguments().Length == 2)
            {
                return CreateInstance(formatterType.MakeGenericType(writeBufferType, readBufferType), arguments, subject);
            }
        }
        catch (Exception broken) when (broken is TypeLoadException or System.IO.FileNotFoundException or System.IO.FileLoadException)
        {
            throw V3AssemblyBoundFormatter(formatterType, subject, broken);
        }
        throw NeitherForm(formatterType, subject);
    }

    static MessagePackFormatterFactory CreateFactory(Type formatterType, object?[] arguments, string subject)
    {
        if (formatterType.ContainsGenericParameters)
        {
            throw new MessagePackSerializationException($"[MessagePackFormatter] on '{subject}': '{formatterType}' must be a fully constructed MessagePackFormatterFactory.");
        }
        return (MessagePackFormatterFactory)CreateInstance(formatterType, arguments, subject);
    }

    static MessagePackSerializationException DidNotCreate(Type formatterType, Type valueType, string subject) =>
        new($"[MessagePackFormatter] on '{subject}': '{formatterType}' did not create a formatter for '{valueType}'.");

    static MessagePackSerializationException NeitherForm(Type formatterType, string subject) =>
        LooksLikeV3Formatter(formatterType)
            ? new($"[MessagePackFormatter] on '{subject}': '{formatterType}' implements v3's IMessagePackFormatter<T>. v3-compiled formatter code cannot run on v4 (it is bound to v3's MessagePackWriter/Reader); port it to a MessagePackFormatterFactory or a buffer-generic formatter (typeof(MyFormatter<,>)).")
            : new($"[MessagePackFormatter] on '{subject}': '{formatterType}' is neither a MessagePackFormatterFactory nor an unbound formatter generic over the buffer pair (typeof(MyFormatter<,>)).");

    // An assembly compiled against v3's MessagePack.dll binds to this v4 assembly by simple name and then cannot find
    // the v3-only types its formatters reference. Surface that as the migration problem it is instead of a bare loader
    // exception.
    static MessagePackSerializationException V3AssemblyBoundFormatter(Type formatterType, string subject, Exception broken) =>
        new($"[MessagePackFormatter] on '{subject}': '{formatterType}' could not be inspected because types it references failed to load. A formatter compiled against MessagePack v3 cannot run on v4; port it to a MessagePackFormatterFactory or a buffer-generic formatter (typeof(MyFormatter<,>)).", broken);

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "diagnostic-only interface scan on a [MessagePackFormatter]-named type, reached only after formatter creation already failed")]
    static bool LooksLikeV3Formatter(Type formatterType)
    {
        try
        {
            foreach (var implemented in formatterType.GetInterfaces())
            {
                // v3's formatter interface is MessagePack.Formatters.IMessagePackFormatter`1
                // (v4's is the buffer-generic MessagePack.IMessagePackFormatter`3)
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition().FullName == "MessagePack.Formatters.IMessagePackFormatter`1")
                {
                    return true;
                }
            }
        }
        catch (TypeLoadException)
        {
        }
        return false;
    }

    // A type-level [MessagePackFormatter] reaching the reflection tier means the source generator did not register the
    // type (the assembly was built without the generator, or generation was suppressed). No runtime tier serves it,
    // because the annotation points at executable code and the one population that would need such a tier, pre-built
    // v3 DLLs, names v3-compiled formatters that cannot run on v4 anyway. Name the actual problem instead of a bare
    // "no formatter" miss.
    internal static MessagePackSerializationException TypeLevelNotCompiled(object attribute, Type type)
    {
        var subject = type.FullName ?? type.Name;
        Type formatterType;
        try
        {
            (formatterType, _) = Read(attribute, subject);
        }
        catch (MessagePackSerializationException noType)
        {
            return noType;
        }
        try
        {
            if (LooksLikeV3Formatter(formatterType))
            {
                return NeitherForm(formatterType, subject);
            }
        }
        catch (Exception broken) when (broken is TypeLoadException or System.IO.FileNotFoundException or System.IO.FileLoadException)
        {
            return V3AssemblyBoundFormatter(formatterType, subject, broken);
        }
        return new($"[MessagePackFormatter] on '{subject}' names '{formatterType}', but no formatter is registered for the type: the type-level annotation is compiled by the MessagePack source generator, which did not run on the assembly declaring '{subject}' (or source generation was suppressed). Build that assembly with the generator, or compose '{formatterType}' into the factory chain explicitly.");
    }

    internal const string AttributeFullName = "MessagePack.MessagePackFormatterAttribute";

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "the constructed type comes from a [MessagePackFormatter] attribute in user code, which roots its constructors; same RequiresUnreferencedCode gate")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "same [MessagePackFormatter]-rooted type as above; GetConstructors only runs on the failure path to build the error message")]
    static object CreateInstance(Type type, object?[] arguments, string subject)
    {
        try
        {
            return Activator.CreateInstance(type, arguments)!;
        }
        catch (MissingMethodException missingMethod)
        {
            foreach (var constructor in type.GetConstructors())
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != arguments.Length)
                {
                    continue;
                }
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (arguments[i] is string text && parameters[i].ParameterType != typeof(string) && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                    {
                        throw new MessagePackSerializationException($"[MessagePackFormatter] on '{subject}': the string argument \"{text}\" looks like the source generator's expression form, which binds at compile time; here pass arguments the runtime can bind directly.", missingMethod);
                    }
                }
            }
            throw new MessagePackSerializationException($"[MessagePackFormatter] on '{subject}': no accessible constructor of '{type}' takes the {arguments.Length} supplied argument(s).", missingMethod);
        }
    }
}

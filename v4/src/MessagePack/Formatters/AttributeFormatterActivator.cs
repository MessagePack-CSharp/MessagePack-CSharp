using System.Diagnostics.CodeAnalysis;
using SerializerFoundation;

namespace MessagePack;

/// <summary>
/// Runtime interpreter for member-level [MessagePackFormatter], used by
/// ReflectionObjectFormatter's member slots (source-generated formatters bind the
/// attribute at compile time instead). The v3 forms are honored: a fully constructed
/// MessagePackFormatterFactory, or a formatter passed as an unbound generic over the
/// buffer pair (typeof(MyFormatter&lt;,&gt;)), with literal constructor arguments; the
/// attribute is matched by NAME so v3 annotation assemblies keep working. The source
/// generator's expression-string form ("StringComparer.OrdinalIgnoreCase") binds at
/// compile time and cannot be resolved at runtime; it is rejected with a pointer at the
/// generator instead of a MissingMethod maze.
/// </summary>
internal static class AttributeFormatterActivator
{
    internal static object? FindAttribute(object[] attributes, string fullName)
    {
        foreach (var attribute in attributes)
        {
            // walk the attribute's base chain so a derived annotation still matches,
            // the way typed GetCustomAttribute<TBase> would
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
        // FactoryType is v4's name; FormatterType is the fallback for v3 annotation
        // assemblies, whose attribute is matched by name like the rest of the contract
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
        throw NeitherForm(formatterType, subject);
    }
#endif

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "callers are gated by RequiresDynamicCode at factory acquisition; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the formatter type comes from a [MessagePackFormatter] attribute in user code, which roots it; same RequiresUnreferencedCode gate")]
    internal static object Create(object attribute, Type valueType, Type writeBufferType, Type readBufferType, string subject)
    {
        var (formatterType, arguments) = Read(attribute, subject);
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
        new($"[MessagePackFormatter] on '{subject}': '{formatterType}' is neither a MessagePackFormatterFactory nor an unbound formatter generic over the buffer pair (typeof(MyFormatter<,>)).");

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

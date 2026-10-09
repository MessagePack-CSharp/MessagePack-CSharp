using System.Reflection;
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
                // the factory itself: ReflectionObjectFormatter creates its formatter through
                // MessagePackFormatterResolver.CreateFormatterWith, which reroutes a downlevel one
                return CreateFactory(formatterType, arguments, subject);
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
                return CreateFactory(formatterType, arguments, subject); // see the generic overload
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

    // candidate A is better than B when every supplied argument converts at least as well to A's parameter as to B's
    // and one strictly better: an exact type beats any conversion, and between two conversions the parameter type
    // that converts implicitly to the other (long into double) is the better target, C#'s rule
    static bool IsBetterCandidate(ParameterInfo[] a, ParameterInfo[] b, int argumentCount)
    {
        var strictlyBetter = false;
        for (int i = 0; i < argumentCount; i++)
        {
            var ta = a[i].ParameterType;
            var tb = b[i].ParameterType;
            if (ta == tb)
            {
                continue;
            }
            var aToB = ConvertsImplicitly(ta, tb);
            var bToA = ConvertsImplicitly(tb, ta);
            if (aToB && !bToA)
            {
                strictlyBetter = true;
            }
            else if (bToA && !aToB)
            {
                return false;
            }
        }
        return strictlyBetter;
    }

    // C#'s implicit numeric conversions (plus reference assignability)
    static bool ConvertsImplicitly(Type from, Type to)
    {
        if (to.IsAssignableFrom(from))
        {
            return true;
        }
        var code = Type.GetTypeCode(from);
        var target = Type.GetTypeCode(to);
        if (!from.IsPrimitive && code != TypeCode.Decimal || !to.IsPrimitive && target != TypeCode.Decimal)
        {
            return false;
        }
        return code switch
        {
            TypeCode.SByte => target is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Byte => target is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int16 => target is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt16 => target is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int32 => target is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt32 => target is TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int64 => target is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt64 => target is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Char => target is TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Single => target is TypeCode.Double,
            _ => false,
        };
    }

    static bool BindsWithDefaults(ParameterInfo[] parameters, object?[] arguments)
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i >= arguments.Length)
            {
                if (!parameters[i].IsOptional)
                {
                    return false;
                }
            }
            else if (!Binds(arguments[i], parameters[i].ParameterType))
            {
                return false;
            }
        }
        return true;
    }

    // an attribute argument is a literal of its own type (an int `10` for a long parameter): the compiler converts
    // these at a normal call site, and the generator's expression form likewise, so the runtime binding does too
    static bool Binds(object? argument, Type parameterType)
    {
        if (argument is null)
        {
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) is not null;
        }
        if (parameterType.IsInstanceOfType(argument))
        {
            return true;
        }
        var target = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (target.IsEnum)
        {
            return argument is IConvertible && argument.GetType().IsPrimitive && argument is not bool && argument is not char;
        }
        // only the implicit conversions a call site would apply (int into long, never int into short: the generator
        // binds the same way, and a narrowing conversion would silently overflow)
        return ConvertsImplicitly(argument.GetType(), target);
    }

    static object? Coerce(object? argument, Type parameterType)
    {
        if (argument is null || parameterType.IsInstanceOfType(argument))
        {
            return argument;
        }
        var target = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        return target.IsEnum
            ? Enum.ToObject(target, argument)
            : Convert.ChangeType(argument, target, System.Globalization.CultureInfo.InvariantCulture);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "the constructed type comes from a [MessagePackFormatter] attribute in user code, which roots its constructors; same RequiresUnreferencedCode gate")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "same [MessagePackFormatter]-rooted type as above; GetConstructors only runs on the failure path to build the error message")]
    static object CreateInstance(Type type, object?[] arguments, string subject)
    {
        // the generator's overload choice, so that a member served through reflection takes the same constructor
        // as one served by generated code: the supplied arguments' conversions decide first (an exact parameter type
        // beats a converting one, then the better conversion target: long over double for an int), and only among
        // conversion-equivalent candidates does the one filling fewer defaults win. Activator.CreateInstance would
        // pick F(long) over F(int, bool = false) for the literal 10, and never consider the optional parameters.
        List<(ConstructorInfo Constructor, ParameterInfo[] Parameters, int Exact)>? applicable = null;
        foreach (var constructor in type.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length < arguments.Length || !BindsWithDefaults(parameters, arguments))
            {
                continue;
            }
            var exact = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] is { } argument && argument.GetType() == parameters[i].ParameterType)
                {
                    exact++;
                }
            }
            (applicable ??= new()).Add((constructor, parameters, exact));
        }
        if (applicable is not null)
        {
            var mostExact = applicable.Max(static a => a.Exact);
            var winners = applicable.Where(a => a.Exact == mostExact).ToList();
            if (winners.Count > 1)
            {
                var best = winners.Where(candidate => winners.All(other => ReferenceEquals(other.Constructor, candidate.Constructor) || IsBetterCandidate(candidate.Parameters, other.Parameters, arguments.Length))).ToList();
                if (best.Count == 1)
                {
                    winners = best;
                }
            }
            if (winners.Count > 1)
            {
                var fewestDefaults = winners.Min(a => a.Parameters.Length - arguments.Length);
                winners = winners.Where(a => a.Parameters.Length - arguments.Length == fewestDefaults).ToList();
            }
            if (winners.Count == 1)
            {
                var (constructor, parameters, _) = winners[0];
                var padded = new object?[parameters.Length];
                for (int i = 0; i < padded.Length; i++)
                {
                    padded[i] = i < arguments.Length
                        ? Coerce(arguments[i], parameters[i].ParameterType)
                        : parameters[i].DefaultValue is DBNull ? Type.Missing : parameters[i].DefaultValue;
                }
                return constructor.Invoke(padded);
            }
            throw new MessagePackSerializationException($"[MessagePackFormatter] on '{subject}': the arguments bind to more than one constructor of '{type}'; disambiguate the overloads.");
        }
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

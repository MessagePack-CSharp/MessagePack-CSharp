using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

// The reflection tier's [MessagePackObject(EnumAsString = true)]: the run-time twin of the source generator's member
// trees (ObjectParser.BuildEnumNameNode, ObjectEmitter.EmitEnumNameNode). The resolver's formatters are shared by every
// use site and keep the integers, so a member whose type holds an enum builds its own formatter tree: the enums are
// EnumAsStringFormatter leaves, and each container is created fresh and takes its enum-bearing slots through
// ICollectionFormatterInitializer / IKeyValueFormatterInitializer before Initialize resolves the rest.
// The walk is the generator's: array elements and generic type arguments, stopping at a type with a schema of its own
// (ReflectionFormatterFactory.HasObjectClaim), at an enum with a type-level [MessagePackFormatter], at a non-generic type
// and at an open type parameter. A container whose formatter takes no nested formatter (a tuple, ILookup, a
// multi-dimensional array, a custom collection) fails at creation with the generator's MsgPack030 advice, and so does
// a collection type whose elements hold the enum without any type argument of its own saying so
// (`class StatusList : List<Status>`), which the walk cannot enter at all.
internal sealed class EnumNameFormatterTree
{
    static GenericEnumAsStringFormatterFactory? enumNames;

    readonly Type type;
    // null for a leaf; otherwise one per element (arrays) or generic type argument, null where no such enum is
    readonly EnumNameFormatterTree?[]? slots;
    // a collection type the walk cannot enter: its single slot is the element's tree, kept for the message only
    readonly bool unreachable;

    EnumNameFormatterTree(Type type, EnumNameFormatterTree?[]? slots, bool unreachable = false)
    {
        this.type = type;
        this.slots = slots;
        this.unreachable = unreachable;
    }

    // null when no enum of the member type is written by name: the member keeps the resolver's formatter. `declared` is
    // the same type as the declaration spells it (ReflectionObjectFormatter.DeclaredMemberType), walked alongside so
    // that a part the declaration leaves to a type parameter stops the walk, as it does in the generator.
    public static EnumNameFormatterTree? Build(Type type, Type declared) => Build(type, declared, null);

    // `probing` is the recursion guard of the element probe (a type that enumerates itself, `Node : IEnumerable<Node>`)
    static EnumNameFormatterTree? Build(Type type, Type declared, HashSet<Type>? probing)
    {
        if (declared.IsGenericParameter)
        {
            return null;
        }
        if (type.IsEnum)
        {
            return AttributeFormatterActivator.FindAttribute(type.GetCustomAttributes(inherit: false), AttributeFormatterActivator.AttributeFullName) is null
                ? new EnumNameFormatterTree(type, null)
                : null;
        }
        Type[] parts;
        Type[] declaredParts;
        if (type.IsArray)
        {
            parts = [type.GetElementType()!];
            declaredParts = [declared.GetElementType()!];
        }
        else if (type.IsGenericType && !type.ContainsGenericParameters && !ReflectionFormatterFactory.HasObjectClaim(type))
        {
            parts = type.GetGenericArguments();
            declaredParts = declared.GetGenericArguments();
        }
        else
        {
            // a non-generic type: nothing to walk, but a collection of the enum would write it through the element
            // formatter the resolver shares, as an integer
            return type.ContainsGenericParameters || ReflectionFormatterFactory.HasObjectClaim(type) ? null : ElementProbe(type, declared, ref probing);
        }
        var slots = new EnumNameFormatterTree?[parts.Length];
        var any = false;
        for (int i = 0; i < parts.Length; i++)
        {
            slots[i] = Build(parts[i], declaredParts[i], probing);
            any |= slots[i] is not null;
        }
        // no type argument reaches an enum, but the elements may all the same: `Tagged<T> : List<Status>`
        return any ? new EnumNameFormatterTree(type, slots) : type.IsArray ? null : ElementProbe(type, declared, ref probing);
    }

    // The generator's EnumOfCollectionElement: the element type of each IEnumerable<T> the type implements is walked like
    // a member type (a dictionary's is its KeyValuePair). A hit is a node that only ever throws, at Create. The declared
    // spelling's interfaces are walked alongside (an instantiation lists its interfaces in its definition's order), so an
    // element the declaration leaves to a type parameter (`List<T>` closed over an enum) stops the probe as it does
    // the walk.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "the interfaces of a serialized member's type, which the reflection tier's RequiresUnreferencedCode gate covers")]
    static EnumNameFormatterTree? ElementProbe(Type collection, Type declared, ref HashSet<Type>? probing)
    {
        if (collection.IsPrimitive || collection == typeof(string) || collection == typeof(object))
        {
            return null;
        }
        probing ??= [];
        if (!probing.Add(collection))
        {
            return null;
        }
        try
        {
            var interfaces = collection.GetInterfaces();
            var declaredInterfaces = declared == collection ? interfaces : declared.GetInterfaces();
            if (declaredInterfaces.Length != interfaces.Length)
            {
                declaredInterfaces = interfaces;
            }
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].IsGenericType
                    && interfaces[i].GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && declaredInterfaces[i].IsGenericType
                    && declaredInterfaces[i].GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && Build(interfaces[i].GetGenericArguments()[0], declaredInterfaces[i].GetGenericArguments()[0], probing) is { } elementTree)
                {
                    return new EnumNameFormatterTree(collection, [elementTree], unreachable: true);
                }
            }
            return null;
        }
        finally
        {
            probing.Remove(collection);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "the reflection tier's member slots only exist behind ReflectionFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "same RequiresUnreferencedCode gate as the reflection tier that builds these trees")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the initializer and formatter interfaces are typeof'd here, which roots them; the type arguments come from the member type, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "the buffer arguments are the resolver's own buffer types (or the Compatible pair), value types whose parameterless constructor the struct constraint already requires")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the Initialize members of the interfaces typeof'd here")]
    public object Create<TWriteBuffer, TReadBuffer>(MessagePackFormatterResolver resolver, string subject)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (slots is null)
        {
            var leaf = CreateFormatter<TWriteBuffer, TReadBuffer>(enumNames ??= new GenericEnumAsStringFormatterFactory(), type)!; // benign race: stateless
            return Initialize<TWriteBuffer, TReadBuffer>(leaf, resolver);
        }
        if (unreachable)
        {
            throw Unreachable(subject);
        }

        // a fresh instance, never the resolver's shared one
        var formatter = CreateFormatter<TWriteBuffer, TReadBuffer>(GenericFormatterFactory.Instance, type);
        var parts = type.IsArray ? [type.GetElementType()!] : type.GetGenericArguments();
        if (slots.Length == 1
            && (!type.IsArray || type.GetArrayRank() == 1)
            && typeof(ICollectionFormatterInitializer<,,>).MakeGenericType(typeof(TWriteBuffer), typeof(TReadBuffer), parts[0]) is { } single
            && single.IsInstanceOfType(formatter))
        {
            single.GetMethod("Initialize")!.InvokeUnwrapped(formatter, [slots[0]!.Create<TWriteBuffer, TReadBuffer>(resolver, subject)]);
            var staged = typeof(IStagedCollectionFormatterInitializer<>).MakeGenericType(parts[0]);
            if (staged.IsInstanceOfType(formatter))
            {
                // a source without a count stages its elements over the Compatible pair, through the same tree over those buffers
                staged.GetMethod("InitializeStaged")!.InvokeUnwrapped(formatter, [slots[0]!.Create<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>(resolver, subject)]);
            }
        }
        else if (slots.Length == 2
            && typeof(IKeyValueFormatterInitializer<,,,>).MakeGenericType(typeof(TWriteBuffer), typeof(TReadBuffer), parts[0], parts[1]) is { } keyValue
            && keyValue.IsInstanceOfType(formatter))
        {
            keyValue.GetMethod("Initialize")!.InvokeUnwrapped(formatter, [slots[0]?.Create<TWriteBuffer, TReadBuffer>(resolver, subject), slots[1]?.Create<TWriteBuffer, TReadBuffer>(resolver, subject)]);
        }
        else
        {
            throw Unreachable(subject);
        }
        return Initialize<TWriteBuffer, TReadBuffer>(formatter!, resolver);
    }

    MessagePackSerializationException Unreachable(string subject)
    {
        var enumType = FirstEnum(this);
        return new MessagePackSerializationException($"'{subject}': EnumAsString cannot reach the enum '{enumType}' inside '{type}', whose formatter takes no nested formatter from outside (tuples, ILookup, multi-dimensional arrays and custom collections do not), so it would stay an integer. Annotate the enum with [MessagePackFormatter(typeof(EnumAsStringFormatterFactory<{enumType.Name}>))] to write it by name everywhere, or give the member a [MessagePackFormatter] of its own.");
    }

    // on net9+ the generic member is the factories' creation path (their Type-based overload declines there)
    static object? CreateFormatter<TWriteBuffer, TReadBuffer>(MessagePackFormatterFactory factory, Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
#if NET9_0_OR_GREATER
        return factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
#else
        return factory.CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), type);
#endif
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "see Create")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "see Create")]
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "see Create")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "see Create")]
    object Initialize<TWriteBuffer, TReadBuffer>(object formatter, MessagePackFormatterResolver resolver)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        typeof(IMessagePackFormatter<,,>).MakeGenericType(typeof(TWriteBuffer), typeof(TReadBuffer), type).GetMethod("Initialize")!.InvokeUnwrapped(formatter, [resolver]);
        return formatter;
    }

    static Type FirstEnum(EnumNameFormatterTree node)
    {
        if (node.slots is null)
        {
            return node.type;
        }
        foreach (var slot in node.slots)
        {
            if (slot is not null)
            {
                return FirstEnum(slot);
            }
        }
        throw new InvalidOperationException("unreachable: a container node holds at least one enum");
    }
}

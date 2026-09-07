using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Closes generic formatters over runtime type arguments for collections, dictionaries, tuples, Nullable, enums and similar shapes.
/// Requires dynamic code, so the AOT chains leave it out.
/// </summary>
public sealed class GenericFormatterFactory : GenericFormatterFactoryBase
{
    static GenericFormatterFactory? instance;

    /// <summary>Shared instance.</summary>
    public static GenericFormatterFactory Instance
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        get => instance ??= new GenericFormatterFactory(); // benign race: stateless singleton
    }

    // Public for [MessagePackFormatter] use (the attribute paths construct via new). The RequiresDynamicCode gate
    // carries over, so AOT-enabled consumers get the warning.
    /// <summary>Creates a new instance, for use with <see cref="MessagePackFormatterAttribute"/>. Chains use <see cref="Instance"/>.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public GenericFormatterFactory()
    {
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "the default-ctor probe only refines routing for the non-generic IList/IDictionary catch-all; this whole tier is gated by RequiresDynamicCode at acquisition, and a trimmed-away ctor just means the type falls through to the missing-formatter path")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "the interface instantiations (IEnumerable<KVP<K,V>> etc.) exist only to probe constructor parameters; this whole tier is gated by RequiresDynamicCode at acquisition")]
    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        constructorArguments = null;
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            typeArguments = [elementType];
            if (elementType.IsEnum && type.GetArrayRank() == 1)
            {
                return typeof(EnumArrayFormatterFactory<>); // the underlying-integer codec loop, see EnumCollectionFormatters.cs
            }
            return type.GetArrayRank() switch
            {
                1 => typeof(ArrayFormatterFactory<>), // jagged arrays recurse here naturally (element = inner array)
                2 => typeof(TwoDimensionalArrayFormatterFactory<>),
                3 => typeof(ThreeDimensionalArrayFormatterFactory<>),
                4 => typeof(FourDimensionalArrayFormatterFactory<>),
                _ => null, // rank > 4 unsupported (v3-parity)
            };
        }
        if (type.IsEnum)
        {
            typeArguments = [type];
            return typeof(EnumFormatterFactory<>);
        }
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            typeArguments = type.GetGenericArguments();

            // common high-priority types
            if (definition == typeof(Nullable<>))
            {
                return typeof(NullableFormatterFactory<>);
            }
#if NET11_0_OR_GREATER
            if (definition == typeof(System.Numerics.Complex<>))
            {
                return typeof(ComplexFormatterFactory<>);
            }
#endif
            if (definition == typeof(List<>))
            {
#if NET9_0_OR_GREATER
                if (typeArguments[0].IsEnum)
                {
                    return typeof(EnumListFormatterFactory<>); // the underlying-integer codec loop, see EnumCollectionFormatters.cs
                }
#endif
                return typeof(ListFormatterFactory<>);
            }
            if (definition == typeof(Dictionary<,>))
            {
                return typeof(DictionaryFormatterFactory<,>);
            }

            // tuples and generic wrappers
            if (definition == typeof(ValueTuple<>))
            {
                return typeof(ValueTupleFormatterFactory<>);
            }
            if (definition == typeof(ValueTuple<,>))
            {
                return typeof(ValueTupleFormatterFactory<,>);
            }
            if (definition == typeof(ValueTuple<,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,>);
            }
            if (definition == typeof(ValueTuple<,,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,,>);
            }
            if (definition == typeof(ValueTuple<,,,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,,,>);
            }
            if (definition == typeof(ValueTuple<,,,,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,,,,>);
            }
            if (definition == typeof(ValueTuple<,,,,,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,,,,,>);
            }
            if (definition == typeof(ValueTuple<,,,,,,,>))
            {
                return typeof(ValueTupleFormatterFactory<,,,,,,,>);
            }
            if (definition == typeof(Tuple<>))
            {
                return typeof(TupleFormatterFactory<>);
            }
            if (definition == typeof(Tuple<,>))
            {
                return typeof(TupleFormatterFactory<,>);
            }
            if (definition == typeof(Tuple<,,>))
            {
                return typeof(TupleFormatterFactory<,,>);
            }
            if (definition == typeof(Tuple<,,,>))
            {
                return typeof(TupleFormatterFactory<,,,>);
            }
            if (definition == typeof(Tuple<,,,,>))
            {
                return typeof(TupleFormatterFactory<,,,,>);
            }
            if (definition == typeof(Tuple<,,,,,>))
            {
                return typeof(TupleFormatterFactory<,,,,,>);
            }
            if (definition == typeof(Tuple<,,,,,,>))
            {
                return typeof(TupleFormatterFactory<,,,,,,>);
            }
            if (definition == typeof(Tuple<,,,,,,,>))
            {
                return typeof(TupleFormatterFactory<,,,,,,,>);
            }
            if (definition == typeof(KeyValuePair<,>))
            {
                return typeof(KeyValuePairFormatterFactory<,>);
            }
            if (definition == typeof(Lazy<>))
            {
                return typeof(LazyFormatterFactory<>);
            }

            // sequence collections (CollectionFormatters.cs)
            if (definition == typeof(LinkedList<>))
            {
                return typeof(LinkedListFormatterFactory<>);
            }
            if (definition == typeof(Queue<>))
            {
                return typeof(QueueFormatterFactory<>);
            }
            if (definition == typeof(Stack<>))
            {
                return typeof(StackFormatterFactory<>);
            }
            if (definition == typeof(HashSet<>))
            {
                return typeof(HashSetFormatterFactory<>);
            }
            if (definition == typeof(SortedSet<>))
            {
                return typeof(SortedSetFormatterFactory<>);
            }
#if NET9_0_OR_GREATER
            if (definition == typeof(System.Collections.ObjectModel.ReadOnlySet<>))
            {
                return typeof(ReadOnlySetFormatterFactory<>);
            }
#endif
            if (definition == typeof(System.Collections.ObjectModel.ReadOnlyCollection<>))
            {
                return typeof(ReadOnlyCollectionFormatterFactory<>);
            }
            if (definition == typeof(System.Collections.ObjectModel.ObservableCollection<>))
            {
                return typeof(ObservableCollectionFormatterFactory<>);
            }
            if (definition == typeof(System.Collections.ObjectModel.ReadOnlyObservableCollection<>))
            {
                return typeof(ReadOnlyObservableCollectionFormatterFactory<>);
            }
            if (definition == typeof(System.Collections.Concurrent.ConcurrentQueue<>))
            {
                return typeof(ConcurrentQueueFormatterFactory<>);
            }
            if (definition == typeof(System.Collections.Concurrent.ConcurrentStack<>))
            {
                return typeof(ConcurrentStackFormatterFactory<>);
            }
            if (definition == typeof(System.Collections.Concurrent.ConcurrentBag<>))
            {
                return typeof(ConcurrentBagFormatterFactory<>);
            }
            if (definition == typeof(ArraySegment<>))
            {
                return typeof(ArraySegmentFormatterFactory<>);
            }
            if (definition == typeof(Memory<>))
            {
                return typeof(MemoryFormatterFactory<>);
            }
            if (definition == typeof(ReadOnlyMemory<>))
            {
                return typeof(ReadOnlyMemoryFormatterFactory<>);
            }
            if (definition == typeof(System.Buffers.ReadOnlySequence<>))
            {
                return typeof(ReadOnlySequenceFormatterFactory<>);
            }
            if (definition == typeof(IEnumerable<>))
            {
                return typeof(InterfaceEnumerableFormatterFactory<>);
            }
            if (definition == typeof(ICollection<>))
            {
                return typeof(InterfaceCollectionFormatterFactory<>);
            }
            if (definition == typeof(IList<>))
            {
                return typeof(InterfaceListFormatterFactory<>);
            }
            if (definition == typeof(IReadOnlyCollection<>))
            {
                return typeof(InterfaceReadOnlyCollectionFormatterFactory<>);
            }
            if (definition == typeof(IReadOnlyList<>))
            {
                return typeof(InterfaceReadOnlyListFormatterFactory<>);
            }
            if (definition == typeof(ISet<>))
            {
                return typeof(InterfaceSetFormatterFactory<>);
            }
#if NET9_0_OR_GREATER
            if (definition == typeof(IReadOnlySet<>))
            {
                return typeof(InterfaceReadOnlySetFormatterFactory<>);
            }
#endif

            // TKey/TValue family (DictionaryFormatters.cs)

            if (definition == typeof(IDictionary<,>))
            {
                return typeof(InterfaceDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(IReadOnlyDictionary<,>))
            {
                return typeof(InterfaceReadOnlyDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>))
            {
                return typeof(ReadOnlyDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(SortedList<,>))
            {
                return typeof(SortedListFormatterFactory<,>);
            }
            if (definition == typeof(SortedDictionary<,>))
            {
                return typeof(SortedDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(ConcurrentDictionary<,>))
            {
                return typeof(ConcurrentDictionaryFormatterFactory<,>);
            }
#if NET9_0_OR_GREATER
            if (definition == typeof(OrderedDictionary<,>))
            {
                return typeof(OrderedDictionaryFormatterFactory<,>);
            }
#endif
#if NET9_0_OR_GREATER
            if (definition == typeof(PriorityQueue<,>))
            {
                return typeof(PriorityQueueFormatterFactory<,>);
            }
#endif

            // ILookup/IGrouping (LookupFormatters.cs)
            if (definition == typeof(IGrouping<,>))
            {
                return typeof(InterfaceGroupingFormatterFactory<,>);
            }
            if (definition == typeof(ILookup<,>))
            {
                return typeof(InterfaceLookupFormatterFactory<,>);
            }

            // System.Collections.Immutable (ImmutableCollectionFormatters.cs)
            if (definition == typeof(ImmutableArray<>))
            {
                return typeof(ImmutableArrayFormatterFactory<>);
            }
            if (definition == typeof(ImmutableList<>))
            {
                return typeof(ImmutableListFormatterFactory<>);
            }
            if (definition == typeof(ImmutableHashSet<>))
            {
                return typeof(ImmutableHashSetFormatterFactory<>);
            }
            if (definition == typeof(ImmutableSortedSet<>))
            {
                return typeof(ImmutableSortedSetFormatterFactory<>);
            }
            if (definition == typeof(ImmutableQueue<>))
            {
                return typeof(ImmutableQueueFormatterFactory<>);
            }
            if (definition == typeof(ImmutableStack<>))
            {
                return typeof(ImmutableStackFormatterFactory<>);
            }
            if (definition == typeof(ImmutableDictionary<,>))
            {
                return typeof(ImmutableDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(ImmutableSortedDictionary<,>))
            {
                return typeof(ImmutableSortedDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(IImmutableList<>))
            {
                return typeof(InterfaceImmutableListFormatterFactory<>);
            }
            if (definition == typeof(IImmutableSet<>))
            {
                return typeof(InterfaceImmutableSetFormatterFactory<>);
            }
            if (definition == typeof(IImmutableQueue<>))
            {
                return typeof(InterfaceImmutableQueueFormatterFactory<>);
            }
            if (definition == typeof(IImmutableStack<>))
            {
                return typeof(InterfaceImmutableStackFormatterFactory<>);
            }
            if (definition == typeof(IImmutableDictionary<,>))
            {
                return typeof(InterfaceImmutableDictionaryFormatterFactory<,>);
            }
            // System.Collections.Frozen (FrozenCollectionFormatters.cs)
            if (definition == typeof(FrozenDictionary<,>))
            {
                return typeof(FrozenDictionaryFormatterFactory<,>);
            }
            if (definition == typeof(FrozenSet<>))
            {
                return typeof(FrozenSetFormatterFactory<>);
            }
        }

        // Catch-alls for concrete collection shapes (v3 DynamicGenericResolver's inherited-type rules). A public default
        // ctor unlocks the Add-based formatters, and a public single-parameter collection-accepting ctor unlocks the
        // construct-from-intermediate formatters (which also serve struct collections). ExpandoObject structurally
        // matches the IDictionary<,> branch, but serving it would silently revive the deprecated quadratic-Add path,
        // so only the opt-in ExpandoObjectFormatterFactory serves it and the miss stays loud.
        if (type != typeof(System.Dynamic.ExpandoObject) && !type.IsAbstract)
        {
            var hasDefaultConstructor = !type.IsValueType && type.GetConstructor(Type.EmptyTypes) != null;
            Type? collectionInterface = null;
            Type? readOnlyDictionaryInterface = null;
            List<Type>? enumerableInterfaces = null;
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }
                var interfaceDefinition = iface.GetGenericTypeDefinition();
                if (hasDefaultConstructor && interfaceDefinition == typeof(IDictionary<,>))
                {
                    var args = iface.GetGenericArguments();
                    typeArguments = [args[0], args[1], type];
                    return typeof(GenericDictionaryFormatterFactory<,,>);
                }
                if (collectionInterface == null && interfaceDefinition == typeof(ICollection<>))
                {
                    collectionInterface = iface; // keep scanning: a dictionary interface may still follow
                }
                if (readOnlyDictionaryInterface == null && interfaceDefinition == typeof(IReadOnlyDictionary<,>))
                {
                    readOnlyDictionaryInterface = iface;
                }
                if (interfaceDefinition == typeof(IEnumerable<>))
                {
                    (enumerableInterfaces ??= []).Add(iface);
                }
            }

            // v3 priority: IDictionary+new (above), then IReadOnlyDictionary+ctor, ICollection+new, non-generic views+new,
            // and IEnumerable<T>+ctor last, so it only catches shapes nothing else claims
            if (readOnlyDictionaryInterface != null)
            {
                var args = readOnlyDictionaryInterface.GetGenericArguments();
                Type kvpEnumerable = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(args));
                if (HasCollectionAcceptingConstructor(type, [typeof(IDictionary<,>).MakeGenericType(args), readOnlyDictionaryInterface, kvpEnumerable]))
                {
                    typeArguments = [args[0], args[1], type];
                    return typeof(GenericReadOnlyDictionaryFormatterFactory<,,>);
                }
            }
            if (hasDefaultConstructor && collectionInterface != null)
            {
                typeArguments = [collectionInterface.GetGenericArguments()[0], type];
                return typeof(GenericCollectionFormatterFactory<,>);
            }

            if (hasDefaultConstructor)
            {
                // non-generic object-element views (ArrayList, Hashtable, ...)
                if (typeof(System.Collections.IList).IsAssignableFrom(type))
                {
                    typeArguments = [type];
                    return typeof(NonGenericListFormatterFactory<>);
                }
                if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
                {
                    typeArguments = [type];
                    return typeof(NonGenericDictionaryFormatterFactory<>);
                }
            }

            if (enumerableInterfaces != null)
            {
                foreach (var iface in enumerableInterfaces)
                {
                    if (HasCollectionAcceptingConstructor(type, [iface]))
                    {
                        typeArguments = [iface.GetGenericArguments()[0], type];
                        return typeof(GenericEnumerableFormatterFactory<,>);
                    }
                }
            }
        }

        typeArguments = Type.EmptyTypes;
        return null;
    }

    // A public single-parameter constructor whose parameter accepts one of the given collection views. Public only,
    // because the formatters construct through Activator's public binder, so a non-public match would just fail later.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "a claim check for the RequiresDynamicCode-gated tier; the formatters' DynamicallyAccessedMembers-annotated type parameters root the constructors wherever the formatter is actually constructed")]
    static bool HasCollectionAcceptingConstructor(Type type, Type[] acceptableArguments)
    {
        foreach (var constructor in type.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
            {
                continue;
            }
            foreach (var argument in acceptableArguments)
            {
                if (parameters[0].ParameterType.IsAssignableFrom(argument))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
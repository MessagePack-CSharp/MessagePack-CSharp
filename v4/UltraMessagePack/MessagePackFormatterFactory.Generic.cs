using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using UltraMessagePack.Formatters;

namespace UltraMessagePack;

public sealed class GenericFormatterFactory : GenericFormatterFactoryBase
{
    static GenericFormatterFactory? instance;

    public static GenericFormatterFactory Instance
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        get => instance ??= new GenericFormatterFactory(); // benign race: stateless singleton
    }

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    GenericFormatterFactory()
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "the default-ctor probe only refines routing for the non-generic IList/IDictionary catch-all; this whole tier is gated by RequiresDynamicCode at acquisition, and a trimmed-away ctor just means the type falls through to the missing-formatter path")]
    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        constructorArguments = null;
        if (type.IsArray)
        {
            typeArguments = [type.GetElementType()!];
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
            if (definition == typeof(List<>))
            {
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
#if NET
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
#if NET
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
#if NET
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

        // catch-alls for anything with a default ctor
        if (!type.IsAbstract && !type.IsValueType && type.GetConstructor(Type.EmptyTypes) != null)
        {
            Type? collectionInterface = null;
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }
                var interfaceDefinition = iface.GetGenericTypeDefinition();
                if (interfaceDefinition == typeof(IDictionary<,>))
                {
                    var args = iface.GetGenericArguments();
                    typeArguments = [args[0], args[1], type];
                    return typeof(GenericDictionaryFormatterFactory<,,>);
                }
                if (collectionInterface == null && interfaceDefinition == typeof(ICollection<>))
                {
                    collectionInterface = iface; // keep scanning: a dictionary interface may still follow
                }
            }
            if (collectionInterface != null)
            {
                typeArguments = [collectionInterface.GetGenericArguments()[0], type];
                return typeof(GenericCollectionFormatterFactory<,>);
            }

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

        typeArguments = Type.EmptyTypes;
        return null;
    }
}
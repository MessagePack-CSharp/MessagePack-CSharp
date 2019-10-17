using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Tests
{
    public static class Loader
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            // adhoc resolver registration to running test.
#if ENABLE_IL2CPP

            StaticCompositeResolver.Register(new IMessagePackFormatter[]{
                new ListFormatter<int>(),
                new LinkedListFormatter<int>(),
                new QueueFormatter<int>(),
                new HashSetFormatter<int>(),
                new ReadOnlyCollectionFormatter<int>(),
                new ObservableCollectionFormatter<int>(),
                new ReadOnlyObservableCollectionFormatter<int>(),
                new InterfaceListFormatter<int>(),
                new InterfaceCollectionFormatter<int>(),
                new InterfaceReadOnlyListFormatter<int>(),
                new InterfaceReadOnlyCollectionFormatter<int>(),
                new InterfaceSetFormatter<int>(),
                new InterfaceLookupFormatter<bool, int>(),
                new StackFormatter<int>(),
                new ConcurrentQueueFormatter<int>(),
                new ConcurrentStackFormatter<int>(),
                new ConcurrentBagFormatter<int>(),
                new ArraySegmentFormatter<int>(),
                new NullableFormatter<ArraySegment<int>>(),
                new InterfaceEnumerableFormatter<int>(),
                new InterfaceGroupingFormatter<bool, int>(),
                new DictionaryFormatter<int,int>(),
                new ReadOnlyDictionaryFormatter<int,int>(),
                new SortedListFormatter<int,int>(),
                new SortedDictionaryFormatter<int,int>(),
                new InterfaceDictionaryFormatter<int,int>(),
                new ConcurrentDictionaryFormatter<int,int>(),
                new InterfaceReadOnlyDictionaryFormatter<int,int>(),

                new GenericEnumFormatter<ByteEnum>(),
                new GenericEnumFormatter<SByteEnum>(),
                new GenericEnumFormatter<ShortEnum>(),
                new GenericEnumFormatter<UShortEnum>(),
                new GenericEnumFormatter<IntEnum>(),
                new GenericEnumFormatter<UIntEnum>(),
                new GenericEnumFormatter<LongEnum>(),
                new GenericEnumFormatter<ULongEnum>(),

                new NullableFormatter<ByteEnum>(),
                new NullableFormatter<SByteEnum>(),
                new NullableFormatter<ShortEnum>(),
                new NullableFormatter<UShortEnum>(),
                new NullableFormatter<IntEnum>(),
                new NullableFormatter<UIntEnum>(),
                new NullableFormatter<LongEnum>(),
                new NullableFormatter<ULongEnum>(),

                new LazyFormatter<int>(),

                new KeyValuePairFormatter<int,string>(),
                new NullableFormatter<KeyValuePair<int,string>>(),

                new ArrayFormatter<FirstSimpleData>(),

                new ValueTupleFormatter<int,int>(),
                new ValueTupleFormatter<int,int,int>(),
                new ValueTupleFormatter<int,int,int,int>(),
                new ArrayFormatter<ValueTuple<int,int>>(),
                new ArrayFormatter<ValueTuple<int,int,int>>(),
                new ArrayFormatter<ValueTuple<int,int,int,int>>(),
                new TwoDimensionalArrayFormatter<int>(),
                new ThreeDimensionalArrayFormatter<int>(),
                new FourDimensionalArrayFormatter<int>(),
                new TwoDimensionalArrayFormatter<(int,int)>(),
                new ThreeDimensionalArrayFormatter<(int,int,int)>(),
                new FourDimensionalArrayFormatter<(int,int,int,int)>(),

                NonGenericInterfaceListFormatter.Instance,
                NonGenericInterfaceDictionaryFormatter.Instance,

                new NullableFormatter<Vector2>(),

                new ArrayFormatter<ComplexdUnion.A>(),
                new ArrayFormatter<ComplexdUnion.A2>(),


        },
            new IFormatterResolver[]{
                MessagePack.Resolvers.GeneratedResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            });

            var resolver = StaticCompositeResolver.Instance;

            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);

#endif
        }
    }
}

using MessagePack;
using MessagePack.Formatters;
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
            //#if ENABLE_IL2CPP

            //var resolver = MessagePack.Resolvers.CompositeResolver.Create(new IMessagePackFormatter[]
            //{
            //    new GenericEnumFormatter<UShortEnum>(),
            //    new GenericEnumFormatter<IntEnum>(),
            //    new NullableFormatter<Vector2>(),
            //    // new Genericli
            //},
            //new[]{
            //    MessagePack.Resolvers.GeneratedResolver.Instance,
            //    MessagePack.Resolvers.StandardResolver.Instance
            //});

            StaticCompositeResolver.Initialize(new IMessagePackFormatter[]{
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
                new NullableFormatter<KeyValuePair<int,string>>()
            },
            new IFormatterResolver[]{
                MessagePack.Resolvers.GeneratedResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            });

            var resolver = StaticCompositeResolver.Instance;

            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            //#endif
        }
    }

    public class StaticCompositeResolver : IFormatterResolver
    {
        public static readonly StaticCompositeResolver Instance = new StaticCompositeResolver();

        IReadOnlyList<IMessagePackFormatter> formatters;
        IReadOnlyList<IFormatterResolver> resolvers;

        StaticCompositeResolver()
        {
            formatters = Array.Empty<IMessagePackFormatter>();
            resolvers = Array.Empty<IFormatterResolver>();
        }

        public static void Initialize(params IMessagePackFormatter[] formatters)
        {
            Instance.formatters = formatters;
        }

        public static void Initialize(params IFormatterResolver[] resolvers)
        {
            Instance.resolvers = resolvers;
        }

        public static void Initialize(IReadOnlyList<IMessagePackFormatter> formatters, IReadOnlyList<IFormatterResolver> resolvers)
        {
            Instance.formatters = formatters;
            Instance.resolvers = resolvers;
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static Cache()
            {
                foreach (var item in Instance.formatters)
                {
                    if (item is IMessagePackFormatter<T> f)
                    {
                        Formatter = f;
                        return;
                    }
                }

                foreach (var item in Instance.resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}

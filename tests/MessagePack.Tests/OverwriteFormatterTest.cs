using MessagePack.Formatters;
using Xunit;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.Tests
{
    public class AddResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(IResolverConfiguration))
            {
                return (IMessagePackFormatter<T>)(object)new MessagePackConfiguration(CollectionDeserializeToBehaviour.Add);
            }
            else
            {
                return StandardResolver.Instance.GetFormatter<T>();
            }
        }
    }

    public class OverwriteResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(IResolverConfiguration))
            {
                return (IMessagePackFormatter<T>)(object)new MessagePackConfiguration(CollectionDeserializeToBehaviour.OverwriteReplace);
            }
            else
            {
                return StandardResolver.Instance.GetFormatter<T>();
            }
        }
    }

    public class CollectionOverwriteFormatterTest
    {
        // test patterns

        // AddResolver/OverwriteResolver

        // - to is null
        // - bin is null
        // - bin is zero
        // - bin is lower than to
        // - bin is equal to to
        // - bin is higher than to

        void DeserializeTo<T>(IMessagePackFormatter<T> formatter, IFormatterResolver resolver, ref T to, T value)
        {
            formatter.DeserializeTo(ref to, MessagePackSerializer.Serialize(value), 0, resolver, out var _);
        }

        [Fact]
        public void ArrayFormatter()
        {
            var formatter = new ArrayFormatter<int>();

            {
                var resolver = new AddResolver();

                int[] to = null;
                DeserializeTo(formatter, resolver, ref to, new[] { 4, 5, 6 });
                to.Is(4, 5, 6);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, (int[])null);
                to.Is(1, 2, 3);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[0]);
                to.Is(1, 2, 3);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5 });
                to.Is(1, 2, 3, 4, 5);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5, 6 });
                to.Is(1, 2, 3, 4, 5, 6);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5, 6, 7 });
                to.Is(1, 2, 3, 4, 5, 6, 7);
            }
            {
                var resolver = new OverwriteResolver();

                int[] to = null;
                DeserializeTo(formatter, resolver, ref to, new[] { 4, 5, 6 });
                to.Is(4, 5, 6);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, (int[])null);
                to.IsNull();

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[0]);
                to.IsZero();

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5 });
                to.Is(4, 5);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5, 6 });
                to.Is(4, 5, 6);

                to = new[] { 1, 2, 3 };
                DeserializeTo(formatter, resolver, ref to, new int[] { 4, 5, 6, 7 });
                to.Is(4, 5, 6, 7);
            }

            // TODO:T is overwritable test
        }
    }
}

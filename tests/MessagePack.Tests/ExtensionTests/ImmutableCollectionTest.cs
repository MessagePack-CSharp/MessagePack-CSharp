using MessagePack.ImmutableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.Collections.Immutable;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class WithImmutableDefaultResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (ImmutableCollectionResolver.Instance.GetFormatter<T>()
                 ?? DefaultResolver.Instance.GetFormatter<T>());
        }
    }

    public class ImmutableCollectionTest
    {
        T Convert<T>(T value)
        {
            var resolver = new WithImmutableDefaultResolver();
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, resolver), resolver);
        }

        public static object collectionTestData = new object[]
        {
            new object[]{ ImmutableList<int>.Empty.AddRange(new[] { 1, 10, 100 }) , null },
            new object[]{ ImmutableDictionary<int,int>.Empty.AddRange(new Dictionary<int,int> { { 1, 10 },{ 2, 10 }, { 3, 100 } }) , null },
            new object[]{ ImmutableHashSet<int>.Empty.Add(1).Add(10).Add(100) , null },
            new object[]{ ImmutableSortedDictionary<int,int>.Empty.AddRange(new Dictionary<int,int> { { 1, 10 },{ 2, 10 }, { 3, 100 } }) , null },
            new object[]{ ImmutableSortedSet<int>.Empty.Add(1).Add(10).Add(100) , null },
            new object[]{ ImmutableQueue<int>.Empty.Enqueue(1).Enqueue(10).Enqueue(100) , null },
            new object[]{ ImmutableStack<int>.Empty.Push(1).Push(10).Push(100) , null },
        };

        [Theory]
        [MemberData(nameof(collectionTestData))]
        public void ConcreteCollectionTest<T>(T x, T y)
        {
            Convert(x).IsStructuralEqual(x);
            Convert(y).IsStructuralEqual(y);
        }



        [Fact]
        public void InterfaceCollectionTest()
        {
            IImmutableList<int> a = ImmutableList<int>.Empty.AddRange(new[] { 1, 10, 100 });
            IImmutableDictionary<int, int> b = ImmutableDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } });
            IImmutableSet<int> c = ImmutableHashSet<int>.Empty.Add(1).Add(10).Add(100);
            IImmutableQueue<int> d = ImmutableQueue<int>.Empty.Enqueue(1).Enqueue(10).Enqueue(100);
            IImmutableStack<int> e = ImmutableStack<int>.Empty.Push(1).Push(10).Push(100);

            Convert(a).IsStructuralEqual(a);
            Convert(b).IsStructuralEqual(b);
            Convert(c).IsStructuralEqual(c);
            Convert(d).IsStructuralEqual(d);
            Convert(e).IsStructuralEqual(e);

            a = null;
            b = null;
            c = null;
            d = null;
            e = null;
            Convert(a).IsNull();
            Convert(b).IsNull();
            Convert(c).IsNull();
            Convert(d).IsNull();
            Convert(e).IsNull();
        }

        [Fact]
        public void ImmutableArrayTest()
        {
            var a = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            ImmutableArray<int>? b = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            ImmutableArray<int>? c = null;

            Convert(a).Is(1, 10, 100);
            Convert(b).Is(1, 10, 100);
            Convert(c).IsNull();
        }
    }
}

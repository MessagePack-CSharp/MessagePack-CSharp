// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.ImmutableCollection;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class ImmutableCollectionTest
    {
        private T Convert<T>(T value)
        {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(new WithImmutableDefaultResolver());
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        public static object[][] CollectionTestData = new object[][]
        {
            new object[] { ImmutableList<int>.Empty.AddRange(new[] { 1, 10, 100 }), null },
            new object[] { ImmutableDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }), null },
            new object[] { ImmutableHashSet<int>.Empty.Add(1).Add(10).Add(100), null },
            new object[] { ImmutableSortedDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }), null },
            new object[] { ImmutableSortedSet<int>.Empty.Add(1).Add(10).Add(100), null },
            new object[] { ImmutableQueue<int>.Empty.Enqueue(1).Enqueue(10).Enqueue(100), null },
            new object[] { ImmutableStack<int>.Empty.Push(1).Push(10).Push(100), null },
        };

        [Theory]
        [MemberData(nameof(CollectionTestData))]
        public void ConcreteCollectionTest<T>(T x, T y)
        {
            this.Convert(x).IsStructuralEqual(x);
            this.Convert(y).IsStructuralEqual(y);
        }

        [Fact]
        public void InterfaceCollectionTest()
        {
            IImmutableList<int> a = ImmutableList<int>.Empty.AddRange(new[] { 1, 10, 100 });
            IImmutableDictionary<int, int> b = ImmutableDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } });
            IImmutableSet<int> c = ImmutableHashSet<int>.Empty.Add(1).Add(10).Add(100);
            IImmutableQueue<int> d = ImmutableQueue<int>.Empty.Enqueue(1).Enqueue(10).Enqueue(100);
            IImmutableStack<int> e = ImmutableStack<int>.Empty.Push(1).Push(10).Push(100);

            this.Convert(a).IsStructuralEqual(a);
            this.Convert(b).IsStructuralEqual(b);
            this.Convert(c).IsStructuralEqual(c);
            this.Convert(d).IsStructuralEqual(d);
            this.Convert(e).IsStructuralEqual(e);

            a = null;
            b = null;
            c = null;
            d = null;
            e = null;
            this.Convert(a).IsNull();
            this.Convert(b).IsNull();
            this.Convert(c).IsNull();
            this.Convert(d).IsNull();
            this.Convert(e).IsNull();
        }

        [Fact]
        public void ImmutableArrayTest()
        {
            var a = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            ImmutableArray<int>? b = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            ImmutableArray<int>? c = null;

            this.Convert(a).Is(1, 10, 100);
            this.Convert(b).Is(1, 10, 100);
            this.Convert(c).IsNull();
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class ImmutableCollectionTest
    {
        private T Convert<T>(T value)
        {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard;
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        public static object[][] CollectionTestData = new object[][]
        {
            new object[] { true, ImmutableList<int>.Empty.AddRange(new[] { 1, 10, 100 }), null },
            new object[] { false, ImmutableDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }), null },
            new object[] { false, ImmutableHashSet<int>.Empty.Add(1).Add(10).Add(100), null },
            new object[] { true, ImmutableSortedDictionary<int, int>.Empty.AddRange(new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }), null },
            new object[] { true, ImmutableSortedSet<int>.Empty.Add(1).Add(10).Add(100), null },
            new object[] { true, ImmutableQueue<int>.Empty.Enqueue(1).Enqueue(10).Enqueue(100), null },
            new object[] { true, ImmutableStack<int>.Empty.Push(1).Push(10).Push(100), null },
        };

        [Theory]
        [MemberData(nameof(CollectionTestData))]
        public void ConcreteCollectionTest<T>(bool ordered, T x, T y)
        {
            if (ordered)
            {
                this.Convert(x).IsStructuralEqual(x);
            }
            else
            {
                this.Convert(x).IsStructuralEqualIgnoreCollectionOrder(x);
            }

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
            this.Convert(b).IsStructuralEqualIgnoreCollectionOrder(b);
            this.Convert(c).IsStructuralEqualIgnoreCollectionOrder(c);
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
        public void ImmutableArray_WithContent()
        {
            ImmutableArray<int> populated = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            this.Convert(populated).Is(1, 10, 100);
        }

        [Fact]
        public void ImmutableArray_Nullable_WithContent()
        {
            ImmutableArray<int>? populatedNullable = ImmutableArray.CreateRange(new[] { 1, 10, 100 });
            this.Convert(populatedNullable).Is(1, 10, 100);
        }

        [Fact]
        public void ImmutableArray_Nullable_Null()
        {
            ImmutableArray<int>? nullNullable = null;
            Assert.Null(this.Convert(nullNullable));
        }

        [Fact]
        public void ImmutableArray_Empty()
        {
            ImmutableArray<int> defaultArray = ImmutableArray<int>.Empty;
            Assert.True(this.Convert(defaultArray).IsEmpty);
        }

        [Fact]
        public void ImmutableArray_Default()
        {
            ImmutableArray<int> defaultArray = default;
            Assert.True(this.Convert(defaultArray).IsDefault);
        }
    }
}

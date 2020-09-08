// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class CollectionTest
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[][] CollectionTestData = new object[][]
        {
            new object[] { new int[] { 1, 10, 100 }, null },
            new object[] { new List<int> { 1, 10, 100 }, null },
            new object[] { new LinkedList<int>(new[] { 1, 10, 100 }), null },
            new object[] { new Queue<int>(new[] { 1, 10, 100 }), null },
            new object[] { new HashSet<int>(new[] { 1, 10, 100 }), null },
            new object[] { new ReadOnlyCollection<int>(new[] { 1, 10, 100 }), null },
            new object[] { new ObservableCollection<int>(new[] { 1, 10, 100 }), null },
            new object[] { new ReadOnlyObservableCollection<int>(new ObservableCollection<int>(new[] { 1, 10, 100 })), null },
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
            var a = (IList<int>)new int[] { 1, 10, 100 };
            var b = (ICollection<int>)new int[] { 1, 10, 100 };
            IEnumerable<int> c = Enumerable.Range(1, 100).AsEnumerable();
            var d = (IReadOnlyList<int>)new int[] { 1, 10, 100 };
            var e = (IReadOnlyCollection<int>)new int[] { 1, 10, 100 };
            var f = (ISet<int>)new HashSet<int>(new[] { 1, 10, 100 });
            var g = (ILookup<bool, int>)Enumerable.Range(1, 100).ToLookup(x => x % 2 == 0);

            this.Convert(a).Is(a);
            this.Convert(b).Is(b);
            this.Convert(c).Is(c);
            this.Convert(d).Is(d);
            this.Convert(e).Is(e);
            this.Convert(f).Is(f);
            this.Convert(g).Is(g);

            a = null;
            b = null;
            c = null;
            d = null;
            e = null;
            f = null;
            g = null;

            this.Convert(a).Is(a);
            this.Convert(b).Is(b);
            this.Convert(c).IsNull();
            this.Convert(d).Is(d);
            this.Convert(e).Is(e);
            this.Convert(f).Is(f);
            this.Convert(g).Is(g);
        }

        [Fact]
        public void InterfaceCollectionsAreDeserializedMutable()
        {
            var list = this.Convert<IList<int>>(new[] { 1, 2, 3 });
            list.Add(4);
            Assert.Equal(new[] { 1, 2, 3, 4 }, list);

            var collection = this.Convert<ICollection<int>>(new[] { 1, 2, 3 });
            collection.Add(4);
            Assert.Equal(new[] { 1, 2, 3, 4 }, collection);

            var setCollection = this.Convert<ISet<int>>(new HashSet<int> { 1, 2, 3 });
            setCollection.Add(4);
            Assert.Equal(new[] { 1, 2, 3, 4 }, setCollection.OrderBy(n => n).ToArray());
        }

        [Fact]
        public void StackTest()
        {
            var stack = new Stack<int>(new[] { 1, 10, 100 });
            stack.AsEnumerable().Is(100, 10, 1);
            this.Convert(stack).AsEnumerable().Is(100, 10, 1);

            stack = new Stack<int>();
            this.Convert(stack).AsEnumerable().Count().Is(0);

            stack = null;
            this.Convert(stack).IsNull();
        }

        [Fact]
        public void ConcurrentCollectionTest()
        {
            var c0 = new ConcurrentQueue<int>(new[] { 1, 10, 100 });
            var c1 = new ConcurrentStack<int>(new[] { 1, 10, 100 });
            var c2 = new ConcurrentBag<int>(new[] { 1, 10, 100 });

            this.Convert(c0).Is(1, 10, 100);
            this.Convert(c1).Is(100, 10, 1);

            this.Convert(c2).OrderBy(x => x).Is(1, 10, 100);

            c0 = null;
            c1 = null;
            c2 = null;

            this.Convert(c0).IsNull();
            this.Convert(c1).IsNull();
            this.Convert(c2).IsNull();
        }

        [Fact]
        public void ArraySegmentTest()
        {
            var test = new ArraySegment<int>(new[] { 1, 10, 100 });
            this.Convert(test).Is(1, 10, 100);
            ArraySegment<int>? nullableTest = new ArraySegment<int>(new[] { 1, 10, 100 });
            this.Convert(nullableTest).Is(1, 10, 100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void MemoryTest()
        {
            var test = new Memory<int>(new[] { 1, 10, 100 });
            this.Convert(test).ToArray().Is(1, 10, 100);
            Memory<int>? nullableTest = new Memory<int>(new[] { 1, 10, 100 });
            this.Convert(nullableTest).Value.ToArray().Is(1, 10, 100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void MemoryOfByteTest()
        {
            var test = new Memory<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(test).ToArray().Is((byte)1, (byte)10, (byte)100);
            Memory<byte>? nullableTest = new Memory<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(nullableTest).Value.ToArray().Is((byte)1, (byte)10, (byte)100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void ReadOnlyMemoryTest()
        {
            var test = new ReadOnlyMemory<int>(new[] { 1, 10, 100 });
            this.Convert(test).ToArray().Is(1, 10, 100);
            ReadOnlyMemory<int>? nullableTest = new ReadOnlyMemory<int>(new[] { 1, 10, 100 });
            this.Convert(nullableTest).Value.ToArray().Is(1, 10, 100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void ReadOnlyMemoryOfByteTest()
        {
            var test = new ReadOnlyMemory<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(test).ToArray().Is((byte)1, (byte)10, (byte)100);
            ReadOnlyMemory<byte>? nullableTest = new ReadOnlyMemory<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(nullableTest).Value.ToArray().Is((byte)1, (byte)10, (byte)100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void ReadOnlySequenceTest()
        {
            var test = new ReadOnlySequence<int>(new[] { 1, 10, 100 });
            this.Convert(test).ToArray().Is(1, 10, 100);
            ReadOnlySequence<int>? nullableTest = new ReadOnlySequence<int>(new[] { 1, 10, 100 });
            this.Convert(nullableTest).Value.ToArray().Is(1, 10, 100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }

        [Fact]
        public void ReadOnlySequenceOfByteTest()
        {
            var test = new ReadOnlySequence<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(test).ToArray().Is((byte)1, (byte)10, (byte)100);
            ReadOnlySequence<byte>? nullableTest = new ReadOnlySequence<byte>(new[] { (byte)1, (byte)10, (byte)100 });
            this.Convert(nullableTest).Value.ToArray().Is((byte)1, (byte)10, (byte)100);
            nullableTest = null;
            this.Convert(nullableTest).IsNull();
        }
    }
}

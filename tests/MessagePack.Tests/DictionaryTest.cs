﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class DictionaryTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        T Convert<T>(T value)
        {
            return serializer.Deserialize<T>(serializer.Serialize(value));
        }

        public static object[][] dictionaryTestData = new object[][]
        {
            new object[]{ new Dictionary<int, int>() { { 1, 100 } }, null },
            new object[]{ new ReadOnlyDictionary<int,int>(new Dictionary<int, int>() { { 1, 100 } }), null },
            new object[]{ new SortedList<int, int>() { { 1, 100 } }, null },
            new object[]{ new SortedDictionary<int, int>() { { 1, 100 } }, null },
        };

        [Theory]
        [MemberData(nameof(dictionaryTestData))]
        public void DictionaryTestAll<T>(T x, T y)
        {
            Convert(x).IsStructuralEqual(x);
            Convert(y).IsStructuralEqual(y);
        }

        [Fact]
        public void InterfaceDictionaryTest()
        {
            var a = (IDictionary<int, int>)new Dictionary<int, int>() { { 1, 100 } };
            var b = (IReadOnlyDictionary<int, int>)new Dictionary<int, int>() { { 1, 100 } };
            var c = (IDictionary<int, int>)null;
            var d = (IReadOnlyDictionary<int, int>)null;

            Convert(a).IsStructuralEqual(a);
            Convert(b).IsStructuralEqual(b);
            Convert(c).IsStructuralEqual(c);
            Convert(d).IsStructuralEqual(d);
        }

        [Fact]
        public void ConcurrentDictionaryTest()
        {
            var cd = new ConcurrentDictionary<int, int>();

            cd.TryAdd(1, 100);
            cd.TryAdd(2, 200);
            cd.TryAdd(3, 300);

            var conv = Convert(cd);
            conv[1].Is(100);
            conv[2].Is(200);
            conv[3].Is(300);

            cd = null;
            Convert(cd).IsNull();
        }
    }
}

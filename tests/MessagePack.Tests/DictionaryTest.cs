// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[][] DictionaryTestData = new object[][]
        {
            new object[] { new Dictionary<int, int>() { { 1, 100 } }, null },
            new object[] { new ReadOnlyDictionary<int, int>(new Dictionary<int, int>() { { 1, 100 } }), null },
            new object[] { new SortedList<int, int>() { { 1, 100 } }, null },
            new object[] { new SortedDictionary<int, int>() { { 1, 100 } }, null },
        };

        [Theory]
        [MemberData(nameof(DictionaryTestData))]
        public void DictionaryTestAll<T>(T x, T y)
        {
            this.Convert(x).IsStructuralEqual(x);
            this.Convert(y).IsStructuralEqual(y);
        }

        [Fact]
        public void InterfaceDictionaryTest()
        {
            var a = (IDictionary<int, int>)new Dictionary<int, int>() { { 1, 100 } };
            var b = (IReadOnlyDictionary<int, int>)new Dictionary<int, int>() { { 1, 100 } };
            var c = (IDictionary<int, int>)null;
            var d = (IReadOnlyDictionary<int, int>)null;

            this.Convert(a).IsStructuralEqual(a);
            this.Convert(b).IsStructuralEqual(b);
            this.Convert(c).IsStructuralEqual(c);
            this.Convert(d).IsStructuralEqual(d);
        }

        [Fact]
        public void ConcurrentDictionaryTest()
        {
            var cd = new ConcurrentDictionary<int, int>();
            ConcurrentDictionary<int, int> conv = this.Convert(cd);
            conv.Count.Is(0);

            cd.TryAdd(1, 100);
            cd.TryAdd(2, 200);
            cd.TryAdd(3, 300);

            conv = this.Convert(cd);
            conv[1].Is(100);
            conv[2].Is(200);
            conv[3].Is(300);

            cd = null;
            this.Convert(cd).IsNull();
        }
    }
}

using SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class UnionResolverTest
    {
        public static object[] unionData = new object[]
        {
            new object[]{new MySubUnion1 { One = 23 },     new MySubUnion1 { One = 23 }},
            new object[]{new MySubUnion2 { Two = 233 },    new MySubUnion2 { Two = 233 }},
            new object[]{new MySubUnion3 { Three = 253 },  new MySubUnion3 { Three = 253 }},
            new object[]{new MySubUnion4 { Four = 24353 }, new MySubUnion4 { Four = 24353 }},
        };

        [Theory]
        [MemberData(nameof(unionData))]
        public void Hoge<T, U>(T data, U data2)
            where T : IUnionChecker
            where U : IUnionChecker2
        {
            var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data);
            var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2);

            var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);
            var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);

            reData1.IsInstanceOf<T>();
            reData2.IsInstanceOf<U>();

            var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null);
            var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null);

            MessagePackSerializer.Deserialize<IUnionChecker>(null1).IsNull();
            MessagePackSerializer.Deserialize<IUnionChecker2>(null1).IsNull();


            var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
            MessagePackSerializer.Deserialize<IUnionChecker>(hoge).IsNull();
        }
    }
}

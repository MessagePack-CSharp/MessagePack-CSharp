using ComplexdUnion;
using MessagePack;
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
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

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

        [Fact]
        public void ComplexTest()
        {
            var union1 = new A[] { new B() { Name = "b", Val = 2 }, new C() { Name = "t", Val = 5, Valer = 99 } };
            var union2 = new A2[] { new B2() { Name = "b", Val = 2 }, new C2() { Name = "t", Val = 5, Valer = 99 } };

            var convert1 = Convert(union1);
            var convert2 = Convert(union2);

            convert1[0].IsInstanceOf<B>().Is(x => x.Name == "b" && x.Val == 2);
            convert1[1].IsInstanceOf<C>().Is(x => x.Name == "t" && x.Val == 5 && x.Valer == 99);

            convert2[0].IsInstanceOf<B2>().Is(x => x.Name == "b" && x.Val == 2);
            convert2[1].IsInstanceOf<C2>().Is(x => x.Name == "t" && x.Val == 5 && x.Valer == 99);
        }
    }
}

namespace ComplexdUnion
{

    [Union(0, typeof(B))]
    [Union(1, typeof(C))]
    public interface A
    {
        string Type { get; }
        string Name { get; set; }
    }

    [MessagePackObject]
    public class B : A
    {
        [Ignore]
        public string Type { get { return "B"; } }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }
    [MessagePackObject]
    public class C : A
    {
        [Ignore]
        public string Type { get { return "C"; } }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }

        [Key(2)]
        public virtual int Valer { get; set; }
    }



    [Union(0, typeof(B2))]
    [Union(1, typeof(C2))]
    public interface A2
    {
        string Type { get; }
        string Name { get; set; }
    }

    [MessagePackObject]
    public class B2 : A2
    {
        [Ignore]
        public string Type { get { return "B"; } }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }

    [MessagePackObject]
    public class C2 : B2
    {
        [Key(2)]
        public virtual int Valer { get; set; }
    }
}

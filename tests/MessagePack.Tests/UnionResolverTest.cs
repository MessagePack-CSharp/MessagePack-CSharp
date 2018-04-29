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

        public static IEnumerable<object[]> unionData = new List<object[]>
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

        [Fact]
        public void Union2()
        {


            var a = MessagePackSerializer.Serialize<IMessageBody>(new TextMessageBody() { Text = "hoge" });
            var b = MessagePackSerializer.Serialize<IMessageBody>(new StampMessageBody() { StampId = 10 });
            var c = MessagePackSerializer.Serialize<IMessageBody>(new QuestMessageBody() { Text = "hugahuga", QuestId = 99 });

            var a2 = MessagePackSerializer.Deserialize<IMessageBody>(a);
            var b2 = MessagePackSerializer.Deserialize<IMessageBody>(b);
            var c2 = MessagePackSerializer.Deserialize<IMessageBody>(c);

            (a2 as TextMessageBody).Text.Is("hoge");
            (b2 as StampMessageBody).StampId.Is(10);
            (c2 as QuestMessageBody).Is(x => x.Text == "hugahuga" && x.QuestId == 99);
        }

        [Fact]
        public void ClassUnion()
        {
            //var a = new RootUnionType() { MyProperty = 10 };
            var b = new SubUnionType1() { MyProperty = 11, MyProperty1 = 100 };
            var c = new SubUnionType2() { MyProperty = 12, MyProperty2 = 200 };

            // var binA = MessagePackSerializer.Serialize<RootUnionType>(a);
            var binB = MessagePackSerializer.Serialize<RootUnionType>(b);
            var binC = MessagePackSerializer.Serialize<RootUnionType>(c);

            var b2 = MessagePackSerializer.Deserialize<RootUnionType>(binB) as SubUnionType1;
            var c2 = MessagePackSerializer.Deserialize<RootUnionType>(binC) as SubUnionType2;

            b2.MyProperty.Is(11); b2.MyProperty1.Is(100);
            c2.MyProperty.Is(12); c2.MyProperty2.Is(200);
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
        [IgnoreMember]
        public string Type { get { return "B"; } }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }
    [MessagePackObject]
    public class C : A
    {
        [IgnoreMember]
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
        [IgnoreMember]
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


namespace ClassUnion
{
    [Union(0, typeof(SubUnionType1))]
    [Union(1, typeof(SubUnionType2))]
    [MessagePackObject]
    public abstract class RootUnionType
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    [MessagePackObject]
    public class SubUnionType1 : RootUnionType
    {
        [Key(1)]
        public int MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class SubUnionType2 : RootUnionType
    {
        [Key(1)]
        public int MyProperty2 { get; set; }
    }
}
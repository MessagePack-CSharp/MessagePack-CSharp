// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComplexdUnion;
using MessagePack;
using SharedData;
using Xunit;

#pragma warning disable SA1302 // Interface names should begin with I
#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Tests
{
    public class UnionResolverTest
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[][] UnionData = new object[][]
        {
            new object[] { new MySubUnion1 { One = 23 },     new MySubUnion1 { One = 23 } },
            new object[] { new MySubUnion2 { Two = 233 },    new MySubUnion2 { Two = 233 } },
            new object[] { new MySubUnion3 { Three = 253 },  new MySubUnion3 { Three = 253 } },
            new object[] { new MySubUnion4 { Four = 24353 }, new MySubUnion4 { Four = 24353 } },
        };

        public static object[][] UnionDataWithStringSubType = new object[][]
        {
            new object[] { new MySubUnion1WithStringSubType { One = 23 },     new MySubUnion1WithStringSubType { One = 23 } },
            new object[] { new MySubUnion2WithStringSubType { Two = 233 },    new MySubUnion2WithStringSubType { Two = 233 } },
            new object[] { new MySubUnion3WithStringSubType { Three = 253 },  new MySubUnion3WithStringSubType { Three = 253 } },
            new object[] { new MySubUnion4WithStringSubType { Four = 24353 }, new MySubUnion4WithStringSubType { Four = 24353 } },
        };

        [Theory]
        [MemberData(nameof(UnionData))]
        public void Hoge<TU, TU2>(TU data, TU2 data2)
            where TU : IUnionChecker
            where TU2 : IUnionChecker2
        {
            var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data);
            var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2);

            IUnionChecker reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);
            IUnionChecker reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);

            reData1.IsInstanceOf<TU>();
            reData2.IsInstanceOf<TU2>();

            var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null);
            var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null);

            MessagePackSerializer.Deserialize<IUnionChecker>(null1).IsNull();
            MessagePackSerializer.Deserialize<IUnionChecker2>(null1).IsNull();

            var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
            MessagePackSerializer.Deserialize<IUnionChecker>(hoge).IsNull();
        }

        [Fact(Skip = "Does not yet pass")]
        public void IL2CPPHint()
        {
#if UNITY_2018_3_OR_NEWER
            if (int.Parse("1") == 1) return;
#endif
            Hoge<MySubUnion1, MySubUnion1>(default, default);
            Hoge<MySubUnion2, MySubUnion2>(default, default);
            Hoge<MySubUnion3, MySubUnion3>(default, default);
            Hoge<MySubUnion4, MySubUnion4>(default, default);
        }

        [Fact]
        public void ComplexTest()
        {
            var union1 = new A[] { new B() { Name = "b", Val = 2 }, new C() { Name = "t", Val = 5, Valer = 99 } };
            var union2 = new A2[] { new B2() { Name = "b", Val = 2 }, new C2() { Name = "t", Val = 5, Valer = 99 } };

            A[] convert1 = this.Convert(union1);
            A2[] convert2 = this.Convert(union2);

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

            IMessageBody a2 = MessagePackSerializer.Deserialize<IMessageBody>(a);
            IMessageBody b2 = MessagePackSerializer.Deserialize<IMessageBody>(b);
            IMessageBody c2 = MessagePackSerializer.Deserialize<IMessageBody>(c);

            (a2 as TextMessageBody).Text.Is("hoge");
            (b2 as StampMessageBody).StampId.Is(10);
            (c2 as QuestMessageBody).Is(x => x.Text == "hugahuga" && x.QuestId == 99);
        }

        [Fact]
        public void ClassUnion()
        {
            ////var a = new RootUnionType() { MyProperty = 10 };
            var b = new SubUnionType1() { MyProperty = 11, MyProperty1 = 100 };
            var c = new SubUnionType2() { MyProperty = 12, MyProperty2 = 200 };

            //// var binA = MessagePackSerializer.Serialize<RootUnionType>(a);
            var binB = MessagePackSerializer.Serialize<RootUnionType>(b);
            var binC = MessagePackSerializer.Serialize<RootUnionType>(c);

            var b2 = MessagePackSerializer.Deserialize<RootUnionType>(binB) as SubUnionType1;
            var c2 = MessagePackSerializer.Deserialize<RootUnionType>(binC) as SubUnionType2;

            b2.MyProperty.Is(11);
            b2.MyProperty1.Is(100);
            c2.MyProperty.Is(12);
            c2.MyProperty2.Is(200);
        }

        [Theory]
        [MemberData(nameof(UnionDataWithStringSubType))]
        public void HogeWithStringSubType<T1, T2>(T1 data, T2 data2)
            where T1 : IUnionCheckerWithStringSubType
            where T2 : IUnionChecker2WithStringSubType
        {
            var unionData1 = MessagePackSerializer.Serialize<IUnionCheckerWithStringSubType>(data);
            var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2WithStringSubType>(data2);

            var reData1 = MessagePackSerializer.Deserialize<IUnionCheckerWithStringSubType>(unionData1);
            var reData2 = MessagePackSerializer.Deserialize<IUnionCheckerWithStringSubType>(unionData1);

            reData1.IsInstanceOf<T1>();
            reData2.IsInstanceOf<T2>();

            var null1 = MessagePackSerializer.Serialize<IUnionCheckerWithStringSubType>(null);
            var null2 = MessagePackSerializer.Serialize<IUnionChecker2WithStringSubType>(null);

            MessagePackSerializer.Deserialize<IUnionCheckerWithStringSubType>(null1).IsNull();
            MessagePackSerializer.Deserialize<IUnionChecker2WithStringSubType>(null1).IsNull();

            var hoge = MessagePackSerializer.Serialize<IIVersioningUnionWithStringSubType>(new VersioningUnionWithStringSubType { FV = 0 });
            MessagePackSerializer.Deserialize<IUnionCheckerWithStringSubType>(hoge).IsNull();
        }

        [Fact]
        public void ComplexTestWithStringSubType()
        {
            var union1 = new AWithStringSubType[] { new BWithStringSubType() { Name = "b", Val = 2 }, new CWithStringSubType() { Name = "t", Val = 5, Valer = 99 } };
            var union2 = new A2WithStringSubType[] { new B2WithStringSubType() { Name = "b", Val = 2 }, new C2WithStringSubType() { Name = "t", Val = 5, Valer = 99 } };

            var convert1 = Convert(union1);
            var convert2 = Convert(union2);

            convert1[0].IsInstanceOf<BWithStringSubType>().Is(x => x.Name == "b" && x.Val == 2);
            convert1[1].IsInstanceOf<CWithStringSubType>().Is(x => x.Name == "t" && x.Val == 5 && x.Valer == 99);

            convert2[0].IsInstanceOf<B2WithStringSubType>().Is(x => x.Name == "b" && x.Val == 2);
            convert2[1].IsInstanceOf<C2WithStringSubType>().Is(x => x.Name == "t" && x.Val == 5 && x.Valer == 99);
        }

        [Fact]
        public void Union2WithStringSubType()
        {
            var a = MessagePackSerializer.Serialize<IMessageBodyWithStringSubType>(new TextMessageBodyWithStringSubType() { Text = "hoge" });
            var b = MessagePackSerializer.Serialize<IMessageBodyWithStringSubType>(new StampMessageBodyWithStringSubType() { StampId = 10 });
            var c = MessagePackSerializer.Serialize<IMessageBodyWithStringSubType>(new QuestMessageBodyWithStringSubType() { Text = "hugahuga", QuestId = 99 });

            var a2 = MessagePackSerializer.Deserialize<IMessageBodyWithStringSubType>(a);
            var b2 = MessagePackSerializer.Deserialize<IMessageBodyWithStringSubType>(b);
            var c2 = MessagePackSerializer.Deserialize<IMessageBodyWithStringSubType>(c);

            (a2 as TextMessageBodyWithStringSubType).Text.Is("hoge");
            (b2 as StampMessageBodyWithStringSubType).StampId.Is(10);
            (c2 as QuestMessageBodyWithStringSubType).Is(x => x.Text == "hugahuga" && x.QuestId == 99);
        }

        [Fact]
        public void ClassUnionWithStringSubType()
        {
            //var a = new RootUnionTypeWithStringSubType() { MyProperty = 10 };
            var b = new SubUnionType1WithStringSubType() { MyProperty = 11, MyProperty1 = 100 };
            var c = new SubUnionType2WithStringSubType() { MyProperty = 12, MyProperty2 = 200 };

            // var binA = MessagePackSerializer.Serialize<RootUnionTypeWithStringSubType>(a);
            var binB = MessagePackSerializer.Serialize<RootUnionTypeWithStringSubType>(b);
            var binC = MessagePackSerializer.Serialize<RootUnionTypeWithStringSubType>(c);

            var b2 = MessagePackSerializer.Deserialize<RootUnionTypeWithStringSubType>(binB) as SubUnionType1WithStringSubType;
            var c2 = MessagePackSerializer.Deserialize<RootUnionTypeWithStringSubType>(binC) as SubUnionType2WithStringSubType;

            b2.MyProperty.Is(11);
            b2.MyProperty1.Is(100);
            c2.MyProperty.Is(12);
            c2.MyProperty2.Is(200);
        }
    }
}

namespace ComplexdUnion
{
    [MessagePackObject(true)]
    public class DummyForGenerate
    {
        public A[] MyProperty1 { get; set; }

        public A2[] MyProperty2 { get; set; }
    }

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
        public string Type
        {
            get { return "B"; }
        }

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }

    [MessagePackObject]
    public class C : A
    {
        [IgnoreMember]
        public string Type
        {
            get { return "C"; }
        }

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
        public string Type
        {
            get { return "B"; }
        }

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

    [Union(0, "ComplexdUnion.BWithStringSubType, MessagePack.Tests")]
    [Union(1, "ComplexdUnion.CWithStringSubType, MessagePack.Tests")]
    public interface AWithStringSubType
    {
        string Type { get; }

        string Name { get; set; }
    }

    [MessagePackObject]
    public class BWithStringSubType : AWithStringSubType
    {
        [IgnoreMember]
        public string Type => "B";

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }

    [MessagePackObject]
    public class CWithStringSubType : AWithStringSubType
    {
        [IgnoreMember]
        public string Type => "C";

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }

        [Key(2)]
        public virtual int Valer { get; set; }
    }

    [Union(0, "ComplexdUnion.B2WithStringSubType, MessagePack.Tests")]
    [Union(1, "ComplexdUnion.C2WithStringSubType, MessagePack.Tests")]
    public interface A2WithStringSubType
    {
        string Type { get; }

        string Name { get; set; }
    }

    [MessagePackObject]
    public class B2WithStringSubType : A2WithStringSubType
    {
        [IgnoreMember]
        public string Type => "B";

        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public virtual int Val { get; set; }
    }

    [MessagePackObject]
    public class C2WithStringSubType : B2WithStringSubType
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

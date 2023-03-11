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
            new object[] { new UnionWithStrings.MySubUnion1WithStringSubType { One = 23 },     new UnionWithStrings.MySubUnion1WithStringSubType { One = 23 } },
            new object[] { new UnionWithStrings.MySubUnion2WithStringSubType { Two = 233 },    new UnionWithStrings.MySubUnion2WithStringSubType { Two = 233 } },
            new object[] { new UnionWithStrings.MySubUnion3WithStringSubType { Three = 253 },  new UnionWithStrings.MySubUnion3WithStringSubType { Three = 253 } },
            new object[] { new UnionWithStrings.MySubUnion4WithStringSubType { Four = 24353 }, new UnionWithStrings.MySubUnion4WithStringSubType { Four = 24353 } },
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
        public void AbstractClassUnion()
        {
            var b = new SubUnionType1() { MyProperty = 11, MyProperty1 = 100 };
            var c = new SubUnionType2() { MyProperty = 12, MyProperty2 = 200 };

            var binB = MessagePackSerializer.Serialize<RootUnionType>(b);
            var binC = MessagePackSerializer.Serialize<RootUnionType>(c);

            var dB = (SubUnionType1)MessagePackSerializer.Deserialize<RootUnionType>(binB);
            Assert.Equal(11, dB.MyProperty);
            Assert.Equal(100, dB.MyProperty1);

            var dC = (SubUnionType2)MessagePackSerializer.Deserialize<RootUnionType>(binC);
            Assert.Equal(12, dC.MyProperty);
            Assert.Equal(200, dC.MyProperty2);
        }

        [Fact(Skip = "Not implemented. See https://github.com/neuecc/MessagePack-CSharp/issues/1465")]
        public void ConcreteClassUnion()
        {
            var a = new ConcreteClassUnion.RootUnionType() { MyProperty = 10 };
            var b = new ConcreteClassUnion.SubUnionType1() { MyProperty = 11, MyProperty1 = 100 };
            var c = new ConcreteClassUnion.SubUnionType2() { MyProperty = 12, MyProperty2 = 200 };

            var binA = MessagePackSerializer.Serialize<ConcreteClassUnion.RootUnionType>(a);
            var binB = MessagePackSerializer.Serialize<ConcreteClassUnion.RootUnionType>(b);
            var binC = MessagePackSerializer.Serialize<ConcreteClassUnion.RootUnionType>(c);

            var dA = MessagePackSerializer.Deserialize<ConcreteClassUnion.RootUnionType>(binA);
            Assert.Equal(10, dA.MyProperty);

            var dB = (ConcreteClassUnion.SubUnionType1)MessagePackSerializer.Deserialize<ConcreteClassUnion.RootUnionType>(binB);
            Assert.Equal(11, dB.MyProperty);
            Assert.Equal(100, dB.MyProperty1);

            var dC = (ConcreteClassUnion.SubUnionType2)MessagePackSerializer.Deserialize<ConcreteClassUnion.RootUnionType>(binC);
            Assert.Equal(12, dC.MyProperty);
            Assert.Equal(200, dC.MyProperty2);
        }

        [Theory]
        [MemberData(nameof(UnionDataWithStringSubType))]
        public void HogeWithStringSubType<T1, T2>(T1 data, T2 data2)
            where T1 : UnionWithStrings.IUnionCheckerWithStringSubType
            where T2 : UnionWithStrings.IUnionChecker2WithStringSubType
        {
            var unionData1 = MessagePackSerializer.Serialize<UnionWithStrings.IUnionCheckerWithStringSubType>(data);
            var unionData2 = MessagePackSerializer.Serialize<UnionWithStrings.IUnionChecker2WithStringSubType>(data2);

            var reData1 = MessagePackSerializer.Deserialize<UnionWithStrings.IUnionCheckerWithStringSubType>(unionData1);
            var reData2 = MessagePackSerializer.Deserialize<UnionWithStrings.IUnionCheckerWithStringSubType>(unionData1);

            reData1.IsInstanceOf<T1>();
            reData2.IsInstanceOf<T2>();

            var null1 = MessagePackSerializer.Serialize<UnionWithStrings.IUnionCheckerWithStringSubType>(null);
            var null2 = MessagePackSerializer.Serialize<UnionWithStrings.IUnionChecker2WithStringSubType>(null);

            MessagePackSerializer.Deserialize<UnionWithStrings.IUnionCheckerWithStringSubType>(null1).IsNull();
            MessagePackSerializer.Deserialize<UnionWithStrings.IUnionChecker2WithStringSubType>(null1).IsNull();

            var hoge = MessagePackSerializer.Serialize<UnionWithStrings.IIVersioningUnionWithStringSubType>(new UnionWithStrings.VersioningUnionWithStringSubType { FV = 0 });
            MessagePackSerializer.Deserialize<UnionWithStrings.IUnionCheckerWithStringSubType>(hoge).IsNull();
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
            var a = MessagePackSerializer.Serialize<UnionWithStrings.IMessageBodyWithStringSubType>(new UnionWithStrings.TextMessageBodyWithStringSubType() { Text = "hoge" });
            var b = MessagePackSerializer.Serialize<UnionWithStrings.IMessageBodyWithStringSubType>(new UnionWithStrings.StampMessageBodyWithStringSubType() { StampId = 10 });
            var c = MessagePackSerializer.Serialize<UnionWithStrings.IMessageBodyWithStringSubType>(new UnionWithStrings.QuestMessageBodyWithStringSubType() { Text = "hugahuga", QuestId = 99 });

            var a2 = MessagePackSerializer.Deserialize<UnionWithStrings.IMessageBodyWithStringSubType>(a);
            var b2 = MessagePackSerializer.Deserialize<UnionWithStrings.IMessageBodyWithStringSubType>(b);
            var c2 = MessagePackSerializer.Deserialize<UnionWithStrings.IMessageBodyWithStringSubType>(c);

            (a2 as UnionWithStrings.TextMessageBodyWithStringSubType).Text.Is("hoge");
            (b2 as UnionWithStrings.StampMessageBodyWithStringSubType).StampId.Is(10);
            (c2 as UnionWithStrings.QuestMessageBodyWithStringSubType).Is(x => x.Text == "hugahuga" && x.QuestId == 99);
        }

        [Fact]
        public void ClassUnionWithStringSubType()
        {
            ////var a = new RootUnionTypeWithStringSubType() { MyProperty = 10 };
            var b = new UnionWithStrings.SubUnionType1WithStringSubType() { MyProperty = 11, MyProperty1 = 100 };
            var c = new UnionWithStrings.SubUnionType2WithStringSubType() { MyProperty = 12, MyProperty2 = 200 };

            // var binA = MessagePackSerializer.Serialize<RootUnionTypeWithStringSubType>(a);
            var binB = MessagePackSerializer.Serialize<UnionWithStrings.RootUnionTypeWithStringSubType>(b);
            var binC = MessagePackSerializer.Serialize<UnionWithStrings.RootUnionTypeWithStringSubType>(c);

            var b2 = MessagePackSerializer.Deserialize<UnionWithStrings.RootUnionTypeWithStringSubType>(binB) as UnionWithStrings.SubUnionType1WithStringSubType;
            var c2 = MessagePackSerializer.Deserialize<UnionWithStrings.RootUnionTypeWithStringSubType>(binC) as UnionWithStrings.SubUnionType2WithStringSubType;

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
    [Union(0, typeof(RootUnionType))]
    [Union(1, typeof(SubUnionType1))]
    [Union(2, typeof(SubUnionType2))]
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

namespace ConcreteClassUnion
{
    [Union(0, typeof(RootUnionType))]
    [Union(1, typeof(SubUnionType1))]
    [Union(2, typeof(SubUnionType2))]
    [MessagePackObject]
    public class RootUnionType
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

namespace UnionWithStrings
{
    [Union(0, "UnionWithStrings.MySubUnion1WithStringSubType, MessagePack.Tests")]
    [Union(1, "UnionWithStrings.MySubUnion2WithStringSubType, MessagePack.Tests")]
    [Union(2, "UnionWithStrings.MySubUnion3WithStringSubType, MessagePack.Tests")]
    [Union(3, "UnionWithStrings.MySubUnion4WithStringSubType, MessagePack.Tests")]
    public interface IUnionCheckerWithStringSubType
    {
    }

    [Union(120, "UnionWithStrings.MySubUnion1WithStringSubType, MessagePack.Tests")]
    [Union(31, "UnionWithStrings.MySubUnion2WithStringSubType, MessagePack.Tests")]
    [Union(42, "UnionWithStrings.MySubUnion3WithStringSubType, MessagePack.Tests")]
    [Union(63, "UnionWithStrings.MySubUnion4WithStringSubType, MessagePack.Tests")]
    public interface IUnionChecker2WithStringSubType
    {
    }

    [Union(0, "UnionWithStrings.MySubUnion1WithStringSubType, MessagePack.Tests")]
    ////[Union(1, "UnionWithStrings.MySubUnion2WithStringSubType, MessagePack.Tests")]
    ////[Union(2, "UnionWithStrings.MySubUnion3WithStringSubType, MessagePack.Tests")]
    ////[Union(3, "UnionWithStrings.MySubUnion4WithStringSubType, MessagePack.Tests")]
    ////[Union(4, "UnionWithStrings.VersioningUnionWithStringSubType, MessagePack.Tests")]
    public interface IIVersioningUnionWithStringSubType
    {
    }

    [Union(0, "UnionWithStrings.SubUnionType1WithStringSubType, MessagePack.Tests")]
    [Union(1, "UnionWithStrings.SubUnionType2WithStringSubType, MessagePack.Tests")]
    [MessagePackObject]
    public abstract class RootUnionTypeWithStringSubType
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    [MessagePackObject]
    public class SubUnionType1WithStringSubType : RootUnionTypeWithStringSubType
    {
        [Key(1)]
        public int MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class SubUnionType2WithStringSubType : RootUnionTypeWithStringSubType
    {
        [Key(1)]
        public int MyProperty2 { get; set; }
    }

    [MessagePackObject]
    public class MySubUnion1WithStringSubType : IUnionCheckerWithStringSubType, IUnionChecker2WithStringSubType
    {
        [Key(3)]
        public int One { get; set; }
    }

    [MessagePackObject]
    public struct MySubUnion2WithStringSubType : IUnionCheckerWithStringSubType, IUnionChecker2WithStringSubType
    {
        [Key(5)]
        public int Two { get; set; }
    }

    [MessagePackObject]
    public class MySubUnion3WithStringSubType : IUnionCheckerWithStringSubType, IUnionChecker2WithStringSubType
    {
        [Key(2)]
        public int Three { get; set; }
    }

    [MessagePackObject]
    public struct MySubUnion4WithStringSubType : IUnionCheckerWithStringSubType, IUnionChecker2WithStringSubType
    {
        [Key(7)]
        public int Four { get; set; }
    }

    [MessagePackObject]
    public class VersioningUnionWithStringSubType : IUnionCheckerWithStringSubType, IIVersioningUnionWithStringSubType
    {
        [Key(7)]
        public int FV { get; set; }
    }

    [Union(10, "UnionWithStrings.TextMessageBodyWithStringSubType, MessagePack.Tests")]
    [Union(14, "UnionWithStrings.StampMessageBodyWithStringSubType, MessagePack.Tests")]
    [Union(25, "UnionWithStrings.QuestMessageBodyWithStringSubType, MessagePack.Tests")]
    public interface IMessageBodyWithStringSubType
    {
    }

    [MessagePackObject]
    public class TextMessageBodyWithStringSubType : IMessageBodyWithStringSubType
    {
        [Key(0)]
        public string Text { get; set; }
    }

    [MessagePackObject]
    public class StampMessageBodyWithStringSubType : IMessageBodyWithStringSubType
    {
        [Key(0)]
        public int StampId { get; set; }
    }

    [MessagePackObject]
    public class QuestMessageBodyWithStringSubType : IMessageBodyWithStringSubType
    {
        [Key(0)]
        public int QuestId { get; set; }

        [Key(1)]
        public string Text { get; set; }
    }
}

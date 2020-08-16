// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1136 // enum values on their own lines
#pragma warning disable SA1201 // type ordering
#pragma warning disable SA1307 // capitalize public fields
#pragma warning disable SA1402 // multiple types in the file
#pragma warning disable SA1502 // elements on single line
#pragma warning disable SA1403 // multiple namespaces in the file
#pragma warning disable SA1649 // type doesn't match file name

using System;
using System.Buffers;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace SharedData
{
    public enum ByteEnum : byte { A, B, C, D, E }

    public enum SByteEnum : sbyte { A, B, C, D, E }

    public enum ShortEnum : short { A, B, C, D, E }

    public enum UShortEnum : ushort { A, B, C, D, E }

    public enum IntEnum : int { A, B, C, D, E }

    public enum UIntEnum : uint { A, B, C, D, E }

    public enum LongEnum : long { A, B, C, D, E }

    public enum ULongEnum : ulong { A, B, C, D, E }

    [MessagePackObject]
    public class FirstSimpleData : IEquatable<FirstSimpleData>
    {
        [Key(0)]
        public int Prop1 { get; set; }

        [Key(1)]
        public string Prop2 { get; set; }

        [Key(2)]
        public int Prop3 { get; set; }

        public bool Equals(FirstSimpleData other)
        {
            return other != null
                && this.Prop1 == other.Prop1
                && this.Prop2 == other.Prop2
                && this.Prop3 == other.Prop3;
        }
    }

    [MessagePackObject]
    public class SimpleIntKeyData
    {
        [Key(0)]
        ////[MessagePackFormatter(typeof(OreOreFormatter))]
        public int Prop1 { get; set; }

        [Key(1)]
        public ByteEnum Prop2 { get; set; }

        [Key(2)]
        public string Prop3 { get; set; }

        [Key(3)]
        public SimpleStringKeyData Prop4 { get; set; }

        [Key(4)]
        public SimpleStructIntKeyData Prop5 { get; set; }

        [Key(5)]
        public SimpleStructStringKeyData Prop6 { get; set; }

        [Key(6)]
        public byte[] BytesSpecial { get; set; }

        ////[Key(7)]
        ////[MessagePackFormatter(typeof(OreOreFormatter2), 100, "hogehoge")]
        ////[MessagePackFormatter(typeof(OreOreFormatter))]
        ////public int Prop7 { get; set; }
    }

    public class OreOreFormatter : IMessagePackFormatter<int>
    {
        public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class OreOreFormatter2 : IMessagePackFormatter<int>
    {
        public OreOreFormatter2(int x, string y)
        {
        }

        public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    [MessagePackObject(true)]
    public class SimpleStringKeyData
    {
        public int Prop1 { get; set; }

        public ByteEnum Prop2 { get; set; }

        public int Prop3 { get; set; }
    }

    [MessagePackObject]
    public struct SimpleStructIntKeyData
    {
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public int Y { get; set; }

        [Key(2)]
        public byte[] BytesSpecial { get; set; }
    }

    [MessagePackObject]
    public struct SimpleStructStringKeyData
    {
        [Key("key-X")]
        public int X { get; set; }

        [Key("key-Y")]
        public int[] Y { get; set; }
    }

    [MessagePackObject]
    public struct Vector2
    {
        [Key(0)]
        public readonly float X;
        [Key(1)]
        public readonly float Y;

        public Vector2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    [MessagePackObject]
    public class EmptyClass
    {
    }

    [MessagePackObject]
    public struct EmptyStruct
    {
    }

    [MessagePackObject]
    public class Version1
    {
        [Key(3)]
        public int MyProperty1 { get; set; }

        [Key(4)]
        public int MyProperty2 { get; set; }

        [Key(5)]
        public int MyProperty3 { get; set; }
    }

    [MessagePackObject]
    public class Version2
    {
        [Key(3)]
        public int MyProperty1 { get; set; }

        [Key(4)]
        public int MyProperty2 { get; set; }

        [Key(5)]
        public int MyProperty3 { get; set; }

        // [Key(6)]
        // public int MyProperty4 { get; set; }
        [Key(7)]
        public int MyProperty5 { get; set; }
    }

    [MessagePackObject]
    public class Version0
    {
        [Key(3)]
        public int MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class HolderV1
    {
        [Key(0)]
        public Version1 MyProperty1 { get; set; }

        [Key(1)]
        public int After { get; set; }
    }

    [MessagePackObject]
    public class HolderV2
    {
        [Key(0)]
        public Version2 MyProperty1 { get; set; }

        [Key(1)]
        public int After { get; set; }
    }

    [MessagePackObject]
    public class HolderV0
    {
        [Key(0)]
        public Version0 MyProperty1 { get; set; }

        [Key(1)]
        public int After { get; set; }
    }

    [MessagePackObject]
    public class Callback1 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        [IgnoreMember]
        public bool CalledBefore { get; private set; }

        [IgnoreMember]
        public bool CalledAfter { get; private set; }

        public Callback1(int x)
        {
        }

        public void OnBeforeSerialize()
        {
            this.CalledBefore = true;
        }

        public void OnAfterDeserialize()
        {
            this.CalledAfter = true;
        }
    }

    [MessagePackObject]
    public class Callback1_2 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        [IgnoreMember]
        public bool CalledBefore { get; private set; }

        [IgnoreMember]
        public bool CalledAfter { get; private set; }

        public Callback1_2(int x)
        {
            this.X = x;
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.CalledBefore = true;
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.CalledAfter = true;
        }
    }

    [MessagePackObject(true)]
    public struct Callback2 : IMessagePackSerializationCallbackReceiver
    {
        public static bool CalledAfter = false;

        [Key(0)]
        public int X { get; set; }

        private Action onBefore;
        private Action onAfter;

        public Callback2(int x)
            : this(x, () => { }, () => { })
        {
        }

        public Callback2(int x, Action onBefore, Action onAfter)
        {
            this.X = x;
            this.onBefore = onBefore;
            this.onAfter = onAfter;
        }

        public void OnBeforeSerialize()
        {
            this.onBefore();
        }

        public void OnAfterDeserialize()
        {
            CalledAfter = true;
        }
    }

    [MessagePackObject(true)]
    public struct Callback2_2 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        public static bool CalledAfter = false;

        public Callback2_2(int x)
            : this(x, () => { }, () => { })
        {
        }

        private Action onBefore;
        private Action onAfter;

        public Callback2_2(int x, Action onBefore, Action onAfter)
        {
            this.X = x;
            this.onBefore = onBefore;
            this.onAfter = onAfter;
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.onBefore();
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            CalledAfter = true;
        }
    }

    [Union(0, typeof(MySubUnion1))]
    [Union(1, typeof(MySubUnion2))]
    [Union(2, typeof(MySubUnion3))]
    [Union(3, typeof(MySubUnion4))]
    public interface IUnionChecker
    {
    }

    [Union(120, typeof(MySubUnion1))]
    [Union(31, typeof(MySubUnion2))]
    [Union(42, typeof(MySubUnion3))]
    [Union(63, typeof(MySubUnion4))]
    public interface IUnionChecker2
    {
    }

    [Union(0, typeof(MySubUnion1))]
    ////[Union(1, typeof(MySubUnion2))]
    ////[Union(2, typeof(MySubUnion3))]
    ////[Union(3, typeof(MySubUnion4))]
    ////[Union(4, typeof(VersioningUnion))]
    public interface IIVersioningUnion
    {
    }

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

    [MessagePackObject]
    public class MySubUnion1 : IUnionChecker, IUnionChecker2
    {
        [Key(3)]
        public int One { get; set; }
    }

    [MessagePackObject]
    public struct MySubUnion2 : IUnionChecker, IUnionChecker2
    {
        [Key(5)]
        public int Two { get; set; }
    }

    [MessagePackObject]
    public class MySubUnion3 : IUnionChecker, IUnionChecker2
    {
        [Key(2)]
        public int Three { get; set; }
    }

    [MessagePackObject]
    public struct MySubUnion4 : IUnionChecker, IUnionChecker2
    {
        [Key(7)]
        public int Four { get; set; }
    }

    [MessagePackObject]
    public class VersioningUnion : IUnionChecker, IIVersioningUnion
    {
        [Key(7)]
        public int FV { get; set; }
    }

    [MessagePackObject]
    public class GenericClass<T1, T2>
    {
        [Key(0)]
        public T1 MyProperty0 { get; set; }

        [Key(1)]
        public T2 MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public struct GenericStruct<T1, T2>
    {
        [Key(0)]
        public T1 MyProperty0 { get; set; }

        [Key(1)]
        public T2 MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class VersionBlockTest
    {
        [Key(0)]
        public int MyProperty { get; set; }

        [Key(1)]
        public MyClass UnknownBlock { get; set; }

        [Key(2)]
        public int MyProperty2 { get; set; }
    }

    [MessagePackObject]
    public class UnVersionBlockTest
    {
        [Key(0)]
        public int MyProperty { get; set; }

        ////[Key(1)]
        ////public MyClass UnknownBlock { get; set; }

        [Key(2)]
        public int MyProperty2 { get; set; }
    }

    [MessagePackObject]
    public class MyClass
    {
        [Key(0)]
        public int MyProperty1 { get; set; }

        [Key(1)]
        public int MyProperty2 { get; set; }

        [Key(2)]
        public int MyProperty3 { get; set; }
    }

    [MessagePackObject]
    public class Empty1
    {
    }

    [MessagePackObject(true)]
    public class Empty2
    {
    }

    [MessagePackObject]
    public class NonEmpty1
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    [MessagePackObject(true)]
    public class NonEmpty2
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    [MessagePackObject]
    public struct VectorLike2
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;

        [SerializationConstructor]
        public VectorLike2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct Vector3Like
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;

        [SerializationConstructor]
        public Vector3Like(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Like operator *(Vector3Like a, float d)
        {
            return new Vector3Like(a.x * d, a.y * d, a.z * d);
        }
    }

    public class ContractlessConstructorCheck
    {
        public int MyProperty1 { get; set; }

        public string MyProperty2 { get; set; }

        public ContractlessConstructorCheck(KeyValuePair<int, string> ok)
        {
        }

        [SerializationConstructor]
        public ContractlessConstructorCheck(int myProperty1, string myProperty2)
        {
            this.MyProperty1 = myProperty1;
            this.MyProperty2 = myProperty2;
        }
    }

    public class FindingConstructorCheck
    {
        public int MyProperty1 { get; private set; }

        public string MyProperty2 { get; private set; }

        public FindingConstructorCheck(KeyValuePair<int, string> ok)
        {
        }

        public FindingConstructorCheck(int myProperty1, string myProperty2)
        {
            this.MyProperty1 = myProperty1;
            this.MyProperty2 = myProperty2;
        }
    }

    [MessagePackObject]
    public class ArrayOptimizeClass
    {
        [Key(0)]
        public int MyProperty0 { get; set; }

        [Key(1)]
        public int MyProperty1 { get; set; }

        [Key(2)]
        public int MyProperty2 { get; set; }

        [Key(3)]
        public int MyProperty3 { get; set; }

        [Key(4)]
        public int MyProperty4 { get; set; }

        [Key(5)]
        public int MyProperty5 { get; set; }

        [Key(6)]
        public int MyProperty6 { get; set; }

        [Key(7)]
        public int MyProperty7 { get; set; }

        [Key(8)]
        public int MyProperty8 { get; set; }

        [Key(9)]
        public int MyProvperty9 { get; set; }

        [Key(10)]
        public int MyProperty10 { get; set; }

        [Key(11)]
        public int MyProperty11 { get; set; }

        [Key(12)]
        public int MyPropverty12 { get; set; }

        [Key(13)]
        public int MyPropevrty13 { get; set; }

        [Key(14)]
        public int MyProperty14 { get; set; }

        [Key(15)]
        public int MyProperty15 { get; set; }
    }

    [MessagePackObject]
    public struct DynamicArgumentTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        [Key(0)]
        public readonly T1 Item1;
        [Key(1)]
        public readonly T2 Item2;
        [Key(2)]
        public readonly T3 Item3;
        [Key(3)]
        public readonly T4 Item4;
        [Key(4)]
        public readonly T5 Item5;
        [Key(5)]
        public readonly T6 Item6;
        [Key(6)]
        public readonly T7 Item7;
        [Key(7)]
        public readonly T8 Item8;
        [Key(8)]
        public readonly T9 Item9;

        [SerializationConstructor]
        public DynamicArgumentTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
            this.Item8 = item8;
            this.Item9 = item9;
        }
    }

    public class DynamicArgumentTupleFormatter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IMessagePackFormatter<DynamicArgumentTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
    {
        private readonly T1 default1;
        private readonly T2 default2;
        private readonly T3 default3;
        private readonly T4 default4;
        private readonly T5 default5;
        private readonly T6 default6;
        private readonly T7 default7;
        private readonly T8 default8;
        private readonly T9 default9;

        public DynamicArgumentTupleFormatter(T1 default1, T2 default2, T3 default3, T4 default4, T5 default5, T6 default6, T7 default7, T8 default8, T9 default9)
        {
            this.default1 = default1;
            this.default2 = default2;
            this.default3 = default3;
            this.default4 = default4;
            this.default5 = default5;
            this.default6 = default6;
            this.default7 = default7;
            this.default8 = default8;
            this.default9 = default9;
        }

        public void Serialize(ref MessagePackWriter writer, DynamicArgumentTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(9);
            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
            resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, options);
            resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, options);
            resolver.GetFormatterWithVerify<T7>().Serialize(ref writer, value.Item7, options);
            resolver.GetFormatterWithVerify<T8>().Serialize(ref writer, value.Item8, options);
            resolver.GetFormatterWithVerify<T9>().Serialize(ref writer, value.Item9, options);
        }

        public DynamicArgumentTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var length = reader.ReadArrayHeader();

            T1 item1 = this.default1;
            T2 item2 = this.default2;
            T3 item3 = this.default3;
            T4 item4 = this.default4;
            T5 item5 = this.default5;
            T6 item6 = this.default6;
            T7 item7 = this.default7;
            T8 item8 = this.default8;
            T9 item9 = this.default9;

            IFormatterResolver resolver = options.Resolver;
            for (var i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);
                        break;
                    case 4:
                        item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, options);
                        break;
                    case 5:
                        item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, options);
                        break;
                    case 6:
                        item7 = resolver.GetFormatterWithVerify<T7>().Deserialize(ref reader, options);
                        break;
                    case 7:
                        item8 = resolver.GetFormatterWithVerify<T8>().Deserialize(ref reader, options);
                        break;
                    case 8:
                        item9 = resolver.GetFormatterWithVerify<T9>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new DynamicArgumentTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(item1, item2, item3, item4, item5, item6, item7, item8, item9);
        }
    }

    public class NestParent
    {
        [MessagePackObject]
        public class NestContract
        {
            [Key(0)]
            public int MyProperty { get; set; }
        }

        public class NestContractless
        {
            public int MyProperty { get; set; }
        }
    }

    [MessagePack.Union(0, typeof(FooClass))]
    [MessagePack.Union(100, typeof(BarClass))]
    public interface IUnionSample
    {
    }

    [MessagePackObject]
    public class FooClass : IUnionSample
    {
        [Key(0)]
        public int XYZ { get; set; }
    }

    [MessagePackObject]
    public class BarClass : IUnionSample
    {
        [Key(0)]
        public string OPQ { get; set; }
    }

    [MessagePackObject]
    public class WithIndexer
    {
        [Key(0)]
        public int Data1 { get; set; }

        [Key(1)]
        public string Data2 { get; set; }

        [Key(2)]
        public int this[int i] => 0;
    }

    public class WithIndexerContractless
    {
        public int Data1 { get; set; }

        public string Data2 { get; set; }

        public int this[int i] => 0;
    }
}

namespace Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad
{
    [MessagePackObject]
    public class TnonodsfarnoiuAtatqaga
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }
}

[MessagePackObject]
public class GlobalMan
{
    [Key(0)]
    public int MyProperty { get; set; }
}

[MessagePackObject]
public class Message
{
    [Key(0)]
    public int UserId { get; set; }

    [Key(1)]
    public int RoomId { get; set; }

    [Key(2)]
    public DateTime PostTime { get; set; }

    // 本文
    [Key(3)]
    public IMessageBody Body { get; set; }
}

[Union(10, typeof(TextMessageBody))]
[Union(14, typeof(StampMessageBody))]
[Union(25, typeof(QuestMessageBody))]
public interface IMessageBody { }

[MessagePackObject]
public class TextMessageBody : IMessageBody
{
    [Key(0)]
    public string Text { get; set; }
}

[MessagePackObject]
public class StampMessageBody : IMessageBody
{
    [Key(0)]
    public int StampId { get; set; }
}

[MessagePackObject]
public class QuestMessageBody : IMessageBody
{
    [Key(0)]
    public int QuestId { get; set; }

    [Key(1)]
    public string Text { get; set; }
}

public enum GlobalMyEnum
{
    Apple, Orange,
}

[MessagePackObject]
public class ArrayTestTest
{
    [Key(0)]
    public int[] MyProperty0 { get; set; }

    [Key(1)]
    public int[,] MyProperty1 { get; set; }

    [Key(2)]
    public GlobalMyEnum[,] MyProperty2 { get; set; }

    [Key(3)]
    public int[,,] MyProperty3 { get; set; }

    [Key(4)]
    public int[,,,] MyProperty4 { get; set; }

    [Key(5)]
    public GlobalMyEnum[] MyProperty5 { get; set; }

    [Key(6)]
    public QuestMessageBody[] MyProperty6 { get; set; }
}

[MessagePackObject(true)]
public class ComplexModel
{
    public IDictionary<string, string> AdditionalProperty { get; private set; }

    public DateTimeOffset CreatedOn { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTimeOffset UpdatedOn { get; set; }

    public IList<SimpleModel> SimpleModels { get; private set; }

    public ComplexModel()
    {
        this.AdditionalProperty = new Dictionary<string, string>();
        this.SimpleModels = new List<SimpleModel>();
    }
}

[MessagePackObject(true)]
public class SimpleModel
{
    private decimal money;

    public int Id { get; set; }

    public string Name { get; set; }

    public DateTime CreatedOn { get; set; }

    public int Precision { get; set; }

    public SimpleModel()
    {
        this.Precision = 4;
    }

    public decimal Money
    {
        get
        {
            return this.money;
        }

        set
        {
            this.money = Math.Round(value, this.Precision);
        }
    }

    public long Amount
    {
        get
        {
            return (long)Math.Round(this.Money, 0, MidpointRounding.ToEven);
        }
    }
}

namespace PerfBenchmarkDotNet
{
    [MessagePackObject(true)]
    public class StringKeySerializerTarget
    {
        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }

        public int MyProperty3 { get; set; }

        public int MyProperty4 { get; set; }

        public int MyProperty5 { get; set; }

        public int MyProperty6 { get; set; }

        public int MyProperty7 { get; set; }

        public int MyProperty8 { get; set; }

        public int MyProperty9 { get; set; }
    }
}

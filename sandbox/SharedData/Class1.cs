using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public class FirstSimpleData
    {
        [Key(0)]
        public int Prop1 { get; set; }
        [Key(1)]
        public string Prop2 { get; set; }
        [Key(2)]
        public int Prop3 { get; set; }
    }

    [MessagePackObject]
    public class SimpleIntKeyData
    {
        [Key(0)]
        public int Prop1 { get; set; }
        [Key(1)]
        public ByteEnum Prop2 { get; set; }
        [Key(2)]
        public string Prop3 { get; set; }
        [Key(3)]
        public SimlpeStringKeyData Prop4 { get; set; }
        [Key(4)]
        public SimpleStructIntKeyData Prop5 { get; set; }
        [Key(5)]
        public SimpleStructStringKeyData Prop6 { get; set; }
        [Key(6)]
        public byte[] BytesSpecial { get; set; }
    }

    [MessagePackObject(true)]
    public class SimlpeStringKeyData
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
            X = x;
            Y = y;
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

        public bool CalledBefore { get; private set; }
        public bool CalledAfter { get; private set; }

        public Callback1(int x)
        {

        }

        public void OnBeforeSerialize()
        {
            CalledBefore = true;
        }

        public void OnAfterDeserialize()
        {
            CalledAfter = true;
        }
    }

    [MessagePackObject]
    public class Callback1_2 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        public bool CalledBefore { get; private set; }
        public bool CalledAfter { get; private set; }
        public Callback1_2(int x)
        {
            this.X = x;
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            CalledBefore = true;
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            CalledAfter = true;
        }
    }

    [MessagePackObject(true)]
    public struct Callback2 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        Action onBefore;
        Action onAfter;

        public static bool CalledAfter = false;

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
            onBefore();
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

        Action onBefore;
        Action onAfter;
        public Callback2_2(int x, Action onBefore, Action onAfter)
        {
            this.X = x;
            this.onBefore = onBefore;
            this.onAfter = onAfter;
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            onBefore();
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
    [Union(1, typeof(MySubUnion2))]
    [Union(2, typeof(MySubUnion3))]
    [Union(3, typeof(MySubUnion4))]
    [Union(4, typeof(VersioningUnion))]
    public interface IIVersioningUnion
    {

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


    [MessagePackObject(true)]
    public class LivingPrimitive
    {
        public short A1;
        public readonly int A2;
        public long A3;
        public ushort A4;
        public uint A5;
        public ulong A6;
        public float A7;
        public double A8;
        public bool A9;
        public readonly byte A10;
        public sbyte A11;
        public DateTime A12;
        public char A13;
        public byte[] A14;
        public string[] A15;
        public string A16;

        public LivingPrimitive(int a2, byte a10)
        {
            A2 = a2;
            A10 = a10;
        }
    }


    [MessagePackObject(true)]
    public class DataIncludeCollection
    {
        public List<FirstSimpleData> Test1;
        public FirstSimpleData[] Test2;
        public Dictionary<int, FirstSimpleData> Test3;
        public ILookup<IntEnum, FirstSimpleData> Test4;
        public IList<string> Test5;
        public Lazy<string> Test6;
        public ConcurrentDictionary<int, string> Test7;
        public Tuple<int, string> Test8;
        public Tuple<int, string, FirstSimpleData, FirstSimpleData, int, int, int, int> Test9;

        public int? TestNullable;
        public MySubUnion4? TestNullable2;


        public ArraySegment<int> S1;
        public ArraySegment<byte> S2;
        public ArraySegment<int>? S3;
        public ArraySegment<byte>? S4;
        public KeyValuePair<int, int> S5;

        public DataIncludeCollection()
        {
        }
    }
}
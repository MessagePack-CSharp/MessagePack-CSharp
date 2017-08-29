using MessagePack;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Diagnostics;
using System.IO;
using ZeroFormatter;
using System.Collections.Generic;
using MessagePack.Internal;
using ProtoBuf;
using SharedData;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;
using System.IO.Compression;
using System.Collections.Concurrent;

namespace Sandbox
{

    [ZeroFormattable]
    [ProtoBuf.ProtoContract]
    [MessagePackObject]
    public class Person : IEquatable<Person>
    {
        [Index(0)]
        [Key(0)]
        [MsgPack.Serialization.MessagePackMember(0)]
        [ProtoMember(1)]
        public virtual int Age { get; set; }
        [Index(1)]
        [Key(1)]
        [MsgPack.Serialization.MessagePackMember(1)]
        [ProtoMember(2)]
        public virtual string FirstName { get; set; }
        [Index(2)]
        [Key(2)]
        [MsgPack.Serialization.MessagePackMember(2)]
        [ProtoMember(3)]
        public virtual string LastName { get; set; }
        [Index(3)]
        [MsgPack.Serialization.MessagePackMember(3)]
        [Key(3)]
        [ProtoMember(4)]
        public virtual Sex Sex { get; set; }

        public bool Equals(Person other)
        {
            return Age == other.Age && FirstName == other.FirstName && LastName == other.LastName && Sex == other.Sex;
        }
    }

    public enum Sex : sbyte
    {
        Unknown, Male, Female,
    }

    public class TestCollection<T> : ICollection<T>
    {
        public List<T> internalCollection = new List<T>();

        public int Count => internalCollection.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item)
        {
            internalCollection.Add(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return internalCollection.GetEnumerator();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }


    [MessagePackObject(true)]
    public class Takox
    {
        public int hoga { get; set; }
        public int huga { get; set; }
        public int tako { get; set; }
    }

    [MessagePackObject]
    public class MyClass
    {
        // Key is serialization index, it is important for versioning.
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; }

        [Key(2)]
        public string LastName { get; set; }

        // public members and does not serialize target, mark IgnoreMemberttribute
        [IgnoreMember]
        public string FullName { get { return FirstName + LastName; } }
    }


    [MessagePackObject(keyAsPropertyName: true)]
    public class Sample1
    {
        [Key(0)]
        public int Foo { get; set; }
        [Key(1)]
        public int Bar { get; set; }
    }

    [MessagePackObject]
    public class Sample2
    {
        [Key("foo")]
        public int Foo { get; set; }
        [Key("bar")]
        public int Bar { get; set; }
    }


    [MessagePackObject]
    public class IntKeySample
    {
        [Key(3)]
        public int A { get; set; }
        [Key(10)]
        public int B { get; set; }
    }



    public class ContractlessSample
    {
        public int MyProperty1 { get; set; }
        public int MyProperty2 { get; set; }
    }

    [MessagePackObject]
    public class SampleCallback : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int Key { get; set; }

        public void OnBeforeSerialize()
        {
            Console.WriteLine("OnBefore");
        }

        public void OnAfterDeserialize()
        {
            Console.WriteLine("OnAfter");
        }
    }



    [MessagePackObject]
    public struct Point
    {
        [Key(0)]
        public readonly int X;
        [Key(1)]
        public readonly int Y;

        // can't find matched constructor parameter, parameterType mismatch. type:Point parameterIndex:0 paramterType:ValueTuple`2
        public Point((int, int) p)
        {
            X = p.Item1;
            Y = p.Item2;
        }

        [SerializationConstructor]
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    // mark inheritance types
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


    [MessagePackFormatter(typeof(CustomObjectFormatter))]
    public class CustomObject
    {
        string internalId;

        public CustomObject()
        {
            this.internalId = Guid.NewGuid().ToString();
        }

        // serialize/deserialize internal field.
        class CustomObjectFormatter : IMessagePackFormatter<CustomObject>
        {
            public int Serialize(ref byte[] bytes, int offset, CustomObject value, IFormatterResolver formatterResolver)
            {
                return formatterResolver.GetFormatterWithVerify<string>().Serialize(ref bytes, offset, value.internalId, formatterResolver);
            }

            public CustomObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                var id = formatterResolver.GetFormatterWithVerify<string>().Deserialize(bytes, offset, formatterResolver, out readSize);
                return new CustomObject { internalId = id };
            }
        }
    }

    public interface IEntity
    {
        string Name { get; }
    }

    public class Event : IEntity
    {
        public Event(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class Holder
    {
        public Holder(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }



    class Program
    {
        static  void Main(string[] args)
        {

        



        }

        static void Benchmark<T>(T target)
        {
            const int Iteration = 10000; // 10000

            var jsonSerializer = new JsonSerializer();
            var msgpack = MsgPack.Serialization.SerializationContext.Default;
            msgpack.GetSerializer<T>().PackSingleObject(target);
            MessagePack.MessagePackSerializer.Serialize(target);
            LZ4MessagePackSerializer.Serialize(target);
            ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
            ProtoBuf.Serializer.Serialize(new MemoryStream(), target);
            jsonSerializer.Serialize(new JsonTextWriter(new StringWriter()), target);


            Console.WriteLine(typeof(T).Name + " serialization test");
            Console.WriteLine();

            Console.WriteLine("Serialize::");
            byte[] data = null;
            byte[] data0 = null;
            byte[] data1 = null;
            byte[] data2 = null;
            byte[] data3 = null;
            byte[] dataJson = null;
            byte[] dataGzipJson = null;
            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data = msgpack.GetSerializer<T>().PackSingleObject(target);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data0 = MessagePack.MessagePackSerializer.Serialize(target);
                }
            }

            using (new Measure("MessagePack(LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data3 = LZ4MessagePackSerializer.Serialize(target);
                }
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data1 = ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
                }
            }

            using (new Measure("JsonNet"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            using (new Measure("JsonNet+Gzip"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                    using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize(ms, target);
                    }
                }
            }

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, target);
                data2 = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }
                dataJson = ms.ToArray();
            }
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }
                dataGzipJson = ms.ToArray();
            }


            msgpack.GetSerializer<T>().UnpackSingleObject(data);
            MessagePack.MessagePackSerializer.Deserialize<T>(data0);
            ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data2));
            LZ4MessagePackSerializer.Deserialize<T>(data3);
            jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(dataJson))));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    msgpack.GetSerializer<T>().UnpackSingleObject(data);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePack.MessagePackSerializer.Deserialize<T>(data0);
                }
            }

            using (new Measure("MessagePack(LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    LZ4MessagePackSerializer.Deserialize<T>(data3);
                }
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ZeroFormatterSerializer.Deserialize<T>(data1);
                }
            }

            using (new Measure("JsonNet"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(dataJson))
                    using (var sr = new StreamReader(ms, Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                    {
                        jsonSerializer.Deserialize<T>(jr);
                    }
                }
            }

            using (new Measure("JsonNet+Gzip"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(dataGzipJson))
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                    using (var sr = new StreamReader(gzip, Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                    {
                        jsonSerializer.Deserialize<T>(jr);
                    }
                }
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(data2))
                    {
                        ProtoBuf.Serializer.Deserialize<T>(ms);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("FileSize::");
            var label = "";
            label = "MsgPack-Cli"; Console.WriteLine($"{label,20}   {data.Length} Byte");
            label = "MessagePack-CSharp"; Console.WriteLine($"{label,20}   {data0.Length} Byte");
            label = "MessagePack(LZ4)"; Console.WriteLine($"{label,20}   {data3.Length} Byte");
            label = "ZeroFormatter"; Console.WriteLine($"{label,20}   {data1.Length} Byte");
            label = "protobuf-net"; Console.WriteLine($"{label,20}   {data2.Length} Byte");
            label = "JsonNet"; Console.WriteLine($"{label,20}   {dataJson.Length} Byte");
            label = "JsonNet+GZip"; Console.WriteLine($"{label,20}   {dataGzipJson.Length} Byte");

            Console.WriteLine();
            Console.WriteLine();
        }


        static string ToHumanReadableSize(long size)
        {
            return ToHumanReadableSize(new Nullable<long>(size));
        }

        static string ToHumanReadableSize(long? size)
        {
            if (size == null) return "NULL";

            double bytes = size.Value;

            if (bytes <= 1024) return bytes.ToString("f2") + " B";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " KB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " MB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " GB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " TB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " PB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " EB";

            bytes = bytes / 1024;
            return bytes + " ZB";
        }
    }

    struct Measure : IDisposable
    {
        string label;
        Stopwatch sw;

        public Measure(string label)
        {
            this.label = label;
            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            this.sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            sw.Stop();
            Console.WriteLine($"{ label,20}   {sw.Elapsed.TotalMilliseconds} ms");

            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
    }



    public class SerializerTarget
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

    // design concept sketch of Union.

    [MessagePack.Union(0, typeof(HogeMoge1))]
    [MessagePack.Union(1, typeof(HogeMoge2))]
    public interface IHogeMoge
    {
    }

    public class HogeMoge1
    {
    }

    public class HogeMoge2
    {
    }



    [MessagePackObject]
    public class TestObject
    {
        [MessagePackObject]
        public class PrimitiveObject
        {
            [Key(0)]
            public int v_int;

            [Key(1)]
            public string v_str;

            [Key(2)]
            public float v_float;

            [Key(3)]
            public bool v_bool;
            public PrimitiveObject(int vi, string vs, float vf, bool vb)
            {
                v_int = vi; v_str = vs; v_float = vf; v_bool = vb;
            }
        }

        [Key(0)]
        public PrimitiveObject[] objectArray;

        [Key(1)]
        public List<PrimitiveObject> objectList;

        [Key(2)]
        public Dictionary<string, PrimitiveObject> objectMap;

        public void CreateArray(int num)
        {
            objectArray = new PrimitiveObject[num];
            for (int i = 0; i < num; i++)
            {
                objectArray[i] = new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false);
            }
        }

        public void CreateList(int num)
        {
            objectList = new List<PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                objectList.Add(new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }

        public void CreateMap(int num)
        {
            objectMap = new Dictionary<string, PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                objectMap.Add(i.ToString(), new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }
        // I only tested with array
        public static TestObject TestBuild()
        {
            TestObject to = new TestObject();
            to.CreateArray(1000000);

            return to;
        }
    }

    public class HogeMogeFormatter : IMessagePackFormatter<IHogeMoge>
    {
        // Type to Key...
        static readonly Dictionary<Type, KeyValuePair<int, int>> map = new Dictionary<Type, KeyValuePair<int, int>>
        {
            {typeof(HogeMoge1), new KeyValuePair<int,int>(0,0) },
            {typeof(HogeMoge2), new KeyValuePair<int,int>(1,1) },
        };

        // If 0~10 don't need it.
        static readonly Dictionary<int, int> keyToJumpTable = new Dictionary<int, int>
        {
            {0, 0 },
            {1, 1 },
        };

        public int Serialize(ref byte[] bytes, int offset, IHogeMoge value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;

            KeyValuePair<int, int> key;
            if (map.TryGetValue(value.GetType(), out key))
            {
                var headerLen = MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
                offset += headerLen;
                var keyLength = MessagePackBinary.WriteInt32(ref bytes, offset, key.Key);
                headerLen += keyLength;

                switch (key.Value)
                {
                    case 0:
                        offset += formatterResolver.GetFormatterWithVerify<HogeMoge1>().Serialize(ref bytes, offset, (HogeMoge1)value, formatterResolver);
                        break;
                    case 1:
                        offset += formatterResolver.GetFormatterWithVerify<HogeMoge2>().Serialize(ref bytes, offset, (HogeMoge2)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return offset - startOffset;
            }

            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public IHogeMoge Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            // TODO:array header...

            int keySize;
            int valueSize;
            var key = MessagePackBinary.ReadInt32(bytes, offset, out keySize);

            switch (key)
            {
                case 0:
                    {
                        var result = formatterResolver.GetFormatterWithVerify<HogeMoge1>().Deserialize(bytes, offset + keySize, formatterResolver, out valueSize);
                        readSize = keySize + valueSize;
                        return (IHogeMoge)result;
                    }
                case 1:
                    {
                        var result = formatterResolver.GetFormatterWithVerify<HogeMoge2>().Deserialize(bytes, offset + keySize, formatterResolver, out valueSize);
                        readSize = keySize + valueSize;
                        return (IHogeMoge)result;
                    }
                default:
                    {

                        throw new NotImplementedException();
                    }
            }

        }
    }

}
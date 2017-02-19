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

namespace Sandbox
{
    public enum MyEnum
    {
        Apple, Orange, Pineapple
    }
    [MessagePackObject]
    public class EmptyClass
    {

    }

    [MessagePackObject]
    public struct EmptyStruct
    {

    }

    [ZeroFormattable]
    [ProtoBuf.ProtoContract]
    [MessagePackObject(true)]
    public class SmallSingleObject
    {
        [Index(0)]
        [Key(0)]
        [ProtoBuf.ProtoMember(1)]
        public virtual int MyProperty { get; set; }
        [Index(1)]
        [Key(1)]
        [ProtoBuf.ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }
        [Index(2)]
        [Key(2)]
        [ProtoBuf.ProtoMember(3)]
        public virtual MyEnum MyProperty3 { get; set; }
    }

    [MessagePackObject]
    public struct MyStruct
    {
        [Key(0)]
        public byte[] MyProperty { get; set; }
    }


    [MessagePackObject(true)]
    public class NewObj
    {
        [Key(0)]
        public int MyProperty { get; private set; }
        public NewObj(int myProperty)
        {
            this.MyProperty = myProperty;
        }
    }

    [MessagePackObject(true)]
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


    class SlowCustomResolver : IFormatterResolver
    {
        Dictionary<Type, object> formatters = new Dictionary<Type, object>
        {
            { typeof(FirstSimpleData), new FirstSimpleDataFormatter() },
            { typeof(Version0), new Version0Formatter() },
            { typeof(Version1), new Version1Formatter() },
            { typeof(Version2), new Version2Formatter() },
        };

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            var f = BuiltinResolver.Instance.GetFormatter<T>();
            if (f != null) return f;
            return (IMessagePackFormatter<T>)formatters[typeof(T)];
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var p = new Person
            {
                Age = 99999,
                FirstName = "Windows",
                LastName = "Server",
                Sex = Sex.Male,
            };
            Person[] l = Enumerable.Range(1, 100).Select(x => new Person { Age = x, FirstName = "Windows", LastName = "Server", Sex = Sex.Female }).ToArray();

            //Benchmark(p);
            //Console.WriteLine();
            //Benchmark(l);


            var test = MessagePack.MessagePackSerializer.Serialize(new FirstSimpleData { Prop1 = 9, Prop3 = 300, Prop2 = "fdasfa" }, new SlowCustomResolver());
            Console.WriteLine(MessagePack.MessagePackSerializer.ToJson(test));



            var resolver = new SlowCustomResolver();
            MessagePackSerializer.SetDefaultResolver(resolver);

            var v1 = new Version1
            {
                MyProperty1 = 100,
                MyProperty2 = 200,
                MyProperty3 = 300
            };

            var v2 = new Version2
            {
                MyProperty1 = 100,
                MyProperty2 = 200,
                MyProperty3 = 300,
                MyProperty4 = 400,
                MyProperty5 = 500,
            };

            var v0 = new Version0
            {
                MyProperty1 = 100,
            };

            var v1Bytes = MessagePackSerializer.Serialize(v1);
            var v2Bytes = MessagePackSerializer.Serialize(v2);
            var v0Bytes = MessagePackSerializer.Serialize(v0);

            var a = MessagePackSerializer.ToJson(v1Bytes);
            var b = MessagePackSerializer.ToJson(v2Bytes);
            var c = MessagePackSerializer.ToJson(v0Bytes);


            // smaller than schema
            var v2_ = MessagePackSerializer.Deserialize<Version2>(v1Bytes);
 

            // larger than schema

            var v0_ = MessagePackSerializer.Deserialize<Version0>(v1Bytes);



            // smaller than schema
            var v2_default = MessagePackSerializer.Deserialize<Version2>(v1Bytes, DefaultResolver.Instance);


            // larger than schema

            var v0_default = MessagePackSerializer.Deserialize<Version0>(v1Bytes, DefaultResolver.Instance);

        }

        static void Benchmark<T>(T target)
        {
            var msgpack = MsgPack.Serialization.SerializationContext.Default;
            msgpack.GetSerializer<T>().PackSingleObject(target);
            MessagePack.MessagePackSerializer.Serialize(target);
            ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
            ProtoBuf.Serializer.Serialize(new MemoryStream(), target);

            Console.WriteLine(typeof(T).Name + " serialization test");
            Console.WriteLine();

            Console.WriteLine("Serialize::");
            byte[] data = null;
            byte[] data0 = null;
            byte[] data1 = null;
            byte[] data2 = null;
            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    data = msgpack.GetSerializer<T>().PackSingleObject(target);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    data0 = MessagePack.MessagePackSerializer.Serialize(target);
                }
            }
            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    data1 = ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
                }
            }
            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    using (var ms = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize(ms, target);
                        data2 = ms.ToArray();
                    }
                }
            }

            msgpack.GetSerializer<T>().UnpackSingleObject(data);
            MessagePack.MessagePackSerializer.Deserialize<T>(data0);
            ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data2));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    msgpack.GetSerializer<T>().UnpackSingleObject(data);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    MessagePack.MessagePackSerializer.Deserialize<T>(data0);
                }
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    ZeroFormatterSerializer.Deserialize<T>(data1);
                }
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < 100000; i++)
                {
                    using (var ms = new MemoryStream(data2))
                    {
                        ProtoBuf.Serializer.Deserialize<T>(ms);
                    }
                }
            }
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

    public class HandwriteMyClassFormatter : IMessagePackFormatter<SmallSingleObject>
    {
        readonly Dictionary<string, int> keyMapping;

        public HandwriteMyClassFormatter()
        {
            keyMapping = new Dictionary<string, int>(5);
            keyMapping.Add("hoge", 100);
        }

        public int Serialize(ref byte[] bytes, int offset, SmallSingleObject value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;

            bytes[offset] = 0;
            bytes[offset + 1] = 1;
            bytes[offset + 2] = 2;
            offset += 3;


            offset += MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2); // optimize 0~15 count
            offset += MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0); // optimize 0~127 key.
            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty, formatterResolver);
            offset += MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1); // optimize 0~127 key.
            offset += Int32Formatter.Instance.Serialize(ref bytes, offset, value.MyProperty2);

            return offset - startOffset;
        }

        public SmallSingleObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var length = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            int __MyProperty1__ = default(int);
            int __MyProperty2__ = default(int);

            // pattern of integer key.
            for (int i = 0; i < length; i++)
            {
                // 
                //var stringKey = "hogehoge";
                //int key;
                //if (keyMapping.TryGetValue(stringKey, out key))
                //{
                //    offset += readSize;
                //}


                var key = Int32Formatter.Instance.Deserialize(bytes, offset, out readSize);
                offset += readSize;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = Int32Formatter.Instance.Deserialize(bytes, offset, out readSize);
                        break;
                    case 1:
                        __MyProperty2__ = Int32Formatter.Instance.Deserialize(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }

                offset += readSize;
            }

            // finish readSize
            readSize = offset - startOffset;

            var __result__ = new SmallSingleObject(); // use constructor(with argument?)
            __result__.MyProperty = __MyProperty1__;
            __result__.MyProperty2 = __MyProperty2__;
            return __result__;
        }
    }


    public class HandWriteResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new HandWriteResolver();

        HandWriteResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(SmallSingleObject))
                {
                    formatter = (IMessagePackFormatter<T>)(object)new HandwriteMyClassFormatter();
                    return;
                }

                // fallback default.
                formatter = DefaultResolver.Instance.GetFormatter<T>();
            }
        }
    }
}
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

            Benchmark(p);
            Console.WriteLine();
            Benchmark(l);

            //var json = MessagePackSerializer.ToJson(MessagePackSerializer.NonGeneric.Serialize(typeof(Person), p));
            //Console.WriteLine(json);

            //var huga = MessagePackSerializer.Serialize(ushort.MaxValue);



            //var huga = MessagePackSerializer.Serialize(a);
            //var l2 = Enumerable.Range(1, 10).ToLookup(x => x % 2 == 0);
            //var b = MessagePackSerializer.Serialize(l2);
            //MessagePackSerializer.Deserialize<ILookup<bool, int>>(b);
            //Console.WriteLine(MessagePackSerializer.ToJson(b));

            Console.WriteLine("Press key to exit.");
            Console.ReadLine();
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
}
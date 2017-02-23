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

            //Benchmark(p);
            //Console.WriteLine();
            //Benchmark(l);

            //var json = MessagePackSerializer.ToJson(MessagePackSerializer.NonGeneric.Serialize(typeof(Person), p));
            //Console.WriteLine(json);

            var t = new Tuple<int, int, int>[3, 4, 5];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        t[i, j, k] = Tuple.Create(i, j, k);
                    }
                }
            }
            var hoge = MessagePackSerializer.Deserialize<Tuple<int, int, int>[,,]>(MessagePackSerializer.Serialize(t));

            Console.WriteLine(hoge);

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
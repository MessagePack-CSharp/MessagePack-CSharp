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


    [MessagePackObject()]
    public class Takox
    {
        public int MyProperty { get; set; }
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
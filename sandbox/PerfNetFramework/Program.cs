using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using ProtoBuf;
using ZeroFormatter;

namespace PerfnetFramework
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

    internal class Program
    {
        private static void Main(string[] args)
        {
            var p = new Person
            {
                Age = 99999,
                FirstName = "Windows",
                LastName = "Server",
                Sex = Sex.Male,
            };

            var rand = new Random(100);
            Person[] l = Enumerable.Range(1, 100)
                .Select(x => new Person
                {
                    Age = x,
                    FirstName = "Windows",
                    LastName = "Server",
                    Sex = (Sex)rand.Next(0, 3)
                })
                .ToArray();

            Benchmark(p);
            Console.WriteLine();
            Benchmark(l);
        }

        internal static readonly MessagePack.MessagePackSerializer DefaultSerializer = new MessagePackSerializer();
        internal static readonly MessagePack.LZ4MessagePackSerializer LZ4Serializer = new LZ4MessagePackSerializer();

        private static void Benchmark<T>(T target)
        {
            const int Iteration = 10000;
            Console.WriteLine("Running {0} iterations...", Iteration);

            var jsonSerializer = new JsonSerializer();
            var msgpack = MsgPack.Serialization.SerializationContext.Default;
            msgpack.GetSerializer<T>().PackSingleObject(target);
            DefaultSerializer.Serialize(target);
            LZ4Serializer.Serialize(target);
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

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data0 = DefaultSerializer.Serialize(target);
                }
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data3 = LZ4Serializer.Serialize(target);
                }
            }

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data = msgpack.GetSerializer<T>().PackSingleObject(target);
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

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data1 = ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
                }
            }

            using (new Measure("Json.NET"))
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

            using (new Measure("Json.NET(+GZip)"))
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
            DefaultSerializer.Deserialize<T>(data0);
            //ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data2));
            LZ4Serializer.Deserialize<T>(data3);
            jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(dataJson))));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    DefaultSerializer.Deserialize<T>(data0);
                }
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    LZ4Serializer.Deserialize<T>(data3);
                }
            }

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    msgpack.GetSerializer<T>().UnpackSingleObject(data);
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

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ZeroFormatterSerializer.Deserialize<T>(data1);
                }
            }

            using (new Measure("Json.NET"))
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

            using (new Measure("Json.NET(+GZip)"))
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

            Console.WriteLine();
            Console.WriteLine("FileSize::");
            var label = "";
            label = "MessagePack for C#"; Console.WriteLine($"{label,-25} {data0.Length,14} byte");
            label = "MessagePack for C# (LZ4)"; Console.WriteLine($"{label,-25} {data3.Length,14} byte");
            label = "MsgPack-Cli"; Console.WriteLine($"{label,-25} {data.Length,14} byte");
            label = "protobuf-net"; Console.WriteLine($"{label,-25} {data2.Length,14} byte");
            label = "ZeroFormatter"; Console.WriteLine($"{label,-25} {data1.Length,14} byte");
            label = "Json.NET"; Console.WriteLine($"{label,-25} {dataJson.Length,14} byte");
            label = "Json.NET(+GZip)"; Console.WriteLine($"{label,-25} {dataGzipJson.Length,14} byte");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static string ToHumanReadableSize(long size)
        {
            return ToHumanReadableSize(new Nullable<long>(size));
        }

        private static string ToHumanReadableSize(long? size)
        {
            if (size == null)
            {
                return "NULL";
            }

            double bytes = size.Value;

            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " B";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " KB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " MB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " GB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " TB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " PB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " EB";
            }

            bytes = bytes / 1024;
            return bytes + " ZB";
        }
    }

    internal struct Measure : IDisposable
    {
        private string label;
        private Stopwatch sw;

        public Measure(string label)
        {
            this.label = label;
            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            this.sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            sw.Stop();
            Console.WriteLine($"{label,-25}   {sw.Elapsed.TotalMilliseconds,12:F2} ms");

            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
    }
}
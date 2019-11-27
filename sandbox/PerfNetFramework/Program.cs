// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using ZeroFormatter;

namespace PerfNetFramework
{
    internal class Program
    {
        private static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        internal static bool Deserializing { get; set; }

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
                    Sex = (Sex)rand.Next(0, 3),
                })
                .ToArray();

            BenchmarkEventSource.Instance.Session(1);
            Benchmark(p);
            BenchmarkEventSource.Instance.SessionEnd();

            Console.WriteLine();
            BenchmarkEventSource.Instance.Session(l.Length);
            Benchmark(l);
            BenchmarkEventSource.Instance.SessionEnd();
        }

        private static void Benchmark<T>(T target)
        {
            const int Iteration = 10000;
            Console.WriteLine("Running {0} iterations...", Iteration);

            var jsonSerializer = new JsonSerializer();
            MsgPack.Serialization.SerializationContext msgpack = MsgPack.Serialization.SerializationContext.Default;
            msgpack.GetSerializer<T>().PackSingleObject(target);
            MessagePackSerializer.Serialize(target);
            MessagePackSerializer.Serialize(target, LZ4Standard);
            ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
            ProtoBuf.Serializer.Serialize(new MemoryStream(), target);
            jsonSerializer.Serialize(new JsonTextWriter(new StringWriter()), target);

            Console.WriteLine(typeof(T).Name + " serialization test");
            Console.WriteLine();

            Console.WriteLine("Serialize::");
            Deserializing = false;

            byte[] data = null;
            var data0 = new Nerdbank.Streams.Sequence<byte>();
            byte[] data1 = null;
            byte[] data2 = null;
            byte[] data3 = null;
            byte[] dataJson = null;
            byte[] dataGzipJson = null;

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data0.Reset();
                    MessagePackSerializer.Serialize(data0, target);
                }
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data3 = MessagePackSerializer.Serialize(target, LZ4Standard);
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
            MessagePackSerializer.Deserialize<T>(data0);
            ////ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data2));
            MessagePackSerializer.Deserialize<T>(data3, LZ4Standard);
            jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(dataJson))));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");
            Deserializing = true;

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(data0);
                }
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(data3, LZ4Standard);
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
            var label = string.Empty;
            label = "MessagePack for C#";
            Console.WriteLine($"{label,-25} {data0.Length,14} byte");
            label = "MessagePack for C# (LZ4)";
            Console.WriteLine($"{label,-25} {data3.Length,14} byte");
            label = "MsgPack-Cli";
            Console.WriteLine($"{label,-25} {data.Length,14} byte");
            label = "protobuf-net";
            Console.WriteLine($"{label,-25} {data2.Length,14} byte");
            label = "ZeroFormatter";
            Console.WriteLine($"{label,-25} {data1.Length,14} byte");
            label = "Json.NET";
            Console.WriteLine($"{label,-25} {dataJson.Length,14} byte");
            label = "Json.NET(+GZip)";
            Console.WriteLine($"{label,-25} {dataGzipJson.Length,14} byte");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static string ToHumanReadableSize(long size)
        {
            return ToHumanReadableSize(new long?(size));
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
}

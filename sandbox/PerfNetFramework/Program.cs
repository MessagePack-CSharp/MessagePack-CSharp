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
            byte[] msgpackCliData = msgpack.GetSerializer<T>().PackSingleObject(target);
            byte[] messagePackData = MessagePackSerializer.Serialize(target);
            byte[] messagePackLZ4Data = MessagePackSerializer.Serialize(target, LZ4Standard);
            byte[] zeroFormatterData = ZeroFormatterSerializer.Serialize(target);
            MemoryStream ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, target);
            byte[] protobufData = ms.ToArray();
#if NETCOREAPP3_1
            byte[] dataOdin = OdinSerialize();
#endif
            jsonSerializer.Serialize(new JsonTextWriter(new StringWriter()), target);

            Console.WriteLine(typeof(T).Name + " serialization test");
            Console.WriteLine();

            Console.WriteLine("Serialize::");
            Deserializing = false;

            byte[] dataJson = null;
            byte[] dataGzipJson = null;

            // prime the CPU
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(target);
            }

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Serialize(target);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(target, LZ4Standard);
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Serialize(target, LZ4Standard);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                msgpack.GetSerializer<T>().PackSingleObject(target);
            }

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    msgpack.GetSerializer<T>().PackSingleObject(target);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                ms.SetLength(0);
                ProtoBuf.Serializer.Serialize(ms, target);
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ms.SetLength(0);
                    ProtoBuf.Serializer.Serialize(ms, target);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                ZeroFormatterSerializer.Serialize(target);
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ZeroFormatterSerializer.Serialize(target);
                }
            }

#if NETCOREAPP3_1
            for (int i = 0; i < Iteration; i++)
            {
                OdinSerialize();
            }

            using (new Measure("OdinSerializer"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    OdinSerialize();
                }
            }
#endif

            for (int i = 0; i < Iteration; i++)
            {
                ms.SetLength(0);
                using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }
            }

            using (new Measure("Json.NET"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ms.SetLength(0);
                    using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                ms.SetLength(0);
                using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, leaveOpen: true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }
            }

            using (new Measure("Json.NET(+GZip)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ms.SetLength(0);
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                    using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, leaveOpen: true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            ms.SetLength(0);
            ProtoBuf.Serializer.Serialize(ms, target);

            ms.SetLength(0);
            using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true))
            using (var jw = new JsonTextWriter(sw))
            {
                jsonSerializer.Serialize(jw, target);
            }

            dataJson = ms.ToArray();

            ms.SetLength(0);
            using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, leaveOpen: true))
            using (var jw = new JsonTextWriter(sw))
            {
                jsonSerializer.Serialize(jw, target);
            }

            dataGzipJson = ms.ToArray();

            msgpack.GetSerializer<T>().UnpackSingleObject(msgpackCliData);
            MessagePackSerializer.Deserialize<T>(messagePackData);
            ////ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(protobufData));
#if NETCOREAPP3_1
            OdinDeserialize(dataOdin);
#endif
            MessagePackSerializer.Deserialize<T>(messagePackLZ4Data, LZ4Standard);
            jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(dataJson))));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");
            Deserializing = true;

            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<T>(messagePackData);
            }

            using (new Measure("MessagePack for C#"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(messagePackData);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<T>(messagePackLZ4Data, LZ4Standard);
            }

            using (new Measure("MessagePack for C# (LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(messagePackLZ4Data, LZ4Standard);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                msgpack.GetSerializer<T>().UnpackSingleObject(msgpackCliData);
            }

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    msgpack.GetSerializer<T>().UnpackSingleObject(msgpackCliData);
                }
            }

            ms.SetLength(0);
            ms.Write(protobufData, 0, protobufData.Length);
            for (int i = 0; i < Iteration; i++)
            {
                ms.Position = 0;
                ProtoBuf.Serializer.Deserialize<T>(ms);
            }

            using (new Measure("protobuf-net"))
            {
                ms.SetLength(0);
                ms.Write(protobufData, 0, protobufData.Length);
                for (int i = 0; i < Iteration; i++)
                {
                    ms.Position = 0;
                    ProtoBuf.Serializer.Deserialize<T>(ms);
                }
            }

            for (int i = 0; i < Iteration; i++)
            {
                ZeroFormatterSerializer.Deserialize<T>(zeroFormatterData);
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ZeroFormatterSerializer.Deserialize<T>(zeroFormatterData);
                }
            }

#if NETCOREAPP3_1
            for (int i = 0; i < Iteration; i++)
            {
                OdinDeserialize(dataOdin);
            }

            using (new Measure("OdinSerializer"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    OdinDeserialize(dataOdin);
                }
            }
#endif

            ms.SetLength(0);
            ms.Write(dataJson, 0, dataJson.Length);
            for (int i = 0; i < Iteration; i++)
            {
                ms.Position = 0;
                using (var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                using (var jr = new JsonTextReader(sr))
                {
                    jsonSerializer.Deserialize<T>(jr);
                }
            }

            using (new Measure("Json.NET"))
            {
                ms.SetLength(0);
                ms.Write(dataJson, 0, dataJson.Length);
                for (int i = 0; i < Iteration; i++)
                {
                    ms.Position = 0;
                    using (var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                    using (var jr = new JsonTextReader(sr))
                    {
                        jsonSerializer.Deserialize<T>(jr);
                    }
                }
            }

            ms.SetLength(0);
            ms.Write(dataGzipJson, 0, dataGzipJson.Length);
            for (int i = 0; i < Iteration; i++)
            {
                ms.Position = 0;
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
                using (var sr = new StreamReader(gzip, Encoding.UTF8))
                using (var jr = new JsonTextReader(sr))
                {
                    jsonSerializer.Deserialize<T>(jr);
                }
            }

            using (new Measure("Json.NET(+GZip)"))
            {
                ms.SetLength(0);
                ms.Write(dataGzipJson, 0, dataGzipJson.Length);
                for (int i = 0; i < Iteration; i++)
                {
                    ms.Position = 0;
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
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
            Console.WriteLine($"{label,-25} {messagePackData.Length,14} byte");
            label = "MessagePack for C# (LZ4)";
            Console.WriteLine($"{label,-25} {messagePackLZ4Data.Length,14} byte");
            label = "MsgPack-Cli";
            Console.WriteLine($"{label,-25} {msgpackCliData.Length,14} byte");
            label = "protobuf-net";
            Console.WriteLine($"{label,-25} {protobufData.Length,14} byte");
            label = "ZeroFormatter";
            Console.WriteLine($"{label,-25} {zeroFormatterData.Length,14} byte");
#if NETCOREAPP3_1
            label = "OdinSerializer";
            Console.WriteLine($"{label,-25} {dataOdin.Length,14} byte");
#endif
            label = "Json.NET";
            Console.WriteLine($"{label,-25} {dataJson.Length,14} byte");
            label = "Json.NET(+GZip)";
            Console.WriteLine($"{label,-25} {dataGzipJson.Length,14} byte");

            Console.WriteLine();
            Console.WriteLine();

#if NETCOREAPP3_1
            byte[] OdinSerialize()
            {
                using (var ctx = OdinSerializer.Utilities.Cache<OdinSerializer.SerializationContext>.Claim())
                {
                    ctx.Value.Config.SerializationPolicy = OdinSerializer.SerializationPolicies.Everything;
                    return OdinSerializer.SerializationUtility.SerializeValue(target, OdinSerializer.DataFormat.Binary, ctx.Value);
                }
            }

            T OdinDeserialize(byte[] input)
            {
                using (var ctx = OdinSerializer.Utilities.Cache<OdinSerializer.DeserializationContext>.Claim())
                {
                    ctx.Value.Config.SerializationPolicy = OdinSerializer.SerializationPolicies.Everything;
                    return OdinSerializer.SerializationUtility.DeserializeValue<T>(input, OdinSerializer.DataFormat.Binary, ctx.Value);
                }
            }
#endif
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

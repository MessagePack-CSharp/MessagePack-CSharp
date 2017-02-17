using MessagePack;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Diagnostics;
using System.IO;
using ZeroFormatter;

namespace Sandbox
{
    public enum MyEnum
    {
        Apple, Orange, Pineapple
    }

    [ZeroFormattable]
    [ProtoBuf.ProtoContract]
    public class MyClass
    {
        [Index(0)]
        [ProtoBuf.ProtoMember(1)]
        public virtual int MyProperty { get; set; }
        [Index(1)]
        [ProtoBuf.ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //MessagePack.MessagePackSerializer.SetDefaultResolver(HandWriteResolver.Instance);

            //var bin = MessagePackSerializer.Serialize(new MyClass() { MyProperty = 100, MyProperty2 = 999 });
            //var json = MessagePackSerializer.ToJson(bin);

            // var target = new MyClass() { MyProperty = 9, MyProperty2 = 100 };

            //var bytes = Enumerable.Repeat(1, 30000).Select(x => (byte)x).ToArray();
            //Benchmark(bytes);

            var dt = new DateTime(2030, 2, 7, 6, 28, 17, 0, DateTimeKind.Utc);
            byte[] bytes = null;
            MessagePackBinary.WriteDateTime(ref bytes, 0, dt);
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

    public class HandwriteMyClassFormatter : IMessagePackFormatter<MyClass>
    {
        public int Serialize(ref byte[] bytes, int offset, MyClass value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2); // optimize 0~15 count
            offset += MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0); // optimize 0~127 key.
            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty, formatterResolver);
            offset += MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1); // optimize 0~127 key.
            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty2, formatterResolver);

            return offset - startOffset;
        }

        public MyClass Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var intKeyFormatter = formatterResolver.GetFormatterWithVerify<int>();
            var length = MessagePackBinary.ReadMapHeaderRaw(bytes, offset, out readSize);
            offset += readSize;

            int __MyProperty1__ = default(int);
            int __MyProperty2__ = default(int);

            // pattern of integer key.
            for (int i = 0; i < length; i++)
            {
                var key = intKeyFormatter.Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __MyProperty2__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        break;
                }

                offset += readSize;
            }

            // pattern of string key
            // TODO:Dictionary Switch... Dictionary<string, int> and use same above code...

            // finish readSize
            readSize = offset - startOffset;

            var __result__ = new MyClass(); // use constructor(with argument?)
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
                // Try Builtin
                var f = BuiltinResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    formatter = f;
                    return;
                }

                // Try Enum
                f = EnumResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    formatter = f;
                    return;
                }

                if (typeof(T) == typeof(MyClass))
                {
                    formatter = (IMessagePackFormatter<T>)(object)new HandwriteMyClassFormatter();
                }

                // Try Union

                // Try Dynamic
                // Unknown
            }
        }
    }
}
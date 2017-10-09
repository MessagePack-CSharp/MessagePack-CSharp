using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackFormatterPerFieldTest
    {
        [MessagePackObject]
        public class MyClass
        {
            [Key(0)]
            [MessagePackFormatter(typeof(Int_x10Formatter))]
            public int MyProperty1 { get; set; }
            [Key(1)]
            public int MyProperty2 { get; set; }
            [Key(2)]
            [MessagePackFormatter(typeof(String_x2Formatter))]
            public string MyProperty3 { get; set; }
            [Key(3)]
            public string MyProperty4 { get; set; }
        }

        [MessagePackObject]
        public struct MyStruct
        {
            [Key(0)]
            [MessagePackFormatter(typeof(Int_x10Formatter))]
            public int MyProperty1 { get; set; }
            [Key(1)]
            public int MyProperty2 { get; set; }
            [Key(2)]
            [MessagePackFormatter(typeof(String_x2Formatter))]
            public string MyProperty3 { get; set; }
            [Key(3)]
            public string MyProperty4 { get; set; }
        }

        public class Int_x10Formatter : IMessagePackFormatter<int>
        {
            public int Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                return MessagePackBinary.ReadInt32(bytes, offset, out readSize) * 10;
            }

            public int Serialize(ref byte[] bytes, int offset, int value, IFormatterResolver formatterResolver)
            {
                return MessagePackBinary.WriteInt32(ref bytes, offset, value * 10);
            }
        }

        public class String_x2Formatter : IMessagePackFormatter<string>
        {
            public string Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                var s = MessagePackBinary.ReadString(bytes, offset, out readSize);
                return s + s;
            }

            public int Serialize(ref byte[] bytes, int offset, string value, IFormatterResolver formatterResolver)
            {
                return MessagePackBinary.WriteString(ref bytes, offset, value + value);
            }
        }


        [Fact]
        public void FooBar()
        {
            MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate
            {
                var bin = MessagePack.MessagePackSerializer.Serialize(new MyClass { MyProperty1 = 100, MyProperty2 = 9, MyProperty3 = "foo", MyProperty4 = "bar" });
                var json = MessagePackSerializer.ToJson(bin);
                json.Is("[1000,9,\"foofoo\",\"bar\"]");

                var r2 = MessagePackSerializer.Deserialize<MyClass>(bin);
                r2.MyProperty1.Is(10000);
                r2.MyProperty2.Is(9);
                r2.MyProperty3.Is("foofoofoofoo");
                r2.MyProperty4.Is("bar");
            }
            {
                var bin = MessagePack.MessagePackSerializer.Serialize(new MyStruct { MyProperty1 = 100, MyProperty2 = 9, MyProperty3 = "foo", MyProperty4 = "bar" });
                var json = MessagePackSerializer.ToJson(bin);
                json.Is("[1000,9,\"foofoo\",\"bar\"]");

                var r2 = MessagePackSerializer.Deserialize<MyStruct>(bin);
                r2.MyProperty1.Is(10000);
                r2.MyProperty2.Is(9);
                r2.MyProperty3.Is("foofoofoofoo");
                r2.MyProperty4.Is("bar");
            }
        }
    }
}

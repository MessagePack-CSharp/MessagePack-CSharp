// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
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
            public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                return reader.ReadInt32() * 10;
            }

            public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options)
            {
                writer.Write(value * 10);
            }
        }

        public class String_x2Formatter : IMessagePackFormatter<string>
        {
            public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var s = reader.ReadString();
                return s + s;
            }

            public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options)
            {
                writer.Write(value + value);
            }
        }

        [Fact]
        public void FooBar()
        {
            {
                var bin = MessagePackSerializer.Serialize(new MyClass { MyProperty1 = 100, MyProperty2 = 9, MyProperty3 = "foo", MyProperty4 = "bar" });
                var json = MessagePackSerializer.ConvertToJson(bin);
                json.Is("[1000,9,\"foofoo\",\"bar\"]");

                MyClass r2 = MessagePackSerializer.Deserialize<MyClass>(bin);
                r2.MyProperty1.Is(10000);
                r2.MyProperty2.Is(9);
                r2.MyProperty3.Is("foofoofoofoo");
                r2.MyProperty4.Is("bar");
            }

            {
                var bin = MessagePackSerializer.Serialize(new MyStruct { MyProperty1 = 100, MyProperty2 = 9, MyProperty3 = "foo", MyProperty4 = "bar" });
                var json = MessagePackSerializer.ConvertToJson(bin);
                json.Is("[1000,9,\"foofoo\",\"bar\"]");

                MyStruct r2 = MessagePackSerializer.Deserialize<MyStruct>(bin);
                r2.MyProperty1.Is(10000);
                r2.MyProperty2.Is(9);
                r2.MyProperty3.Is("foofoofoofoo");
                r2.MyProperty4.Is("bar");
            }
        }
    }
}

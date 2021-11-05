// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class MessagePackFormatterPerFieldTest
    {
        private readonly ITestOutputHelper logger;

        public MessagePackFormatterPerFieldTest(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

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

        [MessagePackObject]
        public class BlockFormattedIntegers
        {
            [Key(0)] [MessagePackFormatter(typeof(ForceByteBlockFormatter))] public byte UInt8Property { get; set; }

            [Key(1)] [MessagePackFormatter(typeof(ForceUInt16BlockFormatter))] public ushort UInt16Property { get; set; }

            [Key(2)] [MessagePackFormatter(typeof(ForceUInt32BlockFormatter))] public uint UInt32Property { get; set; }

            [Key(3)] [MessagePackFormatter(typeof(ForceUInt64BlockFormatter))] public ulong UInt64Property { get; set; }

            [Key(4)] [MessagePackFormatter(typeof(ForceSByteBlockFormatter))] public sbyte Int8Property { get; set; }

            [Key(5)] [MessagePackFormatter(typeof(ForceInt16BlockFormatter))] public short Int16Property { get; set; }

            [Key(6)] [MessagePackFormatter(typeof(ForceInt32BlockFormatter))] public int Int32Property { get; set; }

            [Key(7)] [MessagePackFormatter(typeof(ForceInt64BlockFormatter))] public long Int64Property { get; set; }

            [Key(8)] [MessagePackFormatter(typeof(NullableForceByteBlockFormatter))] public byte? NullableUInt8Property { get; set; }

            [Key(9)] [MessagePackFormatter(typeof(NullableForceUInt16BlockFormatter))] public ushort? NullableUInt16Property { get; set; }

            [Key(10)] [MessagePackFormatter(typeof(NullableForceUInt32BlockFormatter))] public uint? NullableUInt32Property { get; set; }

            [Key(11)] [MessagePackFormatter(typeof(NullableForceUInt64BlockFormatter))] public ulong? NullableUInt64Property { get; set; }

            [Key(12)] [MessagePackFormatter(typeof(NullableForceSByteBlockFormatter))] public sbyte? NullableInt8Property { get; set; }

            [Key(13)] [MessagePackFormatter(typeof(NullableForceInt16BlockFormatter))] public short? NullableInt16Property { get; set; }

            [Key(14)] [MessagePackFormatter(typeof(NullableForceInt32BlockFormatter))] public int? NullableInt32Property { get; set; }

            [Key(15)] [MessagePackFormatter(typeof(NullableForceInt64BlockFormatter))] public long? NullableInt64Property { get; set; }
        }

        /// <summary>
        /// Asserts that every formatter offers the public API required for use by
        /// <see cref="MessagePackFormatterAttribute"/>.
        /// </summary>
        [Fact]
        public void AllFormattersOfferAreAvailableViaAttribute()
        {
            var formatters = (from type in typeof(ForceByteBlockFormatter).Assembly.GetTypes()
                              where typeof(IMessagePackFormatter).IsAssignableFrom(type) && !type.IsAbstract
                              let ctor = GetDefaultConstructor(type)
                              where ctor is object // skip formatters that require special initialization.
                              select new { Type = type, DefaultConstructor = ctor }).ToList();
            this.logger.WriteLine("Found {0} applicable formatters to check.", formatters.Count);

            Assert.All(formatters, formatter => Assert.True(formatter.DefaultConstructor.IsPublic || UsesSingletonPattern(formatter.Type)));
        }

        [Fact]
        public void ForceBlockFormatters()
        {
            var block = new BlockFormattedIntegers
            {
                UInt8Property = 1,
                UInt16Property = 2,
                UInt32Property = 3,
                UInt64Property = 4,

                Int8Property = 1,
                Int16Property = 2,
                Int32Property = 3,
                Int64Property = 4,

                NullableUInt8Property = 1,
                NullableUInt16Property = 2,
                NullableUInt32Property = 3,
                NullableUInt64Property = 4,

                NullableInt8Property = 1,
                NullableInt16Property = 2,
                NullableInt32Property = 3,
                NullableInt64Property = 4,
            };
            byte[] packed = MessagePackSerializer.Serialize(block, MessagePackSerializerOptions.Standard);
            var reader = new MessagePackReader(packed);

            reader.ReadArrayHeader();

            Assert.Equal(MessagePackCode.UInt8, reader.NextCode);
            Assert.Equal(1, reader.ReadByte());
            Assert.Equal(MessagePackCode.UInt16, reader.NextCode);
            Assert.Equal(2, reader.ReadUInt16());
            Assert.Equal(MessagePackCode.UInt32, reader.NextCode);
            Assert.Equal(3u, reader.ReadUInt32());
            Assert.Equal(MessagePackCode.UInt64, reader.NextCode);
            Assert.Equal(4u, reader.ReadUInt64());

            Assert.Equal(MessagePackCode.Int8, reader.NextCode);
            Assert.Equal(1, reader.ReadSByte());
            Assert.Equal(MessagePackCode.Int16, reader.NextCode);
            Assert.Equal(2, reader.ReadInt16());
            Assert.Equal(MessagePackCode.Int32, reader.NextCode);
            Assert.Equal(3, reader.ReadInt32());
            Assert.Equal(MessagePackCode.Int64, reader.NextCode);
            Assert.Equal(4, reader.ReadInt64());

            Assert.Equal(MessagePackCode.UInt8, reader.NextCode);
            Assert.Equal(1, reader.ReadByte());
            Assert.Equal(MessagePackCode.UInt16, reader.NextCode);
            Assert.Equal(2, reader.ReadUInt16());
            Assert.Equal(MessagePackCode.UInt32, reader.NextCode);
            Assert.Equal(3u, reader.ReadUInt32());
            Assert.Equal(MessagePackCode.UInt64, reader.NextCode);
            Assert.Equal(4u, reader.ReadUInt64());

            Assert.Equal(MessagePackCode.Int8, reader.NextCode);
            Assert.Equal(1, reader.ReadSByte());
            Assert.Equal(MessagePackCode.Int16, reader.NextCode);
            Assert.Equal(2, reader.ReadInt16());
            Assert.Equal(MessagePackCode.Int32, reader.NextCode);
            Assert.Equal(3, reader.ReadInt32());
            Assert.Equal(MessagePackCode.Int64, reader.NextCode);
            Assert.Equal(4, reader.ReadInt64());
        }

        private static ConstructorInfo GetDefaultConstructor(Type formatterType) => formatterType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(c => c.GetParameters().Length == 0);

        private static bool UsesSingletonPattern(Type formatterType) => formatterType.GetField("Instance", BindingFlags.Static | BindingFlags.Public)?.IsInitOnly is true;
    }
}

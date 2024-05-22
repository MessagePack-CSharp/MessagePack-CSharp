// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverOrderTest
    {
        private readonly ITestOutputHelper logger;

#if UNITY_2018_3_OR_NEWER

        public DynamicObjectResolverOrderTest()
        {
            this.logger = new NullTestOutputHelper();
        }

#endif

        public DynamicObjectResolverOrderTest(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        private IEnumerable<string> IteratePropertyNames(ReadOnlyMemory<byte> bytes)
        {
            var reader = new MessagePackReader(bytes);
            var mapCount = reader.ReadMapHeader();
            var result = new string[mapCount];
            for (int i = 0; i < mapCount; i++)
            {
                result[i] = reader.ReadString();
                reader.Skip(); // skip the value
            }

            return result;
        }

#if DYNAMIC_GENERATION

        [Fact]
        public void OrderTest()
        {
            var msgRawData = MessagePackSerializer.Serialize(new OrderOrder());
            this.IteratePropertyNames(msgRawData).Is("Bar", "Moge", "Foo", "FooBar", "NoBar");
        }

        [Fact]
        public void InheritIterateOrder()
        {
            RealClass realClass = new RealClass { Str = "X" };
            var msgRawData = MessagePackSerializer.Serialize(realClass);

            this.IteratePropertyNames(msgRawData).Is("Id", "Str");
        }

        [Fact]
        public void NonSequentialKeys_AllowPrivate()
        {
            var options = Resolvers.StandardResolverAllowPrivate.Options;
            var c = new ClassWithMissingKeyPositions
            {
                Id = 2,
                Year = 2017,
                Memo = "some memo",
            };

            byte[] s = MessagePackSerializer.Serialize(c, options);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(s, options));

            ClassWithMissingKeyPositions c2 = MessagePackSerializer.Deserialize<ClassWithMissingKeyPositions>(s, options);
            Assert.Equal(c.Id, c2.Id);
            Assert.Equal(c.Year, c2.Year);
            Assert.Equal(c.Memo, c2.Memo);
        }
#endif

        [MessagePack.MessagePackObject(keyAsPropertyName: true)]
        [Union(0, typeof(RealClass))]
        public abstract class AbstractBase
        {
            [DataMember(Order = 0)]
            public UInt32 Id = 0xaa00aa00;
        }

        public sealed class RealClass : AbstractBase
        {
            public String Str;
        }

        [MessagePack.MessagePackObject(keyAsPropertyName: true)]
        public class OrderOrder
        {
            [DataMember(Order = 5)]
            public int Foo { get; set; }

            [DataMember(Order = 2)]
            public int Moge { get; set; }

            [DataMember(Order = 10)]
            public int FooBar;

            public string NoBar;

            [DataMember(Order = 0)]
            public string Bar;
        }

        [MessagePackObject]
        internal class ClassWithMissingKeyPositions
        {
            public ClassWithMissingKeyPositions()
            {
            }

            [Key(0)]
            internal int Id { get; set; }

            // This position intentionally omitted for the test.
            ////[Key(1)]
            ////public string Name { get; set; }

            [Key(2)]
            internal int Year { get; set; } = 2019;

            [Key(3)]
            internal string Memo { get; set; }
        }
    }
}

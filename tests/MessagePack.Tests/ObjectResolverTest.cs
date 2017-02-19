using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ObjectResolverTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }


        [Fact]
        public void Standard()
        {
            var o = new SimpleIntKeyData()
            {
                Prop1 = 100,
                Prop2 = ByteEnum.C,
                Prop3 = "abcde",
                Prop4 = new SimlpeStringKeyData
                {
                    Prop1 = 99999,
                    Prop2 = ByteEnum.E,
                    Prop3 = 3
                },
                Prop5 = new SimpleStructIntKeyData
                {
                    X = 100,
                    Y = 300,
                    BytesSpecial = new byte[] { 9, 99, 122 }
                },
                Prop6 = new SimpleStructStringKeyData
                {
                    X = 9999,
                    Y = new[] { 1, 10, 100 }
                },
                BytesSpecial = new byte[] { 1, 4, 6 }
            };

            Convert(o).IsStructuralEqual(o);
        }


        [Fact]
        public void Null()
        {
            SimpleIntKeyData n = null;
            var bytes = MessagePackSerializer.Serialize(n);
            MessagePackBinary.IsNil(bytes, 0).IsTrue();
            bytes.Length.Is(1);

            MessagePackSerializer.Deserialize<SimpleIntKeyData>(bytes).IsNull();

            // deserialize from nil
            Xunit.Assert.Throws<InvalidOperationException>(() =>
            {
                MessagePackSerializer.Deserialize<SimpleStructIntKeyData>(bytes);
            });
        }

        [Fact]
        public void WithConstructor()
        {
            var o = new Vector2(100.4f, 4321.1f);
            Convert(o).IsStructuralEqual(o);
        }


        [Fact]
        public void Nullable()
        {
            Vector2? o = new Vector2(100.4f, 4321.1f);
            Convert(o).IsStructuralEqual(o);
            o = null;
            Convert(o).IsStructuralEqual(o);
        }

        [Fact]
        public void EmptyAndNull()
        {
            var o = new EmptyClass();

            Convert(o).IsStructuralEqual(o);
            o = null;
            var r = Convert(o);

            r.IsStructuralEqual(o);

            var o2 = new EmptyStruct();
            Convert(o2).IsStructuralEqual(o2);
        }

        [Fact]
        public void Versioning()
        {
            var v1 = new Version1
            {
                MyProperty1 = 100,
                MyProperty2 = 200,
                MyProperty3 = 300
            };

            var v2 = new Version2
            {
                MyProperty1 = 100,
                MyProperty2 = 200,
                MyProperty3 = 300,
                MyProperty4 = 400,
                MyProperty5 = 500,
            };

            var v0 = new Version0
            {
                MyProperty1 = 100,
            };

            var v1Bytes = MessagePackSerializer.Serialize(v1);
            var v2Bytes = MessagePackSerializer.Serialize(v2);
            var v0Bytes = MessagePackSerializer.Serialize(v0);

            var a = MessagePackSerializer.ToJson(v1Bytes);
            var b = MessagePackSerializer.ToJson(v2Bytes);
            var c = MessagePackSerializer.ToJson(v0Bytes);
            MessagePackSerializer.Deserialize<Version1>(v1Bytes).IsNotStructuralEqual(v1Bytes);
            MessagePackSerializer.Deserialize<Version2>(v2Bytes).IsNotStructuralEqual(v2Bytes);
            MessagePackSerializer.Deserialize<Version0>(v0Bytes).IsNotStructuralEqual(v0Bytes);

            // smaller than schema
            var v2_ = MessagePackSerializer.Deserialize<Version2>(v1Bytes);
            v2_.MyProperty1.Is(v1.MyProperty1);
            v2_.MyProperty2.Is(v1.MyProperty2);
            v2_.MyProperty3.Is(v1.MyProperty3);
            v2_.MyProperty4.Is(0);
            v2_.MyProperty5.Is(0);

            // larger than schema

            var v0_ = MessagePackSerializer.Deserialize<Version0>(v1Bytes);
            v0_.MyProperty1.Is(v1.MyProperty1);
        }

        [Fact]
        public void Versioning2()
        {
            var v1 = new HolderV1
            {
                MyProperty1 = new Version1
                {
                    MyProperty1 = 100,
                    MyProperty2 = 200,
                    MyProperty3 = 300
                },
                After = 9999
            };

            var v2 = new HolderV2
            {
                MyProperty1 = new Version2
                {
                    MyProperty1 = 100,
                    MyProperty2 = 200,
                    MyProperty3 = 300,
                    MyProperty4 = 400,
                    MyProperty5 = 500
                },
                After = 99999999
            };

            var v0 = new HolderV0
            {
                MyProperty1 = new Version0
                {
                    MyProperty1 = 100,
                },
                After = 1999
            };

            var v1Bytes = MessagePackSerializer.Serialize(v1);
            var v2Bytes = MessagePackSerializer.Serialize(v2);
            var v0Bytes = MessagePackSerializer.Serialize(v0);

            // smaller than schema
            var v2_ = MessagePackSerializer.Deserialize<HolderV2>(v1Bytes);
            v2_.MyProperty1.MyProperty1.Is(v1.MyProperty1.MyProperty1);
            v2_.MyProperty1.MyProperty2.Is(v1.MyProperty1.MyProperty2);
            v2_.MyProperty1.MyProperty3.Is(v1.MyProperty1.MyProperty3);
            v2_.MyProperty1.MyProperty4.Is(0);
            v2_.MyProperty1.MyProperty5.Is(0);
            v2_.After.Is(9999);

            // larger than schema
            var v0_ = MessagePackSerializer.Deserialize<HolderV0>(v1Bytes);
            v0_.MyProperty1.MyProperty1.Is(v1.MyProperty1.MyProperty1);
            v0.After.Is(9999);
        }
    }
}

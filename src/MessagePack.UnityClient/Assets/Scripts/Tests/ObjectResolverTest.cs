using RuntimeUnitTestToolkit;
using System.Linq;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class ObjectResolverTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }


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

            var c = Convert(o);

            c.Prop1.Is(o.Prop1);
            c.Prop2.Is(o.Prop2);
            c.Prop3.Is(o.Prop3);
            c.Prop4.Prop1.Is(o.Prop4.Prop1);
            c.Prop4.Prop2.Is(o.Prop4.Prop2);
            c.Prop4.Prop3.Is(o.Prop4.Prop3);
            c.Prop5.X.Is(o.Prop5.X);
            c.Prop5.Y.Is(o.Prop5.Y);
            c.Prop5.BytesSpecial.SequenceEqual(o.Prop5.BytesSpecial).IsTrue();
            c.Prop6.X.Is(o.Prop6.X);
            c.Prop6.Y.SequenceEqual(o.Prop6.Y).IsTrue();
            c.BytesSpecial.SequenceEqual(o.BytesSpecial).IsTrue();
        }


        public void Null()
        {
            SimpleIntKeyData n = null;
            var bytes = MessagePackSerializer.Serialize(n);
            MessagePackBinary.IsNil(bytes, 0).IsTrue();
            bytes.Length.Is(1);

            MessagePackSerializer.Deserialize<SimpleIntKeyData>(bytes).IsNull();

            // deserialize from nil
            Assert.Throws<InvalidOperationException>(() =>
            {
                MessagePackSerializer.Deserialize<SimpleStructIntKeyData>(bytes);
            });
        }

        public void WithConstructor()
        {
            var o = new Vector2(100.4f, 4321.1f);
            var o2 = Convert(o);
            o.X.Is(o2.X);
            o.Y.Is(o2.Y);
        }


        public void Nullable()
        {
            Vector2? o = new Vector2(100.4f, 4321.1f);
            var o2 = Convert(o);
            o.Value.X.Is(o2.Value.X);
            o.Value.Y.Is(o2.Value.Y);
            o = null;
            Convert(o).IsNull();
        }

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
                MyProperty5 = 500,
            };

            var v0 = new Version0
            {
                MyProperty1 = 100,
            };

            var v1Bytes = MessagePackSerializer.Serialize(v1);
            var v2Bytes = MessagePackSerializer.Serialize(v2);
            var v0Bytes = MessagePackSerializer.Serialize(v0);

            var a = MessagePackSerializer.Deserialize<Version1>(v1Bytes);
            a.MyProperty1.Is(100);
            a.MyProperty2.Is(200);
            a.MyProperty3.Is(300);

            var b = MessagePackSerializer.Deserialize<Version2>(v2Bytes);
            b.MyProperty1.Is(100);
            b.MyProperty2.Is(200);
            b.MyProperty3.Is(300);
            b.MyProperty5.Is(500);

            var c = MessagePackSerializer.Deserialize<Version0>(v0Bytes);
            c.MyProperty1.Is(100);

            // smaller than schema
            var v2_ = MessagePackSerializer.Deserialize<Version2>(v1Bytes);
            v2_.MyProperty1.Is(v1.MyProperty1);
            v2_.MyProperty2.Is(v1.MyProperty2);
            v2_.MyProperty3.Is(v1.MyProperty3);
            v2_.MyProperty5.Is(0);

            // larger than schema

            var v0_ = MessagePackSerializer.Deserialize<Version0>(v1Bytes);
            v0_.MyProperty1.Is(v1.MyProperty1);
        }

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
            v2_.MyProperty1.MyProperty5.Is(0);
            v2_.After.Is(9999);

            // larger than schema
            var v1Json = MessagePackSerializer.ToJson(v1Bytes);
            var v0_ = MessagePackSerializer.Deserialize<HolderV0>(v1Bytes);
            v0_.MyProperty1.MyProperty1.Is(v1.MyProperty1.MyProperty1);
            v0_.After.Is(9999);
        }

        public void SerializationCallback()
        {
            {
                var c1 = new Callback1(0);
                var d = MessagePackSerializer.Serialize(c1);
                c1.CalledBefore.IsTrue();
                MessagePackSerializer.Deserialize<Callback1>(d).CalledAfter.IsTrue();
            }
            {
                var before = false;

                var c1 = new Callback2(0, () => before = true, () => { });
                var d = MessagePackSerializer.Serialize(c1);
                before.IsTrue();
                Callback2.CalledAfter = false;
                MessagePackSerializer.Deserialize<Callback2>(d);
                Callback2.CalledAfter.IsTrue();
            }
            {
                var c1 = new Callback1_2(0);
                var d = MessagePackSerializer.Serialize(c1);
                c1.CalledBefore.IsTrue();
                MessagePackSerializer.Deserialize<Callback1_2>(d).CalledAfter.IsTrue();
            }
            {
                var before = false;

                var c1 = new Callback2_2(0, () => before = true, () => { });
                var d = MessagePackSerializer.Serialize(c1);
                before.IsTrue();

                Callback2.CalledAfter = false;
                MessagePackSerializer.Deserialize<Callback2_2>(d);
                Callback2_2.CalledAfter.IsTrue();
            }
        }

        public void GenericClassTest()
        {
            var t = new GenericClass<int, string> { MyProperty0 = 100, MyProperty1 = "aaa" };
            var v = Convert(t);
            v.MyProperty0.Is(100);
            v.MyProperty1.Is("aaa");
        }

        public void GenericStructTest()
        {
            var t = new GenericStruct<int, string> { MyProperty0 = 100, MyProperty1 = "aaa" };
            var v = Convert(t);
            v.MyProperty0.Is(100);
            v.MyProperty1.Is("aaa");
        }
    }
}
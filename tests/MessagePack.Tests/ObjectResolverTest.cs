// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using SharedData;
using Xunit;

namespace MessagePack.Tests
{
    public class ObjectResolverTest
    {
        private T Convert<T>(T value)
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
                Prop4 = new SimpleStringKeyData
                {
                    Prop1 = 99999,
                    Prop2 = ByteEnum.E,
                    Prop3 = 3,
                },
                Prop5 = new SimpleStructIntKeyData
                {
                    X = 100,
                    Y = 300,
                    BytesSpecial = new byte[] { 9, 99, 122 },
                },
                Prop6 = new SimpleStructStringKeyData
                {
                    X = 9999,
                    Y = new[] { 1, 10, 100 },
                },
                BytesSpecial = new byte[] { 1, 4, 6 },
            };

            this.Convert(o).IsStructuralEqual(o);
        }

        [Fact]
        public void Null()
        {
            SimpleIntKeyData n = null;
            var bytes = MessagePackSerializer.Serialize(n);
            var reader = new MessagePackReader(bytes);
            reader.IsNil.IsTrue();
            bytes.Length.Is(1);

            MessagePackSerializer.Deserialize<SimpleIntKeyData>(bytes).IsNull();

            // deserialize from nil
            Xunit.Assert.Throws<MessagePackSerializationException>(() =>
            {
                MessagePackSerializer.Deserialize<SimpleStructIntKeyData>(bytes);
            });
        }

        [Fact]
        public void NullString()
        {
            var o = new SimpleIntKeyData();
            SimpleIntKeyData result = this.Convert(o);
            result.Prop1.Is(0);
            result.Prop3.IsNull();
            result.Prop4.IsNull();
            result.BytesSpecial.IsNull();
        }

        [Fact]
        public void WithConstructor()
        {
            var o = new Vector2(100.4f, 4321.1f);
            this.Convert(o).IsStructuralEqual(o);
        }

        [Fact]
        public void Nullable()
        {
            Vector2? o = new Vector2(100.4f, 4321.1f);
            this.Convert(o).IsStructuralEqual(o);
            o = null;
            this.Convert(o).IsStructuralEqual(o);
        }

        [Fact]
        public void EmptyAndNull()
        {
            var o = new EmptyClass();

            this.Convert(o).IsStructuralEqual(o);
            o = null;
            EmptyClass r = this.Convert(o);

            r.IsStructuralEqual(o);

            var o2 = default(EmptyStruct);
            this.Convert(o2).IsStructuralEqual(o2);
        }

        [Fact]
        public void Versioning()
        {
            var v1 = new Version1
            {
                MyProperty1 = 100,
                MyProperty2 = 200,
                MyProperty3 = 300,
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

            var a = MessagePackSerializer.ConvertToJson(v1Bytes);
            var b = MessagePackSerializer.ConvertToJson(v2Bytes);
            var c = MessagePackSerializer.ConvertToJson(v0Bytes);
            MessagePackSerializer.Deserialize<Version1>(v1Bytes).IsNotStructuralEqual(v1Bytes);
            MessagePackSerializer.Deserialize<Version2>(v2Bytes).IsNotStructuralEqual(v2Bytes);
            MessagePackSerializer.Deserialize<Version0>(v0Bytes).IsNotStructuralEqual(v0Bytes);

            // smaller than schema
            Version2 v2_ = MessagePackSerializer.Deserialize<Version2>(v1Bytes);
            v2_.MyProperty1.Is(v1.MyProperty1);
            v2_.MyProperty2.Is(v1.MyProperty2);
            v2_.MyProperty3.Is(v1.MyProperty3);
            v2_.MyProperty5.Is(0);

            // larger than schema
            Version0 v0_ = MessagePackSerializer.Deserialize<Version0>(v1Bytes);
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
                    MyProperty3 = 300,
                },
                After = 9999,
            };

            var v2 = new HolderV2
            {
                MyProperty1 = new Version2
                {
                    MyProperty1 = 100,
                    MyProperty2 = 200,
                    MyProperty3 = 300,
                    MyProperty5 = 500,
                },
                After = 99999999,
            };

            var v0 = new HolderV0
            {
                MyProperty1 = new Version0
                {
                    MyProperty1 = 100,
                },
                After = 1999,
            };

            var v1Bytes = MessagePackSerializer.Serialize(v1);
            var v2Bytes = MessagePackSerializer.Serialize(v2);
            var v0Bytes = MessagePackSerializer.Serialize(v0);

            // smaller than schema
            HolderV2 v2_ = MessagePackSerializer.Deserialize<HolderV2>(v1Bytes);
            v2_.MyProperty1.MyProperty1.Is(v1.MyProperty1.MyProperty1);
            v2_.MyProperty1.MyProperty2.Is(v1.MyProperty1.MyProperty2);
            v2_.MyProperty1.MyProperty3.Is(v1.MyProperty1.MyProperty3);
            v2_.MyProperty1.MyProperty5.Is(0);
            v2_.After.Is(9999);

            // larger than schema
            var v1Json = MessagePackSerializer.ConvertToJson(v1Bytes);
            HolderV0 v0_ = MessagePackSerializer.Deserialize<HolderV0>(v1Bytes);
            v0_.MyProperty1.MyProperty1.Is(v1.MyProperty1.MyProperty1);
            v0_.After.Is(9999);
        }

        [Fact]
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
                Callback2.CalledAfter.IsFalse();
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

                Callback2_2.CalledAfter.IsFalse();
                MessagePackSerializer.Deserialize<Callback2_2>(d);
                Callback2_2.CalledAfter.IsTrue();
            }
        }

#if DYNAMIC_GENERATION

        [Fact]
        public void GenericClassTest()
        {
            var t = new GenericClass<int, string> { MyProperty0 = 100, MyProperty1 = "aaa" };
            GenericClass<int, string> v = this.Convert(t);
            v.MyProperty0.Is(100);
            v.MyProperty1.Is("aaa");
        }

        [Fact]
        public void GenericStructTest()
        {
            var t = new GenericStruct<int, string> { MyProperty0 = 100, MyProperty1 = "aaa" };
            GenericStruct<int, string> v = this.Convert(t);
            v.MyProperty0.Is(100);
            v.MyProperty1.Is("aaa");
        }

#endif

        [Fact]
        public void Versioning3()
        {
            var binary = MessagePackSerializer.Serialize(new VersionBlockTest { MyProperty = 10, MyProperty2 = 99, UnknownBlock = new MyClass { MyProperty1 = 1, MyProperty2 = 99, MyProperty3 = 999 } });

            UnVersionBlockTest unversion = MessagePackSerializer.Deserialize<UnVersionBlockTest>(binary);

            // MessagePackBinary.
            unversion.MyProperty.Is(10);
            unversion.MyProperty2.Is(99);
        }

        [Fact]
        public void MoreEmpty()
        {
            var e1 = new Empty1();
            var e2 = new Empty2();
            var ne1 = new NonEmpty1();
            var ne2 = new NonEmpty2();

            MessagePackSerializer.SerializeToJson(e1).Is("[]");
            MessagePackSerializer.SerializeToJson(e2).Is("{}");
            MessagePackSerializer.SerializeToJson(ne1).Is("[0]");
            MessagePackSerializer.SerializeToJson(ne2).Is(@"{""MyProperty"":0}");
        }

        [Fact]
        public void NestedClass()
        {
            {
                var data = new NestParent.NestContract() { MyProperty = 1000 };
                var bin = MessagePackSerializer.Serialize(data);
                NestParent.NestContract re = MessagePackSerializer.Deserialize<NestParent.NestContract>(bin);

                re.MyProperty.Is(1000);
            }
        }

        [Fact]
        public void WithIndexer()
        {
            var o = new WithIndexer
            {
                Data1 = 15,
                Data2 = "15",
            };
            var bin = MessagePackSerializer.Serialize(o);
            WithIndexer v = MessagePackSerializer.Deserialize<WithIndexer>(bin);

            v.IsStructuralEqual(o);
        }

#if DYNAMIC_GENERATION

        [Fact]
        public void Contractless()
        {
            var data = new ContractlessConstructorCheck(10, "hogehoge");
            var bin = MessagePackSerializer.Serialize(data, ContractlessStandardResolver.Options);
            ContractlessConstructorCheck re = MessagePackSerializer.Deserialize<ContractlessConstructorCheck>(bin, ContractlessStandardResolver.Options);

            re.MyProperty1.Is(10);
            re.MyProperty2.Is("hogehoge");
        }

        [Fact]
        public void FindingConstructor()
        {
            var data = new FindingConstructorCheck(10, "hogehoge");
            var bin = MessagePackSerializer.Serialize(data, ContractlessStandardResolver.Options);
            FindingConstructorCheck re = MessagePackSerializer.Deserialize<FindingConstructorCheck>(bin, ContractlessStandardResolver.Options);

            re.MyProperty1.Is(10);
            re.MyProperty2.Is("hogehoge");
        }

        [Fact]
        public void NestedClassContractless()
        {
            {
                var data = new NestParent.NestContractless() { MyProperty = 1000 };
                var bin = MessagePackSerializer.Serialize(data, ContractlessStandardResolver.Options);
                NestParent.NestContractless re = MessagePackSerializer.Deserialize<NestParent.NestContractless>(bin, ContractlessStandardResolver.Options);

                re.MyProperty.Is(1000);
            }
        }

        [Fact]
        public void WithIndexerContractless()
        {
            var o = new WithIndexerContractless
            {
                Data1 = 15,
                Data2 = "15",
            };
            var bin = MessagePackSerializer.Serialize(o, ContractlessStandardResolver.Options);
            WithIndexerContractless v = MessagePackSerializer.Deserialize<WithIndexerContractless>(bin, ContractlessStandardResolver.Options);

            v.IsStructuralEqual(o);
        }

#endif

    }
}

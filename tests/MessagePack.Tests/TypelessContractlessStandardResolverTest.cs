using MessagePack.Resolvers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class TypelessContractlessStandardResolverTest
    {
        private MessagePackSerializer defaultSerializer = new MessagePackSerializer();
        private MessagePackSerializer.Typeless typeless = new MessagePackSerializer.Typeless();

        public class Address
        {
            public string Street { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
            public object[] /*Address*/ Addresses { get; set; }
        }

        public class ForTypelessObj
        {
            public object Obj { get; set; }
        }

        [Fact]
        public void AnonymousTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new[]
                {
                        new { Street = "St." },
                        new { Street = "Ave." }
                    }
            };

            var result = defaultSerializer.Serialize(p, new TypelessContractlessStandardResolver());

            defaultSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

            var p2 = defaultSerializer.Deserialize<Person>(result, new TypelessContractlessStandardResolver());
            p2.Name.Is("John");
            var addresses = p2.Addresses as IList;
            var d1 = addresses[0] as IDictionary;
            var d2 = addresses[1] as IDictionary;
            (d1["Street"] as string).Is("St.");
            (d2["Street"] as string).Is("Ave.");
        }

        [Fact]
        public void StrongTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new object[]
                {
                    new Address { Street = "St." },
                    new Address { Street = "Ave." }
                }
            };

            var result = defaultSerializer.Serialize(p, new TypelessContractlessStandardResolver());

            var p2 = defaultSerializer.Deserialize<Person>(result, new TypelessContractlessStandardResolver());
            p.IsStructuralEqual(p2);

            defaultSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
        }

        [Fact]
        public void ObjectRuntimeTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new object[]
                {
                    new object(),
                    new Address { Street = "Ave." }
                }
            };

            var result = defaultSerializer.Serialize(p, new TypelessContractlessStandardResolver());

            var p2 = defaultSerializer.Deserialize<Person>(result, new TypelessContractlessStandardResolver());
            p.IsStructuralEqual(p2);

#if NETFRAMEWORK
            defaultSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""""System.Object, mscorlib""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
#else
            defaultSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""""System.Object, System.Private.CoreLib""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
#endif
        }

        public class A { public int Id; }
        public class B { public A Nested; }

        [Fact]
        public void TypelessContractlessTest()
        {
            object obj = new B() { Nested = new A() { Id = 1 } };
            var result = defaultSerializer.Serialize(obj, new TypelessContractlessStandardResolver());
            defaultSerializer.ToJson(result).Is(@"{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+B, MessagePack.Tests"",""Nested"":{""Id"":1}}");
        }

        [MessagePackObject]
        public class AC {[Key(0)] public int Id; }
        [MessagePackObject]
        public class BC {[Key(0)] public AC Nested;[Key(1)] public string Name; }

        [Fact]
        public void TypelessAttributedTest()
        {
            object obj = new BC() { Nested = new AC() { Id = 1 }, Name = "Zed" };
            var result = defaultSerializer.Serialize(obj, new TypelessContractlessStandardResolver());
            defaultSerializer.ToJson(result).Is(@"[""MessagePack.Tests.TypelessContractlessStandardResolverTest+BC, MessagePack.Tests"",[1],""Zed""]");
        }

        [Fact]
        public void PreservingTimezoneInTypelessCollectionsTest()
        {
            var arr = new Dictionary<object, object>()
            {
                { (byte)1, "a"},
                { (byte)2, new object[] { "level2", new object[] { "level3", new Person() { Name = "Peter", Addresses = new object[] { new Address() { Street = "St." }, new DateTime(2017,6,26,14,58,0) } } } } }
            };
            var result = defaultSerializer.Serialize(arr, new TypelessContractlessStandardResolver());

            var deser = defaultSerializer.Deserialize<Dictionary<object, object>>(result, new TypelessContractlessStandardResolver());
            deser.IsStructuralEqual(arr);

#if NETFRAMEWORK
            defaultSerializer.ToJson(result).Is(@"{""1"":""a"",""2"":[""System.Object[], mscorlib"",""level2"",[""System.Object[], mscorlib"",""level3"",{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Person, MessagePack.Tests"",""Name"":""Peter"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""System.DateTime, mscorlib"",636340858800000000}]}]]}");
#else
            defaultSerializer.ToJson(result).Is(@"{""1"":""a"",""2"":[""System.Object[], System.Private.CoreLib"",""level2"",[""System.Object[], System.Private.CoreLib"",""level3"",{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Person, MessagePack.Tests"",""Name"":""Peter"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""System.DateTime, System.Private.CoreLib"",636340858800000000}]}]]}");
#endif
        }

        [Fact]
        public void PreservingCollectionTypeTest()
        {
            var arr = new object[] { (byte)1, new object[] { (byte)2, new LinkedList<object>(new object[] { "a", (byte)42 }) } };
            var result = defaultSerializer.Serialize(arr, new TypelessContractlessStandardResolver());
            var deser = defaultSerializer.Deserialize<object[]>(result, new TypelessContractlessStandardResolver());
            deser.IsStructuralEqual(arr);

#if NETFRAMEWORK
            defaultSerializer.ToJson(result).Is(@"[1,[""System.Object[], mscorlib"",2,[""System.Collections.Generic.LinkedList`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"",""a"",42]]]");
#else
            defaultSerializer.ToJson(result).Is(@"[1,[""System.Object[], System.Private.CoreLib"",2,[""System.Collections.Generic.LinkedList`1[[System.Object, System.Private.CoreLib]], System.Collections"",""a"",42]]]");
#endif
        }

        [Theory]
        [InlineData((sbyte)0)]
        [InlineData((short)0)]
        [InlineData((int)0)]
        [InlineData((long)0)]
        [InlineData((byte)0)]
        [InlineData((ushort)0)]
        [InlineData((uint)0)]
        [InlineData((ulong)0)]
        [InlineData((char)'a')]
        public void TypelessPrimitive<T>(T p)
        {
            var v = new ForTypelessObj() { Obj = p };

            var bin = typeless.Serialize(v);
            var o = (ForTypelessObj)typeless.Deserialize(bin);

            o.Obj.GetType().Is(typeof(T));
        }

        [Fact]
        public void TypelessPrimitive2()
        {
            {
                var now = DateTime.Now;
                var v = new ForTypelessObj() { Obj = now };

                var bin = typeless.Serialize(v);
                var o = (ForTypelessObj)typeless.Deserialize(bin);

                o.Obj.GetType().Is(typeof(DateTime));
                ((DateTime)o.Obj).Is(now);
            }
            {
                var now = DateTimeOffset.Now;
                var v = new ForTypelessObj() { Obj = now };

                var bin = typeless.Serialize(v);
                var o = (ForTypelessObj)typeless.Deserialize(bin);

                o.Obj.GetType().Is(typeof(DateTimeOffset));
                ((DateTimeOffset)o.Obj).Is(now);
            }
        }

        [Fact]
        public void TypelessEnum()
        {
            var e = typeless.Serialize(GlobalMyEnum.Apple);
            var b = typeless.Deserialize(e);
            b.GetType().Is(typeof(GlobalMyEnum));
        }

        [Fact]
        public void MyTestMethod()
        {
            var sampleMessage = new InternalSampleMessageType
            {
                DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
                GuidProp = Guid.NewGuid(),
                IntProp = 123,
                StringProp = "Hello World"
            };

            {
                var serializedMessage = typeless.Serialize(sampleMessage);
                var r2 = (InternalSampleMessageType)typeless.Deserialize(serializedMessage);
                r2.DateProp.Is(sampleMessage.DateProp);
                r2.GuidProp.Is(sampleMessage.GuidProp);
                r2.IntProp.Is(sampleMessage.IntProp);
                r2.StringProp.Is(sampleMessage.StringProp);
            }

            {
                var lz4 = new LZ4MessagePackSerializer.Typeless();
                var serializedMessage = lz4.Serialize(sampleMessage);
                var r2 = (InternalSampleMessageType)lz4.Deserialize(serializedMessage);
                r2.DateProp.Is(sampleMessage.DateProp);
                r2.GuidProp.Is(sampleMessage.GuidProp);
                r2.IntProp.Is(sampleMessage.IntProp);
                r2.StringProp.Is(sampleMessage.StringProp);
            }
        }

        [Fact]
        public void SaveArrayType()
        {
            {
                string[] array = new[] { "test1", "test2" };
                byte[] bytes = typeless.Serialize(array);
                object obj = typeless.Deserialize(bytes);

                var obj2 = obj as string[];
                obj2.Is("test1", "test2");
            }
            {
                var objRaw = new SomeClass
                {
                    Obj = new string[] { "asd", "asd" }
                };

                var objSer = defaultSerializer.Serialize(objRaw, new TypelessContractlessStandardResolver());

                var objDes = defaultSerializer.Deserialize<SomeClass>(objSer, new TypelessContractlessStandardResolver());

                var expectedTrue = objDes.Obj is string[];
                expectedTrue.IsTrue();
            }
        }
    }



    public class SomeClass
    {
        public object Obj { get; set; }
    }

    internal class InternalSampleMessageType
    {
        public string StringProp { get; set; }
        public int IntProp { get; set; }
        public Guid GuidProp { get; set; }
        public DateTime DateProp { get; set; }
    }
}

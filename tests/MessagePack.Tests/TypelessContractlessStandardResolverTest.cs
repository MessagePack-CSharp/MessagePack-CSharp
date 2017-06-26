using MessagePack.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class TypelessContractlessStandardResolverTest
    {
        public class Address
        {
            public string Street { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
            public object[] /*Address*/ Addresses { get; set; }
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

                var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolver.Instance);

                MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

                var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolver.Instance);
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

            var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolver.Instance);

            var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolver.Instance);
            p.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
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

            var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolver.Instance);

            var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolver.Instance);
            p.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""""System.Object, mscorlib""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
        }

        public class A { public int Id; }
        public class B { public A Nested; }

        [Fact]
        public void TypelessContractlessTest()
        {
            object obj = new B() { Nested = new A() { Id = 1 } };
            var result = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolver.Instance);
            MessagePackSerializer.ToJson(result).Is(@"{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+B, MessagePack.Tests"",""Nested"":{""Id"":1}}");
        }

        [MessagePackObject]
        public class AC { [Key(0)] public int Id; }
        [MessagePackObject]
        public class BC {[Key(0)] public AC Nested; [Key(1)] public string Name; }

        [Fact]
        public void TypelessAttributedTest()
        {
            object obj = new BC() { Nested = new AC() { Id = 1 }, Name = "Zed" };
            var result = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolver.Instance);
            MessagePackSerializer.ToJson(result).Is(@"[""MessagePack.Tests.TypelessContractlessStandardResolverTest+BC, MessagePack.Tests"",[1],""Zed""]");
        }

        [Fact]
        public void PreservingTimezoneInTypelessCollectionsTest()
        {
            var arr = new Dictionary<object, object>()
            {
                { 1, "a"},
                { 2, new object[] { "level2", new object[] { "level3", new Person() { Name = "Peter", Addresses = new object[] { new Address() { Street = "St." }, new DateTime(2017,6,26,14,58,0) } } } } }
            };
            var result = MessagePackSerializer.Serialize(arr, TypelessContractlessStandardResolver.Instance);

            var deser = MessagePackSerializer.Deserialize<Dictionary<object, object>>(result, TypelessContractlessStandardResolver.Instance);
            deser.IsStructuralEqual(arr);

            MessagePackSerializer.ToJson(result).Is(@"{""{""$type"":""System.Int32, mscorlib"",1}"":{""$type"":""System.String, mscorlib"",""a""},""{""$type"":""System.Int32, mscorlib"",2}"":[{""$type"":""System.String, mscorlib"",""level2""},[""System.Object[], mscorlib"",{""$type"":""System.String, mscorlib"",""level3""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Person, MessagePack.Tests"",""Name"":""Peter"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""System.DateTime, mscorlib"",636340858800000000}]}]]}");
        }
    }
}

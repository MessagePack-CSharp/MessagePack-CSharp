using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ContractlessStandardResolverTest
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

                var result = MessagePack.MessagePackSerializer.Serialize(p, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

                MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

                var p2 = MessagePack.MessagePackSerializer.Deserialize<Person>(result, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
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

            var result = MessagePack.MessagePackSerializer.Serialize(p, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            var p2 = MessagePack.MessagePackSerializer.Deserialize<Person>(result, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            p.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""MessagePack.Tests.ContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""MessagePack.Tests.ContractlessStandardResolverTest+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
        }

    }
}

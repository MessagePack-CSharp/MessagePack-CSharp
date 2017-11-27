using System.Collections.Generic;
using Xunit;

namespace MessagePack.Tests
{
    public class BaseClassPrivatePropertyTest
    {
        public class Address
        {
            public string Street { get; set; }
        }

        public class Person
        {
            public Person()
            {
                this.Addresses = new List<Address>();
            }

            public string Name { get; set; }
            public IList<Address> /*Address*/ Addresses { get; private set; }
        }

        public class PersonNew : Person
        {
            public int Age { get; set; }
        }

        [Fact]
        public void SimpleTest()
        {
            var p = new Person
            {
                Name = "John"
            };

            p.Addresses.Add(new Address { Street = "St." });
            p.Addresses.Add(new Address { Street = "Ave." });


            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Instance);
            var result = MessagePack.MessagePackSerializer.Serialize(p);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

            var p2 = MessagePack.MessagePackSerializer.Deserialize<PersonNew>(result);
            p2.Name.Is("John");
            (p2.Addresses[0].Street).Is("St.");
            (p2.Addresses[1].Street).Is("Ave.");
        }
    }
}

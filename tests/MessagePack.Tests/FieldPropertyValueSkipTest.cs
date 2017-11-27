using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class FieldPropertyValueSkipTest
    {
        public class Address
        {
            public string Street { get; set; }

            public string Dummy { get; set; }
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
        public void FieldPropertyValueSkip_SampleTest()
        {
            var p = new Person
            {
                Name = "John"
            };

            p.Addresses.Add(new Address { Street = "St.", Dummy = "D" });
            p.Addresses.Add(new Address { Street = "Ave.", Dummy = "P" });

            var formatters = new IFormatterResolver[]
            {
                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
                DynamicEnumResolver.Instance, // Try Enum
                DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
                DynamicUnionResolver.Instance, // Try Union(Interface)
                DynamicObjectResolverAllowPrivate.Instance, // Try Object
                DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
            };

            var fieldPropertyValueSkipMap = new Dictionary<Type, IEnumerable<string>>();
            fieldPropertyValueSkipMap.Add(typeof(Address), new[] { nameof(Address.Dummy) });
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Instance);
            var result = MessagePack.MessagePackSerializer.Serialize(p, new MessagePack.Resolvers.CompositeFieldPropertyValueSkipResolver(formatters, new ReadOnlyDictionary<Type, IEnumerable<string>>(fieldPropertyValueSkipMap)));
            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

            var p2 = MessagePack.MessagePackSerializer.Deserialize<PersonNew>(result);
            p2.Name.Is("John");
            (p2.Addresses[0].Street).Is("St.");
            (p2.Addresses[0].Dummy).IsNull();
            (p2.Addresses[1].Street).Is("Ave.");
            (p2.Addresses[1].Dummy).IsNull();

            result = MessagePack.MessagePackSerializer.Serialize(p);
            p2 = MessagePack.MessagePackSerializer.Deserialize<PersonNew>(result);
            p2.Name.Is("John");
            (p2.Addresses[0].Street).Is("St.");
            (p2.Addresses[0].Dummy).Is("D");
            (p2.Addresses[1].Street).Is("Ave.");
            (p2.Addresses[1].Dummy).Is("P");
        }
    }
}

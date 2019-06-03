using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverInterfaceTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        void TestConstructorWithParentInterface()
        {
            var myClass = new ConstructorEnumerableTest(new[] { "0", "2", "3" });
            var serialized = this.serializer.Serialize(myClass);
            var deserialized = this.serializer.Deserialize<ConstructorEnumerableTest>(serialized);
            deserialized.Values.IsStructuralEqual(myClass.Values);
        }

    }

    [MessagePackObject]
    public class ConstructorEnumerableTest
    {
        [SerializationConstructor]
        public ConstructorEnumerableTest(IEnumerable<string> values)
        {
            Values = values.ToList().AsReadOnly();
        }

        [Key(0)]
        public IReadOnlyList<string> Values { get; }
    }
}

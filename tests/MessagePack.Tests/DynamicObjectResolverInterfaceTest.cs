using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverInterfaceTest
    {
        [Fact]
        void TestConstructorWithParentInterface()
        {
            var myClass = new ConstructorEnumerableTest(new[] { "0", "2", "3" });
            var serialized = MessagePackSerializer.Serialize(myClass);
            var deserialized =
                MessagePackSerializer.Deserialize<ConstructorEnumerableTest>(serialized);
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

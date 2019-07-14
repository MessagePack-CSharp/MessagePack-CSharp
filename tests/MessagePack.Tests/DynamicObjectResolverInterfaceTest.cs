// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverInterfaceTest
    {
        [Fact]
        private void TestConstructorWithParentInterface()
        {
            var myClass = new ConstructorEnumerableTest(new[] { "0", "2", "3" });
            var serialized = MessagePackSerializer.Serialize(myClass);
            ConstructorEnumerableTest deserialized = MessagePackSerializer.Deserialize<ConstructorEnumerableTest>(serialized);
            deserialized.Values.IsStructuralEqual(myClass.Values);
        }
    }

    [MessagePackObject]
    public class ConstructorEnumerableTest
    {
        [SerializationConstructor]
        public ConstructorEnumerableTest(IEnumerable<string> values)
        {
            this.Values = values.ToList().AsReadOnly();
        }

        [Key(0)]
        public IReadOnlyList<string> Values { get; }
    }
}

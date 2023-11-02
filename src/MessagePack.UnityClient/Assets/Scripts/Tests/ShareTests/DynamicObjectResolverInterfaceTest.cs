// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MessagePack.Tests
{
#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)

    public class DynamicObjectResolverInterfaceTest
    {
        [Fact]
        public void TestConstructorWithParentInterface_Array()
        {
            var myClass = new ConstructorEnumerableTestArray(new[] { "0", "2", "3" });
            var serialized = MessagePackSerializer.Serialize(myClass);
            var deserialized = MessagePackSerializer.Deserialize<ConstructorEnumerableTestArray>(serialized);
            deserialized.Values.IsStructuralEqual(myClass.Values);
        }

        [Fact]
        public void TestConstructorWithParentInterface_Map()
        {
            var myClass = new ConstructorEnumerableTestMap(new[] { "0", "2", "3" });
            var serialized = MessagePackSerializer.Serialize(myClass);
            var deserialized = MessagePackSerializer.Deserialize<ConstructorEnumerableTestMap>(serialized);
            deserialized.Values.IsStructuralEqual(myClass.Values);
        }
    }

    [MessagePackObject]
    public class ConstructorEnumerableTestArray
    {
        [SerializationConstructor]
        public ConstructorEnumerableTestArray(IEnumerable<string> values)
        {
            this.Values = values.ToList().AsReadOnly();
        }

        [Key(0)]
        public IReadOnlyList<string> Values { get; }
    }

    [MessagePackObject]
    public class ConstructorEnumerableTestMap
    {
        [SerializationConstructor]
        public ConstructorEnumerableTestMap(IEnumerable<string> values)
        {
            this.Values = values.ToList().AsReadOnly();
        }

        [Key("Values")]
        public IReadOnlyList<string> Values { get; }
    }

#endif
}

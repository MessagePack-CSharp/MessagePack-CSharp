#nullable enable

using MessagePack;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Tests
{
    public class IL2CPPTest
    {
        [Test]
        public void SimpleSerializeAndDeserialize()
        {
            var mc = new MyClass() { Age = 99, Name = "foo" };
            var bin = MessagePackSerializer.Serialize(mc);

            var formatter = MessagePackSerializer.DefaultOptions.Resolver.GetFormatter<MyClass>();
            Assert.NotNull(formatter);

            var mc2 = MessagePackSerializer.Deserialize<MyClass>(bin);

            Assert.AreEqual(mc.Age, mc2.Age);
            Assert.AreEqual(mc.Name, mc2.Name);
        }

        [Test]
        public void Vector3Serialize()
        {
            var value = new Vector3(1.3f, 3.43f, 8.3f);
            var bin = MessagePackSerializer.Serialize(value);

            var v2 = MessagePackSerializer.Deserialize<Vector3>(bin);

            Assert.AreEqual(value, v2);
        }
    }

    [MessagePackObject]
    public class MyClass
    {
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string? Name { get; set; }
    }
}

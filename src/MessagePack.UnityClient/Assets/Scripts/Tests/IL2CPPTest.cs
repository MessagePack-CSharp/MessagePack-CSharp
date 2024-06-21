#nullable enable

using MessagePack;
using NUnit.Framework;

namespace Assets.Scripts.Tests
{
    public class IL2CPPTest
    {
        [Test]
        public void SimpleSerializeAndDeserialize()
        {
            var mc = new MyClass() { Age = 99, Name = "foo" };
            var bin = MessagePackSerializer.Serialize(mc);

            var canGetFormatter = GeneratedMessagePackResolver.Instance.GetFormatter<MyClass>();
            Assert.AreNotEqual(canGetFormatter, null);

            var mc2 = MessagePackSerializer.Deserialize<MyClass>(bin);

            Assert.AreEqual(mc.Age, mc2.Age);
            Assert.AreEqual(mc.Name, mc2.Name);
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

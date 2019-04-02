using System;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverDerivedAttributeInheritanceTest
    {
        [Fact]
        public void InheritanceAndDerivedAttributeTest()
        {
            var value = new ChildClass(1, "Hello", 2);
            var serialized = MessagePackSerializer.Serialize(value);
            var deserialized =
                MessagePackSerializer.Deserialize<ChildClass>(serialized);
            deserialized.OtherValue.Is(value.OtherValue);
            deserialized.Text.Is(value.Text);
            deserialized.Value.Is(value.Value);
        }
    }

    [MessagePackObject]
    public abstract class BaseClass
    {
        public BaseClass(int value, string text)
        {
            Value = value;
            Text = text;
        }

        [Key(0)]
        public int Value { get; }

        [Key(1)]
        public string Text { get; }
    }

    [Message(1)]
    public class ChildClass : BaseClass
    {
        public ChildClass(int value, string text, int otherValue) : base(
            value,
            text)
        {
            OtherValue = otherValue;
        }

        [Key(2)]
        public int OtherValue { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class MessageAttribute : MessagePackObjectAttribute
    {
        public MessageAttribute(short version)
        {
            Version = version;
        }

        public short Version { get; }
    }
}

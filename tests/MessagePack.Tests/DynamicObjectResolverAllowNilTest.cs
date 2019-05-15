using System;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverAllowNilTest
    {
        private readonly MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        public void MessagePackObjectAllowNilSerializeTest()
        {
            CannotBeNull cannotBeNull = null;

            Action action = () => this.serializer.Serialize(cannotBeNull);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void MessagePackObjectAllowNilDeserializeTest()
        {
            CanBeNull canBeNull = null;

            var buffer = serializer.Serialize(canBeNull);

            Action action = () =>
            {
                var unused = this.serializer.Deserialize<CannotBeNull>(buffer);
            };

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void MessagePackObjectGenericAllowNilSerializeTest()
        {
            GenericCannotBeNull<string> cannotBeNull = null;

            Action action = () => this.serializer.Serialize(cannotBeNull);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void MessagePackObjectGenericAllowNilDeserializeTest()
        {
            GenericCanBeNull<string> canBeNull = null;

            var buffer = serializer.Serialize(canBeNull);

            Action action = () =>
            {
                var unused =
                    this.serializer.Deserialize<GenericCannotBeNull<string>>(
                        buffer);
            };

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void KeyAllowNilSerializeTest()
        {
            var nullPropertyDisallowed = new NullPropertyDisallowed();

            Action action = () =>
                this.serializer.Serialize(nullPropertyDisallowed);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void KeyAllowNilDeserializeTest()
        {
            var nullPropertyAllowed = new NullPropertyAllowed();

            var buffer = serializer.Serialize(nullPropertyAllowed);

            Action action = () =>
            {
                var unused =
                    this.serializer.Deserialize<NullPropertyDisallowed>(buffer);
            };

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void StringKeyAllowNilSerializeTest()
        {
            var nullPropertyDisallowed = new NullPropertyDisallowedStringKey();

            Action action = () =>
                this.serializer.Serialize(nullPropertyDisallowed);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void StringKeyAllowNilDeserializeTest()
        {
            var nullPropertyAllowed = new NullPropertyAllowedStringKey();

            var buffer = serializer.Serialize(nullPropertyAllowed);

            Action action = () =>
            {
                var unused =
                    this.serializer.Deserialize<NullPropertyDisallowedStringKey>(buffer);
            };

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GenericKeyAllowNilSerializeTest()
        {
            var nullPropertyDisallowed =
                new GenericNullPropertyDisallowed<string>();

            Action action = () =>
                this.serializer.Serialize(nullPropertyDisallowed);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GenericKeyAllowNilDeserializeTest()
        {
            var nullPropertyAllowed = new GenericNullPropertyAllowed<string>();

            var buffer = serializer.Serialize(nullPropertyAllowed);

            Action action = () =>
            {
                var unused =
                    this.serializer.Deserialize<NullPropertyDisallowed>(buffer);
            };

            Assert.Throws<InvalidOperationException>(action);
        }
    }

    [MessagePackObject(keyAsPropertyName: false, allowNil: false)]
    public class CannotBeNull
    {
        [Key(0)]
        public int Value { get; set; }
    }

    [MessagePackObject]
    public class CanBeNull
    {
        [Key(0)]
        public int Value { get; set; }
    }

    [MessagePackObject]
    public class NullPropertyDisallowed
    {
        [Key(0, false)]
        public string PropertyValue { get; set; }
    }

    [MessagePackObject]
    public class NullPropertyAllowed
    {
        [Key(0)]
        public string PropertyValue { get; set; }
    }

    [MessagePackObject]
    public class NullPropertyDisallowedStringKey
    {
        [Key("abc", false)]
        public string PropertyValue { get; set; }
    }

    [MessagePackObject]
    public class NullPropertyAllowedStringKey
    {
        [Key("abc")]
        public string PropertyValue { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: false, allowNil: false)]
    public class GenericCannotBeNull<T>
    {
        [Key(0)]
        public T Value { get; set; }
    }

    [MessagePackObject]
    public class GenericCanBeNull<T>
    {
        [Key(0)]
        public T Value { get; set; }
    }

    [MessagePackObject]
    public class GenericNullPropertyDisallowed<T>
    {
        [Key(0, false)]
        public T PropertyValue { get; set; }
    }

    [MessagePackObject]
    public class GenericNullPropertyAllowed<T>
    {
        [Key(0)]
        public T PropertyValue { get; set; }
    }

}

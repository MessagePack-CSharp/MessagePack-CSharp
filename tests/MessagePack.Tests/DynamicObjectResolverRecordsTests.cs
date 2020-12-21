// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

#if NET5_0

namespace MessagePack.Tests
{
    public class DynamicObjectResolverRecordsTests : TestBase
    {
        [Fact]
        public void RoundtripRecord()
        {
            this.AssertRoundTrip(new Person { FirstName = "bob", LastName = "smith" });
        }

        [Fact]
        public void RoundtripPositionalRecord()
        {
            this.AssertRoundTrip(new PersonPositional("bob", "smith"));
        }

        [Fact]
        public void RoundtripDerivedRecord()
        {
            this.AssertRoundTrip(new Student { FirstName = "bob", LastName = "smith", Grade = 5 });
        }

        [Fact]
        public void RoundtripPositionalDerivedRecord()
        {
            this.AssertRoundTrip(new StudentPositional("bob", "smith", 5));
        }

        protected T AssertRoundTrip<T>(T value)
        {
            byte[] msgpack = MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Standard, this.TimeoutToken);
            T deserializedValue = MessagePackSerializer.Deserialize<T>(msgpack, MessagePackSerializerOptions.Standard, this.TimeoutToken);
            Assert.Equal(value, deserializedValue);
            return deserializedValue;
        }

        [MessagePackObject]
        public record PersonPositional([property: Key(0)] string FirstName, [property: Key(1)] string LastName);

        [MessagePackObject]
        public record StudentPositional(string FirstName, string LastName, [property: Key(2)] int Grade)
            : PersonPositional(FirstName, LastName);

        [MessagePackObject]
        public record Person
        {
            [Key(0)]
            public string FirstName { get; init; }

            [Key(1)]
            public string LastName { get; init; }
        }

        [MessagePackObject]
        public record Student : Person
        {
            [Key(2)]
            public int Grade { get; init; }
        }
    }
}

#endif

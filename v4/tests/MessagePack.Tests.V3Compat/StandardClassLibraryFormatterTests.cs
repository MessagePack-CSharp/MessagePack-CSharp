// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Nerdbank.Streams;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class StandardClassLibraryFormatterTests : TestBase
    {
        private readonly ITestOutputHelper logger;

        public StandardClassLibraryFormatterTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact(Skip = "v4 intentional: System.Type serialization is opt-in (TypeFormatterFactory) because deserializing runs Type.GetType over payload names")]
        public void SystemType_Serializable()
        {
            Type type = typeof(string);
            byte[] msgpack = MessagePackSerializer.Serialize(type, MessagePackSerializerOptions.Default);
            Type type2 = MessagePackSerializer.Deserialize<Type>(msgpack, MessagePackSerializerOptions.Default);
            Assert.Equal(type, type2);
        }

        [Fact(Skip = "v4 intentional: System.Type serialization is opt-in (TypeFormatterFactory) because deserializing runs Type.GetType over payload names")]
        public void SystemType_Serializable_Null()
        {
            Type type = null;
            byte[] msgpack = MessagePackSerializer.Serialize(type, MessagePackSerializerOptions.Default);
            Type type2 = MessagePackSerializer.Deserialize<Type>(msgpack, MessagePackSerializerOptions.Default);
            Assert.Equal(type, type2);
        }

#if V3COMPAT_EXCLUDED // V3COMPAT-EDIT: v3 subclassable options (LoadType/ThrowIfDeserializingTypeIsDisallowed); v4 gates type loading via TypelessTypeLoader
        [Fact]
        public void SystemType_DeserializeUsesLoadType()
        {
            byte[] msgpack = MessagePackSerializer.Serialize(new TypeHolder { Type = typeof(Uri) }, MessagePackSerializerOptions.Default);
            var options = new RejectingTypeLoadOptions(typeof(Uri));

            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<TypeHolder>(msgpack, options));
            Assert.IsType<TypeLoadException>(ex.InnerException);
            Assert.Equal(1, options.LoadTypeCalls);
        }

        [Fact]
        public void SystemType_DeserializeRejectsDisallowedType()
        {
            byte[] msgpack = MessagePackSerializer.Serialize(new TypeHolder { Type = typeof(Uri) }, MessagePackSerializerOptions.Default);
            var options = new DisallowingTypeOptions(typeof(Uri));

            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<TypeHolder>(msgpack, options));
            Assert.IsType<TypeAccessException>(ex.InnerException);
        }
#endif

        [Fact]
        public void DeserializeByteArrayFromFixArray()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0x03 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 3 }, byte_array);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_LargerNumbers()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0xCC, 0xD4 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 212 }, byte_array);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_LargerThanByte()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0xCD, 0x08, 0x48 }; // 1, 2, 2120
            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<byte[]>(input));
            this.logger.WriteLine(ex.ToString());
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_ZeroLength()
        {
            var input = new byte[] { 0x90 }; // [ ]
            byte[] actual = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Empty(actual);

            // Make sure we're optimized to reuse singleton empty arrays.
            Assert.Same(Array.Empty<byte>(), actual);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_Array32()
        {
            var input = new byte[] { 0xDD, 0, 0, 0, 3, 1, 2, 3 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 3 }, byte_array);
        }

        [Theory]
        [InlineData(600)]
        [InlineData(650)]
        public void BigInteger(int length)
        {
            var x = System.Numerics.BigInteger.Parse(new string('1', length));
            var bytes = MessagePackSerializer.Serialize(x);
            var y = MessagePackSerializer.Deserialize<BigInteger>(bytes);

            Assert.Equal(x, y);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void DateOnly()
        {
            var value = new DateOnly(2012, 3, 5);
            this.AssertRoundtrip(value);
            this.AssertRoundtrip<DateOnly?>(value);
            this.AssertRoundtrip(new[] { value });
        }

        [Fact]
        public void TimeOnly()
        {
            TimeOnly lowRes = new TimeOnly(5, 4, 3);
            this.AssertRoundtrip(lowRes);
            this.AssertRoundtrip<TimeOnly?>(lowRes);
            this.AssertRoundtrip(new[] { lowRes });

            TimeOnly mediumRes = new TimeOnly(5, 4, 3, 2);
            this.AssertRoundtrip(mediumRes);
            this.AssertRoundtrip<TimeOnly?>(mediumRes);
            this.AssertRoundtrip(new[] { mediumRes });

            TimeOnly highRes = new TimeOnly(lowRes.Ticks + 1);
            this.AssertRoundtrip(highRes);
            this.AssertRoundtrip(System.TimeOnly.MaxValue);
        }
#endif

        private void AssertRoundtrip<T>(T value)
        {
            Assert.Equal(value, this.Roundtrip(value, breakupBuffer: false));
            Assert.Equal(value, this.Roundtrip(value, breakupBuffer: true));
        }

        private T Roundtrip<T>(T value, bool breakupBuffer = false)
        {
            byte[] msgpack = MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Default);
            this.logger.WriteLine("{0} 0x{1}", value, TestUtilities.ToHex(msgpack));

            if (breakupBuffer)
            {
                using (Sequence<byte> seq = new Sequence<byte>())
                {
                    seq.Append(msgpack.AsMemory(0, msgpack.Length - 1));
                    seq.Append(msgpack.AsMemory(msgpack.Length - 1, 1));
                    return MessagePackSerializer.Deserialize<T>(seq, MessagePackSerializerOptions.Default);
                }
            }
            else
            {
                return MessagePackSerializer.Deserialize<T>(msgpack, MessagePackSerializerOptions.Default);
            }
        }

        [MessagePackObject]
        public class TypeHolder
        {
            [Key(0)]
            public Type Type { get; set; }
        }

#if V3COMPAT_EXCLUDED // V3COMPAT-EDIT: v3 subclassable options; v4 options are a sealed record
        private class RejectingTypeLoadOptions : MessagePackSerializerOptions
        {
            private readonly Type rejectedType;

            internal RejectingTypeLoadOptions(Type rejectedType)
                : base(MessagePackSerializerOptions.Default)
            {
                this.rejectedType = rejectedType;
            }

            private RejectingTypeLoadOptions(RejectingTypeLoadOptions copyFrom)
                : base(copyFrom)
            {
                this.rejectedType = copyFrom.rejectedType;
                this.LoadTypeCalls = copyFrom.LoadTypeCalls;
            }

            public int LoadTypeCalls { get; private set; }

            public override Type LoadType(string typeName)
            {
                Type type = base.LoadType(typeName);
                this.LoadTypeCalls++;
                return type == this.rejectedType ? null : type;
            }

            protected override MessagePackSerializerOptions Clone() => new RejectingTypeLoadOptions(this);
        }

        private class DisallowingTypeOptions : MessagePackSerializerOptions
        {
            private readonly Type rejectedType;

            internal DisallowingTypeOptions(Type rejectedType)
                : base(MessagePackSerializerOptions.Default)
            {
                this.rejectedType = rejectedType;
            }

            private DisallowingTypeOptions(DisallowingTypeOptions copyFrom)
                : base(copyFrom)
            {
                this.rejectedType = copyFrom.rejectedType;
            }

            public override void ThrowIfDeserializingTypeIsDisallowed(Type type)
            {
                if (type == this.rejectedType)
                {
                    throw new TypeAccessException();
                }
            }

            protected override MessagePackSerializerOptions Clone() => new DisallowingTypeOptions(this);
        }
#endif
    }
}

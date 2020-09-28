// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace MessagePack.Experimental.Tests
{
    public class ArrayTests
    {
        private MessagePackSerializerOptions options;

        [SetUp]
        public void SetUp()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(Resolvers.PrimitiveArrayResolver.Instance, MessagePack.Resolvers.StandardResolver.Instance);
            options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [Test]
        public void EmptySByteArrayTests()
        {
            var array = Array.Empty<SByte>();
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<SByte[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void NullSByteArrayTests()
        {
            var array = default(SByte[]);
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<SByte[]>(encoded, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void DefaultSByteArrayTests(int length)
        {
            var array = new SByte[length];
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<SByte[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void MinValueSByteArrayTests(int length)
        {
            var array = new SByte[length];
            for (var index = 0; index < array.Length; index++)
            {
                array[index] = SByte.MinValue;
            }

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<SByte[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void RandomValueSByteArrayTests(int length)
        {
            var array = new SByte[length];
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<SByte[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [Test]
        public void EmptyInt16ArrayTests()
        {
            var array = Array.Empty<Int16>();
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int16[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void NullInt16ArrayTests()
        {
            var array = default(Int16[]);
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int16[]>(encoded, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void DefaultInt16ArrayTests(int length)
        {
            var array = new Int16[length];
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int16[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void MinValueInt16ArrayTests(int length)
        {
            var array = new Int16[length];
            for (var index = 0; index < array.Length; index++)
            {
                array[index] = Int16.MinValue;
            }

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int16[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void RandomValueInt16ArrayTests(int length)
        {
            var array = new Int16[length];
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int16[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [Test]
        public void EmptyInt32ArrayTests()
        {
            var array = Array.Empty<Int32>();
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int32[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void NullInt32ArrayTests()
        {
            var array = default(Int32[]);
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int32[]>(encoded, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void DefaultInt32ArrayTests(int length)
        {
            var array = new Int32[length];
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int32[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void MinValueInt32ArrayTests(int length)
        {
            var array = new Int32[length];
            for (var index = 0; index < array.Length; index++)
            {
                array[index] = Int32.MinValue;
            }

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int32[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void RandomValueInt32ArrayTests(int length)
        {
            var array = new Int32[length];
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Int32[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [Test]
        public void EmptySingleArrayTests()
        {
            var array = Array.Empty<Single>();
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Single[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void NullSingleArrayTests()
        {
            var array = default(Single[]);
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Single[]>(encoded, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void DefaultSingleArrayTests(int length)
        {
            var array = new Single[length];
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Single[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void MinValueSingleArrayTests(int length)
        {
            var array = new Single[length];
            for (var index = 0; index < array.Length; index++)
            {
                array[index] = Single.MinValue;
            }

            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Single[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }

        [Test]
        public void EmptyBooleanArrayTests()
        {
            var array = Array.Empty<Boolean>();
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Boolean[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void NullBooleanArrayTests()
        {
            var array = default(Boolean[]);
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Boolean[]>(encoded, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(128)]
        [TestCase(4096)]
        public void DefaultBooleanArrayTests(int length)
        {
            var array = new Boolean[length];
            var encoded = MessagePackSerializer.Serialize(array, options);
            Assert.IsNotNull(encoded);
            var decoded = MessagePackSerializer.Deserialize<Boolean[]>(encoded, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < array.Length; index++)
            {
                Assert.AreEqual(array[index], decoded[index]);
            }
        }
    }
}

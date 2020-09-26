// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using NUnit.Framework;

namespace MessagePack.Experimental.Tests
{
    public class UnsafeUnmanagedStructTest
    {
        private MessagePackSerializerOptions options;
        private Random random;

        [OneTimeSetUp]
        public void SetUp()
        {
            random = new Random();
            var resolver = CompositeResolver.Create(new IMessagePackFormatter[] { new UnsafeUnmanagedStructFormatter<Matrix4x4>(50), new UnsafeUnmanagedStructArrayFormatter<Matrix4x4>(51) }, new[] { StandardResolver.Instance });
            options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [Test]
        public void EmptyArrayTest()
        {
            var original = Array.Empty<Matrix4x4>();
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<Matrix4x4[]>(binary, options);
            Assert.AreSame(original, decoded);
        }

        [Test]
        public void NullArrayTest()
        {
            var binary = MessagePackSerializer.Serialize(default(Matrix4x4[]), options);
            var decoded = MessagePackSerializer.Deserialize<Matrix4x4[]>(binary, options);
            Assert.IsNull(decoded);
        }

        [TestCase(1)]
        [TestCase(16)]
        [TestCase(256)]
        [TestCase(1024)]
        [TestCase(1024 * 16)]
        public void ZeroArrayTest(int length)
        {
            var original = new Matrix4x4[length];
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<Matrix4x4[]>(binary, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            foreach (var x4 in decoded)
            {
                Assert.IsTrue(x4.Equals(default(Matrix4x4)));
            }
        }

        [TestCase(1)]
        [TestCase(16)]
        [TestCase(256)]
        [TestCase(1024)]
        [TestCase(1024 * 16)]
        public void RandomArrayTest(int length)
        {
            var original = new Matrix4x4[length];
            for (var i = 0; i < original.Length; i++)
            {
                original[i].M11 = (float)random.NextDouble();
                original[i].M12 = (float)random.NextDouble();
                original[i].M13 = (float)random.NextDouble();
                original[i].M14 = (float)random.NextDouble();
                original[i].M21 = (float)random.NextDouble();
                original[i].M22 = (float)random.NextDouble();
                original[i].M23 = (float)random.NextDouble();
                original[i].M24 = (float)random.NextDouble();
                original[i].M31 = (float)random.NextDouble();
                original[i].M32 = (float)random.NextDouble();
                original[i].M33 = (float)random.NextDouble();
                original[i].M34 = (float)random.NextDouble();
                original[i].M41 = (float)random.NextDouble();
                original[i].M42 = (float)random.NextDouble();
                original[i].M43 = (float)random.NextDouble();
                original[i].M44 = (float)random.NextDouble();
            }

            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<Matrix4x4[]>(binary, options);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(length, decoded.Length);
            for (var index = 0; index < decoded.Length; index++)
            {
                Assert.IsTrue(decoded[index] == original[index]);
            }
        }

        [Test]
        public void MatrixTest()
        {
            var original = new Matrix4x4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<Matrix4x4>(binary, options);
            Assert.IsTrue(original == decoded);
        }
    }
}

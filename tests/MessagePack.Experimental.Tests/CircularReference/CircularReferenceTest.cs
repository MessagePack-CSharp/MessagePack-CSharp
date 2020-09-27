// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;
using MessagePack.Resolvers;
using NUnit.Framework;

namespace MessagePack.Experimental.Tests.CircularReference
{
    public class CircularReferenceTest
    {
        private MessagePackSerializerOptions options;

        [OneTimeSetUp]
        public void SetUp()
        {
            var formatter = MessagePackSerializerOptions.Standard.Resolver.GetFormatterWithVerify<CircleExample>();
            var resolver = CompositeResolver.Create(new IMessagePackFormatter[] { new CircularFormatter<CircleExample>(formatter) }, new[] { StandardResolver.Instance });
            options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [Test]
        public void NullTest()
        {
            var original = default(CircleExample);
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<CircleExample>(binary, options);
            Assert.IsNull(decoded);
        }

        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void FieldNullTest(int id)
        {
            var original = new CircleExample() { Id = id };
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<CircleExample>(binary, options);
            Assert.IsNotNull(decoded);
            Assert.IsNull(decoded.Parent);
            Assert.IsNull(decoded.Child0);
            Assert.IsNull(decoded.Child1);
            Assert.AreEqual(decoded.Id, id);
        }

        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void SelfParentTest(int id)
        {
            var original = new CircleExample() { Id = id };
            original.Parent = original;
            var binary = MessagePackSerializer.Serialize(original, options);
            var decoded = MessagePackSerializer.Deserialize<CircleExample>(binary, options);
            Assert.IsNotNull(decoded);
            Assert.AreSame(decoded.Parent, decoded);
            Assert.IsNull(decoded.Child0);
            Assert.IsNull(decoded.Child1);
            Assert.AreEqual(decoded.Id, id);
        }
    }
}

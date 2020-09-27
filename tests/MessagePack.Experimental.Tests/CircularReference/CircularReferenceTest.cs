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
            var resolver = CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    new CircularFormatter<CircleExample>(StandardResolver.Instance.GetFormatterWithVerify<CircleExample>()!, CircleExampleOverwriter.Instance),
                },
                new[] { StandardResolver.Instance });
            options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [Test]
        public void NullTest()
        {
            var original = default(CircleExample);

            var binary = MessagePackSerializer.Serialize(original, options);
            var result = MessagePackSerializer.Deserialize<CircleExample>(binary, options);

            Assert.IsNull(result);
        }

        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void FieldNullTest(int id)
        {
            var original = new CircleExample() { Id = id };

            var binary = MessagePackSerializer.Serialize(original, options);
            var result = MessagePackSerializer.Deserialize<CircleExample>(binary, options);

            Assert.IsNotNull(result);
            Assert.IsNull(result.Parent);
            Assert.IsNull(result.Child0);
            Assert.IsNull(result.Child1);
            Assert.AreEqual(result.Id, id);
        }

        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void SelfParentTest(int id)
        {
            var original = new CircleExample() { Id = id };
            original.Parent = original;

            var binary = MessagePackSerializer.Serialize(original, options);
            var result = MessagePackSerializer.Deserialize<CircleExample>(binary, options);

            Assert.IsNotNull(result);
            Assert.AreSame(result.Parent, result);
            Assert.IsNull(result.Child0);
            Assert.IsNull(result.Child1);
            Assert.AreEqual(result.Id, id);
        }

        [TestCase(0, 1, 2)]
        [TestCase(int.MaxValue, int.MinValue, -1)]
        [TestCase(114514, 1919, -810931)]
        public void SimpleTreeTest(int id0, int id1, int id2)
        {
            var child0 = new CircleExample() { Id = id1 };
            var child1 = new CircleExample() { Id = id2 };
            var original = new CircleExample()
            {
                Id = id0,
                Child0 = child0,
                Child1 = child1,
            };
            child0.Parent = original;
            child1.Parent = original;

            var binary = MessagePackSerializer.Serialize(original, options);
            var result = MessagePackSerializer.Deserialize<CircleExample>(binary, options);

            Assert.IsNotNull(result);
            Assert.IsNull(result.Parent);
            Assert.IsNotNull(result.Child0);
            Assert.AreSame(result.Child0!.Parent, result);
            Assert.IsNotNull(result.Child1);
            Assert.AreSame(result.Child1!.Parent, result);
            Assert.AreEqual(result.Id, id0);
            Assert.AreEqual(result.Child0.Id, id1);
            Assert.AreEqual(result.Child1.Id, id2);
        }
    }
}

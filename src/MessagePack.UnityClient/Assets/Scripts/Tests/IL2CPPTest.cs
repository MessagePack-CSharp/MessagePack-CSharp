// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Tests
{
    public class IL2CPPTest
    {
        //In CI, failing?
        //[Test]
        //public void CheckGeneraterMessagePackResolverExists()
        //{
        //    _ = GeneratedMessagePackResolver.Instance;
        //}

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

        [Test]
        public void LZ4Block()
        {
            var xs = Enumerable.Range(1, 1000)
                .Select(x => new MyClass { Age = x, Name = Guid.NewGuid().ToString() })
                .ToArray();

            var lz4Option = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block);
            var bin = MessagePackSerializer.Serialize(xs, lz4Option);

            var ys = MessagePackSerializer.Deserialize<MyClass[]>(bin, lz4Option);
            CollectionAssert.AreEqual(xs, ys);
        }

        [Test]
        public void LZ4BlockArray()
        {
            var xs = Enumerable.Range(1, 1000)
                .Select(x => new MyClass { Age = x, Name = Guid.NewGuid().ToString() })
                .ToArray();

            var lz4Option = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray);
            var bin = MessagePackSerializer.Serialize(xs, lz4Option);

            var ys = MessagePackSerializer.Deserialize<MyClass[]>(bin, lz4Option);
            CollectionAssert.AreEqual(xs, ys);
        }

        [Test]
        public void EnumSerialize()
        {
            var bin = MessagePackSerializer.Serialize(MessagePackCompression.Lz4BlockArray);
            var v2 = MessagePackSerializer.Deserialize<MessagePackCompression>(bin);

            Assert.AreEqual(MessagePackCompression.Lz4BlockArray, v2);
        }

        [Test]
        public void ListSerialize()
        {
            var bin = MessagePackSerializer.Serialize(new List<ulong> { 10UL });
            var v2 = MessagePackSerializer.Deserialize<List<ulong>>(bin);

            CollectionAssert.AreEqual(new List<ulong> { 10UL }, v2);
        }

        [Test]
        public void ConcurrentBagSerialize()
        {
            var bin = MessagePackSerializer.Serialize(new ConcurrentBag<ulong> { 10UL });
            var v2 = MessagePackSerializer.Deserialize<ConcurrentBag<ulong>>(bin);

            CollectionAssert.AreEqual(new ConcurrentBag<ulong> { 10UL }, v2);
        }
    }

    [MessagePackObject]
    public class MyClass : IEquatable<MyClass>
    {
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string? Name { get; set; }

        public bool Equals(MyClass other)
        {
            return Age == other.Age && Name == other.Name;
        }
    }
}

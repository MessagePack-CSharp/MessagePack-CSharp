using System;
using System.Collections.Generic;
using NUnit.Framework;
using RuntimeUnitTestToolkit;
using SharedData;

namespace MessagePack.UnityClient.Tests
{
    public class CollectionFormatterTest
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, MsgPackUnsafeDefaultResolver.Options), MsgPackUnsafeDefaultResolver.Options);
        }

        [Test]
        public void DictionaryTestAll()
        {
            var dict = new Dictionary<int, int>() { { 1, 100 } };
            var dict2 = Convert(dict);
            dict2[1].Is(100);
        }

        [Test]
        public void InterfaceDictionaryTest()
        {
            var a = (IDictionary<int, int>)new Dictionary<int, int>() { { 1, 100 } };
            var c = (IDictionary<int, int>)null;

            Convert(a)[1].Is(100);
            Convert(c).IsNull();
        }

        [Test]
        public void CollectionTest()
        {
            Convert(new[] { 1, 10, 100 }).IsCollection(1, 10, 100);
            Convert(new List<string> { "1", "10", "100" }).IsCollection("1", "10", "100");
        }
    }
}

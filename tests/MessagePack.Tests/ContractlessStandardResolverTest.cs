using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ContractlessStandardResolverTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        public class Address
        {
            public string Street { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
            public object[] /*Address*/ Addresses { get; set; }
        }

        public class V1
        {
            public int ABCDEFG1 { get; set; }
            public int ABCDEFG3 { get; set; }
        }



        public class V2
        {
            public int ABCDEFG1 { get; set; }
            public int ABCDEFG2 { get; set; }
            public int ABCDEFG3 { get; set; }
        }

        public class Dup
        {
            public int ABCDEFGH { get; set; }
            public int ABCDEFGHIJKL { get; set; }
        }

        public class BinSearchSmall
        {
            public int MyP1 { get; set; }
            public int MyP2 { get; set; }
            public int MyP3 { get; set; }
            public int MyP4 { get; set; }
            public int MyP5 { get; set; }
            public int MyP6 { get; set; }
            public int MyP7 { get; set; }
            public int MyP8 { get; set; }
            public int MyP9 { get; set; }
        }

        public class BinSearchWithBranch
        {
            public int MyProperty1 { get; set; }
            public int MyProperty2 { get; set; }
            public int MyProperty3 { get; set; }
            public int MyProperty4 { get; set; }
            public int MyProperty5 { get; set; }
            public int MyProperty6 { get; set; }
            public int MyProperty7 { get; set; }
            public int MyProperty8 { get; set; }
            public int MyProperty9 { get; set; }
        }

        public class LongestString
        {
            public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1 { get; set; }
            public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2 { get; set; }
            public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty { get; set; }
            public int OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ { get; set; }
        }

        [Fact]
        public void SimpleTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new[]
                {
                        new Address { Street = "St." },
                        new Address { Street = "Ave." }
                    }
            };

            var result = serializer.Serialize(p, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            serializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

            var p2 = serializer.Deserialize<Person>(result, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            p2.Name.Is("John");
            var addresses = p2.Addresses as IList;
            var d1 = addresses[0] as IDictionary;
            var d2 = addresses[1] as IDictionary;
            (d1["Street"] as string).Is("St.");
            (d2["Street"] as string).Is("Ave.");
        }

        [Fact]
        public void Versioning()
        {
            var v1 = serializer.Serialize(new V1 { ABCDEFG1 = 10, ABCDEFG3 = 99 }, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v2 = serializer.Serialize(new V2 { ABCDEFG1 = 350, ABCDEFG2 = 34, ABCDEFG3 = 500 }, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            var v1_1 = serializer.Deserialize<V1>(v1, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v1_2 = serializer.Deserialize<V1>(v2, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v2_1 = serializer.Deserialize<V2>(v1, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v2_2 = serializer.Deserialize<V2>(v2, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            v1_1.ABCDEFG1.Is(10); v1_1.ABCDEFG3.Is(99);
            v1_2.ABCDEFG1.Is(350); v1_2.ABCDEFG3.Is(500);
            v2_1.ABCDEFG1.Is(10); v2_1.ABCDEFG2.Is(0); v2_1.ABCDEFG3.Is(99);
            v2_2.ABCDEFG1.Is(350); v2_2.ABCDEFG2.Is(34); v2_2.ABCDEFG3.Is(500);
        }

        [Fact]
        public void DuplicateAutomata()
        {
            var bin = serializer.Serialize(new Dup { ABCDEFGH = 10, ABCDEFGHIJKL = 99 }, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v = serializer.Deserialize<Dup>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            v.ABCDEFGH.Is(10);
            v.ABCDEFGHIJKL.Is(99);
        }

        [Fact]
        public void BinSearchSmallCheck()
        {
            var o = new BinSearchSmall
            {
                MyP1 = 1,
                MyP2 = 10,
                MyP3 = 1000,
                MyP4 = 100000,
                MyP5 = 32421,
                MyP6 = 52521,
                MyP7 = 46363631,
                MyP8 = 7373731,
                MyP9 = 73573731,
            };
            var bin = serializer.Serialize(o, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v = serializer.Deserialize<BinSearchSmall>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            v.IsStructuralEqual(o);
        }

        [Fact]
        public void BinSearchWithBranchCheck()
        {
            var o = new BinSearchWithBranch
            {
                MyProperty1 = 1,
                MyProperty2 = 10,
                MyProperty3 = 1000,
                MyProperty4 = 100000,
                MyProperty5 = 32421,
                MyProperty6 = 52521,
                MyProperty7 = 46363631,
                MyProperty8 = 7373731,
                MyProperty9 = 73573731,
            };
            var bin = serializer.Serialize(o, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v = serializer.Deserialize<BinSearchWithBranch>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            v.IsStructuralEqual(o);
        }

        [Fact]
        public void LongestStringCheck()
        {
            var o = new LongestString
            {
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1 = 431413,
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2 = 352525252,
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty = 532525252,
                OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ = 3352666,
            };
            var bin = serializer.Serialize(o, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var v = serializer.Deserialize<LongestString>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            v.IsStructuralEqual(o);
        }
    }
}

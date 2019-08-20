using NUnit.Framework;
using RuntimeUnitTestToolkit;
using SharedData;
using System;
using System.Collections.Generic;

namespace MessagePack.UnityClient.Tests
{
    public class Contractless
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        [Test]
        public void Versioning()
        {
            var v1 = MessagePackSerializer.Serialize(new V1 { ABCDEFG1 = 10, ABCDEFG3 = 99 });
            var v2 = MessagePackSerializer.Serialize(new V2 { ABCDEFG1 = 350, ABCDEFG2 = 34, ABCDEFG3 = 500 });

            var v1_1 = MessagePackSerializer.Deserialize<V1>(v1);
            var v1_2 = MessagePackSerializer.Deserialize<V1>(v2);
            var v2_1 = MessagePackSerializer.Deserialize<V2>(v1);
            var v2_2 = MessagePackSerializer.Deserialize<V2>(v2);

            v1_1.ABCDEFG1.Is(10); v1_1.ABCDEFG3.Is(99);
            v1_2.ABCDEFG1.Is(350); v1_2.ABCDEFG3.Is(500);
            v2_1.ABCDEFG1.Is(10); v2_1.ABCDEFG2.Is(0); v2_1.ABCDEFG3.Is(99);
            v2_2.ABCDEFG1.Is(350); v2_2.ABCDEFG2.Is(34); v2_2.ABCDEFG3.Is(500);
        }

        [Test]
        public void DuplicateAutomata()
        {
            var bin = MessagePackSerializer.Serialize(new Dup { ABCDEFGH = 10, ABCDEFGHIJKL = 99 });
            var v = MessagePackSerializer.Deserialize<Dup>(bin);

            v.ABCDEFGH.Is(10);
            v.ABCDEFGHIJKL.Is(99);
        }

        [Test]
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
            var bin = MessagePackSerializer.Serialize(o);
            var v = MessagePackSerializer.Deserialize<BinSearchSmall>(bin);

            v.MyP1.Is(o.MyP1);
            v.MyP2.Is(o.MyP2);
            v.MyP3.Is(o.MyP3);
            v.MyP4.Is(o.MyP4);
            v.MyP5.Is(o.MyP5);
            v.MyP6.Is(o.MyP6);
            v.MyP7.Is(o.MyP7);
            v.MyP8.Is(o.MyP8);
            v.MyP9.Is(o.MyP9);
        }

        [Test]
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
            var bin = MessagePackSerializer.Serialize(o);
            var v = MessagePackSerializer.Deserialize<BinSearchWithBranch>(bin);

            v.MyProperty1.Is(o.MyProperty1);
            v.MyProperty2.Is(o.MyProperty2);
            v.MyProperty3.Is(o.MyProperty3);
            v.MyProperty4.Is(o.MyProperty4);
            v.MyProperty5.Is(o.MyProperty5);
            v.MyProperty6.Is(o.MyProperty6);
            v.MyProperty7.Is(o.MyProperty7);
            v.MyProperty8.Is(o.MyProperty8);
            v.MyProperty9.Is(o.MyProperty9);
        }

        [Test]
        public void LongestStringCheck()
        {
            var o = new LongestString
            {
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1 = 431413,
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2 = 352525252,
                MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty = 532525252,
                OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ = 3352666,
            };
            var bin = MessagePackSerializer.Serialize(o);
            var v = MessagePackSerializer.Deserialize<LongestString>(bin);

            v.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1.Is(o.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1);
            v.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2.Is(o.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2);
            v.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty.Is(o.MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty);
            v.OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ.Is(o.OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ);
        }
    }

    [MessagePackObject(true)]
    public class V1
    {
        public int ABCDEFG1 { get; set; }
        public int ABCDEFG3 { get; set; }
    }


    [MessagePackObject(true)]
    public class V2
    {
        public int ABCDEFG1 { get; set; }
        public int ABCDEFG2 { get; set; }
        public int ABCDEFG3 { get; set; }
    }

    [MessagePackObject(true)]
    public class Dup
    {
        public int ABCDEFGH { get; set; }
        public int ABCDEFGHIJKL { get; set; }
    }

    [MessagePackObject(true)]
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

    [MessagePackObject(true)]
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

    [MessagePackObject(true)]
    public class LongestString
    {
        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1 { get; set; }
        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2 { get; set; }
        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty { get; set; }
        public int OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ { get; set; }
    }

}

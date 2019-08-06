using NUnit.Framework;
using RuntimeUnitTestToolkit;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class UnionTest
    {
        [Test]
        public void Union()
        {
            {
                var data = new MySubUnion1 { One = 23 };
                var data2 = new MySubUnion1 { One = 23 };

                var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data, MsgPackUnsafeDefaultResolver.Options);
                var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2, MsgPackUnsafeDefaultResolver.Options);

                var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1, MsgPackUnsafeDefaultResolver.Options);
                var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1, MsgPackUnsafeDefaultResolver.Options);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null, MsgPackUnsafeDefaultResolver.Options);

                var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null, MsgPackUnsafeDefaultResolver.Options);

                MessagePackSerializer.Deserialize<IUnionChecker>(null1, MsgPackUnsafeDefaultResolver.Options).IsNull();
                MessagePackSerializer.Deserialize<IUnionChecker2>(null1, MsgPackUnsafeDefaultResolver.Options).IsNull();

                var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 }, MsgPackUnsafeDefaultResolver.Options);
                MessagePackSerializer.Deserialize<IUnionChecker>(hoge, MsgPackUnsafeDefaultResolver.Options).IsNull();
            }

            {
                var data = new MySubUnion2 { Two = 23 };
                var data2 = new MySubUnion2 { Two = 23 };

                var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data, MsgPackUnsafeDefaultResolver.Options);
                var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2, MsgPackUnsafeDefaultResolver.Options);

                var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1, MsgPackUnsafeDefaultResolver.Options);
                var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1, MsgPackUnsafeDefaultResolver.Options);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null, MsgPackUnsafeDefaultResolver.Options);
                var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null, MsgPackUnsafeDefaultResolver.Options);

                MessagePackSerializer.Deserialize<IUnionChecker>(null1, MsgPackUnsafeDefaultResolver.Options).IsNull();
                MessagePackSerializer.Deserialize<IUnionChecker2>(null1, MsgPackUnsafeDefaultResolver.Options).IsNull();

                var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 }, MsgPackUnsafeDefaultResolver.Options);
                MessagePackSerializer.Deserialize<IUnionChecker>(hoge, MsgPackUnsafeDefaultResolver.Options).IsNull();
            }
        }
    }
}

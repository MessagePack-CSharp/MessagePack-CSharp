using RuntimeUnitTestToolkit;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class UnionTest
    {
        public void Union()
        {
            {
                var data = new MySubUnion1 { One = 23 };
                var data2 = new MySubUnion1 { One = 23 };

                var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data);
                var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2);

                var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);
                var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null);

                var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null);

                MessagePackSerializer.Deserialize<IUnionChecker>(null1).IsNull();
                MessagePackSerializer.Deserialize<IUnionChecker2>(null1).IsNull();


                var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
                MessagePackSerializer.Deserialize<IUnionChecker>(hoge).IsNull();
            }
            {
                var data = new MySubUnion2 { Two = 23 };
                var data2 = new MySubUnion2 { Two = 23 };

                var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data);
                var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2);

                var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);
                var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null);
                var null2 = MessagePackSerializer.Serialize<IUnionChecker2>(null);

                MessagePackSerializer.Deserialize<IUnionChecker>(null1).IsNull();
                MessagePackSerializer.Deserialize<IUnionChecker2>(null1).IsNull();


                var hoge = MessagePackSerializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
                MessagePackSerializer.Deserialize<IUnionChecker>(hoge).IsNull();
            }
        }
    }
}
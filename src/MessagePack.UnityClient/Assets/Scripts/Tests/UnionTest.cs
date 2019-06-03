using RuntimeUnitTestToolkit;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class UnionTest
    {
        private readonly MessagePackSerializer serializer = new MessagePackSerializer(MsgPackUnsafeDefaultResolver.Instance);

        public void Union()
        {
            {
                var data = new MySubUnion1 { One = 23 };
                var data2 = new MySubUnion1 { One = 23 };

                var unionData1 = this.serializer.Serialize<IUnionChecker>(data);
                var unionData2 = this.serializer.Serialize<IUnionChecker2>(data2);

                var reData1 = this.serializer.Deserialize<IUnionChecker>(unionData1);
                var reData2 = this.serializer.Deserialize<IUnionChecker>(unionData1);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = this.serializer.Serialize<IUnionChecker>(null);

                var null2 = this.serializer.Serialize<IUnionChecker2>(null);

                this.serializer.Deserialize<IUnionChecker>(null1).IsNull();
                this.serializer.Deserialize<IUnionChecker2>(null1).IsNull();


                var hoge = this.serializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
                this.serializer.Deserialize<IUnionChecker>(hoge).IsNull();
            }
            {
                var data = new MySubUnion2 { Two = 23 };
                var data2 = new MySubUnion2 { Two = 23 };

                var unionData1 = this.serializer.Serialize<IUnionChecker>(data);
                var unionData2 = this.serializer.Serialize<IUnionChecker2>(data2);

                var reData1 = this.serializer.Deserialize<IUnionChecker>(unionData1);
                var reData2 = this.serializer.Deserialize<IUnionChecker>(unionData1);

                reData1.IsInstanceOf<IUnionChecker>();
                reData2.IsInstanceOf<IUnionChecker2>();

                var null1 = this.serializer.Serialize<IUnionChecker>(null);
                var null2 = this.serializer.Serialize<IUnionChecker2>(null);

                this.serializer.Deserialize<IUnionChecker>(null1).IsNull();
                this.serializer.Deserialize<IUnionChecker2>(null1).IsNull();


                var hoge = this.serializer.Serialize<IIVersioningUnion>(new VersioningUnion { FV = 0 });
                this.serializer.Deserialize<IUnionChecker>(hoge).IsNull();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class PrimitiveResolverTest
    {

        [Theory]
        [InlineData((bool)true)]
        [InlineData((byte)10)]
        [InlineData((sbyte)123)]
        [InlineData((short)(-4123))]
        [InlineData((ushort)42342)]
        [InlineData((int)(int.MaxValue))]
        [InlineData((UInt32)432423)]
        [InlineData((long)(235))]
        [InlineData((UInt64)65346464)]
        [InlineData((float)1241.42342f)]
        [InlineData((double)1241312.4242342)]
        [InlineData("hogehoge")]
        [InlineData(new byte[] { 1, 10, 100 })]
        public void PrimitiveObjectTest<T>(T x)
        {
            var bin = MessagePackSerializer.Serialize<object>(x);
            var re1 = MessagePackSerializer.Deserialize<object>(bin);
            ((T)re1).Is(x);
        }

        [Fact]
        public void PrimitiveTest2()
        {
            {
                var x = DateTime.UtcNow;
                var bin = MessagePackSerializer.Serialize<object>(x);
                var re1 = MessagePackSerializer.Deserialize<object>(bin);
                (re1).Is(x);
            }
            {
                var x = 'あ';
                var bin = MessagePackSerializer.Serialize<object>(x);
                var re1 = MessagePackSerializer.Deserialize<object>(bin);
                ((char)(ushort)re1).Is(x);
            }
            {
                var x = SharedData.IntEnum.C;
                var bin = MessagePackSerializer.Serialize<object>(x);
                var re1 = MessagePackSerializer.Deserialize<object>(bin);
                ((SharedData.IntEnum)re1).Is(x);
            }
            {
                var x = new object[] { 1, 10, 1000, new[] { 999, 424 }, new Dictionary<string, int> { { "hoge", 100 }, { "foo", 999 } }, true };

                var bin = MessagePackSerializer.Serialize<object>(x);
                var re1 = (object[])MessagePackSerializer.Deserialize<object>(bin);

                x[0].Is(re1[0]);
                x[1].Is(re1[1]);
                x[2].Is(re1[2]);
                x[5].Is(re1[5]);

                ((int[])x[3])[0].Is(((object[])re1[3])[0]);
                ((int[])x[3])[1].Is(((object[])re1[3])[1]);

                (x[4] as Dictionary<string, int>)["hoge"].Is((re1[4] as Dictionary<object, object>)["hoge"]);
                (x[4] as Dictionary<string, int>)["foo"].Is((re1[4] as Dictionary<object, object>)["foo"]);
            }
        }
    }
}

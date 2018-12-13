using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class AnonymousTypeTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        public void SerializeAndToJson()
        {
            var testData = new { Hoge = 100, Huga = true, Yaki = new { Rec = 1, T = 10 }, Nano = "nanoanno" };

            var data = serializer.Serialize(testData, ContractlessStandardResolver.Instance);

            serializer.ToJson(data).Is(@"{""Hoge"":100,""Huga"":true,""Yaki"":{""Rec"":1,""T"":10},""Nano"":""nanoanno""}");
        }
    }
}

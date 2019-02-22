using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ToJsonTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();
        private LZ4MessagePackSerializer lz4Serializer = new LZ4MessagePackSerializer();

        string JsonConvert(string json)
        {
            return serializer.ConvertToJson(serializer.ConvertFromJson(json));
        }

        string JsonConvertLZ4(string json)
        {
            return lz4Serializer.ConvertToJson(lz4Serializer.ConvertFromJson(json));
        }

        [Theory]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("4141.431242")]
        [InlineData("414")]
        [InlineData(@"{""hoge"":100,""huga"":null,""nano"":false}")]
        [InlineData(@"[1,20,false,true,3424.432]")]
        public void SimpleToJson(string json)
        {
            JsonConvert(json).Is(json);
            JsonConvertLZ4(json).Is(json);
        }

        [Fact]
        public void ComplexToJson()
        {
            var json = @"{""reservations"":[{""instances"":[{""type"":""small"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""large"",""state"":{""name"":""stopped""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]}]},{""instances"":[{""type"":""medium"",""state"":{""name"":""terminated""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""xlarge"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""DB""]},{""Key"":""version"",""Values"":[""1""]}]}]}]}";
            JsonConvert(json).Is(json);
            JsonConvertLZ4(json).Is(json);
        }

        [Fact]
        public void FloatJson()
        {
            var f = 3.33f;
            var xs = serializer.Serialize(f);
            var json = serializer.ConvertToJson(xs);
            json.Is("3.33");
        }

        [Theory]
        [InlineData(@"{""Amount"":1.0E-6}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1.0E-06}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1E-6}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1E-06}", @"{""Amount"":1E-06}")]
        public void ScientificFloatJsonRoundTrip(string inputJson, string expectedRoundTripJson)
        {
            JsonConvert(inputJson).Is(expectedRoundTripJson);
            JsonConvertLZ4(inputJson).Is(expectedRoundTripJson);
        }
    }
}

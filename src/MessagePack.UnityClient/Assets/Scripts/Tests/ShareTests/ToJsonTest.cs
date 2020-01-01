// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class ToJsonTest
    {
        private static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        private string JsonConvert(string json, MessagePackSerializerOptions options)
        {
            var sequence = new Sequence<byte>();
            var sequenceWriter = new MessagePackWriter(sequence);
            MessagePackSerializer.ConvertFromJson(json, ref sequenceWriter, options);
            sequenceWriter.Flush();
            return MessagePackSerializer.ConvertToJson(sequence.AsReadOnlySequence, options);
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
            this.JsonConvert(json, MessagePackSerializerOptions.Standard).Is(json);
            this.JsonConvert(json, LZ4Standard).Is(json);
        }

        [Fact]
        public void ComplexToJson()
        {
            var json = @"{""reservations"":[{""instances"":[{""type"":""small"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""large"",""state"":{""name"":""stopped""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]}]},{""instances"":[{""type"":""medium"",""state"":{""name"":""terminated""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""xlarge"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""DB""]},{""Key"":""version"",""Values"":[""1""]}]}]}]}";
            this.JsonConvert(json, MessagePackSerializerOptions.Standard).Is(json);
            this.JsonConvert(json, LZ4Standard).Is(json);
        }

        [Fact]
        public void FloatJson()
        {
            var f = 3.33f;
            var xs = MessagePackSerializer.Serialize(f);
            var json = MessagePackSerializer.ConvertToJson(xs);
            json.Is("3.33");
        }

        [Theory]
        [InlineData(@"{""Amount"":1.0E-6}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1.0E-06}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1E-6}", @"{""Amount"":1E-06}")]
        [InlineData(@"{""Amount"":1E-06}", @"{""Amount"":1E-06}")]
        public void ScientificFloatJsonRoundTrip(string inputJson, string expectedRoundTripJson)
        {
            this.JsonConvert(inputJson, MessagePackSerializerOptions.Standard).Is(expectedRoundTripJson);
            this.JsonConvert(inputJson, LZ4Standard).Is(expectedRoundTripJson);
        }

        [Fact]
        public void ExtJson()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteExtensionFormat(new ExtensionResult(47, new byte[] { 1, 10, 100 }));
            writer.Flush();

            var msgpack = sequence.AsReadOnlySequence;
            var str = MessagePackSerializer.ConvertToJson(msgpack);
            var b64 = Convert.ToBase64String(new byte[] { 1, 10, 100 });

            str.Is(@"[47,""" + b64 + @"""]");
        }

        [Fact]
        public void DateTimeJson()
        {
            var now = new DateTime(1999, 12, 19, 11, 19, 19, DateTimeKind.Utc);
            var bin = MessagePackSerializer.Serialize(now);
            var json = MessagePackSerializer.ConvertToJson(bin);
            json.Is(@"""1999-12-19T11:19:19.0000000Z""");
        }
    }
}

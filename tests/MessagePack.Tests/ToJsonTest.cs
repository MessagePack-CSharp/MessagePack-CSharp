﻿// Copyright (c) All contributors. All rights reserved.
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
        private string JsonConvert(string json, MessagePackSerializerOptions options)
        {
            var sequence = new Sequence<byte>();
            var sequenceWriter = new MessagePackWriter(sequence);
            MessagePackSerializer.ConvertFromJson(json, sequenceWriter, options);
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
            this.JsonConvert(json, MessagePackSerializerOptions.Default).Is(json);
            this.JsonConvert(json, MessagePackSerializerOptions.LZ4Default).Is(json);
        }

        [Fact]
        public void ComplexToJson()
        {
            var json = @"{""reservations"":[{""instances"":[{""type"":""small"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""large"",""state"":{""name"":""stopped""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]}]},{""instances"":[{""type"":""medium"",""state"":{""name"":""terminated""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""xlarge"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""DB""]},{""Key"":""version"",""Values"":[""1""]}]}]}]}";
            this.JsonConvert(json, MessagePackSerializerOptions.Default).Is(json);
            this.JsonConvert(json, MessagePackSerializerOptions.LZ4Default).Is(json);
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
            this.JsonConvert(inputJson, MessagePackSerializerOptions.Default).Is(expectedRoundTripJson);
            this.JsonConvert(inputJson, MessagePackSerializerOptions.LZ4Default).Is(expectedRoundTripJson);
        }
    }
}

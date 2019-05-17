﻿using System;
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
        private MessagePackSerializer serializer = new MessagePackSerializer();
        private LZ4MessagePackSerializer lz4Serializer = new LZ4MessagePackSerializer();

        string JsonConvert(string json, MessagePackSerializer serializer)
        {
            var sequence = new Sequence<byte>();
            var sequenceWriter = new MessagePackWriter(sequence);
            serializer.ConvertFromJson(json, ref sequenceWriter);
            sequenceWriter.Flush();
            return serializer.ConvertToJson(sequence.AsReadOnlySequence);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("4141.431242")]
        [InlineData("0.00431242")]
        [InlineData("1.0431242")]
        [InlineData("414")]
        [InlineData(@"{""hoge"":100,""huga"":null,""nano"":false}")]
        [InlineData(@"[1,20,false,true,3424.432]")]
        public void SimpleToJson(string json)
        {
            JsonConvert(json, serializer).Is(json);
            JsonConvert(json, lz4Serializer).Is(json);
        }

        [Fact]
        public void ComplexToJson()
        {
            var json = @"{""reservations"":[{""instances"":[{""type"":""small"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""large"",""state"":{""name"":""stopped""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]}]},{""instances"":[{""type"":""medium"",""state"":{""name"":""terminated""},""tags"":[{""Key"":""Name"",""Values"":[""Web""]},{""Key"":""version"",""Values"":[""1""]}]},{""type"":""xlarge"",""state"":{""name"":""running""},""tags"":[{""Key"":""Name"",""Values"":[""DB""]},{""Key"":""version"",""Values"":[""1""]}]}]}]}";
            JsonConvert(json, serializer).Is(json);
            JsonConvert(json, lz4Serializer).Is(json);
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
            JsonConvert(inputJson, serializer).Is(expectedRoundTripJson);
            JsonConvert(inputJson, lz4Serializer).Is(expectedRoundTripJson);
        }

        [Fact]
        public void TestIfFloatCanBeSerialized()
        {
            // Arrange
            var value = 0.1f;

            // Act
            var bytes = MessagePackSerializer.Serialize(value);
            var deserialized = MessagePackSerializer.Deserialize<float>(bytes);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void TestIfFloatCanBeSerializedLz4()
        {
            // Arrange
            var value = 0.1f;

            // Act
            var bytes = LZ4MessagePackSerializer.Serialize(value);
            var deserialized = LZ4MessagePackSerializer.Deserialize<float>(bytes);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void TestIfFloatCanBeSerializedJson()
        {
            // Arrange
            var value = 0.1f;

            // Act
            var json = MessagePackSerializer.ToJson(value);
            var bytes = MessagePackSerializer.FromJson(json);
            var deserialized = MessagePackSerializer.Deserialize<float>(bytes);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void TestIfFloatCanBeSerializedJsonLz4()
        {
            // Arrange
            var value = 0.1f;

            // Act
            var json = LZ4MessagePackSerializer.ToJson(value);
            var bytes = LZ4MessagePackSerializer.FromJson(json);
            var deserialized = LZ4MessagePackSerializer.Deserialize<float>(bytes);

            // Assert
            Assert.Equal(value, deserialized);
        }
    }
}

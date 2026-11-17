// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackTestSuiteTests
    {
        private const string VectorResourceName = "MessagePackTestSuite.json";

        public static IEnumerable<object[]> DecodeTestCases
        {
            get
            {
                foreach (VectorCase vectorCase in ReadVectorCases())
                {
                    for (int i = 0; i < vectorCase.Encodings.Length; i++)
                    {
                        yield return new object[]
                        {
                            $"{vectorCase.Tag}:{vectorCase.Index}:{i}",
                            vectorCase.Kind,
                            vectorCase.ValueJson,
                            vectorCase.Encodings[i],
                        };
                    }
                }
            }
        }

        public static IEnumerable<object[]> EncodeTestCases
        {
            get
            {
                foreach (VectorCase vectorCase in ReadVectorCases())
                {
                    yield return new object[]
                    {
                        $"{vectorCase.Tag}:{vectorCase.Index}:encode",
                        vectorCase.Kind,
                        vectorCase.ValueJson,
                        JsonSerializer.Serialize(vectorCase.Encodings),
                    };
                }
            }
        }

        [Theory]
        [MemberData(nameof(DecodeTestCases))]
        public void Decode_accepts_every_listed_encoding(string id, string kind, string valueJson, string encodedHex)
        {
            using JsonDocument valueDocument = JsonDocument.Parse(valueJson);
            byte[] encoded = ParseHex(encodedHex);
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(encoded));

            ReadVectorValue(ref reader, kind, valueDocument.RootElement, encoded[0]);

            Assert.True(reader.End, $"{id} left trailing MessagePack bytes.");
        }

        [Theory]
        [MemberData(nameof(EncodeTestCases))]
        public void Encode_matches_one_listed_encoding(string id, string kind, string valueJson, string encodingsJson)
        {
            using JsonDocument valueDocument = JsonDocument.Parse(valueJson);
            string[] expectedEncodings = JsonSerializer.Deserialize<string[]>(encodingsJson)!;
            var output = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(output);

            WriteVectorValue(ref writer, kind, valueDocument.RootElement);
            writer.Flush();

            bool matched = expectedEncodings.Any(expected => output.WrittenSpan.SequenceEqual(ParseHex(expected)));
            Assert.True(matched, $"{id} produced {Convert.ToHexString(output.WrittenSpan)}.");
        }

        private static IEnumerable<VectorCase> ReadVectorCases()
        {
            using Stream stream = typeof(MessagePackTestSuiteTests).Assembly.GetManifestResourceStream(VectorResourceName)
                ?? throw new InvalidOperationException($"Embedded vector resource '{VectorResourceName}' was not found.");
            using JsonDocument document = JsonDocument.Parse(stream);

            foreach (JsonProperty group in document.RootElement.EnumerateObject())
            {
                string tag = group.Name.EndsWith(".yaml", StringComparison.Ordinal) ? group.Name[..^5] : group.Name;
                int caseIndex = 0;
                foreach (JsonElement sourceCase in group.Value.EnumerateArray())
                {
                    string kind;
                    string valueJson;
                    if (sourceCase.TryGetProperty("bignum", out JsonElement bignum))
                    {
                        kind = "bignum";
                        valueJson = bignum.GetRawText();
                    }
                    else
                    {
                        JsonProperty value = sourceCase.EnumerateObject().First(property => property.Name != "msgpack");
                        kind = value.Name;
                        valueJson = value.Value.GetRawText();
                    }

                    string[] encodings = sourceCase.GetProperty("msgpack").EnumerateArray().Select(item => item.GetString()!).ToArray();
                    yield return new VectorCase(tag, caseIndex++, kind, valueJson, encodings);
                }
            }
        }

        private static void ReadVectorValue(ref MessagePackReader reader, string kind, JsonElement expected, byte encodedFirstByte)
        {
            switch (kind)
            {
                case "nil":
                    reader.ReadNil();
                    break;
                case "bool":
                    Assert.Equal(expected.GetBoolean(), reader.ReadBoolean());
                    break;
                case "binary":
                    {
                        ReadOnlySequence<byte>? actual = reader.ReadBytes();
                        Assert.True(actual.HasValue);
                        Assert.Equal(ParseHex(expected.GetString()!), actual!.Value.ToArray());
                        break;
                    }

                case "number":
                    ReadNumber(ref reader, expected, encodedFirstByte);
                    break;
                case "bignum":
                    ReadNumber(ref reader, BigInteger.Parse(expected.GetString()!, CultureInfo.InvariantCulture));
                    break;
                case "string":
                    Assert.Equal(expected.GetString(), reader.ReadString());
                    break;
                case "timestamp":
                    {
                        ExtensionResult extension = reader.ReadExtensionFormat();
                        Assert.Equal((sbyte)-1, extension.TypeCode);
                        Assert.True(TryReadTimestamp(extension.Data.ToArray(), out long seconds, out uint nanoseconds));
                        Assert.Equal(expected[0].GetInt64(), seconds);
                        Assert.Equal(expected[1].GetUInt32(), nanoseconds);
                        break;
                    }

                case "ext":
                    {
                        ExtensionResult extension = reader.ReadExtensionFormat();
                        Assert.Equal((sbyte)expected[0].GetInt32(), extension.TypeCode);
                        Assert.Equal(ParseHex(expected[1].GetString()!), extension.Data.ToArray());
                        break;
                    }

                default:
                    ReadJsonValue(ref reader, expected);
                    break;
            }
        }

        private static void ReadJsonValue(ref MessagePackReader reader, JsonElement expected)
        {
            switch (expected.ValueKind)
            {
                case JsonValueKind.Null:
                    reader.ReadNil();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    Assert.Equal(expected.GetBoolean(), reader.ReadBoolean());
                    break;
                case JsonValueKind.Number:
                    ReadNumber(ref reader, expected);
                    break;
                case JsonValueKind.String:
                    Assert.Equal(expected.GetString(), reader.ReadString());
                    break;
                case JsonValueKind.Array:
                    Assert.Equal(expected.GetArrayLength(), reader.ReadArrayHeader());
                    foreach (JsonElement item in expected.EnumerateArray())
                    {
                        ReadJsonValue(ref reader, item);
                    }

                    break;
                case JsonValueKind.Object:
                    {
                        var remaining = expected.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
                        int expectedCount = remaining.Count;
                        Assert.Equal(expectedCount, reader.ReadMapHeader());
                        for (int i = 0; i < expectedCount; i++)
                        {
                            string key = reader.ReadString()!;
                            Assert.True(remaining.Remove(key, out JsonElement value), $"Unexpected map key '{key}'.");
                            ReadJsonValue(ref reader, value);
                        }

                        Assert.Empty(remaining);
                        break;
                    }

                default:
                    throw new InvalidDataException($"Unsupported vector JSON value: {expected.ValueKind}.");
            }
        }

        private static void ReadNumber(ref MessagePackReader reader, JsonElement expected)
        {
            if (TryGetInteger(expected, out BigInteger expectedInteger))
            {
                ReadInteger(ref reader, expectedInteger);
                return;
            }

            Assert.Equal(MessagePackType.Float, reader.NextMessagePackType);
            if (reader.NextCode == 0xca)
            {
                Assert.Equal(expected.GetSingle(), reader.ReadSingle());
            }
            else
            {
                Assert.Equal(expected.GetDouble(), reader.ReadDouble());
            }
        }

        private static void ReadNumber(ref MessagePackReader reader, JsonElement expected, byte encodedFirstByte)
        {
            if (encodedFirstByte is 0xca or 0xcb)
            {
                Assert.Equal(MessagePackType.Float, reader.NextMessagePackType);
                if (encodedFirstByte == 0xca)
                {
                    Assert.Equal(expected.GetSingle(), reader.ReadSingle());
                }
                else
                {
                    Assert.Equal(expected.GetDouble(), reader.ReadDouble());
                }

                return;
            }

            Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
            if (!TryGetInteger(expected, out BigInteger expectedInteger))
            {
                throw new InvalidDataException($"Integer encoding has a non-integer vector value: {expected.GetRawText()}.");
            }

            ReadInteger(ref reader, expectedInteger);
        }

        private static void ReadNumber(ref MessagePackReader reader, BigInteger expected)
        {
            if (reader.NextMessagePackType == MessagePackType.Float)
            {
                double actual = reader.NextCode == 0xca ? reader.ReadSingle() : reader.ReadDouble();
                Assert.Equal(actual, Math.Truncate(actual));
                Assert.Equal(expected, new BigInteger(actual));
                return;
            }

            Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
            byte code = reader.NextCode;
            BigInteger actualInteger = code <= 0x7f || code is >= 0xcc and <= 0xcf
                ? new BigInteger(reader.ReadUInt64())
                : new BigInteger(reader.ReadInt64());
            Assert.Equal(expected, actualInteger);
        }

        private static void ReadInteger(ref MessagePackReader reader, BigInteger expected)
        {
            Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
            byte code = reader.NextCode;
            BigInteger actualInteger = code <= 0x7f || code is >= 0xcc and <= 0xcf
                ? new BigInteger(reader.ReadUInt64())
                : new BigInteger(reader.ReadInt64());
            Assert.Equal(expected, actualInteger);
        }

        private static void WriteVectorValue(ref MessagePackWriter writer, string kind, JsonElement value)
        {
            switch (kind)
            {
                case "nil":
                    writer.WriteNil();
                    break;
                case "bool":
                    writer.Write(value.GetBoolean());
                    break;
                case "binary":
                    writer.Write(ParseHex(value.GetString()!));
                    break;
                case "number":
                    WriteJsonValue(ref writer, value);
                    break;
                case "bignum":
                    WriteInteger(ref writer, BigInteger.Parse(value.GetString()!, CultureInfo.InvariantCulture));
                    break;
                case "string":
                    writer.WriteString(Encoding.UTF8.GetBytes(value.GetString()!));
                    break;
                case "timestamp":
                    writer.WriteExtensionFormat(new ExtensionResult(-1, new ReadOnlySequence<byte>(WriteTimestamp(value[0].GetInt64(), value[1].GetUInt32()))));
                    break;
                case "ext":
                    writer.WriteExtensionFormat(new ExtensionResult(checked((sbyte)value[0].GetInt32()), new ReadOnlySequence<byte>(ParseHex(value[1].GetString()!))));
                    break;
                default:
                    WriteJsonValue(ref writer, value);
                    break;
            }
        }

        private static void WriteJsonValue(ref MessagePackWriter writer, JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    writer.WriteNil();
                    break;
                case JsonValueKind.True:
                    writer.Write(true);
                    break;
                case JsonValueKind.False:
                    writer.Write(false);
                    break;
                case JsonValueKind.Number:
                    if (value.TryGetInt64(out long signed))
                    {
                        writer.Write(signed);
                    }
                    else if (value.TryGetUInt64(out ulong unsigned))
                    {
                        writer.Write(unsigned);
                    }
                    else
                    {
                        writer.Write(value.GetDouble());
                    }

                    break;
                case JsonValueKind.String:
                    writer.WriteString(Encoding.UTF8.GetBytes(value.GetString()!));
                    break;
                case JsonValueKind.Array:
                    writer.WriteArrayHeader(value.GetArrayLength());
                    foreach (JsonElement item in value.EnumerateArray())
                    {
                        WriteJsonValue(ref writer, item);
                    }

                    break;
                case JsonValueKind.Object:
                    writer.WriteMapHeader(value.EnumerateObject().Count());
                    foreach (JsonProperty property in value.EnumerateObject())
                    {
                        writer.WriteString(Encoding.UTF8.GetBytes(property.Name));
                        WriteJsonValue(ref writer, property.Value);
                    }

                    break;
                default:
                    throw new InvalidDataException($"Unsupported vector JSON value: {value.ValueKind}.");
            }
        }

        private static void WriteInteger(ref MessagePackWriter writer, BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                writer.Write((long)value);
            }
            else if (value >= BigInteger.Zero && value <= ulong.MaxValue)
            {
                writer.Write((ulong)value);
            }
            else
            {
                throw new OverflowException($"Integer '{value}' is outside MessagePack's 64-bit range.");
            }
        }

        private static bool TryGetInteger(JsonElement value, out BigInteger integer)
        {
            if (value.TryGetInt64(out long signed))
            {
                integer = new BigInteger(signed);
                return true;
            }

            if (value.TryGetUInt64(out ulong unsigned))
            {
                integer = new BigInteger(unsigned);
                return true;
            }

            integer = default;
            return false;
        }

        private static byte[] ParseHex(string value) => Convert.FromHexString(value.Replace("-", string.Empty, StringComparison.Ordinal));

        private static bool TryReadTimestamp(byte[] data, out long seconds, out uint nanoseconds)
        {
            seconds = 0;
            nanoseconds = 0;
            if (data.Length == 4)
            {
                seconds = BinaryPrimitives.ReadUInt32BigEndian(data);
                return true;
            }

            if (data.Length == 8)
            {
                ulong packed = BinaryPrimitives.ReadUInt64BigEndian(data);
                nanoseconds = (uint)(packed >> 34);
                seconds = (long)(packed & ((1UL << 34) - 1));
                return nanoseconds < 1_000_000_000;
            }

            if (data.Length == 12)
            {
                nanoseconds = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4));
                seconds = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(4, 8));
                return nanoseconds < 1_000_000_000;
            }

            return false;
        }

        private static byte[] WriteTimestamp(long seconds, uint nanoseconds)
        {
            if (nanoseconds >= 1_000_000_000)
            {
                throw new ArgumentOutOfRangeException(nameof(nanoseconds));
            }

            if (nanoseconds == 0 && seconds >= 0 && seconds <= uint.MaxValue)
            {
                byte[] compact = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(compact, (uint)seconds);
                return compact;
            }

            if (seconds >= 0 && seconds < (1L << 34))
            {
                byte[] compact = new byte[8];
                ulong packed = ((ulong)nanoseconds << 34) | (ulong)seconds;
                BinaryPrimitives.WriteUInt64BigEndian(compact, packed);
                return compact;
            }

            byte[] extended = new byte[12];
            BinaryPrimitives.WriteUInt32BigEndian(extended.AsSpan(0, 4), nanoseconds);
            BinaryPrimitives.WriteInt64BigEndian(extended.AsSpan(4, 8), seconds);
            return extended;
        }

        private sealed record VectorCase(string Tag, int Index, string Kind, string ValueJson, string[] Encodings);
    }
}

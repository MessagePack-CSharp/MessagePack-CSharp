using System.Text;
using System.Text.Json;
using static MessagePack.JsonCodec;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

// JSON conversion utilities (debug / interop view), built on System.Text.Json.
// The mapping is lossy where JSON is poorer than msgpack: bin becomes a Base64 string,
// the timestamp ext becomes an ISO-8601 string, other exts become {"$extension": code, "$data": base64},
// non-string map keys are stringified, and NaN/Infinity become strings.

// This implementation does not chase performance to the extreme

public static partial class MessagePackSerializer
{
    // Renders one MessagePack value as JSON text (a debugging/interop view, not a wire format: bin, ext and non-string map keys have no JSON equivalent and are approximated).
    static readonly JsonWriterOptions JsonViewOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Converts one MessagePack value to JSON text. This is a diagnostic view, not a
    /// lossless conversion: binary renders as base64, timestamps as ISO 8601 strings,
    /// other extensions as <c>{"$extension", "$data"}</c> objects, and non-string map
    /// keys are stringified.
    /// </summary>
    public static string ConvertToJson(ReadOnlySpan<byte> messagePack)
    {
#if NETSTANDARD2_0
        // ns2.0 has no ArrayBufferWriter: keep the stream target, but read the exposable
        // buffer instead of copying it out with ToArray
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, JsonViewOptions))
        {
            var position = 0;
            WriteJsonValue(writer, messagePack, ref position);
        }
        stream.TryGetBuffer(out var buffer);
        return Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
#else
        // IBufferWriter target: Utf8JsonWriter writes into it directly, where a Stream
        // target goes through the writer's internal rented buffer plus a flush copy
        var output = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(output, JsonViewOptions))
        {
            var position = 0;
            WriteJsonValue(writer, messagePack, ref position);
        }
        return Encoding.UTF8.GetString(output.WrittenSpan);
#endif
    }

    /// <inheritdoc cref="ConvertToJson(ReadOnlySpan{byte})"/>
    public static string ConvertToJson(in ReadOnlySequence<byte> messagePack)
    {
        if (messagePack.IsSingleSegment)
        {
            return ConvertToJson(messagePack.FirstSpan);
        }
        return ConvertToJson(messagePack.ToArray()); // not performant but ok
    }

    /// <summary>
    /// Converts JSON text to one MessagePack value: objects to maps, arrays to arrays,
    /// numbers to the smallest integer form (or float64), strings/booleans/null to their
    /// natural forms. Invalid JSON throws <see cref="JsonException"/>.
    /// </summary>
    public static byte[] ConvertFromJson(string json)
    {
        return ConvertFromJson(json.AsSpan());
    }

    /// <inheritdoc cref="ConvertFromJson(string)"/>
    public static byte[] ConvertFromJson(ReadOnlySpan<char> json)
    {
        return ConvertFromParsedJson(JsonElement.Parse(json));
    }

    /// <inheritdoc cref="ConvertFromJson(string)"/>
    public static byte[] ConvertFromJson(ReadOnlySpan<byte> utf8Json)
    {
        return ConvertFromParsedJson(JsonElement.Parse(utf8Json));
    }

    static byte[] ConvertFromParsedJson(JsonElement root)
    {
        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            WriteMessagePackValue(ref buffer, root);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }
}

file static class JsonCodec
{
    internal static void WriteJsonValue(Utf8JsonWriter writer, ReadOnlySpan<byte> source, ref int position)
    {
        if (position >= source.Length)
        {
            throw new MessagePackSerializationException("Truncated MessagePack data converting to JSON.");
        }

        var span = source.Slice(position);
        var code = span[0];

        if (code <= MessagePackCode.MaxFixInt)
        {
            writer.WriteNumberValue(code);
            position++;
            return;
        }
        if (code >= MessagePackCode.MinNegativeFixInt)
        {
            writer.WriteNumberValue(unchecked((sbyte)code));
            position++;
            return;
        }
        if (code < MessagePackCode.MinFixArray) // fixmap
        {
            WriteJsonMap(writer, source, ref position, code & 0b0000_1111, headerSize: 1);
            return;
        }
        if (code < MessagePackCode.MinFixStr) // fixarray
        {
            WriteJsonArray(writer, source, ref position, code & 0b0000_1111, headerSize: 1);
            return;
        }
        if (code < MessagePackCode.Nil) // fixstr
        {
            writer.WriteStringValue(ReadStringPayload(source, ref position));
            return;
        }

        switch (code)
        {
            case MessagePackCode.Nil:
                writer.WriteNullValue();
                position++;
                return;
            case MessagePackCode.False:
            case MessagePackCode.True:
                writer.WriteBooleanValue(code == MessagePackCode.True);
                position++;
                return;
            case MessagePackCode.UInt8:
            case MessagePackCode.UInt16:
            case MessagePackCode.UInt32:
            case MessagePackCode.Int8:
            case MessagePackCode.Int16:
            case MessagePackCode.Int32:
            case MessagePackCode.Int64:
                writer.WriteNumberValue(ReadInt64Token(span, ref position));
                return;
            case MessagePackCode.UInt64:
                Ensure(TryReadUInt64(span, out ulong unsigned, out var uintSize), "uint64");
                writer.WriteNumberValue(unsigned);
                position += uintSize;
                return;
            case MessagePackCode.Float32:
                Ensure(TryReadSingle(span, out float single, out var singleSize), "float32");
                WriteJsonFloat(writer, single);
                position += singleSize;
                return;
            case MessagePackCode.Float64:
                Ensure(TryReadDouble(span, out double floating, out var doubleSize), "float64");
                WriteJsonFloat(writer, floating);
                position += doubleSize;
                return;
            case MessagePackCode.Str8:
            case MessagePackCode.Str16:
            case MessagePackCode.Str32:
                writer.WriteStringValue(ReadStringPayload(source, ref position));
                return;
            case MessagePackCode.Bin8:
            case MessagePackCode.Bin16:
            case MessagePackCode.Bin32:
                {
                    Ensure(TryReadBinHeader(span, out var byteCount, out var headerSize), "bin");
                    EnsurePayload(source, position + headerSize, byteCount);
                    writer.WriteBase64StringValue(source.Slice(position + headerSize, byteCount));
                    position += headerSize + byteCount;
                    return;
                }
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
            case MessagePackCode.Ext8:
            case MessagePackCode.Ext16:
            case MessagePackCode.Ext32:
                {
                    if (TryReadTimestamp(span, out var timestamp, out var timestampSize) == DecodeResult.Success)
                    {
                        writer.WriteStringValue(timestamp); // ISO-8601
                        position += timestampSize;
                        return;
                    }
                    Ensure(TryReadExtHeader(span, out var extCode, out var dataLength, out var headerSize), "ext");
                    EnsurePayload(source, position + headerSize, dataLength);
                    writer.WriteStartObject();
                    writer.WriteNumber("$extension"u8, extCode);
                    writer.WriteBase64String("$data"u8, source.Slice(position + headerSize, dataLength));
                    writer.WriteEndObject();
                    position += headerSize + dataLength;
                    return;
                }
            case MessagePackCode.Array16:
            case MessagePackCode.Array32:
                {
                    Ensure(MessagePackPrimitives.TryReadArrayHeader(span, out var count, out var headerSize), "array header");
                    WriteJsonArray(writer, source, ref position, count, headerSize);
                    return;
                }
            case MessagePackCode.Map16:
            case MessagePackCode.Map32:
                {
                    Ensure(TryReadMapHeader(span, out var count, out var headerSize), "map header");
                    WriteJsonMap(writer, source, ref position, count, headerSize);
                    return;
                }
            default: // 0xc1 (never used)
                throw new MessagePackSerializationException($"Unrecognized MessagePack code 0x{code:x2} converting to JSON.");
        }
    }

    internal static void WriteJsonArray(Utf8JsonWriter writer, ReadOnlySpan<byte> source, ref int position, int count, int headerSize)
    {
        position += headerSize;
        writer.WriteStartArray();
        for (var i = 0; i < count; i++)
        {
            WriteJsonValue(writer, source, ref position);
        }
        writer.WriteEndArray();
    }

    internal static void WriteJsonMap(Utf8JsonWriter writer, ReadOnlySpan<byte> source, ref int position, int count, int headerSize)
    {
        position += headerSize;
        writer.WriteStartObject();
        for (var i = 0; i < count; i++)
        {
            WriteJsonPropertyName(writer, source, ref position);
            WriteJsonValue(writer, source, ref position);
        }
        writer.WriteEndObject();
    }

    // JSON property names must be strings: str keys pass through as UTF-8, scalar keys
    // are stringified invariantly, container/bin/ext keys have no sane JSON spelling
    internal static void WriteJsonPropertyName(Utf8JsonWriter writer, ReadOnlySpan<byte> source, ref int position)
    {
        if (position >= source.Length)
        {
            throw new MessagePackSerializationException("Truncated MessagePack data converting to JSON.");
        }

        var span = source.Slice(position);
        var code = span[0];

        if ((code >= MessagePackCode.MinFixStr && code < MessagePackCode.Nil) ||
            code is MessagePackCode.Str8 or MessagePackCode.Str16 or MessagePackCode.Str32)
        {
            writer.WritePropertyName(ReadStringPayload(source, ref position));
            return;
        }
        if (code <= MessagePackCode.MaxFixInt || code >= MessagePackCode.MinNegativeFixInt ||
            code is MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32
                or MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64)
        {
            writer.WritePropertyName(ReadInt64Token(span, ref position).ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (code == MessagePackCode.UInt64)
        {
            Ensure(TryReadUInt64(span, out ulong unsigned, out var tokenSize), "uint64");
            writer.WritePropertyName(unsigned.ToString(System.Globalization.CultureInfo.InvariantCulture));
            position += tokenSize;
            return;
        }
        if (code is MessagePackCode.True or MessagePackCode.False)
        {
            writer.WritePropertyName(code == MessagePackCode.True ? "true" : "false");
            position++;
            return;
        }
        if (code is MessagePackCode.Float32 or MessagePackCode.Float64)
        {
            Ensure(TryReadDouble(span, out double floating, out var tokenSize), "float");
            writer.WritePropertyName(floating.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            position += tokenSize;
            return;
        }
        throw new MessagePackSerializationException($"Map key with MessagePack code 0x{code:x2} cannot be represented as a JSON property name.");
    }

    internal static long ReadInt64Token(ReadOnlySpan<byte> span, ref int position)
    {
        Ensure(TryReadInt64(span, out long value, out var tokenSize), "integer");
        position += tokenSize;
        return value;
    }

    internal static ReadOnlySpan<byte> ReadStringPayload(ReadOnlySpan<byte> source, ref int position)
    {
        Ensure(TryReadStringHeader(source.Slice(position), out var byteCount, out var headerSize), "str header");
        EnsurePayload(source, position + headerSize, byteCount);
        var payload = source.Slice(position + headerSize, byteCount);
        position += headerSize + byteCount;
        return payload;
    }

    internal static void WriteJsonFloat(Utf8JsonWriter writer, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            // JSON numbers cannot spell these; strings are the conventional escape hatch
            writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }

    internal static void Ensure(bool success, string what)
    {
        if (!success)
        {
            throw new MessagePackSerializationException($"Malformed or truncated MessagePack {what} converting to JSON.");
        }
    }

    internal static void Ensure(DecodeResult result, string what) => Ensure(result == DecodeResult.Success, what);

    internal static void EnsurePayload(ReadOnlySpan<byte> source, int start, int length)
    {
        if ((uint)length > (uint)(source.Length - start))
        {
            throw new MessagePackSerializationException("Truncated MessagePack payload converting to JSON.");
        }
    }

    internal static void WriteMessagePackValue(ref CompatibleArrayPoolListWriteBuffer buffer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var count = 0;
                    foreach (var _ in element.EnumerateObject())
                    {
                        count++;
                    }
                    buffer.WriteMapHeader(count);
                    foreach (var property in element.EnumerateObject())
                    {
                        buffer.WriteString(property.Name);
                        WriteMessagePackValue(ref buffer, property.Value);
                    }
                    return;
                }
            case JsonValueKind.Array:
                buffer.WriteArrayHeader(element.GetArrayLength());
                foreach (var item in element.EnumerateArray())
                {
                    WriteMessagePackValue(ref buffer, item);
                }
                return;
            case JsonValueKind.String:
                buffer.WriteString(element.GetString());
                return;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var signed))
                {
                    buffer.WriteInt64(signed);
                }
                else if (element.TryGetUInt64(out var unsigned))
                {
                    buffer.WriteUInt64(unsigned);
                }
                else
                {
                    buffer.WriteDouble(element.GetDouble());
                }
                return;
            case JsonValueKind.True:
            case JsonValueKind.False:
                buffer.WriteBoolean(element.ValueKind == JsonValueKind.True);
                return;
            default: // Null (Undefined cannot appear in a parsed document)
                buffer.WriteNil();
                return;
        }
    }
}
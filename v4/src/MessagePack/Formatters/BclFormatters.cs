using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Buffers.Binary;
using System.Buffers.Text;

namespace MessagePack.Formatters;

// BCL scalars composed from plain str/int/array wire formats, created by BuiltInFormatterFactory

// MsgPack104: the str/timestamp tokens here are wire representations of non-string types (decimal, Uri, BigInteger, TimeSpan, ...), not string/DateTime values of the object model.
// Routing them through the resolver's formatter would tax immediately-parsed temporaries with interning and block the utf8 fast paths.
#pragma warning disable MsgPack104

/// <summary>Serializes <see cref="decimal"/> as a culture-invariant str.</summary>
public sealed partial class DecimalFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, decimal>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, decimal value)
    {
        // invariant decimal is at most 31 utf8 bytes (sign + 29 digits + point)
        // always fixstr, so the 1-byte header is known upfront and the digits are formatted straight into the buffer window.
        var span = buffer.GetSpan(1 + MessagePackCode.MaxFixStringLength);
        if (Utf8Formatter.TryFormat(value, span.Slice(1, MessagePackCode.MaxFixStringLength), out var written))
        {
            span[0] = (byte)(MessagePackCode.MinFixStr | written);
            buffer.Advance(1 + written);
        }
        else
        {
            // unreachable by the length argument above
            buffer.WriteString(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref decimal value)
    {
        value = DecimalCodec.ReadString(ref buffer);
    }
}

// str-form decimal reader shared by the default formatter and the DotNetOptimized
// reader's default-form fallback (migration: readers upgrade before writers flip)
internal static class DecimalCodec
{
    internal static decimal ReadString<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var byteCount = buffer.ReadStringHeader();
        if (!buffer.TryGetSpan(byteCount, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading decimal.");
        }
        if (!Utf8Parser.TryParse(payload.Slice(0, byteCount), out decimal result, out var consumed) || consumed != byteCount)
        {
            throw new MessagePackSerializationException("Can't parse to decimal, input string was not in a correct format.");
        }
        buffer.Advance(byteCount);
        return result;
    }
}

/// <summary>Serializes <see cref="TimeSpan"/> as its tick count.</summary>
public sealed partial class TimeSpanFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TimeSpan>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TimeSpan value)
    {
        buffer.WriteInt64(value.Ticks);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TimeSpan value)
    {
        value = new TimeSpan(buffer.ReadInt64());
    }
}

/// <summary>Serializes <see cref="DateTimeOffset"/> as [timestamp, offset-minutes].</summary>
public sealed partial class DateTimeOffsetFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTimeOffset>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateTimeOffset value)
    {
        buffer.WriteArrayHeader(2);

        // We're writing a *local* DateTime value in msgpack encoding as if it were UTC time.
        // That's incorrect msgpack encoding, but fixing it now would compromise backward compatibility.
        buffer.WriteTimestamp(new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
        buffer.WriteInt16((short)value.Offset.TotalMinutes); // offset is normalized in minutes
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTimeOffset value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
        }
        var ticks = buffer.ReadTimestamp().Ticks;
        var offsetMinutes = buffer.ReadInt16();
        // the ctor rejects offsets beyond +-14h and clock ticks whose UTC equivalent
        // leaves the DateTime range; both are reachable from forged payloads (int16 can
        // claim +-546h), so validate here to surface them as data errors, not ArgumentException
        if ((uint)(offsetMinutes + 840) > 1680 ||
            (ulong)(ticks - offsetMinutes * TimeSpan.TicksPerMinute) > (ulong)DateTime.MaxValue.Ticks)
        {
            throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
        }
        value = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }
}

/// <summary>Serializes <see cref="Guid"/> as its 36-character "D" str.</summary>
public sealed partial class GuidFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Guid>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Guid value)
    {
        // "D" is always exactly 36 bytes: the str8 header is known upfront, format directly.
        var span = buffer.GetSpan(2 + 36);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (36 << 8) | MessagePackCode.Str8);
        Utf8Formatter.TryFormat(value, span.Slice(2, 36), out _);
        buffer.Advance(2 + 36);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Guid value)
    {
        value = GuidCodec.ReadString(ref buffer);
    }
}

// str-form Guid reader shared by the default formatter and the DotNetOptimized
// reader's default-form fallback (migration: readers upgrade before writers flip)
internal static class GuidCodec
{
    internal static Guid ReadString<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var byteCount = buffer.ReadStringHeader();
        if (byteCount != 36)
        {
            throw new MessagePackSerializationException("Unexpected length of string.");
        }
        if (!buffer.TryGetSpan(36, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Guid.");
        }
        if (!Utf8Parser.TryParse(payload.Slice(0, 36), out Guid result, out _))
        {
            throw new MessagePackSerializationException("Can't parse to Guid, input string was not in a correct format.");
        }
        buffer.Advance(36);
        return result;
    }
}

/// <summary>Serializes <see cref="Uri"/> as its <see cref="Uri.OriginalString"/>.</summary>
public sealed partial class UriFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Uri?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Uri? value)
    {
        buffer.WriteString(value?.OriginalString);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Uri? value)
    {
        var text = buffer.ReadString();
        if (text == null)
        {
            value = null;
            return;
        }

        if (!Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw new MessagePackSerializationException("Can't parse to Uri, input string was not in a correct format.");
        }
        value = uri;
    }
}

/// <summary>Serializes <see cref="Version"/> as its string form.</summary>
public sealed partial class VersionFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Version?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Version? value)
    {
#if NET
        // Version is IUtf8SpanFormattable on net: format straight into the buffer window.
        // 4 int components + 3 dots = at most 43 bytes, so TryFormat cannot fail. The
        // header is 1 byte (fixstr) or 2 (str8) depending on the formatted length, so
        // format at the fixstr offset speculatively — only a >31-byte version (10+-digit
        // components) pays a 1-byte payload shift (same shape as UnsafeWriteString)
        if (value != null)
        {
            var span = buffer.GetSpan(2 + 43);
            value.TryFormat(span.Slice(1, 43), out var written);
            if (written <= MessagePackCode.MaxFixStringLength)
            {
                span[0] = (byte)(MessagePackCode.MinFixStr | written);
                buffer.Advance(1 + written);
            }
            else
            {
                span.Slice(1, written).CopyTo(span.Slice(2)); // CopyTo has memmove semantics
                BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)((written << 8) | MessagePackCode.Str8));
                buffer.Advance(2 + written);
            }
            return;
        }
        buffer.WriteNil();
#else
        buffer.WriteString(value?.ToString());
#endif
    }

#if NET
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Version? value)
    {
        // Version is IUtf8SpanParsable on net: parse the payload without the intermediate string
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var byteCount = buffer.ReadStringHeader();
        if (!buffer.TryGetSpan(byteCount, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Version.");
        }
        if (!Version.TryParse(payload.Slice(0, byteCount), out var version))
        {
            throw new MessagePackSerializationException("Can't parse to Version, input string was not in a correct format.");
        }
        value = version;
        buffer.Advance(byteCount);
    }
#else
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Version? value)
    {
        var text = buffer.ReadString();
        if (text == null)
        {
            value = null;
            return;
        }

        if (!Version.TryParse(text, out var version))
        {
            throw new MessagePackSerializationException("Can't parse to Version, input string was not in a correct format.");
        }
        value = version;
    }
#endif
}

/// <summary>Serializes <see cref="StringBuilder"/> as a str.</summary>
public sealed partial class StringBuilderFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, StringBuilder?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, StringBuilder? value)
    {
        // GetChunks-based zero-string encode needs chunk-seam surrogate stitching, it is too much complexity for a type this rare.
        buffer.WriteString(value?.ToString());
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref StringBuilder? value)
    {
        var text = buffer.ReadString();
        value = text == null ? null : new StringBuilder(text);
    }
}

/// <summary>Serializes <see cref="BitArray"/> as an array of booleans.</summary>
public sealed partial class BitArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BitArray?>
{
    // This format is a failure of versions v1 through v3.
    // If you are using BitArray, please use NativeBitArrayFormatter (or DotNetOptimized options).
    // However, for the sake of compatibility, this will remain the default.

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BitArray? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var len = value.Length;
        buffer.WriteArrayHeader(len);
        for (int i = 0; i < len; i++)
        {
            buffer.WriteBoolean(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BitArray? value)
    {
        // dual-format: also accepts the DotNetOptimized packed form, so a default-
        // configured peer can read packed data (the forms are type-tag distinguishable)
        value = BitArrayCodec.Read(ref buffer);
    }
}

/// <summary>
/// Serializes <see cref="System.Globalization.CultureInfo"/> as its <see cref="System.Globalization.CultureInfo.Name"/>
/// ("" = invariant). Instance customizations beyond the name do not roundtrip.
/// </summary>
public sealed partial class CultureInfoFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, CultureInfo?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, CultureInfo? value)
    {
        buffer.WriteString(value?.Name);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref CultureInfo? value)
    {
        var name = buffer.ReadString();
        if (name == null)
        {
            value = null;
            return;
        }
        // GetCultureInfo over the ctor: cached read-only instances, no per-call allocation
        try
        {
            value = CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException ex)
        {
            throw new MessagePackSerializationException($"Can't load culture '{name}'.", ex);
        }
    }
}

/// <summary>
/// Serializes <see cref="TimeZoneInfo"/> as its <see cref="TimeZoneInfo.Id"/>.
/// The id must resolve via <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>
/// on the READING machine: custom time zones do not roundtrip, and Windows/IANA id
/// availability differs per platform (net8+ converts between the two families).
/// </summary>
public sealed partial class TimeZoneInfoFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TimeZoneInfo?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TimeZoneInfo? value)
    {
        buffer.WriteString(value?.Id);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TimeZoneInfo? value)
    {
        var id = buffer.ReadString();
        if (id == null)
        {
            value = null;
            return;
        }
        try
        {
            value = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new MessagePackSerializationException($"Can't load time zone '{id}'.", ex);
        }
    }
}

#if NET

/// <summary>
/// Serializes <see cref="Index"/> as an int32: the value itself, or the from-end value
/// one's-complemented (the struct's own internal encoding), so ^2 travels as -3.
/// </summary>
public sealed partial class IndexFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Index>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Index value)
    {
        buffer.WriteInt32(value.IsFromEnd ? ~value.Value : value.Value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Index value)
    {
        var raw = buffer.ReadInt32();
        value = raw < 0 ? new Index(~raw, fromEnd: true) : new Index(raw);
    }
}

/// <summary>Serializes <see cref="Range"/> as [start, end] of <see cref="Index"/>-encoded int32s.</summary>
public sealed partial class RangeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Range>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Range value)
    {
        buffer.WriteFixArrayHeader(2);
        buffer.WriteInt32(value.Start.IsFromEnd ? ~value.Start.Value : value.Start.Value);
        buffer.WriteInt32(value.End.IsFromEnd ? ~value.End.Value : value.End.Value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Range value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Range format.");
        }
        var start = buffer.ReadInt32();
        var end = buffer.ReadInt32();
        value = new Range(
            start < 0 ? new Index(~start, fromEnd: true) : new Index(start),
            end < 0 ? new Index(~end, fromEnd: true) : new Index(end));
    }
}

/// <summary>Serializes <see cref="Half"/> as a float32.</summary>
public sealed partial class HalfFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Half>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Half value)
    {
        // msgpack spec does not have float16, so we serialize as float32 (4 bytes) instead of 2 bytes.
        buffer.WriteSingle((float)value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Half value)
    {
        value = (Half)buffer.ReadSingle();
    }
}

/// <summary>Serializes <see cref="Rune"/> as its Unicode scalar value.</summary>
public sealed partial class RuneFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Rune>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Rune value)
    {
        buffer.WriteInt32(value.Value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Rune value)
    {
        if (!Rune.TryCreate(buffer.ReadInt32(), out value))
        {
            throw new MessagePackSerializationException("Invalid Rune scalar value.");
        }
    }
}

/// <summary>Serializes <see cref="DateOnly"/> as its <see cref="DateOnly.DayNumber"/>.</summary>
public sealed partial class DateOnlyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateOnly>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateOnly value)
    {
        buffer.WriteInt32(value.DayNumber);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateOnly value)
    {
        var dayNumber = buffer.ReadInt32();
        if ((uint)dayNumber > (uint)DateOnly.MaxValue.DayNumber)
        {
            throw new MessagePackSerializationException("Invalid DateOnly day number.");
        }
        value = DateOnly.FromDayNumber(dayNumber);
    }
}

/// <summary>Serializes <see cref="TimeOnly"/> as its tick count.</summary>
public sealed partial class TimeOnlyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TimeOnly>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TimeOnly value)
    {
        buffer.WriteInt64(value.Ticks);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TimeOnly value)
    {
        var ticks = buffer.ReadInt64();
        if ((ulong)ticks >= (ulong)TimeSpan.TicksPerDay)
        {
            throw new MessagePackSerializationException("Invalid TimeOnly tick count.");
        }
        value = new TimeOnly(ticks);
    }
}

/// <summary>Serializes <see cref="Int128"/> as a 16-byte little-endian bin.</summary>
public sealed partial class Int128Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int128>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Int128 value)
    {
        // always exactly 16 bytes: the bin8 header is known upfront (fused constant store)
        var span = buffer.GetSpan(2 + 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (16 << 8) | MessagePackCode.Bin8);
        BinaryPrimitives.WriteInt128LittleEndian(span.Slice(2), value);
        buffer.Advance(2 + 16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Int128 value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 16)
        {
            throw new MessagePackSerializationException("Invalid Int128 data.");
        }
        if (!buffer.TryGetSpan(16, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Int128.");
        }
        value = BinaryPrimitives.ReadInt128LittleEndian(payload.Slice(0, 16));
        buffer.Advance(16);
    }
}

/// <summary>Serializes <see cref="UInt128"/> as a 16-byte little-endian bin.</summary>
public sealed partial class UInt128Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, UInt128>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, UInt128 value)
    {
        var span = buffer.GetSpan(2 + 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (16 << 8) | MessagePackCode.Bin8);
        BinaryPrimitives.WriteUInt128LittleEndian(span.Slice(2), value);
        buffer.Advance(2 + 16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref UInt128 value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 16)
        {
            throw new MessagePackSerializationException("Invalid UInt128 data.");
        }
        if (!buffer.TryGetSpan(16, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading UInt128.");
        }
        value = BinaryPrimitives.ReadUInt128LittleEndian(payload.Slice(0, 16));
        buffer.Advance(16);
    }
}

#endif
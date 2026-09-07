using System.Buffers.Binary;
using System.Collections;

namespace MessagePack.Formatters;

// The DotNetOptimized tier: alternate wire formats for .NET-to-.NET traffic, served by
// DotNetOptimizedFormatterFactory and composed via MessagePackFormatterFactory.DotNetOptimized.
// WRITING requires both ends to opt in, but every READER here also accepts the default
// wire form (str Guid/decimal, timestamp DateTime/DateTimeOffset).

// MsgPack104: these are the DateTime customization itself; ReadTimestamp is the documented default-wire-form fallback, not a bypass.
#pragma warning disable MsgPack104

/// <summary>
/// Serializes <see cref="Guid"/> as its 16-byte little-endian image.
/// </summary>
public sealed partial class DotNetOptimizedGuidFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Guid>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Guid value)
    {
        // RFC 4122/RFC 9562 specifies big-endian.
        // However, this formatter must keep v3 compatibility, and since .NET-to-.NET interop is the main use case, little-endian causes no real problems.
        // If your goal is interoperability with other languages and you also want to avoid strings, please create a separate formatter for that.
        // Either way, since it is stored as ext in little-endian, it is not interoperable anyway, so it is not much different from the current spec.

        var span = buffer.GetSpan(2 + 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (16 << 8) | MessagePackCode.Bin8);
#if NETSTANDARD2_0
        value.ToByteArray().CopyTo(span.Slice(2)); // ns2.0 has no TryWriteBytes
#else
        value.TryWriteBytes(span.Slice(2, 16));
#endif
        buffer.Advance(2 + 16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Guid value)
    {
        // default str form (migration fallback, see the file header)
        if (buffer.TryPeek(out var code) &&
            ((code & 0xE0) == MessagePackCode.MinFixStr ||
             code is MessagePackCode.Str8 or MessagePackCode.Str16 or MessagePackCode.Str32))
        {
            value = GuidCodec.ReadString(ref buffer);
            return;
        }

        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 16)
        {
            throw new MessagePackSerializationException("Invalid Guid size.");
        }
        if (!buffer.TryGetSpan(16, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Guid.");
        }
#if NETSTANDARD2_0
        value = new Guid(payload.Slice(0, 16).ToArray());
#else
        value = new Guid(payload.Slice(0, 16));
#endif
        buffer.Advance(16);
    }
}

/// <summary>
/// Serializes <see cref="decimal"/> as its 16-byte little-endian image (flags, hi, lo, mid).
/// </summary>
public sealed partial class DotNetOptimizedDecimalFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, decimal>
{
    // flags layout: bits 16-23 scale (0..28), bit 31 sign; everything else must be zero
    const int InvalidFlagsMask = 0x7F00FFFF;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, decimal value)
    {
        var span = buffer.GetSpan(2 + 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (16 << 8) | MessagePackCode.Bin8);
#if NET9_0_OR_GREATER
        Span<int> bits = stackalloc int[4]; // [lo, mid, hi, flags]
        decimal.GetBits(value, bits);
#else
        var bits = decimal.GetBits(value);
#endif
        // written in the struct's memory-image order so the wire matches the v3 blit
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(2), bits[3]);  // flags
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(6), bits[2]);  // hi
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(10), bits[0]); // lo
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(14), bits[1]); // mid
        buffer.Advance(2 + 16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref decimal value)
    {
        // default str form (migration fallback, see the file header)
        if (buffer.TryPeek(out var code) &&
            ((code & 0xE0) == MessagePackCode.MinFixStr ||
             code is MessagePackCode.Str8 or MessagePackCode.Str16 or MessagePackCode.Str32))
        {
            value = DecimalCodec.ReadString(ref buffer);
            return;
        }

        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 16)
        {
            throw new MessagePackSerializationException("Invalid decimal size.");
        }
        if (!buffer.TryGetSpan(16, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading decimal.");
        }
        var flags = BinaryPrimitives.ReadInt32LittleEndian(payload);
        var hi = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4));
        var lo = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8));
        var mid = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12));
        var scale = (flags >> 16) & 0xFF;
        if ((flags & InvalidFlagsMask) != 0 || scale > 28)
        {
            throw new MessagePackSerializationException("Invalid decimal flag bits.");
        }
        value = new decimal(lo, mid, hi, flags < 0, (byte)scale);
        buffer.Advance(16);
    }
}

/// <summary>
/// Serializes <see cref="DateTime"/> as its <see cref="DateTime.ToBinary"/> value.
/// Unlike the default msgpack timestamp ext, this preserves <see cref="DateTimeKind"/>
/// exactly (Local stays Local, Unspecified stays Unspecified) — .NET-to-.NET only.
/// </summary>
public sealed partial class DotNetOptimizedDateTimeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateTime value)
    {
        // constant shape: forced int64, always 0xd3 + 9 bytes.
        MessagePackPrimitives.UnsafeWriteForcedInt64(ref buffer.GetReference(9), value.ToBinary());
        buffer.Advance(9);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTime value)
    {
        // our writer always emits 0xd3 9 bytes; the 0xcf arm fast-paths smallest-format
        // producers (v3 NativeDateTimeFormatter data). A uint64 payload with bit 63 set
        // doesn't fit long — fall through and let ReadInt64 raise the data error.
        long binary;
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 9 &&
            (span[0] == MessagePackCode.Int64 ||
             (span[0] == MessagePackCode.UInt64 && (sbyte)span[1] >= 0)))
        {
            binary = BinaryPrimitives.ReadInt64BigEndian(span.Slice(1));
            buffer.Advance(9);
        }
        else if (buffer.TryPeek(out var code) &&
                 code is MessagePackCode.FixExt4 or MessagePackCode.FixExt8 or MessagePackCode.Ext8)
        {
            // default timestamp form (migration fallback) — yields Utc, the same
            // contract as the default reader
            value = buffer.ReadTimestamp();
            return;
        }
        else
        {
            // foreign smallest-format encodings and seam-straddling windows
            binary = buffer.ReadInt64();
        }

        // Utc/Unspecified (bit 63 clear) decode as pure bit work.
        if (binary >= 0)
        {
            var ticks = (ulong)binary & 0x3FFF_FFFF_FFFF_FFFF;
            if (ticks > (ulong)DateTime.MaxValue.Ticks)
            {
                throw new MessagePackSerializationException("Invalid DateTime binary value.");
            }
            value = new DateTime((long)ticks, (DateTimeKind)((ulong)binary >> 62)); // 0 or 1
            return;
        }

        value = FromBinaryLocal(binary);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DateTime FromBinaryLocal(long binary)
    {
        // no Try variant exists for FromBinary: forged tick bits throw ArgumentException,
        // wrapped as the data error it is (cold path keeps the EH out of Deserialize)
        try
        {
            return DateTime.FromBinary(binary);
        }
        catch (ArgumentException ex)
        {
            throw new MessagePackSerializationException("Invalid DateTime binary value.", ex);
        }
    }
}

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> as a constant-shape [ticks, offsetMinutes]
/// with forced-width int64/int16 — 13 bytes flat, no timestamp ext and none of the
/// default format's local-ticks-tagged-as-UTC quirk.
/// </summary>
public sealed partial class DotNetOptimizedDateTimeOffsetFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTimeOffset>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateTimeOffset value)
    {
        // single-reservation fusion: fixarray(2) + forced int64(9) + forced int16(3) = 13 bytes
        ref var r = ref buffer.GetReference(13);
        r = (byte)(MessagePackCode.MinFixArray | 2);
        MessagePackPrimitives.UnsafeWriteForcedInt64(ref Unsafe.Add(ref r, 1), value.Ticks);
        MessagePackPrimitives.UnsafeWriteForcedInt16(ref Unsafe.Add(ref r, 10), (short)value.Offset.TotalMinutes);
        buffer.Advance(13);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTimeOffset value)
    {
        long ticks;
        int offsetMinutes;
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 13 &&
            span[0] == (MessagePackCode.MinFixArray | 2) &&
            span[1] == MessagePackCode.Int64 &&
            span[10] == MessagePackCode.Int16)
        {
            // fused fast path over the canonical constant shape
            ticks = BinaryPrimitives.ReadInt64BigEndian(span.Slice(2));
            offsetMinutes = BinaryPrimitives.ReadInt16BigEndian(span.Slice(11));
            buffer.Advance(13);
        }
        else
        {
            // foreign smallest-format encodings and seam-straddling windows
            if (buffer.ReadArrayHeader() != 2)
            {
                throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
            }
            if (buffer.TryPeek(out var code) &&
                code is MessagePackCode.FixExt4 or MessagePackCode.FixExt8 or MessagePackCode.Ext8)
            {
                // default [timestamp, int16] form (migration fallback): its timestamp
                // carries clock ticks tagged as UTC — the same slot our int64 carries
                ticks = buffer.ReadTimestamp().Ticks;
            }
            else
            {
                ticks = buffer.ReadInt64();
            }
            offsetMinutes = buffer.ReadInt16();
        }

        // same data-error gates as the default formatter, plus the raw ticks range (they
        // arrive as a bare int64 here, not through the timestamp reader's guarantees)
        if ((uint)(offsetMinutes + 840) > 1680 ||
            (ulong)(ticks - offsetMinutes * TimeSpan.TicksPerMinute) > (ulong)DateTime.MaxValue.Ticks ||
            (ulong)ticks > (ulong)DateTime.MaxValue.Ticks)
        {
            throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
        }
        value = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }
}

/// <summary>
/// Serializes <see cref="BitArray"/> bit-packed as [bitLength, bin] — 1/8th of the
/// default bool-array form. The two forms are type-tag distinguishable, and BOTH
/// BitArray formatters read BOTH, so default-configured peers can read this data.
/// </summary>
public sealed partial class PackedBitArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BitArray?>
{
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

        var bitLength = value.Length;
        var byteCount = (bitLength + 7) >> 3;
        buffer.WriteFixArrayHeader(2);
        buffer.WriteInt32(bitLength);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            value.CopyTo(rented, 0); // LSB-first packed bytes, the BitArray(byte[]) ctor's own layout
            buffer.WriteBinary(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BitArray? value)
    {
        value = BitArrayCodec.Read(ref buffer);
    }
}

// dual-format BitArray reader shared by the default and packed formatters: nil, the
// legacy bool-array form, and the packed [bitLength, bin] form — distinguished by the
// FIRST element's type tag (bool => legacy, integer => packed), which is unambiguous
// because the legacy form's elements are always booleans
static class BitArrayCodec
{
    internal static BitArray? Read<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (buffer.TryReadNil())
        {
            return null;
        }

        // ReadArrayHeader validates the claimed count against BytesRemaining
        var count = buffer.ReadArrayHeader();
        if (count != 0 && buffer.TryPeek(out var code) &&
            code != MessagePackCode.False && code != MessagePackCode.True)
        {
            // packed form: [bitLength, bin]
            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid BitArray format.");
            }
            var bitLength = buffer.ReadInt32();
            var byteCount = buffer.ReadBinHeader();
            if (bitLength < 0 || byteCount != (int)(((long)bitLength + 7) >> 3))
            {
                throw new MessagePackSerializationException("Invalid BitArray format.");
            }
            if (!buffer.TryGetSpan(byteCount, out var payload))
            {
                throw new MessagePackSerializationException("Truncated MessagePack data reading BitArray.");
            }
            var bytes = payload.Slice(0, byteCount).ToArray();
            buffer.Advance(byteCount);
            var packed = new BitArray(bytes);
            packed.Length = bitLength; // trim the padding bits of the last byte
            return packed;
        }

        // legacy bool-array form (1 byte/bool, already remaining-guarded by the header read)
        var result = new BitArray(count);
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadBoolean();
        }
        return result;
    }
}

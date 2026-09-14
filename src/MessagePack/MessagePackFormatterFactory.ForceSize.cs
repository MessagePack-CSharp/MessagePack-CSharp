using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Fixed-width integer formats: every integer type is always written in its own msgpack format
/// (int8/uint8 through int64/uint64) regardless of value, instead of the smallest format that fits.
/// Reads stay lenient and accept any integer format.
/// Covers the eight integer primitives, their nullable forms, and their arrays (byte[] stays bin).
/// Not in any default chain: put it on a member with <see cref="MessagePackFormatterAttribute"/>,
/// or compose <see cref="Instance"/> ahead of a default factory to force the width everywhere.
/// </summary>
public sealed partial class ForceSizeFormatterFactory : MessagePackFormatterFactory
{
    /// <summary>Shared instance.</summary>
    public static readonly ForceSizeFormatterFactory Instance = new ForceSizeFormatterFactory();

    /// <summary>Creates a new instance, for use with <see cref="MessagePackFormatterAttribute"/> on a single member. Chains use <see cref="Instance"/>.</summary>
    public ForceSizeFormatterFactory()
    {
    }

    // One method, two signatures. net9+ overrides the base virtual (constraints inherited), while downlevel has no base
    // member, so the constraints are spelled out.
    /// <summary>Creates a formatter for <paramref name="type"/>, or returns null when it is not a forced-width integer type.</summary>
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(sbyte)) return new ForceSByteBlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(byte)) return new ForceByteBlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(short)) return new ForceInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ushort)) return new ForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(int)) return new ForceInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(uint)) return new ForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(long)) return new ForceInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ulong)) return new ForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer>();

        if (type == typeof(sbyte?)) return new NullableForceSByteBlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(byte?)) return new NullableForceByteBlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(short?)) return new NullableForceInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ushort?)) return new NullableForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(int?)) return new NullableForceInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(uint?)) return new NullableForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(long?)) return new NullableForceInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ulong?)) return new NullableForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer>();

        // no byte[]: that is bin territory (ByteArrayFormatters), and v3 has no ForceByteBlockArrayFormatter either
        if (type == typeof(sbyte[])) return new ForceSByteBlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(short[])) return new ForceInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ushort[])) return new ForceUInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(int[])) return new ForceInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(uint[])) return new ForceUInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(long[])) return new ForceInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ulong[])) return new ForceUInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
        return null;
    }
}

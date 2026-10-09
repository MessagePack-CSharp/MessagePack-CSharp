using System.ComponentModel;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Fixed-width integer formats: every integer type is always written in its own msgpack format
/// (int8/uint8 through int64/uint64) regardless of value, instead of the smallest format that fits.
/// Reads stay lenient and accept any integer format.
/// Covers the eight integer primitives, their nullable forms, and their arrays (byte[] stays bin).
/// </summary>
/// <remarks>
/// For use as the argument of <see cref="MessagePackFormatterAttribute"/> on a member only:
/// <c>[MessagePackFormatter(typeof(ForceSizeFormatterFactory))] public int Value { get; set; }</c>. It is not a
/// factory to compose into a resolver chain: the generated formatters write integer members directly (their own
/// width codec, as v3's generated code did), the primitive-collection codecs on modern targets do the same, and only
/// the reflection tier would consult the chain, so a chain placement would force the width on some paths and not on
/// others. The member attribute reaches every path, through the generated and the reflection formatters alike.
/// </remarks>
public sealed partial class ForceSizeFormatterFactory : MessagePackFormatterFactory
{
    /// <summary>
    /// For <see cref="MessagePackFormatterAttribute"/> only: the generated and reflection formatters construct the
    /// factory the attribute names. Not meant to be called, or composed into a chain, by hand (see the type remarks).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
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

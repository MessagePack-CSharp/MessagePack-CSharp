using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Serializes <see cref="DateTime"/> as its <see cref="DateTime.ToBinary"/> int64, preserving <see cref="DateTimeKind"/>:
/// the format of v3's NativeDateTimeResolver, which v3's TypelessContractlessStandardResolver (and so
/// <c>MessagePackSerializer.Typeless</c>) put first. Compose it before the chain to read and write v3 Typeless data,
/// where DateTime, DateTime? and DateTime[] members and DateTime collections travel in this form:
/// <c>MessagePackFormatterFactory.Combine(NativeDateTimeFormatterFactory.Instance, MessagePackFormatterFactory.Default).WithContractless(allowPrivate: true).WithTypeless(...)</c>.
/// Reads also accept the default timestamp ext. Only DateTime is affected; the other types keep the chain's formats
/// (use <see cref="DotNetOptimizedFormatterFactory"/> for the full .NET-to-.NET set).
/// </summary>
public sealed partial class NativeDateTimeFormatterFactory : MessagePackFormatterFactory
{
    /// <summary>Shared instance.</summary>
    public static readonly NativeDateTimeFormatterFactory Instance = new NativeDateTimeFormatterFactory();

    /// <summary>Creates a new instance, for use with <see cref="MessagePackFormatterAttribute"/> on a single member. Chains use <see cref="Instance"/>.</summary>
    public NativeDateTimeFormatterFactory()
    {
    }

    // One method, two signatures, see DotNetOptimizedFormatterFactory.
    /// <summary>Creates the formatter for <see cref="DateTime"/>, or returns null for any other type.</summary>
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(DateTime) ? new DotNetOptimizedDateTimeFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

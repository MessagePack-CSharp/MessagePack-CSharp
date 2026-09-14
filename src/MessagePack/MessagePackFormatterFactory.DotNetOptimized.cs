using System.Collections;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Formats that favor .NET-to-.NET exchange, used by <see cref="MessagePackFormatterFactory.DotNetOptimized"/>.
/// Guid and decimal are written as 16-byte binary, DateTime through ToBinary preserving <see cref="DateTimeKind"/>, DateTimeOffset as ticks and offset, and BitArray bit-packed.
/// Reads are validated, so untrusted input is as safe as with the default formats.
/// </summary>
public sealed partial class DotNetOptimizedFormatterFactory : MessagePackFormatterFactory
{
    /// <summary>Shared instance.</summary>
    public static readonly DotNetOptimizedFormatterFactory Instance = new DotNetOptimizedFormatterFactory();

    // Public because [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))] constructs its own instance
    // (the attribute paths cannot reach Instance) and the type dispatch picks the member's format, so this one factory
    // covers per-member use too. Chain composition keeps using Instance.
    /// <summary>Creates a new instance, for use with <see cref="MessagePackFormatterAttribute"/> on a single member. Chains use <see cref="Instance"/>.</summary>
    public DotNetOptimizedFormatterFactory()
    {
    }

    // One method, two signatures. net9+ overrides the base virtual (constraints inherited), while downlevel has no base
    // member, so the constraints are spelled out.
    /// <summary>Creates a formatter for <paramref name="type"/>, or returns null when it has no .NET-optimized format.</summary>
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(Guid)) return new DotNetOptimizedGuidFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(decimal)) return new DotNetOptimizedDecimalFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(DateTime)) return new DotNetOptimizedDateTimeFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(DateTimeOffset)) return new DotNetOptimizedDateTimeOffsetFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(BitArray)) return new PackedBitArrayFormatter<TWriteBuffer, TReadBuffer>();
        return null;
    }
}

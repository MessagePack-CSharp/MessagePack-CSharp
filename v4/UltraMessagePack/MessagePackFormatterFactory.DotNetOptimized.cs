using System.Collections;
using UltraMessagePack.Formatters;

namespace UltraMessagePack;

/// <summary>
/// The .NET-to-.NET wire tier behind <see cref="MessagePackFormatterFactory.DotNetOptimized"/>:
/// Guid/decimal as 16-byte little-endian binary images, DateTime as ToBinary
/// (Kind-preserving), DateTimeOffset as Tick + Offset, BitArray bit-packed.
/// All reads are validated - as safe for untrusted input as the default chain.
/// </summary>
public sealed partial class DotNetOptimizedFormatterFactory : MessagePackFormatterFactory
{
    public static readonly DotNetOptimizedFormatterFactory Instance = new DotNetOptimizedFormatterFactory();

    DotNetOptimizedFormatterFactory()
    {
    }

    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
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

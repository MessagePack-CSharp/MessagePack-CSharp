using System.Buffers.Binary;

namespace MessagePack;

// Naming follows Microsoft.NET.HostModel's ConvertToBigEndian/ConvertFromBigEndian.

/// <summary>
/// Host to msgpack byte order (big-endian) conversion.
/// ToBigEndian for writes, FromBigEndian for reads.
/// </summary>
internal static class MessagePackEndian
{
    // ToBigEndian

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ToBigEndian(ushort value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static short ToBigEndian(short value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ToBigEndian(uint value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ToBigEndian(int value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ToBigEndian(ulong value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ToBigEndian(long value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    // FromBigEndian

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort FromBigEndian(ushort value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static short FromBigEndian(short value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint FromBigEndian(uint value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int FromBigEndian(int value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong FromBigEndian(ulong value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long FromBigEndian(long value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
}

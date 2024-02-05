// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFloat32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFloat64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.Float64;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt8(ref byte destination, sbyte value)
    {
        destination = MessagePackCode.Int8;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt8(ref byte destination, byte value)
    {
        destination = MessagePackCode.UInt8;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt16(ref byte destination, short value)
    {
        destination = MessagePackCode.Int16;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt16(ref byte destination, ushort value)
    {
        destination = MessagePackCode.UInt16;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32(ref byte destination, int value)
    {
        destination = MessagePackCode.Int32;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt32(ref byte destination, uint value)
    {
        destination = MessagePackCode.UInt32;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt64(ref byte destination, long value)
    {
        destination = MessagePackCode.Int64;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.UInt64;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }
}

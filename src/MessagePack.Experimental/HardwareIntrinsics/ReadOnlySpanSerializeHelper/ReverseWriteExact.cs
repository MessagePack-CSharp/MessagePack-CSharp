// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteFloat32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteFloat64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.Float64;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 56);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 48);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 40);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(value >> 32);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 6) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 7) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 8) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteInt16(ref byte destination, ushort value)
    {
        destination = MessagePackCode.Int16;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteUInt16(ref byte destination, ushort value)
    {
        destination = MessagePackCode.UInt16;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteInt32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Int32;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteUInt32(ref byte destination, uint value)
    {
        destination = MessagePackCode.UInt32;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteInt64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.Int64;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 56);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 48);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 40);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(value >> 32);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 6) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 7) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 8) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteUInt64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.UInt64;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 56);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 48);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 40);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(value >> 32);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 6) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 7) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 8) = (byte)value;
    }
}

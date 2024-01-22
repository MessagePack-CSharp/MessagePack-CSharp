// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePack.Formatters;

internal static partial class ReadOnlySpanSerializeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFloat64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.Float64;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte ReverseWriteFloat64(ref byte destination, ulong value)
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
        return ref Unsafe.AddByteOffset(ref destination, sizeof(ulong) + 1);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref double input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFloat32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 1), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte ReverseWriteFloat32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.AddByteOffset(ref destination, 1) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)value;
        return ref Unsafe.AddByteOffset(ref destination, sizeof(ulong) + 1);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref float input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }
}

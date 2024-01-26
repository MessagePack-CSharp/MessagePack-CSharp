// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack.Formatters;

internal static partial class ReadOnlySpanSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref readonly DateTime input, int length)
    {
        ThrowIfOldSpec(ref writer);
        writer.WriteArrayHeader(length);
        if (length == 0)
        {
            return;
        }

        ref var inputIterator = ref Unsafe.AsRef(in input);
        if (!BitConverter.IsLittleEndian)
        {
            BigEndianSerialize(ref writer, ref inputIterator, length, writer.CancellationToken);
            return;
        }

        const int maxOutputElementSize = 15;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;
        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            nuint outputOffset = 0;
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), Unsafe.Add(ref inputIterator, index));
            }

            length -= inputLength;
            writer.Advance((int)outputOffset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Local)
        {
            dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);
        }

        var ticksSinceBclEpoch = dateTime.Ticks - (BclSecondsAtUnixEpoch * TimeSpan.TicksPerSecond);
        var seconds = ticksSinceBclEpoch / TimeSpan.TicksPerSecond;
        var nanoseconds = (ticksSinceBclEpoch % TimeSpan.TicksPerSecond) * NanosecondsPerTick;

        if ((seconds >>> 34) == 0)
        {
            var data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
            if ((data64 >>> 32) == 0)
            {
                // timestamp 32(seconds in 32-bit unsigned int)
                ReverseWriteFixExt4DateTime(ref destination, (uint)data64);
                return 6;
            }
            else
            {
                // timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
                ReverseWriteFixExt8DateTime(ref destination, data64);
                return 10;
            }
        }
        else
        {
            // timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
            ReverseWriteExt8DateTime(ref destination, (uint)nanoseconds, (ulong)seconds);
            return 15;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFixExt4DateTime(ref byte destination, uint value)
    {
        destination = MessagePackCode.FixExt4;
        Unsafe.AddByteOffset(ref destination, 1) = DateTimeExtensionTypeCode;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 2), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFixExt8DateTime(ref byte destination, ulong value)
    {
        destination = MessagePackCode.FixExt8;
        Unsafe.AddByteOffset(ref destination, 1) = DateTimeExtensionTypeCode;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 2), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteExt8DateTime(ref byte destination, uint nanoseconds, ulong seconds)
    {
        destination = MessagePackCode.Ext8;
        Unsafe.AddByteOffset(ref destination, 1) = 12;
        Unsafe.AddByteOffset(ref destination, 2) = DateTimeExtensionTypeCode;
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 3), nanoseconds);
        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref destination, 7), seconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteFixExt4DateTime(ref byte destination, uint value)
    {
        destination = MessagePackCode.FixExt4;
        Unsafe.AddByteOffset(ref destination, 1) = DateTimeExtensionTypeCode;
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteFixExt8DateTime(ref byte destination, ulong value)
    {
        destination = MessagePackCode.FixExt8;
        Unsafe.AddByteOffset(ref destination, 1) = DateTimeExtensionTypeCode;
        Unsafe.AddByteOffset(ref destination, 2) = (byte)(value >> 56);
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(value >> 48);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(value >> 40);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)(value >> 32);
        Unsafe.AddByteOffset(ref destination, 6) = (byte)(value >> 24);
        Unsafe.AddByteOffset(ref destination, 7) = (byte)(value >> 16);
        Unsafe.AddByteOffset(ref destination, 8) = (byte)(value >> 8);
        Unsafe.AddByteOffset(ref destination, 9) = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseWriteExt8DateTime(ref byte destination, uint nanoseconds, ulong seconds)
    {
        destination = MessagePackCode.Ext8;
        Unsafe.AddByteOffset(ref destination, 1) = 12;
        Unsafe.AddByteOffset(ref destination, 2) = DateTimeExtensionTypeCode;
        Unsafe.AddByteOffset(ref destination, 3) = (byte)(nanoseconds >> 24);
        Unsafe.AddByteOffset(ref destination, 4) = (byte)(nanoseconds >> 16);
        Unsafe.AddByteOffset(ref destination, 5) = (byte)(nanoseconds >> 8);
        Unsafe.AddByteOffset(ref destination, 6) = (byte)nanoseconds;
        Unsafe.AddByteOffset(ref destination, 7) = (byte)(seconds >> 56);
        Unsafe.AddByteOffset(ref destination, 8) = (byte)(seconds >> 48);
        Unsafe.AddByteOffset(ref destination, 9) = (byte)(seconds >> 40);
        Unsafe.AddByteOffset(ref destination, 10) = (byte)(seconds >> 32);
        Unsafe.AddByteOffset(ref destination, 11) = (byte)(seconds >> 24);
        Unsafe.AddByteOffset(ref destination, 12) = (byte)(seconds >> 16);
        Unsafe.AddByteOffset(ref destination, 13) = (byte)(seconds >> 8);
        Unsafe.AddByteOffset(ref destination, 14) = (byte)seconds;
    }

    private const byte DateTimeExtensionTypeCode = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
    private const long BclSecondsAtUnixEpoch = 62135596800;
    private const int NanosecondsPerTick = 100;
    private const long FifteenByteTicks = 78125L << 41;

    private static void ThrowIfOldSpec(ref MessagePackWriter writer)
    {
        if (writer.OldSpec)
        {
            throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(writer.OldSpec)} mode. Turn off {nameof(writer.OldSpec)} mode or use NativeDateTimeFormatter.");
        }
    }
}

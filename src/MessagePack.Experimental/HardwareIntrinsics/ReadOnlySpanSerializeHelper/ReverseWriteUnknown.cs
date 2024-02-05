// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, byte value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            destination = value;
            return 1;
        }
        else
        {
            WriteUInt8(ref destination, value);
            return 2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, sbyte value)
    {
        if (value < MessagePackRange.MinFixNegativeInt)
        {
            WriteInt8(ref destination, value);
            return 2;
        }
        else
        {
            destination = (byte)value;
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, ushort value)
    {
        if (value <= byte.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, (byte)value);
        }
        else
        {
            ReverseWriteUInt16(ref destination, value);
            return 3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, short value)
    {
        if (value < 0)
        {
            if (value >= sbyte.MinValue)
            {
                return ReverseWriteUnknown(ref destination, (sbyte)value);
            }
            else
            {
                ReverseWriteInt16(ref destination, (ushort)value);
                return 3;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, (ushort)value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, uint value)
    {
        if (value <= ushort.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, (ushort)value);
        }
        else
        {
            ReverseWriteUInt32(ref destination, value);
            return 5;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, int value)
    {
        if (value < 0)
        {
            if (value >= short.MinValue)
            {
                return ReverseWriteUnknown(ref destination, (short)value);
            }
            else
            {
                ReverseWriteInt32(ref destination, (uint)value);
                return 5;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, (uint)value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, ulong value)
    {
        if (value <= uint.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, (uint)value);
        }
        else
        {
            ReverseWriteUInt64(ref destination, value);
            return 9;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, long value)
    {
        if (value < 0)
        {
            if (value >= int.MinValue)
            {
                return ReverseWriteUnknown(ref destination, (int)value);
            }
            else
            {
                ReverseWriteInt64(ref destination, (ulong)value);
                return 9;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, (ulong)value);
        }
    }
}

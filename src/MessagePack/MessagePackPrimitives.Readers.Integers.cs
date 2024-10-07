// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;

#pragma warning disable SA1205 // Partial elements should declare access

namespace MessagePack;

partial class MessagePackPrimitives
{
    /// <summary>
    /// Tries to read an <see cref="byte"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out byte value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((byte)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((byte)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (byte)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="ushort"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out ushort value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ushort)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((ushort)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (ushort)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="uint"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out uint value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((uint)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((uint)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (uint)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="ulong"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out ulong value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((ulong)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((ulong)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (ulong)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="sbyte"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out sbyte value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((sbyte)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((sbyte)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (sbyte)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="short"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out short value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((short)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((short)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (short)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="int"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((int)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((int)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (int)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="long"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryRead(ReadOnlySpan<byte> source, out long value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = 0;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((long)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((long)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (long)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="float"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe ReadResult TryRead(ReadOnlySpan<byte> source, out float value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < 1)
        {
            value = default;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Float32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return ReadResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                value = *(float*)&uintValue;
                return ReadResult.Success;
            case MessagePackCode.Float64:
                tokenSize = 9;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return ReadResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ulong ulongValue));
                value = (float)(*(double*)&ulongValue);
                return ReadResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                ReadResult result = TryRead(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryRead(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="double"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe ReadResult TryRead(ReadOnlySpan<byte> source, out double value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < 1)
        {
            value = default;
            return ReadResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Float32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return ReadResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                value = *(float*)&uintValue;
                return ReadResult.Success;
            case MessagePackCode.Float64:
                tokenSize = 9;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return ReadResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ulong ulongValue));
                value = (double)(*(double*)&ulongValue);
                return ReadResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                ReadResult result = TryRead(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryRead(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return ReadResult.TokenMismatch;
        }
    }
}

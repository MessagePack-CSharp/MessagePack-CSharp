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
    /// Tries to read an <see cref="Byte"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadByte(ReadOnlySpan<byte> source, out Byte value, out int tokenSize)
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

                value = checked((Byte)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Byte)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((Byte)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (Byte)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="UInt16"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadUInt16(ReadOnlySpan<byte> source, out UInt16 value, out int tokenSize)
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

                value = checked((UInt16)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt16)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((UInt16)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (UInt16)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="UInt32"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadUInt32(ReadOnlySpan<byte> source, out UInt32 value, out int tokenSize)
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

                value = checked((UInt32)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt32)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((UInt32)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (UInt32)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="UInt64"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadUInt64(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
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

                value = checked((UInt64)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((UInt64)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((UInt64)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (UInt64)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="SByte"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadSByte(ReadOnlySpan<byte> source, out SByte value, out int tokenSize)
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

                value = checked((SByte)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((SByte)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((SByte)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (SByte)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Int16"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadInt16(ReadOnlySpan<byte> source, out Int16 value, out int tokenSize)
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

                value = checked((Int16)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int16)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((Int16)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (Int16)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Int32"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadInt32(ReadOnlySpan<byte> source, out Int32 value, out int tokenSize)
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

                value = checked((Int32)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int32)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((Int32)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (Int32)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Int64"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static ReadResult TryReadInt64(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
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

                value = checked((Int64)source[1]);
                return ReadResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)unchecked((sbyte)source[1]));
                return ReadResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)ushortResult);
                return ReadResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)shortResult);
                return ReadResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)uintResult);
                return ReadResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)intResult);
                return ReadResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)ulongResult);
                return ReadResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return ReadResult.InsufficientBuffer;
                }

                value = checked((Int64)longResult);
                return ReadResult.Success;
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                value = checked((Int64)unchecked((sbyte)source[0]));
                return ReadResult.Success;
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                value = (Int64)source[0];
                return ReadResult.Success;
            default:
                value = 0;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Single"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe ReadResult TryReadSingle(ReadOnlySpan<byte> source, out Single value, out int tokenSize)
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
                value = (Single)(*(double*)&ulongValue);
                return ReadResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                ReadResult result = TryReadInt64(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryReadUInt64(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return ReadResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Double"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe ReadResult TryReadDouble(ReadOnlySpan<byte> source, out Double value, out int tokenSize)
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
                value = (Double)(*(double*)&ulongValue);
                return ReadResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                ReadResult result = TryReadInt64(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryReadUInt64(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return ReadResult.TokenMismatch;
        }
    }
}

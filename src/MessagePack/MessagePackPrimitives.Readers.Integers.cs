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
    static partial class Decoders
    {
        private class ReadInt64Invalid : IReadInt64
        {
            internal static readonly ReadInt64Invalid Instance = new();

            private ReadInt64Invalid()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                value = 0;
                tokenSize = 1;
                return DecodeResult.TokenMismatch;
            }
        }

        private class ReadInt64FixInt : IReadInt64
        {
            internal static readonly ReadInt64FixInt Instance = new();

            private ReadInt64FixInt()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 1;
                value = source[0];
                return DecodeResult.Success;
            }
        }

        private class ReadInt64NegativeFixInt : IReadInt64
        {
            internal static readonly ReadInt64NegativeFixInt Instance = new();

            private ReadInt64NegativeFixInt()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 1;
                value = checked((Int64)unchecked((sbyte)source[0]));
                return DecodeResult.Success;
            }
        }

        private class ReadInt64UInt8 : IReadInt64
        {
            internal static readonly ReadInt64UInt8 Instance = new();

            private ReadInt64UInt8()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = source[1];
                return DecodeResult.Success;
            }
        }

        private class ReadInt64UInt16 : IReadInt64
        {
            internal static readonly ReadInt64UInt16 Instance = new();

            private ReadInt64UInt16()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = ushortResult;
                return DecodeResult.Success;
            }
        }

        private class ReadInt64UInt32 : IReadInt64
        {
            internal static readonly ReadInt64UInt32 Instance = new();

            private ReadInt64UInt32()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = uintResult;
                return DecodeResult.Success;
            }
        }

        private class ReadInt64UInt64 : IReadInt64
        {
            internal static readonly ReadInt64UInt64 Instance = new();

            private ReadInt64UInt64()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((Int64)ulongResult);
                return DecodeResult.Success;
            }
        }

        private class ReadInt64Int8 : IReadInt64
        {
            internal static readonly ReadInt64Int8 Instance = new();

            private ReadInt64Int8()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((Int64)unchecked((sbyte)source[1]));
                return DecodeResult.Success;
            }
        }

        private class ReadInt64Int16 : IReadInt64
        {
            internal static readonly ReadInt64Int16 Instance = new();

            private ReadInt64Int16()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((Int64)shortResult);
                return DecodeResult.Success;
            }
        }

        private class ReadInt64Int32 : IReadInt64
        {
            internal static readonly ReadInt64Int32 Instance = new();

            private ReadInt64Int32()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((Int64)intResult);
                return DecodeResult.Success;
            }
        }

        private class ReadInt64Int64 : IReadInt64
        {
            internal static readonly ReadInt64Int64 Instance = new();

            private ReadInt64Int64()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
            {
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((Int64)longResult);
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64Invalid : IReadUInt64
        {
            internal static readonly ReadUInt64Invalid Instance = new();

            private ReadUInt64Invalid()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                value = 0;
                tokenSize = 1;
                return DecodeResult.TokenMismatch;
            }
        }

        private class ReadUInt64FixInt : IReadUInt64
        {
            internal static readonly ReadUInt64FixInt Instance = new();

            private ReadUInt64FixInt()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 1;
                value = source[0];
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64NegativeFixInt : IReadUInt64
        {
            internal static readonly ReadUInt64NegativeFixInt Instance = new();

            private ReadUInt64NegativeFixInt()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 1;
                value = checked((UInt64)unchecked((sbyte)source[0]));
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64UInt8 : IReadUInt64
        {
            internal static readonly ReadUInt64UInt8 Instance = new();

            private ReadUInt64UInt8()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = source[1];
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64UInt16 : IReadUInt64
        {
            internal static readonly ReadUInt64UInt16 Instance = new();

            private ReadUInt64UInt16()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out ushort ushortResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = ushortResult;
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64UInt32 : IReadUInt64
        {
            internal static readonly ReadUInt64UInt32 Instance = new();

            private ReadUInt64UInt32()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out uint uintResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = uintResult;
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64UInt64 : IReadUInt64
        {
            internal static readonly ReadUInt64UInt64 Instance = new();

            private ReadUInt64UInt64()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out ulong ulongResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((UInt64)ulongResult);
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64Int8 : IReadUInt64
        {
            internal static readonly ReadUInt64Int8 Instance = new();

            private ReadUInt64Int8()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((UInt64)unchecked((sbyte)source[1]));
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64Int16 : IReadUInt64
        {
            internal static readonly ReadUInt64Int16 Instance = new();

            private ReadUInt64Int16()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 3;
                if (!TryReadBigEndian(source.Slice(1), out short shortResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((UInt64)shortResult);
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64Int32 : IReadUInt64
        {
            internal static readonly ReadUInt64Int32 Instance = new();

            private ReadUInt64Int32()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 5;
                if (!TryReadBigEndian(source.Slice(1), out int intResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((UInt64)intResult);
                return DecodeResult.Success;
            }
        }

        private class ReadUInt64Int64 : IReadUInt64
        {
            internal static readonly ReadUInt64Int64 Instance = new();

            private ReadUInt64Int64()
            {
            }

            public DecodeResult Read(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
            {
                tokenSize = 9;
                if (!TryReadBigEndian(source.Slice(1), out long longResult))
                {
                    value = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                value = checked((UInt64)longResult);
                return DecodeResult.Success;
            }
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Byte"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static DecodeResult TryReadByte(ReadOnlySpan<byte> source, out Byte value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.UInt64JumpTable[source[0]].Read(source, out ulong longValue, out tokenSize);
            value = checked((Byte)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadUInt16(ReadOnlySpan<byte> source, out UInt16 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.UInt64JumpTable[source[0]].Read(source, out ulong longValue, out tokenSize);
            value = checked((UInt16)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadUInt32(ReadOnlySpan<byte> source, out UInt32 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.UInt64JumpTable[source[0]].Read(source, out ulong longValue, out tokenSize);
            value = checked((UInt32)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadUInt64(ReadOnlySpan<byte> source, out UInt64 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.UInt64JumpTable[source[0]].Read(source, out ulong longValue, out tokenSize);
            value = checked((UInt64)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadSByte(ReadOnlySpan<byte> source, out SByte value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.Int64JumpTable[source[0]].Read(source, out long longValue, out tokenSize);
            value = checked((SByte)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadInt16(ReadOnlySpan<byte> source, out Int16 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.Int64JumpTable[source[0]].Read(source, out long longValue, out tokenSize);
            value = checked((Int16)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadInt32(ReadOnlySpan<byte> source, out Int32 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.Int64JumpTable[source[0]].Read(source, out long longValue, out tokenSize);
            value = checked((Int32)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
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
    public static DecodeResult TryReadInt64(ReadOnlySpan<byte> source, out Int64 value, out int tokenSize)
    {
        if (source.Length > 0)
        {
            DecodeResult result = Decoders.Int64JumpTable[source[0]].Read(source, out long longValue, out tokenSize);
            value = checked((Int64)longValue);
            return result;
        }
        else
        {
            tokenSize = 1;
            value = 0;
            return DecodeResult.EmptyBuffer;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Single"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe DecodeResult TryReadSingle(ReadOnlySpan<byte> source, out Single value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < 1)
        {
            value = default;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Float32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                value = *(float*)&uintValue;
                return DecodeResult.Success;
            case MessagePackCode.Float64:
                tokenSize = 9;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ulong ulongValue));
                value = (Single)(*(double*)&ulongValue);
                return DecodeResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                DecodeResult result = TryReadInt64(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryReadUInt64(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read an <see cref="Double"/> value from a <see cref="MessagePackCode.Float64"/> or <see cref="MessagePackCode.Float32"/>
    /// or any of the integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public static unsafe DecodeResult TryReadDouble(ReadOnlySpan<byte> source, out Double value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < 1)
        {
            value = default;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Float32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                value = *(float*)&uintValue;
                return DecodeResult.Success;
            case MessagePackCode.Float64:
                tokenSize = 9;
                if (source.Length < tokenSize)
                {
                    value = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ulong ulongValue));
                value = (Double)(*(double*)&ulongValue);
                return DecodeResult.Success;
            case MessagePackCode.Int8 or MessagePackCode.Int16 or MessagePackCode.Int32 or MessagePackCode.Int64:
            case >= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt:
                DecodeResult result = TryReadInt64(source, out long longValue, out tokenSize);
                value = longValue;
                return result;
            case MessagePackCode.UInt8 or MessagePackCode.UInt16 or MessagePackCode.UInt32 or MessagePackCode.UInt64:
            case >= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt:
                result = TryReadUInt64(source, out ulongValue, out tokenSize);
                value = ulongValue;
                return result;
            default:
                value = default;
                return DecodeResult.TokenMismatch;
        }
    }
}

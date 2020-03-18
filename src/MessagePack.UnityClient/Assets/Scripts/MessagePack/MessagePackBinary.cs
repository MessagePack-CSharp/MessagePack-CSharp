// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using MessagePack.Internal;

namespace MessagePack
{
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    static class MessagePackBinary
    {
        /// <summary>
        /// Ties to write a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        public static int WriteNil(Span<byte> span)
        {
            if (span.Length >= 1)
            {
                span[0] = MessagePackCode.Nil;
                return 1;
            }
            else
            {
                return ~1;
            }
        }

        /// <summary>
        /// Tries to write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="count">The number of elements that will be written in the array.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        public static int WriteArrayHeader(Span<byte> span, int count) => WriteArrayHeader(span, (uint)count);

        /// <summary>
        /// Tries to write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="count">The number of elements that will be written in the array.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteArrayHeader(Span<byte> span, uint count)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                if (span.Length >= 1)
                {
                    span[0] = (byte)(MessagePackCode.MinFixArray | count);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (count <= ushort.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.Array16;
                    WriteBigEndian(span, (ushort)count);
                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else
            {
                if (span.Length >= 5)
                {
                    span[0] = MessagePackCode.Array32;
                    WriteBigEndian(span, count);
                    return 5;
                }
                else
                {
                    return ~5;
                }
            }
        }

        /// <summary>
        /// Tries to write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        public static int WriteMapHeader(Span<byte> span, int count) => WriteMapHeader(span, (uint)count);

        /// <summary>
        /// Tries to write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteMapHeader(Span<byte> span, uint count)
        {
            if (count <= MessagePackRange.MaxFixMapCount)
            {
                if (span.Length >= 1)
                {
                    span[0] = (byte)(MessagePackCode.MinFixMap | count);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (count <= ushort.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.Map16;
                    WriteBigEndian(span, (ushort)count);
                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else
            {
                if (span.Length >= 5)
                {
                    span[0] = MessagePackCode.Map32;
                    WriteBigEndian(span, count);
                    return 5;
                }
                else
                {
                    return ~5;
                }
            }
        }

        /// <summary>
        /// Tries to write a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, byte value)
        {
            if (value <= MessagePackCode.MaxFixInt)
            {
                if (span.Length >= 1)
                {
                    span[0] = value;
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else
            {
                return WriteUInt8(span, value);
            }
        }

        /// <summary>
        /// Tries to write a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt8(Span<byte> span, byte value)
        {
            if (span.Length >= 2)
            {
                span[0] = MessagePackCode.UInt8;
                span[1] = value;
                return 2;
            }
            else
            {
                return ~2;
            }
        }

        /// <summary>
        /// Tries to write an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, sbyte value)
        {
            if (value < MessagePackRange.MinFixNegativeInt)
            {
                return WriteInt8(span, value);
            }
            else
            {
                if (span.Length >= 1)
                {
                    span[0] = unchecked((byte)value);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
        }

        /// <summary>
        /// Tries to write an 8-bit value using <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt8(Span<byte> span, sbyte value)
        {
            if (span.Length >= 2)
            {
                span[0] = MessagePackCode.Int8;
                span[1] = unchecked((byte)value);
                return 2;
            }
            else
            {
                return ~2;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, ushort value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                if (span.Length >= 1)
                {
                    span[0] = unchecked((byte)value);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (value <= byte.MaxValue)
            {
                if (span.Length >= 2)
                {
                    span[0] = MessagePackCode.UInt8;
                    span[1] = unchecked((byte)value);
                    return 2;
                }
                else
                {
                    return ~2;
                }
            }
            else
            {
                return WriteUInt16(span, value);
            }
        }

        /// <summary>
        /// Tries to write a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt16(Span<byte> span, ushort value)
        {
            if (span.Length >= 3)
            {
                span[0] = MessagePackCode.UInt16;
                WriteBigEndian(span, value);
                return 3;
            }
            else
            {
                return ~3;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, short value)
        {
            if (value >= 0)
            {
                return Write(span, (ushort)value);
            }
            else
            {
                // negative int(use int)
                if (value >= MessagePackRange.MinFixNegativeInt)
                {
                    if (span.Length >= 1)
                    {
                        span[0] = unchecked((byte)value);
                        return 1;
                    }
                    else
                    {
                        return ~1;
                    }
                }
                else if (value >= sbyte.MinValue)
                {
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.Int8;
                        span[1] = unchecked((byte)value);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }
                }
                else
                {
                    return WriteInt16(span, value);
                }
            }
        }

        /// <summary>
        /// Tries to write a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt16(Span<byte> span, short value)
        {
            if (span.Length >= 3)
            {
                span[0] = MessagePackCode.Int16;
                WriteBigEndian(span, value);
                return 3;
            }
            else
            {
                return ~3;
            }
        }

        /// <summary>
        /// Tries to write an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                if (span.Length >= 1)
                {
                    span[0] = unchecked((byte)value);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (value <= byte.MaxValue)
            {
                if (span.Length >= 2)
                {
                    span[0] = MessagePackCode.UInt8;
                    span[1] = unchecked((byte)value);
                    return 2;
                }
                else
                {
                    return ~2;
                }
            }
            else if (value <= ushort.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.UInt16;
                    WriteBigEndian(span, (ushort)value);
                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else
            {
                return WriteUInt32(span, value);
            }
        }

        /// <summary>
        /// Tries to write an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt32(Span<byte> span, uint value)
        {
            if (span.Length >= 5)
            {
                span[0] = MessagePackCode.UInt32;
                WriteBigEndian(span, value);
                return 5;
            }
            else
            {
                return ~5;
            }
        }

        /// <summary>
        /// Tries to write an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, int value)
        {
            if (value >= 0)
            {
                return Write(span, (uint)value);
            }
            else
            {
                // negative int(use int)
                if (value >= MessagePackRange.MinFixNegativeInt)
                {
                    if (span.Length >= 1)
                    {
                        span[0] = unchecked((byte)value);
                        return 1;
                    }
                    else
                    {
                        return ~1;
                    }
                }
                else if (value >= sbyte.MinValue)
                {
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.Int8;
                        span[1] = unchecked((byte)value);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }
                }
                else if (value >= short.MinValue)
                {
                    if (span.Length >= 3)
                    {
                        span[0] = MessagePackCode.Int16;
                        WriteBigEndian(span, (short)value);
                        return 3;
                    }
                    else
                    {
                        return ~3;
                    }
                }
                else
                {
                    return WriteInt32(span, value);
                }
            }
        }

        /// <summary>
        /// Tries to write an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt32(Span<byte> span, int value)
        {
            if (span.Length >= 5)
            {
                span[0] = MessagePackCode.Int32;
                WriteBigEndian(span, value);
                return 5;
            }
            else
            {
                return ~5;
            }
        }

        /// <summary>
        /// Tries to write an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, ulong value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                if (span.Length >= 1)
                {
                    span[0] = unchecked((byte)value);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (value <= byte.MaxValue)
            {
                if (span.Length >= 2)
                {
                    span[0] = MessagePackCode.UInt8;
                    span[1] = unchecked((byte)value);
                    return 2;
                }
                else
                {
                    return ~2;
                }
            }
            else if (value <= ushort.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.UInt16;
                    WriteBigEndian(span, (ushort)value);
                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else if (value <= uint.MaxValue)
            {
                if (span.Length >= 5)
                {
                    span[0] = MessagePackCode.UInt32;
                    WriteBigEndian(span, (uint)value);
                    return 5;
                }
                else
                {
                    return ~5;
                }
            }
            else
            {
                return WriteUInt64(span, value);
            }
        }

        /// <summary>
        /// Tries to write an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt64(Span<byte> span, ulong value)
        {
            if (span.Length >= 9)
            {
                span[0] = MessagePackCode.UInt64;
                WriteBigEndian(span, value);
                return 9;
            }
            else
            {
                return ~9;
            }
        }

        /// <summary>
        /// Tries to write an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, long value)
        {
            if (value >= 0)
            {
                return Write(span, (ulong)value);
            }
            else
            {
                // negative int(use int)
                if (value >= MessagePackRange.MinFixNegativeInt)
                {
                    if (span.Length >= 1)
                    {
                        span[0] = unchecked((byte)value);
                        return 1;
                    }
                    else
                    {
                        return ~1;
                    }
                }
                else if (value >= sbyte.MinValue)
                {
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.Int8;
                        span[1] = unchecked((byte)value);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }
                }
                else if (value >= short.MinValue)
                {
                    if (span.Length >= 3)
                    {
                        span[0] = MessagePackCode.Int16;
                        WriteBigEndian(span, (short)value);
                        return 3;
                    }
                    else
                    {
                        return ~3;
                    }
                }
                else if (value >= int.MinValue)
                {
                    if (span.Length >= 5)
                    {
                        span[0] = MessagePackCode.Int32;
                        WriteBigEndian(span, (int)value);
                        return 5;
                    }
                    else
                    {
                        return ~5;
                    }
                }
                else
                {
                    return WriteInt64(span, value);
                }
            }
        }

        /// <summary>
        /// Tries to write a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt64(Span<byte> span, long value)
        {
            if (span.Length >= 9)
            {
                span[0] = MessagePackCode.Int64;
                WriteBigEndian(span, value);
                return 9;
            }
            else
            {
                return ~9;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, bool value)
        {
            if (span.Length >= 1)
            {
                span[0] = value ? MessagePackCode.True : MessagePackCode.False;
                return 1;
            }
            else
            {
                return ~1;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        public static int Write(Span<byte> span, char value) => Write(span, (ushort)value);

        /// <summary>
        /// Tries to write a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, float value)
        {
            if (span.Length >= 5)
            {
                span[0] = MessagePackCode.Float32;
                WriteBigEndian(span, value);
                return 5;
            }
            else
            {
                return ~5;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="value">The value.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, double value)
        {
            if (span.Length >= 9)
            {
                span[0] = MessagePackCode.Float64;
                WriteBigEndian(span, value);
                return 9;
            }
            else
            {
                return ~9;
            }
        }

        /// <summary>
        /// Tries to write a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="dateTime">The value to write.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> span, DateTime dateTime)
        {
            // Timestamp spec
            // https://github.com/msgpack/msgpack/pull/209
            // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
            // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
            // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

            // The spec requires UTC. Convert to UTC if we're sure the value was expressed as Local time.
            // If it's Unspecified, we want to leave it alone since .NET will change the value when we convert
            // and we simply don't know, so we should leave it as-is.
            if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }

            var secondsSinceBclEpoch = dateTime.Ticks / TimeSpan.TicksPerSecond;
            var seconds = secondsSinceBclEpoch - DateTimeConstants.BclSecondsAtUnixEpoch;
            var nanoseconds = (dateTime.Ticks % TimeSpan.TicksPerSecond) * DateTimeConstants.NanosecondsPerTick;

            // reference pseudo code.
            /*
            struct timespec {
                long tv_sec;  // seconds
                long tv_nsec; // nanoseconds
            } time;
            if ((time.tv_sec >> 34) == 0)
            {
                uint64_t data64 = (time.tv_nsec << 34) | time.tv_sec;
                if (data & 0xffffffff00000000L == 0)
                {
                    // timestamp 32
                    uint32_t data32 = data64;
                    serialize(0xd6, -1, data32)
                }
                else
                {
                    // timestamp 64
                    serialize(0xd7, -1, data64)
                }
            }
            else
            {
                // timestamp 96
                serialize(0xc7, 12, -1, time.tv_nsec, time.tv_sec)
            }
            */

            if ((seconds >> 34) == 0)
            {
                var data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
                if ((data64 & 0xffffffff00000000L) == 0)
                {
                    // timestamp 32(seconds in 32-bit unsigned int)
                    var data32 = (uint)data64;
                    if (span.Length >= 6)
                    {
                        span[0] = MessagePackCode.FixExt4;
                        span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                        WriteBigEndian(span.Slice(1), data32);
                        return 6;
                    }
                    else
                    {
                        return ~6;
                    }
                }
                else
                {
                    // timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
                    if (span.Length >= 10)
                    {
                        span[0] = MessagePackCode.FixExt8;
                        span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                        WriteBigEndian(span.Slice(1), data64);
                        return 10;
                    }
                    else
                    {
                        return ~10;
                    }
                }
            }
            else
            {
                // timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
                if (span.Length >= 15)
                {
                    span[0] = MessagePackCode.Ext8;
                    span[1] = 12;
                    span[2] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                    WriteBigEndian(span.Slice(2), (uint)nanoseconds);
                    WriteBigEndian(span.Slice(6), seconds);
                    return 15;
                }
                else
                {
                    return ~5;
                }
            }
        }

        /// <summary>
        /// Tries to write the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="length">The length of bytes that will be written next.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteBinHeader(Span<byte> span, int length)
        {
            if (length <= byte.MaxValue)
            {
                if (span.Length >= 2)
                {
                    span[0] = MessagePackCode.Bin8;
                    span[1] = (byte)length;
                    return 2;
                }
                else
                {
                    return ~2;
                }
            }
            else if (length <= UInt16.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.Bin16;
                    WriteBigEndian(span, (ushort)length);

                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else
            {
                if (span.Length >= 5)
                {
                    span[0] = MessagePackCode.Bin32;
                    WriteBigEndian(span, length);

                    return 5;
                }
                else
                {
                    return ~5;
                }
            }
        }

        /// <summary>
        /// Tries to write out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
        /// <param name="oldSpec">If true, the old spec will be used instead.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteStringHeader(Span<byte> span, int byteCount, bool oldSpec = false)
        {
            // When we write the header, we'll ask for all the space we need for the payload as well
            // as that may help ensure we only allocate a buffer once.
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (span.Length >= 1)
                {
                    span[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                    return 1;
                }
                else
                {
                    return ~1;
                }
            }
            else if (byteCount <= byte.MaxValue && !oldSpec)
            {
                if (span.Length >= 2)
                {
                    span[0] = MessagePackCode.Str8;
                    span[1] = unchecked((byte)byteCount);
                    return 2;
                }
                else
                {
                    return ~2;
                }
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (span.Length >= 3)
                {
                    span[0] = MessagePackCode.Str16;
                    WriteBigEndian(span, (ushort)byteCount);
                    return 3;
                }
                else
                {
                    return ~3;
                }
            }
            else
            {
                if (span.Length >= 5)
                {
                    span[0] = MessagePackCode.Str32;
                    WriteBigEndian(span, byteCount);
                    return 5;
                }
                else
                {
                    return ~5;
                }
            }
        }

        /// <summary>
        /// Tries to write the extension format header, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="span">The memory region to write to.</param>
        /// <param name="extensionHeader">The extension header.</param>
        /// <returns>The number of bytes written or the binary negation of the number of bytes that would be written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteExtensionFormatHeader(Span<byte> span, in ExtensionHeader extensionHeader)
        {
            int dataLength = (int)extensionHeader.Length;
            byte typeCode = unchecked((byte)extensionHeader.TypeCode);
            switch (dataLength)
            {
                case 1:
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.FixExt1;
                        span[1] = unchecked(typeCode);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }

                case 2:
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.FixExt2;
                        span[1] = unchecked(typeCode);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }

                case 4:
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.FixExt4;
                        span[1] = unchecked(typeCode);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }

                case 8:
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.FixExt8;
                        span[1] = unchecked(typeCode);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }

                case 16:
                    if (span.Length >= 2)
                    {
                        span[0] = MessagePackCode.FixExt16;
                        span[1] = unchecked(typeCode);
                        return 2;
                    }
                    else
                    {
                        return ~2;
                    }

                default:
                    unchecked
                    {
                        if (dataLength <= byte.MaxValue)
                        {
                            if (span.Length >= 3)
                            {
                                span[0] = MessagePackCode.Ext8;
                                span[1] = unchecked((byte)dataLength);
                                span[2] = unchecked(typeCode);
                                return 3;
                            }
                            else
                            {
                                return ~3;
                            }
                        }
                        else if (dataLength <= UInt16.MaxValue)
                        {
                            if (span.Length >= 4)
                            {
                                span[0] = MessagePackCode.Ext16;
                                WriteBigEndian(span, (ushort)dataLength);
                                span[3] = unchecked(typeCode);
                                return 4;
                            }
                            else
                            {
                                return ~4;
                            }
                        }
                        else
                        {
                            if (span.Length >= 6)
                            {
                                span[0] = MessagePackCode.Ext32;
                                WriteBigEndian(span, dataLength);
                                span[5] = unchecked(typeCode);
                                return 6;
                            }
                            else
                            {
                                return ~6;
                            }
                        }
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, short value) => WriteBigEndian(span, unchecked((ushort)value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, int value) => WriteBigEndian(span, unchecked((uint)value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, long value) => WriteBigEndian(span, unchecked((ulong)value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, ushort value)
        {
            unchecked
            {
                span[1] = (byte)(value >> 8);
                span[2] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, uint value)
        {
            unchecked
            {
                span[1] = (byte)(value >> 24);
                span[2] = (byte)(value >> 16);
                span[3] = (byte)(value >> 8);
                span[4] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBigEndian(Span<byte> span, ulong value)
        {
            unchecked
            {
                span[1] = (byte)(value >> 56);
                span[2] = (byte)(value >> 48);
                span[3] = (byte)(value >> 40);
                span[4] = (byte)(value >> 32);
                span[5] = (byte)(value >> 24);
                span[6] = (byte)(value >> 16);
                span[7] = (byte)(value >> 8);
                span[8] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteBigEndian(Span<byte> span, float value) => WriteBigEndian(span, *(int*)&value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteBigEndian(Span<byte> span, double value) => WriteBigEndian(span, *(long*)&value);
    }
}

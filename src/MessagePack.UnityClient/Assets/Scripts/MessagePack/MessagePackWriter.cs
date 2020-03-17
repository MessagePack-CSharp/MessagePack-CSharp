// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using MessagePack.Internal;
using Microsoft;

namespace MessagePack
{
    public static class MessagePackBinary
    {
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
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        public static int WriteArrayHeader(Span<byte> span, int count) => WriteArrayHeader(span, (uint)count);

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
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
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public static int WriteMapHeader(Span<byte> span, int count) => WriteMapHeader(span, (uint)count);

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
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
        /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
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
        /// <param name="value">The value to write.</param>
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
        /// Writes a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
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
        /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public static int Write(Span<byte> span, char value) => Write(span, (ushort)value);

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
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
        /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The value to write.</param>
        /// <exception cref="NotSupportedException">Thrown when <see cref="OldSpec"/> is true because the old spec does not define a <see cref="DateTime"/> format.</exception>
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
        /// Writes the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="length">The length of bytes that will be written next.</param>
        /// <remarks>
        /// <para>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </para>
        /// <para>
        /// When <see cref="OldSpec"/> is <c>true</c>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </para>
        /// </remarks>
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

#if !LATER


        /// <summary>
        /// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
        /// <remarks>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="WriteString(ReadOnlySpan{byte})"/> or <see cref="WriteString(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </remarks>
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
        /// Writes the extension format header, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionHeader">The extension header.</param>
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

#endif

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

    /// <summary>
    /// A primitive types writer for the MessagePack format.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
    /// </remarks>
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    ref struct MessagePackWriter
    {
        /// <summary>
        /// The writer to use.
        /// </summary>
        private BufferWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
        /// </summary>
        /// <param name="writer">The writer to use.</param>
        public MessagePackWriter(IBufferWriter<byte> writer)
            : this()
        {
            this.writer = new BufferWriter(writer);
            this.OldSpec = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
        /// </summary>
        /// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
        /// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
        internal MessagePackWriter(SequencePool sequencePool, byte[] array)
            : this()
        {
            this.writer = new BufferWriter(sequencePool, array);
            this.OldSpec = false;
        }

        /// <summary>
        /// Gets or sets the cancellation token for this serialization operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to write in <see href="https://github.com/msgpack/msgpack/blob/master/spec-old.md">old spec</see> compatibility mode.
        /// </summary>
        public bool OldSpec { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct,
        /// with the same settings as this one, but with its own buffer writer.
        /// </summary>
        /// <param name="writer">The writer to use for the new instance.</param>
        /// <returns>The new writer.</returns>
        public MessagePackWriter Clone(IBufferWriter<byte> writer) => new MessagePackWriter(writer)
        {
            OldSpec = this.OldSpec,
            CancellationToken = this.CancellationToken,
        };

        /// <summary>
        /// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
        /// </summary>
        public void Flush() => this.writer.Commit();

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        public void WriteNil()
        {
            int result = MessagePackBinary.WriteNil(this.writer.Span);
            if (result < 0)
            {
                result = MessagePackBinary.WriteNil(this.writer.GetSpan(~result));
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySpan<byte> rawMessagePackBlock) => this.writer.Write(rawMessagePackBlock);

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(in ReadOnlySequence<byte> rawMessagePackBlock)
        {
            foreach (ReadOnlyMemory<byte> segment in rawMessagePackBlock)
            {
                this.writer.Write(segment.Span);
            }
        }

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        public void WriteArrayHeader(int count) => this.WriteArrayHeader((uint)count);

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(uint count)
        {
            int result = MessagePackBinary.WriteArrayHeader(this.writer.Span, count);
            if (result < 0)
            {
                result = MessagePackBinary.WriteArrayHeader(this.writer.GetSpan(~result), count);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(int count) => this.WriteMapHeader((uint)count);

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(uint count)
        {
            int result = MessagePackBinary.WriteMapHeader(this.writer.Span, count);
            if (result < 0)
            {
                result = MessagePackBinary.WriteMapHeader(this.writer.GetSpan(~result), count);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(byte value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt8(byte value)
        {
            int result = MessagePackBinary.WriteUInt8(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteUInt8(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(sbyte value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt8(sbyte value)
        {
            int result = MessagePackBinary.WriteInt8(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteInt8(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(ushort value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt16(ushort value)
        {
            int result = MessagePackBinary.WriteUInt16(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteUInt16(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(short value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt16(short value)
        {
            int result = MessagePackBinary.WriteInt16(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteInt16(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt32(uint value)
        {
            int result = MessagePackBinary.WriteUInt32(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteUInt32(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt32(int value)
        {
            int result = MessagePackBinary.WriteInt32(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteInt32(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ulong value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt64(ulong value)
        {
            int result = MessagePackBinary.WriteUInt64(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteUInt64(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
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
        /// <param name="value">The value to write.</param>
        public void Write(long value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt64(long value)
        {
            int result = MessagePackBinary.WriteInt64(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.WriteInt64(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(bool value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(char value) => this.Write((ushort)value);

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(float value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(double value)
        {
            int result = MessagePackBinary.Write(this.writer.Span, value);
            if (result < 0)
            {
                result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The value to write.</param>
        /// <exception cref="NotSupportedException">Thrown when <see cref="OldSpec"/> is true because the old spec does not define a <see cref="DateTime"/> format.</exception>
        public void Write(DateTime value)
        {
            if (this.OldSpec)
            {
                throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(this.OldSpec)} mode. Turn off {nameof(this.OldSpec)} mode or use NativeDateTimeFormatter.");
            }
            else
            {
                int result = MessagePackBinary.Write(this.writer.Span, value);
                if (result < 0)
                {
                    result = MessagePackBinary.Write(this.writer.GetSpan(~result), value);
                }

                this.writer.Advance(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="byte"/>[], prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>,
        /// <see cref="MessagePackCode.Bin32"/>,
        /// or <see cref="MessagePackCode.Nil"/> if <paramref name="src"/> is <c>null</c>.
        /// </summary>
        /// <param name="src">The array of bytes to write. May be <c>null</c>.</param>
        public void Write(byte[] src)
        {
            if (src == null)
            {
                this.WriteNil();
            }
            else
            {
                this.Write(src.AsSpan());
            }
        }

        /// <summary>
        /// Writes a span of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        /// <remarks>
        /// When <see cref="OldSpec"/> is <c>true</c>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </remarks>
        public void Write(ReadOnlySpan<byte> src)
        {
            if (this.OldSpec)
            {
                this.WriteStringHeader(src.Length);
                src.CopyTo(this.writer.GetSpan(src.Length));
                this.Advance(src.Length);
            }
            else
            {
                int result = MessagePackBinary.WriteBinHeader(this.writer.Span, src.Length);
                if (result < 0)
                {
                    // Did not write anything, so we need to allocate. It's better to allocate enough
                    // for data as well.
                    int size = src.Length + ~result;
                    var buffer = this.writer.GetSpan(size);
                    src.CopyTo(buffer.Slice(MessagePackBinary.WriteBinHeader(buffer, src.Length)));
                    this.writer.Advance(size);
                }
                else
                {
                    this.writer.Advance(result);
                    src.CopyTo(this.writer.GetSpan(src.Length));
                    this.writer.Advance(src.Length);
                }
            }
        }

        /// <summary>
        /// Writes a sequence of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        /// <remarks>
        /// When <see cref="OldSpec"/> is <c>true</c>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </remarks>
        public void Write(in ReadOnlySequence<byte> src)
        {
            if (this.OldSpec)
            {
                this.WriteStringHeader((int)src.Length);
                src.CopyTo(this.writer.GetSpan((int)src.Length));
                this.Advance((int)src.Length);
            }
            else
            {
                int result = MessagePackBinary.WriteBinHeader(this.writer.Span, (int)src.Length);
                if (result < 0)
                {
                    // Did not write anything, so we need to allocate. It's better to allocate enough
                    // for data as well.
                    int size = (int)src.Length + ~result;
                    var buffer = this.writer.GetSpan(size);
                    src.CopyTo(buffer.Slice(MessagePackBinary.WriteBinHeader(buffer, (int)src.Length)));
                    this.writer.Advance(size);
                }
                else
                {
                    this.writer.Advance(result);
                    src.CopyTo(this.writer.GetSpan((int)src.Length));
                    this.writer.Advance((int)src.Length);
                }
            }
        }

        /// <summary>
        /// Writes the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="length">The length of bytes that will be written next.</param>
        /// <remarks>
        /// <para>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </para>
        /// <para>
        /// When <see cref="OldSpec"/> is <c>true</c>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </para>
        /// </remarks>
        public void WriteBinHeader(int length)
        {
            if (this.OldSpec)
            {
                this.WriteStringHeader(length);
            }
            else
            {
                int result = MessagePackBinary.WriteBinHeader(this.writer.Span, length);
                if (result < 0)
                {
                    result = MessagePackBinary.WriteBinHeader(this.writer.GetSpan(~result), length);
                }

                this.writer.Advance(result);
            }
        }

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(in ReadOnlySequence<byte> utf8stringBytes)
        {
            var length = (int)utf8stringBytes.Length;
            int result = MessagePackBinary.WriteStringHeader(this.writer.Span, length, this.OldSpec);
            if (result < 0)
            {
                // Did not write anything, so we need to allocate. It's better to allocate enough
                // for data as well.
                int size = length + ~result;
                var buffer = this.writer.GetSpan(size);
                utf8stringBytes.CopyTo(buffer.Slice(MessagePackBinary.WriteStringHeader(buffer, length, this.OldSpec)));
                this.writer.Advance(size);
            }
            else
            {
                this.writer.Advance(result);
                utf8stringBytes.CopyTo(this.writer.GetSpan(length));
                this.writer.Advance(length);
            }
        }

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(ReadOnlySpan<byte> utf8stringBytes)
        {
            var length = utf8stringBytes.Length;
            int result = MessagePackBinary.WriteStringHeader(this.writer.Span, length, this.OldSpec);
            if (result < 0)
            {
                // Did not write anything, so we need to allocate. It's better to allocate enough
                // for data as well.
                int size = length + ~result;
                var buffer = this.writer.GetSpan(size);
                utf8stringBytes.CopyTo(buffer.Slice(MessagePackBinary.WriteStringHeader(buffer, length, this.OldSpec)));
                this.writer.Advance(size);
            }
            else
            {
                this.writer.Advance(result);
                utf8stringBytes.CopyTo(this.writer.GetSpan(length));
                this.writer.Advance(length);
            }
        }

        /// <summary>
        /// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
        /// <remarks>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="WriteString(ReadOnlySpan{byte})"/> or <see cref="WriteString(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </remarks>
        public void WriteStringHeader(int byteCount)
        {
            int result = MessagePackBinary.WriteStringHeader(this.writer.Span, byteCount, this.OldSpec);
            if (result < 0)
            {
                result = MessagePackBinary.WriteStringHeader(this.writer.GetSpan(~result), byteCount, this.OldSpec);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or <see cref="MessagePackCode.Nil"/> if the <paramref name="value"/> is <c>null</c>.
        /// </summary>
        /// <param name="value">The value to write. May be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(string value)
        {
            if (value == null)
            {
                this.WriteNil();
                return;
            }

            ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
            fixed (char* pValue = value)
            fixed (byte* pBuffer = &buffer)
            {
                int byteCount = StringEncoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
                this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
            }
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public unsafe void Write(ReadOnlySpan<char> value)
        {
            ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
            fixed (char* pValue = value)
            fixed (byte* pBuffer = &buffer)
            {
                int byteCount = StringEncoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
                this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
            }
        }

        /// <summary>
        /// Writes the extension format header, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionHeader">The extension header.</param>
        public void WriteExtensionFormatHeader(ExtensionHeader extensionHeader)
        {
            int result = MessagePackBinary.WriteExtensionFormatHeader(this.writer.Span, extensionHeader);
            if (result < 0)
            {
                result = MessagePackBinary.WriteExtensionFormatHeader(this.writer.GetSpan(~result), extensionHeader);
            }

            this.writer.Advance(result);
        }

        /// <summary>
        /// Writes an extension format, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionData">The extension data.</param>
        public void WriteExtensionFormat(ExtensionResult extensionData)
        {
            var length = (int)extensionData.Data.Length;
            int result = MessagePackBinary.WriteExtensionFormatHeader(this.writer.Span, extensionData.Header);
            if (result < 0)
            {
                // Did not write anything, so we need to allocate. It's better to allocate enough
                // for data as well.
                int size = length + ~result;
                var buffer = this.writer.GetSpan(size);
                extensionData.Data.CopyTo(buffer.Slice(MessagePackBinary.WriteExtensionFormatHeader(buffer, extensionData.Header)));
                this.writer.Advance(size);
            }
            else
            {
                this.writer.Advance(result);
                extensionData.Data.CopyTo(this.writer.GetSpan(length));
                this.writer.Advance(length);
            }
        }

        /// <summary>
        /// Gets memory where raw messagepack data can be written.
        /// </summary>
        /// <param name="length">The size of the memory block required.</param>
        /// <returns>The span of memory to write to. This *may* exceed <paramref name="length"/>.</returns>
        /// <remarks>
        /// <para>After initializing the resulting memory, always follow up with a call to <see cref="Advance(int)"/>.</para>
        /// <para>
        /// This is similar in purpose to <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// but provides uninitialized memory for the caller to write to instead of copying initialized memory from elsewhere.
        /// </para>
        /// </remarks>
        /// <seealso cref="IBufferWriter{T}.GetSpan(int)"/>
        public Span<byte> GetSpan(int length) => this.writer.GetSpan(length);

        /// <summary>
        /// Commits memory previously returned from <see cref="GetSpan(int)"/> as initialized.
        /// </summary>
        /// <param name="length">The number of bytes initialized with messagepack data from the previously returned span.</param>
        /// <seealso cref="IBufferWriter{T}.Advance(int)"/>
        public void Advance(int length) => this.writer.Advance(length);

        internal byte[] FlushAndGetArray()
        {
            if (this.writer.TryGetUncommittedSpan(out ReadOnlySpan<byte> span))
            {
                return span.ToArray();
            }
            else
            {
                if (this.writer.SequenceRental.Value == null)
                {
                    throw new NotSupportedException("This instance was not initialized to support this operation.");
                }

                this.Flush();
                byte[] result = this.writer.SequenceRental.Value.AsReadOnlySequence.ToArray();
                this.writer.SequenceRental.Dispose();
                return result;
            }
        }

        private static unsafe void WriteBigEndian(ushort value, byte* span)
        {
            unchecked
            {
                span[0] = (byte)(value >> 8);
                span[1] = (byte)value;
            }
        }

        private static unsafe void WriteBigEndian(uint value, byte* span)
        {
            unchecked
            {
                span[0] = (byte)(value >> 24);
                span[1] = (byte)(value >> 16);
                span[2] = (byte)(value >> 8);
                span[3] = (byte)value;
            }
        }

        /// <summary>
        /// Estimates the length of the header required for a given string.
        /// </summary>
        /// <param name="characterLength">The length of the string to be written, in characters.</param>
        /// <param name="bufferSize">Receives the guaranteed length of the returned buffer.</param>
        /// <param name="encodedBytesOffset">Receives the offset within the returned buffer to write the encoded string to.</param>
        /// <returns>
        /// A reference to the first byte in the buffer.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte WriteString_PrepareSpan(int characterLength, out int bufferSize, out int encodedBytesOffset)
        {
            // MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
            // solves heuristic length check

            // ensure buffer by MaxByteCount(faster than GetByteCount)
            bufferSize = StringEncoding.UTF8.GetMaxByteCount(characterLength) + 5;
            ref byte buffer = ref this.writer.GetPointer(bufferSize);

            int useOffset;
            if (characterLength <= MessagePackRange.MaxFixStringLength)
            {
                useOffset = 1;
            }
            else if (characterLength <= byte.MaxValue && !this.OldSpec)
            {
                useOffset = 2;
            }
            else if (characterLength <= ushort.MaxValue)
            {
                useOffset = 3;
            }
            else
            {
                useOffset = 5;
            }

            encodedBytesOffset = useOffset;
            return ref buffer;
        }

        /// <summary>
        /// Finalizes an encoding of a string.
        /// </summary>
        /// <param name="pBuffer">A pointer obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
        /// <param name="estimatedOffset">The offset obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
        /// <param name="byteCount">The number of bytes used to actually encode the string.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteString_PostEncoding(byte* pBuffer, int estimatedOffset, int byteCount)
        {
            int bufferLength = estimatedOffset + byteCount;

            // move body and write prefix
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (estimatedOffset != 1)
                {
                    MemoryCopy(pBuffer + estimatedOffset, pBuffer + 1, byteCount, byteCount);
                }

                pBuffer[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                this.writer.Advance(byteCount + 1);
            }
            else if (byteCount <= byte.MaxValue && !this.OldSpec)
            {
                if (estimatedOffset != 2)
                {
                    MemoryCopy(pBuffer + estimatedOffset, pBuffer + 2, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str8;
                pBuffer[1] = unchecked((byte)byteCount);
                this.writer.Advance(byteCount + 2);
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (estimatedOffset != 3)
                {
                    MemoryCopy(pBuffer + estimatedOffset, pBuffer + 3, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str16;
                WriteBigEndian((ushort)byteCount, pBuffer + 1);
                this.writer.Advance(byteCount + 3);
            }
            else
            {
                if (estimatedOffset != 5)
                {
                    MemoryCopy(pBuffer + estimatedOffset, pBuffer + 5, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str32;
                WriteBigEndian((uint)byteCount, pBuffer + 1);
                this.writer.Advance(byteCount + 5);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy)
        {
            if (Utilities.IsMono)
            {
                // mono does not guarantee overlapped memcpy so for Unity and NETSTANDARD use slow path.
                // https://github.com/neuecc/MessagePack-CSharp/issues/562
                var buffer = ArrayPool<byte>.Shared.Rent((int)sourceBytesToCopy);
                try
                {
                    fixed (byte* p = buffer)
                    {
                        Buffer.MemoryCopy(source, p, sourceBytesToCopy, sourceBytesToCopy);
                        Buffer.MemoryCopy(p, destination, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                Buffer.MemoryCopy(source, destination, destinationSizeInBytes, sourceBytesToCopy);
            }
        }
    }
}

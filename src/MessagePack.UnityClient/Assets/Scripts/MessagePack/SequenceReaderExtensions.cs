// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information. */

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack
{
    internal static partial class SequenceReaderExtensions
    {
        /// <summary>
        /// Try to read the given type out of the buffer if possible. Warning: this is dangerous to use with arbitrary
        /// structs- see remarks for full details.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: The read is a straight copy of bits. If a struct depends on specific state of its members to
        /// behave correctly this can lead to exceptions, etc. If reading endian specific integers, use the explicit
        /// overloads such as <see cref="TryReadBigEndian(ref SequenceReader{byte}, out short)"/>.
        /// </remarks>
        /// <returns>
        /// True if successful. <paramref name="value"/> will be default if failed (due to lack of space).
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryRead<T>(ref this SequenceReader<byte> reader, out T value)
            where T : unmanaged
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < sizeof(T))
            {
                return TryReadMultisegment(ref reader, out value);
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            reader.Advance(sizeof(T));
            return true;
        }

#if UNITY_ANDROID

        /// <summary>
        /// In Android 32bit device(armv7) + IL2CPP does not work correctly on Unsafe.ReadUnaligned.
        /// Perhaps it is about memory alignment bug of Unity's IL2CPP VM.
        /// For a workaround, read memory manually.
        /// https://github.com/neuecc/MessagePack-CSharp/issues/748
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryRead(ref this SequenceReader<byte> reader, out long value)
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < sizeof(long))
            {
                return TryReadMultisegment(ref reader, out value);
            }

            value = MessagePack.SafeBitConverter.ToInt64(span);
            reader.Advance(sizeof(long));
            return true;
        }

#endif

        private static unsafe bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value)
            where T : unmanaged
        {
            Debug.Assert(reader.UnreadSpan.Length < sizeof(T), "reader.UnreadSpan.Length < sizeof(T)");

            // Not enough data in the current segment, try to peek for the data we need.
            T buffer = default;
            Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(T));

            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
            reader.Advance(sizeof(T));
            return true;
        }

#if UNITY_ANDROID

        private static unsafe bool TryReadMultisegment(ref SequenceReader<byte> reader, out long value)
        {
            Debug.Assert(reader.UnreadSpan.Length < sizeof(long), "reader.UnreadSpan.Length < sizeof(long)");

            // Not enough data in the current segment, try to peek for the data we need.
            long buffer = default;
            Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(long));

            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }

            value = MessagePack.SafeBitConverter.ToInt64(tempSpan);
            reader.Advance(sizeof(long));
            return true;
        }

#endif

        /// <summary>
        /// Reads an <see cref="sbyte"/> from the next position in the sequence.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="value">Receives the value read.</param>
        /// <returns><see langword="true"/> if there was another byte in the sequence; <see langword="false"/> otherwise.</returns>
        public static bool TryRead(ref this SequenceReader<byte> reader, out sbyte value)
        {
            if (TryRead(ref reader, out byte byteValue))
            {
                value = unchecked((sbyte)byteValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads an <see cref="Int16"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="Int16"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out short value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return reader.TryRead(out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        /// <summary>
        /// Reads an <see cref="UInt16"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt16"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ushort value)
        {
            if (TryReadBigEndian(ref reader, out short shortValue))
            {
                value = unchecked((ushort)shortValue);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out short value)
        {
            if (reader.TryRead(out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads an <see cref="Int32"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="Int32"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out int value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return reader.TryRead(out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        /// <summary>
        /// Reads an <see cref="UInt32"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt32"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out uint value)
        {
            if (TryReadBigEndian(ref reader, out int intValue))
            {
                value = unchecked((uint)intValue);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out int value)
        {
            if (reader.TryRead(out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads an <see cref="Int64"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="Int64"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out long value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return reader.TryRead(out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        /// <summary>
        /// Reads an <see cref="UInt64"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ulong value)
        {
            if (TryReadBigEndian(ref reader, out long longValue))
            {
                value = unchecked((ulong)longValue);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out long value)
        {
            if (reader.TryRead(out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a <see cref="Single"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for a <see cref="Single"/>.</returns>
        public static unsafe bool TryReadBigEndian(ref this SequenceReader<byte> reader, out float value)
        {
            if (TryReadBigEndian(ref reader, out int intValue))
            {
                value = *(float*)&intValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads a <see cref="Double"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for a <see cref="Double"/>.</returns>
        public static unsafe bool TryReadBigEndian(ref this SequenceReader<byte> reader, out double value)
        {
            if (TryReadBigEndian(ref reader, out long longValue))
            {
                value = *(double*)&longValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}

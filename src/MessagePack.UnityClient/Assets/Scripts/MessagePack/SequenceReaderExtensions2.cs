// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack
{
    internal static partial class SequenceReaderExtensions2
    {
        /// <summary>
        /// Reads an <see cref="sbyte"/> from the next position in the sequence.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="value">Receives the value read.</param>
        /// <returns><c>true</c> if there was another byte in the sequence; <c>false</c> otherwise.</returns>
        public static bool TryRead(ref this SequenceReader<byte> reader, out sbyte value)
        {
            if (SequenceReaderExtensions.TryRead(ref reader, out byte byteValue))
            {
                value = unchecked((sbyte)byteValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads an <see cref="UInt16"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt16"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ushort value)
        {
            if (SequenceReaderExtensions.TryReadBigEndian(ref reader, out short shortValue))
            {
                value = unchecked((ushort)shortValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads an <see cref="UInt32"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt32"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out uint value)
        {
            if (SequenceReaderExtensions.TryReadBigEndian(ref reader, out int intValue))
            {
                value = unchecked((uint)intValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads an <see cref="UInt64"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ulong value)
        {
            if (SequenceReaderExtensions.TryReadBigEndian(ref reader, out long longValue))
            {
                value = unchecked((ulong)longValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Reads a <see cref="Single"/> as big endian.
        /// </summary>
        /// <returns>False if there wasn't enough data for a <see cref="Single"/>.</returns>
        public static unsafe bool TryReadBigEndian(ref this SequenceReader<byte> reader, out float value)
        {
            if (SequenceReaderExtensions.TryReadBigEndian(ref reader, out int intValue))
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
            if (SequenceReaderExtensions.TryReadBigEndian(ref reader, out long longValue))
            {
                value = *(double*)&longValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}

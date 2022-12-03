// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.ComponentModel;

namespace MessagePack.Internal
{
    /// <summary>
    /// Helpers for generated code.
    /// </summary>
    /// <remarks>
    /// This code is used by dynamically generated code as well as AOT generated code,
    /// and thus must be public for the "C# generated and compiled into saved assembly" scenario.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CodeGenHelpers
    {
        /// <summary>
        /// Gets the messagepack encoding for a given string.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>The messagepack encoding for <paramref name="value"/>, including messagepack header and UTF-8 bytes.</returns>
        public static byte[] GetEncodedStringBytes(string value)
        {
            var byteCount = StringEncoding.UTF8.GetByteCount(value);
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                var bytes = new byte[byteCount + 1];
                bytes[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 1);
                return bytes;
            }
            else if (byteCount <= byte.MaxValue)
            {
                var bytes = new byte[byteCount + 2];
                bytes[0] = MessagePackCode.Str8;
                bytes[1] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 2);
                return bytes;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                var bytes = new byte[byteCount + 3];
                bytes[0] = MessagePackCode.Str16;
                bytes[1] = unchecked((byte)(byteCount >> 8));
                bytes[2] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 3);
                return bytes;
            }
            else
            {
                var bytes = new byte[byteCount + 5];
                bytes[0] = MessagePackCode.Str32;
                bytes[1] = unchecked((byte)(byteCount >> 24));
                bytes[2] = unchecked((byte)(byteCount >> 16));
                bytes[3] = unchecked((byte)(byteCount >> 8));
                bytes[4] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 5);
                return bytes;
            }
        }

        /// <summary>
        /// Gets a single <see cref="ReadOnlySpan{T}"/> containing all bytes in a given <see cref="ReadOnlySequence{T}"/>.
        /// An array may be allocated if the bytes are not already contiguous in memory.
        /// </summary>
        /// <param name="sequence">The sequence to get a span for.</param>
        /// <returns>The span.</returns>
        public static ReadOnlySpan<byte> GetSpanFromSequence(in ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsSingleSegment)
            {
                return sequence.First.Span;
            }

            return sequence.ToArray();
        }

        /// <summary>
        /// Reads a string as a contiguous span of UTF-8 encoded characters.
        /// An array may be allocated if the string is not already contiguous in memory.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <returns>The span of UTF-8 encoded characters.</returns>
        public static ReadOnlySpan<byte> ReadStringSpan(ref MessagePackReader reader)
        {
            if (!reader.TryReadStringSpan(out ReadOnlySpan<byte> result))
            {
                return GetSpanFromSequence(reader.ReadStringSequence());
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="byte"/> array for a given sequence, or <see langword="null" /> if the optional sequence is itself <see langword="null" />.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns>The byte array or <see langword="null" /> .</returns>
        public static byte[]? GetArrayFromNullableSequence(in ReadOnlySequence<byte>? sequence) => sequence?.ToArray();

        private static ReadOnlySpan<byte> GetSpanFromSequence(in ReadOnlySequence<byte>? sequence)
        {
            return sequence.HasValue ? GetSpanFromSequence(sequence.Value) : default;
        }
    }
}

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

        public static ReadOnlySpan<byte> GetSpanFromSequence(in ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsSingleSegment)
            {
                return sequence.First.Span;
            }

            return sequence.ToArray();
        }

        public static ReadOnlySpan<byte> ReadStringSpan(ref MessagePackReader reader)
        {
            if (!reader.TryReadStringSpan(out ReadOnlySpan<byte> result))
            {
                return GetSpanFromSequence(reader.ReadStringSequence());
            }

            return result;
        }

        public static byte[] GetArrayFromNullableSequence(in ReadOnlySequence<byte>? sequence) => sequence?.ToArray();

        private static ReadOnlySpan<byte> GetSpanFromSequence(in ReadOnlySequence<byte>? sequence)
        {
            return sequence.HasValue ? GetSpanFromSequence(sequence.Value) : default;
        }
    }
}

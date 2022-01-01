// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Cysharp.Text;
using StringLiteral;

namespace MessagePackCompiler.Generator
{
    public static partial class EmbedStringHelper
    {
        public static readonly Encoding Utf8 = new UTF8Encoding(false);

        [Utf8("new byte[")]
        private static partial ReadOnlySpan<byte> GetUtf8New();

        [Utf8(" + ")]
        private static partial ReadOnlySpan<byte> GetUtf8Add();

        [Utf8("] { ")]
        private static partial ReadOnlySpan<byte> GetUtf8Open();

        [Utf8(", ")]
        private static partial ReadOnlySpan<byte> GetUtf8Comma();

        [Utf8(" }")]
        private static partial ReadOnlySpan<byte> GetUtf8Close();

        private static void AppendLiteral(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
        {
            var dest = builder.GetSpan(span.Length);
            span.CopyTo(dest);
            builder.Advance(span.Length);
        }

        public static void ToByteArrayString(ref Utf8ValueStringBuilder builder, byte[] binary)
        {
            var headerLength = GetHeaderLength(binary.Length);
            Span<byte> header = stackalloc byte[headerLength];
            EmbedHeader(binary.Length, header);
            builder.AppendLiteral(GetUtf8New());
            builder.Append(headerLength);
            builder.AppendLiteral(GetUtf8Add());
            builder.Append(binary.Length);
            builder.AppendLiteral(GetUtf8Open());
            builder.Append(header[0]);
            foreach (var b in header.Slice(1))
            {
                builder.AppendLiteral(GetUtf8Comma());
                builder.Append(b);
            }

            foreach (var b in binary)
            {
                builder.AppendLiteral(GetUtf8Comma());
                builder.Append(b);
            }

            builder.AppendLiteral(GetUtf8Close());
        }

        public static int GetHeaderLength(int byteCount)
        {
            if (byteCount <= 31)
            {
                return 1;
            }

            if (byteCount <= byte.MaxValue)
            {
                return 2;
            }

            return byteCount <= ushort.MaxValue ? 3 : 5;
        }

        public static void EmbedHeader(int byteCount, Span<byte> destination)
        {
            if (byteCount <= 31)
            {
                destination[0] = (byte)(0xa0 | byteCount);
                return;
            }

            if (byteCount <= byte.MaxValue)
            {
                destination[0] = 0xd9;
                destination[1] = unchecked((byte)byteCount);
                return;
            }

            if (byteCount <= ushort.MaxValue)
            {
                destination[0] = 0xda;
                destination[1] = unchecked((byte)(byteCount >> 8));
                destination[2] = unchecked((byte)byteCount);
                return;
            }

            destination[0] = 0xdb;
            destination[1] = unchecked((byte)(byteCount >> 24));
            destination[2] = unchecked((byte)(byteCount >> 16));
            destination[3] = unchecked((byte)(byteCount >> 8));
            destination[4] = unchecked((byte)byteCount);
        }

        public static byte[] GetEncodedStringBytes(string value)
        {
            var byteCount = Utf8.GetByteCount(value);
            var headerLength = GetHeaderLength(byteCount);
            var bytes = new byte[headerLength + byteCount];
            EmbedHeader(byteCount, bytes);
            Utf8.GetBytes(value, 0, value.Length, bytes, headerLength);
            return bytes;
        }
    }
}

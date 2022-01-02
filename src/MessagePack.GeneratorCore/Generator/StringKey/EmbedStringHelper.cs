// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Cysharp.Text;
using StringLiteral;

namespace MessagePackCompiler.Generator;

public static partial class EmbedStringHelper
{
    public static readonly Encoding Utf8 = new UTF8Encoding(false);

    [Utf8("new byte[")]
    private static partial ReadOnlySpan<byte> GetUtf8New();

    [Utf8(" + ")]
    private static partial ReadOnlySpan<byte> GetUtf8Add();

    [Utf8("] { 0x")]
    private static partial ReadOnlySpan<byte> GetUtf8Open0x();

    [Utf8("] { 0xd9, 0x")]
    private static partial ReadOnlySpan<byte> GetUtf8Open0xd9();

    [Utf8("] { 0xda, 0x")]
    private static partial ReadOnlySpan<byte> GetUtf8Open0xda();

    [Utf8("] { 0xdb, 0x")]
    private static partial ReadOnlySpan<byte> GetUtf8Open0xdb();

    [Utf8(", 0x")]
    private static partial ReadOnlySpan<byte> GetUtf8Comma0x();

    [Utf8(" }")]
    private static partial ReadOnlySpan<byte> GetUtf8Close();

    private static void AppendLiteral(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
    {
        span.CopyTo(builder.GetSpan(span.Length));
        builder.Advance(span.Length);
    }

    private static void AppendLiteral(ref this Utf8ValueStringBuilder builder, byte value)
    {
        var span = builder.GetSpan(2);
        span[0] = ToAscii((byte)(value >> 4));
        span[1] = ToAscii((byte)(value & 0b1111));
        builder.Advance(2);
    }

    public static unsafe void ToByteArrayString(ref Utf8ValueStringBuilder builder, byte[] binary)
    {
        var headerLength = GetHeaderLength(binary.Length);
        builder.AppendLiteral(GetUtf8New());
        builder.Append(headerLength);
        builder.AppendLiteral(GetUtf8Add());
        builder.Append(binary.Length);

        switch (binary.Length)
        {
            case < 32:
                builder.AppendLiteral(GetUtf8Open0x());
                builder.AppendLiteral(unchecked((byte)(binary.Length | 0xa0)));
                break;
            case <= byte.MaxValue:
                builder.AppendLiteral(GetUtf8Open0xd9());
                builder.AppendLiteral(unchecked((byte)binary.Length));
                break;
            case <= ushort.MaxValue:
                builder.AppendLiteral(GetUtf8Open0xda());
                builder.AppendLiteral(unchecked((byte)(binary.Length >> 8)));
                builder.AppendLiteral(unchecked((byte)binary.Length));
                break;
            default:
                builder.AppendLiteral(GetUtf8Open0xdb());
                builder.AppendLiteral(unchecked((byte)(binary.Length >> 24)));
                builder.AppendLiteral(unchecked((byte)(binary.Length >> 16)));
                builder.AppendLiteral(unchecked((byte)(binary.Length >> 8)));
                builder.AppendLiteral(unchecked((byte)binary.Length));
                break;
        }

        var span = builder.GetSpan(binary.Length * 6);
        fixed (byte* commaPtr = GetUtf8Comma0x())
        fixed (byte* dest = span)
        fixed (byte* src = &binary[0])
        {
            var comma = *(uint*)commaPtr;
            var destItr = dest;
            var srcEnd = src + binary.Length;
            var srcItr = src;
            for (; srcItr != srcEnd; ++srcItr)
            {
                *(uint*)destItr = comma;
                destItr += sizeof(uint);
                var value = *srcItr;
                *(destItr++) = ToAscii((byte)(value >> 4));
                *(destItr++) = ToAscii((byte)(value & 0b1111));
            }
        }

        builder.Advance(binary.Length * 6);
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

    private static byte ToAscii(byte lower4Bit)
    {
        if (lower4Bit < 10)
        {
            return (byte)('0' + lower4Bit);
        }
        else
        {
            return (byte)('A' + lower4Bit - 10);
        }
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
}

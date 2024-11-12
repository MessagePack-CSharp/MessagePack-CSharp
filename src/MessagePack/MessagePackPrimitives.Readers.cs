﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack.Internal;

namespace MessagePack;

#pragma warning disable SA1205 // Partial elements should declare access

partial class MessagePackPrimitives
{
    /// <summary>
    /// Enumerates the possible outcomes of a read operation.
    /// </summary>
    public enum DecodeResult
    {
        /// <summary>
        /// The token was successfully read from the buffer.
        /// </summary>
        Success,

        /// <summary>
        /// The token read from the buffer did not match the expected token.
        /// </summary>
        TokenMismatch,

        /// <summary>
        /// The buffer is empty and no token could be read.
        /// </summary>
        EmptyBuffer,

        /// <summary>
        /// The token is of the expected type, but the buffer does not include all the bytes needed to read the value.
        /// </summary>
        InsufficientBuffer,
    }

    static partial class Decoders
    {
        internal static readonly IReadInt64[] Int64JumpTable;
        internal static readonly IReadUInt64[] UInt64JumpTable;

        static Decoders()
        {
            Int64JumpTable = new IReadInt64[256];
            Int64JumpTable.AsSpan().Fill(ReadInt64Invalid.Instance);
            Int64JumpTable[MessagePackCode.UInt8] = ReadInt64UInt8.Instance;
            Int64JumpTable[MessagePackCode.UInt16] = ReadInt64UInt16.Instance;
            Int64JumpTable[MessagePackCode.UInt32] = ReadInt64UInt32.Instance;
            Int64JumpTable[MessagePackCode.UInt64] = ReadInt64UInt64.Instance;
            Int64JumpTable[MessagePackCode.Int8] = ReadInt64Int8.Instance;
            Int64JumpTable[MessagePackCode.Int16] = ReadInt64Int16.Instance;
            Int64JumpTable[MessagePackCode.Int32] = ReadInt64Int32.Instance;
            Int64JumpTable[MessagePackCode.Int64] = ReadInt64Int64.Instance;
            for (int i = MessagePackCode.MinNegativeFixInt; i <= MessagePackCode.MaxNegativeFixInt; i++)
            {
                Int64JumpTable[i] = ReadInt64NegativeFixInt.Instance;
            }

            for (int i = MessagePackCode.MinFixInt; i <= MessagePackCode.MaxFixInt; i++)
            {
                Int64JumpTable[i] = ReadInt64FixInt.Instance;
            }

            UInt64JumpTable = new IReadUInt64[256];
            UInt64JumpTable.AsSpan().Fill(ReadUInt64Invalid.Instance);
            UInt64JumpTable[MessagePackCode.UInt8] = ReadUInt64UInt8.Instance;
            UInt64JumpTable[MessagePackCode.UInt16] = ReadUInt64UInt16.Instance;
            UInt64JumpTable[MessagePackCode.UInt32] = ReadUInt64UInt32.Instance;
            UInt64JumpTable[MessagePackCode.UInt64] = ReadUInt64UInt64.Instance;
            UInt64JumpTable[MessagePackCode.Int8] = ReadUInt64Int8.Instance;
            UInt64JumpTable[MessagePackCode.Int16] = ReadUInt64Int16.Instance;
            UInt64JumpTable[MessagePackCode.Int32] = ReadUInt64Int32.Instance;
            UInt64JumpTable[MessagePackCode.Int64] = ReadUInt64Int64.Instance;
            for (int i = MessagePackCode.MinNegativeFixInt; i <= MessagePackCode.MaxNegativeFixInt; i++)
            {
                UInt64JumpTable[i] = ReadUInt64NegativeFixInt.Instance;
            }

            for (int i = MessagePackCode.MinFixInt; i <= MessagePackCode.MaxFixInt; i++)
            {
                UInt64JumpTable[i] = ReadUInt64FixInt.Instance;
            }
        }

        internal interface IReadInt64
        {
            DecodeResult Read(ReadOnlySpan<byte> source, out long value, out int tokenSize);
        }

        internal interface IReadUInt64
        {
            DecodeResult Read(ReadOnlySpan<byte> source, out ulong value, out int tokenSize);
        }
    }

    /// <summary>
    /// Tries to read a nil value from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads a <see cref="MessagePackCode.Nil"/> value from the buffer.
    /// </remarks>
    public static DecodeResult TryReadNil(ReadOnlySpan<byte> source, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            return DecodeResult.EmptyBuffer;
        }

        return source[0] switch
        {
            MessagePackCode.Nil => DecodeResult.Success,
            _ => DecodeResult.TokenMismatch,
        };
    }

    /// <summary>
    /// Tries to read the header of an array from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="count">Receives the number of elements in the array, if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads an array header from
    /// <see cref="MessagePackCode.Array16"/>,
    /// <see cref="MessagePackCode.Array32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>.
    /// </remarks>
    public static DecodeResult TryReadArrayHeader(ReadOnlySpan<byte> source, out uint count, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            count = 0;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case >= MessagePackCode.MinFixArray and <= MessagePackCode.MaxFixArray:
                count = (byte)(source[0] & 0xF);
                return DecodeResult.Success;

            case MessagePackCode.Array16:
                tokenSize = 3;
                if (TryReadBigEndian(source.Slice(1), out ushort ushortValue))
                {
                    count = ushortValue;
                    return DecodeResult.Success;
                }
                else
                {
                    count = 0;
                    return DecodeResult.InsufficientBuffer;
                }

            case MessagePackCode.Array32:
                tokenSize = 5;
                if (TryReadBigEndian(source.Slice(1), out uint uintValue))
                {
                    count = uintValue;
                    return DecodeResult.Success;
                }
                else
                {
                    count = 0;
                    return DecodeResult.InsufficientBuffer;
                }

            default:
                count = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read the header of a map from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="count">Receives the number of key-value pairs in the map, if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads a map header from
    /// <see cref="MessagePackCode.Map16"/>,
    /// <see cref="MessagePackCode.Map32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
    /// </remarks>
    public static DecodeResult TryReadMapHeader(ReadOnlySpan<byte> source, out uint count, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            count = 0;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case >= MessagePackCode.MinFixMap and <= MessagePackCode.MaxFixMap:
                count = (byte)(source[0] & 0xF);
                return DecodeResult.Success;
            case MessagePackCode.Map16:
                tokenSize = 3;
                if (TryReadBigEndian(source.Slice(1), out ushort ushortValue))
                {
                    count = ushortValue;
                    return DecodeResult.Success;
                }
                else
                {
                    count = 0;
                    return DecodeResult.InsufficientBuffer;
                }

            case MessagePackCode.Map32:
                tokenSize = 5;
                if (TryReadBigEndian(source.Slice(1), out uint uintValue))
                {
                    count = uintValue;
                    return DecodeResult.Success;
                }
                else
                {
                    count = 0;
                    return DecodeResult.InsufficientBuffer;
                }

            default:
                count = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read a boolean value from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="value">Receives the boolean value if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads a <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/> value from the buffer.
    /// </remarks>
    public static DecodeResult TryReadBool(ReadOnlySpan<byte> source, out bool value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length == 0)
        {
            value = default;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.True:
                value = true;
                return DecodeResult.Success;
            case MessagePackCode.False:
                value = false;
                return DecodeResult.Success;
            default:
                value = false;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read a singly encoded character from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="value">Receives the character if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads a ushort value using <see cref="TryReadBigEndian(ReadOnlySpan{byte}, out ushort)"/> from the buffer and interprets it as a <see langword="char" />.
    /// </remarks>
    public static DecodeResult TryReadChar(ReadOnlySpan<byte> source, out char value, out int tokenSize)
    {
        DecodeResult result = TryReadUInt16(source, out ushort ordinal, out tokenSize);
        if (result == DecodeResult.Success)
        {
            value = (char)ordinal;
        }
        else
        {
            value = default;
        }

        return result;
    }

    /// <summary>
    /// Tries to read a <see cref="DateTime"/> from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="value">Receives the timestamp if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads the extension header using <see cref="TryReadExtensionHeader(ReadOnlySpan{byte}, out ExtensionHeader, out int)"/>
    /// then the extension itself using <see cref="TryReadDateTime(ReadOnlySpan{byte}, ExtensionHeader, out DateTime, out int)"/>.
    /// </remarks>
    public static DecodeResult TryReadDateTime(ReadOnlySpan<byte> source, out DateTime value, out int tokenSize)
    {
        DecodeResult result = TryReadExtensionHeader(source, out ExtensionHeader header, out tokenSize);
        if (result != DecodeResult.Success)
        {
            value = default;
            return result;
        }

        result = TryReadDateTime(source.Slice(tokenSize), header, out value, out int extensionSize);
        tokenSize += extensionSize;
        return result;
    }

    /// <summary>
    /// Tries to read a <see cref="DateTime"/> from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="header">The extension header that introduces the timestamp. This extension is expected to carry a <see cref="ExtensionHeader.TypeCode"/> value of <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.</param>
    /// <param name="value">Receives the timestamp if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    public static DecodeResult TryReadDateTime(ReadOnlySpan<byte> source, ExtensionHeader header, out DateTime value, out int tokenSize)
    {
        tokenSize = checked((int)header.Length);
        if (header.TypeCode != ReservedMessagePackExtensionTypeCode.DateTime)
        {
            value = default;
            return DecodeResult.TokenMismatch;
        }

        if (source.Length < tokenSize)
        {
            value = default;
            return DecodeResult.InsufficientBuffer;
        }

        switch (header.Length)
        {
            case 4:
                AssumesTrue(TryReadBigEndian(source, out uint uintValue));
                value = DateTimeConstants.UnixEpoch.AddSeconds(uintValue);
                return DecodeResult.Success;
            case 8:
                AssumesTrue(TryReadBigEndian(source, out ulong ulongValue));
                long nanoseconds = (long)(ulongValue >> 34);
                ulong seconds = ulongValue & 0x00000003ffffffffL;
                value = DateTimeConstants.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
                return DecodeResult.Success;
            case 12:
                AssumesTrue(TryReadBigEndian(source, out uintValue));
                nanoseconds = uintValue;
                AssumesTrue(TryReadBigEndian(source.Slice(sizeof(uint)), out long longValue));
                value = DateTimeConstants.UnixEpoch.AddSeconds(longValue).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
                return DecodeResult.Success;
            default:
                value = default;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read the header of an extension from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="extensionHeader">Receives the extension header, if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// Reads an extension header from
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>.
    /// </remarks>
    public static DecodeResult TryReadExtensionHeader(ReadOnlySpan<byte> source, out ExtensionHeader extensionHeader, out int tokenSize)
    {
        tokenSize = 2;
        if (source.Length < tokenSize)
        {
            extensionHeader = default;
            return source.Length == 0 ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;
        }

        uint length = 0;
        switch (source[0])
        {
            case MessagePackCode.FixExt1:
                length = 1;
                break;
            case MessagePackCode.FixExt2:
                length = 2;
                break;
            case MessagePackCode.FixExt4:
                length = 4;
                break;
            case MessagePackCode.FixExt8:
                length = 8;
                break;
            case MessagePackCode.FixExt16:
                length = 16;
                break;
            case MessagePackCode.Ext8:
                tokenSize = 3;
                if (source.Length < tokenSize)
                {
                    extensionHeader = default;
                    return DecodeResult.InsufficientBuffer;
                }

                length = source[1];
                break;
            case MessagePackCode.Ext16:
                tokenSize = 4;
                if (source.Length < tokenSize)
                {
                    extensionHeader = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ushort ushortValue));
                length = ushortValue;
                break;
            case MessagePackCode.Ext32:
                tokenSize = 6;
                if (source.Length < tokenSize)
                {
                    extensionHeader = default;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                length = uintValue;
                break;
            default:
                extensionHeader = default;
                return DecodeResult.TokenMismatch;
        }

        sbyte typeCode = unchecked((sbyte)source[tokenSize - 1]);
        extensionHeader = new ExtensionHeader(typeCode, length);
        return DecodeResult.Success;
    }

    /// <summary>
    /// Tries to read the header of a binary data segment from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="length">Receives the length of the binary data, if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// <para>
    /// Reads a binary data header from
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>, or
    /// <see cref="MessagePackCode.Bin32"/>.
    /// </para>
    /// <para>
    /// Note that in the original msgpack spec, there were no binary data headers, so binary data was
    /// introduced using string headers.
    /// This should be read using <see cref="TryReadStringHeader(ReadOnlySpan{byte}, out uint, out int)"/>.
    /// </para>
    /// </remarks>
    public static DecodeResult TryReadBinHeader(ReadOnlySpan<byte> source, out uint length, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < tokenSize)
        {
            length = 0;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Bin8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                length = source[1];
                return DecodeResult.Success;
            case MessagePackCode.Bin16:
                tokenSize = 3;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ushort ushortValue));
                length = ushortValue;
                return DecodeResult.Success;
            case MessagePackCode.Bin32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                length = uintValue;
                return DecodeResult.Success;
            default:
                length = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Tries to read the header of a string from the specified buffer.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="length">Receives the length of the string, if successful.</param>
    /// <param name="tokenSize">Receives the number of bytes read from the source, or the minimum length of <paramref name="source"/> required to read the data.</param>
    /// <returns>The result classification of the read operation.</returns>
    /// <remarks>
    /// <para>
    /// Reads a string header from
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>,
    /// or any value between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
    /// </para>
    /// <para>
    /// Note that in the original msgpack spec, there were no binary data headers, so binary data was
    /// introduced using string headers.
    /// </para>
    /// </remarks>
    public static DecodeResult TryReadStringHeader(ReadOnlySpan<byte> source, out uint length, out int tokenSize)
    {
        tokenSize = 1;
        if (source.Length < tokenSize)
        {
            length = 0;
            return DecodeResult.EmptyBuffer;
        }

        switch (source[0])
        {
            case MessagePackCode.Str8:
                tokenSize = 2;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                length = source[1];
                return DecodeResult.Success;
            case MessagePackCode.Str16:
                tokenSize = 3;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out ushort ushortValue));
                length = ushortValue;
                return DecodeResult.Success;
            case MessagePackCode.Str32:
                tokenSize = 5;
                if (source.Length < tokenSize)
                {
                    length = 0;
                    return DecodeResult.InsufficientBuffer;
                }

                AssumesTrue(TryReadBigEndian(source.Slice(1), out uint uintValue));
                length = uintValue;
                return DecodeResult.Success;
            case >= MessagePackCode.MinFixStr and <= MessagePackCode.MaxFixStr:
                length = (byte)(source[0] & 0x1F);
                return DecodeResult.Success;
            default:
                length = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>
    /// Reads an <see cref="ushort"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="ushort"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out ushort value)
    {
        if (source.Length < sizeof(short))
        {
            value = default;
            return false;
        }

        value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(source));
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return true;
    }

    /// <summary>
    /// Reads a <see cref="short"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for a <see cref="short"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out short value)
    {
        if (TryReadBigEndian(source, out ushort ushortValue))
        {
            value = unchecked((short)ushortValue);
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Reads a <see cref="uint"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="uint"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out uint value)
    {
        if (source.Length < sizeof(uint))
        {
            value = default;
            return false;
        }

        value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(source));
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return true;
    }

    /// <summary>
    /// Reads an <see cref="int"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="int"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out int value)
    {
        if (TryReadBigEndian(source, out uint uintValue))
        {
            value = unchecked((int)uintValue);
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Reads a <see cref="uint"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="uint"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out ulong value)
    {
        if (source.Length < sizeof(ulong))
        {
            value = default;
            return false;
        }

        value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(source));
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return true;
    }

    /// <summary>
    /// Reads a <see cref="long"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="long"/>.</returns>
    private static bool TryReadBigEndian(ReadOnlySpan<byte> source, out long value)
    {
        if (TryReadBigEndian(source, out ulong ulongValue))
        {
            value = unchecked((long)ulongValue);
            return true;
        }

        value = 0;
        return false;
    }

    private static void AssumesTrue([DoesNotReturnIf(false)] bool condition)
    {
        if (!condition)
        {
            throw new Exception("Internal error.");
        }
    }
}

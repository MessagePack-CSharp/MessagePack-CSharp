// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using MessagePack.Internal;

namespace MessagePack
{
    /// <summary>
    /// A primitive types reader for the MessagePack format.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
    /// </remarks>
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    ref partial struct MessagePackReader
    {
        /// <summary>
        /// The reader over the sequence.
        /// </summary>
        private SequenceReader<byte> reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackReader"/> struct.
        /// </summary>
        /// <param name="memory">The buffer to read from.</param>
        public MessagePackReader(ReadOnlyMemory<byte> memory)
        {
            this.reader = new SequenceReader<byte>(memory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackReader"/> struct.
        /// </summary>
        /// <param name="readOnlySequence">The sequence to read from.</param>
        public MessagePackReader(in ReadOnlySequence<byte> readOnlySequence)
        {
            this.reader = new SequenceReader<byte>(readOnlySequence);
        }

        /// <summary>
        /// Gets the <see cref="ReadOnlySequence{T}"/> originally supplied to the constructor.
        /// </summary>
        public ReadOnlySequence<byte> Sequence => this.reader.Sequence;

        /// <summary>
        /// Gets the current position of the reader within <see cref="Sequence"/>.
        /// </summary>
        public SequencePosition Position => this.reader.Position;

        /// <summary>
        /// Gets the number of bytes consumed by the reader.
        /// </summary>
        public long Consumed => this.reader.Consumed;

        /// <summary>
        /// Gets a value indicating whether the reader is at the end of the sequence.
        /// </summary>
        public bool End => this.reader.End;

        /// <summary>
        /// Gets a value indicating whether the reader position is pointing at a nil value.
        /// </summary>
        /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
        public bool IsNil => this.NextCode == MessagePackCode.Nil;

        /// <summary>
        /// Gets the next message pack type to be read.
        /// </summary>
        public MessagePackType NextMessagePackType => MessagePackCode.ToMessagePackType(this.NextCode);

        /// <summary>
        /// Gets the type of the next MessagePack block.
        /// </summary>
        /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
        /// <remarks>
        /// See <see cref="MessagePackCode"/> for valid message pack codes and ranges.
        /// </remarks>
        public byte NextCode
        {
            get
            {
                ThrowInsufficientBufferUnless(this.reader.TryPeek(out byte code));
                return code;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackReader"/> struct,
        /// with the same settings as this one, but with its own buffer to read from.
        /// </summary>
        /// <param name="readOnlySequence">The sequence to read from.</param>
        /// <returns>The new reader.</returns>
        public MessagePackReader Clone(in ReadOnlySequence<byte> readOnlySequence) => new MessagePackReader(readOnlySequence)
        {
        };

        /// <summary>
        /// Creates a new <see cref="MessagePackReader"/> at this reader's current position.
        /// The two readers may then be used independently without impacting each other.
        /// </summary>
        /// <returns>A new <see cref="MessagePackReader"/>.</returns>
        public MessagePackReader CreatePeekReader() => this.Clone(this.reader.Sequence.Slice(this.reader.Position));

        /// <summary>
        /// Advances the reader to the next MessagePack primitive to be read.
        /// </summary>
        /// <remarks>
        /// The entire primitive is skipped, including content of maps or arrays, or any other type with payloads.
        /// </remarks>
        public void Skip()
        {
            byte code = this.NextCode;
            switch (code)
            {
                case MessagePackCode.Nil:
                case MessagePackCode.True:
                case MessagePackCode.False:
                    this.reader.Advance(1);
                    break;
                case MessagePackCode.Int8:
                case MessagePackCode.UInt8:
                    this.reader.Advance(2);
                    break;
                case MessagePackCode.Int16:
                case MessagePackCode.UInt16:
                    this.reader.Advance(3);
                    break;
                case MessagePackCode.Int32:
                case MessagePackCode.UInt32:
                case MessagePackCode.Float32:
                    this.reader.Advance(5);
                    break;
                case MessagePackCode.Int64:
                case MessagePackCode.UInt64:
                case MessagePackCode.Float64:
                    this.reader.Advance(9);
                    break;
                case MessagePackCode.Map16:
                case MessagePackCode.Map32:
                    this.ReadNextMap();
                    break;
                case MessagePackCode.Array16:
                case MessagePackCode.Array32:
                    this.ReadNextArray();
                    break;
                case MessagePackCode.Str8:
                case MessagePackCode.Str16:
                case MessagePackCode.Str32:
                    int length = this.GetStringLengthInBytes();
                    this.reader.Advance(length);
                    break;
                case MessagePackCode.Bin8:
                case MessagePackCode.Bin16:
                case MessagePackCode.Bin32:
                    length = this.GetBytesLength();
                    this.reader.Advance(length);
                    break;
                case MessagePackCode.FixExt1:
                case MessagePackCode.FixExt2:
                case MessagePackCode.FixExt4:
                case MessagePackCode.FixExt8:
                case MessagePackCode.FixExt16:
                case MessagePackCode.Ext8:
                case MessagePackCode.Ext16:
                case MessagePackCode.Ext32:
                    ExtensionHeader header = this.ReadExtensionFormatHeader();
                    this.reader.Advance(header.Length);
                    break;
                default:
                    if ((code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt) ||
                        (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt))
                    {
                        this.reader.Advance(1);
                        break;
                    }

                    if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                    {
                        this.ReadNextMap();
                        break;
                    }

                    if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                    {
                        this.ReadNextArray();
                        break;
                    }

                    if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                    {
                        length = this.GetStringLengthInBytes();
                        this.reader.Advance(length);
                        break;
                    }

                    // We don't actually expect to ever hit this point, since every code is supported.
                    Debug.Fail("Missing handler for code: " + code);
                    throw ThrowInvalidCode(code);
            }
        }

        /// <summary>
        /// Reads a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        /// <returns>A nil value.</returns>
        public Nil ReadNil()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            return code == MessagePackCode.Nil
                ? Nil.Default
                : throw ThrowInvalidCode(code);
        }

        /// <summary>
        /// Reads nil if it is the next token.
        /// </summary>
        /// <returns><c>true</c> if the next token was nil; <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadNil()
        {
            if (this.NextCode == MessagePackCode.Nil)
            {
                this.reader.Advance(1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a sequence of bytes without any decoding.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The sequence of bytes read.</returns>
        public ReadOnlySequence<byte> ReadRaw(long length)
        {
            ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
            this.reader.Advance(length);
            return result;
        }

        /// <summary>
        /// Read an array header from
        /// <see cref="MessagePackCode.Array16"/>,
        /// <see cref="MessagePackCode.Array32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadArrayHeader()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            int count;
            switch (code)
            {
                case MessagePackCode.Array16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortValue));
                    count = unchecked((ushort)shortValue);
                    break;
                case MessagePackCode.Array32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    count = intValue;
                    break;
                default:
                    if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                    {
                        count = code & 0xF;
                        break;
                    }

                    throw ThrowInvalidCode(code);
            }

            // Protected against corrupted or mischievious data that may lead to allocating way too much memory.
            // We allow for each primitive to be the minimal 1 byte in size.
            // Formatters that know each element is larger can double-check our work.
            if (count > this.reader.Remaining)
            {
                ThrowNotEnoughBytesException();
            }

            return count;
        }

        /// <summary>
        /// Read a map header from
        /// <see cref="MessagePackCode.Map16"/>,
        /// <see cref="MessagePackCode.Map32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
        /// </summary>
        public int ReadMapHeader()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            int count;
            switch (code)
            {
                case MessagePackCode.Map16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortValue));
                    count = unchecked((ushort)shortValue);
                    break;
                case MessagePackCode.Map32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    count = intValue;
                    break;
                default:
                    if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                    {
                        count = (byte)(code & 0xF);
                        break;
                    }

                    throw ThrowInvalidCode(code);
            }

            // Protected against corrupted or mischievious data that may lead to allocating way too much memory.
            // We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
            // Formatters that know each element is larger can double-check our work.
            if (count * 2 > this.reader.Remaining)
            {
                ThrowNotEnoughBytesException();
            }

            return count;
        }

        /// <summary>
        /// Reads a boolean value from either a <see cref="MessagePackCode.False"/> or <see cref="MessagePackCode.True"/>.
        /// </summary>
        /// <returns>The value.</returns>
        public bool ReadBoolean()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));
            switch (code)
            {
                case MessagePackCode.True:
                    return true;
                case MessagePackCode.False:
                    return false;
                default:
                    throw ThrowInvalidCode(code);
            }
        }

        /// <summary>
        /// Reads a <see cref="char"/> from any of:
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// or anything between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
        /// </summary>
        /// <returns>A character.</returns>
        public char ReadChar() => (char)this.ReadUInt16();

        /// <summary>
        /// Reads an <see cref="float"/> value from any value encoded with:
        /// <see cref="MessagePackCode.Float32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>,
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
        /// </summary>
        /// <returns>The value.</returns>
        public unsafe float ReadSingle()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.Float32:
                    byte* pScratch32 = stackalloc byte[4];
                    Span<byte> scratch32 = new Span<byte>(pScratch32, 4);
                    ThrowInsufficientBufferUnless(this.reader.TryCopyTo(scratch32));
                    this.reader.Advance(4);
                    var floatValue = new Float32Bits(scratch32);
                    return floatValue.Value;
                case MessagePackCode.Float64:
                    byte* pScratch64 = stackalloc byte[8];
                    Span<byte> scratch64 = new Span<byte>(pScratch64, 8);
                    ThrowInsufficientBufferUnless(this.reader.TryCopyTo(scratch64));
                    this.reader.Advance(8);
                    var doubleValue = new Float64Bits(scratch64);
                    return (float)doubleValue.Value;
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteValue));
                    return sbyteValue;
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortValue));
                    return shortValue;
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    return intValue;
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longValue));
                    return longValue;
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteValue));
                    return byteValue;
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortValue));
                    return ushortValue;
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintValue));
                    return uintValue;
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongValue));
                    return ulongValue;
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return unchecked((sbyte)code);
                    }
                    else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return code;
                    }

                    throw ThrowInvalidCode(code);
            }
        }

        /// <summary>
        /// Reads an <see cref="double"/> value from any value encoded with:
        /// <see cref="MessagePackCode.Float64"/>,
        /// <see cref="MessagePackCode.Float32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>,
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
        /// </summary>
        /// <returns>The value.</returns>
        public unsafe double ReadDouble()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.Float64:
                    byte* pScratch8 = stackalloc byte[8];
                    Span<byte> scratch8 = new Span<byte>(pScratch8, 8);
                    ThrowInsufficientBufferUnless(this.reader.TryCopyTo(scratch8));
                    this.reader.Advance(scratch8.Length);
                    var doubleValue = new Float64Bits(scratch8);
                    return doubleValue.Value;
                case MessagePackCode.Float32:
                    byte* pScratch4 = stackalloc byte[4];
                    Span<byte> scratch4 = new Span<byte>(pScratch4, 4);
                    ThrowInsufficientBufferUnless(this.reader.TryCopyTo(scratch4));
                    this.reader.Advance(scratch4.Length);
                    var floatValue = new Float32Bits(scratch4);
                    return floatValue.Value;
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteValue));
                    return unchecked((sbyte)byteValue);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortValue));
                    return shortValue;
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    return intValue;
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longValue));
                    return longValue;
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byteValue));
                    return byteValue;
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out shortValue));
                    return unchecked((ushort)shortValue);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out intValue));
                    return unchecked((uint)intValue);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out longValue));
                    return unchecked((ulong)longValue);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return unchecked((sbyte)code);
                    }
                    else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return code;
                    }

                    throw ThrowInvalidCode(code);
            }
        }

        /// <summary>
        /// Reads a <see cref="DateTime"/> from a value encoded with
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>, or
        /// <see cref="MessagePackCode.Ext8"/>.
        /// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <returns>The value.</returns>
        public DateTime ReadDateTime() => this.ReadDateTime(this.ReadExtensionFormatHeader());

        /// <summary>
        /// Reads a <see cref="DateTime"/> from a value encoded with
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.Ext8"/>.
        /// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="header">The extension header that was already read.</param>
        /// <returns>The value.</returns>
        internal DateTime ReadDateTime(ExtensionHeader header)
        {
            if (header.TypeCode != ReservedMessagePackExtensionTypeCode.DateTime)
            {
                throw new InvalidOperationException(string.Format("Extension TypeCode is invalid. typeCode: {0}", header.TypeCode));
            }

            switch (header.Length)
            {
                case 4:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    return DateTimeConstants.UnixEpoch.AddSeconds(unchecked((uint)intValue));
                case 8:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longValue));
                    ulong ulongValue = unchecked((ulong)longValue);
                    long nanoseconds = (long)(ulongValue >> 34);
                    ulong seconds = ulongValue & 0x00000003ffffffffL;
                    return DateTimeConstants.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
                case 12:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out intValue));
                    nanoseconds = unchecked((uint)intValue);
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out longValue));
                    return DateTimeConstants.UnixEpoch.AddSeconds(longValue).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
                default:
                    throw new InvalidOperationException($"Length of extension was {header.Length}. Either 4 or 8 were expected.");
            }
        }

        /// <summary>
        /// Reads a span of bytes, whose length is determined by a header of one of these types:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>,
        /// <see cref="MessagePackCode.Bin32"/>,
        /// or to support OldSpec compatibility:
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or something beteween <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
        /// </summary>
        /// <returns>
        /// A sequence of bytes.
        /// The data is a slice from the original sequence passed to this reader's constructor.
        /// </returns>
        public ReadOnlySequence<byte> ReadBytes()
        {
            int length = this.GetBytesLength();
            ThrowInsufficientBufferUnless(this.reader.Remaining >= length);
            ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
            this.reader.Advance(length);
            return result;
        }

        /// <summary>
        /// Reads a string of bytes, whose length is determined by a header of one of these types:
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
        /// </summary>
        /// <returns>
        /// The sequence of bytes.
        /// The data is a slice from the original sequence passed to this reader's constructor.
        /// </returns>
        public ReadOnlySequence<byte> ReadStringSegment()
        {
            int length = this.GetStringLengthInBytes();
            ThrowInsufficientBufferUnless(this.reader.Remaining >= length);
            ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
            this.reader.Advance(length);
            return result;
        }

        /// <summary>
        /// Reads a string, whose length is determined by a header of one of these types:
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
        /// </summary>
        /// <returns>A string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            int byteLength = this.GetStringLengthInBytes();

            ReadOnlySpan<byte> unreadSpan = this.reader.UnreadSpan;
            if (unreadSpan.Length >= byteLength)
            {
                // Fast path: all bytes to decode appear in the same span.
                string value = StringEncoding.UTF8.GetString(unreadSpan.Slice(0, byteLength));
                this.reader.Advance(byteLength);
                return value;
            }
            else
            {
                return this.ReadStringSlow(byteLength);
            }
        }

        /// <summary>
        /// Reads an extension format header, based on one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <returns>The extension header.</returns>
        public ExtensionHeader ReadExtensionFormatHeader()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            uint length;
            switch (code)
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
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteLength));
                    length = byteLength;
                    break;
                case MessagePackCode.Ext16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortLength));
                    length = unchecked((ushort)shortLength);
                    break;
                case MessagePackCode.Ext32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intLength));
                    length = unchecked((uint)intLength);
                    break;
                default:
                    throw ThrowInvalidCode(code);
            }

            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte typeCode));
            return new ExtensionHeader(unchecked((sbyte)typeCode), length);
        }

        /// <summary>
        /// Reads an extension format header and data, based on one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <returns>
        /// The extension format.
        /// The data is a slice from the original sequence passed to this reader's constructor.
        /// </returns>
        public ExtensionResult ReadExtensionFormat()
        {
            ExtensionHeader header = this.ReadExtensionFormatHeader();
            ReadOnlySequence<byte> data = this.reader.Sequence.Slice(this.reader.Position, header.Length);
            this.reader.Advance(header.Length);
            return new ExtensionResult(header.TypeCode, data);
        }

        /// <summary>
        /// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
        /// the promised data.
        /// </summary>
        private static void ThrowNotEnoughBytesException() => throw new EndOfStreamException();

        private static Exception ThrowInvalidCode(byte code)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code: {0} format: {1}", code, MessagePackCode.ToFormatName(code)));
        }

        /// <summary>
        /// Throws <see cref="EndOfStreamException"/> if a condition is false.
        /// </summary>
        /// <param name="condition">A boolean value.</param>
        /// <exception cref="EndOfStreamException">Thrown if <paramref name="condition"/> is <c>false</c>.</exception>
        private static void ThrowInsufficientBufferUnless(bool condition)
        {
            if (!condition)
            {
                throw new EndOfStreamException();
            }
        }

        private int GetBytesLength()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            // In OldSpec mode, Bin didn't exist, so Str was used. Str8 didn't exist either.
            int length;
            switch (code)
            {
                case MessagePackCode.Bin8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteLength));
                    length = byteLength;
                    break;
                case MessagePackCode.Bin16:
                case MessagePackCode.Str16: // OldSpec compatibility
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortLength));
                    length = unchecked((ushort)shortLength);
                    break;
                case MessagePackCode.Bin32:
                case MessagePackCode.Str32: // OldSpec compatibility
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out length));
                    break;
                default:
                    // OldSpec compatibility
                    if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                    {
                        length = code & 0x1F;
                        break;
                    }

                    throw ThrowInvalidCode(code);
            }

            return length;
        }

        /// <summary>
        /// Gets the length of the next string.
        /// </summary>
        /// <returns>The length of the next string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetStringLengthInBytes()
        {
            ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

            if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
            {
                return code & 0x1F;
            }

            return this.GetStringLengthInBytesSlow(code);
        }

        private int GetStringLengthInBytesSlow(byte code)
        {
            switch (code)
            {
                case MessagePackCode.Str8:
                    ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteValue));
                    return byteValue;
                case MessagePackCode.Str16:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortValue));
                    return unchecked((ushort)shortValue);
                case MessagePackCode.Str32:
                    ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intValue));
                    return intValue;
                default:
                    if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                    {
                        return code & 0x1F;
                    }

                    throw ThrowInvalidCode(code);
            }
        }

        /// <summary>
        /// Reads a string assuming that it is spread across multiple spans in the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="byteLength">The length of the string to be decoded, in bytes.</param>
        /// <returns>The decoded string.</returns>
        private string ReadStringSlow(int byteLength)
        {
            ThrowInsufficientBufferUnless(this.reader.Remaining >= byteLength);

            // We need to decode bytes incrementally across multiple spans.
            int maxCharLength = StringEncoding.UTF8.GetMaxCharCount(byteLength);
            char[] charArray = ArrayPool<char>.Shared.Rent(maxCharLength);
            System.Text.Decoder decoder = StringEncoding.UTF8.GetDecoder();

            int remainingByteLength = byteLength;
            int initializedChars = 0;
            while (remainingByteLength > 0)
            {
                int bytesRead = Math.Min(remainingByteLength, this.reader.UnreadSpan.Length);
                remainingByteLength -= bytesRead;
                bool flush = remainingByteLength == 0;
#if NETCOREAPP2_1
                initializedChars += decoder.GetChars(this.reader.UnreadSpan.Slice(0, bytesRead), charArray.AsSpan(initializedChars), flush);
#else
                unsafe
                {
                    fixed (byte* pUnreadSpan = this.reader.UnreadSpan)
                    fixed (char* pCharArray = &charArray[initializedChars])
                    {
                        initializedChars += decoder.GetChars(pUnreadSpan, bytesRead, pCharArray, charArray.Length - initializedChars, flush);
                    }
                }
#endif
                this.reader.Advance(bytesRead);
            }

            string value = new string(charArray, 0, initializedChars);
            ArrayPool<char>.Shared.Return(charArray);
            return value;
        }

        private void ReadNextArray()
        {
            int count = this.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                this.Skip();
            }
        }

        private void ReadNextMap()
        {
            int count = this.ReadMapHeader();
            for (int i = 0; i < count; i++)
            {
                this.Skip(); // key
                this.Skip(); // value
            }
        }
    }
}

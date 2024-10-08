// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using MessagePack.Internal;

namespace MessagePack
{
    /// <summary>
    /// A primitive types reader for the MessagePack format.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
    /// </remarks>
    /// <exception cref="MessagePackSerializationException">Thrown when reading methods fail due to invalid data.</exception>
    /// <exception cref="EndOfStreamException">Thrown by reading methods when there are not enough bytes to read the required value.</exception>
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
            : this()
        {
            this.reader = new SequenceReader<byte>(memory);
            this.Depth = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackReader"/> struct.
        /// </summary>
        /// <param name="readOnlySequence">The sequence to read from.</param>
        public MessagePackReader(in ReadOnlySequence<byte> readOnlySequence)
            : this()
        {
            this.reader = new SequenceReader<byte>(readOnlySequence);
            this.Depth = 0;
        }

        /// <summary>
        /// Gets or sets the cancellation token for this deserialization operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the present depth of the object graph being deserialized.
        /// </summary>
        public int Depth { get; set; }

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
            CancellationToken = this.CancellationToken,
            Depth = this.Depth,
        };

        /// <summary>
        /// Creates a new <see cref="MessagePackReader"/> at this reader's current position.
        /// The two readers may then be used independently without impacting each other.
        /// </summary>
        /// <returns>A new <see cref="MessagePackReader"/>.</returns>
        /// <devremarks>
        /// Since this is a struct, copying it completely is as simple as returning itself
        /// from a property that isn't a "ref return" property.
        /// </devremarks>
        public MessagePackReader CreatePeekReader() => this;

        /// <summary>
        /// Advances the reader to the next MessagePack structure to be read.
        /// </summary>
        /// <remarks>
        /// The entire structure is skipped, including content of maps or arrays, or any other type with payloads.
        /// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw()"/> instead.
        /// </remarks>
        public void Skip() => ThrowInsufficientBufferUnless(this.TrySkip());

        /// <summary>
        /// Advances the reader to the next MessagePack structure to be read.
        /// </summary>
        /// <returns><see langword="true"/> if the entire structure beginning at the current <see cref="Position"/> is found in the <see cref="Sequence"/>; <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// The entire structure is skipped, including content of maps or arrays, or any other type with payloads.
        /// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw()"/> instead.
        /// WARNING: when false is returned, the position of the reader is undefined.
        /// </remarks>
        internal bool TrySkip()
        {
            if (this.reader.Remaining == 0)
            {
                return false;
            }

            byte code = this.NextCode;
            switch (code)
            {
                case MessagePackCode.Nil:
                case MessagePackCode.True:
                case MessagePackCode.False:
                    return this.reader.TryAdvance(1);
                case MessagePackCode.Int8:
                case MessagePackCode.UInt8:
                    return this.reader.TryAdvance(2);
                case MessagePackCode.Int16:
                case MessagePackCode.UInt16:
                    return this.reader.TryAdvance(3);
                case MessagePackCode.Int32:
                case MessagePackCode.UInt32:
                case MessagePackCode.Float32:
                    return this.reader.TryAdvance(5);
                case MessagePackCode.Int64:
                case MessagePackCode.UInt64:
                case MessagePackCode.Float64:
                    return this.reader.TryAdvance(9);
                case MessagePackCode.Map16:
                case MessagePackCode.Map32:
                    return this.TrySkipNextMap();
                case MessagePackCode.Array16:
                case MessagePackCode.Array32:
                    return this.TrySkipNextArray();
                case MessagePackCode.Str8:
                case MessagePackCode.Str16:
                case MessagePackCode.Str32:
                    return this.TryGetStringLengthInBytes(out uint length) && this.reader.TryAdvance(length);
                case MessagePackCode.Bin8:
                case MessagePackCode.Bin16:
                case MessagePackCode.Bin32:
                    return this.TryGetBytesLength(out length) && this.reader.TryAdvance(length);
                case MessagePackCode.FixExt1:
                case MessagePackCode.FixExt2:
                case MessagePackCode.FixExt4:
                case MessagePackCode.FixExt8:
                case MessagePackCode.FixExt16:
                case MessagePackCode.Ext8:
                case MessagePackCode.Ext16:
                case MessagePackCode.Ext32:
                    return this.TryReadExtensionFormatHeader(out ExtensionHeader header) && this.reader.TryAdvance(header.Length);
                default:
                    if ((code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt) ||
                        (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt))
                    {
                        return this.reader.TryAdvance(1);
                    }

                    if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                    {
                        return this.TrySkipNextMap();
                    }

                    if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                    {
                        return this.TrySkipNextArray();
                    }

                    if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                    {
                        return this.TryGetStringLengthInBytes(out length) && this.reader.TryAdvance(length);
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
        /// <returns><see langword="true"/> if the next token was nil; <see langword="false"/> otherwise.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
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
            try
            {
                ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
                this.reader.Advance(length);
                return result;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ThrowNotEnoughBytesException(ex);
            }
        }

        /// <summary>
        /// Reads the next MessagePack primitive.
        /// </summary>
        /// <returns>The raw MessagePack sequence.</returns>
        /// <remarks>
        /// The entire primitive is read, including content of maps or arrays, or any other type with payloads.
        /// </remarks>
        public ReadOnlySequence<byte> ReadRaw()
        {
            SequencePosition initialPosition = this.Position;
            this.Skip();
            return this.Sequence.Slice(initialPosition, this.Position);
        }

        /// <summary>
        /// Read an array header from
        /// <see cref="MessagePackCode.Array16"/>,
        /// <see cref="MessagePackCode.Array32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        /// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
        /// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
        /// </exception>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an array header is encountered.</exception>
        public int ReadArrayHeader()
        {
            ThrowInsufficientBufferUnless(this.TryReadArrayHeader(out int count));

            // Protect against corrupted or mischievous data that may lead to allocating way too much memory.
            // We allow for each primitive to be the minimal 1 byte in size.
            // Formatters that know each element is larger can optionally add a stronger check.
            ThrowInsufficientBufferUnless(this.reader.Remaining >= count);

            return count;
        }

        /// <summary>
        /// Reads an array header from
        /// <see cref="MessagePackCode.Array16"/>,
        /// <see cref="MessagePackCode.Array32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>
        /// if there is sufficient buffer to read it.
        /// </summary>
        /// <param name="count">Receives the number of elements in the array if the entire array header could be read.</param>
        /// <returns><see langword="true"/> if there was sufficient buffer and an array header was found; <see langword="false"/> if the buffer incompletely describes an array header.</returns>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an array header is encountered.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadArrayHeader(out int count)
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadArrayHeader(this.reader.UnreadSpan, out uint uintCount, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    count = checked((int)uintCount);
                    this.reader.Advance(tokenSize);
                    return true;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadArrayHeader(buffer, out uintCount, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        count = 0;
                        return false;
                    }

                default:
                    throw ThrowUnreachable();
            }
        }

        /// <summary>
        /// Read a map header from
        /// <see cref="MessagePackCode.Map16"/>,
        /// <see cref="MessagePackCode.Map32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
        /// </summary>
        /// <returns>The number of key=value pairs in the map.</returns>
        /// <exception cref="EndOfStreamException">
        /// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
        /// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
        /// </exception>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an map header is encountered.</exception>
        public int ReadMapHeader()
        {
            ThrowInsufficientBufferUnless(this.TryReadMapHeader(out int count));

            // Protect against corrupted or mischievous data that may lead to allocating way too much memory.
            // We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
            // Formatters that know each element is larger can optionally add a stronger check.
            ThrowInsufficientBufferUnless(this.reader.Remaining >= count * 2);

            return count;
        }

        /// <summary>
        /// Reads a map header from
        /// <see cref="MessagePackCode.Map16"/>,
        /// <see cref="MessagePackCode.Map32"/>, or
        /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>
        /// if there is sufficient buffer to read it.
        /// </summary>
        /// <param name="count">Receives the number of key=value pairs in the map if the entire map header can be read.</param>
        /// <returns><see langword="true"/> if there was sufficient buffer and a map header was found; <see langword="false"/> if the buffer incompletely describes an map header.</returns>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an map header is encountered.</exception>
        public bool TryReadMapHeader(out int count)
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadMapHeader(this.reader.UnreadSpan, out uint uintCount, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    count = checked((int)uintCount);
                    this.reader.Advance(tokenSize);
                    return true;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadMapHeader(buffer, out uintCount, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        count = 0;
                        return false;
                    }

                default:
                    throw ThrowUnreachable();
            }
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
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadSingle(this.reader.UnreadSpan, out float value, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return value;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadSingle(buffer, out value, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        throw ThrowNotEnoughBytesException();
                    }

                default:
                    throw ThrowUnreachable();
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
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadDouble(this.reader.UnreadSpan, out double value, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return value;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadDouble(buffer, out value, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        throw ThrowNotEnoughBytesException();
                    }

                default:
                    throw ThrowUnreachable();
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
        public DateTime ReadDateTime()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadDateTime(this.reader.UnreadSpan, out DateTime value, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return value;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadDateTime(buffer, out value, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        throw ThrowNotEnoughBytesException();
                    }

                default:
                    throw ThrowUnreachable();
            }
        }

        /// <summary>
        /// Reads a <see cref="DateTime"/> from a value encoded with
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.Ext8"/>.
        /// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="header">The extension header that was already read.</param>
        /// <returns>The value.</returns>
        public DateTime ReadDateTime(ExtensionHeader header)
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadDateTime(this.reader.UnreadSpan, header, out DateTime value, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return value;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadDateTime(buffer, header, out value, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        throw ThrowNotEnoughBytesException();
                    }

                default:
                    throw ThrowUnreachable();
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
        /// or something between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
        /// </summary>
        /// <returns>
        /// A sequence of bytes, or <see langword="null"/> if the read token is <see cref="MessagePackCode.Nil"/>.
        /// The data is a slice from the original sequence passed to this reader's constructor.
        /// </returns>
        public ReadOnlySequence<byte>? ReadBytes()
        {
            if (this.TryReadNil())
            {
                return null;
            }

            uint length = this.GetBytesLength();
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
        /// The sequence of bytes, or <see langword="null"/> if the read token is <see cref="MessagePackCode.Nil"/>.
        /// The data is a slice from the original sequence passed to this reader's constructor.
        /// </returns>
        public ReadOnlySequence<byte>? ReadStringSequence()
        {
            if (this.TryReadNil())
            {
                return null;
            }

            uint length = this.GetStringLengthInBytes();
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
        /// <param name="span">Receives the span to the string.</param>
        /// <returns>
        /// <see langword="true"/> if the string is contiguous in memory such that it could be set as a single span.
        /// <see langword="false"/> if the read token is <see cref="MessagePackCode.Nil"/> or the string is not in a contiguous span.
        /// </returns>
        /// <remarks>
        /// Callers should generally be prepared for a <see langword="false"/> result and failover to calling <see cref="ReadStringSequence"/>
        /// which can represent a <see langword="null"/> result and handle strings that are not contiguous in memory.
        /// </remarks>
        public bool TryReadStringSpan(out ReadOnlySpan<byte> span)
        {
            if (this.IsNil)
            {
                span = default;
                return false;
            }

            long oldPosition = this.reader.Consumed;
            int length = checked((int)this.GetStringLengthInBytes());
            ThrowInsufficientBufferUnless(this.reader.Remaining >= length);

            if (this.reader.CurrentSpanIndex + length <= this.reader.CurrentSpan.Length)
            {
                span = this.reader.CurrentSpan.Slice(this.reader.CurrentSpanIndex, length);
                this.reader.Advance(length);
                return true;
            }
            else
            {
                this.reader.Rewind(this.reader.Consumed - oldPosition);
                span = default;
                return false;
            }
        }

        /// <summary>
        /// Reads a string, whose length is determined by a header of one of these types:
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
        /// </summary>
        /// <returns>A string, or <see langword="null"/> if the current msgpack token is <see cref="MessagePackCode.Nil"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? ReadString()
        {
            if (this.TryReadNil())
            {
                return null;
            }

            uint byteLength = this.GetStringLengthInBytes();

            ReadOnlySpan<byte> unreadSpan = this.reader.UnreadSpan;
            ////UnityEngine.Debug.Log(reader.CurrentSpan[0]);
            ////UnityEngine.Debug.Log(unreadSpan[0]);
            if (unreadSpan.Length >= byteLength)
            {
                // Fast path: all bytes to decode appear in the same span.
                string value = StringEncoding.UTF8.GetString(unreadSpan.Slice(0, checked((int)byteLength)));
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
        /// <exception cref="EndOfStreamException">
        /// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
        /// or if it is clear that there are insufficient bytes remaining after the header to include all the bytes the header claims to be there.
        /// </exception>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an extension format header is encountered.</exception>
        public ExtensionHeader ReadExtensionFormatHeader()
        {
            ThrowInsufficientBufferUnless(this.TryReadExtensionFormatHeader(out ExtensionHeader header));

            // Protect against corrupted or mischievous data that may lead to allocating way too much memory.
            ThrowInsufficientBufferUnless(this.reader.Remaining >= header.Length);

            return header;
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
        /// <see cref="MessagePackCode.Ext32"/>
        /// if there is sufficient buffer to read it.
        /// </summary>
        /// <param name="extensionHeader">Receives the extension header if the remaining bytes in the <see cref="Sequence"/> fully describe the header.</param>
        /// <returns>The number of key=value pairs in the map.</returns>
        /// <exception cref="MessagePackSerializationException">Thrown if a code other than an extension format header is encountered.</exception>
        /// <remarks>
        /// When this method returns <see langword="false"/> the position of the reader is left in an undefined position.
        /// The caller is expected to recreate the reader (presumably with a longer sequence to read from) before continuing.
        /// </remarks>
        public bool TryReadExtensionFormatHeader(out ExtensionHeader extensionHeader)
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadExtensionHeader(this.reader.UnreadSpan, out extensionHeader, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return true;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadExtensionHeader(buffer, out extensionHeader, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        extensionHeader = default;
                        return false;
                    }

                default:
                    throw ThrowUnreachable();
            }
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
            try
            {
                ReadOnlySequence<byte> data = this.reader.Sequence.Slice(this.reader.Position, header.Length);
                this.reader.Advance(header.Length);
                return new ExtensionResult(header.TypeCode, data);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ThrowNotEnoughBytesException(ex);
            }
        }

        /// <summary>
        /// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
        /// the promised data.
        /// </summary>
        private static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException();

        /// <summary>
        /// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
        /// the promised data.
        /// </summary>
        private static EndOfStreamException ThrowNotEnoughBytesException(Exception innerException) => throw new EndOfStreamException(new EndOfStreamException().Message, innerException);

        /// <summary>
        /// Throws an <see cref="MessagePackSerializationException"/> explaining an unexpected code was encountered.
        /// </summary>
        /// <param name="code">The code that was encountered.</param>
        /// <returns>Nothing. This method always throws.</returns>
        [DoesNotReturn]
        private static Exception ThrowInvalidCode(byte code)
        {
            throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, MessagePackCode.ToFormatName(code)));
        }

        /// <summary>
        /// Throws <see cref="EndOfStreamException"/> if a condition is false.
        /// </summary>
        /// <param name="condition">A boolean value.</param>
        /// <exception cref="EndOfStreamException">Thrown if <paramref name="condition"/> is <see langword="false"/>.</exception>
        private static void ThrowInsufficientBufferUnless(bool condition)
        {
            if (!condition)
            {
                ThrowNotEnoughBytesException();
            }
        }

        [DoesNotReturn]
        private static Exception ThrowUnreachable() => throw new Exception("Presumed unreachable point in code reached.");

        private uint GetBytesLength()
        {
            ThrowInsufficientBufferUnless(this.TryGetBytesLength(out uint length));
            return length;
        }

        private bool TryGetBytesLength(out uint length)
        {
            bool usingBinaryHeader = true;
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadBinHeader(this.reader.UnreadSpan, out length, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return true;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    if (usingBinaryHeader)
                    {
                        usingBinaryHeader = false;
                        readResult = MessagePackPrimitives.TryReadStringHeader(this.reader.UnreadSpan, out length, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                    }

                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = usingBinaryHeader
                            ? MessagePackPrimitives.TryReadBinHeader(buffer, out length, out tokenSize)
                            : MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        length = default;
                        return false;
                    }

                default:
                    throw ThrowUnreachable();
            }
        }

        /// <summary>
        /// Gets the length of the next string.
        /// </summary>
        /// <param name="length">Receives the length of the next string, if there were enough bytes to read it.</param>
        /// <returns><see langword="true"/> if there were enough bytes to read the length of the next string; <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetStringLengthInBytes(out uint length)
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadStringHeader(this.reader.UnreadSpan, out length, out int tokenSize);
retry:
            switch (readResult)
            {
                case MessagePackPrimitives.ReadResult.Success:
                    this.reader.Advance(tokenSize);
                    return true;
                case MessagePackPrimitives.ReadResult.TokenMismatch:
                    throw ThrowInvalidCode(this.reader.UnreadSpan[0]);
                case MessagePackPrimitives.ReadResult.EmptyBuffer:
                case MessagePackPrimitives.ReadResult.InsufficientBuffer:
                    Span<byte> buffer = stackalloc byte[tokenSize];
                    if (this.reader.TryCopyTo(buffer))
                    {
                        readResult = MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
                        goto retry;
                    }
                    else
                    {
                        length = default;
                        return false;
                    }

                default:
                    throw ThrowUnreachable();
            }
        }

        /// <summary>
        /// Gets the length of the next string.
        /// </summary>
        /// <returns>The length of the next string.</returns>
        private uint GetStringLengthInBytes()
        {
            ThrowInsufficientBufferUnless(this.TryGetStringLengthInBytes(out uint length));
            return length;
        }

        /// <summary>
        /// Reads a string assuming that it is spread across multiple spans in the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="byteLength">The length of the string to be decoded, in bytes.</param>
        /// <returns>The decoded string.</returns>
        private string ReadStringSlow(uint byteLength)
        {
            ThrowInsufficientBufferUnless(this.reader.Remaining >= byteLength);

            // We need to decode bytes incrementally across multiple spans.
            int remainingByteLength = checked((int)byteLength);
            int maxCharLength = StringEncoding.UTF8.GetMaxCharCount(remainingByteLength);
            char[] charArray = ArrayPool<char>.Shared.Rent(maxCharLength);
            System.Text.Decoder decoder = StringEncoding.UTF8.GetDecoder();

            int initializedChars = 0;
            while (remainingByteLength > 0)
            {
                int bytesRead = Math.Min(remainingByteLength, this.reader.UnreadSpan.Length);
                remainingByteLength -= bytesRead;
                bool flush = remainingByteLength == 0;
#if NETCOREAPP
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

        private bool TrySkipNextArray() => this.TryReadArrayHeader(out int count) && this.TrySkip(count);

        private bool TrySkipNextMap() => this.TryReadMapHeader(out int count) && this.TrySkip(count * 2);

        private bool TrySkip(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!this.TrySkip())
                {
                    return false;
                }
            }

            return true;
        }
    }
}

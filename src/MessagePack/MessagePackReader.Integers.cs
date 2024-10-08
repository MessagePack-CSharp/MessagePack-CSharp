// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;

namespace MessagePack
{
#pragma warning disable SA1205 // Partial elements should declare access
    ref partial struct MessagePackReader
#pragma warning restore SA1205 // Partial elements should declare access
    {
        /// <summary>
        /// Reads an <see cref="Byte"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public Byte ReadByte()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadByte(this.reader.UnreadSpan, out Byte value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadByte(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="UInt16"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public UInt16 ReadUInt16()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadUInt16(this.reader.UnreadSpan, out UInt16 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadUInt16(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="UInt32"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public UInt32 ReadUInt32()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadUInt32(this.reader.UnreadSpan, out UInt32 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadUInt32(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="UInt64"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public UInt64 ReadUInt64()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadUInt64(this.reader.UnreadSpan, out UInt64 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadUInt64(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="SByte"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public SByte ReadSByte()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadSByte(this.reader.UnreadSpan, out SByte value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadSByte(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="Int16"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public Int16 ReadInt16()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadInt16(this.reader.UnreadSpan, out Int16 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadInt16(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="Int32"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public Int32 ReadInt32()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadInt32(this.reader.UnreadSpan, out Int32 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadInt32(buffer, out value, out tokenSize);
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
        /// Reads an <see cref="Int64"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public Int64 ReadInt64()
        {
            MessagePackPrimitives.ReadResult readResult = MessagePackPrimitives.TryReadInt64(this.reader.UnreadSpan, out Int64 value, out int tokenSize);
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
                        readResult = MessagePackPrimitives.TryReadInt64(buffer, out value, out tokenSize);
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
    }
}

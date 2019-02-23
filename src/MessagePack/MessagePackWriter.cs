// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Internal;
using Microsoft;

namespace MessagePack
{
    /// <summary>
    /// A primitive types writer for the MessagePack format.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>
    /// </remarks>
    public ref struct MessagePackWriter
    {
        private byte[] bytes;

        private int offset;

        private int originalOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
        /// </summary>
        public MessagePackWriter(byte[] bytes = null, int offset = 0)
        {
            this.bytes = bytes;
            this.originalOffset = offset;
            this.offset = offset;
            this.OldSpec = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write in <see href="https://github.com/msgpack/msgpack/blob/master/spec-old.md">old spec</see> compatibility mode.
        /// </summary>
        public bool OldSpec { get; set; }

        public ReadOnlySequence<byte> WrittenBytes => new ReadOnlySequence<byte>(this.bytes.AsMemory(this.originalOffset, this.offset - this.originalOffset));

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct,
        /// with the same settings as this one, but with its own buffer writer.
        /// </summary>
        /// <returns>The new writer.</returns>
        public MessagePackWriter Clone() => new MessagePackWriter()
        {
            OldSpec = this.OldSpec,
        };

        /// <summary>
        /// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
        /// </summary>
        public void Flush() { }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        public void WriteNil() => this.offset += MessagePackBinary.WriteNil(ref this.bytes, this.offset);

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySpan<byte> rawMessagePackBlock)
        {
            var span = this.GetSpan(rawMessagePackBlock.Length);
            rawMessagePackBlock.CopyTo(span);
            this.Advance(rawMessagePackBlock.Length);
        }

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySequence<byte> rawMessagePackBlock)
        {
            var span = this.GetSpan((int)rawMessagePackBlock.Length);
            foreach (var segment in rawMessagePackBlock)
            {
                segment.Span.CopyTo(span);
                span = span.Slice(segment.Length);
            }

            this.Advance((int)rawMessagePackBlock.Length);
        }

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        public void WriteArrayHeader(int count) => this.offset += MessagePackBinary.WriteArrayHeader(ref this.bytes, this.offset, count);

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(uint count) => this.offset += MessagePackBinary.WriteArrayHeader(ref this.bytes, this.offset, count);

        /// <summary>
        /// Write the length of the next array to be written as <see cref="MessagePackCode.MinFixArray"/>.
        /// </summary>
        /// <param name="count">
        /// The number of elements that will be written in the array. This MUST be less than <see cref="MessagePackRange.MaxFixArrayCount"/>.
        /// This condition is NOT checked within this method, and violating this rule will result in data corruption.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedArrayHeaderUnsafe(uint count) => this.offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref this.bytes, this.offset, (int)count);

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(int count) => this.offset += MessagePackBinary.WriteMapHeader(ref this.bytes, this.offset, count);

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(uint count) => this.offset += MessagePackBinary.WriteMapHeader(ref this.bytes, this.offset, count);

        /// <summary>
        /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(byte value) => this.offset += MessagePackBinary.WriteByte(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt8(byte value) => this.offset += MessagePackBinary.WriteByteForceByteBlock(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(sbyte value) => this.offset += MessagePackBinary.WriteSByte(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt8(sbyte value) => this.offset += MessagePackBinary.WriteSByteForceSByteBlock(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(ushort value) => this.offset += MessagePackBinary.WriteUInt16(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt16(ushort value) => this.offset += MessagePackBinary.WriteUInt16ForceUInt16Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(short value) => this.offset += MessagePackBinary.WriteInt16(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt16(short value) => this.offset += MessagePackBinary.WriteInt16ForceInt16Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value) => this.offset += MessagePackBinary.WriteUInt32(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt32(uint value) => this.offset += MessagePackBinary.WriteUInt32ForceUInt32Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value) => this.offset += MessagePackBinary.WriteInt32(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt32(int value) => this.offset += MessagePackBinary.WriteInt32ForceInt32Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ulong value) => this.offset += MessagePackBinary.WriteUInt64(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.UInt64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt64(ulong value) => this.offset += MessagePackBinary.WriteUInt64ForceUInt64Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(long value) => this.offset += MessagePackBinary.WriteInt64(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt64(long value) => this.offset += MessagePackBinary.WriteInt64ForceInt64Block(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(bool value) => this.offset += MessagePackBinary.WriteBoolean(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(char value) => this.offset += MessagePackBinary.WriteChar(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(float value) => this.offset += MessagePackBinary.WriteSingle(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(double value) => this.offset += MessagePackBinary.WriteDouble(ref this.bytes, this.offset, value);

        /// <summary>
        /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The value to write.</param>
        public void Write(DateTime dateTime)
        {
            if (this.OldSpec)
            {
                throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(OldSpec)} mode. Turn off {nameof(OldSpec)} mode or use the {nameof(NativeDateTimeFormatter)}.");
            }
            else
            {
                this.offset += MessagePackBinary.WriteDateTime(ref this.bytes, this.offset, dateTime);
            }
        }

        /// <summary>
        /// Writes a span of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>,
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        public void Write(ReadOnlySpan<byte> src) => this.offset += this.OldSpec ? MessagePackBinary.WriteStringBytes(ref this.bytes, offset, src.ToArray()) : MessagePackBinary.WriteBytes(ref this.bytes, this.offset, src.ToArray());

        /// <summary>
        /// Writes a sequence of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>,
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        public void Write(ReadOnlySequence<byte> src) => this.offset += this.OldSpec ? MessagePackBinary.WriteStringBytes(ref this.bytes, offset, src.ToArray()) : MessagePackBinary.WriteBytes(ref this.bytes, this.offset, src.ToArray());

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(ReadOnlySequence<byte> utf8stringBytes) => this.offset += MessagePackBinary.WriteStringBytes(ref this.bytes, this.offset, utf8stringBytes.ToArray());

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(ReadOnlySpan<byte> utf8stringBytes) => this.offset += MessagePackBinary.WriteStringBytes(ref this.bytes, this.offset, utf8stringBytes.ToArray());

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="value">The value to write. Must not be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string value) => this.offset += MessagePackBinary.WriteString(ref this.bytes, this.offset, value, this.OldSpec);

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ReadOnlySpan<char> value) => this.offset += MessagePackBinary.WriteString(ref this.bytes, this.offset, value, this.OldSpec);

        /// <summary>
        /// Writes the extension format header, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionHeader">The extension header.</param>
        public void WriteExtensionFormatHeader(ExtensionHeader extensionHeader) => this.offset += MessagePackBinary.WriteExtensionFormatHeader(ref this.bytes, this.offset, extensionHeader.TypeCode, (int)extensionHeader.Length);

        /// <summary>
        /// Writes an extension format, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionData">The extension data.</param>
        public void WriteExtensionFormat(ExtensionResult extensionData) => this.offset += MessagePackBinary.WriteExtensionFormat(ref this.bytes, this.offset, extensionData.TypeCode, extensionData.Data.ToArray());

        internal Span<byte> GetSpan(int length)
        {
            MessagePackBinary.EnsureCapacity(ref this.bytes, this.offset, length);
            return this.bytes.AsSpan(this.offset);
        }

        internal void Advance(int length) => this.offset += length;
    }
}

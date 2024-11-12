// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePack
{
    /// <summary>
    /// A primitive types writer for the MessagePack format.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
    /// </remarks>
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    ref struct MessagePackWriter
    {
        /// <summary>
        /// The writer to use.
        /// </summary>
        private BufferWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
        /// </summary>
        /// <param name="writer">The writer to use.</param>
        public MessagePackWriter(IBufferWriter<byte> writer)
            : this()
        {
            this.writer = new BufferWriter(writer);
            this.OldSpec = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
        /// </summary>
        /// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
        /// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
        internal MessagePackWriter(SequencePool sequencePool, byte[] array)
            : this()
        {
            this.writer = new BufferWriter(sequencePool, array);
            this.OldSpec = false;
        }

        /// <summary>
        /// Gets or sets the cancellation token for this serialization operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to write in <see href="https://github.com/msgpack/msgpack/blob/master/spec-old.md">old spec</see> compatibility mode.
        /// </summary>
        public bool OldSpec { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter"/> struct,
        /// with the same settings as this one, but with its own buffer writer.
        /// </summary>
        /// <param name="writer">The writer to use for the new instance.</param>
        /// <returns>The new writer.</returns>
        public MessagePackWriter Clone(IBufferWriter<byte> writer) => new MessagePackWriter(writer)
        {
            OldSpec = this.OldSpec,
            CancellationToken = this.CancellationToken,
        };

        /// <summary>
        /// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
        /// </summary>
        public void Flush() => this.writer.Commit();

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        public void WriteNil()
        {
            Span<byte> span = this.writer.GetSpan(1);
            AssumesTrue(MessagePackPrimitives.TryWriteNil(span, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySpan<byte> rawMessagePackBlock) => this.writer.Write(rawMessagePackBlock);

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(in ReadOnlySequence<byte> rawMessagePackBlock)
        {
            foreach (ReadOnlyMemory<byte> segment in rawMessagePackBlock)
            {
                this.writer.Write(segment.Span);
            }
        }

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        public void WriteArrayHeader(int count) => this.WriteArrayHeader((uint)count);

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>.
        /// </summary>
        /// <param name="count">The number of elements that will be written in the array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(uint count)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWriteArrayHeader(span, count, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(int count) => this.WriteMapHeader((uint)count);

        /// <summary>
        /// Write the length of the next map to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixMap"/>,
        /// <see cref="MessagePackCode.Map16"/>, or
        /// <see cref="MessagePackCode.Map32"/>.
        /// </summary>
        /// <param name="count">The number of key=value pairs that will be written in the map.</param>
        public void WriteMapHeader(uint count)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWriteMapHeader(span, count, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(byte value)
        {
            Span<byte> span = this.writer.GetSpan(2);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt8(byte value)
        {
            Span<byte> span = this.writer.GetSpan(2);
            AssumesTrue(MessagePackPrimitives.TryWriteUInt8(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(sbyte value)
        {
            Span<byte> span = this.writer.GetSpan(2);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt8(sbyte value)
        {
            Span<byte> span = this.writer.GetSpan(2);
            AssumesTrue(MessagePackPrimitives.TryWriteInt8(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(ushort value)
        {
            Span<byte> span = this.writer.GetSpan(3);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt16(ushort value)
        {
            Span<byte> span = this.writer.GetSpan(3);
            AssumesTrue(MessagePackPrimitives.TryWriteUInt16(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(short value)
        {
            Span<byte> span = this.writer.GetSpan(3);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt16(short value)
        {
            Span<byte> span = this.writer.GetSpan(3);
            AssumesTrue(MessagePackPrimitives.TryWriteInt16(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt32(uint value)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWriteUInt32(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt32(int value)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWriteInt32(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ulong value)
        {
            Span<byte> span = this.writer.GetSpan(9);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt64(ulong value)
        {
            Span<byte> span = this.writer.GetSpan(9);
            AssumesTrue(MessagePackPrimitives.TryWriteUInt64(span, value, out int written));
            this.writer.Advance(written);
        }

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
        /// <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(long value)
        {
            Span<byte> span = this.writer.GetSpan(9);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt64(long value)
        {
            Span<byte> span = this.writer.GetSpan(9);
            AssumesTrue(MessagePackPrimitives.TryWriteInt64(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(bool value)
        {
            Span<byte> span = this.writer.GetSpan(1);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(char value)
        {
            Span<byte> span = this.writer.GetSpan(3);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(float value)
        {
            Span<byte> span = this.writer.GetSpan(5);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(double value)
        {
            Span<byte> span = this.writer.GetSpan(9);
            AssumesTrue(MessagePackPrimitives.TryWrite(span, value, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The value to write.</param>
        /// <exception cref="NotSupportedException">Thrown when <see cref="OldSpec"/> is true because the old spec does not define a <see cref="DateTime"/> format.</exception>
        public void Write(DateTime dateTime)
        {
            if (this.OldSpec)
            {
                throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(this.OldSpec)} mode. Turn off {nameof(this.OldSpec)} mode or use NativeDateTimeFormatter.");
            }
            else
            {
                Span<byte> span = this.writer.GetSpan(15);
                AssumesTrue(MessagePackPrimitives.TryWrite(span, dateTime, out int written));
                this.writer.Advance(written);
            }
        }

        /// <summary>
        /// Writes a <see cref="byte"/>[], prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>,
        /// <see cref="MessagePackCode.Bin32"/>,
        /// or <see cref="MessagePackCode.Nil"/> if <paramref name="src"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="src">The array of bytes to write. May be <see langword="null"/>.</param>
        public void Write(byte[]? src)
        {
            if (src == null)
            {
                this.WriteNil();
            }
            else
            {
                this.Write(src.AsSpan());
            }
        }

        /// <summary>
        /// Writes a span of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        /// <remarks>
        /// When <see cref="OldSpec"/> is <see langword="true"/>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </remarks>
        public void Write(ReadOnlySpan<byte> src)
        {
            int length = (int)src.Length;
            this.WriteBinHeader(length);
            var span = this.writer.GetSpan(length);
            src.CopyTo(span);
            this.writer.Advance(length);
        }

        /// <summary>
        /// Writes a sequence of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        /// <remarks>
        /// When <see cref="OldSpec"/> is <see langword="true"/>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </remarks>
        public void Write(in ReadOnlySequence<byte> src)
        {
            int length = (int)src.Length;
            this.WriteBinHeader(length);
            var span = this.writer.GetSpan(length);
            src.CopyTo(span);
            this.writer.Advance(length);
        }

        /// <summary>
        /// Writes the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>.
        /// </summary>
        /// <param name="length">The length of bytes that will be written next.</param>
        /// <remarks>
        /// <para>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </para>
        /// <para>
        /// When <see cref="OldSpec"/> is <see langword="true"/>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
        /// </para>
        /// </remarks>
        public void WriteBinHeader(int length)
        {
            if (this.OldSpec)
            {
                this.WriteStringHeader(length);
                return;
            }

            // When we write the header, we'll ask for all the space we need for the payload as well
            // as that may help ensure we only allocate a buffer once.
            Span<byte> span = this.writer.GetSpan(length + 5);
            AssumesTrue(MessagePackPrimitives.TryWriteBinHeader(span, (uint)length, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(in ReadOnlySequence<byte> utf8stringBytes)
        {
            var length = (int)utf8stringBytes.Length;
            this.WriteStringHeader(length);
            Span<byte> span = this.writer.GetSpan(length);
            utf8stringBytes.CopyTo(span);
            this.writer.Advance(length);
        }

        /// <summary>
        /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteString(ReadOnlySpan<byte> utf8stringBytes)
        {
            var length = utf8stringBytes.Length;
            this.WriteStringHeader(length);
            Span<byte> span = this.writer.GetSpan(length);
            utf8stringBytes.CopyTo(span);
            this.writer.Advance(length);
        }

        /// <summary>
        /// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>, or
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
        /// <remarks>
        /// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// after calling this method to actually write the content.
        /// Alternatively a single call to <see cref="WriteString(ReadOnlySpan{byte})"/> or <see cref="WriteString(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
        /// </remarks>
        public void WriteStringHeader(int byteCount)
        {
            // When we write the header, we'll ask for all the space we need for the payload as well
            // as that may help ensure we only allocate a buffer once.
            Span<byte> span = this.writer.GetSpan(byteCount + 5);
            AssumesTrue(MessagePackPrimitives.TryWriteStringHeader(span, (uint)byteCount, out int written));
            this.writer.Advance(written);
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// or <see cref="MessagePackCode.Nil"/> if the <paramref name="value"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to write. May be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(string? value)
        {
            if (value == null)
            {
                this.WriteNil();
                return;
            }

            ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
            fixed (char* pValue = value)
            fixed (byte* pBuffer = &buffer)
            {
                int byteCount = StringEncoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
                this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
            }
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public unsafe void Write(ReadOnlySpan<char> value)
        {
            ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
            fixed (char* pValue = value)
            fixed (byte* pBuffer = &buffer)
            {
                int byteCount = StringEncoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
                this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
            }
        }

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
        public void WriteExtensionFormatHeader(ExtensionHeader extensionHeader)
        {
            // When we write the header, we'll ask for all the space we need for the payload as well
            // as that may help ensure we only allocate a buffer once.
            Span<byte> span = this.writer.GetSpan((int)(extensionHeader.Length + 6));
            AssumesTrue(MessagePackPrimitives.TryWriteExtensionFormatHeader(span, extensionHeader, out int written));
            this.writer.Advance(written);
        }

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
        public void WriteExtensionFormat(ExtensionResult extensionData)
        {
            this.WriteExtensionFormatHeader(extensionData.Header);
            this.WriteRaw(extensionData.Data);
        }

        /// <summary>
        /// Gets memory where raw messagepack data can be written.
        /// </summary>
        /// <param name="length">The minimum length of the returned System.Span`1. If 0, a non-empty buffer is returned.</param>
        /// <returns>The span of memory to write to. This *may* exceed <paramref name="length"/>.</returns>
        /// <remarks>
        /// <para>After initializing the resulting memory, always follow up with a call to <see cref="Advance(int)"/>.</para>
        /// <para>
        /// This is similar in purpose to <see cref="WriteRaw(ReadOnlySpan{byte})"/>
        /// but provides uninitialized memory for the caller to write to instead of copying initialized memory from elsewhere.
        /// </para>
        /// </remarks>
        /// <seealso cref="IBufferWriter{T}.GetSpan(int)"/>
        public Span<byte> GetSpan(int length) => this.writer.GetSpan(length);

        /// <summary>
        /// Commits memory previously returned from <see cref="GetSpan(int)"/> as initialized.
        /// </summary>
        /// <param name="length">The number of bytes initialized with messagepack data from the previously returned span.</param>
        /// <seealso cref="IBufferWriter{T}.Advance(int)"/>
        public void Advance(int length) => this.writer.Advance(length);

        internal byte[] FlushAndGetArray()
        {
            if (this.writer.TryGetUncommittedSpan(out ReadOnlySpan<byte> span))
            {
                return span.ToArray();
            }
            else
            {
                if (this.writer.SequenceRental.Value == null)
                {
                    throw new NotSupportedException("This instance was not initialized to support this operation.");
                }

                this.Flush();
                byte[] result = this.writer.SequenceRental.Value.AsReadOnlySequence.ToArray();
                this.writer.SequenceRental.Dispose();
                return result;
            }
        }

        private static unsafe void WriteBigEndian(ushort value, byte* span)
        {
            unchecked
            {
                span[0] = (byte)(value >> 8);
                span[1] = (byte)value;
            }
        }

        private static unsafe void WriteBigEndian(uint value, byte* span)
        {
            unchecked
            {
                span[0] = (byte)(value >> 24);
                span[1] = (byte)(value >> 16);
                span[2] = (byte)(value >> 8);
                span[3] = (byte)value;
            }
        }

        /// <summary>
        /// Estimates the length of the header required for a given string.
        /// </summary>
        /// <param name="characterLength">The length of the string to be written, in characters.</param>
        /// <param name="bufferSize">Receives the guaranteed length of the returned buffer.</param>
        /// <param name="encodedBytesOffset">Receives the offset within the returned buffer to write the encoded string to.</param>
        /// <returns>
        /// A reference to the first byte in the buffer.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte WriteString_PrepareSpan(int characterLength, out int bufferSize, out int encodedBytesOffset)
        {
            // MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
            // solves heuristic length check

            // ensure buffer by MaxByteCount(faster than GetByteCount)
            bufferSize = StringEncoding.UTF8.GetMaxByteCount(characterLength) + 5;
            ref byte buffer = ref this.writer.GetPointer(bufferSize);

            int useOffset;
            if (characterLength <= MessagePackRange.MaxFixStringLength)
            {
                useOffset = 1;
            }
            else if (characterLength <= byte.MaxValue && !this.OldSpec)
            {
                useOffset = 2;
            }
            else if (characterLength <= ushort.MaxValue)
            {
                useOffset = 3;
            }
            else
            {
                useOffset = 5;
            }

            encodedBytesOffset = useOffset;
            return ref buffer;
        }

        /// <summary>
        /// Finalizes an encoding of a string.
        /// </summary>
        /// <param name="pBuffer">A pointer obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
        /// <param name="estimatedOffset">The offset obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
        /// <param name="byteCount">The number of bytes used to actually encode the string.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteString_PostEncoding(byte* pBuffer, int estimatedOffset, int byteCount)
        {
            // move body and write prefix
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (estimatedOffset != 1)
                {
                    Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 1, byteCount, byteCount);
                }

                pBuffer[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                this.writer.Advance(byteCount + 1);
            }
            else if (byteCount <= byte.MaxValue && !this.OldSpec)
            {
                if (estimatedOffset != 2)
                {
                    Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 2, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str8;
                pBuffer[1] = unchecked((byte)byteCount);
                this.writer.Advance(byteCount + 2);
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (estimatedOffset != 3)
                {
                    Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 3, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str16;
                WriteBigEndian((ushort)byteCount, pBuffer + 1);
                this.writer.Advance(byteCount + 3);
            }
            else
            {
                if (estimatedOffset != 5)
                {
                    Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 5, byteCount, byteCount);
                }

                pBuffer[0] = MessagePackCode.Str32;
                WriteBigEndian((uint)byteCount, pBuffer + 1);
                this.writer.Advance(byteCount + 5);
            }
        }

        /// <summary>
        /// Get the number of bytes required to encode a value in msgpack.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>The byte length; One of 1, 2, 3, 5 or 9 bytes.</returns>
        public static int GetEncodedLength(long value)
        {
            return value switch
            {
                >= 0 => value switch
                {
                    <= MessagePackRange.MaxFixPositiveInt => 1,
                    <= byte.MaxValue => 2,
                    <= ushort.MaxValue => 3,
                    <= uint.MaxValue => 5,
                    _ => 9,
                },
                _ => value switch
                {
                    >= MessagePackRange.MinFixNegativeInt => 1,
                    >= sbyte.MinValue => 2,
                    >= short.MinValue => 3,
                    >= int.MinValue => 5,
                    _ => 9,
                },
            };
        }

        /// <summary>
        /// Get the number of bytes required to encode a value in msgpack.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>The byte length; One of 1, 2, 3, 5 or 9 bytes.</returns>
        public static int GetEncodedLength(ulong value)
        {
            return value switch
            {
                > long.MaxValue => 9,
                _ => GetEncodedLength((long)value),
            };
        }

        private static void AssumesTrue([DoesNotReturnIf(false)] bool condition)
        {
            if (!condition)
            {
                throw new Exception("Internal error.");
            }
        }
    }
}

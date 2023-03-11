// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed class NativeGuidFormatter : IMessagePackFormatter<Guid>
    {
        /// <summary>
        /// Unsafe binary Guid formatter. this is only allowed on LittleEndian environment.
        /// </summary>
        public static readonly IMessagePackFormatter<Guid> Instance = new NativeGuidFormatter();

        private NativeGuidFormatter()
        {
        }

        /* Guid's underlying _a,...,_k field is sequential and same layout as .NET Framework and Mono(Unity).
         * But target machines must be same endian so restrict only for little endian. */

        public unsafe void Serialize(ref MessagePackWriter writer, Guid value, MessagePackSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
            }

            var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Guid));
            writer.Write(valueSpan);
        }

        public unsafe Guid Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
            }

            ReadOnlySequence<byte> valueSequence = reader.ReadBytes() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<Guid>();

            if (valueSequence.Length != sizeof(Guid))
            {
                throw new MessagePackSerializationException("Invalid Guid Size.");
            }

            Guid result;
            var resultSpan = new Span<byte>(&result, sizeof(Guid));
            valueSequence.CopyTo(resultSpan);
            return result;
        }
    }

    public sealed class NativeDecimalFormatter : IMessagePackFormatter<Decimal>
    {
        /// <summary>
        /// Unsafe binary Decimal formatter. this is only allows on LittleEndian environment.
        /// </summary>
        public static readonly IMessagePackFormatter<Decimal> Instance = new NativeDecimalFormatter();

        private NativeDecimalFormatter()
        {
        }

        /* decimal underlying "flags, hi, lo, mid" fields are sequential and same layuout with .NET Framework and Mono(Unity)
         * But target machines must be same endian so restrict only for little endian. */

        public unsafe void Serialize(ref MessagePackWriter writer, Decimal value, MessagePackSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
            }

            var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Decimal));
            writer.Write(valueSpan);
        }

        public unsafe Decimal Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
            }

            ReadOnlySequence<byte> valueSequence = reader.ReadBytes() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<decimal>();

            if (valueSequence.Length != sizeof(decimal))
            {
                throw new MessagePackSerializationException("Invalid decimal Size.");
            }

            decimal result;
            var resultSpan = new Span<byte>(&result, sizeof(decimal));
            valueSequence.CopyTo(resultSpan);
            return result;
        }
    }
}

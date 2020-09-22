﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed unsafe class UnsafeUnmanagedStructFormatter<T> : IMessagePackFormatter<T>
        where T : unmanaged
    {
        public const sbyte ExtensionTypeCode = 50;

        public static readonly UnsafeUnmanagedStructFormatter<T> Instance = new UnsafeUnmanagedStructFormatter<T>();

        private UnsafeUnmanagedStructFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            writer.WriteExtensionFormatHeader(new ExtensionHeader(ExtensionTypeCode, sizeof(T)));
            var span = writer.GetSpan(sizeof(T));
            Unsafe.As<byte, T>(ref span[0]) = value;
            writer.Advance(sizeof(T));
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != ExtensionTypeCode)
            {
                throw new MessagePackSerializationException("Extension TypeCode is invalid. typeCode: " + header.TypeCode);
            }

            if (header.Length != sizeof(T))
            {
                throw new MessagePackSerializationException("Extension Length is invalid. actual: " + header.Length + ", expected: " + sizeof(T));
            }

            var sequence = reader.ReadRaw(sizeof(T));
            if (sequence.IsSingleSegment)
            {
                return Unsafe.As<byte, T>(ref Unsafe.AsRef(sequence.FirstSpan[0]));
            }

            T answer;
            sequence.CopyTo(new Span<byte>(&answer, sizeof(T)));
            return answer;
        }
    }
}

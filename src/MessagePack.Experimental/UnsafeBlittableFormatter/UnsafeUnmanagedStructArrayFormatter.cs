﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed unsafe class UnsafeUnmanagedStructArrayFormatter<T> : IMessagePackFormatter<T[]?>
        where T : unmanaged
    {
        public const sbyte ExtensionTypeCode = 51;

        public static readonly UnsafeUnmanagedStructArrayFormatter<T> Instance = new UnsafeUnmanagedStructArrayFormatter<T>();

        private UnsafeUnmanagedStructArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, T[]? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var byteCount = sizeof(T) * value.Length;
            writer.WriteExtensionFormatHeader(new ExtensionHeader(ExtensionTypeCode, byteCount));
            if (byteCount == 0)
            {
                return;
            }

            var destinationSpan = writer.GetSpan(byteCount);
            fixed (void* destination = &destinationSpan[0])
            fixed (void* source = &value[0])
            {
                Buffer.MemoryCopy(source, destination, byteCount, byteCount);
            }

            writer.Advance(byteCount);
        }

        public T[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != ExtensionTypeCode)
            {
                throw new MessagePackSerializationException("Extension TypeCode is invalid. typeCode: " + header.TypeCode);
            }

            if (header.Length == 0)
            {
                return Array.Empty<T>();
            }

            var elementCount = header.Length / sizeof(T);
            if (elementCount * sizeof(T) != header.Length)
            {
                throw new MessagePackSerializationException("Extension Length is invalid. actual: " + header.Length + ", element size: " + sizeof(T));
            }

            var answer = new T[elementCount];
            reader.ReadRaw(header.Length).CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
            return answer;
        }
    }
}
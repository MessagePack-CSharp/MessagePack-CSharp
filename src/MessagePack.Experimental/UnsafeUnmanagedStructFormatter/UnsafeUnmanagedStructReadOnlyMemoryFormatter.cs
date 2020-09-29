// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed unsafe class UnsafeUnmanagedStructReadOnlyMemoryFormatter<T> : IMessagePackFormatter<ReadOnlyMemory<T>>
        where T : unmanaged
    {
#pragma warning disable SA1401 // Fields should be private
        public readonly sbyte TypeCode;
#pragma warning restore SA1401 // Fields should be private

        public UnsafeUnmanagedStructReadOnlyMemoryFormatter(sbyte typeCode)
        {
            TypeCode = typeCode;
        }

        public void Serialize(ref MessagePackWriter writer, ReadOnlyMemory<T> value, MessagePackSerializerOptions options)
        {
            var byteCount = sizeof(T) * value.Length;
            writer.WriteExtensionFormatHeader(new ExtensionHeader(TypeCode, byteCount));
            if (byteCount == 0)
            {
                return;
            }

            var destinationSpan = writer.GetSpan(byteCount);
            fixed (void* destination = &destinationSpan[0])
            fixed (void* source = &value.Span[0])
            {
                Buffer.MemoryCopy(source, destination, byteCount, byteCount);
            }

            writer.Advance(byteCount);
        }

        public ReadOnlyMemory<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return ReadOnlyMemory<T>.Empty;
            }

            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != TypeCode)
            {
                throw new MessagePackSerializationException("Extension TypeCode is invalid. typeCode: " + header.TypeCode);
            }

            if (header.Length == 0)
            {
                return ReadOnlyMemory<T>.Empty;
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

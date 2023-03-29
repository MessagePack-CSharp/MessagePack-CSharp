// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed unsafe class UnsafeUnmanagedStructReadOnlySequenceFormatter<T> : IMessagePackFormatter<ReadOnlySequence<T>>
        where T : unmanaged
    {
#pragma warning disable SA1401 // Fields should be private
        public readonly sbyte TypeCode;
#pragma warning restore SA1401 // Fields should be private

        public UnsafeUnmanagedStructReadOnlySequenceFormatter(sbyte typeCode)
        {
            TypeCode = typeCode;
        }

        public void Serialize(ref MessagePackWriter writer, ReadOnlySequence<T> value, MessagePackSerializerOptions options)
        {
            var byteCount = (int)(sizeof(T) * value.Length);
            writer.WriteExtensionFormatHeader(new ExtensionHeader(TypeCode, byteCount));
            if (byteCount == 0)
            {
                return;
            }

            var destinationSpan = writer.GetSpan(byteCount);
            fixed (void* destination = &destinationSpan[0])
            {
                if (value.IsSingleSegment)
                {
                    fixed (void* source = &value.FirstSpan[0])
                    {
                        Buffer.MemoryCopy(source, destination, byteCount, byteCount);
                    }
                }
                else
                {
                    var destinationIterator = (byte*)destination;
                    var destinationCapacity = byteCount;
                    foreach (var sourceMemory in value)
                    {
                        var sourceSpan = sourceMemory.Span;
                        fixed (void* source = &sourceSpan[0])
                        {
                            Buffer.MemoryCopy(source, destinationIterator, destinationCapacity, sourceSpan.Length);
                        }

                        destinationCapacity -= sourceSpan.Length;
                        destinationIterator += sourceSpan.Length;
                    }
                }
            }

            writer.Advance(byteCount);
        }

        public ReadOnlySequence<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return ReadOnlySequence<T>.Empty;
            }

            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != TypeCode)
            {
                throw new MessagePackSerializationException("Extension TypeCode is invalid. typeCode: " + header.TypeCode);
            }

            if (header.Length == 0)
            {
                return ReadOnlySequence<T>.Empty;
            }

            var elementCount = header.Length / sizeof(T);
            if (elementCount * sizeof(T) != header.Length)
            {
                throw new MessagePackSerializationException("Extension Length is invalid. actual: " + header.Length + ", element size: " + sizeof(T));
            }

            var answer = new T[elementCount];
            reader.ReadRaw(header.Length).CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
            return new ReadOnlySequence<T>(answer);
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace MessagePack.Formatters
{
    public sealed class GenericEnumFormatter<T> : IMessagePackFormatter<T>
        where T : struct, Enum
    {
        private delegate void EnumSerialize(ref MessagePackWriter writer, ref T value);

        private delegate T EnumDeserialize(ref MessagePackReader reader);

        private readonly EnumSerialize serializer;
        private readonly EnumDeserialize deserializer;

        public GenericEnumFormatter()
        {
            var underlyingType = typeof(T).GetEnumUnderlyingType();
            switch (Type.GetTypeCode(underlyingType))
            {
#pragma warning disable SA1107 // Avoid multiple statements on same line.
                case TypeCode.Byte:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, Byte>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadByte(); return Unsafe.As<Byte, T>(ref v); };
                    break;
                case TypeCode.Int16:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int16>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadInt16(); return Unsafe.As<Int16, T>(ref v); };
                    break;
                case TypeCode.Int32:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int32>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadInt32(); return Unsafe.As<Int32, T>(ref v); };
                    break;
                case TypeCode.Int64:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int64>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadInt64(); return Unsafe.As<Int64, T>(ref v); };
                    break;
                case TypeCode.SByte:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, SByte>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadSByte(); return Unsafe.As<SByte, T>(ref v); };
                    break;
                case TypeCode.UInt16:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt16>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadUInt16(); return Unsafe.As<UInt16, T>(ref v); };
                    break;
                case TypeCode.UInt32:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt32>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadUInt32(); return Unsafe.As<UInt32, T>(ref v); };
                    break;
                case TypeCode.UInt64:
                    serializer = (ref MessagePackWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt64>(ref value));
                    deserializer = (ref MessagePackReader reader) => { var v = reader.ReadUInt64(); return Unsafe.As<UInt64, T>(ref v); };
                    break;
                default:
                    throw new NotSupportedException("Unsupported base type for generic type argument.");
#pragma warning restore SA1107 // Avoid multiple statements on same line.
            }
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            serializer(ref writer, ref value);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return deserializer(ref reader);
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MessagePack.Formatters
{
    // Note:This implemenataion is 'not' fastest, should more improve.
    public sealed class EnumAsStringFormatter<T> : IMessagePackFormatter<T>
    {
        private readonly Dictionary<string, T> nameValueMapping;
        private readonly Dictionary<T, string> valueNameMapping;

        public EnumAsStringFormatter()
        {
            var names = Enum.GetNames(typeof(T));
            Array values = Enum.GetValues(typeof(T));

            this.nameValueMapping = new Dictionary<string, T>(names.Length);
            this.valueNameMapping = new Dictionary<T, string>(names.Length);

            for (int i = 0; i < names.Length; i++)
            {
                this.nameValueMapping[names[i]] = (T)values.GetValue(i);
                this.valueNameMapping[(T)values.GetValue(i)] = names[i];
            }
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            string name;
            if (!this.valueNameMapping.TryGetValue(value, out name))
            {
                name = value.ToString(); // fallback for flags etc, But Enum.ToString is too slow.
            }

            writer.Write(name);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var name = reader.ReadString();

            T value;
            if (!this.nameValueMapping.TryGetValue(name, out value))
            {
                value = (T)Enum.Parse(typeof(T), name); // Enum.Parse is too slow
            }

            return value;
        }
    }

    public sealed class GenericEnumFormatter<T> : IMessagePackFormatter<T>
        where T : Enum
    {
        delegate void EnumSerialize(ref MessagePackWriter writer, ref T value);
        delegate T EnumDeserialize(ref MessagePackReader reader);

        readonly EnumSerialize serializer;
        readonly EnumDeserialize deserializer;

        public GenericEnumFormatter()
        {
            var underlyingType = typeof(T).GetEnumUnderlyingType();
            switch (Type.GetTypeCode(underlyingType))
            {

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
                    break;
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

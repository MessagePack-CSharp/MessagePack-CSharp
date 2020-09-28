// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;

namespace MessagePack.Formatters
{
    public class PrimitiveObjectFormatter : IMessagePackFormatter<object>
    {
        public static readonly IMessagePackFormatter<object> Instance = new PrimitiveObjectFormatter();

        private static readonly Dictionary<Type, int> TypeToJumpCode = new Dictionary<Type, int>()
        {
            // When adding types whose size exceeds 32-bits, add support in MessagePackSecurity.GetHashCollisionResistantEqualityComparer<T>()
            { typeof(Boolean), 0 },
            { typeof(Char), 1 },
            { typeof(SByte), 2 },
            { typeof(Byte), 3 },
            { typeof(Int16), 4 },
            { typeof(UInt16), 5 },
            { typeof(Int32), 6 },
            { typeof(UInt32), 7 },
            { typeof(Int64), 8 },
            { typeof(UInt64), 9 },
            { typeof(Single), 10 },
            { typeof(Double), 11 },
            { typeof(DateTime), 12 },
            { typeof(string), 13 },
            { typeof(byte[]), 14 },
        };

        protected PrimitiveObjectFormatter()
        {
        }

        public static bool IsSupportedType(Type type, TypeInfo typeInfo, object value)
        {
            if (value == null)
            {
                return true;
            }

            if (TypeToJumpCode.ContainsKey(type))
            {
                return true;
            }

            if (typeInfo.IsEnum)
            {
                return true;
            }

            if (value is System.Collections.IDictionary)
            {
                return true;
            }

            if (value is System.Collections.ICollection)
            {
                return true;
            }

            return false;
        }

        public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            Type t = value.GetType();

            int code;
            if (TypeToJumpCode.TryGetValue(t, out code))
            {
                switch (code)
                {
                    case 0:
                        writer.Write((bool)value);
                        return;
                    case 1:
                        writer.Write((char)value);
                        return;
                    case 2:
                        writer.WriteInt8((sbyte)value);
                        return;
                    case 3:
                        writer.WriteUInt8((byte)value);
                        return;
                    case 4:
                        writer.WriteInt16((Int16)value);
                        return;
                    case 5:
                        writer.WriteUInt16((UInt16)value);
                        return;
                    case 6:
                        writer.WriteInt32((Int32)value);
                        return;
                    case 7:
                        writer.WriteUInt32((UInt32)value);
                        return;
                    case 8:
                        writer.WriteInt64((Int64)value);
                        return;
                    case 9:
                        writer.WriteUInt64((UInt64)value);
                        return;
                    case 10:
                        writer.Write((Single)value);
                        return;
                    case 11:
                        writer.Write((double)value);
                        return;
                    case 12:
                        writer.Write((DateTime)value);
                        return;
                    case 13:
                        writer.Write((string)value);
                        return;
                    case 14:
                        writer.Write((byte[])value);
                        return;
                    default:
                        throw new MessagePackSerializationException("Not supported primitive object resolver. type:" + t.Name);
                }
            }
            else
            {
#if UNITY_2018_3_OR_NEWER && !NETFX_CORE
                if (t.IsEnum)
#else
                if (t.GetTypeInfo().IsEnum)
#endif
                {
                    Type underlyingType = Enum.GetUnderlyingType(t);
                    var code2 = TypeToJumpCode[underlyingType];
                    switch (code2)
                    {
                        case 2:
                            writer.WriteInt8((sbyte)value);
                            return;
                        case 3:
                            writer.WriteUInt8((byte)value);
                            return;
                        case 4:
                            writer.WriteInt16((Int16)value);
                            return;
                        case 5:
                            writer.WriteUInt16((UInt16)value);
                            return;
                        case 6:
                            writer.WriteInt32((Int32)value);
                            return;
                        case 7:
                            writer.WriteUInt32((UInt32)value);
                            return;
                        case 8:
                            writer.WriteInt64((Int64)value);
                            return;
                        case 9:
                            writer.WriteUInt64((UInt64)value);
                            return;
                        default:
                            break;
                    }
                }
                else if (value is System.Collections.IDictionary)
                {
                    // check IDictionary first
                    var d = value as System.Collections.IDictionary;
                    writer.WriteMapHeader(d.Count);
                    foreach (System.Collections.DictionaryEntry item in d)
                    {
                        this.Serialize(ref writer, item.Key, options);
                        this.Serialize(ref writer, item.Value, options);
                    }

                    return;
                }
                else if (value is System.Collections.ICollection)
                {
                    var c = value as System.Collections.ICollection;
                    writer.WriteArrayHeader(c.Count);
                    foreach (var item in c)
                    {
                        this.Serialize(ref writer, item, options);
                    }

                    return;
                }
            }

            throw new MessagePackSerializationException("Not supported primitive object resolver. type:" + t.Name);
        }

        public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            MessagePackType type = reader.NextMessagePackType;
            IFormatterResolver resolver = options.Resolver;
            switch (type)
            {
                case MessagePackType.Integer:
                    var code = reader.NextCode;
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return reader.ReadSByte();
                    }
                    else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return reader.ReadByte();
                    }
                    else if (code == MessagePackCode.Int8)
                    {
                        return reader.ReadSByte();
                    }
                    else if (code == MessagePackCode.Int16)
                    {
                        return reader.ReadInt16();
                    }
                    else if (code == MessagePackCode.Int32)
                    {
                        return reader.ReadInt32();
                    }
                    else if (code == MessagePackCode.Int64)
                    {
                        return reader.ReadInt64();
                    }
                    else if (code == MessagePackCode.UInt8)
                    {
                        return reader.ReadByte();
                    }
                    else if (code == MessagePackCode.UInt16)
                    {
                        return reader.ReadUInt16();
                    }
                    else if (code == MessagePackCode.UInt32)
                    {
                        return reader.ReadUInt32();
                    }
                    else if (code == MessagePackCode.UInt64)
                    {
                        return reader.ReadUInt64();
                    }

                    throw new MessagePackSerializationException("Invalid primitive bytes.");
                case MessagePackType.Boolean:
                    return reader.ReadBoolean();
                case MessagePackType.Float:
                    if (reader.NextCode == MessagePackCode.Float32)
                    {
                        return reader.ReadSingle();
                    }
                    else
                    {
                        return reader.ReadDouble();
                    }

                case MessagePackType.String:
                    return reader.ReadString();
                case MessagePackType.Binary:
                    // We must copy the sequence returned by ReadBytes since the reader's sequence is only valid during deserialization.
                    return reader.ReadBytes()?.ToArray();
                case MessagePackType.Extension:
                    ExtensionHeader ext = reader.ReadExtensionFormatHeader();
                    if (ext.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        return reader.ReadDateTime(ext);
                    }

                    throw new MessagePackSerializationException("Invalid primitive bytes.");
                case MessagePackType.Array:
                    {
                        var length = reader.ReadArrayHeader();
                        if (length == 0)
                        {
                            return Array.Empty<object>();
                        }

                        IMessagePackFormatter<object> objectFormatter = resolver.GetFormatter<object>();
                        var array = new object[length];
                        options.Security.DepthStep(ref reader);
                        try
                        {
                            for (int i = 0; i < length; i++)
                            {
                                array[i] = objectFormatter.Deserialize(ref reader, options);
                            }
                        }
                        finally
                        {
                            reader.Depth--;
                        }

                        return array;
                    }

                case MessagePackType.Map:
                    {
                        var length = reader.ReadMapHeader();

                        options.Security.DepthStep(ref reader);
                        try
                        {
                            return this.DeserializeMap(ref reader, length, options);
                        }
                        finally
                        {
                            reader.Depth--;
                        }
                    }

                case MessagePackType.Nil:
                    reader.ReadNil();
                    return null;
                default:
                    throw new MessagePackSerializationException("Invalid primitive bytes.");
            }
        }

        protected virtual object DeserializeMap(ref MessagePackReader reader, int length, MessagePackSerializerOptions options)
        {
            IMessagePackFormatter<object> objectFormatter = options.Resolver.GetFormatter<object>();
            var dictionary = new Dictionary<object, object>(length, options.Security.GetEqualityComparer<object>());
            for (int i = 0; i < length; i++)
            {
                var key = objectFormatter.Deserialize(ref reader, options);
                var value = objectFormatter.Deserialize(ref reader, options);
                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
}

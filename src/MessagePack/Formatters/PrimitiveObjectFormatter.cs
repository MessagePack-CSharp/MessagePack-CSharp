using System;
using System.Reflection;
using System.Collections.Generic;

namespace MessagePack.Formatters
{
    public class PrimitiveObjectFormatter : IMessagePackFormatter<object>
    {
        public static readonly IMessagePackFormatter<object> Instance = new PrimitiveObjectFormatter();

        static readonly Dictionary<Type, int> typeToJumpCode = new Dictionary<Type, int>()
        {
            { typeof(Boolean), 0 },
            { typeof(Char), 1 },
            { typeof(SByte), 2 },
            { typeof(Byte), 3 },
            { typeof(Int16), 4 },
            { typeof(UInt16), 5 },
            { typeof(Int32), 6 },
            { typeof(UInt32), 7 },
            { typeof(Int64), 8 },
            { typeof(UInt64),9  },
            { typeof(Single), 10 },
            { typeof(Double), 11 },
            { typeof(DateTime), 12 },
            { typeof(string), 13 },
            { typeof(byte[]), 14 }
        };

        PrimitiveObjectFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var t = value.GetType();

            int code;
            if (typeToJumpCode.TryGetValue(t, out code))
            {
                switch (code)
                {
                    case 0:
                        return MessagePackBinary.WriteBoolean(ref bytes, offset, (bool)value);
                    case 1:
                        return MessagePackBinary.WriteChar(ref bytes, offset, (char)value);
                    case 2:
                        return MessagePackBinary.WriteSByteForceSByteBlock(ref bytes, offset, (sbyte)value);
                    case 3:
                        return MessagePackBinary.WriteByteForceByteBlock(ref bytes, offset, (byte)value);
                    case 4:
                        return MessagePackBinary.WriteInt16ForceInt16Block(ref bytes, offset, (Int16)value);
                    case 5:
                        return MessagePackBinary.WriteUInt16ForceUInt16Block(ref bytes, offset, (UInt16)value);
                    case 6:
                        return MessagePackBinary.WriteInt32ForceInt32Block(ref bytes, offset, (Int32)value);
                    case 7:
                        return MessagePackBinary.WriteUInt32ForceUInt32Block(ref bytes, offset, (UInt32)value);
                    case 8:
                        return MessagePackBinary.WriteInt64ForceInt64Block(ref bytes, offset, (Int64)value);
                    case 9:
                        return MessagePackBinary.WriteUInt64ForceUInt64Block(ref bytes, offset, (UInt64)value);
                    case 10:
                        return MessagePackBinary.WriteSingle(ref bytes, offset, (Single)value);
                    case 11:
                        return MessagePackBinary.WriteDouble(ref bytes, offset, (double)value);
                    case 12:
                        return MessagePackBinary.WriteDateTime(ref bytes, offset, (DateTime)value);
                    case 13:
                        return MessagePackBinary.WriteString(ref bytes, offset, (string)value);
                    case 14:
                        return MessagePackBinary.WriteBytes(ref bytes, offset, (byte[])value);
                    default:
                        throw new InvalidOperationException("Not supported primitive object resolver. type:" + t.Name);
                }
            }
            else
            {
                if (t.GetTypeInfo().IsEnum)
                {
                    var underlyingType = Enum.GetUnderlyingType(t);
                    var code2 = typeToJumpCode[underlyingType];
                    switch (code2)
                    {
                        case 2:
                            return MessagePackBinary.WriteSByteForceSByteBlock(ref bytes, offset, (sbyte)value);
                        case 3:
                            return MessagePackBinary.WriteByteForceByteBlock(ref bytes, offset, (byte)value);
                        case 4:
                            return MessagePackBinary.WriteInt16ForceInt16Block(ref bytes, offset, (Int16)value);
                        case 5:
                            return MessagePackBinary.WriteUInt16ForceUInt16Block(ref bytes, offset, (UInt16)value);
                        case 6:
                            return MessagePackBinary.WriteInt32ForceInt32Block(ref bytes, offset, (Int32)value);
                        case 7:
                            return MessagePackBinary.WriteUInt32ForceUInt32Block(ref bytes, offset, (UInt32)value);
                        case 8:
                            return MessagePackBinary.WriteInt64ForceInt64Block(ref bytes, offset, (Int64)value);
                        case 9:
                            return MessagePackBinary.WriteUInt64ForceUInt64Block(ref bytes, offset, (UInt64)value);
                        default:
                            break;
                    }
                }
                else if (value is System.Collections.IDictionary) // check IDictionary first
                {
                    var d = value as System.Collections.IDictionary;
                    var startOffset = offset;
                    offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, d.Count);
                    foreach (System.Collections.DictionaryEntry item in d)
                    {
                        offset += Serialize(ref bytes, offset, item.Key, formatterResolver);
                        offset += Serialize(ref bytes, offset, item.Value, formatterResolver);
                    }
                    return offset - startOffset;
                }
                else if (value is System.Collections.ICollection)
                {
                    var c = value as System.Collections.ICollection;
                    var startOffset = offset;
                    offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, c.Count);
                    foreach (var item in c)
                    {
                        offset += Serialize(ref bytes, offset, item, formatterResolver);
                    }
                    return offset - startOffset;
                }
            }

            throw new InvalidOperationException("Not supported primitive object resolver. type:" + t.Name);
        }

        public object Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var type = MessagePackBinary.GetMessagePackType(bytes, offset);
            switch (type)
            {
                case MessagePackType.Integer:
                    var code = bytes[offset];
                    if (MessagePackCode.MinNegativeFixInt <= code && code <= MessagePackCode.MaxNegativeFixInt) return MessagePackBinary.ReadSByte(bytes, offset, out readSize);
                    else if (MessagePackCode.MinFixInt <= code && code <= MessagePackCode.MaxFixInt) return MessagePackBinary.ReadByte(bytes, offset, out readSize);
                    else if (code == MessagePackCode.Int8) return MessagePackBinary.ReadSByte(bytes, offset, out readSize);
                    else if (code == MessagePackCode.Int16) return MessagePackBinary.ReadInt16(bytes, offset, out readSize);
                    else if (code == MessagePackCode.Int32) return MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                    else if (code == MessagePackCode.Int64) return MessagePackBinary.ReadInt64(bytes, offset, out readSize);
                    else if (code == MessagePackCode.UInt8) return MessagePackBinary.ReadByte(bytes, offset, out readSize);
                    else if (code == MessagePackCode.UInt16) return MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
                    else if (code == MessagePackCode.UInt32) return MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
                    else if (code == MessagePackCode.UInt64) return MessagePackBinary.ReadUInt64(bytes, offset, out readSize);
                    throw new InvalidOperationException("Invalid primitive bytes.");
                case MessagePackType.Boolean:
                    return MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
                case MessagePackType.Float:
                    if (MessagePackCode.Float32 == bytes[offset])
                    {
                        return MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                    }
                    else
                    {
                        return MessagePackBinary.ReadDouble(bytes, offset, out readSize);
                    }
                case MessagePackType.String:
                    return MessagePackBinary.ReadString(bytes, offset, out readSize);
                case MessagePackType.Binary:
                    return MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                case MessagePackType.Extension:
                    var ext = MessagePackBinary.ReadExtensionFormat(bytes, offset, out readSize);
                    if (ext.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        return MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
                    }
                    throw new InvalidOperationException("Invalid primitive bytes.");
                case MessagePackType.Array:
                    {
                        var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                        var startOffset = offset;
                        offset += readSize;

                        var array = new object[length];
                        for (int i = 0; i < length; i++)
                        {
                            array[i] = Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                        }

                        readSize = offset - startOffset;
                        return array;
                    }
                case MessagePackType.Map:
                    {
                        var length = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
                        var startOffset = offset;
                        offset += readSize;

                        var hash = new Dictionary<object, object>(length);
                        for (int i = 0; i < length; i++)
                        {
                            var key = Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;

                            var value = Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;

                            hash.Add(key, value);
                        }

                        readSize = offset - startOffset;
                        return hash;
                    }
                case MessagePackType.Nil:
                    readSize = 1;
                    return null;
                default:
                    throw new InvalidOperationException("Invalid primitive bytes.");
            }
        }
    }
}


namespace Test
{
    using System;
    using MessagePack;

    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        GeneratedResolver()
        {

        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(39)
            {
                {typeof(global::SharedData.ByteEnum), 0 },
                {typeof(global::System.Collections.Generic.List<global::SharedData.FirstSimpleData>), 1 },
                {typeof(global::System.Collections.Generic.Dictionary<int, global::SharedData.FirstSimpleData>), 2 },
                {typeof(global::System.Linq.ILookup<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>), 3 },
                {typeof(global::System.Linq.IGrouping<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>), 4 },
                {typeof(global::System.Collections.Generic.IList<string>), 5 },
                {typeof(global::System.Lazy<string>), 6 },
                {typeof(global::System.Collections.Concurrent.ConcurrentDictionary<int, string>), 7 },
                {typeof(global::System.Tuple<int, string>), 8 },
                {typeof(global::System.Tuple<int, string, global::SharedData.FirstSimpleData, global::SharedData.FirstSimpleData, int, int, int, int>), 9 },
                {typeof(global::SharedData.MySubUnion4?), 10 },
                {typeof(global::System.ArraySegment<int>), 11 },
                {typeof(global::System.ArraySegment<int>?), 12 },
                {typeof(global::System.Collections.Generic.KeyValuePair<int, int>), 13 },
                {typeof(global::SharedData.FirstSimpleData), 14 },
                {typeof(global::SharedData.SimlpeStringKeyData), 15 },
                {typeof(global::SharedData.SimpleStructIntKeyData), 16 },
                {typeof(global::SharedData.SimpleStructStringKeyData), 17 },
                {typeof(global::SharedData.SimpleIntKeyData), 18 },
                {typeof(global::SharedData.Vector2), 19 },
                {typeof(global::SharedData.EmptyClass), 20 },
                {typeof(global::SharedData.EmptyStruct), 21 },
                {typeof(global::SharedData.Version1), 22 },
                {typeof(global::SharedData.Version2), 23 },
                {typeof(global::SharedData.Version0), 24 },
                {typeof(global::SharedData.HolderV1), 25 },
                {typeof(global::SharedData.HolderV2), 26 },
                {typeof(global::SharedData.HolderV0), 27 },
                {typeof(global::SharedData.Callback1), 28 },
                {typeof(global::SharedData.Callback1_2), 29 },
                {typeof(global::SharedData.Callback2), 30 },
                {typeof(global::SharedData.Callback2_2), 31 },
                {typeof(global::SharedData.MySubUnion1), 32 },
                {typeof(global::SharedData.MySubUnion2), 33 },
                {typeof(global::SharedData.MySubUnion3), 34 },
                {typeof(global::SharedData.MySubUnion4), 35 },
                {typeof(global::SharedData.VersioningUnion), 36 },
                {typeof(global::SharedData.LivingPrimitive), 37 },
                {typeof(global::SharedData.DataIncludeCollection), 38 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key)) return null;

            switch (key)
            {
                case 0: return new global::SharedData.ByteEnumFormatter();
                case 1: return new global::MessagePack.Formatters.ListFormatter<global::SharedData.FirstSimpleData>();
                case 2: return new global::MessagePack.Formatters.DictionaryFormatter<int, global::SharedData.FirstSimpleData>();
                case 3: return new global::MessagePack.Formatters.InterfaceLookupFormatter<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>();
                case 4: return new global::MessagePack.Formatters.InterfaceGroupingFormatter<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>();
                case 5: return new global::MessagePack.Formatters.InterfaceListFormatter<string>();
                case 6: return new global::MessagePack.Formatters.LazyFormatter<string>();
                case 7: return new global::MessagePack.Formatters.ConcurrentDictionaryFormatter<int, string>();
                case 8: return new global::MessagePack.Formatters.TupleFormatter<int, string>();
                case 9: return new global::MessagePack.Formatters.TupleFormatter<int, string, global::SharedData.FirstSimpleData, global::SharedData.FirstSimpleData, int, int, int, int>();
                case 10: return new global::MessagePack.Formatters.NullableFormatter<global::SharedData.MySubUnion4>();
                case 11: return new global::MessagePack.Formatters.ArraySegmentFormatter<int>();
                case 12: return new global::MessagePack.Formatters.NullableFormatter<global::System.ArraySegment<int>>();
                case 13: return new global::MessagePack.Formatters.KeyValuePairFormatter<int, int>();
                case 14: return new global::SharedData.FirstSimpleDataFormatter();
                case 15: return new global::SharedData.SimlpeStringKeyDataFormatter();
                case 16: return new global::SharedData.SimpleStructIntKeyDataFormatter();
                case 17: return new global::SharedData.SimpleStructStringKeyDataFormatter();
                case 18: return new global::SharedData.SimpleIntKeyDataFormatter();
                case 19: return new global::SharedData.Vector2Formatter();
                case 20: return new global::SharedData.EmptyClassFormatter();
                case 21: return new global::SharedData.EmptyStructFormatter();
                case 22: return new global::SharedData.Version1Formatter();
                case 23: return new global::SharedData.Version2Formatter();
                case 24: return new global::SharedData.Version0Formatter();
                case 25: return new global::SharedData.HolderV1Formatter();
                case 26: return new global::SharedData.HolderV2Formatter();
                case 27: return new global::SharedData.HolderV0Formatter();
                case 28: return new global::SharedData.Callback1Formatter();
                case 29: return new global::SharedData.Callback1_2Formatter();
                case 30: return new global::SharedData.Callback2Formatter();
                case 31: return new global::SharedData.Callback2_2Formatter();
                case 32: return new global::SharedData.MySubUnion1Formatter();
                case 33: return new global::SharedData.MySubUnion2Formatter();
                case 34: return new global::SharedData.MySubUnion3Formatter();
                case 35: return new global::SharedData.MySubUnion4Formatter();
                case 36: return new global::SharedData.VersioningUnionFormatter();
                case 37: return new global::SharedData.LivingPrimitiveFormatter();
                case 38: return new global::SharedData.DataIncludeCollectionFormatter();
                default: return null;
            }
        }
    }
}


namespace SharedData
{
    using System;
    using MessagePack;

    public sealed class ByteEnumFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.ByteEnum>
    {
        public int Serialize(ref byte[] bytes, int offset, global::SharedData.ByteEnum value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteByte(ref bytes, offset, (Byte)value);
        }
        
        public global::SharedData.ByteEnum Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            return (global::SharedData.ByteEnum)MessagePackBinary.ReadByte(bytes, offset, out readSize);
        }
    }


}


namespace SharedData
{
    using System;
    using MessagePack;


    public sealed class FirstSimpleDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.FirstSimpleData>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.FirstSimpleData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Prop1);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Prop2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Prop3);
            return offset - startOffset;
        }

        public global::SharedData.FirstSimpleData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Prop1__ = default(int);
            var __Prop2__ = default(string);
            var __Prop3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Prop1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Prop2__ = MessagePackBinary.ReadString(bytes, offset, out readSize);
                        break;
                    case 2:
                        __Prop3__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.FirstSimpleData();
            ____result.Prop1 = __Prop1__;
            ____result.Prop2 = __Prop2__;
            ____result.Prop3 = __Prop3__;
            return ____result;
        }
    }


    public sealed class SimlpeStringKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimlpeStringKeyData>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public SimlpeStringKeyDataFormatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(3)
            {
                { "Prop1", 0},
                { "Prop2", 1},
                { "Prop3", 2},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.SimlpeStringKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Prop1", 5);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Prop1);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Prop2", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref bytes, offset, value.Prop2, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Prop3", 5);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Prop3);
            return offset - startOffset;
        }

        public global::SharedData.SimlpeStringKeyData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Prop1__ = default(int);
            var __Prop2__ = default(global::SharedData.ByteEnum);
            var __Prop3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __Prop1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __Prop3__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.SimlpeStringKeyData();
            ____result.Prop1 = __Prop1__;
            ____result.Prop2 = __Prop2__;
            ____result.Prop3 = __Prop3__;
            return ____result;
        }
    }


    public sealed class SimpleStructIntKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleStructIntKeyData>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.SimpleStructIntKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Y);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, value.BytesSpecial);
            return offset - startOffset;
        }

        public global::SharedData.SimpleStructIntKeyData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);
            var __Y__ = default(int);
            var __BytesSpecial__ = default(byte[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Y__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 2:
                        __BytesSpecial__ = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.SimpleStructIntKeyData();
            ____result.X = __X__;
            ____result.Y = __Y__;
            ____result.BytesSpecial = __BytesSpecial__;
            return ____result;
        }
    }


    public sealed class SimpleStructStringKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleStructStringKeyData>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public SimpleStructStringKeyDataFormatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(2)
            {
                { "key-X", 0},
                { "key-Y", 1},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.SimpleStructStringKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "key-X", 5);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "key-Y", 5);
            offset += formatterResolver.GetFormatterWithVerify<int[]>().Serialize(ref bytes, offset, value.Y, formatterResolver);
            return offset - startOffset;
        }

        public global::SharedData.SimpleStructStringKeyData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);
            var __Y__ = default(int[]);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Y__ = formatterResolver.GetFormatterWithVerify<int[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.SimpleStructStringKeyData();
            ____result.X = __X__;
            ____result.Y = __Y__;
            return ____result;
        }
    }


    public sealed class SimpleIntKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleIntKeyData>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.SimpleIntKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 6);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Prop1);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref bytes, offset, value.Prop2, formatterResolver);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Prop3);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.SimlpeStringKeyData>().Serialize(ref bytes, offset, value.Prop4, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Serialize(ref bytes, offset, value.Prop5, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Serialize(ref bytes, offset, value.Prop6, formatterResolver);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, value.BytesSpecial);
            return offset - startOffset;
        }

        public global::SharedData.SimpleIntKeyData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Prop1__ = default(int);
            var __Prop2__ = default(global::SharedData.ByteEnum);
            var __Prop3__ = default(string);
            var __Prop4__ = default(global::SharedData.SimlpeStringKeyData);
            var __Prop5__ = default(global::SharedData.SimpleStructIntKeyData);
            var __Prop6__ = default(global::SharedData.SimpleStructStringKeyData);
            var __BytesSpecial__ = default(byte[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Prop1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __Prop3__ = MessagePackBinary.ReadString(bytes, offset, out readSize);
                        break;
                    case 3:
                        __Prop4__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimlpeStringKeyData>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 4:
                        __Prop5__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 5:
                        __Prop6__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 6:
                        __BytesSpecial__ = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.SimpleIntKeyData();
            ____result.Prop1 = __Prop1__;
            ____result.Prop2 = __Prop2__;
            ____result.Prop3 = __Prop3__;
            ____result.Prop4 = __Prop4__;
            ____result.Prop5 = __Prop5__;
            ____result.Prop6 = __Prop6__;
            ____result.BytesSpecial = __BytesSpecial__;
            return ____result;
        }
    }


    public sealed class Vector2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Vector2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Vector2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.X);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.Y);
            return offset - startOffset;
        }

        public global::SharedData.Vector2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(float);
            var __Y__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __Y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Vector2();
            return ____result;
        }
    }


    public sealed class EmptyClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.EmptyClass>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.EmptyClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 0);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            return offset - startOffset;
        }

        public global::SharedData.EmptyClass Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;


            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.EmptyClass();
            return ____result;
        }
    }


    public sealed class EmptyStructFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.EmptyStruct>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.EmptyStruct value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 0);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            return offset - startOffset;
        }

        public global::SharedData.EmptyStruct Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;


            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.EmptyStruct();
            return ____result;
        }
    }


    public sealed class Version1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version1>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 5);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty1);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty3);
            return offset - startOffset;
        }

        public global::SharedData.Version1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __MyProperty1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 4:
                        __MyProperty2__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 5:
                        __MyProperty3__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Version1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            return ____result;
        }
    }


    public sealed class Version2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 7);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty1);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty3);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty5);
            return offset - startOffset;
        }

        public global::SharedData.Version2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);
            var __MyProperty5__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __MyProperty1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 4:
                        __MyProperty2__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 5:
                        __MyProperty3__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 7:
                        __MyProperty5__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Version2();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty5 = __MyProperty5__;
            return ____result;
        }
    }


    public sealed class Version0Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version0>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version0 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.MyProperty1);
            return offset - startOffset;
        }

        public global::SharedData.Version0 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __MyProperty1__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Version0();
            ____result.MyProperty1 = __MyProperty1__;
            return ____result;
        }
    }


    public sealed class HolderV1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV1>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.After);
            return offset - startOffset;
        }

        public global::SharedData.HolderV1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(global::SharedData.Version1);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __After__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.HolderV1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class HolderV2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.After);
            return offset - startOffset;
        }

        public global::SharedData.HolderV2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(global::SharedData.Version2);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __After__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.HolderV2();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class HolderV0Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV0>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV0 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.After);
            return offset - startOffset;
        }

        public global::SharedData.HolderV0 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(global::SharedData.Version0);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __After__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.HolderV0();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class Callback1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback1>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Callback1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            value.OnBeforeSerialize();
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 0);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            return offset - startOffset;
        }

        public global::SharedData.Callback1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Callback1(__X__);
            ____result.X = __X__;
			____result.OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback1_2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback1_2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Callback1_2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 0);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            return offset - startOffset;
        }

        public global::SharedData.Callback1_2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Callback1_2(__X__);
            ____result.X = __X__;
            ((IMessagePackSerializationCallbackReceiver)____result).OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback2>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public Callback2Formatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(1)
            {
                { "X", 0},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Callback2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            value.OnBeforeSerialize();
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 1);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "X", 1);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            return offset - startOffset;
        }

        public global::SharedData.Callback2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Callback2();
            ____result.X = __X__;
			____result.OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback2_2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback2_2>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public Callback2_2Formatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(1)
            {
                { "X", 0},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Callback2_2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 1);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "X", 1);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
            return offset - startOffset;
        }

        public global::SharedData.Callback2_2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __X__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.Callback2_2();
            ____result.X = __X__;
            ((IMessagePackSerializationCallbackReceiver)____result).OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class MySubUnion1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion1>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.MySubUnion1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.One);
            return offset - startOffset;
        }

        public global::SharedData.MySubUnion1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __One__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __One__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.MySubUnion1();
            ____result.One = __One__;
            return ____result;
        }
    }


    public sealed class MySubUnion2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.MySubUnion2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 5);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Two);
            return offset - startOffset;
        }

        public global::SharedData.MySubUnion2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Two__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 5:
                        __Two__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.MySubUnion2();
            ____result.Two = __Two__;
            return ____result;
        }
    }


    public sealed class MySubUnion3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion3>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.MySubUnion3 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Three);
            return offset - startOffset;
        }

        public global::SharedData.MySubUnion3 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Three__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 2:
                        __Three__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.MySubUnion3();
            ____result.Three = __Three__;
            return ____result;
        }
    }


    public sealed class MySubUnion4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion4>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.MySubUnion4 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 7);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Four);
            return offset - startOffset;
        }

        public global::SharedData.MySubUnion4 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Four__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 7:
                        __Four__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.MySubUnion4();
            ____result.Four = __Four__;
            return ____result;
        }
    }


    public sealed class VersioningUnionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.VersioningUnion>
    {

        public int Serialize(ref byte[] bytes, int offset, global::SharedData.VersioningUnion value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 7);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.FV);
            return offset - startOffset;
        }

        public global::SharedData.VersioningUnion Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __FV__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 7:
                        __FV__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.VersioningUnion();
            ____result.FV = __FV__;
            return ____result;
        }
    }


    public sealed class LivingPrimitiveFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.LivingPrimitive>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public LivingPrimitiveFormatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(16)
            {
                { "A1", 0},
                { "A2", 1},
                { "A3", 2},
                { "A4", 3},
                { "A5", 4},
                { "A6", 5},
                { "A7", 6},
                { "A8", 7},
                { "A9", 8},
                { "A10", 9},
                { "A11", 10},
                { "A12", 11},
                { "A13", 12},
                { "A14", 13},
                { "A15", 14},
                { "A16", 15},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.LivingPrimitive value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteMapHeader(ref bytes, offset, 16);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A1", 2);
            offset += MessagePackBinary.WriteInt16(ref bytes, offset, value.A1);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A2", 2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.A2);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A3", 2);
            offset += MessagePackBinary.WriteInt64(ref bytes, offset, value.A3);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A4", 2);
            offset += MessagePackBinary.WriteUInt16(ref bytes, offset, value.A4);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A5", 2);
            offset += MessagePackBinary.WriteUInt32(ref bytes, offset, value.A5);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A6", 2);
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.A6);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A7", 2);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.A7);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A8", 2);
            offset += MessagePackBinary.WriteDouble(ref bytes, offset, value.A8);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A9", 2);
            offset += MessagePackBinary.WriteBoolean(ref bytes, offset, value.A9);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A10", 3);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, value.A10);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A11", 3);
            offset += MessagePackBinary.WriteSByte(ref bytes, offset, value.A11);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A12", 3);
            offset += MessagePackBinary.WriteDateTime(ref bytes, offset, value.A12);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A13", 3);
            offset += MessagePackBinary.WriteChar(ref bytes, offset, value.A13);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A14", 3);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, value.A14);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A15", 3);
            offset += formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref bytes, offset, value.A15, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "A16", 3);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.A16);
            return offset - startOffset;
        }

        public global::SharedData.LivingPrimitive Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __A1__ = default(short);
            var __A2__ = default(int);
            var __A3__ = default(long);
            var __A4__ = default(ushort);
            var __A5__ = default(uint);
            var __A6__ = default(ulong);
            var __A7__ = default(float);
            var __A8__ = default(double);
            var __A9__ = default(bool);
            var __A10__ = default(byte);
            var __A11__ = default(sbyte);
            var __A12__ = default(global::System.DateTime);
            var __A13__ = default(char);
            var __A14__ = default(byte[]);
            var __A15__ = default(string[]);
            var __A16__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __A1__ = MessagePackBinary.ReadInt16(bytes, offset, out readSize);
                        break;
                    case 1:
                        __A2__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 2:
                        __A3__ = MessagePackBinary.ReadInt64(bytes, offset, out readSize);
                        break;
                    case 3:
                        __A4__ = MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
                        break;
                    case 4:
                        __A5__ = MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
                        break;
                    case 5:
                        __A6__ = MessagePackBinary.ReadUInt64(bytes, offset, out readSize);
                        break;
                    case 6:
                        __A7__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 7:
                        __A8__ = MessagePackBinary.ReadDouble(bytes, offset, out readSize);
                        break;
                    case 8:
                        __A9__ = MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
                        break;
                    case 9:
                        __A10__ = MessagePackBinary.ReadByte(bytes, offset, out readSize);
                        break;
                    case 10:
                        __A11__ = MessagePackBinary.ReadSByte(bytes, offset, out readSize);
                        break;
                    case 11:
                        __A12__ = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
                        break;
                    case 12:
                        __A13__ = MessagePackBinary.ReadChar(bytes, offset, out readSize);
                        break;
                    case 13:
                        __A14__ = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                        break;
                    case 14:
                        __A15__ = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 15:
                        __A16__ = MessagePackBinary.ReadString(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.LivingPrimitive(__A2__, __A10__);
            ____result.A1 = __A1__;
            ____result.A3 = __A3__;
            ____result.A4 = __A4__;
            ____result.A5 = __A5__;
            ____result.A6 = __A6__;
            ____result.A7 = __A7__;
            ____result.A8 = __A8__;
            ____result.A9 = __A9__;
            ____result.A11 = __A11__;
            ____result.A12 = __A12__;
            ____result.A13 = __A13__;
            ____result.A14 = __A14__;
            ____result.A15 = __A15__;
            ____result.A16 = __A16__;
            return ____result;
        }
    }


    public sealed class DataIncludeCollectionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.DataIncludeCollection>
    {

        readonly global::System.Collections.Generic.Dictionary<string, int> ____keyMapping;

        public DataIncludeCollectionFormatter()
        {
            this.____keyMapping = new global::System.Collections.Generic.Dictionary<string, int>(16)
            {
                { "Test1", 0},
                { "Test2", 1},
                { "Test3", 2},
                { "Test4", 3},
                { "Test5", 4},
                { "Test6", 5},
                { "Test7", 6},
                { "Test8", 7},
                { "Test9", 8},
                { "TestNullable", 9},
                { "TestNullable2", 10},
                { "S1", 11},
                { "S2", 12},
                { "S3", 13},
                { "S4", 14},
                { "S5", 15},
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::SharedData.DataIncludeCollection value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteMapHeader(ref bytes, offset, 16);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test1", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::SharedData.FirstSimpleData>>().Serialize(ref bytes, offset, value.Test1, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test2", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.FirstSimpleData[]>().Serialize(ref bytes, offset, value.Test2, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test3", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<int, global::SharedData.FirstSimpleData>>().Serialize(ref bytes, offset, value.Test3, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test4", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Linq.ILookup<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>>().Serialize(ref bytes, offset, value.Test4, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test5", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<string>>().Serialize(ref bytes, offset, value.Test5, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test6", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Lazy<string>>().Serialize(ref bytes, offset, value.Test6, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test7", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Concurrent.ConcurrentDictionary<int, string>>().Serialize(ref bytes, offset, value.Test7, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test8", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Tuple<int, string>>().Serialize(ref bytes, offset, value.Test8, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "Test9", 5);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Tuple<int, string, global::SharedData.FirstSimpleData, global::SharedData.FirstSimpleData, int, int, int, int>>().Serialize(ref bytes, offset, value.Test9, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "TestNullable", 12);
            offset += formatterResolver.GetFormatterWithVerify<int?>().Serialize(ref bytes, offset, value.TestNullable, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "TestNullable2", 13);
            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4?>().Serialize(ref bytes, offset, value.TestNullable2, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "S1", 2);
            offset += formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<int>>().Serialize(ref bytes, offset, value.S1, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "S2", 2);
            offset += formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<byte>>().Serialize(ref bytes, offset, value.S2, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "S3", 2);
            offset += formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<int>?>().Serialize(ref bytes, offset, value.S3, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "S4", 2);
            offset += formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<byte>?>().Serialize(ref bytes, offset, value.S4, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringUnsafe(ref bytes, offset, "S5", 2);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.KeyValuePair<int, int>>().Serialize(ref bytes, offset, value.S5, formatterResolver);
            return offset - startOffset;
        }

        public global::SharedData.DataIncludeCollection Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __Test1__ = default(global::System.Collections.Generic.List<global::SharedData.FirstSimpleData>);
            var __Test2__ = default(global::SharedData.FirstSimpleData[]);
            var __Test3__ = default(global::System.Collections.Generic.Dictionary<int, global::SharedData.FirstSimpleData>);
            var __Test4__ = default(global::System.Linq.ILookup<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>);
            var __Test5__ = default(global::System.Collections.Generic.IList<string>);
            var __Test6__ = default(global::System.Lazy<string>);
            var __Test7__ = default(global::System.Collections.Concurrent.ConcurrentDictionary<int, string>);
            var __Test8__ = default(global::System.Tuple<int, string>);
            var __Test9__ = default(global::System.Tuple<int, string, global::SharedData.FirstSimpleData, global::SharedData.FirstSimpleData, int, int, int, int>);
            var __TestNullable__ = default(int?);
            var __TestNullable2__ = default(global::SharedData.MySubUnion4?);
            var __S1__ = default(global::System.ArraySegment<int>);
            var __S2__ = default(global::System.ArraySegment<byte>);
            var __S3__ = default(global::System.ArraySegment<int>?);
            var __S4__ = default(global::System.ArraySegment<byte>?);
            var __S5__ = default(global::System.Collections.Generic.KeyValuePair<int, int>);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (____keyMapping.TryGetValue(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __Test1__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::SharedData.FirstSimpleData>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __Test2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.FirstSimpleData[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __Test3__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<int, global::SharedData.FirstSimpleData>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 3:
                        __Test4__ = formatterResolver.GetFormatterWithVerify<global::System.Linq.ILookup<global::SharedData.IntEnum, global::SharedData.FirstSimpleData>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 4:
                        __Test5__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<string>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 5:
                        __Test6__ = formatterResolver.GetFormatterWithVerify<global::System.Lazy<string>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 6:
                        __Test7__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Concurrent.ConcurrentDictionary<int, string>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 7:
                        __Test8__ = formatterResolver.GetFormatterWithVerify<global::System.Tuple<int, string>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 8:
                        __Test9__ = formatterResolver.GetFormatterWithVerify<global::System.Tuple<int, string, global::SharedData.FirstSimpleData, global::SharedData.FirstSimpleData, int, int, int, int>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 9:
                        __TestNullable__ = formatterResolver.GetFormatterWithVerify<int?>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 10:
                        __TestNullable2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4?>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 11:
                        __S1__ = formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<int>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 12:
                        __S2__ = formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<byte>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 13:
                        __S3__ = formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<int>?>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 14:
                        __S4__ = formatterResolver.GetFormatterWithVerify<global::System.ArraySegment<byte>?>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 15:
                        __S5__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.KeyValuePair<int, int>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::SharedData.DataIncludeCollection();
            ____result.Test1 = __Test1__;
            ____result.Test2 = __Test2__;
            ____result.Test3 = __Test3__;
            ____result.Test4 = __Test4__;
            ____result.Test5 = __Test5__;
            ____result.Test6 = __Test6__;
            ____result.Test7 = __Test7__;
            ____result.Test8 = __Test8__;
            ____result.Test9 = __Test9__;
            ____result.TestNullable = __TestNullable__;
            ____result.TestNullable2 = __TestNullable2__;
            ____result.S1 = __S1__;
            ____result.S2 = __S2__;
            ____result.S3 = __S3__;
            ____result.S4 = __S4__;
            ____result.S5 = __S5__;
            return ____result;
        }
    }

}

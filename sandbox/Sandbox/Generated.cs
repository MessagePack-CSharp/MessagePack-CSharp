
//namespace Test
//{
//    using System;
//    using MessagePack;

//    public class ComposittedResolver : global::MessagePack.IFormatterResolver
//    {
//        public static IFormatterResolver Instance = new ComposittedResolver();

//        ComposittedResolver()
//        {

//        }

//        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
//        {
//            return FormatterCache<T>.formatter;
//        }

//        static class FormatterCache<T>
//        {
//            public static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> formatter;

//            static FormatterCache()
//            {
//                var f = GeneratedResolver.Instance.GetFormatter<T>();
//                if (f != null)
//                {
//                    formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
//                    return;
//                }
//                formatter = MessagePack.Resolvers.DefaultResolver.Instance.GetFormatter<T>();
//            }
//        }
//    }

//    public class GeneratedResolver : global::MessagePack.IFormatterResolver
//    {
//        public static IFormatterResolver Instance = new GeneratedResolver();

//        GeneratedResolver()
//        {

//        }

//        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
//        {
//            return FormatterCache<T>.formatter;
//        }

//        static class FormatterCache<T>
//        {
//            public static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> formatter;

//            static FormatterCache()
//            {
//                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
//                if (f != null)
//                {
//                    formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
//                }
//            }
//        }
//    }

//    internal static class GeneratedResolverGetFormatterHelper
//    {
//        static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

//        static GeneratedResolverGetFormatterHelper()
//        {
//            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(7)
//            {
//                {typeof(global::SharedData.FirstSimpleData), 0 },
//                {typeof(global::SharedData.Version1), 1 },
//                {typeof(global::SharedData.Version2), 2 },
//                {typeof(global::SharedData.Version0), 3 },
//                {typeof(global::SharedData.HolderV1), 4 },
//                {typeof(global::SharedData.HolderV2), 5 },
//                {typeof(global::SharedData.HolderV0), 6 },
//            };
//        }

//        internal static object GetFormatter(Type t)
//        {
//            int key;
//            if (!lookup.TryGetValue(t, out key)) return null;

//            switch (key)
//            {
//                case 0: return new global::SharedData.FirstSimpleDataFormatter();
//                case 1: return new global::SharedData.Version1Formatter();
//                case 2: return new global::SharedData.Version2Formatter();
//                case 3: return new global::SharedData.Version0Formatter();
//                case 4: return new global::SharedData.HolderV1Formatter();
//                case 5: return new global::SharedData.HolderV2Formatter();
//                case 6: return new global::SharedData.HolderV0Formatter();
//                default: return null;
//            }
//        }
//    }
//}



//namespace SharedData
//{
//    using System;
//    using MessagePack;


//    public class FirstSimpleDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.FirstSimpleData>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.FirstSimpleData value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 3);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.Prop1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1);
//            offset += formatterResolver.GetFormatterWithVerify<string>().Serialize(ref bytes, offset, value.Prop2, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 2);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.Prop3, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.FirstSimpleData Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __Prop1__ = default(int);
//            var __Prop2__ = default(string);
//            var __Prop3__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 0:
//                        __Prop1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 1:
//                        __Prop2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 2:
//                        __Prop3__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.FirstSimpleData();
//            ____result.Prop1 = __Prop1__;
//            ____result.Prop2 = __Prop2__;
//            ____result.Prop3 = __Prop3__;
//            return ____result;
//        }
//    }


//    public class Version1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version1>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version1 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 3);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 340);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 101);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty2, formatterResolver);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 252);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty3, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.Version1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(int);
//            var __MyProperty2__ = default(int);
//            var __MyProperty3__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 340:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 101:
//                        __MyProperty2__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 252:
//                        __MyProperty3__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.Version1();
//            ____result.MyProperty1 = __MyProperty1__;
//            ____result.MyProperty2 = __MyProperty2__;
//            ____result.MyProperty3 = __MyProperty3__;
//            return ____result;
//        }
//    }


//    public class Version2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version2>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version2 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 5);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 340);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 101);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty2, formatterResolver);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 252);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty3, formatterResolver);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 3009);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty4, formatterResolver);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 201);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty5, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.Version2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(int);
//            var __MyProperty2__ = default(int);
//            var __MyProperty3__ = default(int);
//            var __MyProperty4__ = default(int);
//            var __MyProperty5__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 340:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 101:
//                        __MyProperty2__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 252:
//                        __MyProperty3__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 3009:
//                        __MyProperty4__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 201:
//                        __MyProperty5__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.Version2();
//            ____result.MyProperty1 = __MyProperty1__;
//            ____result.MyProperty2 = __MyProperty2__;
//            ____result.MyProperty3 = __MyProperty3__;
//            ____result.MyProperty4 = __MyProperty4__;
//            ____result.MyProperty5 = __MyProperty5__;
//            return ____result;
//        }
//    }


//    public class Version0Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version0>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.Version0 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 1);
//            offset += global::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, 340);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.Version0 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 340:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.Version0();
//            ____result.MyProperty1 = __MyProperty1__;
//            return ____result;
//        }
//    }


//    public class HolderV1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV1>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV1 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0);
//            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.After, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.HolderV1 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(global::SharedData.Version1);
//            var __After__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 0:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 1:
//                        __After__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.HolderV1();
//            ____result.MyProperty1 = __MyProperty1__;
//            ____result.After = __After__;
//            return ____result;
//        }
//    }


//    public class HolderV2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV2>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV2 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0);
//            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.After, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.HolderV2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(global::SharedData.Version2);
//            var __After__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 0:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 1:
//                        __After__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.HolderV2();
//            ____result.MyProperty1 = __MyProperty1__;
//            ____result.After = __After__;
//            return ____result;
//        }
//    }


//    public class HolderV0Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV0>
//    {

//        public int Serialize(ref byte[] bytes, int offset, global::SharedData.HolderV0 value, global::MessagePack.IFormatterResolver formatterResolver)
//        {
//            if (value == null)
//            {
//                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
//            }
            
//            var startOffset = offset;
//            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 0);
//            offset += formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Serialize(ref bytes, offset, value.MyProperty1, formatterResolver);
 
//            offset += global::MessagePack.MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1);
//            offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.After, formatterResolver);
//            return offset - startOffset;
//        }

//        public global::SharedData.HolderV0 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
//        {
//            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
//            {
//                readSize = 1;
//                return null;
//            }

//            var startOffset = offset;
//            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
//            offset += readSize;

//            var __MyProperty1__ = default(global::SharedData.Version0);
//            var __After__ = default(int);

//            for (int i = 0; i < length; i++)
//            {
//                var key = global::MessagePack.MessagePackBinary.ReadInt32(bytes, offset, out readSize);
//                offset += readSize;

//                switch (key)
//                {
//                    case 0:
//                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    case 1:
//                        __After__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
//                        break;
//                    default:
//                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
//                        break;
//                }
//                offset += readSize;
//            }

//            readSize = offset - startOffset;

//            var ____result = new global::SharedData.HolderV0();
//            ____result.MyProperty1 = __MyProperty1__;
//            ____result.After = __After__;
//            return ____result;
//        }
//    }

//}

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Resolvers
{
    using System;
    using MessagePack;

    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

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
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(5)
            {
                {typeof(global::System.Collections.Generic.List<global::TestData2.A>), 0 },
                {typeof(global::System.Collections.Generic.List<global::TestData2.B>), 1 },
                {typeof(global::TestData2.C), 2 },
                {typeof(global::TestData2.B), 3 },
                {typeof(global::TestData2.A), 4 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key)) return null;

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.ListFormatter<global::TestData2.A>();
                case 1: return new global::MessagePack.Formatters.ListFormatter<global::TestData2.B>();
                case 2: return new MessagePack.Formatters.TestData2.CFormatter();
                case 3: return new MessagePack.Formatters.TestData2.BFormatter();
                case 4: return new MessagePack.Formatters.TestData2.AFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning disable 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612



#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.TestData2
{
    using System;
    using MessagePack;


    public sealed class CFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.C>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public CFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "b", 0},
                { "a", 1},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::System.Text.Encoding.UTF8.GetBytes("b"),
                global::System.Text.Encoding.UTF8.GetBytes("a"),
                
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::TestData2.C value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[0]);
            offset += formatterResolver.GetFormatterWithVerify<global::TestData2.B>().Serialize(ref bytes, offset, value.b, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[1]);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.a);
            return offset - startOffset;
        }

        public global::TestData2.C Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __b__ = default(global::TestData2.B);
            var __a__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (!____keyMapping.TryGetValueSafe(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __b__ = formatterResolver.GetFormatterWithVerify<global::TestData2.B>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __a__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::TestData2.C();
            ____result.b = __b__;
            ____result.a = __a__;
            return ____result;
        }
    }


    public sealed class BFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.B>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public BFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "ass", 0},
                { "c", 1},
                { "a", 2},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::System.Text.Encoding.UTF8.GetBytes("ass"),
                global::System.Text.Encoding.UTF8.GetBytes("c"),
                global::System.Text.Encoding.UTF8.GetBytes("a"),
                
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::TestData2.B value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[0]);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>().Serialize(ref bytes, offset, value.ass, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[1]);
            offset += formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Serialize(ref bytes, offset, value.c, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[2]);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.a);
            return offset - startOffset;
        }

        public global::TestData2.B Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __ass__ = default(global::System.Collections.Generic.List<global::TestData2.A>);
            var __c__ = default(global::TestData2.C);
            var __a__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (!____keyMapping.TryGetValueSafe(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __ass__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __c__ = formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __a__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::TestData2.B();
            ____result.ass = __ass__;
            ____result.c = __c__;
            ____result.a = __a__;
            return ____result;
        }
    }


    public sealed class AFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.A>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public AFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "a", 0},
                { "bs", 1},
                { "c", 2},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::System.Text.Encoding.UTF8.GetBytes("a"),
                global::System.Text.Encoding.UTF8.GetBytes("bs"),
                global::System.Text.Encoding.UTF8.GetBytes("c"),
                
            };
        }


        public int Serialize(ref byte[] bytes, int offset, global::TestData2.A value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }
            
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[0]);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.a);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[1]);
            offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>().Serialize(ref bytes, offset, value.bs, formatterResolver);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, this.____stringByteKeys[2]);
            offset += formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Serialize(ref bytes, offset, value.c, formatterResolver);
            return offset - startOffset;
        }

        public global::TestData2.A Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            var __a__ = default(int);
            var __bs__ = default(global::System.Collections.Generic.List<global::TestData2.B>);
            var __c__ = default(global::TestData2.C);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
                offset += readSize;
                int key;
                if (!____keyMapping.TryGetValueSafe(stringKey, out key))
                {
                    readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                    goto NEXT_LOOP;
                }

                switch (key)
                {
                    case 0:
                        __a__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __bs__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __c__ = formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                
                NEXT_LOOP:
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::TestData2.A();
            ____result.a = __a__;
            ____result.bs = __bs__;
            ____result.c = __c__;
            return ____result;
        }
    }

}

#pragma warning disable 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

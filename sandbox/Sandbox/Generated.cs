﻿#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Resolvers
{
    using System;
    using System.Buffers;
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
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(65)
            {
                {typeof(int[,]), 0 },
                {typeof(global::GlobalMyEnum[,]), 1 },
                {typeof(int[,,]), 2 },
                {typeof(int[,,,]), 3 },
                {typeof(global::GlobalMyEnum[]), 4 },
                {typeof(global::QuestMessageBody[]), 5 },
                {typeof(global::System.Collections.Generic.IDictionary<string, string>), 6 },
                {typeof(global::System.Collections.Generic.IList<global::SimpleModel>), 7 },
                {typeof(global::SharedData.ByteEnum), 8 },
                {typeof(global::GlobalMyEnum), 9 },
                {typeof(global::SharedData.IUnionChecker), 10 },
                {typeof(global::SharedData.IUnionChecker2), 11 },
                {typeof(global::SharedData.IIVersioningUnion), 12 },
                {typeof(global::SharedData.RootUnionType), 13 },
                {typeof(global::SharedData.IUnionSample), 14 },
                {typeof(global::IMessageBody), 15 },
                {typeof(global::SharedData.FirstSimpleData), 16 },
                {typeof(global::SharedData.SimpleStringKeyData), 17 },
                {typeof(global::SharedData.SimpleStructIntKeyData), 18 },
                {typeof(global::SharedData.SimpleStructStringKeyData), 19 },
                {typeof(global::SharedData.SimpleIntKeyData), 20 },
                {typeof(global::SharedData.Vector2), 21 },
                {typeof(global::SharedData.EmptyClass), 22 },
                {typeof(global::SharedData.EmptyStruct), 23 },
                {typeof(global::SharedData.Version1), 24 },
                {typeof(global::SharedData.Version2), 25 },
                {typeof(global::SharedData.Version0), 26 },
                {typeof(global::SharedData.HolderV1), 27 },
                {typeof(global::SharedData.HolderV2), 28 },
                {typeof(global::SharedData.HolderV0), 29 },
                {typeof(global::SharedData.Callback1), 30 },
                {typeof(global::SharedData.Callback1_2), 31 },
                {typeof(global::SharedData.Callback2), 32 },
                {typeof(global::SharedData.Callback2_2), 33 },
                {typeof(global::SharedData.SubUnionType1), 34 },
                {typeof(global::SharedData.SubUnionType2), 35 },
                {typeof(global::SharedData.MySubUnion1), 36 },
                {typeof(global::SharedData.MySubUnion2), 37 },
                {typeof(global::SharedData.MySubUnion3), 38 },
                {typeof(global::SharedData.MySubUnion4), 39 },
                {typeof(global::SharedData.VersioningUnion), 40 },
                {typeof(global::SharedData.MyClass), 41 },
                {typeof(global::SharedData.VersionBlockTest), 42 },
                {typeof(global::SharedData.UnVersionBlockTest), 43 },
                {typeof(global::SharedData.Empty1), 44 },
                {typeof(global::SharedData.Empty2), 45 },
                {typeof(global::SharedData.NonEmpty1), 46 },
                {typeof(global::SharedData.NonEmpty2), 47 },
                {typeof(global::SharedData.VectorLike2), 48 },
                {typeof(global::SharedData.Vector3Like), 49 },
                {typeof(global::SharedData.ArrayOptimizeClass), 50 },
                {typeof(global::SharedData.NestParent.NestContract), 51 },
                {typeof(global::SharedData.FooClass), 52 },
                {typeof(global::SharedData.BarClass), 53 },
                {typeof(global::SharedData.WithIndexer), 54 },
                {typeof(global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga), 55 },
                {typeof(global::GlobalMan), 56 },
                {typeof(global::Message), 57 },
                {typeof(global::TextMessageBody), 58 },
                {typeof(global::StampMessageBody), 59 },
                {typeof(global::QuestMessageBody), 60 },
                {typeof(global::ArrayTestTest), 61 },
                {typeof(global::SimpleModel), 62 },
                {typeof(global::ComplexModel), 63 },
                {typeof(global::PerfBenchmarkDotNet.StringKeySerializerTarget), 64 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key)) return null;

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.TwoDimentionalArrayFormatter<int>();
                case 1: return new global::MessagePack.Formatters.TwoDimentionalArrayFormatter<global::GlobalMyEnum>();
                case 2: return new global::MessagePack.Formatters.ThreeDimentionalArrayFormatter<int>();
                case 3: return new global::MessagePack.Formatters.FourDimentionalArrayFormatter<int>();
                case 4: return new global::MessagePack.Formatters.ArrayFormatter<global::GlobalMyEnum>();
                case 5: return new global::MessagePack.Formatters.ArrayFormatter<global::QuestMessageBody>();
                case 6: return new global::MessagePack.Formatters.InterfaceDictionaryFormatter<string, string>();
                case 7: return new global::MessagePack.Formatters.InterfaceListFormatter<global::SimpleModel>();
                case 8: return new MessagePack.Formatters.SharedData.ByteEnumFormatter();
                case 9: return new MessagePack.Formatters.GlobalMyEnumFormatter();
                case 10: return new MessagePack.Formatters.SharedData.IUnionCheckerFormatter();
                case 11: return new MessagePack.Formatters.SharedData.IUnionChecker2Formatter();
                case 12: return new MessagePack.Formatters.SharedData.IIVersioningUnionFormatter();
                case 13: return new MessagePack.Formatters.SharedData.RootUnionTypeFormatter();
                case 14: return new MessagePack.Formatters.SharedData.IUnionSampleFormatter();
                case 15: return new MessagePack.Formatters.IMessageBodyFormatter();
                case 16: return new MessagePack.Formatters.SharedData.FirstSimpleDataFormatter();
                case 17: return new MessagePack.Formatters.SharedData.SimpleStringKeyDataFormatter();
                case 18: return new MessagePack.Formatters.SharedData.SimpleStructIntKeyDataFormatter();
                case 19: return new MessagePack.Formatters.SharedData.SimpleStructStringKeyDataFormatter();
                case 20: return new MessagePack.Formatters.SharedData.SimpleIntKeyDataFormatter();
                case 21: return new MessagePack.Formatters.SharedData.Vector2Formatter();
                case 22: return new MessagePack.Formatters.SharedData.EmptyClassFormatter();
                case 23: return new MessagePack.Formatters.SharedData.EmptyStructFormatter();
                case 24: return new MessagePack.Formatters.SharedData.Version1Formatter();
                case 25: return new MessagePack.Formatters.SharedData.Version2Formatter();
                case 26: return new MessagePack.Formatters.SharedData.Version0Formatter();
                case 27: return new MessagePack.Formatters.SharedData.HolderV1Formatter();
                case 28: return new MessagePack.Formatters.SharedData.HolderV2Formatter();
                case 29: return new MessagePack.Formatters.SharedData.HolderV0Formatter();
                case 30: return new MessagePack.Formatters.SharedData.Callback1Formatter();
                case 31: return new MessagePack.Formatters.SharedData.Callback1_2Formatter();
                case 32: return new MessagePack.Formatters.SharedData.Callback2Formatter();
                case 33: return new MessagePack.Formatters.SharedData.Callback2_2Formatter();
                case 34: return new MessagePack.Formatters.SharedData.SubUnionType1Formatter();
                case 35: return new MessagePack.Formatters.SharedData.SubUnionType2Formatter();
                case 36: return new MessagePack.Formatters.SharedData.MySubUnion1Formatter();
                case 37: return new MessagePack.Formatters.SharedData.MySubUnion2Formatter();
                case 38: return new MessagePack.Formatters.SharedData.MySubUnion3Formatter();
                case 39: return new MessagePack.Formatters.SharedData.MySubUnion4Formatter();
                case 40: return new MessagePack.Formatters.SharedData.VersioningUnionFormatter();
                case 41: return new MessagePack.Formatters.SharedData.MyClassFormatter();
                case 42: return new MessagePack.Formatters.SharedData.VersionBlockTestFormatter();
                case 43: return new MessagePack.Formatters.SharedData.UnVersionBlockTestFormatter();
                case 44: return new MessagePack.Formatters.SharedData.Empty1Formatter();
                case 45: return new MessagePack.Formatters.SharedData.Empty2Formatter();
                case 46: return new MessagePack.Formatters.SharedData.NonEmpty1Formatter();
                case 47: return new MessagePack.Formatters.SharedData.NonEmpty2Formatter();
                case 48: return new MessagePack.Formatters.SharedData.VectorLike2Formatter();
                case 49: return new MessagePack.Formatters.SharedData.Vector3LikeFormatter();
                case 50: return new MessagePack.Formatters.SharedData.ArrayOptimizeClassFormatter();
                case 51: return new MessagePack.Formatters.SharedData.NestParent_NestContractFormatter();
                case 52: return new MessagePack.Formatters.SharedData.FooClassFormatter();
                case 53: return new MessagePack.Formatters.SharedData.BarClassFormatter();
                case 54: return new MessagePack.Formatters.SharedData.WithIndexerFormatter();
                case 55: return new MessagePack.Formatters.Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqagaFormatter();
                case 56: return new MessagePack.Formatters.GlobalManFormatter();
                case 57: return new MessagePack.Formatters.MessageFormatter();
                case 58: return new MessagePack.Formatters.TextMessageBodyFormatter();
                case 59: return new MessagePack.Formatters.StampMessageBodyFormatter();
                case 60: return new MessagePack.Formatters.QuestMessageBodyFormatter();
                case 61: return new MessagePack.Formatters.ArrayTestTestFormatter();
                case 62: return new MessagePack.Formatters.SimpleModelFormatter();
                case 63: return new MessagePack.Formatters.ComplexModelFormatter();
                case 64: return new MessagePack.Formatters.PerfBenchmarkDotNet.StringKeySerializerTargetFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.SharedData
{
    using System;
	using System.Buffers;
    using MessagePack;

    public sealed class ByteEnumFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.ByteEnum>
    {
        public void Serialize(ref MessagePackWriter writer, global::SharedData.ByteEnum value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.Write((Byte)value);
        }
        
        public global::SharedData.ByteEnum Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            return (global::SharedData.ByteEnum)reader.ReadByte();
        }
    }


}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612
#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters
{
    using System;
	using System.Buffers;
    using MessagePack;

    public sealed class GlobalMyEnumFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::GlobalMyEnum>
    {
        public void Serialize(ref MessagePackWriter writer, global::GlobalMyEnum value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.Write((Int32)value);
        }
        
        public global::GlobalMyEnum Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            return (global::GlobalMyEnum)reader.ReadInt32();
        }
    }


}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.SharedData
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class IUnionCheckerFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.IUnionChecker>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public IUnionCheckerFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(4, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::SharedData.MySubUnion1).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::SharedData.MySubUnion2).TypeHandle, new KeyValuePair<int, int>(1, 1) },
                { typeof(global::SharedData.MySubUnion3).TypeHandle, new KeyValuePair<int, int>(2, 2) },
                { typeof(global::SharedData.MySubUnion4).TypeHandle, new KeyValuePair<int, int>(3, 3) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(4)
            {
                { 0, 0 },
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionChecker value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, formatterResolver);
                        break;
                    case 1:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Serialize(ref writer, (global::SharedData.MySubUnion2)value, formatterResolver);
                        break;
                    case 2:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Serialize(ref writer, (global::SharedData.MySubUnion3)value, formatterResolver);
                        break;
                    case 3:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Serialize(ref writer, (global::SharedData.MySubUnion4)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionChecker Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::SharedData.IUnionChecker");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::SharedData.IUnionChecker result = null;
            switch (key)
            {
                case 0:
                    result = (global::SharedData.IUnionChecker)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, formatterResolver);
                    break;
                case 1:
                    result = (global::SharedData.IUnionChecker)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Deserialize(ref reader, formatterResolver);
                    break;
                case 2:
                    result = (global::SharedData.IUnionChecker)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Deserialize(ref reader, formatterResolver);
                    break;
                case 3:
                    result = (global::SharedData.IUnionChecker)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }

    public sealed class IUnionChecker2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.IUnionChecker2>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public IUnionChecker2Formatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(4, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::SharedData.MySubUnion2).TypeHandle, new KeyValuePair<int, int>(31, 0) },
                { typeof(global::SharedData.MySubUnion3).TypeHandle, new KeyValuePair<int, int>(42, 1) },
                { typeof(global::SharedData.MySubUnion4).TypeHandle, new KeyValuePair<int, int>(63, 2) },
                { typeof(global::SharedData.MySubUnion1).TypeHandle, new KeyValuePair<int, int>(120, 3) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(4)
            {
                { 31, 0 },
                { 42, 1 },
                { 63, 2 },
                { 120, 3 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionChecker2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Serialize(ref writer, (global::SharedData.MySubUnion2)value, formatterResolver);
                        break;
                    case 1:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Serialize(ref writer, (global::SharedData.MySubUnion3)value, formatterResolver);
                        break;
                    case 2:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Serialize(ref writer, (global::SharedData.MySubUnion4)value, formatterResolver);
                        break;
                    case 3:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionChecker2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::SharedData.IUnionChecker2");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::SharedData.IUnionChecker2 result = null;
            switch (key)
            {
                case 0:
                    result = (global::SharedData.IUnionChecker2)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Deserialize(ref reader, formatterResolver);
                    break;
                case 1:
                    result = (global::SharedData.IUnionChecker2)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Deserialize(ref reader, formatterResolver);
                    break;
                case 2:
                    result = (global::SharedData.IUnionChecker2)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Deserialize(ref reader, formatterResolver);
                    break;
                case 3:
                    result = (global::SharedData.IUnionChecker2)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }

    public sealed class IIVersioningUnionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.IIVersioningUnion>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public IIVersioningUnionFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(1, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::SharedData.MySubUnion1).TypeHandle, new KeyValuePair<int, int>(0, 0) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(1)
            {
                { 0, 0 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IIVersioningUnion value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IIVersioningUnion Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::SharedData.IIVersioningUnion");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::SharedData.IIVersioningUnion result = null;
            switch (key)
            {
                case 0:
                    result = (global::SharedData.IIVersioningUnion)formatterResolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }

    public sealed class RootUnionTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.RootUnionType>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public RootUnionTypeFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(2, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::SharedData.SubUnionType1).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::SharedData.SubUnionType2).TypeHandle, new KeyValuePair<int, int>(1, 1) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(2)
            {
                { 0, 0 },
                { 1, 1 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.RootUnionType value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.SubUnionType1>().Serialize(ref writer, (global::SharedData.SubUnionType1)value, formatterResolver);
                        break;
                    case 1:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.SubUnionType2>().Serialize(ref writer, (global::SharedData.SubUnionType2)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.RootUnionType Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::SharedData.RootUnionType");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::SharedData.RootUnionType result = null;
            switch (key)
            {
                case 0:
                    result = (global::SharedData.RootUnionType)formatterResolver.GetFormatterWithVerify<global::SharedData.SubUnionType1>().Deserialize(ref reader, formatterResolver);
                    break;
                case 1:
                    result = (global::SharedData.RootUnionType)formatterResolver.GetFormatterWithVerify<global::SharedData.SubUnionType2>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }

    public sealed class IUnionSampleFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.IUnionSample>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public IUnionSampleFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(2, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::SharedData.FooClass).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::SharedData.BarClass).TypeHandle, new KeyValuePair<int, int>(100, 1) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(2)
            {
                { 0, 0 },
                { 100, 1 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionSample value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.FooClass>().Serialize(ref writer, (global::SharedData.FooClass)value, formatterResolver);
                        break;
                    case 1:
                        formatterResolver.GetFormatterWithVerify<global::SharedData.BarClass>().Serialize(ref writer, (global::SharedData.BarClass)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionSample Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::SharedData.IUnionSample");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::SharedData.IUnionSample result = null;
            switch (key)
            {
                case 0:
                    result = (global::SharedData.IUnionSample)formatterResolver.GetFormatterWithVerify<global::SharedData.FooClass>().Deserialize(ref reader, formatterResolver);
                    break;
                case 1:
                    result = (global::SharedData.IUnionSample)formatterResolver.GetFormatterWithVerify<global::SharedData.BarClass>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }


}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class IMessageBodyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::IMessageBody>
    {
        readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        readonly Dictionary<int, int> keyToJumpMap;

        public IMessageBodyFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(3, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::TextMessageBody).TypeHandle, new KeyValuePair<int, int>(10, 0) },
                { typeof(global::StampMessageBody).TypeHandle, new KeyValuePair<int, int>(14, 1) },
                { typeof(global::QuestMessageBody).TypeHandle, new KeyValuePair<int, int>(25, 2) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(3)
            {
                { 10, 0 },
                { 14, 1 },
                { 25, 2 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::IMessageBody value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteFixedArrayHeaderUnsafe(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        formatterResolver.GetFormatterWithVerify<global::TextMessageBody>().Serialize(ref writer, (global::TextMessageBody)value, formatterResolver);
                        break;
                    case 1:
                        formatterResolver.GetFormatterWithVerify<global::StampMessageBody>().Serialize(ref writer, (global::StampMessageBody)value, formatterResolver);
                        break;
                    case 2:
                        formatterResolver.GetFormatterWithVerify<global::QuestMessageBody>().Serialize(ref writer, (global::QuestMessageBody)value, formatterResolver);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::IMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::IMessageBody");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::IMessageBody result = null;
            switch (key)
            {
                case 0:
                    result = (global::IMessageBody)formatterResolver.GetFormatterWithVerify<global::TextMessageBody>().Deserialize(ref reader, formatterResolver);
                    break;
                case 1:
                    result = (global::IMessageBody)formatterResolver.GetFormatterWithVerify<global::StampMessageBody>().Deserialize(ref reader, formatterResolver);
                    break;
                case 2:
                    result = (global::IMessageBody)formatterResolver.GetFormatterWithVerify<global::QuestMessageBody>().Deserialize(ref reader, formatterResolver);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }


}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612


#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.SharedData
{
    using System;
	using System.Buffers;
    using MessagePack;


    public sealed class FirstSimpleDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.FirstSimpleData>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.FirstSimpleData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.Prop1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Prop2, formatterResolver);
            writer.Write(value.Prop3);
        }

        public global::SharedData.FirstSimpleData Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __Prop1__ = default(int);
            var __Prop2__ = default(string);
            var __Prop3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Prop1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __Prop3__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.FirstSimpleData();
            ____result.Prop1 = __Prop1__;
            ____result.Prop2 = __Prop2__;
            ____result.Prop3 = __Prop3__;
            return ____result;
        }
    }


    public sealed class SimpleStringKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleStringKeyData>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public SimpleStringKeyDataFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "Prop1", 0},
                { "Prop2", 1},
                { "Prop3", 2},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop1"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop2"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop3"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStringKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(3);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.Prop1);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref writer, value.Prop2, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[2]);
            writer.Write(value.Prop3);
        }

        public global::SharedData.SimpleStringKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();

            var __Prop1__ = default(int);
            var __Prop2__ = default(global::SharedData.ByteEnum);
            var __Prop3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __Prop1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __Prop3__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.SimpleStringKeyData();
            ____result.Prop1 = __Prop1__;
            ____result.Prop2 = __Prop2__;
            ____result.Prop3 = __Prop3__;
            return ____result;
        }
    }


    public sealed class SimpleStructIntKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleStructIntKeyData>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStructIntKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, value.BytesSpecial, formatterResolver);
        }

        public global::SharedData.SimpleStructIntKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __X__ = default(int);
            var __Y__ = default(int);
            var __BytesSpecial__ = default(byte[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Y__ = reader.ReadInt32();
                        break;
                    case 2:
                        __BytesSpecial__ = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.SimpleStructIntKeyData();
            ____result.X = __X__;
            ____result.Y = __Y__;
            ____result.BytesSpecial = __BytesSpecial__;
            return ____result;
        }
    }


    public sealed class SimpleStructStringKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleStructStringKeyData>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public SimpleStructStringKeyDataFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "key-X", 0},
                { "key-Y", 1},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("key-X"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("key-Y"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStructStringKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteMapHeader(2);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.Y, formatterResolver);
        }

        public global::SharedData.SimpleStructStringKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadMapHeader();

            var __X__ = default(int);
            var __Y__ = default(int[]);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Y__ = formatterResolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.SimpleStructStringKeyData();
            ____result.X = __X__;
            ____result.Y = __Y__;
            return ____result;
        }
    }


    public sealed class SimpleIntKeyDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SimpleIntKeyData>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleIntKeyData value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(7);
            writer.Write(value.Prop1);
            formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref writer, value.Prop2, formatterResolver);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Prop3, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStringKeyData>().Serialize(ref writer, value.Prop4, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Serialize(ref writer, value.Prop5, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Serialize(ref writer, value.Prop6, formatterResolver);
            formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, value.BytesSpecial, formatterResolver);
        }

        public global::SharedData.SimpleIntKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __Prop1__ = default(int);
            var __Prop2__ = default(global::SharedData.ByteEnum);
            var __Prop3__ = default(string);
            var __Prop4__ = default(global::SharedData.SimpleStringKeyData);
            var __Prop5__ = default(global::SharedData.SimpleStructIntKeyData);
            var __Prop6__ = default(global::SharedData.SimpleStructStringKeyData);
            var __BytesSpecial__ = default(byte[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Prop1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __Prop3__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 3:
                        __Prop4__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStringKeyData>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 4:
                        __Prop5__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 5:
                        __Prop6__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 6:
                        __BytesSpecial__ = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Vector2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public global::SharedData.Vector2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __X__ = default(float);
            var __Y__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadSingle();
                        break;
                    case 1:
                        __Y__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Vector2(__X__, __Y__);
            return ____result;
        }
    }


    public sealed class EmptyClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.EmptyClass>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.EmptyClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(0);
        }

        public global::SharedData.EmptyClass Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();


            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.EmptyClass();
            return ____result;
        }
    }


    public sealed class EmptyStructFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.EmptyStruct>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.EmptyStruct value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(0);
        }

        public global::SharedData.EmptyStruct Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();


            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.EmptyStruct();
            return ____result;
        }
    }


    public sealed class Version1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(6);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
        }

        public global::SharedData.Version1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 4:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 5:
                        __MyProperty3__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Version1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            return ____result;
        }
    }


    public sealed class Version2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Version2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
            writer.WriteNil();
            writer.Write(value.MyProperty5);
        }

        public global::SharedData.Version2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

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
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 4:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 5:
                        __MyProperty3__ = reader.ReadInt32();
                        break;
                    case 7:
                        __MyProperty5__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version0 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(4);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
        }

        public global::SharedData.Version0 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Version0();
            ____result.MyProperty1 = __MyProperty1__;
            return ____result;
        }
    }


    public sealed class HolderV1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Serialize(ref writer, value.MyProperty1, formatterResolver);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(global::SharedData.Version1);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 1:
                        __After__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.HolderV1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class HolderV2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Serialize(ref writer, value.MyProperty1, formatterResolver);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(global::SharedData.Version2);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 1:
                        __After__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.HolderV2();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class HolderV0Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.HolderV0>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV0 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Serialize(ref writer, value.MyProperty1, formatterResolver);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV0 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(global::SharedData.Version0);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 1:
                        __After__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.HolderV0();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.After = __After__;
            return ____result;
        }
    }


    public sealed class Callback1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            value.OnBeforeSerialize();
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.X);
        }

        public global::SharedData.Callback1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Callback1(__X__);
            ____result.X = __X__;
            ____result.OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback1_2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback1_2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback1_2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.X);
        }

        public global::SharedData.Callback1_2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Callback1_2(__X__);
            ____result.X = __X__;
            ((IMessagePackSerializationCallbackReceiver)____result).OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback2>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public Callback2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "X", 0},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("X"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            value.OnBeforeSerialize();
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
        }

        public global::SharedData.Callback2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadMapHeader();

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Callback2(__X__);
            ____result.X = __X__;
            ____result.OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class Callback2_2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Callback2_2>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public Callback2_2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "X", 0},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("X"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback2_2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
        }

        public global::SharedData.Callback2_2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadMapHeader();

            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __X__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Callback2_2(__X__);
            ____result.X = __X__;
            ((IMessagePackSerializationCallbackReceiver)____result).OnAfterDeserialize();
            return ____result;
        }
    }


    public sealed class SubUnionType1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SubUnionType1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SubUnionType1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty1);
        }

        public global::SharedData.SubUnionType1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(int);
            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 1:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.SubUnionType1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class SubUnionType2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.SubUnionType2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SubUnionType2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.SubUnionType2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty2__ = default(int);
            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 1:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.SubUnionType2();
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class MySubUnion1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(4);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.One);
        }

        public global::SharedData.MySubUnion1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __One__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 3:
                        __One__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.MySubUnion1();
            ____result.One = __One__;
            return ____result;
        }
    }


    public sealed class MySubUnion2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(6);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Two);
        }

        public global::SharedData.MySubUnion2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __Two__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 5:
                        __Two__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.MySubUnion2();
            ____result.Two = __Two__;
            return ____result;
        }
    }


    public sealed class MySubUnion3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion3>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion3 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Three);
        }

        public global::SharedData.MySubUnion3 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __Three__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 2:
                        __Three__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.MySubUnion3();
            ____result.Three = __Three__;
            return ____result;
        }
    }


    public sealed class MySubUnion4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MySubUnion4>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion4 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Four);
        }

        public global::SharedData.MySubUnion4 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __Four__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 7:
                        __Four__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.MySubUnion4();
            ____result.Four = __Four__;
            return ____result;
        }
    }


    public sealed class VersioningUnionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.VersioningUnion>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.VersioningUnion value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.FV);
        }

        public global::SharedData.VersioningUnion Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __FV__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 7:
                        __FV__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.VersioningUnion();
            ____result.FV = __FV__;
            return ____result;
        }
    }


    public sealed class MyClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.MyClass>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.MyClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
        }

        public global::SharedData.MyClass Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty3__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.MyClass();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            return ____result;
        }
    }


    public sealed class VersionBlockTestFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.VersionBlockTest>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.VersionBlockTest value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.MyProperty);
            formatterResolver.GetFormatterWithVerify<global::SharedData.MyClass>().Serialize(ref writer, value.UnknownBlock, formatterResolver);
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.VersionBlockTest Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);
            var __UnknownBlock__ = default(global::SharedData.MyClass);
            var __MyProperty2__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    case 1:
                        __UnknownBlock__ = formatterResolver.GetFormatterWithVerify<global::SharedData.MyClass>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.VersionBlockTest();
            ____result.MyProperty = __MyProperty__;
            ____result.UnknownBlock = __UnknownBlock__;
            ____result.MyProperty2 = __MyProperty2__;
            return ____result;
        }
    }


    public sealed class UnVersionBlockTestFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.UnVersionBlockTest>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.UnVersionBlockTest value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.MyProperty);
            writer.WriteNil();
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.UnVersionBlockTest Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);
            var __MyProperty2__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.UnVersionBlockTest();
            ____result.MyProperty = __MyProperty__;
            ____result.MyProperty2 = __MyProperty2__;
            return ____result;
        }
    }


    public sealed class Empty1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Empty1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Empty1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(0);
        }

        public global::SharedData.Empty1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();


            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Empty1();
            return ____result;
        }
    }


    public sealed class Empty2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Empty2>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public Empty2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
            };

            this.____stringByteKeys = new byte[][]
            {
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Empty2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(0);
        }

        public global::SharedData.Empty2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();


            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Empty2();
            return ____result;
        }
    }


    public sealed class NonEmpty1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.NonEmpty1>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.NonEmpty1 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NonEmpty1 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.NonEmpty1();
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class NonEmpty2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.NonEmpty2>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public NonEmpty2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty", 0},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SharedData.NonEmpty2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NonEmpty2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();

            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.NonEmpty2();
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class VectorLike2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.VectorLike2>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.VectorLike2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::SharedData.VectorLike2 Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __x__ = default(float);
            var __y__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadSingle();
                        break;
                    case 1:
                        __y__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.VectorLike2(__x__, __y__);
            ____result.x = __x__;
            ____result.y = __y__;
            return ____result;
        }
    }


    public sealed class Vector3LikeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.Vector3Like>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Vector3Like value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::SharedData.Vector3Like Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();

            var __x__ = default(float);
            var __y__ = default(float);
            var __z__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadSingle();
                        break;
                    case 1:
                        __y__ = reader.ReadSingle();
                        break;
                    case 2:
                        __z__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.Vector3Like(__x__, __y__, __z__);
            ____result.x = __x__;
            ____result.y = __y__;
            ____result.z = __z__;
            return ____result;
        }
    }


    public sealed class ArrayOptimizeClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.ArrayOptimizeClass>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.ArrayOptimizeClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteArrayHeader(16);
            writer.Write(value.MyProperty0);
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
            writer.Write(value.MyProperty4);
            writer.Write(value.MyProperty5);
            writer.Write(value.MyProperty6);
            writer.Write(value.MyProperty7);
            writer.Write(value.MyProperty8);
            writer.Write(value.MyProvperty9);
            writer.Write(value.MyProperty10);
            writer.Write(value.MyProperty11);
            writer.Write(value.MyPropverty12);
            writer.Write(value.MyPropevrty13);
            writer.Write(value.MyProperty14);
            writer.Write(value.MyProperty15);
        }

        public global::SharedData.ArrayOptimizeClass Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty0__ = default(int);
            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);
            var __MyProperty4__ = default(int);
            var __MyProperty5__ = default(int);
            var __MyProperty6__ = default(int);
            var __MyProperty7__ = default(int);
            var __MyProperty8__ = default(int);
            var __MyProvperty9__ = default(int);
            var __MyProperty10__ = default(int);
            var __MyProperty11__ = default(int);
            var __MyPropverty12__ = default(int);
            var __MyPropevrty13__ = default(int);
            var __MyProperty14__ = default(int);
            var __MyProperty15__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty0__ = reader.ReadInt32();
                        break;
                    case 1:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 3:
                        __MyProperty3__ = reader.ReadInt32();
                        break;
                    case 4:
                        __MyProperty4__ = reader.ReadInt32();
                        break;
                    case 5:
                        __MyProperty5__ = reader.ReadInt32();
                        break;
                    case 6:
                        __MyProperty6__ = reader.ReadInt32();
                        break;
                    case 7:
                        __MyProperty7__ = reader.ReadInt32();
                        break;
                    case 8:
                        __MyProperty8__ = reader.ReadInt32();
                        break;
                    case 9:
                        __MyProvperty9__ = reader.ReadInt32();
                        break;
                    case 10:
                        __MyProperty10__ = reader.ReadInt32();
                        break;
                    case 11:
                        __MyProperty11__ = reader.ReadInt32();
                        break;
                    case 12:
                        __MyPropverty12__ = reader.ReadInt32();
                        break;
                    case 13:
                        __MyPropevrty13__ = reader.ReadInt32();
                        break;
                    case 14:
                        __MyProperty14__ = reader.ReadInt32();
                        break;
                    case 15:
                        __MyProperty15__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.ArrayOptimizeClass();
            ____result.MyProperty0 = __MyProperty0__;
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty4 = __MyProperty4__;
            ____result.MyProperty5 = __MyProperty5__;
            ____result.MyProperty6 = __MyProperty6__;
            ____result.MyProperty7 = __MyProperty7__;
            ____result.MyProperty8 = __MyProperty8__;
            ____result.MyProvperty9 = __MyProvperty9__;
            ____result.MyProperty10 = __MyProperty10__;
            ____result.MyProperty11 = __MyProperty11__;
            ____result.MyPropverty12 = __MyPropverty12__;
            ____result.MyPropevrty13 = __MyPropevrty13__;
            ____result.MyProperty14 = __MyProperty14__;
            ____result.MyProperty15 = __MyProperty15__;
            return ____result;
        }
    }


    public sealed class NestParent_NestContractFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.NestParent.NestContract>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.NestParent.NestContract value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NestParent.NestContract Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.NestParent.NestContract();
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class FooClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.FooClass>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.FooClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.XYZ);
        }

        public global::SharedData.FooClass Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __XYZ__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __XYZ__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.FooClass();
            ____result.XYZ = __XYZ__;
            return ____result;
        }
    }


    public sealed class BarClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.BarClass>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.BarClass value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.OPQ, formatterResolver);
        }

        public global::SharedData.BarClass Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __OPQ__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __OPQ__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.BarClass();
            ____result.OPQ = __OPQ__;
            return ____result;
        }
    }


    public sealed class WithIndexerFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.WithIndexer>
    {

        public void Serialize(ref MessagePackWriter writer, global::SharedData.WithIndexer value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.Data1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Data2, formatterResolver);
        }

        public global::SharedData.WithIndexer Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __Data1__ = default(int);
            var __Data2__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Data1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Data2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SharedData.WithIndexer();
            ____result.Data1 = __Data1__;
            ____result.Data2 = __Data2__;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad
{
    using System;
	using System.Buffers;
    using MessagePack;


    public sealed class TnonodsfarnoiuAtatqagaFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga>
    {

        public void Serialize(ref MessagePackWriter writer, global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.MyProperty);
        }

        public global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga();
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters
{
    using System;
	using System.Buffers;
    using MessagePack;


    public sealed class GlobalManFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::GlobalMan>
    {

        public void Serialize(ref MessagePackWriter writer, global::GlobalMan value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.MyProperty);
        }

        public global::GlobalMan Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::GlobalMan();
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }


    public sealed class MessageFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Message>
    {

        public void Serialize(ref MessagePackWriter writer, global::Message value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(4);
            writer.Write(value.UserId);
            writer.Write(value.RoomId);
            formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Serialize(ref writer, value.PostTime, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::IMessageBody>().Serialize(ref writer, value.Body, formatterResolver);
        }

        public global::Message Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __UserId__ = default(int);
            var __RoomId__ = default(int);
            var __PostTime__ = default(global::System.DateTime);
            var __Body__ = default(global::IMessageBody);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __UserId__ = reader.ReadInt32();
                        break;
                    case 1:
                        __RoomId__ = reader.ReadInt32();
                        break;
                    case 2:
                        __PostTime__ = formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 3:
                        __Body__ = formatterResolver.GetFormatterWithVerify<global::IMessageBody>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Message();
            ____result.UserId = __UserId__;
            ____result.RoomId = __RoomId__;
            ____result.PostTime = __PostTime__;
            ____result.Body = __Body__;
            return ____result;
        }
    }


    public sealed class TextMessageBodyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TextMessageBody>
    {

        public void Serialize(ref MessagePackWriter writer, global::TextMessageBody value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Text, formatterResolver);
        }

        public global::TextMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __Text__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Text__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::TextMessageBody();
            ____result.Text = __Text__;
            return ____result;
        }
    }


    public sealed class StampMessageBodyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::StampMessageBody>
    {

        public void Serialize(ref MessagePackWriter writer, global::StampMessageBody value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(1);
            writer.Write(value.StampId);
        }

        public global::StampMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __StampId__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __StampId__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::StampMessageBody();
            ____result.StampId = __StampId__;
            return ____result;
        }
    }


    public sealed class QuestMessageBodyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::QuestMessageBody>
    {

        public void Serialize(ref MessagePackWriter writer, global::QuestMessageBody value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(2);
            writer.Write(value.QuestId);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Text, formatterResolver);
        }

        public global::QuestMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __QuestId__ = default(int);
            var __Text__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __QuestId__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Text__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::QuestMessageBody();
            ____result.QuestId = __QuestId__;
            ____result.Text = __Text__;
            return ____result;
        }
    }


    public sealed class ArrayTestTestFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ArrayTestTest>
    {

        public void Serialize(ref MessagePackWriter writer, global::ArrayTestTest value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteFixedArrayHeaderUnsafe(7);
            formatterResolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.MyProperty0, formatterResolver);
            formatterResolver.GetFormatterWithVerify<int[,]>().Serialize(ref writer, value.MyProperty1, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[,]>().Serialize(ref writer, value.MyProperty2, formatterResolver);
            formatterResolver.GetFormatterWithVerify<int[,,]>().Serialize(ref writer, value.MyProperty3, formatterResolver);
            formatterResolver.GetFormatterWithVerify<int[,,,]>().Serialize(ref writer, value.MyProperty4, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[]>().Serialize(ref writer, value.MyProperty5, formatterResolver);
            formatterResolver.GetFormatterWithVerify<global::QuestMessageBody[]>().Serialize(ref writer, value.MyProperty6, formatterResolver);
        }

        public global::ArrayTestTest Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadArrayHeader();

            var __MyProperty0__ = default(int[]);
            var __MyProperty1__ = default(int[,]);
            var __MyProperty2__ = default(global::GlobalMyEnum[,]);
            var __MyProperty3__ = default(int[,,]);
            var __MyProperty4__ = default(int[,,,]);
            var __MyProperty5__ = default(global::GlobalMyEnum[]);
            var __MyProperty6__ = default(global::QuestMessageBody[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty0__ = formatterResolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 1:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int[,]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __MyProperty2__ = formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[,]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 3:
                        __MyProperty3__ = formatterResolver.GetFormatterWithVerify<int[,,]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 4:
                        __MyProperty4__ = formatterResolver.GetFormatterWithVerify<int[,,,]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 5:
                        __MyProperty5__ = formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 6:
                        __MyProperty6__ = formatterResolver.GetFormatterWithVerify<global::QuestMessageBody[]>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ArrayTestTest();
            ____result.MyProperty0 = __MyProperty0__;
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty4 = __MyProperty4__;
            ____result.MyProperty5 = __MyProperty5__;
            ____result.MyProperty6 = __MyProperty6__;
            return ____result;
        }
    }


    public sealed class SimpleModelFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SimpleModel>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public SimpleModelFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "Id", 0},
                { "Name", 1},
                { "CreatedOn", 2},
                { "Precision", 3},
                { "Money", 4},
                { "Amount", 5},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Id"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Name"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("CreatedOn"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Precision"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Money"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Amount"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::SimpleModel value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(6);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.Id);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[2]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Serialize(ref writer, value.CreatedOn, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[3]);
            writer.Write(value.Precision);
            writer.WriteRaw(this.____stringByteKeys[4]);
            formatterResolver.GetFormatterWithVerify<decimal>().Serialize(ref writer, value.Money, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[5]);
            writer.Write(value.Amount);
        }

        public global::SimpleModel Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();

            var __Id__ = default(int);
            var __Name__ = default(string);
            var __CreatedOn__ = default(global::System.DateTime);
            var __Precision__ = default(int);
            var __Money__ = default(decimal);
            var __Amount__ = default(long);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __Id__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __CreatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 3:
                        __Precision__ = reader.ReadInt32();
                        break;
                    case 4:
                        __Money__ = formatterResolver.GetFormatterWithVerify<decimal>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 5:
                        __Amount__ = reader.ReadInt64();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::SimpleModel();
            ____result.Id = __Id__;
            ____result.Name = __Name__;
            ____result.CreatedOn = __CreatedOn__;
            ____result.Precision = __Precision__;
            ____result.Money = __Money__;
            return ____result;
        }
    }


    public sealed class ComplexModelFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexModel>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public ComplexModelFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "AdditionalProperty", 0},
                { "CreatedOn", 1},
                { "Id", 2},
                { "Name", 3},
                { "UpdatedOn", 4},
                { "SimpleModels", 5},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("AdditionalProperty"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("CreatedOn"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Id"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Name"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("UpdatedOn"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("SimpleModels"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::ComplexModel value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(6);
            writer.WriteRaw(this.____stringByteKeys[0]);
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IDictionary<string, string>>().Serialize(ref writer, value.AdditionalProperty, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Serialize(ref writer, value.CreatedOn, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[2]);
            formatterResolver.GetFormatterWithVerify<global::System.Guid>().Serialize(ref writer, value.Id, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[3]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[4]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Serialize(ref writer, value.UpdatedOn, formatterResolver);
            writer.WriteRaw(this.____stringByteKeys[5]);
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<global::SimpleModel>>().Serialize(ref writer, value.SimpleModels, formatterResolver);
        }

        public global::ComplexModel Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();

            var __AdditionalProperty__ = default(global::System.Collections.Generic.IDictionary<string, string>);
            var __CreatedOn__ = default(global::System.DateTimeOffset);
            var __Id__ = default(global::System.Guid);
            var __Name__ = default(string);
            var __UpdatedOn__ = default(global::System.DateTimeOffset);
            var __SimpleModels__ = default(global::System.Collections.Generic.IList<global::SimpleModel>);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __AdditionalProperty__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IDictionary<string, string>>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 1:
                        __CreatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 2:
                        __Id__ = formatterResolver.GetFormatterWithVerify<global::System.Guid>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 3:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 4:
                        __UpdatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Deserialize(ref reader, formatterResolver);
                        break;
                    case 5:
                        __SimpleModels__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<global::SimpleModel>>().Deserialize(ref reader, formatterResolver);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ComplexModel();
            ____result.CreatedOn = __CreatedOn__;
            ____result.Id = __Id__;
            ____result.Name = __Name__;
            ____result.UpdatedOn = __UpdatedOn__;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

namespace MessagePack.Formatters.PerfBenchmarkDotNet
{
    using System;
	using System.Buffers;
    using MessagePack;


    public sealed class StringKeySerializerTargetFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::PerfBenchmarkDotNet.StringKeySerializerTarget>
    {

        readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        readonly byte[][] ____stringByteKeys;

        public StringKeySerializerTargetFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty1", 0},
                { "MyProperty2", 1},
                { "MyProperty3", 2},
                { "MyProperty4", 3},
                { "MyProperty5", 4},
                { "MyProperty6", 5},
                { "MyProperty7", 6},
                { "MyProperty8", 7},
                { "MyProperty9", 8},
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty1"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty2"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty3"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty4"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty5"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty6"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty7"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty8"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty9"),
            };
        }


        public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteMapHeader(9);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.MyProperty1);
            writer.WriteRaw(this.____stringByteKeys[1]);
            writer.Write(value.MyProperty2);
            writer.WriteRaw(this.____stringByteKeys[2]);
            writer.Write(value.MyProperty3);
            writer.WriteRaw(this.____stringByteKeys[3]);
            writer.Write(value.MyProperty4);
            writer.WriteRaw(this.____stringByteKeys[4]);
            writer.Write(value.MyProperty5);
            writer.WriteRaw(this.____stringByteKeys[5]);
            writer.Write(value.MyProperty6);
            writer.WriteRaw(this.____stringByteKeys[6]);
            writer.Write(value.MyProperty7);
            writer.WriteRaw(this.____stringByteKeys[7]);
            writer.Write(value.MyProperty8);
            writer.WriteRaw(this.____stringByteKeys[8]);
            writer.Write(value.MyProperty9);
        }

        public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();

            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(int);
            var __MyProperty4__ = default(int);
            var __MyProperty5__ = default(int);
            var __MyProperty6__ = default(int);
            var __MyProperty7__ = default(int);
            var __MyProperty8__ = default(int);
            var __MyProperty9__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var stringKey = reader.ReadStringSegment();
                int key;
                if (!____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = reader.ReadInt32();
                        break;
                    case 1:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty3__ = reader.ReadInt32();
                        break;
                    case 3:
                        __MyProperty4__ = reader.ReadInt32();
                        break;
                    case 4:
                        __MyProperty5__ = reader.ReadInt32();
                        break;
                    case 5:
                        __MyProperty6__ = reader.ReadInt32();
                        break;
                    case 6:
                        __MyProperty7__ = reader.ReadInt32();
                        break;
                    case 7:
                        __MyProperty8__ = reader.ReadInt32();
                        break;
                    case 8:
                        __MyProperty9__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::PerfBenchmarkDotNet.StringKeySerializerTarget();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty4 = __MyProperty4__;
            ____result.MyProperty5 = __MyProperty5__;
            ____result.MyProperty6 = __MyProperty6__;
            ____result.MyProperty7 = __MyProperty7__;
            ____result.MyProperty8 = __MyProperty8__;
            ____result.MyProperty9 = __MyProperty9__;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612


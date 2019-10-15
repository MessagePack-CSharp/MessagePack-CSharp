#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    using System;
    using System.Buffers;
    using MessagePack;

    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        private GeneratedResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(84)
            {
                { typeof(int[,]), 0 },
                { typeof(global::GlobalMyEnum[,]), 1 },
                { typeof(int[,,]), 2 },
                { typeof(int[,,,]), 3 },
                { typeof(global::GlobalMyEnum[]), 4 },
                { typeof(global::QuestMessageBody[]), 5 },
                { typeof(global::System.Collections.Generic.IDictionary<string, string>), 6 },
                { typeof(global::System.Collections.Generic.IList<global::SimpleModel>), 7 },
                { typeof(global::SharedData.ByteEnum), 8 },
                { typeof(global::GlobalMyEnum), 9 },
                { typeof(global::SharedData.IUnionChecker), 10 },
                { typeof(global::SharedData.IUnionChecker2), 11 },
                { typeof(global::SharedData.IIVersioningUnion), 12 },
                { typeof(global::SharedData.RootUnionType), 13 },
                { typeof(global::SharedData.IUnionSample), 14 },
                { typeof(global::IMessageBody), 15 },
                { typeof(global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase), 16 },
                { typeof(global::ComplexdUnion.A), 17 },
                { typeof(global::ComplexdUnion.A2), 18 },
                { typeof(global::ClassUnion.RootUnionType), 19 },
                { typeof(global::SharedData.FirstSimpleData), 20 },
                { typeof(global::SharedData.SimpleStringKeyData), 21 },
                { typeof(global::SharedData.SimpleStructIntKeyData), 22 },
                { typeof(global::SharedData.SimpleStructStringKeyData), 23 },
                { typeof(global::SharedData.SimpleIntKeyData), 24 },
                { typeof(global::SharedData.Vector2), 25 },
                { typeof(global::SharedData.EmptyClass), 26 },
                { typeof(global::SharedData.EmptyStruct), 27 },
                { typeof(global::SharedData.Version1), 28 },
                { typeof(global::SharedData.Version2), 29 },
                { typeof(global::SharedData.Version0), 30 },
                { typeof(global::SharedData.HolderV1), 31 },
                { typeof(global::SharedData.HolderV2), 32 },
                { typeof(global::SharedData.HolderV0), 33 },
                { typeof(global::SharedData.Callback1), 34 },
                { typeof(global::SharedData.Callback1_2), 35 },
                { typeof(global::SharedData.Callback2), 36 },
                { typeof(global::SharedData.Callback2_2), 37 },
                { typeof(global::SharedData.SubUnionType1), 38 },
                { typeof(global::SharedData.SubUnionType2), 39 },
                { typeof(global::SharedData.MySubUnion1), 40 },
                { typeof(global::SharedData.MySubUnion2), 41 },
                { typeof(global::SharedData.MySubUnion3), 42 },
                { typeof(global::SharedData.MySubUnion4), 43 },
                { typeof(global::SharedData.VersioningUnion), 44 },
                { typeof(global::SharedData.MyClass), 45 },
                { typeof(global::SharedData.VersionBlockTest), 46 },
                { typeof(global::SharedData.UnVersionBlockTest), 47 },
                { typeof(global::SharedData.Empty1), 48 },
                { typeof(global::SharedData.Empty2), 49 },
                { typeof(global::SharedData.NonEmpty1), 50 },
                { typeof(global::SharedData.NonEmpty2), 51 },
                { typeof(global::SharedData.VectorLike2), 52 },
                { typeof(global::SharedData.Vector3Like), 53 },
                { typeof(global::SharedData.ArrayOptimizeClass), 54 },
                { typeof(global::SharedData.NestParent.NestContract), 55 },
                { typeof(global::SharedData.FooClass), 56 },
                { typeof(global::SharedData.BarClass), 57 },
                { typeof(global::SharedData.WithIndexer), 58 },
                { typeof(global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga), 59 },
                { typeof(global::GlobalMan), 60 },
                { typeof(global::Message), 61 },
                { typeof(global::TextMessageBody), 62 },
                { typeof(global::StampMessageBody), 63 },
                { typeof(global::QuestMessageBody), 64 },
                { typeof(global::ArrayTestTest), 65 },
                { typeof(global::SimpleModel), 66 },
                { typeof(global::ComplexModel), 67 },
                { typeof(global::PerfBenchmarkDotNet.StringKeySerializerTarget), 68 },
                { typeof(global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor1), 69 },
                { typeof(global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor2), 70 },
                { typeof(global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor3), 71 },
                { typeof(global::MessagePack.Tests.DynamicObjectResolverOrderTest.OrderOrder), 72 },
                { typeof(global::MessagePack.Tests.IgnoreTest.ViewModel), 73 },
                { typeof(global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyClass), 74 },
                { typeof(global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyStruct), 75 },
                { typeof(global::MessagePack.Tests.NewGuidFormatterTest.InClass), 76 },
                { typeof(global::MessagePack.Tests.PrimitivelikeFormatterTest.MyDateTimeResolverTest), 77 },
                { typeof(global::ComplexdUnion.B), 78 },
                { typeof(global::ComplexdUnion.C), 79 },
                { typeof(global::ComplexdUnion.B2), 80 },
                { typeof(global::ComplexdUnion.C2), 81 },
                { typeof(global::ClassUnion.SubUnionType1), 82 },
                { typeof(global::ClassUnion.SubUnionType2), 83 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.TwoDimensionalArrayFormatter<int>();
                case 1: return new global::MessagePack.Formatters.TwoDimensionalArrayFormatter<global::GlobalMyEnum>();
                case 2: return new global::MessagePack.Formatters.ThreeDimensionalArrayFormatter<int>();
                case 3: return new global::MessagePack.Formatters.FourDimensionalArrayFormatter<int>();
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
                case 16: return new MessagePack.Formatters.MessagePack.Tests.AbstractBaseFormatter();
                case 17: return new MessagePack.Formatters.ComplexdUnion.AFormatter();
                case 18: return new MessagePack.Formatters.ComplexdUnion.A2Formatter();
                case 19: return new MessagePack.Formatters.ClassUnion.RootUnionTypeFormatter();
                case 20: return new MessagePack.Formatters.SharedData.FirstSimpleDataFormatter();
                case 21: return new MessagePack.Formatters.SharedData.SimpleStringKeyDataFormatter();
                case 22: return new MessagePack.Formatters.SharedData.SimpleStructIntKeyDataFormatter();
                case 23: return new MessagePack.Formatters.SharedData.SimpleStructStringKeyDataFormatter();
                case 24: return new MessagePack.Formatters.SharedData.SimpleIntKeyDataFormatter();
                case 25: return new MessagePack.Formatters.SharedData.Vector2Formatter();
                case 26: return new MessagePack.Formatters.SharedData.EmptyClassFormatter();
                case 27: return new MessagePack.Formatters.SharedData.EmptyStructFormatter();
                case 28: return new MessagePack.Formatters.SharedData.Version1Formatter();
                case 29: return new MessagePack.Formatters.SharedData.Version2Formatter();
                case 30: return new MessagePack.Formatters.SharedData.Version0Formatter();
                case 31: return new MessagePack.Formatters.SharedData.HolderV1Formatter();
                case 32: return new MessagePack.Formatters.SharedData.HolderV2Formatter();
                case 33: return new MessagePack.Formatters.SharedData.HolderV0Formatter();
                case 34: return new MessagePack.Formatters.SharedData.Callback1Formatter();
                case 35: return new MessagePack.Formatters.SharedData.Callback1_2Formatter();
                case 36: return new MessagePack.Formatters.SharedData.Callback2Formatter();
                case 37: return new MessagePack.Formatters.SharedData.Callback2_2Formatter();
                case 38: return new MessagePack.Formatters.SharedData.SubUnionType1Formatter();
                case 39: return new MessagePack.Formatters.SharedData.SubUnionType2Formatter();
                case 40: return new MessagePack.Formatters.SharedData.MySubUnion1Formatter();
                case 41: return new MessagePack.Formatters.SharedData.MySubUnion2Formatter();
                case 42: return new MessagePack.Formatters.SharedData.MySubUnion3Formatter();
                case 43: return new MessagePack.Formatters.SharedData.MySubUnion4Formatter();
                case 44: return new MessagePack.Formatters.SharedData.VersioningUnionFormatter();
                case 45: return new MessagePack.Formatters.SharedData.MyClassFormatter();
                case 46: return new MessagePack.Formatters.SharedData.VersionBlockTestFormatter();
                case 47: return new MessagePack.Formatters.SharedData.UnVersionBlockTestFormatter();
                case 48: return new MessagePack.Formatters.SharedData.Empty1Formatter();
                case 49: return new MessagePack.Formatters.SharedData.Empty2Formatter();
                case 50: return new MessagePack.Formatters.SharedData.NonEmpty1Formatter();
                case 51: return new MessagePack.Formatters.SharedData.NonEmpty2Formatter();
                case 52: return new MessagePack.Formatters.SharedData.VectorLike2Formatter();
                case 53: return new MessagePack.Formatters.SharedData.Vector3LikeFormatter();
                case 54: return new MessagePack.Formatters.SharedData.ArrayOptimizeClassFormatter();
                case 55: return new MessagePack.Formatters.SharedData.NestParent_NestContractFormatter();
                case 56: return new MessagePack.Formatters.SharedData.FooClassFormatter();
                case 57: return new MessagePack.Formatters.SharedData.BarClassFormatter();
                case 58: return new MessagePack.Formatters.SharedData.WithIndexerFormatter();
                case 59: return new MessagePack.Formatters.Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqagaFormatter();
                case 60: return new MessagePack.Formatters.GlobalManFormatter();
                case 61: return new MessagePack.Formatters.MessageFormatter();
                case 62: return new MessagePack.Formatters.TextMessageBodyFormatter();
                case 63: return new MessagePack.Formatters.StampMessageBodyFormatter();
                case 64: return new MessagePack.Formatters.QuestMessageBodyFormatter();
                case 65: return new MessagePack.Formatters.ArrayTestTestFormatter();
                case 66: return new MessagePack.Formatters.SimpleModelFormatter();
                case 67: return new MessagePack.Formatters.ComplexModelFormatter();
                case 68: return new MessagePack.Formatters.PerfBenchmarkDotNet.StringKeySerializerTargetFormatter();
                case 69: return new MessagePack.Formatters.MessagePack.Tests.DynamicObjectResolverConstructorTest_TestConstructor1Formatter();
                case 70: return new MessagePack.Formatters.MessagePack.Tests.DynamicObjectResolverConstructorTest_TestConstructor2Formatter();
                case 71: return new MessagePack.Formatters.MessagePack.Tests.DynamicObjectResolverConstructorTest_TestConstructor3Formatter();
                case 72: return new MessagePack.Formatters.MessagePack.Tests.DynamicObjectResolverOrderTest_OrderOrderFormatter();
                case 73: return new MessagePack.Formatters.MessagePack.Tests.IgnoreTest_ViewModelFormatter();
                case 74: return new MessagePack.Formatters.MessagePack.Tests.MessagePackFormatterPerFieldTest_MyClassFormatter();
                case 75: return new MessagePack.Formatters.MessagePack.Tests.MessagePackFormatterPerFieldTest_MyStructFormatter();
                case 76: return new MessagePack.Formatters.MessagePack.Tests.NewGuidFormatterTest_InClassFormatter();
                case 77: return new MessagePack.Formatters.MessagePack.Tests.PrimitivelikeFormatterTest_MyDateTimeResolverTestFormatter();
                case 78: return new MessagePack.Formatters.ComplexdUnion.BFormatter();
                case 79: return new MessagePack.Formatters.ComplexdUnion.CFormatter();
                case 80: return new MessagePack.Formatters.ComplexdUnion.B2Formatter();
                case 81: return new MessagePack.Formatters.ComplexdUnion.C2Formatter();
                case 82: return new MessagePack.Formatters.ClassUnion.SubUnionType1Formatter();
                case 83: return new MessagePack.Formatters.ClassUnion.SubUnionType2Formatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1649 // File name should match first type name


#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.SharedData
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class ByteEnumFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.ByteEnum>
    {
        public void Serialize(ref MessagePackWriter writer, global::SharedData.ByteEnum value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((Byte)value);
        }

        public global::SharedData.ByteEnum Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::SharedData.ByteEnum)reader.ReadByte();
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class GlobalMyEnumFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::GlobalMyEnum>
    {
        public void Serialize(ref MessagePackWriter writer, global::GlobalMyEnum value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((Int32)value);
        }

        public global::GlobalMyEnum Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::GlobalMyEnum)reader.ReadInt32();
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name


#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.SharedData
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class IUnionCheckerFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.IUnionChecker>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionChecker value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Serialize(ref writer, (global::SharedData.MySubUnion2)value, options);
                        break;
                    case 2:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Serialize(ref writer, (global::SharedData.MySubUnion3)value, options);
                        break;
                    case 3:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Serialize(ref writer, (global::SharedData.MySubUnion4)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionChecker Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::SharedData.IUnionChecker)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::SharedData.IUnionChecker)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Deserialize(ref reader, options);
                    break;
                case 2:
                    result = (global::SharedData.IUnionChecker)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Deserialize(ref reader, options);
                    break;
                case 3:
                    result = (global::SharedData.IUnionChecker)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Deserialize(ref reader, options);
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
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionChecker2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Serialize(ref writer, (global::SharedData.MySubUnion2)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Serialize(ref writer, (global::SharedData.MySubUnion3)value, options);
                        break;
                    case 2:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Serialize(ref writer, (global::SharedData.MySubUnion4)value, options);
                        break;
                    case 3:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionChecker2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::SharedData.IUnionChecker2)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion2>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::SharedData.IUnionChecker2)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion3>().Deserialize(ref reader, options);
                    break;
                case 2:
                    result = (global::SharedData.IUnionChecker2)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion4>().Deserialize(ref reader, options);
                    break;
                case 3:
                    result = (global::SharedData.IUnionChecker2)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, options);
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
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IIVersioningUnion value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Serialize(ref writer, (global::SharedData.MySubUnion1)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IIVersioningUnion Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::SharedData.IIVersioningUnion)options.Resolver.GetFormatterWithVerify<global::SharedData.MySubUnion1>().Deserialize(ref reader, options);
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
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.RootUnionType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.SubUnionType1>().Serialize(ref writer, (global::SharedData.SubUnionType1)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.SubUnionType2>().Serialize(ref writer, (global::SharedData.SubUnionType2)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.RootUnionType Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::SharedData.RootUnionType)options.Resolver.GetFormatterWithVerify<global::SharedData.SubUnionType1>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::SharedData.RootUnionType)options.Resolver.GetFormatterWithVerify<global::SharedData.SubUnionType2>().Deserialize(ref reader, options);
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
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::SharedData.IUnionSample value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.FooClass>().Serialize(ref writer, (global::SharedData.FooClass)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::SharedData.BarClass>().Serialize(ref writer, (global::SharedData.BarClass)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::SharedData.IUnionSample Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::SharedData.IUnionSample)options.Resolver.GetFormatterWithVerify<global::SharedData.FooClass>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::SharedData.IUnionSample)options.Resolver.GetFormatterWithVerify<global::SharedData.BarClass>().Deserialize(ref reader, options);
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

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class IMessageBodyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::IMessageBody>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

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

        public void Serialize(ref MessagePackWriter writer, global::IMessageBody value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::TextMessageBody>().Serialize(ref writer, (global::TextMessageBody)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::StampMessageBody>().Serialize(ref writer, (global::StampMessageBody)value, options);
                        break;
                    case 2:
                        options.Resolver.GetFormatterWithVerify<global::QuestMessageBody>().Serialize(ref writer, (global::QuestMessageBody)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::IMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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
                    result = (global::IMessageBody)options.Resolver.GetFormatterWithVerify<global::TextMessageBody>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::IMessageBody)options.Resolver.GetFormatterWithVerify<global::StampMessageBody>().Deserialize(ref reader, options);
                    break;
                case 2:
                    result = (global::IMessageBody)options.Resolver.GetFormatterWithVerify<global::QuestMessageBody>().Deserialize(ref reader, options);
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

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.MessagePack.Tests
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class AbstractBaseFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

        public AbstractBaseFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(1, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::MessagePack.Tests.DynamicObjectResolverOrderTest.RealClass).TypeHandle, new KeyValuePair<int, int>(0, 0) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(1)
            {
                { 0, 0 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::MessagePack.Tests.DynamicObjectResolverOrderTest.RealClass>().Serialize(ref writer, (global::MessagePack.Tests.DynamicObjectResolverOrderTest.RealClass)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase result = null;
            switch (key)
            {
                case 0:
                    result = (global::MessagePack.Tests.DynamicObjectResolverOrderTest.AbstractBase)options.Resolver.GetFormatterWithVerify<global::MessagePack.Tests.DynamicObjectResolverOrderTest.RealClass>().Deserialize(ref reader, options);
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

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.ComplexdUnion
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class AFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.A>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

        public AFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(2, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::ComplexdUnion.B).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::ComplexdUnion.C).TypeHandle, new KeyValuePair<int, int>(1, 1) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(2)
            {
                { 0, 0 },
                { 1, 1 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.A value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.B>().Serialize(ref writer, (global::ComplexdUnion.B)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.C>().Serialize(ref writer, (global::ComplexdUnion.C)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::ComplexdUnion.A Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::ComplexdUnion.A");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::ComplexdUnion.A result = null;
            switch (key)
            {
                case 0:
                    result = (global::ComplexdUnion.A)options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.B>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::ComplexdUnion.A)options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.C>().Deserialize(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            return result;
        }
    }

    public sealed class A2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.A2>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

        public A2Formatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(2, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::ComplexdUnion.B2).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::ComplexdUnion.C2).TypeHandle, new KeyValuePair<int, int>(1, 1) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(2)
            {
                { 0, 0 },
                { 1, 1 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.A2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.B2>().Serialize(ref writer, (global::ComplexdUnion.B2)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.C2>().Serialize(ref writer, (global::ComplexdUnion.C2)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::ComplexdUnion.A2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::ComplexdUnion.A2");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::ComplexdUnion.A2 result = null;
            switch (key)
            {
                case 0:
                    result = (global::ComplexdUnion.A2)options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.B2>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::ComplexdUnion.A2)options.Resolver.GetFormatterWithVerify<global::ComplexdUnion.C2>().Deserialize(ref reader, options);
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

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.ClassUnion
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using MessagePack;

    public sealed class RootUnionTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ClassUnion.RootUnionType>
    {
        private readonly Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly Dictionary<int, int> keyToJumpMap;

        public RootUnionTypeFormatter()
        {
            this.typeToKeyAndJumpMap = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>(2, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::ClassUnion.SubUnionType1).TypeHandle, new KeyValuePair<int, int>(0, 0) },
                { typeof(global::ClassUnion.SubUnionType2).TypeHandle, new KeyValuePair<int, int>(1, 1) },
            };
            this.keyToJumpMap = new Dictionary<int, int>(2)
            {
                { 0, 0 },
                { 1, 1 },
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::ClassUnion.RootUnionType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::ClassUnion.SubUnionType1>().Serialize(ref writer, (global::ClassUnion.SubUnionType1)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::ClassUnion.SubUnionType2>().Serialize(ref writer, (global::ClassUnion.SubUnionType2)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::ClassUnion.RootUnionType Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid Union data was detected. Type:global::ClassUnion.RootUnionType");
            }

            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::ClassUnion.RootUnionType result = null;
            switch (key)
            {
                case 0:
                    result = (global::ClassUnion.RootUnionType)options.Resolver.GetFormatterWithVerify<global::ClassUnion.SubUnionType1>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::ClassUnion.RootUnionType)options.Resolver.GetFormatterWithVerify<global::ClassUnion.SubUnionType2>().Deserialize(ref reader, options);
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

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name


#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.SharedData
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class FirstSimpleDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::SharedData.FirstSimpleData>
    {


        public void Serialize(ref MessagePackWriter writer, global::SharedData.FirstSimpleData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.Prop1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Prop2, options);
            writer.Write(value.Prop3);
        }

        public global::SharedData.FirstSimpleData Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public SimpleStringKeyDataFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "Prop1", 0 },
                { "Prop2", 1 },
                { "Prop3", 2 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop1"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop2"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Prop3"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStringKeyData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.Prop1);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref writer, value.Prop2, options);
            writer.WriteRaw(this.____stringByteKeys[2]);
            writer.Write(value.Prop3);
        }

        public global::SharedData.SimpleStringKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __Prop1__ = default(int);
            var __Prop2__ = default(global::SharedData.ByteEnum);
            var __Prop3__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStructIntKeyData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, value.BytesSpecial, options);
        }

        public global::SharedData.SimpleStructIntKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __BytesSpecial__ = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, options);
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public SimpleStructStringKeyDataFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "key-X", 0 },
                { "key-Y", 1 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("key-X"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("key-Y"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleStructStringKeyData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.Y, options);
        }

        public global::SharedData.SimpleStructStringKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __X__ = default(int);
            var __Y__ = default(int[]);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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
                        __Y__ = formatterResolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SimpleIntKeyData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(7);
            writer.Write(value.Prop1);
            formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Serialize(ref writer, value.Prop2, options);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Prop3, options);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStringKeyData>().Serialize(ref writer, value.Prop4, options);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Serialize(ref writer, value.Prop5, options);
            formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Serialize(ref writer, value.Prop6, options);
            formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, value.BytesSpecial, options);
        }

        public global::SharedData.SimpleIntKeyData Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __Prop2__ = formatterResolver.GetFormatterWithVerify<global::SharedData.ByteEnum>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __Prop3__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __Prop4__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStringKeyData>().Deserialize(ref reader, options);
                        break;
                    case 4:
                        __Prop5__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructIntKeyData>().Deserialize(ref reader, options);
                        break;
                    case 5:
                        __Prop6__ = formatterResolver.GetFormatterWithVerify<global::SharedData.SimpleStructStringKeyData>().Deserialize(ref reader, options);
                        break;
                    case 6:
                        __BytesSpecial__ = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Vector2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public global::SharedData.Vector2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.EmptyClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(0);
        }

        public global::SharedData.EmptyClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.EmptyStruct value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(0);
        }

        public global::SharedData.EmptyStruct Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(6);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
        }

        public global::SharedData.Version1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
            writer.WriteNil();
            writer.Write(value.MyProperty5);
        }

        public global::SharedData.Version2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Version0 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.MyProperty1);
        }

        public global::SharedData.Version0 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Serialize(ref writer, value.MyProperty1, options);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(global::SharedData.Version1);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version1>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Serialize(ref writer, value.MyProperty1, options);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(global::SharedData.Version2);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version2>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.HolderV0 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Serialize(ref writer, value.MyProperty1, options);
            writer.Write(value.After);
        }

        public global::SharedData.HolderV0 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(global::SharedData.Version0);
            var __After__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::SharedData.Version0>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            value.OnBeforeSerialize();
            writer.WriteArrayHeader(1);
            writer.Write(value.X);
        }

        public global::SharedData.Callback1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback1_2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            writer.WriteArrayHeader(1);
            writer.Write(value.X);
        }

        public global::SharedData.Callback1_2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public Callback2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "X", 0 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("X"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            value.OnBeforeSerialize();
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
        }

        public global::SharedData.Callback2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public Callback2_2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "X", 0 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("X"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Callback2_2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            ((IMessagePackSerializationCallbackReceiver)value).OnBeforeSerialize();
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
        }

        public global::SharedData.Callback2_2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __X__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SubUnionType1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty1);
        }

        public global::SharedData.SubUnionType1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.SubUnionType2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.SubUnionType2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.One);
        }

        public global::SharedData.MySubUnion1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(6);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Two);
        }

        public global::SharedData.MySubUnion2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Three);
        }

        public global::SharedData.MySubUnion3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.MySubUnion4 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.Four);
        }

        public global::SharedData.MySubUnion4 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.VersioningUnion value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(8);
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.WriteNil();
            writer.Write(value.FV);
        }

        public global::SharedData.VersioningUnion Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.MyClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.MyProperty1);
            writer.Write(value.MyProperty2);
            writer.Write(value.MyProperty3);
        }

        public global::SharedData.MyClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.VersionBlockTest value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.MyProperty);
            formatterResolver.GetFormatterWithVerify<global::SharedData.MyClass>().Serialize(ref writer, value.UnknownBlock, options);
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.VersionBlockTest Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __UnknownBlock__ = formatterResolver.GetFormatterWithVerify<global::SharedData.MyClass>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.UnVersionBlockTest value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.MyProperty);
            writer.WriteNil();
            writer.Write(value.MyProperty2);
        }

        public global::SharedData.UnVersionBlockTest Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Empty1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(0);
        }

        public global::SharedData.Empty1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public Empty2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
            };

            this.____stringByteKeys = new byte[][]
            {
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.Empty2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(0);
        }

        public global::SharedData.Empty2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.NonEmpty1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NonEmpty1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public NonEmpty2Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty", 0 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::SharedData.NonEmpty2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(1);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NonEmpty2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __MyProperty__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.VectorLike2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::SharedData.VectorLike2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.Vector3Like value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::SharedData.Vector3Like Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.ArrayOptimizeClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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

        public global::SharedData.ArrayOptimizeClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.NestParent.NestContract value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.MyProperty);
        }

        public global::SharedData.NestParent.NestContract Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.FooClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.XYZ);
        }

        public global::SharedData.FooClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.BarClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.OPQ, options);
        }

        public global::SharedData.BarClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __OPQ__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __OPQ__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::SharedData.WithIndexer value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.Data1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Data2, options);
        }

        public global::SharedData.WithIndexer Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __Data2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
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

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class TnonodsfarnoiuAtatqagaFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga>
    {


        public void Serialize(ref MessagePackWriter writer, global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.MyProperty);
        }

        public global::Abcdefg.Efcdigjl.Ateatatea.Hgfagfafgad.TnonodsfarnoiuAtatqaga Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class GlobalManFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::GlobalMan>
    {


        public void Serialize(ref MessagePackWriter writer, global::GlobalMan value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.MyProperty);
        }

        public global::GlobalMan Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::Message value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            writer.Write(value.UserId);
            writer.Write(value.RoomId);
            formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Serialize(ref writer, value.PostTime, options);
            formatterResolver.GetFormatterWithVerify<global::IMessageBody>().Serialize(ref writer, value.Body, options);
        }

        public global::Message Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __PostTime__ = formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __Body__ = formatterResolver.GetFormatterWithVerify<global::IMessageBody>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::TextMessageBody value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Text, options);
        }

        public global::TextMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Text__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Text__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::StampMessageBody value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.StampId);
        }

        public global::StampMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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


        public void Serialize(ref MessagePackWriter writer, global::QuestMessageBody value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.QuestId);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Text, options);
        }

        public global::QuestMessageBody Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __Text__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
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


        public void Serialize(ref MessagePackWriter writer, global::ArrayTestTest value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(7);
            formatterResolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.MyProperty0, options);
            formatterResolver.GetFormatterWithVerify<int[,]>().Serialize(ref writer, value.MyProperty1, options);
            formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[,]>().Serialize(ref writer, value.MyProperty2, options);
            formatterResolver.GetFormatterWithVerify<int[,,]>().Serialize(ref writer, value.MyProperty3, options);
            formatterResolver.GetFormatterWithVerify<int[,,,]>().Serialize(ref writer, value.MyProperty4, options);
            formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[]>().Serialize(ref writer, value.MyProperty5, options);
            formatterResolver.GetFormatterWithVerify<global::QuestMessageBody[]>().Serialize(ref writer, value.MyProperty6, options);
        }

        public global::ArrayTestTest Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                        __MyProperty0__ = formatterResolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int[,]>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __MyProperty2__ = formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[,]>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __MyProperty3__ = formatterResolver.GetFormatterWithVerify<int[,,]>().Deserialize(ref reader, options);
                        break;
                    case 4:
                        __MyProperty4__ = formatterResolver.GetFormatterWithVerify<int[,,,]>().Deserialize(ref reader, options);
                        break;
                    case 5:
                        __MyProperty5__ = formatterResolver.GetFormatterWithVerify<global::GlobalMyEnum[]>().Deserialize(ref reader, options);
                        break;
                    case 6:
                        __MyProperty6__ = formatterResolver.GetFormatterWithVerify<global::QuestMessageBody[]>().Deserialize(ref reader, options);
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public SimpleModelFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "Id", 0 },
                { "Name", 1 },
                { "CreatedOn", 2 },
                { "Precision", 3 },
                { "Money", 4 },
                { "Amount", 5 },
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

        public void Serialize(ref MessagePackWriter writer, global::SimpleModel value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(6);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.Id);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.WriteRaw(this.____stringByteKeys[2]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Serialize(ref writer, value.CreatedOn, options);
            writer.WriteRaw(this.____stringByteKeys[3]);
            writer.Write(value.Precision);
            writer.WriteRaw(this.____stringByteKeys[4]);
            formatterResolver.GetFormatterWithVerify<decimal>().Serialize(ref writer, value.Money, options);
            writer.WriteRaw(this.____stringByteKeys[5]);
            writer.Write(value.Amount);
        }

        public global::SimpleModel Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __Id__ = default(int);
            var __Name__ = default(string);
            var __CreatedOn__ = default(global::System.DateTime);
            var __Precision__ = default(int);
            var __Money__ = default(decimal);
            var __Amount__ = default(long);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __CreatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __Precision__ = reader.ReadInt32();
                        break;
                    case 4:
                        __Money__ = formatterResolver.GetFormatterWithVerify<decimal>().Deserialize(ref reader, options);
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


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public ComplexModelFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "AdditionalProperty", 0 },
                { "CreatedOn", 1 },
                { "Id", 2 },
                { "Name", 3 },
                { "UpdatedOn", 4 },
                { "SimpleModels", 5 },
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

        public void Serialize(ref MessagePackWriter writer, global::ComplexModel value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(6);
            writer.WriteRaw(this.____stringByteKeys[0]);
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IDictionary<string, string>>().Serialize(ref writer, value.AdditionalProperty, options);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Serialize(ref writer, value.CreatedOn, options);
            writer.WriteRaw(this.____stringByteKeys[2]);
            formatterResolver.GetFormatterWithVerify<global::System.Guid>().Serialize(ref writer, value.Id, options);
            writer.WriteRaw(this.____stringByteKeys[3]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.WriteRaw(this.____stringByteKeys[4]);
            formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Serialize(ref writer, value.UpdatedOn, options);
            writer.WriteRaw(this.____stringByteKeys[5]);
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<global::SimpleModel>>().Serialize(ref writer, value.SimpleModels, options);
        }

        public global::ComplexModel Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __AdditionalProperty__ = default(global::System.Collections.Generic.IDictionary<string, string>);
            var __CreatedOn__ = default(global::System.DateTimeOffset);
            var __Id__ = default(global::System.Guid);
            var __Name__ = default(string);
            var __UpdatedOn__ = default(global::System.DateTimeOffset);
            var __SimpleModels__ = default(global::System.Collections.Generic.IList<global::SimpleModel>);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __AdditionalProperty__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IDictionary<string, string>>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __CreatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __Id__ = formatterResolver.GetFormatterWithVerify<global::System.Guid>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 4:
                        __UpdatedOn__ = formatterResolver.GetFormatterWithVerify<global::System.DateTimeOffset>().Deserialize(ref reader, options);
                        break;
                    case 5:
                        __SimpleModels__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.IList<global::SimpleModel>>().Deserialize(ref reader, options);
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

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.PerfBenchmarkDotNet
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class StringKeySerializerTargetFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::PerfBenchmarkDotNet.StringKeySerializerTarget>
    {


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public StringKeySerializerTargetFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty1", 0 },
                { "MyProperty2", 1 },
                { "MyProperty3", 2 },
                { "MyProperty4", 3 },
                { "MyProperty5", 4 },
                { "MyProperty6", 5 },
                { "MyProperty7", 6 },
                { "MyProperty8", 7 },
                { "MyProperty9", 8 },
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

        public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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

        public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.MessagePack.Tests
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class DynamicObjectResolverConstructorTest_TestConstructor1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor1>
    {


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public DynamicObjectResolverConstructorTest_TestConstructor1Formatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "X", 0 },
                { "Y", 1 },
                { "Z", 2 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("X"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Y"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Z"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.X);
            writer.WriteRaw(this.____stringByteKeys[1]);
            writer.Write(value.Y);
            writer.WriteRaw(this.____stringByteKeys[2]);
            writer.Write(value.Z);
        }

        public global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __X__ = default(int);
            var __Y__ = default(int);
            var __Z__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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
                        __Y__ = reader.ReadInt32();
                        break;
                    case 2:
                        __Z__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor1(__X__, __Y__, __Z__);
            return ____result;
        }
    }

    public sealed class DynamicObjectResolverConstructorTest_TestConstructor2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor2>
    {


        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __X__ = default(int);
            var __Y__ = default(int);
            var __Z__ = default(int);

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
                        __Z__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor2(__X__, __Y__, __Z__);
            return ____result;
        }
    }

    public sealed class DynamicObjectResolverConstructorTest_TestConstructor3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor3>
    {


        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __X__ = default(int);
            var __Y__ = default(int);
            var __Z__ = default(int);

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
                        __Z__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.DynamicObjectResolverConstructorTest.TestConstructor3(__X__, __Y__);
            return ____result;
        }
    }

    public sealed class DynamicObjectResolverOrderTest_OrderOrderFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.DynamicObjectResolverOrderTest.OrderOrder>
    {


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public DynamicObjectResolverOrderTest_OrderOrderFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "Foo", 0 },
                { "Moge", 1 },
                { "FooBar", 2 },
                { "NoBar", 3 },
                { "Bar", 4 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Foo"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Moge"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("FooBar"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("NoBar"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Bar"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.DynamicObjectResolverOrderTest.OrderOrder value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(5);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.Foo);
            writer.WriteRaw(this.____stringByteKeys[1]);
            writer.Write(value.Moge);
            writer.WriteRaw(this.____stringByteKeys[2]);
            writer.Write(value.FooBar);
            writer.WriteRaw(this.____stringByteKeys[3]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.NoBar, options);
            writer.WriteRaw(this.____stringByteKeys[4]);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Bar, options);
        }

        public global::MessagePack.Tests.DynamicObjectResolverOrderTest.OrderOrder Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __Foo__ = default(int);
            var __Moge__ = default(int);
            var __FooBar__ = default(int);
            var __NoBar__ = default(string);
            var __Bar__ = default(string);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __Foo__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Moge__ = reader.ReadInt32();
                        break;
                    case 2:
                        __FooBar__ = reader.ReadInt32();
                        break;
                    case 3:
                        __NoBar__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 4:
                        __Bar__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.DynamicObjectResolverOrderTest.OrderOrder();
            ____result.Foo = __Foo__;
            ____result.Moge = __Moge__;
            ____result.FooBar = __FooBar__;
            ____result.NoBar = __NoBar__;
            ____result.Bar = __Bar__;
            return ____result;
        }
    }

    public sealed class IgnoreTest_ViewModelFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.IgnoreTest.ViewModel>
    {


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public IgnoreTest_ViewModelFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty1", 0 },
                { "MyProperty2", 1 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty1"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty2"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.IgnoreTest.ViewModel value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.MyProperty1);
            writer.WriteRaw(this.____stringByteKeys[1]);
            writer.Write(value.MyProperty2);
        }

        public global::MessagePack.Tests.IgnoreTest.ViewModel Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
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
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.IgnoreTest.ViewModel();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            return ____result;
        }
    }

    public sealed class MessagePackFormatterPerFieldTest_MyClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyClass>
    {
        global::MessagePack.Tests.MessagePackFormatterPerFieldTest.Int_x10Formatter __MyProperty1CustomFormatter__ = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.Int_x10Formatter();
        global::MessagePack.Tests.MessagePackFormatterPerFieldTest.String_x2Formatter __MyProperty3CustomFormatter__ = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.String_x2Formatter();


        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            this.__MyProperty1CustomFormatter__.Serialize(ref writer, value.MyProperty1, options);
            writer.Write(value.MyProperty2);
            this.__MyProperty3CustomFormatter__.Serialize(ref writer, value.MyProperty3, options);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty4, options);
        }

        public global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(string);
            var __MyProperty4__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = this.__MyProperty1CustomFormatter__.Deserialize(ref reader, options);
                        break;
                    case 1:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty3__ = this.__MyProperty3CustomFormatter__.Deserialize(ref reader, options);
                        break;
                    case 3:
                        __MyProperty4__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyClass();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty4 = __MyProperty4__;
            return ____result;
        }
    }

    public sealed class MessagePackFormatterPerFieldTest_MyStructFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyStruct>
    {
        global::MessagePack.Tests.MessagePackFormatterPerFieldTest.Int_x10Formatter __MyProperty1CustomFormatter__ = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.Int_x10Formatter();
        global::MessagePack.Tests.MessagePackFormatterPerFieldTest.String_x2Formatter __MyProperty3CustomFormatter__ = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.String_x2Formatter();


        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyStruct value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            this.__MyProperty1CustomFormatter__.Serialize(ref writer, value.MyProperty1, options);
            writer.Write(value.MyProperty2);
            this.__MyProperty3CustomFormatter__.Serialize(ref writer, value.MyProperty3, options);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty4, options);
        }

        public global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyStruct Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(int);
            var __MyProperty2__ = default(int);
            var __MyProperty3__ = default(string);
            var __MyProperty4__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = this.__MyProperty1CustomFormatter__.Deserialize(ref reader, options);
                        break;
                    case 1:
                        __MyProperty2__ = reader.ReadInt32();
                        break;
                    case 2:
                        __MyProperty3__ = this.__MyProperty3CustomFormatter__.Deserialize(ref reader, options);
                        break;
                    case 3:
                        __MyProperty4__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.MessagePackFormatterPerFieldTest.MyStruct();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty3 = __MyProperty3__;
            ____result.MyProperty4 = __MyProperty4__;
            return ____result;
        }
    }

    public sealed class NewGuidFormatterTest_InClassFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.NewGuidFormatterTest.InClass>
    {


        private readonly global::MessagePack.Internal.AutomataDictionary ____keyMapping;
        private readonly byte[][] ____stringByteKeys;

        public NewGuidFormatterTest_InClassFormatter()
        {
            this.____keyMapping = new global::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty", 0 },
                { "Guid", 1 },
            };

            this.____stringByteKeys = new byte[][]
            {
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("MyProperty"),
                global::MessagePack.Internal.CodeGenHelpers.GetEncodedStringBytes("Guid"),
            };
        }

        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.NewGuidFormatterTest.InClass value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(this.____stringByteKeys[0]);
            writer.Write(value.MyProperty);
            writer.WriteRaw(this.____stringByteKeys[1]);
            formatterResolver.GetFormatterWithVerify<global::System.Guid>().Serialize(ref writer, value.Guid, options);
        }

        public global::MessagePack.Tests.NewGuidFormatterTest.InClass Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __MyProperty__ = default(int);
            var __Guid__ = default(global::System.Guid);

            for (int i = 0; i < length; i++)
            {
                ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                int key;
                if (!this.____keyMapping.TryGetValue(stringKey, out key))
                {
                    reader.Skip();
                    continue;
                }

                switch (key)
                {
                    case 0:
                        __MyProperty__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Guid__ = formatterResolver.GetFormatterWithVerify<global::System.Guid>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.NewGuidFormatterTest.InClass();
            ____result.MyProperty = __MyProperty__;
            ____result.Guid = __Guid__;
            return ____result;
        }
    }

    public sealed class PrimitivelikeFormatterTest_MyDateTimeResolverTestFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessagePack.Tests.PrimitivelikeFormatterTest.MyDateTimeResolverTest>
    {


        public void Serialize(ref MessagePackWriter writer, global::MessagePack.Tests.PrimitivelikeFormatterTest.MyDateTimeResolverTest value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Serialize(ref writer, value.MyProperty1, options);
        }

        public global::MessagePack.Tests.PrimitivelikeFormatterTest.MyDateTimeResolverTest Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MyProperty1__ = default(global::System.DateTime);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __MyProperty1__ = formatterResolver.GetFormatterWithVerify<global::System.DateTime>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessagePack.Tests.PrimitivelikeFormatterTest.MyDateTimeResolverTest();
            ____result.MyProperty1 = __MyProperty1__;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.ComplexdUnion
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class BFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.B>
    {


        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.B value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.Write(value.Val);
        }

        public global::ComplexdUnion.B Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Name__ = default(string);
            var __Val__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __Val__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ComplexdUnion.B();
            ____result.Name = __Name__;
            ____result.Val = __Val__;
            return ____result;
        }
    }

    public sealed class CFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.C>
    {


        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.C value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.Write(value.Val);
            writer.Write(value.Valer);
        }

        public global::ComplexdUnion.C Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Name__ = default(string);
            var __Val__ = default(int);
            var __Valer__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __Val__ = reader.ReadInt32();
                        break;
                    case 2:
                        __Valer__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ComplexdUnion.C();
            ____result.Name = __Name__;
            ____result.Val = __Val__;
            ____result.Valer = __Valer__;
            return ____result;
        }
    }

    public sealed class B2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.B2>
    {


        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.B2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.Write(value.Val);
        }

        public global::ComplexdUnion.B2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Name__ = default(string);
            var __Val__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __Val__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ComplexdUnion.B2();
            ____result.Name = __Name__;
            ____result.Val = __Val__;
            return ____result;
        }
    }

    public sealed class C2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ComplexdUnion.C2>
    {


        public void Serialize(ref MessagePackWriter writer, global::ComplexdUnion.C2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
            writer.Write(value.Val);
            writer.Write(value.Valer);
        }

        public global::ComplexdUnion.C2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Valer__ = default(int);
            var __Name__ = default(string);
            var __Val__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 2:
                        __Valer__ = reader.ReadInt32();
                        break;
                    case 0:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __Val__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::ComplexdUnion.C2();
            ____result.Valer = __Valer__;
            ____result.Name = __Name__;
            ____result.Val = __Val__;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.ClassUnion
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class SubUnionType1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ClassUnion.SubUnionType1>
    {


        public void Serialize(ref MessagePackWriter writer, global::ClassUnion.SubUnionType1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty1);
        }

        public global::ClassUnion.SubUnionType1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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

            var ____result = new global::ClassUnion.SubUnionType1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }

    public sealed class SubUnionType2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::ClassUnion.SubUnionType2>
    {


        public void Serialize(ref MessagePackWriter writer, global::ClassUnion.SubUnionType2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.MyProperty);
            writer.Write(value.MyProperty2);
        }

        public global::ClassUnion.SubUnionType2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
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

            var ____result = new global::ClassUnion.SubUnionType2();
            ____result.MyProperty2 = __MyProperty2__;
            ____result.MyProperty = __MyProperty__;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name


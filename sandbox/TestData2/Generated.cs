// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

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
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(13)
            {
                { typeof(global::System.Collections.Generic.List<global::TestData2.A>), 0 },
                { typeof(global::System.Collections.Generic.List<global::TestData2.B>), 1 },
                { typeof(global::TestData2.Nest1.Id), 2 },
                { typeof(global::TestData2.Nest2.Id), 3 },
                { typeof(global::TestData2.A), 4 },
                { typeof(global::TestData2.B), 5 },
                { typeof(global::TestData2.C), 6 },
                { typeof(global::TestData2.Nest1), 7 },
                { typeof(global::TestData2.Nest1.IdType), 8 },
                { typeof(global::TestData2.Nest2), 9 },
                { typeof(global::TestData2.Nest2.IdType), 10 },
                { typeof(global::TestData2.PropNameCheck1), 11 },
                { typeof(global::TestData2.PropNameCheck2), 12 },
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
                case 0: return new global::MessagePack.Formatters.ListFormatter<global::TestData2.A>();
                case 1: return new global::MessagePack.Formatters.ListFormatter<global::TestData2.B>();
                case 2: return new MessagePack.Formatters.TestData2.Nest1_IdFormatter();
                case 3: return new MessagePack.Formatters.TestData2.Nest2_IdFormatter();
                case 4: return new MessagePack.Formatters.TestData2.AFormatter();
                case 5: return new MessagePack.Formatters.TestData2.BFormatter();
                case 6: return new MessagePack.Formatters.TestData2.CFormatter();
                case 7: return new MessagePack.Formatters.TestData2.Nest1Formatter();
                case 8: return new MessagePack.Formatters.TestData2.Nest1_IdTypeFormatter();
                case 9: return new MessagePack.Formatters.TestData2.Nest2Formatter();
                case 10: return new MessagePack.Formatters.TestData2.Nest2_IdTypeFormatter();
                case 11: return new MessagePack.Formatters.TestData2.PropNameCheck1Formatter();
                case 12: return new MessagePack.Formatters.TestData2.PropNameCheck2Formatter();
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


// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.TestData2
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class Nest1_IdFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1.Id>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest1.Id value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((Int32)value);
        }

        public global::TestData2.Nest1.Id Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::TestData2.Nest1.Id)reader.ReadInt32();
        }
    }

    public sealed class Nest2_IdFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2.Id>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest2.Id value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((Int32)value);
        }

        public global::TestData2.Nest2.Id Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::TestData2.Nest2.Id)reader.ReadInt32();
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



// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

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

namespace MessagePack.Formatters.TestData2
{
    using System;
    using System.Buffers;
    using System.Runtime.InteropServices;
    using MessagePack;

    public sealed class AFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.A>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.A value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            // a
            writer.WriteRaw(new byte[] { 0xA1, 0x61, });
            writer.Write(value.a);
            // bs
            writer.WriteRaw(new byte[] { 0xA2, 0x62, 0x73, });
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>().Serialize(ref writer, value.bs, options);
            // c
            writer.WriteRaw(new byte[] { 0xA1, 0x63, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Serialize(ref writer, value.c, options);
        }

        public global::TestData2.A Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __a__ = default(int);
            var __bs__ = default(global::System.Collections.Generic.List<global::TestData2.B>);
            var __c__ = default(global::TestData2.C);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 1:
                        {
                            uint last = stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x61U:
                                    __a__ = reader.ReadInt32();
                                    continue;
                                case 0x63U:
                                    __c__ = formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                    case 2:
                        {
                            uint last = stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x7362U:
                                    __bs__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.A();
            ____result.a = __a__;
            ____result.bs = __bs__;
            ____result.c = __c__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class BFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.B>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.B value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            // ass
            writer.WriteRaw(new byte[] { 0xA3, 0x61, 0x73, 0x73, });
            formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>().Serialize(ref writer, value.ass, options);
            // c
            writer.WriteRaw(new byte[] { 0xA1, 0x63, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Serialize(ref writer, value.c, options);
            // a
            writer.WriteRaw(new byte[] { 0xA1, 0x61, });
            writer.Write(value.a);
        }

        public global::TestData2.B Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __ass__ = default(global::System.Collections.Generic.List<global::TestData2.A>);
            var __c__ = default(global::TestData2.C);
            var __a__ = default(int);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 1:
                        {
                            uint last = stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x61U:
                                    __a__ = reader.ReadInt32();
                                    continue;
                                case 0x63U:
                                    __c__ = formatterResolver.GetFormatterWithVerify<global::TestData2.C>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                    case 3:
                        {
                            uint last = stringKey[2];
                            last <<= 8;
                            last |= stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x737361U:
                                    __ass__ = formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.B();
            ____result.ass = __ass__;
            ____result.c = __c__;
            ____result.a = __a__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class CFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.C>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.C value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            // b
            writer.WriteRaw(new byte[] { 0xA1, 0x62, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.B>().Serialize(ref writer, value.b, options);
            // a
            writer.WriteRaw(new byte[] { 0xA1, 0x61, });
            writer.Write(value.a);
        }

        public global::TestData2.C Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __b__ = default(global::TestData2.B);
            var __a__ = default(int);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 1:
                        {
                            uint last = stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x61U:
                                    __a__ = reader.ReadInt32();
                                    continue;
                                case 0x62U:
                                    __b__ = formatterResolver.GetFormatterWithVerify<global::TestData2.B>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.C();
            ____result.b = __b__;
            ____result.a = __a__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class Nest1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            // EnumId
            writer.WriteRaw(new byte[] { 0xA6, 0x45, 0x6E, 0x75, 0x6D, 0x49, 0x64, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.Nest1.Id>().Serialize(ref writer, value.EnumId, options);
            // ClassId
            writer.WriteRaw(new byte[] { 0xA7, 0x43, 0x6C, 0x61, 0x73, 0x73, 0x49, 0x64, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.Nest1.IdType>().Serialize(ref writer, value.ClassId, options);
        }

        public global::TestData2.Nest1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __EnumId__ = default(global::TestData2.Nest1.Id);
            var __ClassId__ = default(global::TestData2.Nest1.IdType);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 6:
                        {
                            ulong last = stringKey[5];
                            last <<= 8;
                            last |= stringKey[4];
                            last <<= 8;
                            last |= stringKey[3];
                            last <<= 8;
                            last |= stringKey[2];
                            last <<= 8;
                            last |= stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x64496D756E45UL:
                                    __EnumId__ = formatterResolver.GetFormatterWithVerify<global::TestData2.Nest1.Id>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                    case 7:
                        {
                            ulong last = stringKey[6];
                            last <<= 8;
                            last |= stringKey[5];
                            last <<= 8;
                            last |= stringKey[4];
                            last <<= 8;
                            last |= stringKey[3];
                            last <<= 8;
                            last |= stringKey[2];
                            last <<= 8;
                            last |= stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x64497373616C43UL:
                                    __ClassId__ = formatterResolver.GetFormatterWithVerify<global::TestData2.Nest1.IdType>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.Nest1();
            ____result.EnumId = __EnumId__;
            ____result.ClassId = __ClassId__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class Nest1_IdTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1.IdType>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest1.IdType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(0);
        }

        public global::TestData2.Nest1.IdType Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                reader.Skip();
                reader.Skip();
            }

            var ____result = new global::TestData2.Nest1.IdType();
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class Nest2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            // EnumId
            writer.WriteRaw(new byte[] { 0xA6, 0x45, 0x6E, 0x75, 0x6D, 0x49, 0x64, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.Nest2.Id>().Serialize(ref writer, value.EnumId, options);
            // ClassId
            writer.WriteRaw(new byte[] { 0xA7, 0x43, 0x6C, 0x61, 0x73, 0x73, 0x49, 0x64, });
            formatterResolver.GetFormatterWithVerify<global::TestData2.Nest2.IdType>().Serialize(ref writer, value.ClassId, options);
        }

        public global::TestData2.Nest2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __EnumId__ = default(global::TestData2.Nest2.Id);
            var __ClassId__ = default(global::TestData2.Nest2.IdType);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 6:
                        {
                            ulong last = stringKey[5];
                            last <<= 8;
                            last |= stringKey[4];
                            last <<= 8;
                            last |= stringKey[3];
                            last <<= 8;
                            last |= stringKey[2];
                            last <<= 8;
                            last |= stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x64496D756E45UL:
                                    __EnumId__ = formatterResolver.GetFormatterWithVerify<global::TestData2.Nest2.Id>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                    case 7:
                        {
                            ulong last = stringKey[6];
                            last <<= 8;
                            last |= stringKey[5];
                            last <<= 8;
                            last |= stringKey[4];
                            last <<= 8;
                            last |= stringKey[3];
                            last <<= 8;
                            last |= stringKey[2];
                            last <<= 8;
                            last |= stringKey[1];
                            last <<= 8;
                            last |= stringKey[0];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x64497373616C43UL:
                                    __ClassId__ = formatterResolver.GetFormatterWithVerify<global::TestData2.Nest2.IdType>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.Nest2();
            ____result.EnumId = __EnumId__;
            ____result.ClassId = __ClassId__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class Nest2_IdTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2.IdType>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.Nest2.IdType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(0);
        }

        public global::TestData2.Nest2.IdType Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                reader.Skip();
                reader.Skip();
            }

            var ____result = new global::TestData2.Nest2.IdType();
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class PropNameCheck1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.PropNameCheck1>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.PropNameCheck1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            // MyProperty1
            writer.WriteRaw(new byte[] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x31, });
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty1, options);
            // MyProperty2
            writer.WriteRaw(new byte[] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32, });
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty2, options);
        }

        public global::TestData2.PropNameCheck1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __MyProperty1__ = default(string);
            var __MyProperty2__ = default(string);
            var isBigEndian = !global::System.BitConverter.IsLittleEndian;

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                ReadOnlySpan<ulong> ulongs = isBigEndian ? stackalloc ulong[stringKey.Length >> 3] : MemoryMarshal.Cast<byte, ulong>(stringKey);
                if (isBigEndian)
                {
                    for (var index = 0; index < ulongs.Length; index++)
                    {
                        var index8times = index << 3;
                        ref var number = ref global::System.Runtime.CompilerServices.Unsafe.AsRef(ulongs[index]);
                        number = stringKey[index8times + 7];
                        for (var numberIndex = index8times + 6; numberIndex >= index8times; numberIndex--)
                        {
                            number <<= 8;
                            number |= stringKey[numberIndex];
                        }
                    }
                }

                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 11:
                        if (ulongs[0] != 0x7265706F7250794DUL) goto FAIL; // MyProper
                        {
                            uint last = stringKey[10];
                            last <<= 8;
                            last |= stringKey[9];
                            last <<= 8;
                            last |= stringKey[8];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x317974U:
                                    __MyProperty1__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                                    continue;
                                case 0x327974U:
                                    __MyProperty2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.PropNameCheck1();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            reader.Depth--;
            return ____result;
        }
    }
    public sealed class PropNameCheck2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.PropNameCheck2>
    {
        public void Serialize(ref MessagePackWriter writer, global::TestData2.PropNameCheck2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            // MyProperty1
            writer.WriteRaw(new byte[] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x31, });
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty1, options);
            // MyProperty2
            writer.WriteRaw(new byte[] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32, });
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.MyProperty2, options);
        }

        public global::TestData2.PropNameCheck2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var __MyProperty1__ = default(string);
            var __MyProperty2__ = default(string);
            var isBigEndian = !global::System.BitConverter.IsLittleEndian;

            for (int i = 0, length = reader.ReadMapHeader(); i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                ReadOnlySpan<ulong> ulongs = isBigEndian ? stackalloc ulong[stringKey.Length >> 3] : MemoryMarshal.Cast<byte, ulong>(stringKey);
                if (isBigEndian)
                {
                    for (var index = 0; index < ulongs.Length; index++)
                    {
                        var index8times = index << 3;
                        ref var number = ref global::System.Runtime.CompilerServices.Unsafe.AsRef(ulongs[index]);
                        number = stringKey[index8times + 7];
                        for (var numberIndex = index8times + 6; numberIndex >= index8times; numberIndex--)
                        {
                            number <<= 8;
                            number |= stringKey[numberIndex];
                        }
                    }
                }

                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                        reader.Skip();
                        continue;
                    case 11:
                        if (ulongs[0] != 0x7265706F7250794DUL) goto FAIL; // MyProper
                        {
                            uint last = stringKey[10];
                            last <<= 8;
                            last |= stringKey[9];
                            last <<= 8;
                            last |= stringKey[8];
                            switch (last)
                            {
                                default: goto FAIL;
                                case 0x317974U:
                                    __MyProperty1__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                                    continue;
                                case 0x327974U:
                                    __MyProperty2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                                    continue;
                            }
                        }
                }
            }

            var ____result = new global::TestData2.PropNameCheck2();
            ____result.MyProperty1 = __MyProperty1__;
            ____result.MyProperty2 = __MyProperty2__;
            reader.Depth--;
            return ____result;
        }
    }
}


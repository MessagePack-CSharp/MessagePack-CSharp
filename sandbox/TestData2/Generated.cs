// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
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
        private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<global::System.Type, int>(14)
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
                { typeof(global::TestData2.Record), 13 },
            };
        }

        internal static object GetFormatter(global::System.Type t)
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
                case 13: return new MessagePack.Formatters.TestData2.RecordFormatter();
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
#pragma warning restore SA1649 // File name should match first type name


// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.TestData2
{

    public sealed class Nest1_IdFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1.Id>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest1.Id value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((global::System.Int32)value);
        }

        public global::TestData2.Nest1.Id Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::TestData2.Nest1.Id)reader.ReadInt32();
        }
    }

    public sealed class Nest2_IdFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2.Id>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest2.Id value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((global::System.Int32)value);
        }

        public global::TestData2.Nest2.Id Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::TestData2.Nest2.Id)reader.ReadInt32();
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

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
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.TestData2
{
    public sealed class AFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.A>
    {
        // a
        private static global::System.ReadOnlySpan<byte> GetSpan_a() => new byte[1 + 1] { 0xA1, 0x61 };
        // bs
        private static global::System.ReadOnlySpan<byte> GetSpan_bs() => new byte[1 + 2] { 0xA2, 0x62, 0x73 };
        // c
        private static global::System.ReadOnlySpan<byte> GetSpan_c() => new byte[1 + 1] { 0xA1, 0x63 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.A value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            writer.WriteRaw(GetSpan_a());
            writer.Write(value.a);
            writer.WriteRaw(GetSpan_bs());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>(formatterResolver).Serialize(ref writer, value.bs, options);
            writer.WriteRaw(GetSpan_c());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.C>(formatterResolver).Serialize(ref writer, value.c, options);
        }

        public global::TestData2.A Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.A();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 1:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 97UL:
                                ____result.a = reader.ReadInt32();
                                continue;
                            case 99UL:
                                ____result.c = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.C>(formatterResolver).Deserialize(ref reader, options);
                                continue;
                        }
                    case 2:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 29538UL) { goto FAIL; }

                        ____result.bs = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.B>>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class BFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.B>
    {
        // ass
        private static global::System.ReadOnlySpan<byte> GetSpan_ass() => new byte[1 + 3] { 0xA3, 0x61, 0x73, 0x73 };
        // c
        private static global::System.ReadOnlySpan<byte> GetSpan_c() => new byte[1 + 1] { 0xA1, 0x63 };
        // a
        private static global::System.ReadOnlySpan<byte> GetSpan_a() => new byte[1 + 1] { 0xA1, 0x61 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.B value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(3);
            writer.WriteRaw(GetSpan_ass());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>(formatterResolver).Serialize(ref writer, value.ass, options);
            writer.WriteRaw(GetSpan_c());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.C>(formatterResolver).Serialize(ref writer, value.c, options);
            writer.WriteRaw(GetSpan_a());
            writer.Write(value.a);
        }

        public global::TestData2.B Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.B();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 3:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 7566177UL) { goto FAIL; }

                        ____result.ass = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.List<global::TestData2.A>>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                    case 1:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 99UL:
                                ____result.c = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.C>(formatterResolver).Deserialize(ref reader, options);
                                continue;
                            case 97UL:
                                ____result.a = reader.ReadInt32();
                                continue;
                        }
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class CFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.C>
    {
        // b
        private static global::System.ReadOnlySpan<byte> GetSpan_b() => new byte[1 + 1] { 0xA1, 0x62 };
        // a
        private static global::System.ReadOnlySpan<byte> GetSpan_a() => new byte[1 + 1] { 0xA1, 0x61 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.C value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_b());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.B>(formatterResolver).Serialize(ref writer, value.b, options);
            writer.WriteRaw(GetSpan_a());
            writer.Write(value.a);
        }

        public global::TestData2.C Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.C();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 1:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 98UL:
                                ____result.b = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.B>(formatterResolver).Deserialize(ref reader, options);
                                continue;
                            case 97UL:
                                ____result.a = reader.ReadInt32();
                                continue;
                        }
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class Nest1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1>
    {
        // EnumId
        private static global::System.ReadOnlySpan<byte> GetSpan_EnumId() => new byte[1 + 6] { 0xA6, 0x45, 0x6E, 0x75, 0x6D, 0x49, 0x64 };
        // ClassId
        private static global::System.ReadOnlySpan<byte> GetSpan_ClassId() => new byte[1 + 7] { 0xA7, 0x43, 0x6C, 0x61, 0x73, 0x73, 0x49, 0x64 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_EnumId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest1.Id>(formatterResolver).Serialize(ref writer, value.EnumId, options);
            writer.WriteRaw(GetSpan_ClassId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest1.IdType>(formatterResolver).Serialize(ref writer, value.ClassId, options);
        }

        public global::TestData2.Nest1 Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.Nest1();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 6:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 110266531802693UL) { goto FAIL; }

                        ____result.EnumId = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest1.Id>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                    case 7:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 28228257876896835UL) { goto FAIL; }

                        ____result.ClassId = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest1.IdType>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class Nest1_IdTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest1.IdType>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest1.IdType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(0);
        }

        public global::TestData2.Nest1.IdType Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            reader.Skip();
            var ____result = new global::TestData2.Nest1.IdType();
            return ____result;
        }
    }

    public sealed class Nest2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2>
    {
        // EnumId
        private static global::System.ReadOnlySpan<byte> GetSpan_EnumId() => new byte[1 + 6] { 0xA6, 0x45, 0x6E, 0x75, 0x6D, 0x49, 0x64 };
        // ClassId
        private static global::System.ReadOnlySpan<byte> GetSpan_ClassId() => new byte[1 + 7] { 0xA7, 0x43, 0x6C, 0x61, 0x73, 0x73, 0x49, 0x64 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_EnumId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest2.Id>(formatterResolver).Serialize(ref writer, value.EnumId, options);
            writer.WriteRaw(GetSpan_ClassId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest2.IdType>(formatterResolver).Serialize(ref writer, value.ClassId, options);
        }

        public global::TestData2.Nest2 Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.Nest2();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 6:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 110266531802693UL) { goto FAIL; }

                        ____result.EnumId = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest2.Id>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                    case 7:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 28228257876896835UL) { goto FAIL; }

                        ____result.ClassId = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::TestData2.Nest2.IdType>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class Nest2_IdTypeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Nest2.IdType>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Nest2.IdType value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(0);
        }

        public global::TestData2.Nest2.IdType Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            reader.Skip();
            var ____result = new global::TestData2.Nest2.IdType();
            return ____result;
        }
    }

    public sealed class PropNameCheck1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.PropNameCheck1>
    {
        // MyProperty1
        private static global::System.ReadOnlySpan<byte> GetSpan_MyProperty1() => new byte[1 + 11] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x31 };
        // MyProperty2
        private static global::System.ReadOnlySpan<byte> GetSpan_MyProperty2() => new byte[1 + 11] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.PropNameCheck1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_MyProperty1());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.MyProperty1, options);
            writer.WriteRaw(GetSpan_MyProperty2());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.MyProperty2, options);
        }

        public global::TestData2.PropNameCheck1 Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.PropNameCheck1();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 11:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 8243118316933118285UL:
                                switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                                {
                                    default: goto FAIL;
                                    case 3242356UL:
                                        ____result.MyProperty1 = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                                        continue;
                                    case 3307892UL:
                                        ____result.MyProperty2 = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                                        continue;
                                }
                        }
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class PropNameCheck2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.PropNameCheck2>
    {
        // MyProperty1
        private static global::System.ReadOnlySpan<byte> GetSpan_MyProperty1() => new byte[1 + 11] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x31 };
        // MyProperty2
        private static global::System.ReadOnlySpan<byte> GetSpan_MyProperty2() => new byte[1 + 11] { 0xAB, 0x4D, 0x79, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.PropNameCheck2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_MyProperty1());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.MyProperty1, options);
            writer.WriteRaw(GetSpan_MyProperty2());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.MyProperty2, options);
        }

        public global::TestData2.PropNameCheck2 Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var ____result = new global::TestData2.PropNameCheck2();

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 11:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 8243118316933118285UL:
                                switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                                {
                                    default: goto FAIL;
                                    case 3242356UL:
                                        ____result.MyProperty1 = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                                        continue;
                                    case 3307892UL:
                                        ____result.MyProperty2 = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                                        continue;
                                }
                        }
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

    public sealed class RecordFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::TestData2.Record>
    {
        // SomeProperty
        private static global::System.ReadOnlySpan<byte> GetSpan_SomeProperty() => new byte[1 + 12] { 0xAC, 0x53, 0x6F, 0x6D, 0x65, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::TestData2.Record value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(1);
            writer.WriteRaw(GetSpan_SomeProperty());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.SomeProperty, options);
        }

        public global::TestData2.Record Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __SomeProperty__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 12:
                        if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_SomeProperty().Slice(1))) { goto FAIL; }

                        __SomeProperty__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                }
            }

            var ____result = new global::TestData2.Record(__SomeProperty__);
            reader.Depth--;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

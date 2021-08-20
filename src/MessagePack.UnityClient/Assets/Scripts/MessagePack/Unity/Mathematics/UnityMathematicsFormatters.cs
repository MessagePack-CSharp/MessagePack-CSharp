// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if USE_UNITY_MATHEMATICS

using System;

#pragma warning disable SA1312 // variable naming
#pragma warning disable SA1402 // one type per file
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity
{
    public sealed class Bool2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.bool2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(bool);
            var __y__ = default(bool);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadBoolean();
                        break;
                    case 1:
                        __y__ = reader.ReadBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class Bool2x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool2x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool2x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.bool2x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool2);
            var __c1__ = default(global::Unity.Mathematics.bool2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool2x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Bool2x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool2x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool2x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.bool2x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool2);
            var __c1__ = default(global::Unity.Mathematics.bool2);
            var __c2__ = default(global::Unity.Mathematics.bool2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool2x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Bool2x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool2x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool2x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.bool2x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool2);
            var __c1__ = default(global::Unity.Mathematics.bool2);
            var __c2__ = default(global::Unity.Mathematics.bool2);
            var __c3__ = default(global::Unity.Mathematics.bool2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool2x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Bool3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.bool3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(bool);
            var __y__ = default(bool);
            var __z__ = default(bool);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadBoolean();
                        break;
                    case 1:
                        __y__ = reader.ReadBoolean();
                        break;
                    case 2:
                        __z__ = reader.ReadBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class Bool3x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool3x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.bool3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool3);
            var __c1__ = default(global::Unity.Mathematics.bool3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool3x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Bool3x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool3x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool3x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.bool3x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool3);
            var __c1__ = default(global::Unity.Mathematics.bool3);
            var __c2__ = default(global::Unity.Mathematics.bool3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool3x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Bool3x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool3x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool3x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.bool3x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool3);
            var __c1__ = default(global::Unity.Mathematics.bool3);
            var __c2__ = default(global::Unity.Mathematics.bool3);
            var __c3__ = default(global::Unity.Mathematics.bool3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool3x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Bool4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.bool4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(bool);
            var __y__ = default(bool);
            var __z__ = default(bool);
            var __w__ = default(bool);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadBoolean();
                        break;
                    case 1:
                        __y__ = reader.ReadBoolean();
                        break;
                    case 2:
                        __z__ = reader.ReadBoolean();
                        break;
                    case 3:
                        __w__ = reader.ReadBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class Bool4x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool4x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool4x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.bool4x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool4);
            var __c1__ = default(global::Unity.Mathematics.bool4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool4x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Bool4x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool4x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool4x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.bool4x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool4);
            var __c1__ = default(global::Unity.Mathematics.bool4);
            var __c2__ = default(global::Unity.Mathematics.bool4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool4x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Bool4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.bool4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.bool4);
            var __c1__ = default(global::Unity.Mathematics.bool4);
            var __c2__ = default(global::Unity.Mathematics.bool4);
            var __c3__ = default(global::Unity.Mathematics.bool4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.bool4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.bool4x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Double2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.double2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(double);
            var __y__ = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadDouble();
                        break;
                    case 1:
                        __y__ = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class Double2x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double2x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double2x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.double2x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double2);
            var __c1__ = default(global::Unity.Mathematics.double2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double2x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Double2x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double2x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double2x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.double2x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double2);
            var __c1__ = default(global::Unity.Mathematics.double2);
            var __c2__ = default(global::Unity.Mathematics.double2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double2x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Double2x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double2x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double2x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.double2x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double2);
            var __c1__ = default(global::Unity.Mathematics.double2);
            var __c2__ = default(global::Unity.Mathematics.double2);
            var __c3__ = default(global::Unity.Mathematics.double2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double2x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Double3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.double3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(double);
            var __y__ = default(double);
            var __z__ = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadDouble();
                        break;
                    case 1:
                        __y__ = reader.ReadDouble();
                        break;
                    case 2:
                        __z__ = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class Double3x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double3x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.double3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double3);
            var __c1__ = default(global::Unity.Mathematics.double3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double3x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Double3x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double3x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double3x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.double3x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double3);
            var __c1__ = default(global::Unity.Mathematics.double3);
            var __c2__ = default(global::Unity.Mathematics.double3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double3x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Double3x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double3x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double3x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.double3x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double3);
            var __c1__ = default(global::Unity.Mathematics.double3);
            var __c2__ = default(global::Unity.Mathematics.double3);
            var __c3__ = default(global::Unity.Mathematics.double3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double3x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Double4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.double4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(double);
            var __y__ = default(double);
            var __z__ = default(double);
            var __w__ = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadDouble();
                        break;
                    case 1:
                        __y__ = reader.ReadDouble();
                        break;
                    case 2:
                        __z__ = reader.ReadDouble();
                        break;
                    case 3:
                        __w__ = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class Double4x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double4x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double4x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.double4x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double4);
            var __c1__ = default(global::Unity.Mathematics.double4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double4x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Double4x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double4x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double4x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.double4x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double4);
            var __c1__ = default(global::Unity.Mathematics.double4);
            var __c2__ = default(global::Unity.Mathematics.double4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double4x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Double4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.double4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.double4);
            var __c1__ = default(global::Unity.Mathematics.double4);
            var __c2__ = default(global::Unity.Mathematics.double4);
            var __c3__ = default(global::Unity.Mathematics.double4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.double4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.double4x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Float2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.float2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
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

            var ___result = new global::Unity.Mathematics.float2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class Float2x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float2x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float2x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.float2x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float2);
            var __c1__ = default(global::Unity.Mathematics.float2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float2x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Float2x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float2x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float2x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.float2x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float2);
            var __c1__ = default(global::Unity.Mathematics.float2);
            var __c2__ = default(global::Unity.Mathematics.float2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float2x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Float2x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float2x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float2x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.float2x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float2);
            var __c1__ = default(global::Unity.Mathematics.float2);
            var __c2__ = default(global::Unity.Mathematics.float2);
            var __c3__ = default(global::Unity.Mathematics.float2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float2x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Float3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.float3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
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

            var ___result = new global::Unity.Mathematics.float3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class Float3x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float3x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.float3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float3);
            var __c1__ = default(global::Unity.Mathematics.float3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float3x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Float3x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float3x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float3x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.float3x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float3);
            var __c1__ = default(global::Unity.Mathematics.float3);
            var __c2__ = default(global::Unity.Mathematics.float3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float3x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Float3x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float3x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float3x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.float3x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float3);
            var __c1__ = default(global::Unity.Mathematics.float3);
            var __c2__ = default(global::Unity.Mathematics.float3);
            var __c3__ = default(global::Unity.Mathematics.float3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float3x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Float4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.float4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(float);
            var __y__ = default(float);
            var __z__ = default(float);
            var __w__ = default(float);
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
                    case 3:
                        __w__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class Float4x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float4x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float4x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.float4x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float4);
            var __c1__ = default(global::Unity.Mathematics.float4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float4x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Float4x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float4x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float4x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.float4x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float4);
            var __c1__ = default(global::Unity.Mathematics.float4);
            var __c2__ = default(global::Unity.Mathematics.float4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float4x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Float4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.float4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.float4);
            var __c1__ = default(global::Unity.Mathematics.float4);
            var __c2__ = default(global::Unity.Mathematics.float4);
            var __c3__ = default(global::Unity.Mathematics.float4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.float4x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class HalfFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.half>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.half value, MessagePackSerializerOptions options)
        {
            writer.Write(value.value);
        }

        public global::Unity.Mathematics.half Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new global::Unity.Mathematics.half() { value = reader.ReadUInt16() };
        }
    }

    public sealed class Half2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.half2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.half2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.half2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(global::Unity.Mathematics.half);
            var __y__ = default(global::Unity.Mathematics.half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __y__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.half2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class Half3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.half3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.half3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.half3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(global::Unity.Mathematics.half);
            var __y__ = default(global::Unity.Mathematics.half);
            var __z__ = default(global::Unity.Mathematics.half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __y__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __z__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.half3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class Half4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.half4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.half4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.half4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(global::Unity.Mathematics.half);
            var __y__ = default(global::Unity.Mathematics.half);
            var __z__ = default(global::Unity.Mathematics.half);
            var __w__ = default(global::Unity.Mathematics.half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __y__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __z__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __w__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.half>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.half4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class Int2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.int2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(int);
            var __y__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class Int2x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int2x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int2x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.int2x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int2);
            var __c1__ = default(global::Unity.Mathematics.int2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int2x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Int2x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int2x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int2x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.int2x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int2);
            var __c1__ = default(global::Unity.Mathematics.int2);
            var __c2__ = default(global::Unity.Mathematics.int2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int2x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Int2x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int2x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int2x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.int2x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int2);
            var __c1__ = default(global::Unity.Mathematics.int2);
            var __c2__ = default(global::Unity.Mathematics.int2);
            var __c3__ = default(global::Unity.Mathematics.int2);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int2x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Int3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.int3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(int);
            var __y__ = default(int);
            var __z__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadInt32();
                        break;
                    case 2:
                        __z__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class Int3x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int3x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.int3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int3);
            var __c1__ = default(global::Unity.Mathematics.int3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int3x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Int3x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int3x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int3x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.int3x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int3);
            var __c1__ = default(global::Unity.Mathematics.int3);
            var __c2__ = default(global::Unity.Mathematics.int3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int3x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Int3x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int3x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int3x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.int3x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int3);
            var __c1__ = default(global::Unity.Mathematics.int3);
            var __c2__ = default(global::Unity.Mathematics.int3);
            var __c3__ = default(global::Unity.Mathematics.int3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int3x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class Int4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.int4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(int);
            var __y__ = default(int);
            var __z__ = default(int);
            var __w__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadInt32();
                        break;
                    case 2:
                        __z__ = reader.ReadInt32();
                        break;
                    case 3:
                        __w__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class Int4x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int4x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int4x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.int4x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int4);
            var __c1__ = default(global::Unity.Mathematics.int4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int4x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class Int4x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int4x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int4x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.int4x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int4);
            var __c1__ = default(global::Unity.Mathematics.int4);
            var __c2__ = default(global::Unity.Mathematics.int4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int4x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class Int4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.int4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.int4);
            var __c1__ = default(global::Unity.Mathematics.int4);
            var __c2__ = default(global::Unity.Mathematics.int4);
            var __c3__ = default(global::Unity.Mathematics.int4);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.int4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.int4x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class UInt2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.uint2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(uint);
            var __y__ = default(uint);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadUInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadUInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint2(__x__, __y__);
            ___result.x = __x__;
            ___result.y = __y__;
            return ___result;
        }
    }

    public sealed class UInt2x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint2x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint2x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.uint2x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint2);
            var __c1__ = default(global::Unity.Mathematics.uint2);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint2x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class UInt2x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint2x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint2x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.uint2x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint2);
            var __c1__ = default(global::Unity.Mathematics.uint2);
            var __c2__ = default(global::Unity.Mathematics.uint2);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint2x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class UInt2x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint2x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint2x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.uint2x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint2);
            var __c1__ = default(global::Unity.Mathematics.uint2);
            var __c2__ = default(global::Unity.Mathematics.uint2);
            var __c3__ = default(global::Unity.Mathematics.uint2);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint2>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint2x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class UInt3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.uint3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(uint);
            var __y__ = default(uint);
            var __z__ = default(uint);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadUInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadUInt32();
                        break;
                    case 2:
                        __z__ = reader.ReadUInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint3(__x__, __y__, __z__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            return ___result;
        }
    }

    public sealed class UInt3x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint3x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.uint3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint3);
            var __c1__ = default(global::Unity.Mathematics.uint3);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint3x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class UInt3x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint3x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint3x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.uint3x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint3);
            var __c1__ = default(global::Unity.Mathematics.uint3);
            var __c2__ = default(global::Unity.Mathematics.uint3);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint3x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class UInt3x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint3x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint3x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.uint3x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint3);
            var __c1__ = default(global::Unity.Mathematics.uint3);
            var __c2__ = default(global::Unity.Mathematics.uint3);
            var __c3__ = default(global::Unity.Mathematics.uint3);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint3x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class UInt4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::Unity.Mathematics.uint4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(uint);
            var __y__ = default(uint);
            var __z__ = default(uint);
            var __w__ = default(uint);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __x__ = reader.ReadUInt32();
                        break;
                    case 1:
                        __y__ = reader.ReadUInt32();
                        break;
                    case 2:
                        __z__ = reader.ReadUInt32();
                        break;
                    case 3:
                        __w__ = reader.ReadUInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint4(__x__, __y__, __z__, __w__);
            ___result.x = __x__;
            ___result.y = __y__;
            ___result.z = __z__;
            ___result.w = __w__;
            return ___result;
        }
    }

    public sealed class UInt4x2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint4x2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint4x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c1, options);
        }

        public global::Unity.Mathematics.uint4x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint4);
            var __c1__ = default(global::Unity.Mathematics.uint4);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint4x2(__c0__, __c1__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            return ___result;
        }
    }

    public sealed class UInt4x3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint4x3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint4x3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c2, options);
        }

        public global::Unity.Mathematics.uint4x3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint4);
            var __c1__ = default(global::Unity.Mathematics.uint4);
            var __c2__ = default(global::Unity.Mathematics.uint4);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint4x3(__c0__, __c1__, __c2__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            return ___result;
        }
    }

    public sealed class UInt4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.uint4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.uint4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c0, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c1, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c2, options);
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Serialize(ref writer, value.c3, options);
        }

        public global::Unity.Mathematics.uint4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __c0__ = default(global::Unity.Mathematics.uint4);
            var __c1__ = default(global::Unity.Mathematics.uint4);
            var __c2__ = default(global::Unity.Mathematics.uint4);
            var __c3__ = default(global::Unity.Mathematics.uint4);
            for (uint i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __c0__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __c1__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __c2__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    case 3:
                        __c3__ = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.uint4>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ___result = new global::Unity.Mathematics.uint4x4(__c0__, __c1__, __c2__, __c3__);
            ___result.c0 = __c0__;
            ___result.c1 = __c1__;
            ___result.c2 = __c2__;
            ___result.c3 = __c3__;
            return ___result;
        }
    }

    public sealed class MathQuaternionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.quaternion>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.quaternion value, MessagePackSerializerOptions options)
        {
            options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Serialize(ref writer, value.value, options);
        }

        public global::Unity.Mathematics.quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new global::Unity.Mathematics.quaternion(options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>().Deserialize(ref reader, options));
        }
    }
}
#endif

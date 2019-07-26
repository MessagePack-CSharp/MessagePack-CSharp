// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using MessagePack;
using MessagePack.Formatters;

#pragma warning disable SA1312 // variable naming
#pragma warning disable SA1402 // one type per file
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity
{
    public sealed class Vector2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::UnityEngine.Vector2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Vector2(x, y);
            return result;
        }
    }

    public sealed class Vector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::UnityEngine.Vector3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Vector3(x, y, z);
            return result;
        }
    }

    public sealed class Vector4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector4>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector4 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::UnityEngine.Vector4 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            var w = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    case 3:
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Vector4(x, y, z, w);
            return result;
        }
    }

    public sealed class QuaternionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Quaternion>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Quaternion value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public global::UnityEngine.Quaternion Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            var w = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    case 3:
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Quaternion(x, y, z, w);
            return result;
        }
    }

    public sealed class ColorFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Color>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Color value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.r);
            writer.Write(value.g);
            writer.Write(value.b);
            writer.Write(value.a);
        }

        public global::UnityEngine.Color Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var r = default(float);
            var g = default(float);
            var b = default(float);
            var a = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        r = reader.ReadSingle();
                        break;
                    case 1:
                        g = reader.ReadSingle();
                        break;
                    case 2:
                        b = reader.ReadSingle();
                        break;
                    case 3:
                        a = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Color(r, g, b, a);
            return result;
        }
    }

    public sealed class BoundsFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Bounds>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Bounds value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver resolver = options.Resolver;
            writer.WriteArrayHeader(2);
            resolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref writer, value.center, options);
            resolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref writer, value.size, options);
        }

        public global::UnityEngine.Bounds Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            IFormatterResolver resolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var center = default(global::UnityEngine.Vector3);
            var size = default(global::UnityEngine.Vector3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        center = resolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        size = resolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Bounds(center, size);
            return result;
        }
    }

    public sealed class RectFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Rect>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Rect value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }

        public global::UnityEngine.Rect Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var width = default(float);
            var height = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        width = reader.ReadSingle();
                        break;
                    case 3:
                        height = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::UnityEngine.Rect(x, y, width, height);
            return result;
        }
    }

    // additional
    public sealed class WrapModeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.WrapMode>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.WrapMode value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((int)value);
        }

        public global::UnityEngine.WrapMode Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::UnityEngine.WrapMode)reader.ReadInt32();
        }
    }

    public sealed class GradientModeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.GradientMode>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.GradientMode value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((int)value);
        }

        public global::UnityEngine.GradientMode Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::UnityEngine.GradientMode)reader.ReadInt32();
        }
    }

    public sealed class KeyframeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Keyframe>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Keyframe value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.time);
            writer.Write(value.value);
            writer.Write(value.inTangent);
            writer.Write(value.outTangent);
        }

        public global::UnityEngine.Keyframe Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __time__ = default(float);
            var __value__ = default(float);
            var __inTangent__ = default(float);
            var __outTangent__ = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __time__ = reader.ReadSingle();
                        break;
                    case 1:
                        __value__ = reader.ReadSingle();
                        break;
                    case 2:
                        __inTangent__ = reader.ReadSingle();
                        break;
                    case 3:
                        __outTangent__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.Keyframe(__time__, __value__, __inTangent__, __outTangent__);
            ____result.time = __time__;
            ____result.value = __value__;
            ____result.inTangent = __inTangent__;
            ____result.outTangent = __outTangent__;
            return ____result;
        }
    }

    public sealed class AnimationCurveFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.AnimationCurve>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.AnimationCurve value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver resolver = options.Resolver;
            writer.WriteArrayHeader(3);
            resolver.GetFormatterWithVerify<global::UnityEngine.Keyframe[]>().Serialize(ref writer, value.keys, options);
            resolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Serialize(ref writer, value.postWrapMode, options);
            resolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Serialize(ref writer, value.preWrapMode, options);
        }

        public global::UnityEngine.AnimationCurve Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                return null;
            }

            IFormatterResolver resolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __keys__ = default(global::UnityEngine.Keyframe[]);
            var __postWrapMode__ = default(global::UnityEngine.WrapMode);
            var __preWrapMode__ = default(global::UnityEngine.WrapMode);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __keys__ = resolver.GetFormatterWithVerify<global::UnityEngine.Keyframe[]>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __postWrapMode__ = resolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __preWrapMode__ = resolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.AnimationCurve();
            ____result.keys = __keys__;
            ____result.postWrapMode = __postWrapMode__;
            ____result.preWrapMode = __preWrapMode__;
            return ____result;
        }
    }

    public sealed class Matrix4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Matrix4x4>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Matrix4x4 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(16);
            writer.Write(value.m00);
            writer.Write(value.m10);
            writer.Write(value.m20);
            writer.Write(value.m30);
            writer.Write(value.m01);
            writer.Write(value.m11);
            writer.Write(value.m21);
            writer.Write(value.m31);
            writer.Write(value.m02);
            writer.Write(value.m12);
            writer.Write(value.m22);
            writer.Write(value.m32);
            writer.Write(value.m03);
            writer.Write(value.m13);
            writer.Write(value.m23);
            writer.Write(value.m33);
        }

        public global::UnityEngine.Matrix4x4 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __m00__ = default(float);
            var __m10__ = default(float);
            var __m20__ = default(float);
            var __m30__ = default(float);
            var __m01__ = default(float);
            var __m11__ = default(float);
            var __m21__ = default(float);
            var __m31__ = default(float);
            var __m02__ = default(float);
            var __m12__ = default(float);
            var __m22__ = default(float);
            var __m32__ = default(float);
            var __m03__ = default(float);
            var __m13__ = default(float);
            var __m23__ = default(float);
            var __m33__ = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __m00__ = reader.ReadSingle();
                        break;
                    case 1:
                        __m10__ = reader.ReadSingle();
                        break;
                    case 2:
                        __m20__ = reader.ReadSingle();
                        break;
                    case 3:
                        __m30__ = reader.ReadSingle();
                        break;
                    case 4:
                        __m01__ = reader.ReadSingle();
                        break;
                    case 5:
                        __m11__ = reader.ReadSingle();
                        break;
                    case 6:
                        __m21__ = reader.ReadSingle();
                        break;
                    case 7:
                        __m31__ = reader.ReadSingle();
                        break;
                    case 8:
                        __m02__ = reader.ReadSingle();
                        break;
                    case 9:
                        __m12__ = reader.ReadSingle();
                        break;
                    case 10:
                        __m22__ = reader.ReadSingle();
                        break;
                    case 11:
                        __m32__ = reader.ReadSingle();
                        break;
                    case 12:
                        __m03__ = reader.ReadSingle();
                        break;
                    case 13:
                        __m13__ = reader.ReadSingle();
                        break;
                    case 14:
                        __m23__ = reader.ReadSingle();
                        break;
                    case 15:
                        __m33__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = default(global::UnityEngine.Matrix4x4);
            ____result.m00 = __m00__;
            ____result.m10 = __m10__;
            ____result.m20 = __m20__;
            ____result.m30 = __m30__;
            ____result.m01 = __m01__;
            ____result.m11 = __m11__;
            ____result.m21 = __m21__;
            ____result.m31 = __m31__;
            ____result.m02 = __m02__;
            ____result.m12 = __m12__;
            ____result.m22 = __m22__;
            ____result.m32 = __m32__;
            ____result.m03 = __m03__;
            ____result.m13 = __m13__;
            ____result.m23 = __m23__;
            ____result.m33 = __m33__;
            return ____result;
        }
    }

    public sealed class GradientColorKeyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.GradientColorKey>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.GradientColorKey value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::UnityEngine.Color>().Serialize(ref writer, value.color, options);
            writer.Write(value.time);
        }

        public global::UnityEngine.GradientColorKey Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __color__ = default(global::UnityEngine.Color);
            var __time__ = default(float);
            IFormatterResolver resolver = options.Resolver;
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __color__ = resolver.GetFormatterWithVerify<global::UnityEngine.Color>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __time__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.GradientColorKey(__color__, __time__);
            ____result.color = __color__;
            ____result.time = __time__;
            return ____result;
        }
    }

    public sealed class GradientAlphaKeyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.GradientAlphaKey>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.GradientAlphaKey value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.alpha);
            writer.Write(value.time);
        }

        public global::UnityEngine.GradientAlphaKey Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __alpha__ = default(float);
            var __time__ = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __alpha__ = reader.ReadSingle();
                        break;
                    case 1:
                        __time__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.GradientAlphaKey(__alpha__, __time__);
            ____result.alpha = __alpha__;
            ____result.time = __time__;
            return ____result;
        }
    }

    public sealed class GradientFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Gradient>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Gradient value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver resolver = options.Resolver;
            writer.WriteArrayHeader(3);
            resolver.GetFormatterWithVerify<global::UnityEngine.GradientColorKey[]>().Serialize(ref writer, value.colorKeys, options);
            resolver.GetFormatterWithVerify<global::UnityEngine.GradientAlphaKey[]>().Serialize(ref writer, value.alphaKeys, options);
            resolver.GetFormatterWithVerify<global::UnityEngine.GradientMode>().Serialize(ref writer, value.mode, options);
        }

        public global::UnityEngine.Gradient Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                return null;
            }

            IFormatterResolver resolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __colorKeys__ = default(global::UnityEngine.GradientColorKey[]);
            var __alphaKeys__ = default(global::UnityEngine.GradientAlphaKey[]);
            var __mode__ = default(global::UnityEngine.GradientMode);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __colorKeys__ = resolver.GetFormatterWithVerify<global::UnityEngine.GradientColorKey[]>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __alphaKeys__ = resolver.GetFormatterWithVerify<global::UnityEngine.GradientAlphaKey[]>().Deserialize(ref reader, options);
                        break;
                    case 2:
                        __mode__ = resolver.GetFormatterWithVerify<global::UnityEngine.GradientMode>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.Gradient();
            ____result.colorKeys = __colorKeys__;
            ____result.alphaKeys = __alphaKeys__;
            ____result.mode = __mode__;
            return ____result;
        }
    }

    public sealed class Color32Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Color32>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Color32 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.r);
            writer.Write(value.g);
            writer.Write(value.b);
            writer.Write(value.a);
        }

        public global::UnityEngine.Color32 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __r__ = default(byte);
            var __g__ = default(byte);
            var __b__ = default(byte);
            var __a__ = default(byte);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __r__ = reader.ReadByte();
                        break;
                    case 1:
                        __g__ = reader.ReadByte();
                        break;
                    case 2:
                        __b__ = reader.ReadByte();
                        break;
                    case 3:
                        __a__ = reader.ReadByte();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.Color32(__r__, __g__, __b__, __a__);
            ____result.r = __r__;
            ____result.g = __g__;
            ____result.b = __b__;
            ____result.a = __a__;
            return ____result;
        }
    }

    public sealed class RectOffsetFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.RectOffset>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.RectOffset value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(4);
            writer.Write(value.left);
            writer.Write(value.right);
            writer.Write(value.top);
            writer.Write(value.bottom);
        }

        public global::UnityEngine.RectOffset Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                return null;
            }

            var length = reader.ReadArrayHeader();
            var __left__ = default(int);
            var __right__ = default(int);
            var __top__ = default(int);
            var __bottom__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __left__ = reader.ReadInt32();
                        break;
                    case 1:
                        __right__ = reader.ReadInt32();
                        break;
                    case 2:
                        __top__ = reader.ReadInt32();
                        break;
                    case 3:
                        __bottom__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.RectOffset();
            ____result.left = __left__;
            ____result.right = __right__;
            ____result.top = __top__;
            ____result.bottom = __bottom__;
            return ____result;
        }
    }

    public sealed class LayerMaskFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.LayerMask>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.LayerMask value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(1);
            writer.Write(value.value);
        }

        public global::UnityEngine.LayerMask Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var __value__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __value__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = default(global::UnityEngine.LayerMask);
            ____result.value = __value__;
            return ____result;
        }
    }
#if UNITY_2017_2_OR_NEWER
    public sealed class Vector2IntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2Int>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector2Int value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.WriteInt32(value.x);
            writer.WriteInt32(value.y);
        }
        public global::UnityEngine.Vector2Int Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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

            var ____result = new global::UnityEngine.Vector2Int(__x__, __y__);
            ____result.x = __x__;
            ____result.y = __y__;
            return ____result;
        }
    }

    public sealed class Vector3IntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3Int>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector3Int value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.WriteInt32(value.x);
            writer.WriteInt32(value.y);
            writer.WriteInt32(value.z);
        }
        public global::UnityEngine.Vector3Int Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
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

            var ____result = new global::UnityEngine.Vector3Int(__x__, __y__, __z__);
            ____result.x = __x__;
            ____result.y = __y__;
            ____result.z = __z__;
            return ____result;
        }
    }

    public sealed class RangeIntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.RangeInt>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.RangeInt value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.WriteInt32(value.start);
            writer.WriteInt32(value.length);
        }
        public global::UnityEngine.RangeInt Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __start__ = default(int);
            var __length__ = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __start__ = reader.ReadInt32();
                        break;
                    case 1:
                        __length__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.RangeInt(__start__, __length__);
            ____result.start = __start__;
            ____result.length = __length__;
            return ____result;
        }
    }

    public sealed class RectIntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.RectInt>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.RectInt value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.WriteInt32(value.x);
            writer.WriteInt32(value.y);
            writer.WriteInt32(value.width);
            writer.WriteInt32(value.height);
        }
        public global::UnityEngine.RectInt Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __x__ = default(int);
            var __y__ = default(int);
            var __width__ = default(int);
            var __height__ = default(int);
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
                        __width__ = reader.ReadInt32();
                        break;
                    case 3:
                        __height__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.RectInt(__x__, __y__, __width__, __height__);
            ____result.x = __x__;
            ____result.y = __y__;
            ____result.width = __width__;
            ____result.height = __height__;
            return ____result;
        }
    }

    public sealed class BoundsIntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.BoundsInt>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.BoundsInt value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Serialize(ref writer, value.position, options);
            options.Resolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Serialize(ref writer, value.size, options);
        }
        public global::UnityEngine.BoundsInt Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            var length = reader.ReadArrayHeader();
            var __position__ = default(global::UnityEngine.Vector3Int);
            var __size__ = default(global::UnityEngine.Vector3Int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __position__ = options.Resolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __size__ = options.Resolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::UnityEngine.BoundsInt(__position__, __size__);
            ____result.position = __position__;
            ____result.size = __size__;
            return ____result;
        }
    }
#endif
}

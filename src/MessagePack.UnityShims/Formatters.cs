using System;
using MessagePack;

namespace MessagePack.Unity
{
    public sealed class Vector2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            return offset - startOffset;
        }

        public global::UnityEngine.Vector2 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var x = default(float);
            var y = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        x = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        y = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Vector2(x, y);
            return result;
        }
    }


    public sealed class Vector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector3 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.z);
            return offset - startOffset;
        }

        public global::UnityEngine.Vector3 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var x = default(float);
            var y = default(float);
            var z = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        x = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        y = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        z = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Vector3(x, y, z);
            return result;
        }
    }


    public sealed class Vector4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector4>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector4 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.z);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.w);
            return offset - startOffset;
        }

        public global::UnityEngine.Vector4 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        x = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        y = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        z = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        w = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Vector4(x, y, z, w);
            return result;
        }
    }


    public sealed class QuaternionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Quaternion>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Quaternion value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.z);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.w);
            return offset - startOffset;
        }

        public global::UnityEngine.Quaternion Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        x = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        y = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        z = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        w = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Quaternion(x, y, z, w);
            return result;
        }
    }


    public sealed class ColorFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Color>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Color value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.r);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.g);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.b);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.a);
            return offset - startOffset;
        }

        public global::UnityEngine.Color Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        r = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        g = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        b = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        a = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Color(r, g, b, a);
            return result;
        }
    }


    public sealed class BoundsFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Bounds>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Bounds value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref bytes, offset, value.center, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref bytes, offset, value.size, formatterResolver);
            return offset - startOffset;
        }

        public global::UnityEngine.Bounds Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var center = default(global::UnityEngine.Vector3);
            var size = default(global::UnityEngine.Vector3);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        center = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        size = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Bounds(center, size);
            return result;
        }
    }


    public sealed class RectFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Rect>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Rect value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.width);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.height);
            return offset - startOffset;
        }

        public global::UnityEngine.Rect Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        x = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        y = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        width = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        height = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var result = new global::UnityEngine.Rect(x, y, width, height);
            return result;
        }
    }

    // additional

    public sealed class WrapModeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.WrapMode>
    {
        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.WrapMode value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt32(ref bytes, offset, (Int32)value);
        }

        public global::UnityEngine.WrapMode Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            return (global::UnityEngine.WrapMode)MessagePackBinary.ReadInt32(bytes, offset, out readSize);
        }
    }

    public sealed class GradientModeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.GradientMode>
    {
        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.GradientMode value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt32(ref bytes, offset, (Int32)value);
        }

        public global::UnityEngine.GradientMode Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            return (global::UnityEngine.GradientMode)MessagePackBinary.ReadInt32(bytes, offset, out readSize);
        }
    }

    public sealed class KeyframeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Keyframe>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Keyframe value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.time);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.value);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.inTangent);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.outTangent);
            return offset - startOffset;
        }

        public global::UnityEngine.Keyframe Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        __time__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __value__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __inTangent__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __outTangent__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

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

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.AnimationCurve value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Keyframe[]>().Serialize(ref bytes, offset, value.keys, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Serialize(ref bytes, offset, value.postWrapMode, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Serialize(ref bytes, offset, value.preWrapMode, formatterResolver);
            return offset - startOffset;
        }

        public global::UnityEngine.AnimationCurve Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __keys__ = default(global::UnityEngine.Keyframe[]);
            var __postWrapMode__ = default(global::UnityEngine.WrapMode);
            var __preWrapMode__ = default(global::UnityEngine.WrapMode);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __keys__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Keyframe[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __postWrapMode__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __preWrapMode__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.WrapMode>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.AnimationCurve();
            ____result.keys = __keys__;
            ____result.postWrapMode = __postWrapMode__;
            ____result.preWrapMode = __preWrapMode__;
            return ____result;
        }
    }

    public sealed class Matrix4x4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Matrix4x4>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Matrix4x4 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteArrayHeader(ref bytes, offset, 16);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m00);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m10);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m20);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m30);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m01);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m11);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m21);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m31);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m02);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m12);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m22);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m32);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m03);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m13);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m23);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.m33);
            return offset - startOffset;
        }

        public global::UnityEngine.Matrix4x4 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        __m00__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __m10__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __m20__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __m30__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 4:
                        __m01__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 5:
                        __m11__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 6:
                        __m21__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 7:
                        __m31__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 8:
                        __m02__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 9:
                        __m12__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 10:
                        __m22__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 11:
                        __m32__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 12:
                        __m03__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 13:
                        __m13__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 14:
                        __m23__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 15:
                        __m33__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Matrix4x4();
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

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.GradientColorKey value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Color>().Serialize(ref bytes, offset, value.color, formatterResolver);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.time);
            return offset - startOffset;
        }

        public global::UnityEngine.GradientColorKey Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __color__ = default(global::UnityEngine.Color);
            var __time__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __color__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Color>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __time__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.GradientColorKey(__color__, __time__);
            ____result.color = __color__;
            ____result.time = __time__;
            return ____result;
        }
    }


    public sealed class GradientAlphaKeyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.GradientAlphaKey>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.GradientAlphaKey value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.alpha);
            offset += MessagePackBinary.WriteSingle(ref bytes, offset, value.time);
            return offset - startOffset;
        }

        public global::UnityEngine.GradientAlphaKey Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __alpha__ = default(float);
            var __time__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __alpha__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __time__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.GradientAlphaKey(__alpha__, __time__);
            ____result.alpha = __alpha__;
            ____result.time = __time__;
            return ____result;
        }
    }


    public sealed class GradientFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Gradient>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Gradient value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientColorKey[]>().Serialize(ref bytes, offset, value.colorKeys, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientAlphaKey[]>().Serialize(ref bytes, offset, value.alphaKeys, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientMode>().Serialize(ref bytes, offset, value.mode, formatterResolver);
            return offset - startOffset;
        }

        public global::UnityEngine.Gradient Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __colorKeys__ = default(global::UnityEngine.GradientColorKey[]);
            var __alphaKeys__ = default(global::UnityEngine.GradientAlphaKey[]);
            var __mode__ = default(global::UnityEngine.GradientMode);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __colorKeys__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientColorKey[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __alphaKeys__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientAlphaKey[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 2:
                        __mode__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.GradientMode>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Gradient();
            ____result.colorKeys = __colorKeys__;
            ____result.alphaKeys = __alphaKeys__;
            ____result.mode = __mode__;
            return ____result;
        }
    }


    public sealed class Color32Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Color32>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Color32 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, value.r);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, value.g);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, value.b);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, value.a);
            return offset - startOffset;
        }

        public global::UnityEngine.Color32 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        __r__ = MessagePackBinary.ReadByte(bytes, offset, out readSize);
                        break;
                    case 1:
                        __g__ = MessagePackBinary.ReadByte(bytes, offset, out readSize);
                        break;
                    case 2:
                        __b__ = MessagePackBinary.ReadByte(bytes, offset, out readSize);
                        break;
                    case 3:
                        __a__ = MessagePackBinary.ReadByte(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

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

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.RectOffset value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.left);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.right);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.top);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.bottom);
            return offset - startOffset;
        }

        public global::UnityEngine.RectOffset Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        __left__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __right__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 2:
                        __top__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 3:
                        __bottom__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

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

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.LayerMask value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.value);
            return offset - startOffset;
        }

        public global::UnityEngine.LayerMask Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __value__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __value__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.LayerMask();
            ____result.value = __value__;
            return ____result;
        }
    }

#if UNITY_2017_2_OR_NEWER

    public sealed class Vector2IntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2Int>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector2Int value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.y);
            return offset - startOffset;
        }

        public global::UnityEngine.Vector2Int Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __x__ = default(int);
            var __y__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Vector2Int(__x__, __y__);
            ____result.x = __x__;
            ____result.y = __y__;
            return ____result;
        }
    }


    public sealed class Vector3IntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3Int>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector3Int value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.z);
            return offset - startOffset;
        }

        public global::UnityEngine.Vector3Int Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __x__ = default(int);
            var __y__ = default(int);
            var __z__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 2:
                        __z__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Vector3Int(__x__, __y__, __z__);
            ____result.x = __x__;
            ____result.y = __y__;
            ____result.z = __z__;
            return ____result;
        }
    }


    public sealed class RangeIntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.RangeInt>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.RangeInt value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.start);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.length);
            return offset - startOffset;
        }

        public global::UnityEngine.RangeInt Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __start__ = default(int);
            var __length__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __start__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __length__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.RangeInt(__start__, __length__);
            ____result.start = __start__;
            ____result.length = __length__;
            return ____result;
        }
    }


    public sealed class RectIntFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.RectInt>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.RectInt value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 4);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.x);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.y);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.width);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.height);
            return offset - startOffset;
        }

        public global::UnityEngine.RectInt Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

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
                        __x__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 2:
                        __width__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    case 3:
                        __height__ = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

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

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.BoundsInt value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Serialize(ref bytes, offset, value.position, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Serialize(ref bytes, offset, value.size, formatterResolver);
            return offset - startOffset;
        }

        public global::UnityEngine.BoundsInt Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            if (global::MessagePack.MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __position__ = default(global::UnityEngine.Vector3Int);
            var __size__ = default(global::UnityEngine.Vector3Int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __position__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __size__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3Int>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNextBlock(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.BoundsInt(__position__, __size__);
            ____result.position = __position__;
            ____result.size = __size__;
            return ____result;
        }
    }

#endif
}
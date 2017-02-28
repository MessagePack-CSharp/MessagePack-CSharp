using System;
using MessagePack;

namespace MessagePack.Unity
{
    public sealed class Vector2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector2 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
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

            var __x__ = default(float);
            var __y__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Vector2(__x__, __y__);
            return ____result;
        }
    }


    public sealed class Vector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector3 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);
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

            var __x__ = default(float);
            var __y__ = default(float);
            var __z__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __z__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Vector3(__x__, __y__, __z__);
            return ____result;
        }
    }


    public sealed class Vector4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector4>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Vector4 value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
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
                        __x__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __z__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __w__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Vector4(__x__, __y__, __z__, __w__);
            return ____result;
        }
    }


    public sealed class QuaternionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Quaternion>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Quaternion value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
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
                        __x__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __z__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __w__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Quaternion(__x__, __y__, __z__, __w__);
            return ____result;
        }
    }


    public sealed class ColorFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Color>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Color value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
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

            var __r__ = default(float);
            var __g__ = default(float);
            var __b__ = default(float);
            var __a__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __r__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __g__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __b__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __a__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Color(__r__, __g__, __b__);
            return ____result;
        }
    }


    public sealed class BoundsFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Bounds>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Bounds value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 1);
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

            var __center__ = default(global::UnityEngine.Vector3);
            var __size__ = default(global::UnityEngine.Vector3);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __center__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    case 1:
                        __size__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Bounds(__center__, __size__);
            return ____result;
        }
    }


    public sealed class RectFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Rect>
    {

        public int Serialize(ref byte[] bytes, int offset, global::UnityEngine.Rect value, global::MessagePack.IFormatterResolver formatterResolver)
        {

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
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

            var __x__ = default(float);
            var __y__ = default(float);
            var __width__ = default(float);
            var __height__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __x__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __y__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __width__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 3:
                        __height__ = MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

            var ____result = new global::UnityEngine.Rect(__x__, __y__, __width__, __height__);
            return ____result;
        }
    }
}
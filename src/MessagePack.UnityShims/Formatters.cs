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
}
#if ENABLE_UNSAFE_MSGPACK

using MessagePack.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MessagePack.Unity.Extension
{
    public static class ReservedUnityExtensionTypeCode
    {
        public const sbyte Vector2 = 30;
        public const sbyte Vector3 = 31;
        public const sbyte Vector4 = 32;
        public const sbyte Quaternion = 33;
        public const sbyte Color = 34;
        public const sbyte Bounds = 35;
        public const sbyte Rect = 36;
        public const sbyte Int = 37;
        public const sbyte Float = 38;
        public const sbyte Double = 39;
    }

    // use ext instead of ArrayFormatter to extremely boost up performance.
    // Layout: [extHeader, byteSize(integer), isLittleEndian(bool), bytes()]

    // Used Ext:30~36

    public abstract class UnsafeBlitFormatterBase<T> : IMessagePackFormatter<T[]>
        where T : struct
    {
        protected abstract sbyte TypeCode { get; }
        protected void CopyDeserializeUnsafe(ReadOnlySpan<byte> src, Span<T> dest) => src.CopyTo(MemoryMarshal.Cast<T, byte>(dest));

        public void Serialize(ref MessagePackWriter writer, T[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var byteLen = value.Length * Marshal.SizeOf<T>();

            writer.WriteExtensionFormatHeader(new ExtensionHeader(TypeCode, byteLen));
            writer.Write(byteLen); // write original header(not array header)
            writer.Write(BitConverter.IsLittleEndian);
            writer.WriteRaw(MemoryMarshal.Cast<T, byte>(value));
        }

        public T[] Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != TypeCode) throw new InvalidOperationException("Invalid typeCode.");

            var byteLength = reader.ReadInt32();
            var isLittleEndian = reader.ReadBoolean();

            // Allocate a T[] that we will return. We'll then cast the T[] as byte[] so we can copy the byte sequence directly into it.
            var result = new T[byteLength / Marshal.SizeOf<T>()];
            var resultAsBytes = MemoryMarshal.Cast<T, byte>(result);
            reader.ReadRaw(byteLength).CopyTo(resultAsBytes);

            // Reverse the byte order if necessary.
            if (isLittleEndian != BitConverter.IsLittleEndian)
            {
                for (int i = 0, j = resultAsBytes.Length - 1; i < j; i++, j--)
                {
                    byte tmp = resultAsBytes[i];
                    resultAsBytes[i] = resultAsBytes[j];
                    resultAsBytes[j] = tmp;
                }
            }

            return result;
        }
    }

    public class Vector2ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector2>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Vector2;
            }
        }
    }

    public class Vector3ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector3>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Vector3;
            }
        }
    }

    public class Vector4ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector4>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Vector4;
            }
        }
    }

    public class QuaternionArrayBlitFormatter : UnsafeBlitFormatterBase<Quaternion>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Quaternion;
            }
        }
    }

    public class ColorArrayBlitFormatter : UnsafeBlitFormatterBase<Color>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Color;
            }
        }
    }

    public class BoundsArrayBlitFormatter : UnsafeBlitFormatterBase<Bounds>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Bounds;
            }
        }
    }

    public class RectArrayBlitFormatter : UnsafeBlitFormatterBase<Rect>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedUnityExtensionTypeCode.Rect;
            }
        }
    }

    public class IntArrayBlitFormatter : UnsafeBlitFormatterBase<int>
    {
        protected override sbyte TypeCode { get { return ReservedUnityExtensionTypeCode.Int; } }
    }

    public class FloatArrayBlitFormatter : UnsafeBlitFormatterBase<float>
    {
        protected override sbyte TypeCode { get { return ReservedUnityExtensionTypeCode.Float; } }
    }

    public class DoubleArrayBlitFormatter : UnsafeBlitFormatterBase<double>
    {
        protected override sbyte TypeCode { get { return ReservedUnityExtensionTypeCode.Double; } }
    }
}

#endif
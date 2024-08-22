// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MessagePack.Formatters;
using UnityEngine;

#pragma warning disable SA1402 // multiple types in a file
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity.Extension
{
    public interface IReverseEndianessHelper
    {
        public void ReverseEndianess(Span<byte> span);
    }

    // use ext instead of ArrayFormatter to extremely boost up performance.
    // Layout: [extHeader, byteSize(integer), isLittleEndian(bool), bytes()]
    // Used Ext:30~36
    public abstract class UnsafeBlitFormatterBase<T, TReverseEndianessHelper> : IMessagePackFormatter<T[]?>
        where T : unmanaged
        where TReverseEndianessHelper : struct, IReverseEndianessHelper
    {
        protected abstract sbyte TypeCode { get; }

        protected void CopyDeserializeUnsafe(ReadOnlySpan<byte> src, Span<T> dest) => src.CopyTo(MemoryMarshal.Cast<T, byte>(dest));

        public unsafe void Serialize(ref MessagePackWriter writer, T[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var byteLen = value.Length * sizeof(T);
            var realLen = MessagePackWriter.GetEncodedLength(byteLen) + byteLen + 1;

            writer.WriteExtensionFormatHeader(new ExtensionHeader(this.TypeCode, realLen));
            writer.Write(byteLen); // write original header(not array header)
            writer.Write(BitConverter.IsLittleEndian);
            writer.WriteRaw(MemoryMarshal.Cast<T, byte>(value));
        }

        public unsafe T[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            ExtensionHeader header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != this.TypeCode)
            {
                throw new InvalidOperationException("Invalid typeCode.");
            }

            var byteLength = reader.ReadInt32();
            var isLittleEndian = reader.ReadBoolean();

            // Allocate a T[] that we will return. We'll then cast the T[] as byte[] so we can copy the byte sequence directly into it.
            var result = new T[byteLength / sizeof(T)];
            Span<byte> resultAsBytes = MemoryMarshal.Cast<T, byte>(result);
            reader.ReadRaw(byteLength).CopyTo(resultAsBytes);

            // Reverse the byte order if necessary.
            if (isLittleEndian != BitConverter.IsLittleEndian && result.Length > 0)
            {
                TReverseEndianessHelper reverseEndianessHelper = default;
                for (int offset = 0; offset < resultAsBytes.Length; offset += sizeof(T))
                {
                    reverseEndianessHelper.ReverseEndianess(resultAsBytes.Slice(offset, sizeof(T)));
                }
            }

            return result;
        }
    }

    public struct ReverseEndianessHelperSimpleSingle : IReverseEndianessHelper
    {
        public unsafe void ReverseEndianess(Span<byte> span)
        {
            for (var i = 0; (i << 1) < span.Length; i++)
            {
                (span[span.Length - 1 - i], span[i]) = (span[i], span[span.Length - 1 - i]);
            }
        }
    }

    public struct ReverseEndianessHelperSimpleRepeat<T> : IReverseEndianessHelper
        where T : unmanaged
    {
        public unsafe void ReverseEndianess(Span<byte> span)
        {
            for (var offset = 0; offset < span.Length; offset += sizeof(T))
            {
                for (var i = 0; (i << 1) < sizeof(T); i++)
                {
                    (span[offset + sizeof(T) - 1 - i], span[offset + i]) = (span[offset + i], span[offset + sizeof(T) - 1 - i]);
                }
            }
        }
    }

    public class Vector2ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector2, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityVector2;
            }
        }
    }

    public class Vector3ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector3, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityVector3;
            }
        }
    }

    public class Vector4ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector4, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityVector4;
            }
        }
    }

    public class QuaternionArrayBlitFormatter : UnsafeBlitFormatterBase<Quaternion, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityQuaternion;
            }
        }
    }

    public class ColorArrayBlitFormatter : UnsafeBlitFormatterBase<Color, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityColor;
            }
        }
    }

    public class BoundsArrayBlitFormatter : UnsafeBlitFormatterBase<Bounds, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityBounds;
            }
        }
    }

    public class RectArrayBlitFormatter : UnsafeBlitFormatterBase<Rect, ReverseEndianessHelperSimpleRepeat<float>>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityRect;
            }
        }
    }

    public class IntArrayBlitFormatter : UnsafeBlitFormatterBase<int, ReverseEndianessHelperSimpleSingle>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityInt; }
        }
    }

    public class FloatArrayBlitFormatter : UnsafeBlitFormatterBase<float, ReverseEndianessHelperSimpleSingle>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityFloat; }
        }
    }

    public class DoubleArrayBlitFormatter : UnsafeBlitFormatterBase<double, ReverseEndianessHelperSimpleSingle>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityDouble; }
        }
    }
}

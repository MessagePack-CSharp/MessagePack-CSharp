// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MessagePack.Formatters;
using UnityEngine;

#pragma warning disable SA1402 // multiple types in a file
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity.Extension
{
    // use ext instead of ArrayFormatter to extremely boost up performance.
    // Layout: [extHeader, byteSize(integer), isLittleEndian(bool), bytes()]
    // Used Ext:30~36
    public abstract class UnsafeBlitFormatterBase<T> : IMessagePackFormatter<T[]?>
        where T : unmanaged
    {
        protected abstract sbyte TypeCode { get; }

        protected void CopyDeserializeUnsafe(ReadOnlySpan<byte> src, Span<T> dest) => src.CopyTo(MemoryMarshal.Cast<T, byte>(dest));

        public void Serialize(ref MessagePackWriter writer, T[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var byteLen = value.Length * Marshal.SizeOf<T>();
            var realLen = MessagePackWriter.GetEncodedLength(byteLen) + byteLen + 1;

            writer.WriteExtensionFormatHeader(new ExtensionHeader(this.TypeCode, realLen));
            writer.Write(byteLen); // write original header(not array header)
            writer.Write(BitConverter.IsLittleEndian);
            writer.WriteRaw(MemoryMarshal.Cast<T, byte>(value));
        }

        public T[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
            var result = new T[byteLength / Marshal.SizeOf<T>()];
            Span<byte> resultAsBytes = MemoryMarshal.Cast<T, byte>(result);
            reader.ReadRaw(byteLength).CopyTo(resultAsBytes);

            // Reverse the byte order if necessary.
            if (isLittleEndian != BitConverter.IsLittleEndian)
            {
                unsafe
                {
                    for (int i = 0, byteOffset = 0; i < result.Length; i++, byteOffset += sizeof(T))
                    {
                        for (var byteIndex = 0; (byteIndex << 1) < sizeof(T); byteIndex++)
                        {
                            var targetByteIndex = sizeof(T) - byteIndex - 1 + byteOffset;
                            (resultAsBytes[byteOffset + byteIndex], resultAsBytes[targetByteIndex]) = (resultAsBytes[targetByteIndex], resultAsBytes[byteOffset + byteIndex]);
                        }
                    }
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
                return ReservedExtensionTypeCodes.UnityVector2;
            }
        }
    }

    public class Vector3ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector3>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityVector3;
            }
        }
    }

    public class Vector4ArrayBlitFormatter : UnsafeBlitFormatterBase<Vector4>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityVector4;
            }
        }
    }

    public class QuaternionArrayBlitFormatter : UnsafeBlitFormatterBase<Quaternion>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityQuaternion;
            }
        }
    }

    public class ColorArrayBlitFormatter : UnsafeBlitFormatterBase<Color>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityColor;
            }
        }
    }

    public class BoundsArrayBlitFormatter : UnsafeBlitFormatterBase<Bounds>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityBounds;
            }
        }
    }

    public class RectArrayBlitFormatter : UnsafeBlitFormatterBase<Rect>
    {
        protected override sbyte TypeCode
        {
            get
            {
                return ReservedExtensionTypeCodes.UnityRect;
            }
        }
    }

    public class IntArrayBlitFormatter : UnsafeBlitFormatterBase<int>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityInt; }
        }
    }

    public class FloatArrayBlitFormatter : UnsafeBlitFormatterBase<float>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityFloat; }
        }
    }

    public class DoubleArrayBlitFormatter : UnsafeBlitFormatterBase<double>
    {
        protected override sbyte TypeCode
        {
            get { return ReservedExtensionTypeCodes.UnityDouble; }
        }
    }
}

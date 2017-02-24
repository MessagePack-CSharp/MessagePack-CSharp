using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MessagePack.Unity.Extension
{
    internal static class MemoryUtil
    {
        // If you use memcpy or check alignment and word copy, can more improve performance
        public static unsafe void SimpleMemoryCopy(void* dest, void* src, int byteCount)
        {
            var pDest = (byte*)dest;
            var pSrc = (byte*)src;
            for (int i = 0; i < byteCount; i++)
            {
                *pDest = *pSrc;
                pDest++;
                pSrc++;
            }
        }
    }

    public static class ReservedUnityExtensionTypeCode
    {
        public const sbyte Vector2 = 30;
        public const sbyte Vector3 = 31;
        public const sbyte Vector4 = 32;
        public const sbyte Quaternion = 33;
        public const sbyte Color = 34;
        public const sbyte Bounds = 35;
        public const sbyte Rect = 36;
    }

    // use ext instead of ArrayFormatter for extremely boostup performance.
    // Layout: [extHeader, byteSize(integer), isLittlEendian(bool), bytes()]

    // Used Ext:30~36

    public abstract class UnsafeBlitFormatterBase<T> : IMessagePackFormatter<T[]>
        where T : struct
    {
        protected abstract sbyte TypeCode { get; }
        protected abstract int StructLength { get; }
        protected abstract void CopySerializeUnsafe(ref T[] src, ref byte[] dest, int destOffset, int byteLength);
        protected abstract void CopyDeserializeUnsafe(ref byte[] src, int srcOffset, ref T[] dest, int byteLength);

        public unsafe int Serialize(ref byte[] bytes, int offset, T[] value, IFormatterResolver formatterResolver)
        {
            if (value == null) return MessagePackBinary.WriteNil(ref bytes, offset);

            var startOffset = offset;

            var byteLen = value.Length * StructLength;

            offset += MessagePackBinary.WriteExtensionFormatHeader(ref bytes, offset, TypeCode, byteLen);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, byteLen); // write original header(not array header)
            offset += MessagePackBinary.WriteBoolean(ref bytes, offset, BitConverter.IsLittleEndian);

            MessagePackBinary.EnsureCapacity(ref bytes, offset, byteLen);
            CopySerializeUnsafe(ref value, ref bytes, offset, byteLen);

            offset += byteLen;
            return offset - startOffset;
        }

        public unsafe T[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var startOffset = offset;
            var header = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out readSize);
            offset += readSize;

            if (header.TypeCode != TypeCode) throw new InvalidOperationException("Invalid typeCode.");

            var byteLength = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
            offset += readSize;

            var isLittleEndian = MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
            offset += readSize;

            if (isLittleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, offset, byteLength);
            }

            var result = new T[byteLength / StructLength];
            CopyDeserializeUnsafe(ref bytes, offset, ref result, byteLength);
            offset += readSize;

            readSize = offset - startOffset;
            return result;
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

        protected override int StructLength
        {
            get
            {
                return 12;
            }
        }

        protected override unsafe void CopySerializeUnsafe(ref Vector3[] src, ref byte[] dest, int destOffset, int byteLength)
        {
            fixed (void* pSrc = src)
            fixed (void* pDest = &dest[destOffset])
            {
                MemoryUtil.SimpleMemoryCopy(pDest, pSrc, byteLength);
            }
        }

        protected override unsafe void CopyDeserializeUnsafe(ref byte[] src, int srcOffset, ref Vector3[] dest, int byteLength)
        {
            fixed (void* pSrc = &src[srcOffset])
            fixed (void* pDest = dest)
            {
                MemoryUtil.SimpleMemoryCopy(pDest, pSrc, byteLength);
            }
        }
    }

}
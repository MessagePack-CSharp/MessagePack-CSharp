using MessagePack.Internal;
using System;
using System.IO;

namespace MessagePack
{
    /// <summary>
    /// Encode/Decode Utility of MessagePack Spec.
    /// https://github.com/msgpack/msgpack/blob/master/spec.md
    /// </summary>
    public static partial class MessagePackBinary
    {
        const int ArrayMaxSize = 0x7FFFFFC7; // https://msdn.microsoft.com/en-us/library/system.array

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static void EnsureCapacity(ref byte[] bytes, int offset, int appendLength)
        {
            var newLength = offset + appendLength;

            // If null(most case fisrt time) fill byte.
            if (bytes == null)
            {
                bytes = new byte[newLength];
                return;
            }

            // like MemoryStream.EnsureCapacity
            var current = bytes.Length;
            if (newLength > current)
            {
                int num = newLength;
                if (num < 256)
                {
                    num = 256;
                    FastResize(ref bytes, num);
                    return;
                }

                if (current == ArrayMaxSize)
                {
                    throw new InvalidOperationException("byte[] size reached maximum size of array(0x7FFFFFC7), can not write to single byte[]. Details: https://msdn.microsoft.com/en-us/library/system.array");
                }

                var newSize = unchecked((current * 2));
                if (newSize < 0) // overflow
                {
                    num = ArrayMaxSize;
                }
                else
                {
                    if (num < newSize)
                    {
                        num = newSize;
                    }
                }

                FastResize(ref bytes, num);
            }
        }

        // Buffer.BlockCopy version of Array.Resize
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static void FastResize(ref byte[] array, int newSize)
        {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");

            byte[] array2 = array;
            if (array2 == null)
            {
                array = new byte[newSize];
                return;
            }

            if (array2.Length != newSize)
            {
                byte[] array3 = new byte[newSize];
                Buffer.BlockCopy(array2, 0, array3, 0, (array2.Length > newSize) ? newSize : array2.Length);
                array = array3;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static byte[] FastCloneWithResize(byte[] array, int newSize)
        {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");

            byte[] array2 = array;
            if (array2 == null)
            {
                array = new byte[newSize];
                return array;
            }

            byte[] array3 = new byte[newSize];
            Buffer.BlockCopy(array2, 0, array3, 0, (array2.Length > newSize) ? newSize : array2.Length);
            return array3;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteNil(ref byte[] bytes, int offset)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = MessagePackCode.Nil;
            return 1;
        }

        public static int WriteRaw(ref byte[] bytes, int offset, byte[] rawMessagePackBlock)
        {
            EnsureCapacity(ref bytes, offset, rawMessagePackBlock.Length);

#if !UNITY
            if (UnsafeMemory.Is32Bit)
            {
                switch (rawMessagePackBlock.Length)
                {
                    case 1:
                        UnsafeMemory32.WriteRaw1(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 2:
                        UnsafeMemory32.WriteRaw2(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 3:
                        UnsafeMemory32.WriteRaw3(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 4:
                        UnsafeMemory32.WriteRaw4(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 5:
                        UnsafeMemory32.WriteRaw5(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 6:
                        UnsafeMemory32.WriteRaw6(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 7:
                        UnsafeMemory32.WriteRaw7(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 8:
                        UnsafeMemory32.WriteRaw8(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 9:
                        UnsafeMemory32.WriteRaw9(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 10:
                        UnsafeMemory32.WriteRaw10(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 11:
                        UnsafeMemory32.WriteRaw11(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 12:
                        UnsafeMemory32.WriteRaw12(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 13:
                        UnsafeMemory32.WriteRaw13(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 14:
                        UnsafeMemory32.WriteRaw14(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 15:
                        UnsafeMemory32.WriteRaw15(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 16:
                        UnsafeMemory32.WriteRaw16(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 17:
                        UnsafeMemory32.WriteRaw17(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 18:
                        UnsafeMemory32.WriteRaw18(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 19:
                        UnsafeMemory32.WriteRaw19(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 20:
                        UnsafeMemory32.WriteRaw20(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 21:
                        UnsafeMemory32.WriteRaw21(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 22:
                        UnsafeMemory32.WriteRaw22(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 23:
                        UnsafeMemory32.WriteRaw23(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 24:
                        UnsafeMemory32.WriteRaw24(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 25:
                        UnsafeMemory32.WriteRaw25(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 26:
                        UnsafeMemory32.WriteRaw26(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 27:
                        UnsafeMemory32.WriteRaw27(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 28:
                        UnsafeMemory32.WriteRaw28(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 29:
                        UnsafeMemory32.WriteRaw29(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 30:
                        UnsafeMemory32.WriteRaw30(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 31:
                        UnsafeMemory32.WriteRaw31(ref bytes, offset, rawMessagePackBlock);
                        break;
                    default:
                        Buffer.BlockCopy(rawMessagePackBlock, 0, bytes, offset, rawMessagePackBlock.Length);
                        break;
                }
            }
            else
            {
                switch (rawMessagePackBlock.Length)
                {
                    case 1:
                        UnsafeMemory64.WriteRaw1(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 2:
                        UnsafeMemory64.WriteRaw2(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 3:
                        UnsafeMemory64.WriteRaw3(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 4:
                        UnsafeMemory64.WriteRaw4(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 5:
                        UnsafeMemory64.WriteRaw5(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 6:
                        UnsafeMemory64.WriteRaw6(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 7:
                        UnsafeMemory64.WriteRaw7(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 8:
                        UnsafeMemory64.WriteRaw8(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 9:
                        UnsafeMemory64.WriteRaw9(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 10:
                        UnsafeMemory64.WriteRaw10(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 11:
                        UnsafeMemory64.WriteRaw11(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 12:
                        UnsafeMemory64.WriteRaw12(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 13:
                        UnsafeMemory64.WriteRaw13(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 14:
                        UnsafeMemory64.WriteRaw14(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 15:
                        UnsafeMemory64.WriteRaw15(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 16:
                        UnsafeMemory64.WriteRaw16(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 17:
                        UnsafeMemory64.WriteRaw17(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 18:
                        UnsafeMemory64.WriteRaw18(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 19:
                        UnsafeMemory64.WriteRaw19(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 20:
                        UnsafeMemory64.WriteRaw20(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 21:
                        UnsafeMemory64.WriteRaw21(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 22:
                        UnsafeMemory64.WriteRaw22(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 23:
                        UnsafeMemory64.WriteRaw23(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 24:
                        UnsafeMemory64.WriteRaw24(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 25:
                        UnsafeMemory64.WriteRaw25(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 26:
                        UnsafeMemory64.WriteRaw26(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 27:
                        UnsafeMemory64.WriteRaw27(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 28:
                        UnsafeMemory64.WriteRaw28(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 29:
                        UnsafeMemory64.WriteRaw29(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 30:
                        UnsafeMemory64.WriteRaw30(ref bytes, offset, rawMessagePackBlock);
                        break;
                    case 31:
                        UnsafeMemory64.WriteRaw31(ref bytes, offset, rawMessagePackBlock);
                        break;
                    default:
                        Buffer.BlockCopy(rawMessagePackBlock, 0, bytes, offset, rawMessagePackBlock.Length);
                        break;
                }
            }
#else
            Buffer.BlockCopy(rawMessagePackBlock, 0, bytes, offset, rawMessagePackBlock.Length);
#endif
            return rawMessagePackBlock.Length;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixMapCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedMapHeaderUnsafe(ref byte[] bytes, int offset, int count)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)(MessagePackCode.MinFixMap | count);
            return 1;
        }

        /// <summary>
        /// Write map count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeader(ref byte[] bytes, int offset, int count)
        {
            checked
            {
                return WriteMapHeader(ref bytes, offset, (uint)count);
            }
        }

        /// <summary>
        /// Write map count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeader(ref byte[] bytes, int offset, uint count)
        {
            if (count <= MessagePackRange.MaxFixMapCount)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = (byte)(MessagePackCode.MinFixMap | count);
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 3);
                unchecked
                {
                    bytes[offset] = MessagePackCode.Map16;
                    bytes[offset + 1] = (byte)(count >> 8);
                    bytes[offset + 2] = (byte)(count);
                }
                return 3;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 5);
                unchecked
                {
                    bytes[offset] = MessagePackCode.Map32;
                    bytes[offset + 1] = (byte)(count >> 24);
                    bytes[offset + 2] = (byte)(count >> 16);
                    bytes[offset + 3] = (byte)(count >> 8);
                    bytes[offset + 4] = (byte)(count);
                }
                return 5;
            }
        }

        /// <summary>
        /// Write map format header, always use map32 format(length is fixed, 5).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeaderForceMap32Block(ref byte[] bytes, int offset, uint count)
        {
            EnsureCapacity(ref bytes, offset, 5);
            unchecked
            {
                bytes[offset] = MessagePackCode.Map32;
                bytes[offset + 1] = (byte)(count >> 24);
                bytes[offset + 2] = (byte)(count >> 16);
                bytes[offset + 3] = (byte)(count >> 8);
                bytes[offset + 4] = (byte)(count);
            }
            return 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int GetArrayHeaderLength(int count)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                return 3;
            }
            else
            {
                return 5;
            }
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixArrayCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedArrayHeaderUnsafe(ref byte[] bytes, int offset, int count)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)(MessagePackCode.MinFixArray | count);
            return 1;
        }

        /// <summary>
        /// Write array count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeader(ref byte[] bytes, int offset, int count)
        {
            checked
            {
                return WriteArrayHeader(ref bytes, offset, (uint)count);
            }
        }

        /// <summary>
        /// Write array count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeader(ref byte[] bytes, int offset, uint count)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = (byte)(MessagePackCode.MinFixArray | count);
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 3);
                unchecked
                {
                    bytes[offset] = MessagePackCode.Array16;
                    bytes[offset + 1] = (byte)(count >> 8);
                    bytes[offset + 2] = (byte)(count);
                }
                return 3;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 5);
                unchecked
                {
                    bytes[offset] = MessagePackCode.Array32;
                    bytes[offset + 1] = (byte)(count >> 24);
                    bytes[offset + 2] = (byte)(count >> 16);
                    bytes[offset + 3] = (byte)(count >> 8);
                    bytes[offset + 4] = (byte)(count);
                }
                return 5;
            }
        }

        /// <summary>
        /// Write array format header, always use array32 format(length is fixed, 5).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeaderForceArray32Block(ref byte[] bytes, int offset, uint count)
        {
            EnsureCapacity(ref bytes, offset, 5);
            unchecked
            {
                bytes[offset] = MessagePackCode.Array32;
                bytes[offset + 1] = (byte)(count >> 24);
                bytes[offset + 2] = (byte)(count >> 16);
                bytes[offset + 3] = (byte)(count >> 8);
                bytes[offset + 4] = (byte)(count);
            }
            return 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBoolean(ref byte[] bytes, int offset, bool value)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = (value ? MessagePackCode.True : MessagePackCode.False);
            return 1;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteByte(ref byte[] bytes, int offset, byte value)
        {
            if (value <= MessagePackCode.MaxFixInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = value;
                return 1;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.UInt8;
                bytes[offset + 1] = value;
                return 2;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteByteForceByteBlock(ref byte[] bytes, int offset, byte value)
        {
            EnsureCapacity(ref bytes, offset, 2);
            bytes[offset] = MessagePackCode.UInt8;
            bytes[offset + 1] = value;
            return 2;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBytes(ref byte[] bytes, int offset, byte[] value)
        {
            if (value == null)
            {
                return WriteNil(ref bytes, offset);
            }
            else
            {
                return WriteBytes(ref bytes, offset, value, 0, value.Length);
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBytes(ref byte[] dest, int dstOffset, byte[] src, int srcOffset, int count)
        {
            if (src == null)
            {
                return WriteNil(ref dest, dstOffset);
            }

            if (count <= byte.MaxValue)
            {
                var size = count + 2;
                EnsureCapacity(ref dest, dstOffset, size);

                dest[dstOffset] = MessagePackCode.Bin8;
                dest[dstOffset + 1] = (byte)count;

                Buffer.BlockCopy(src, srcOffset, dest, dstOffset + 2, count);
                return size;
            }
            else if (count <= UInt16.MaxValue)
            {
                var size = count + 3;
                EnsureCapacity(ref dest, dstOffset, size);

                unchecked
                {
                    dest[dstOffset] = MessagePackCode.Bin16;
                    dest[dstOffset + 1] = (byte)(count >> 8);
                    dest[dstOffset + 2] = (byte)(count);
                }

                Buffer.BlockCopy(src, srcOffset, dest, dstOffset + 3, count);
                return size;
            }
            else
            {
                var size = count + 5;
                EnsureCapacity(ref dest, dstOffset, size);

                unchecked
                {
                    dest[dstOffset] = MessagePackCode.Bin32;
                    dest[dstOffset + 1] = (byte)(count >> 24);
                    dest[dstOffset + 2] = (byte)(count >> 16);
                    dest[dstOffset + 3] = (byte)(count >> 8);
                    dest[dstOffset + 4] = (byte)(count);
                }

                Buffer.BlockCopy(src, srcOffset, dest, dstOffset + 5, count);
                return size;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSByte(ref byte[] bytes, int offset, sbyte value)
        {
            if (value < MessagePackRange.MinFixNegativeInt)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.Int8;
                bytes[offset + 1] = unchecked((byte)(value));
                return 2;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSByteForceSByteBlock(ref byte[] bytes, int offset, sbyte value)
        {
            EnsureCapacity(ref bytes, offset, 2);
            bytes[offset] = MessagePackCode.Int8;
            bytes[offset + 1] = unchecked((byte)(value));
            return 2;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSingle(ref byte[] bytes, int offset, float value)
        {
            EnsureCapacity(ref bytes, offset, 5);

            bytes[offset] = MessagePackCode.Float32;

            var num = new Float32Bits(value);
            if (BitConverter.IsLittleEndian)
            {
                bytes[offset + 1] = num.Byte3;
                bytes[offset + 2] = num.Byte2;
                bytes[offset + 3] = num.Byte1;
                bytes[offset + 4] = num.Byte0;
            }
            else
            {
                bytes[offset + 1] = num.Byte0;
                bytes[offset + 2] = num.Byte1;
                bytes[offset + 3] = num.Byte2;
                bytes[offset + 4] = num.Byte3;
            }

            return 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteDouble(ref byte[] bytes, int offset, double value)
        {
            EnsureCapacity(ref bytes, offset, 9);

            bytes[offset] = MessagePackCode.Float64;

            var num = new Float64Bits(value);
            if (BitConverter.IsLittleEndian)
            {
                bytes[offset + 1] = num.Byte7;
                bytes[offset + 2] = num.Byte6;
                bytes[offset + 3] = num.Byte5;
                bytes[offset + 4] = num.Byte4;
                bytes[offset + 5] = num.Byte3;
                bytes[offset + 6] = num.Byte2;
                bytes[offset + 7] = num.Byte1;
                bytes[offset + 8] = num.Byte0;
            }
            else
            {
                bytes[offset + 1] = num.Byte0;
                bytes[offset + 2] = num.Byte1;
                bytes[offset + 3] = num.Byte2;
                bytes[offset + 4] = num.Byte3;
                bytes[offset + 5] = num.Byte4;
                bytes[offset + 6] = num.Byte5;
                bytes[offset + 7] = num.Byte6;
                bytes[offset + 8] = num.Byte7;
            }

            return 9;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt16(ref byte[] bytes, int offset, short value)
        {
            if (value >= 0)
            {
                // positive int(use uint)
                if (value <= MessagePackRange.MaxFixPositiveInt)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (value <= byte.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.UInt8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.UInt16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (sbyte.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.Int8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.Int16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt16ForceInt16Block(ref byte[] bytes, int offset, short value)
        {
            EnsureCapacity(ref bytes, offset, 3);
            bytes[offset] = MessagePackCode.Int16;
            bytes[offset + 1] = unchecked((byte)(value >> 8));
            bytes[offset + 2] = unchecked((byte)value);
            return 3;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackCode.MaxFixInt(127), can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WritePositiveFixedIntUnsafe(ref byte[] bytes, int offset, int value)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)value;
            return 1;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt32(ref byte[] bytes, int offset, int value)
        {
            if (value >= 0)
            {
                // positive int(use uint)
                if (value <= MessagePackRange.MaxFixPositiveInt)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (value <= byte.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.UInt8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else if (value <= ushort.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.UInt16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 5);
                    bytes[offset] = MessagePackCode.UInt32;
                    bytes[offset + 1] = unchecked((byte)(value >> 24));
                    bytes[offset + 2] = unchecked((byte)(value >> 16));
                    bytes[offset + 3] = unchecked((byte)(value >> 8));
                    bytes[offset + 4] = unchecked((byte)value);
                    return 5;
                }
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (sbyte.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.Int8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else if (short.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.Int16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 5);
                    bytes[offset] = MessagePackCode.Int32;
                    bytes[offset + 1] = unchecked((byte)(value >> 24));
                    bytes[offset + 2] = unchecked((byte)(value >> 16));
                    bytes[offset + 3] = unchecked((byte)(value >> 8));
                    bytes[offset + 4] = unchecked((byte)value);
                    return 5;
                }
            }
        }

        /// <summary>
        /// Acquire static message block(always 5 bytes).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt32ForceInt32Block(ref byte[] bytes, int offset, int value)
        {
            EnsureCapacity(ref bytes, offset, 5);
            bytes[offset] = MessagePackCode.Int32;
            bytes[offset + 1] = unchecked((byte)(value >> 24));
            bytes[offset + 2] = unchecked((byte)(value >> 16));
            bytes[offset + 3] = unchecked((byte)(value >> 8));
            bytes[offset + 4] = unchecked((byte)value);
            return 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt64(ref byte[] bytes, int offset, long value)
        {
            if (value >= 0)
            {
                // positive int(use uint)
                if (value <= MessagePackRange.MaxFixPositiveInt)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (value <= byte.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.UInt8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else if (value <= ushort.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.UInt16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
                else if (value <= uint.MaxValue)
                {
                    EnsureCapacity(ref bytes, offset, 5);
                    bytes[offset] = MessagePackCode.UInt32;
                    bytes[offset + 1] = unchecked((byte)(value >> 24));
                    bytes[offset + 2] = unchecked((byte)(value >> 16));
                    bytes[offset + 3] = unchecked((byte)(value >> 8));
                    bytes[offset + 4] = unchecked((byte)value);
                    return 5;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 9);
                    bytes[offset] = MessagePackCode.UInt64;
                    bytes[offset + 1] = unchecked((byte)(value >> 56));
                    bytes[offset + 2] = unchecked((byte)(value >> 48));
                    bytes[offset + 3] = unchecked((byte)(value >> 40));
                    bytes[offset + 4] = unchecked((byte)(value >> 32));
                    bytes[offset + 5] = unchecked((byte)(value >> 24));
                    bytes[offset + 6] = unchecked((byte)(value >> 16));
                    bytes[offset + 7] = unchecked((byte)(value >> 8));
                    bytes[offset + 8] = unchecked((byte)value);
                    return 9;
                }
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    EnsureCapacity(ref bytes, offset, 1);
                    bytes[offset] = unchecked((byte)value);
                    return 1;
                }
                else if (sbyte.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 2);
                    bytes[offset] = MessagePackCode.Int8;
                    bytes[offset + 1] = unchecked((byte)value);
                    return 2;
                }
                else if (short.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.Int16;
                    bytes[offset + 1] = unchecked((byte)(value >> 8));
                    bytes[offset + 2] = unchecked((byte)value);
                    return 3;
                }
                else if (int.MinValue <= value)
                {
                    EnsureCapacity(ref bytes, offset, 5);
                    bytes[offset] = MessagePackCode.Int32;
                    bytes[offset + 1] = unchecked((byte)(value >> 24));
                    bytes[offset + 2] = unchecked((byte)(value >> 16));
                    bytes[offset + 3] = unchecked((byte)(value >> 8));
                    bytes[offset + 4] = unchecked((byte)value);
                    return 5;
                }
                else
                {
                    EnsureCapacity(ref bytes, offset, 9);
                    bytes[offset] = MessagePackCode.Int64;
                    bytes[offset + 1] = unchecked((byte)(value >> 56));
                    bytes[offset + 2] = unchecked((byte)(value >> 48));
                    bytes[offset + 3] = unchecked((byte)(value >> 40));
                    bytes[offset + 4] = unchecked((byte)(value >> 32));
                    bytes[offset + 5] = unchecked((byte)(value >> 24));
                    bytes[offset + 6] = unchecked((byte)(value >> 16));
                    bytes[offset + 7] = unchecked((byte)(value >> 8));
                    bytes[offset + 8] = unchecked((byte)value);
                    return 9;
                }
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt64ForceInt64Block(ref byte[] bytes, int offset, long value)
        {
            EnsureCapacity(ref bytes, offset, 9);
            bytes[offset] = MessagePackCode.Int64;
            bytes[offset + 1] = unchecked((byte)(value >> 56));
            bytes[offset + 2] = unchecked((byte)(value >> 48));
            bytes[offset + 3] = unchecked((byte)(value >> 40));
            bytes[offset + 4] = unchecked((byte)(value >> 32));
            bytes[offset + 5] = unchecked((byte)(value >> 24));
            bytes[offset + 6] = unchecked((byte)(value >> 16));
            bytes[offset + 7] = unchecked((byte)(value >> 8));
            bytes[offset + 8] = unchecked((byte)value);
            return 9;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt16(ref byte[] bytes, int offset, ushort value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.UInt8;
                bytes[offset + 1] = unchecked((byte)value);
                return 2;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 3);
                bytes[offset] = MessagePackCode.UInt16;
                bytes[offset + 1] = unchecked((byte)(value >> 8));
                bytes[offset + 2] = unchecked((byte)value);
                return 3;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt16ForceUInt16Block(ref byte[] bytes, int offset, ushort value)
        {
            EnsureCapacity(ref bytes, offset, 3);
            bytes[offset] = MessagePackCode.UInt16;
            bytes[offset + 1] = unchecked((byte)(value >> 8));
            bytes[offset + 2] = unchecked((byte)value);
            return 3;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt32(ref byte[] bytes, int offset, uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.UInt8;
                bytes[offset + 1] = unchecked((byte)value);
                return 2;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 3);
                bytes[offset] = MessagePackCode.UInt16;
                bytes[offset + 1] = unchecked((byte)(value >> 8));
                bytes[offset + 2] = unchecked((byte)value);
                return 3;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 5);
                bytes[offset] = MessagePackCode.UInt32;
                bytes[offset + 1] = unchecked((byte)(value >> 24));
                bytes[offset + 2] = unchecked((byte)(value >> 16));
                bytes[offset + 3] = unchecked((byte)(value >> 8));
                bytes[offset + 4] = unchecked((byte)value);
                return 5;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt32ForceUInt32Block(ref byte[] bytes, int offset, uint value)
        {
            EnsureCapacity(ref bytes, offset, 5);
            bytes[offset] = MessagePackCode.UInt32;
            bytes[offset + 1] = unchecked((byte)(value >> 24));
            bytes[offset + 2] = unchecked((byte)(value >> 16));
            bytes[offset + 3] = unchecked((byte)(value >> 8));
            bytes[offset + 4] = unchecked((byte)value);
            return 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt64(ref byte[] bytes, int offset, ulong value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.UInt8;
                bytes[offset + 1] = unchecked((byte)value);
                return 2;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 3);
                bytes[offset] = MessagePackCode.UInt16;
                bytes[offset + 1] = unchecked((byte)(value >> 8));
                bytes[offset + 2] = unchecked((byte)value);
                return 3;
            }
            else if (value <= uint.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 5);
                bytes[offset] = MessagePackCode.UInt32;
                bytes[offset + 1] = unchecked((byte)(value >> 24));
                bytes[offset + 2] = unchecked((byte)(value >> 16));
                bytes[offset + 3] = unchecked((byte)(value >> 8));
                bytes[offset + 4] = unchecked((byte)value);
                return 5;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, 9);
                bytes[offset] = MessagePackCode.UInt64;
                bytes[offset + 1] = unchecked((byte)(value >> 56));
                bytes[offset + 2] = unchecked((byte)(value >> 48));
                bytes[offset + 3] = unchecked((byte)(value >> 40));
                bytes[offset + 4] = unchecked((byte)(value >> 32));
                bytes[offset + 5] = unchecked((byte)(value >> 24));
                bytes[offset + 6] = unchecked((byte)(value >> 16));
                bytes[offset + 7] = unchecked((byte)(value >> 8));
                bytes[offset + 8] = unchecked((byte)value);
                return 9;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt64ForceUInt64Block(ref byte[] bytes, int offset, ulong value)
        {
            EnsureCapacity(ref bytes, offset, 9);
            bytes[offset] = MessagePackCode.UInt64;
            bytes[offset + 1] = unchecked((byte)(value >> 56));
            bytes[offset + 2] = unchecked((byte)(value >> 48));
            bytes[offset + 3] = unchecked((byte)(value >> 40));
            bytes[offset + 4] = unchecked((byte)(value >> 32));
            bytes[offset + 5] = unchecked((byte)(value >> 24));
            bytes[offset + 6] = unchecked((byte)(value >> 16));
            bytes[offset + 7] = unchecked((byte)(value >> 8));
            bytes[offset + 8] = unchecked((byte)value);
            return 9;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteChar(ref byte[] bytes, int offset, char value)
        {
            return WriteUInt16(ref bytes, offset, (ushort)value);
        }

        /// <summary>
        /// Unsafe. If value is guranteed length is 0 ~ 31, can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedStringUnsafe(ref byte[] bytes, int offset, string value, int byteCount)
        {
            EnsureCapacity(ref bytes, offset, byteCount + 1);
            bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
            StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 1);

            return byteCount + 1;
        }

        /// <summary>
        /// Unsafe. If pre-calculated byteCount of target string, can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteStringUnsafe(ref byte[] bytes, int offset, string value, int byteCount)
        {
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 1);
                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 1);
                return byteCount + 1;
            }
            else if (byteCount <= byte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 2);
                bytes[offset] = MessagePackCode.Str8;
                bytes[offset + 1] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 2);
                return byteCount + 2;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 3);
                bytes[offset] = MessagePackCode.Str16;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 2] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 3);
                return byteCount + 3;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, byteCount + 5);
                bytes[offset] = MessagePackCode.Str32;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
                bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
                bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 4] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 5);
                return byteCount + 5;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteStringBytes(ref byte[] bytes, int offset, byte[] utf8stringBytes)
        {
            var byteCount = utf8stringBytes.Length;
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 1);
                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                Buffer.BlockCopy(utf8stringBytes, 0, bytes, offset + 1, byteCount);
                return byteCount + 1;
            }
            else if (byteCount <= byte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 2);
                bytes[offset] = MessagePackCode.Str8;
                bytes[offset + 1] = unchecked((byte)byteCount);
                Buffer.BlockCopy(utf8stringBytes, 0, bytes, offset + 2, byteCount);
                return byteCount + 2;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 3);
                bytes[offset] = MessagePackCode.Str16;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 2] = unchecked((byte)byteCount);
                Buffer.BlockCopy(utf8stringBytes, 0, bytes, offset + 3, byteCount);
                return byteCount + 3;
            }
            else
            {
                EnsureCapacity(ref bytes, offset, byteCount + 5);
                bytes[offset] = MessagePackCode.Str32;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
                bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
                bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 4] = unchecked((byte)byteCount);
                Buffer.BlockCopy(utf8stringBytes, 0, bytes, offset + 5, byteCount);
                return byteCount + 5;
            }
        }

        public static byte[] GetEncodedStringBytes(string value)
        {
            var byteCount = StringEncoding.UTF8.GetByteCount(value);
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                var bytes = new byte[byteCount + 1];
                bytes[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 1);
                return bytes;
            }
            else if (byteCount <= byte.MaxValue)
            {
                var bytes = new byte[byteCount + 2];
                bytes[0] = MessagePackCode.Str8;
                bytes[1] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 2);
                return bytes;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                var bytes = new byte[byteCount + 3];
                bytes[0] = MessagePackCode.Str16;
                bytes[1] = unchecked((byte)(byteCount >> 8));
                bytes[2] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 3);
                return bytes;
            }
            else
            {
                var bytes = new byte[byteCount + 5];
                bytes[0] = MessagePackCode.Str32;
                bytes[1] = unchecked((byte)(byteCount >> 24));
                bytes[2] = unchecked((byte)(byteCount >> 16));
                bytes[3] = unchecked((byte)(byteCount >> 8));
                bytes[4] = unchecked((byte)byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, 5);
                return bytes;
            }
        }

        public static int WriteString(ref byte[] bytes, int offset, string value)
        {
            if (value == null) return WriteNil(ref bytes, offset);

            // MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
            // solves heuristic length check

            // ensure buffer by MaxByteCount(faster than GetByteCount)
            MessagePackBinary.EnsureCapacity(ref bytes, offset, StringEncoding.UTF8.GetMaxByteCount(value.Length) + 5);

            int useOffset;
            if (value.Length <= MessagePackRange.MaxFixStringLength)
            {
                useOffset = 1;
            }
            else if (value.Length <= byte.MaxValue)
            {
                useOffset = 2;
            }
            else if (value.Length <= ushort.MaxValue)
            {
                useOffset = 3;
            }
            else
            {
                useOffset = 5;
            }

            // skip length area
            var writeBeginOffset = offset + useOffset;
            var byteCount = StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, writeBeginOffset);

            // move body and write prefix
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (useOffset != 1)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 1, byteCount);
                }
                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                return byteCount + 1;
            }
            else if (byteCount <= byte.MaxValue)
            {
                if (useOffset != 2)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 2, byteCount);
                }

                bytes[offset] = MessagePackCode.Str8;
                bytes[offset + 1] = unchecked((byte)byteCount);
                return byteCount + 2;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (useOffset != 3)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 3, byteCount);
                }

                bytes[offset] = MessagePackCode.Str16;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 2] = unchecked((byte)byteCount);
                return byteCount + 3;
            }
            else
            {
                if (useOffset != 5)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 5, byteCount);
                }

                bytes[offset] = MessagePackCode.Str32;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
                bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
                bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 4] = unchecked((byte)byteCount);
                return byteCount + 5;
            }
        }

        public static int WriteStringForceStr32Block(ref byte[] bytes, int offset, string value)
        {
            if (value == null) return WriteNil(ref bytes, offset);

            MessagePackBinary.EnsureCapacity(ref bytes, offset, StringEncoding.UTF8.GetMaxByteCount(value.Length) + 5);

            var byteCount = StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 5);

            bytes[offset] = MessagePackCode.Str32;
            bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
            bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
            bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
            bytes[offset + 4] = unchecked((byte)byteCount);
            return byteCount + 5;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormatHeader(ref byte[] bytes, int offset, sbyte typeCode, int dataLength)
        {
            switch (dataLength)
            {
                case 1:
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.FixExt1;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    return 2;
                case 2:
                    EnsureCapacity(ref bytes, offset, 4);
                    bytes[offset] = MessagePackCode.FixExt2;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    return 2;
                case 4:
                    EnsureCapacity(ref bytes, offset, 6);
                    bytes[offset] = MessagePackCode.FixExt4;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    return 2;
                case 8:
                    EnsureCapacity(ref bytes, offset, 10);
                    bytes[offset] = MessagePackCode.FixExt8;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    return 2;
                case 16:
                    EnsureCapacity(ref bytes, offset, 18);
                    bytes[offset] = MessagePackCode.FixExt16;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    return 2;
                default:
                    unchecked
                    {
                        if (dataLength <= byte.MaxValue)
                        {
                            EnsureCapacity(ref bytes, offset, dataLength + 3);
                            bytes[offset] = MessagePackCode.Ext8;
                            bytes[offset + 1] = unchecked((byte)(dataLength));
                            bytes[offset + 2] = unchecked((byte)typeCode);
                            return 3;
                        }
                        else if (dataLength <= UInt16.MaxValue)
                        {
                            EnsureCapacity(ref bytes, offset, dataLength + 4);
                            bytes[offset] = MessagePackCode.Ext16;
                            bytes[offset + 1] = unchecked((byte)(dataLength >> 8));
                            bytes[offset + 2] = unchecked((byte)(dataLength));
                            bytes[offset + 3] = unchecked((byte)typeCode);
                            return 4;
                        }
                        else
                        {
                            EnsureCapacity(ref bytes, offset, dataLength + 6);
                            bytes[offset] = MessagePackCode.Ext32;
                            bytes[offset + 1] = unchecked((byte)(dataLength >> 24));
                            bytes[offset + 2] = unchecked((byte)(dataLength >> 16));
                            bytes[offset + 3] = unchecked((byte)(dataLength >> 8));
                            bytes[offset + 4] = unchecked((byte)dataLength);
                            bytes[offset + 5] = unchecked((byte)typeCode);
                            return 6;
                        }
                    }
            }
        }

        /// <summary>
        /// Write extension format header, always use ext32 format(length is fixed, 6).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormatHeaderForceExt32Block(ref byte[] bytes, int offset, sbyte typeCode, int dataLength)
        {
            EnsureCapacity(ref bytes, offset, dataLength + 6);
            bytes[offset] = MessagePackCode.Ext32;
            bytes[offset + 1] = unchecked((byte)(dataLength >> 24));
            bytes[offset + 2] = unchecked((byte)(dataLength >> 16));
            bytes[offset + 3] = unchecked((byte)(dataLength >> 8));
            bytes[offset + 4] = unchecked((byte)dataLength);
            bytes[offset + 5] = unchecked((byte)typeCode);
            return 6;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormat(ref byte[] bytes, int offset, sbyte typeCode, byte[] data)
        {
            var length = data.Length;
            switch (length)
            {
                case 1:
                    EnsureCapacity(ref bytes, offset, 3);
                    bytes[offset] = MessagePackCode.FixExt1;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    bytes[offset + 2] = data[0];
                    return 3;
                case 2:
                    EnsureCapacity(ref bytes, offset, 4);
                    bytes[offset] = MessagePackCode.FixExt2;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    bytes[offset + 2] = data[0];
                    bytes[offset + 3] = data[1];
                    return 4;
                case 4:
                    EnsureCapacity(ref bytes, offset, 6);
                    bytes[offset] = MessagePackCode.FixExt4;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    bytes[offset + 2] = data[0];
                    bytes[offset + 3] = data[1];
                    bytes[offset + 4] = data[2];
                    bytes[offset + 5] = data[3];
                    return 6;
                case 8:
                    EnsureCapacity(ref bytes, offset, 10);
                    bytes[offset] = MessagePackCode.FixExt8;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    bytes[offset + 2] = data[0];
                    bytes[offset + 3] = data[1];
                    bytes[offset + 4] = data[2];
                    bytes[offset + 5] = data[3];
                    bytes[offset + 6] = data[4];
                    bytes[offset + 7] = data[5];
                    bytes[offset + 8] = data[6];
                    bytes[offset + 9] = data[7];
                    return 10;
                case 16:
                    EnsureCapacity(ref bytes, offset, 18);
                    bytes[offset] = MessagePackCode.FixExt16;
                    bytes[offset + 1] = unchecked((byte)typeCode);
                    bytes[offset + 2] = data[0];
                    bytes[offset + 3] = data[1];
                    bytes[offset + 4] = data[2];
                    bytes[offset + 5] = data[3];
                    bytes[offset + 6] = data[4];
                    bytes[offset + 7] = data[5];
                    bytes[offset + 8] = data[6];
                    bytes[offset + 9] = data[7];
                    bytes[offset + 10] = data[8];
                    bytes[offset + 11] = data[9];
                    bytes[offset + 12] = data[10];
                    bytes[offset + 13] = data[11];
                    bytes[offset + 14] = data[12];
                    bytes[offset + 15] = data[13];
                    bytes[offset + 16] = data[14];
                    bytes[offset + 17] = data[15];
                    return 18;
                default:
                    unchecked
                    {
                        if (data.Length <= byte.MaxValue)
                        {
                            EnsureCapacity(ref bytes, offset, length + 3);
                            bytes[offset] = MessagePackCode.Ext8;
                            bytes[offset + 1] = unchecked((byte)(length));
                            bytes[offset + 2] = unchecked((byte)typeCode);
                            Buffer.BlockCopy(data, 0, bytes, offset + 3, length);
                            return length + 3;
                        }
                        else if (data.Length <= UInt16.MaxValue)
                        {
                            EnsureCapacity(ref bytes, offset, length + 4);
                            bytes[offset] = MessagePackCode.Ext16;
                            bytes[offset + 1] = unchecked((byte)(length >> 8));
                            bytes[offset + 2] = unchecked((byte)(length));
                            bytes[offset + 3] = unchecked((byte)typeCode);
                            Buffer.BlockCopy(data, 0, bytes, offset + 4, length);
                            return length + 4;
                        }
                        else
                        {
                            EnsureCapacity(ref bytes, offset, length + 6);
                            bytes[offset] = MessagePackCode.Ext32;
                            bytes[offset + 1] = unchecked((byte)(length >> 24));
                            bytes[offset + 2] = unchecked((byte)(length >> 16));
                            bytes[offset + 3] = unchecked((byte)(length >> 8));
                            bytes[offset + 4] = unchecked((byte)length);
                            bytes[offset + 5] = unchecked((byte)typeCode);
                            Buffer.BlockCopy(data, 0, bytes, offset + 6, length);
                            return length + 6;
                        }
                    }
            }
        }

        // Timestamp spec
        // https://github.com/msgpack/msgpack/pull/209
        // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
        // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteDateTime(ref byte[] bytes, int offset, DateTime dateTime)
        {
            dateTime = dateTime.ToUniversalTime();

            var secondsSinceBclEpoch = dateTime.Ticks / TimeSpan.TicksPerSecond;
            var seconds = secondsSinceBclEpoch - DateTimeConstants.BclSecondsAtUnixEpoch;
            var nanoseconds = (dateTime.Ticks % TimeSpan.TicksPerSecond) * DateTimeConstants.NanosecondsPerTick;

            // reference pseudo code.
            /*
            struct timespec {
                long tv_sec;  // seconds
                long tv_nsec; // nanoseconds
            } time;
            if ((time.tv_sec >> 34) == 0)
            {
                uint64_t data64 = (time.tv_nsec << 34) | time.tv_sec;
                if (data & 0xffffffff00000000L == 0)
                {
                    // timestamp 32
                    uint32_t data32 = data64;
                    serialize(0xd6, -1, data32)
                }
                else
                {
                    // timestamp 64
                    serialize(0xd7, -1, data64)
                }
            }
            else
            {
                // timestamp 96
                serialize(0xc7, 12, -1, time.tv_nsec, time.tv_sec)
            }
            */

            if ((seconds >> 34) == 0)
            {
                var data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
                if ((data64 & 0xffffffff00000000L) == 0)
                {
                    // timestamp 32(seconds in 32-bit unsigned int)
                    var data32 = (UInt32)data64;
                    EnsureCapacity(ref bytes, offset, 6);
                    bytes[offset] = MessagePackCode.FixExt4;
                    bytes[offset + 1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                    bytes[offset + 2] = unchecked((byte)(data32 >> 24));
                    bytes[offset + 3] = unchecked((byte)(data32 >> 16));
                    bytes[offset + 4] = unchecked((byte)(data32 >> 8));
                    bytes[offset + 5] = unchecked((byte)data32);
                    return 6;
                }
                else
                {
                    // timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
                    EnsureCapacity(ref bytes, offset, 10);
                    bytes[offset] = MessagePackCode.FixExt8;
                    bytes[offset + 1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                    bytes[offset + 2] = unchecked((byte)(data64 >> 56));
                    bytes[offset + 3] = unchecked((byte)(data64 >> 48));
                    bytes[offset + 4] = unchecked((byte)(data64 >> 40));
                    bytes[offset + 5] = unchecked((byte)(data64 >> 32));
                    bytes[offset + 6] = unchecked((byte)(data64 >> 24));
                    bytes[offset + 7] = unchecked((byte)(data64 >> 16));
                    bytes[offset + 8] = unchecked((byte)(data64 >> 8));
                    bytes[offset + 9] = unchecked((byte)data64);
                    return 10;
                }
            }
            else
            {
                // timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
                EnsureCapacity(ref bytes, offset, 15);
                bytes[offset] = MessagePackCode.Ext8;
                bytes[offset + 1] = (byte)12;
                bytes[offset + 2] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                bytes[offset + 3] = unchecked((byte)(nanoseconds >> 24));
                bytes[offset + 4] = unchecked((byte)(nanoseconds >> 16));
                bytes[offset + 5] = unchecked((byte)(nanoseconds >> 8));
                bytes[offset + 6] = unchecked((byte)nanoseconds);
                bytes[offset + 7] = unchecked((byte)(seconds >> 56));
                bytes[offset + 8] = unchecked((byte)(seconds >> 48));
                bytes[offset + 9] = unchecked((byte)(seconds >> 40));
                bytes[offset + 10] = unchecked((byte)(seconds >> 32));
                bytes[offset + 11] = unchecked((byte)(seconds >> 24));
                bytes[offset + 12] = unchecked((byte)(seconds >> 16));
                bytes[offset + 13] = unchecked((byte)(seconds >> 8));
                bytes[offset + 14] = unchecked((byte)seconds);
                return 15;
            }
        }
    }

    // Stream Overload
    partial class MessagePackBinary
    {
        static class StreamDecodeMemoryPool
        {
            [ThreadStatic]
            static byte[] buffer = null;

            public static byte[] GetBuffer()
            {
                if (buffer == null)
                {
                    buffer = new byte[65536];
                }
                return buffer;
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteNil(Stream stream)
        {
            stream.WriteByte(MessagePackCode.Nil);
            return 1;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixMapCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedMapHeaderUnsafe(Stream stream, int count)
        {
            stream.WriteByte((byte)(MessagePackCode.MinFixMap | count));
            return 1;
        }

        /// <summary>
        /// Write map count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeader(Stream stream, int count)
        {
            checked
            {
                return WriteMapHeader(stream, (uint)count);
            }
        }

        /// <summary>
        /// Write map count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeader(Stream stream, uint count)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteMapHeader(ref buffer, 0, count);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Write map format header, always use map32 format(length is fixed, 5).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteMapHeaderForceMap32Block(Stream stream, uint count)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteMapHeaderForceMap32Block(ref buffer, 0, count);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixArrayCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedArrayHeaderUnsafe(Stream stream, int count)
        {
            stream.WriteByte((byte)(MessagePackCode.MinFixArray | count));
            return 1;
        }

        /// <summary>
        /// Write array count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeader(Stream stream, int count)
        {
            checked
            {
                return WriteArrayHeader(stream, (uint)count);
            }
        }

        /// <summary>
        /// Write array count.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeader(Stream stream, uint count)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteArrayHeader(ref buffer, 0, count);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Write array format header, always use array32 format(length is fixed, 5).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteArrayHeaderForceArray32Block(Stream stream, uint count)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteArrayHeaderForceArray32Block(ref buffer, 0, count);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBoolean(Stream stream, bool value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteBoolean(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteByte(Stream stream, byte value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteByte(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteByteForceByteBlock(Stream stream, byte value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteByteForceByteBlock(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBytes(Stream stream, byte[] value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteBytes(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBytes(Stream stream, byte[] src, int srcOffset, int count)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteBytes(ref buffer, 0, src, srcOffset, count);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSByte(Stream stream, sbyte value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteSByte(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSByteForceSByteBlock(Stream stream, sbyte value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteSByteForceSByteBlock(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSingle(Stream stream, float value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteSingle(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteDouble(Stream stream, double value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteDouble(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt16(Stream stream, short value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt16(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt16ForceInt16Block(Stream stream, short value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt16ForceInt16Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackCode.MaxFixInt(127), can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WritePositiveFixedIntUnsafe(Stream stream, int value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WritePositiveFixedIntUnsafe(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt32(Stream stream, int value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt32(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Acquire static message block(always 5 bytes).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt32ForceInt32Block(Stream stream, int value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt32ForceInt32Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt64(Stream stream, long value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt64(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteInt64ForceInt64Block(Stream stream, long value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteInt64ForceInt64Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt16(Stream stream, ushort value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt16(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt16ForceUInt16Block(Stream stream, ushort value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt16ForceUInt16Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt32(Stream stream, uint value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt32(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt32ForceUInt32Block(Stream stream, uint value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt32ForceUInt32Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt64(Stream stream, ulong value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt64(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteUInt64ForceUInt64Block(Stream stream, ulong value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteUInt64ForceUInt64Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteChar(Stream stream, char value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteChar(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Unsafe. If value is guranteed length is 0 ~ 31, can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteFixedStringUnsafe(Stream stream, string value, int byteCount)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteFixedStringUnsafe(ref buffer, 0, value, byteCount);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Unsafe. If pre-calculated byteCount of target string, can use this method.
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteStringUnsafe(Stream stream, string value, int byteCount)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteStringUnsafe(ref buffer, 0, value, byteCount);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteStringBytes(Stream stream, byte[] utf8stringBytes)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteStringBytes(ref buffer, 0, utf8stringBytes);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        public static int WriteString(Stream stream, string value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteString(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        public static int WriteStringForceStr32Block(Stream stream, string value)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteStringForceStr32Block(ref buffer, 0, value);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormatHeader(Stream stream, sbyte typeCode, int dataLength)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteExtensionFormatHeader(ref buffer, 0, typeCode, dataLength);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

        /// <summary>
        /// Write extension format header, always use ext32 format(length is fixed, 6).
        /// </summary>
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormatHeaderForceExt32Block(Stream stream, sbyte typeCode, int dataLength)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteExtensionFormatHeaderForceExt32Block(ref buffer, 0, typeCode, dataLength);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteExtensionFormat(Stream stream, sbyte typeCode, byte[] data)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteExtensionFormat(ref buffer, 0, typeCode, data);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteDateTime(Stream stream, DateTime dateTime)
        {
            var buffer = StreamDecodeMemoryPool.GetBuffer();
            var writeCount = WriteDateTime(ref buffer, 0, dateTime);
            stream.Write(buffer, 0, writeCount);
            return writeCount;
        }
    }
}

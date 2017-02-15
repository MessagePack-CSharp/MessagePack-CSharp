using MessagePack.Decoders;
using System;

namespace MessagePack
{
    /// <summary>
    /// Encode/Decode Utility of MessagePack Spec.
    /// https://github.com/msgpack/msgpack/blob/master/spec.md
    /// </summary>
    public static class MessagePackBinary
    {
        const int MaxSize = 256; // [0] ~ [255]

        static readonly IMapHeaderDecoder[] mapHeaderDecoders = new IMapHeaderDecoder[MaxSize];
        static readonly IArrayHeaderDecoder[] arrayHeaderDecoders = new IArrayHeaderDecoder[MaxSize];
        static readonly IBooleanDecoder[] booleanDecoders = new IBooleanDecoder[MaxSize];
        static readonly IByteDecoder[] byteDecoders = new IByteDecoder[MaxSize];
        static readonly IBytesDecoder[] bytesDecoders = new IBytesDecoder[MaxSize];
        static readonly ISByteDecoder[] sbyteDecoders = new ISByteDecoder[MaxSize];
        static readonly ISingleDecoder[] singleDecoders = new ISingleDecoder[MaxSize];
        static readonly IDoubleDecoder[] doubleDecoders = new IDoubleDecoder[MaxSize];
        static readonly IInt16Decoder[] int16Decoders = new IInt16Decoder[MaxSize];
        static readonly IInt32Decoder[] int32Decoders = new IInt32Decoder[MaxSize];
        static readonly IInt64Decoder[] int64Decoders = new IInt64Decoder[MaxSize];

        static MessagePackBinary()
        {
            // Init LookupTable.
            for (int i = 0; i < MaxSize; i++)
            {
                mapHeaderDecoders[i] = Decoders.InvalidMapHeader.Instance;
                arrayHeaderDecoders[i] = Decoders.InvalidArrayHeader.Instance;
                booleanDecoders[i] = Decoders.InvalidBoolean.Instance;
                byteDecoders[i] = Decoders.InvalidByte.Instance;
                bytesDecoders[i] = Decoders.InvalidBytes.Instance;
                sbyteDecoders[i] = Decoders.InvalidSByte.Instance;
                singleDecoders[i] = Decoders.InvalidSingle.Instance;
                doubleDecoders[i] = Decoders.InvalidDouble.Instance;
                int16Decoders[i] = Decoders.InvalidInt16.Instance;
                int32Decoders[i] = Decoders.InvalidInt32.Instance;
                int64Decoders[i] = Decoders.InvalidInt64.Instance;
            }

            // Number
            for (int i = MessagePackCode.MinNegativeFixInt; i <= MessagePackCode.MaxNegativeFixInt; i++)
            {
                sbyteDecoders[i] = Decoders.FixSByte.Instance;
                int16Decoders[i] = Decoders.FixNegativeInt16.Instance;
                int32Decoders[i] = Decoders.FixNegativeInt32.Instance;
                int64Decoders[i] = Decoders.FixNegativeInt64.Instance;
            }
            for (int i = MessagePackCode.MinFixInt; i <= MessagePackCode.MaxFixInt; i++)
            {
                byteDecoders[i] = Decoders.FixByte.Instance;
                sbyteDecoders[i] = Decoders.FixSByte.Instance;
                int16Decoders[i] = Decoders.FixInt16.Instance;
                int32Decoders[i] = Decoders.FixInt32.Instance;
                int64Decoders[i] = Decoders.FixInt64.Instance;
            }

            byteDecoders[MessagePackCode.UInt8] = Decoders.UInt8Byte.Instance;

            //typeLookupTable[MessagePackCode.UInt16] = MessagePackType.Integer;
            //typeLookupTable[MessagePackCode.UInt32] = MessagePackType.Integer;
            //typeLookupTable[MessagePackCode.UInt64] = MessagePackType.Integer;

            sbyteDecoders[MessagePackCode.Int8] = Decoders.Int8SByte.Instance;
            int16Decoders[MessagePackCode.Int8] = Decoders.Int8Int16.Instance;
            int16Decoders[MessagePackCode.Int16] = Decoders.Int16Int16.Instance;
            int32Decoders[MessagePackCode.Int8] = Decoders.Int8Int32.Instance;
            int32Decoders[MessagePackCode.Int16] = Decoders.Int16Int32.Instance;
            int32Decoders[MessagePackCode.Int32] = Decoders.Int32Int32.Instance;
            int64Decoders[MessagePackCode.Int8] = Decoders.Int8Int64.Instance;
            int64Decoders[MessagePackCode.Int16] = Decoders.Int16Int64.Instance;
            int64Decoders[MessagePackCode.Int32] = Decoders.Int32Int64.Instance;
            int64Decoders[MessagePackCode.Int64] = Decoders.Int64Int64.Instance;

            singleDecoders[MessagePackCode.Float32] = Decoders.Float32Single.Instance;
            doubleDecoders[MessagePackCode.Float32] = Decoders.Float32Double.Instance;
            doubleDecoders[MessagePackCode.Float64] = Decoders.Float64Double.Instance;

            // Map
            for (int i = MessagePackCode.MinFixMap; i <= MessagePackCode.MaxFixMap; i++)
            {
                mapHeaderDecoders[i] = Decoders.FixMapHeader.Instance;
            }
            mapHeaderDecoders[MessagePackCode.Map16] = Decoders.Map16Header.Instance;
            mapHeaderDecoders[MessagePackCode.Map32] = Decoders.Map32Header.Instance;

            // Array
            for (int i = MessagePackCode.MinFixArray; i <= MessagePackCode.MaxFixArray; i++)
            {
                arrayHeaderDecoders[i] = Decoders.FixArrayHeader.Instance;
            }
            arrayHeaderDecoders[MessagePackCode.Array16] = Decoders.Array16Header.Instance;
            arrayHeaderDecoders[MessagePackCode.Array32] = Decoders.Array32Header.Instance;

            // Str
            for (int i = MessagePackCode.MinFixStr; i <= MessagePackCode.MaxFixStr; i++)
            {
                //typeLookupTable[i] = MessagePackType.String;
            }
            //typeLookupTable[MessagePackCode.Str8] = MessagePackType.String;
            //typeLookupTable[MessagePackCode.Str16] = MessagePackType.String;
            //typeLookupTable[MessagePackCode.Str32] = MessagePackType.String;

            // Others
            bytesDecoders[MessagePackCode.Nil] = Decoders.NilBytes.Instance;

            booleanDecoders[MessagePackCode.False] = Decoders.False.Instance;
            booleanDecoders[MessagePackCode.True] = Decoders.True.Instance;

            bytesDecoders[MessagePackCode.Bin8] = Decoders.Bin8Bytes.Instance;
            bytesDecoders[MessagePackCode.Bin16] = Decoders.Bin16Bytes.Instance;
            bytesDecoders[MessagePackCode.Bin32] = Decoders.Bin32Bytes.Instance;

            // Ext

            //typeLookupTable[MessagePackCode.FixExt1] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.FixExt2] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.FixExt4] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.FixExt8] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.FixExt16] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.Ext8] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.Ext16] = MessagePackType.Extension;
            //typeLookupTable[MessagePackCode.Ext32] = MessagePackType.Extension;
        }

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
                if (num < current * 2)
                {
                    num = current * 2;
                }

                FastResize(ref bytes, num);
            }
        }

        // Buffer.BlockCopy version of Array.Resize
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

        public static byte[] FastResizeClone(byte[] array, int newSize)
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

        public static MessagePackType GetMessagePackType(byte[] bytes, int offset)
        {
            return MessagePackCode.ToMessagePackType(bytes[offset]);
        }

        public static int WriteNil(ref byte[] bytes, int offset)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = MessagePackCode.Nil;
            return 1;
        }

        public static Nil ReadNil(byte[] bytes, int offset, out int readSize)
        {
            if (bytes[offset] == MessagePackCode.Nil)
            {
                readSize = 1;
                return Nil.Default;
            }
            else
            {
                throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
            }
        }

        public static bool IsNil(byte[] bytes, int offset)
        {
            return bytes[offset] == MessagePackCode.Nil;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixMapCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
        public static int WriteFixedMapHeaderUnsafe(ref byte[] bytes, int offset, int count)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)(MessagePackCode.MinFixMap | count);
            return 1;
        }

        /// <summary>
        /// Write map count.
        /// </summary>
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
        /// Return map count.
        /// </summary>
        public static uint ReadMapHeader(byte[] bytes, int offset, out int readSize)
        {
            return mapHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        /// <summary>
        /// Write array count.
        /// </summary>
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
        /// Return array count.
        /// </summary>
        public static uint ReadArraydHeader(byte[] bytes, int offset, out int readSize)
        {
            return arrayHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }


        public static int WriteBoolean(ref byte[] bytes, int offset, bool value)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = (value ? MessagePackCode.True : MessagePackCode.False);
            return 1;
        }

        public static bool ReadBoolean(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return booleanDecoders[bytes[offset]].Read();
        }

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

        public static byte ReadByte(byte[] bytes, int offset, out int readSize)
        {
            return byteDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        public static int WriteBytes(ref byte[] bytes, int offset, byte[] value)
        {
            if (value == null)
            {
                return WriteNil(ref bytes, offset);
            }

            var len = value.Length;
            if (len <= byte.MaxValue)
            {
                var size = value.Length + 2;
                EnsureCapacity(ref bytes, offset, size);

                bytes[offset] = MessagePackCode.Bin8;
                bytes[offset + 1] = (byte)len;

                Buffer.BlockCopy(value, 0, bytes, offset + 2, value.Length);
                return size;
            }
            else if (len <= UInt16.MaxValue)
            {
                var size = value.Length + 3;
                EnsureCapacity(ref bytes, offset, size);

                unchecked
                {
                    bytes[offset] = MessagePackCode.Bin16;
                    bytes[offset + 1] = (byte)(len >> 8);
                    bytes[offset + 2] = (byte)(len);
                }

                Buffer.BlockCopy(value, 0, bytes, offset + 3, value.Length);
                return size;
            }
            else
            {
                var size = value.Length + 5;
                EnsureCapacity(ref bytes, offset, size);

                unchecked
                {
                    bytes[offset] = MessagePackCode.Bin32;
                    bytes[offset + 1] = (byte)(len >> 24);
                    bytes[offset + 2] = (byte)(len >> 16);
                    bytes[offset + 3] = (byte)(len >> 8);
                    bytes[offset + 4] = (byte)(len);
                }

                Buffer.BlockCopy(value, 0, bytes, offset + 5, value.Length);
                return size;
            }
        }
        public static byte[] ReadBytes(byte[] bytes, int offset, out int readSize)
        {
            return bytesDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

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

        public static sbyte ReadSByte(byte[] bytes, int offset, out int readSize)
        {
            return sbyteDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

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

        public static float ReadSingle(byte[] bytes, int offset, out int readSize)
        {
            return singleDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

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

        public static double ReadDouble(byte[] bytes, int offset, out int readSize)
        {
            return doubleDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        public static int WriteInt16(ref byte[] bytes, int offset, short value)
        {
            if (MessagePackRange.MinFixNegativeInt <= value && value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
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

        public static short ReadInt16(byte[] bytes, int offset, out int readSize)
        {
            return int16Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackCode.MaxFixInt(127), can use this method.
        /// </summary>
        /// <returns></returns>
        public static int WritePositiveFixedIntUnsafe(ref byte[] bytes, int offset, int value)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)value;
            return 1;
        }

        public static int WriteInt32(ref byte[] bytes, int offset, int value)
        {
            if (MessagePackRange.MinFixNegativeInt <= value && value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.Int8;
                bytes[offset + 1] = unchecked((byte)value);
                return 2;
            }
            else if (short.MinValue <= value && value <= short.MaxValue)
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

        public static int ReadInt32(byte[] bytes, int offset, out int readSize)
        {
            return int32Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        public static int WriteInt64(ref byte[] bytes, int offset, long value)
        {
            if (MessagePackRange.MinFixNegativeInt <= value && value <= MessagePackRange.MaxFixPositiveInt)
            {
                EnsureCapacity(ref bytes, offset, 1);
                bytes[offset] = unchecked((byte)value);
                return 1;
            }
            else if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 2);
                bytes[offset] = MessagePackCode.Int8;
                bytes[offset + 1] = unchecked((byte)value);
                return 2;
            }
            else if (short.MinValue <= value && value <= short.MaxValue)
            {
                EnsureCapacity(ref bytes, offset, 3);
                bytes[offset] = MessagePackCode.Int16;
                bytes[offset + 1] = unchecked((byte)(value >> 8));
                bytes[offset + 2] = unchecked((byte)value);
                return 3;
            }
            else if (int.MinValue <= value && value <= int.MaxValue)
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

        public static long ReadInt64(byte[] bytes, int offset, out int readSize)
        {
            return int64Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe int WriteUInt16(ref byte[] bytes, int offset, ushort value)
        {
            EnsureCapacity(ref bytes, offset, 2);

            fixed (byte* ptr = bytes)
            {
                *(ushort*)(ptr + offset) = value;
            }

            return 2;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe ushort ReadUInt16(ref byte[] bytes, int offset)
        {
            fixed (byte* ptr = bytes)
            {
                return *(ushort*)(ptr + offset);
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe int WriteUInt32(ref byte[] bytes, int offset, uint value)
        {
            EnsureCapacity(ref bytes, offset, 4);

            fixed (byte* ptr = bytes)
            {
                *(uint*)(ptr + offset) = value;
            }

            return 4;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe uint ReadUInt32(ref byte[] bytes, int offset)
        {
            fixed (byte* ptr = bytes)
            {
                return *(uint*)(ptr + offset);
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe int WriteUInt64(ref byte[] bytes, int offset, ulong value)
        {
            EnsureCapacity(ref bytes, offset, 8);

            fixed (byte* ptr = bytes)
            {
                *(ulong*)(ptr + offset) = value;
            }

            return 8;
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe ulong ReadUInt64(ref byte[] bytes, int offset)
        {
            fixed (byte* ptr = bytes)
            {
                return *(ulong*)(ptr + offset);
            }
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteChar(ref byte[] bytes, int offset, char value)
        {
            return WriteUInt16(ref bytes, offset, (ushort)value);
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static char ReadChar(ref byte[] bytes, int offset)
        {
            return (char)ReadUInt16(ref bytes, offset);
        }

        /// <summary>
        /// Unsafe. If value is guranteed length is 0 ~ 31, can use this method.
        /// </summary>
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
        public static int WriteVariableStringUnsafe(ref byte[] bytes, int offset, string value, int byteCount)
        {
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                EnsureCapacity(ref bytes, offset, byteCount + 1);
                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, offset + 1);
            }

            // TODO:str8, str16, str32 format...
            throw new NotImplementedException();
        }

        public static int WriteString(ref byte[] bytes, int offset, string value)
        {
            var byteCount = StringEncoding.UTF8.GetByteCount(value);
            return WriteVariableStringUnsafe(ref bytes, offset, value, byteCount);
        }

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static string ReadString(ref byte[] bytes, int offset, int count)
        {
            return StringEncoding.UTF8.GetString(bytes, offset, count);
        }

        #region Timestamp/Duration

        public static unsafe int WriteTimeSpan(ref byte[] bytes, int offset, TimeSpan timeSpan)
        {
            checked
            {
                long ticks = timeSpan.Ticks;
                long seconds = ticks / TimeSpan.TicksPerSecond;
                int nanos = (int)(ticks % TimeSpan.TicksPerSecond) * Duration.NanosecondsPerTick;

                EnsureCapacity(ref bytes, offset, 12);
                fixed (byte* ptr = bytes)
                {
                    *(long*)(ptr + offset) = seconds;
                    *(int*)(ptr + offset + 8) = nanos;
                }

                return 12;
            }
        }

        public static unsafe TimeSpan ReadTimeSpan(ref byte[] bytes, int offset)
        {
            checked
            {
                fixed (byte* ptr = bytes)
                {
                    var seconds = *(long*)(ptr + offset);
                    var nanos = *(int*)(ptr + offset + 8);

                    if (!Duration.IsNormalized(seconds, nanos))
                    {
                        throw new InvalidOperationException("Duration was not a valid normalized duration");
                    }
                    long ticks = seconds * TimeSpan.TicksPerSecond + nanos / Duration.NanosecondsPerTick;
                    return TimeSpan.FromTicks(ticks);
                }
            }
        }

        public static unsafe int WriteDateTime(ref byte[] bytes, int offset, DateTime dateTime)
        {
            dateTime = dateTime.ToUniversalTime();

            // Do the arithmetic using DateTime.Ticks, which is always non-negative, making things simpler.
            long secondsSinceBclEpoch = dateTime.Ticks / TimeSpan.TicksPerSecond;
            int nanoseconds = (int)(dateTime.Ticks % TimeSpan.TicksPerSecond) * Duration.NanosecondsPerTick;

            EnsureCapacity(ref bytes, offset, 12);
            fixed (byte* ptr = bytes)
            {
                *(long*)(ptr + offset) = (secondsSinceBclEpoch - Timestamp.BclSecondsAtUnixEpoch);
                *(int*)(ptr + offset + 8) = nanoseconds;
            }

            return 12;
        }

        public static unsafe DateTime ReadDateTime(ref byte[] bytes, int offset)
        {
            fixed (byte* ptr = bytes)
            {
                var seconds = *(long*)(ptr + offset);
                var nanos = *(int*)(ptr + offset + 8);

                if (!Timestamp.IsNormalized(seconds, nanos))
                {
                    throw new InvalidOperationException(string.Format(@"Timestamp contains invalid values: Seconds={0}; Nanos={1}", seconds, nanos));
                }
                return Timestamp.UnixEpoch.AddSeconds(seconds).AddTicks(nanos / Duration.NanosecondsPerTick);
            }
        }

        internal static class Timestamp
        {
            internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            internal const long BclSecondsAtUnixEpoch = 62135596800;
            internal const long UnixSecondsAtBclMaxValue = 253402300799;
            internal const long UnixSecondsAtBclMinValue = -BclSecondsAtUnixEpoch;
            internal const int MaxNanos = Duration.NanosecondsPerSecond - 1;

            internal static bool IsNormalized(long seconds, int nanoseconds)
            {
                return nanoseconds >= 0 &&
                    nanoseconds <= MaxNanos &&
                    seconds >= UnixSecondsAtBclMinValue &&
                    seconds <= UnixSecondsAtBclMaxValue;
            }
        }

        internal static class Duration
        {
            public const int NanosecondsPerSecond = 1000000000;
            public const int NanosecondsPerTick = 100;
            public const long MaxSeconds = 315576000000L;
            public const long MinSeconds = -315576000000L;
            internal const int MaxNanoseconds = NanosecondsPerSecond - 1;
            internal const int MinNanoseconds = -NanosecondsPerSecond + 1;

            internal static bool IsNormalized(long seconds, int nanoseconds)
            {
                // Simple boundaries
                if (seconds < MinSeconds || seconds > MaxSeconds ||
                    nanoseconds < MinNanoseconds || nanoseconds > MaxNanoseconds)
                {
                    return false;
                }
                // We only have a problem is one is strictly negative and the other is
                // strictly positive.
                return Math.Sign(seconds) * Math.Sign(nanoseconds) != -1;
            }
        }

        #endregion
    }
}

namespace MessagePack.Decoders
{
    internal interface IMapHeaderDecoder
    {
        uint Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixMapHeader : IMapHeaderDecoder
    {
        internal static readonly IMapHeaderDecoder Instance = new FixMapHeader();

        FixMapHeader()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return (uint)(bytes[offset] & 0xF);
        }
    }

    internal class Map16Header : IMapHeaderDecoder
    {
        internal static readonly IMapHeaderDecoder Instance = new Map16Header();

        Map16Header()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (uint)((bytes[offset + 1] << 8) + (bytes[offset + 2]));
            }
        }
    }

    internal class Map32Header : IMapHeaderDecoder
    {
        internal static readonly IMapHeaderDecoder Instance = new Map32Header();

        Map32Header()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (uint)((bytes[offset + 1] << 24) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 8) + bytes[offset + 4]);
            }
        }
    }

    internal class InvalidMapHeader : IMapHeaderDecoder
    {
        internal static readonly IMapHeaderDecoder Instance = new InvalidMapHeader();

        InvalidMapHeader()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IArrayHeaderDecoder
    {
        uint Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixArrayHeader : IArrayHeaderDecoder
    {
        internal static readonly IArrayHeaderDecoder Instance = new FixArrayHeader();

        FixArrayHeader()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return (uint)(bytes[offset] & 0xF);
        }
    }

    internal class Array16Header : IArrayHeaderDecoder
    {
        internal static readonly IArrayHeaderDecoder Instance = new Array16Header();

        Array16Header()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (uint)((bytes[offset + 1] << 8) + (bytes[offset + 2]));
            }
        }
    }

    internal class Array32Header : IArrayHeaderDecoder
    {
        internal static readonly IArrayHeaderDecoder Instance = new Array32Header();

        Array32Header()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (uint)((bytes[offset + 1] << 24) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 8) + bytes[offset + 4]);
            }
        }
    }

    internal class InvalidArrayHeader : IArrayHeaderDecoder
    {
        internal static readonly IArrayHeaderDecoder Instance = new InvalidArrayHeader();

        InvalidArrayHeader()
        {

        }

        public uint Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IBooleanDecoder
    {
        bool Read();
    }

    internal class True : IBooleanDecoder
    {
        internal static IBooleanDecoder Instance = new True();

        True() { }

        public bool Read()
        {
            return true;
        }
    }

    internal class False : IBooleanDecoder
    {
        internal static IBooleanDecoder Instance = new False();

        False() { }

        public bool Read()
        {
            return false;
        }
    }

    internal class InvalidBoolean : IBooleanDecoder
    {
        internal static IBooleanDecoder Instance = new InvalidBoolean();

        InvalidBoolean() { }

        public bool Read()
        {
            throw new InvalidOperationException("code is invalid.");
        }
    }

    internal interface IByteDecoder
    {
        byte Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixByte : IByteDecoder
    {
        internal static readonly IByteDecoder Instance = new FixByte();

        FixByte()
        {

        }

        public byte Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return bytes[offset];
        }
    }

    internal class UInt8Byte : IByteDecoder
    {
        internal static readonly IByteDecoder Instance = new UInt8Byte();

        UInt8Byte()
        {

        }

        public byte Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return bytes[offset + 1];
        }
    }

    internal class InvalidByte : IByteDecoder
    {
        internal static readonly IByteDecoder Instance = new InvalidByte();

        InvalidByte()
        {

        }

        public byte Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IBytesDecoder
    {
        byte[] Read(byte[] bytes, int offset, out int readSize);
    }

    internal class NilBytes : IBytesDecoder
    {
        internal static readonly IBytesDecoder Instance = new NilBytes();

        NilBytes()
        {

        }

        public byte[] Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return null;
        }
    }

    internal class Bin8Bytes : IBytesDecoder
    {
        internal static readonly IBytesDecoder Instance = new Bin8Bytes();

        Bin8Bytes()
        {

        }

        public byte[] Read(byte[] bytes, int offset, out int readSize)
        {
            var length = bytes[offset + 1];
            var newBytes = new byte[length];
            Buffer.BlockCopy(bytes, offset + 2, newBytes, 0, length);

            readSize = length + 2;
            return newBytes;
        }
    }

    internal class Bin16Bytes : IBytesDecoder
    {
        internal static readonly IBytesDecoder Instance = new Bin16Bytes();

        Bin16Bytes()
        {

        }

        public byte[] Read(byte[] bytes, int offset, out int readSize)
        {
            var length = (bytes[offset + 1] << 8) + (bytes[offset + 2]);
            var newBytes = new byte[length];
            Buffer.BlockCopy(bytes, offset + 3, newBytes, 0, length);

            readSize = length + 3;
            return newBytes;
        }
    }

    internal class Bin32Bytes : IBytesDecoder
    {
        internal static readonly IBytesDecoder Instance = new Bin32Bytes();

        Bin32Bytes()
        {

        }

        public byte[] Read(byte[] bytes, int offset, out int readSize)
        {
            var length = (bytes[offset + 1] << 24) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 8) + (bytes[offset + 4]);
            var newBytes = new byte[length];
            Buffer.BlockCopy(bytes, offset + 5, newBytes, 0, length);

            readSize = length + 5;
            return newBytes;
        }
    }

    internal class InvalidBytes : IBytesDecoder
    {
        internal static readonly IBytesDecoder Instance = new InvalidBytes();

        InvalidBytes()
        {

        }

        public byte[] Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface ISByteDecoder
    {
        sbyte Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixSByte : ISByteDecoder
    {
        internal static readonly ISByteDecoder Instance = new FixSByte();

        FixSByte()
        {

        }

        public sbyte Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((sbyte)bytes[offset]);
        }
    }

    internal class Int8SByte : ISByteDecoder
    {
        internal static readonly ISByteDecoder Instance = new Int8SByte();

        Int8SByte()
        {

        }

        public sbyte Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((sbyte)(bytes[offset + 1]));
        }
    }

    internal class InvalidSByte : ISByteDecoder
    {
        internal static readonly ISByteDecoder Instance = new InvalidSByte();

        InvalidSByte()
        {

        }

        public sbyte Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface ISingleDecoder
    {
        float Read(byte[] bytes, int offset, out int readSize);
    }

    internal class Float32Single : ISingleDecoder
    {
        internal static readonly ISingleDecoder Instance = new Float32Single();

        Float32Single()
        {

        }

        public Single Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            return new Float32Bits(bytes, offset + 1).Value;
        }
    }

    internal class InvalidSingle : ISingleDecoder
    {
        internal static readonly ISingleDecoder Instance = new InvalidSingle();

        InvalidSingle()
        {

        }

        public Single Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IDoubleDecoder
    {
        double Read(byte[] bytes, int offset, out int readSize);
    }

    internal class Float32Double : IDoubleDecoder
    {
        internal static readonly IDoubleDecoder Instance = new Float32Double();

        Float32Double()
        {

        }

        public Double Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            return new Float32Bits(bytes, offset + 1).Value;
        }
    }

    internal class Float64Double : IDoubleDecoder
    {
        internal static readonly IDoubleDecoder Instance = new Float64Double();

        Float64Double()
        {

        }

        public Double Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 9;
            return new Float64Bits(bytes, offset + 1).Value;
        }
    }

    internal class InvalidDouble : IDoubleDecoder
    {
        internal static readonly IDoubleDecoder Instance = new InvalidDouble();

        InvalidDouble()
        {

        }

        public Double Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IInt16Decoder
    {
        Int16 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixNegativeInt16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new FixNegativeInt16();

        FixNegativeInt16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((short)(sbyte)bytes[offset]);
        }
    }

    internal class FixInt16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new FixInt16();

        FixInt16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((short)bytes[offset]);
        }
    }

    internal class Int8Int16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new Int8Int16();

        Int8Int16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((short)(sbyte)(bytes[offset + 1]));
        }
    }

    internal class Int16Int16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new Int16Int16();

        Int16Int16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (short)((bytes[offset + 1] << 8) + (bytes[offset + 2]));
            }
        }
    }

    internal class InvalidInt16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new InvalidInt16();

        InvalidInt16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IInt32Decoder
    {
        Int32 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixNegativeInt32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new FixNegativeInt32();

        FixNegativeInt32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((int)(sbyte)bytes[offset]);
        }
    }

    internal class FixInt32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new FixInt32();

        FixInt32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((int)bytes[offset]);
        }
    }

    internal class Int8Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new Int8Int32();

        Int8Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((int)(sbyte)(bytes[offset + 1]));
        }
    }

    internal class Int16Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new Int16Int32();

        Int16Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (int)(short)((bytes[offset + 1] << 8) + (bytes[offset + 2]));
            }
        }
    }

    internal class Int32Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new Int32Int32();

        Int32Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (int)((bytes[offset + 1] << 24) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 8) + bytes[offset + 4]);
            }
        }
    }

    internal class InvalidInt32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new InvalidInt32();

        InvalidInt32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

















    internal interface IInt64Decoder
    {
        Int64 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixNegativeInt64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new FixNegativeInt64();

        FixNegativeInt64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((long)(sbyte)bytes[offset]);
        }
    }

    internal class FixInt64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new FixInt64();

        FixInt64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((long)bytes[offset]);
        }
    }

    internal class Int8Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new Int8Int64();

        Int8Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((long)(sbyte)(bytes[offset + 1]));
        }
    }

    internal class Int16Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new Int16Int64();

        Int16Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (long)(short)((bytes[offset + 1] << 8) + (bytes[offset + 2]));
            }
        }
    }

    internal class Int32Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new Int32Int64();

        Int32Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (long)(int)((bytes[offset + 1] << 24) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 8) + bytes[offset + 4]);
            }
        }
    }

    internal class Int64Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new Int64Int64();

        Int64Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 9;
            unchecked
            {
                return (long)bytes[offset + 1] << 56 | (long)bytes[offset + 2] << 48 | (long)bytes[offset + 3] << 40 | (long)bytes[offset + 4] << 32
                     | (long)bytes[offset + 5] << 24 | (long)bytes[offset + 6] << 16 | (long)bytes[offset + 7] << 8 | (long)bytes[offset + 8];
            }
        }
    }

    internal class InvalidInt64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new InvalidInt64();

        InvalidInt64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }
}
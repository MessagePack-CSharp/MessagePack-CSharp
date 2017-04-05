using MessagePack.Decoders;
using MessagePack.Internal;
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
        static readonly IUInt16Decoder[] uint16Decoders = new IUInt16Decoder[MaxSize];
        static readonly IUInt32Decoder[] uint32Decoders = new IUInt32Decoder[MaxSize];
        static readonly IUInt64Decoder[] uint64Decoders = new IUInt64Decoder[MaxSize];
        static readonly IStringDecoder[] stringDecoders = new IStringDecoder[MaxSize];
        static readonly IExtDecoder[] extDecoders = new IExtDecoder[MaxSize];
        static readonly IExtHeaderDecoder[] extHeaderDecoders = new IExtHeaderDecoder[MaxSize];
        static readonly IDateTimeDecoder[] dateTimeDecoders = new IDateTimeDecoder[MaxSize];
        static readonly IReadNextDecoder[] readNextDecoders = new IReadNextDecoder[MaxSize];

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
                uint16Decoders[i] = Decoders.InvalidUInt16.Instance;
                uint32Decoders[i] = Decoders.InvalidUInt32.Instance;
                uint64Decoders[i] = Decoders.InvalidUInt64.Instance;
                stringDecoders[i] = Decoders.InvalidString.Instance;
                extDecoders[i] = Decoders.InvalidExt.Instance;
                extHeaderDecoders[i] = Decoders.InvalidExtHeader.Instance;
                dateTimeDecoders[i] = Decoders.InvalidDateTime.Instance;
            }

            // Number
            for (int i = MessagePackCode.MinNegativeFixInt; i <= MessagePackCode.MaxNegativeFixInt; i++)
            {
                sbyteDecoders[i] = Decoders.FixSByte.Instance;
                int16Decoders[i] = Decoders.FixNegativeInt16.Instance;
                int32Decoders[i] = Decoders.FixNegativeInt32.Instance;
                int64Decoders[i] = Decoders.FixNegativeInt64.Instance;
                readNextDecoders[i] = Decoders.ReadNext1.Instance;
            }
            for (int i = MessagePackCode.MinFixInt; i <= MessagePackCode.MaxFixInt; i++)
            {
                byteDecoders[i] = Decoders.FixByte.Instance;
                sbyteDecoders[i] = Decoders.FixSByte.Instance;
                int16Decoders[i] = Decoders.FixInt16.Instance;
                int32Decoders[i] = Decoders.FixInt32.Instance;
                int64Decoders[i] = Decoders.FixInt64.Instance;
                uint16Decoders[i] = Decoders.FixUInt16.Instance;
                uint32Decoders[i] = Decoders.FixUInt32.Instance;
                uint64Decoders[i] = Decoders.FixUInt64.Instance;
                readNextDecoders[i] = Decoders.ReadNext1.Instance;
            }

            byteDecoders[MessagePackCode.UInt8] = Decoders.UInt8Byte.Instance;
            sbyteDecoders[MessagePackCode.Int8] = Decoders.Int8SByte.Instance;
            int16Decoders[MessagePackCode.UInt8] = Decoders.UInt8Int16.Instance;
            int16Decoders[MessagePackCode.UInt16] = Decoders.UInt16Int16.Instance;
            int16Decoders[MessagePackCode.Int8] = Decoders.Int8Int16.Instance;
            int16Decoders[MessagePackCode.Int16] = Decoders.Int16Int16.Instance;
            int32Decoders[MessagePackCode.UInt8] = Decoders.UInt8Int32.Instance;
            int32Decoders[MessagePackCode.UInt16] = Decoders.UInt16Int32.Instance;
            int32Decoders[MessagePackCode.UInt32] = Decoders.UInt32Int32.Instance;
            int32Decoders[MessagePackCode.Int8] = Decoders.Int8Int32.Instance;
            int32Decoders[MessagePackCode.Int16] = Decoders.Int16Int32.Instance;
            int32Decoders[MessagePackCode.Int32] = Decoders.Int32Int32.Instance;
            int64Decoders[MessagePackCode.UInt8] = Decoders.UInt8Int64.Instance;
            int64Decoders[MessagePackCode.UInt16] = Decoders.UInt16Int64.Instance;
            int64Decoders[MessagePackCode.UInt32] = Decoders.UInt32Int64.Instance;
            int64Decoders[MessagePackCode.UInt64] = Decoders.UInt64Int64.Instance;
            int64Decoders[MessagePackCode.Int8] = Decoders.Int8Int64.Instance;
            int64Decoders[MessagePackCode.Int16] = Decoders.Int16Int64.Instance;
            int64Decoders[MessagePackCode.Int32] = Decoders.Int32Int64.Instance;
            int64Decoders[MessagePackCode.Int64] = Decoders.Int64Int64.Instance;
            uint16Decoders[MessagePackCode.UInt8] = Decoders.UInt8UInt16.Instance;
            uint16Decoders[MessagePackCode.UInt16] = Decoders.UInt16UInt16.Instance;
            uint32Decoders[MessagePackCode.UInt8] = Decoders.UInt8UInt32.Instance;
            uint32Decoders[MessagePackCode.UInt16] = Decoders.UInt16UInt32.Instance;
            uint32Decoders[MessagePackCode.UInt32] = Decoders.UInt32UInt32.Instance;
            uint64Decoders[MessagePackCode.UInt8] = Decoders.UInt8UInt64.Instance;
            uint64Decoders[MessagePackCode.UInt16] = Decoders.UInt16UInt64.Instance;
            uint64Decoders[MessagePackCode.UInt32] = Decoders.UInt32UInt64.Instance;
            uint64Decoders[MessagePackCode.UInt64] = Decoders.UInt64UInt64.Instance;
            singleDecoders[MessagePackCode.Float32] = Decoders.Float32Single.Instance;
            doubleDecoders[MessagePackCode.Float32] = Decoders.Float32Double.Instance;
            doubleDecoders[MessagePackCode.Float64] = Decoders.Float64Double.Instance;

            readNextDecoders[MessagePackCode.Int8] = Decoders.ReadNext2.Instance;
            readNextDecoders[MessagePackCode.Int16] = Decoders.ReadNext3.Instance;
            readNextDecoders[MessagePackCode.Int32] = Decoders.ReadNext5.Instance;
            readNextDecoders[MessagePackCode.Int64] = Decoders.ReadNext9.Instance;
            readNextDecoders[MessagePackCode.UInt8] = Decoders.ReadNext2.Instance;
            readNextDecoders[MessagePackCode.UInt16] = Decoders.ReadNext3.Instance;
            readNextDecoders[MessagePackCode.UInt32] = Decoders.ReadNext5.Instance;
            readNextDecoders[MessagePackCode.UInt64] = Decoders.ReadNext9.Instance;
            readNextDecoders[MessagePackCode.Float32] = Decoders.ReadNext5.Instance;
            readNextDecoders[MessagePackCode.Float64] = Decoders.ReadNext9.Instance;

            // Map
            for (int i = MessagePackCode.MinFixMap; i <= MessagePackCode.MaxFixMap; i++)
            {
                mapHeaderDecoders[i] = Decoders.FixMapHeader.Instance;
                readNextDecoders[i] = Decoders.ReadNext1.Instance;
            }
            mapHeaderDecoders[MessagePackCode.Map16] = Decoders.Map16Header.Instance;
            mapHeaderDecoders[MessagePackCode.Map32] = Decoders.Map32Header.Instance;
            readNextDecoders[MessagePackCode.Map16] = Decoders.ReadNextMap.Instance;
            readNextDecoders[MessagePackCode.Map32] = Decoders.ReadNextMap.Instance;

            // Array
            for (int i = MessagePackCode.MinFixArray; i <= MessagePackCode.MaxFixArray; i++)
            {
                arrayHeaderDecoders[i] = Decoders.FixArrayHeader.Instance;
                readNextDecoders[i] = Decoders.ReadNext1.Instance;
            }
            arrayHeaderDecoders[MessagePackCode.Array16] = Decoders.Array16Header.Instance;
            arrayHeaderDecoders[MessagePackCode.Array32] = Decoders.Array32Header.Instance;
            readNextDecoders[MessagePackCode.Array16] = Decoders.ReadNextArray.Instance;
            readNextDecoders[MessagePackCode.Array32] = Decoders.ReadNextArray.Instance;

            // Str
            for (int i = MessagePackCode.MinFixStr; i <= MessagePackCode.MaxFixStr; i++)
            {
                stringDecoders[i] = Decoders.FixString.Instance;
                readNextDecoders[i] = Decoders.ReadNextFixStr.Instance;
            }

            stringDecoders[MessagePackCode.Str8] = Decoders.Str8String.Instance;
            stringDecoders[MessagePackCode.Str16] = Decoders.Str16String.Instance;
            stringDecoders[MessagePackCode.Str32] = Decoders.Str32String.Instance;
            readNextDecoders[MessagePackCode.Str8] = Decoders.ReadNextStr8.Instance;
            readNextDecoders[MessagePackCode.Str16] = Decoders.ReadNextStr16.Instance;
            readNextDecoders[MessagePackCode.Str32] = Decoders.ReadNextStr32.Instance;

            // Others
            stringDecoders[MessagePackCode.Nil] = Decoders.NilString.Instance;
            bytesDecoders[MessagePackCode.Nil] = Decoders.NilBytes.Instance;
            readNextDecoders[MessagePackCode.Nil] = Decoders.ReadNext1.Instance;

            booleanDecoders[MessagePackCode.False] = Decoders.False.Instance;
            booleanDecoders[MessagePackCode.True] = Decoders.True.Instance;
            readNextDecoders[MessagePackCode.False] = Decoders.ReadNext1.Instance;
            readNextDecoders[MessagePackCode.True] = Decoders.ReadNext1.Instance;

            bytesDecoders[MessagePackCode.Bin8] = Decoders.Bin8Bytes.Instance;
            bytesDecoders[MessagePackCode.Bin16] = Decoders.Bin16Bytes.Instance;
            bytesDecoders[MessagePackCode.Bin32] = Decoders.Bin32Bytes.Instance;
            readNextDecoders[MessagePackCode.Bin8] = Decoders.ReadNextBin8.Instance;
            readNextDecoders[MessagePackCode.Bin16] = Decoders.ReadNextBin16.Instance;
            readNextDecoders[MessagePackCode.Bin32] = Decoders.ReadNextBin32.Instance;

            // Ext
            extDecoders[MessagePackCode.FixExt1] = Decoders.FixExt1.Instance;
            extDecoders[MessagePackCode.FixExt2] = Decoders.FixExt2.Instance;
            extDecoders[MessagePackCode.FixExt4] = Decoders.FixExt4.Instance;
            extDecoders[MessagePackCode.FixExt8] = Decoders.FixExt8.Instance;
            extDecoders[MessagePackCode.FixExt16] = Decoders.FixExt16.Instance;
            extDecoders[MessagePackCode.Ext8] = Decoders.Ext8.Instance;
            extDecoders[MessagePackCode.Ext16] = Decoders.Ext16.Instance;
            extDecoders[MessagePackCode.Ext32] = Decoders.Ext32.Instance;

            extHeaderDecoders[MessagePackCode.FixExt1] = Decoders.FixExt1Header.Instance;
            extHeaderDecoders[MessagePackCode.FixExt2] = Decoders.FixExt2Header.Instance;
            extHeaderDecoders[MessagePackCode.FixExt4] = Decoders.FixExt4Header.Instance;
            extHeaderDecoders[MessagePackCode.FixExt8] = Decoders.FixExt8Header.Instance;
            extHeaderDecoders[MessagePackCode.FixExt16] = Decoders.FixExt16Header.Instance;
            extHeaderDecoders[MessagePackCode.Ext8] = Decoders.Ext8Header.Instance;
            extHeaderDecoders[MessagePackCode.Ext16] = Decoders.Ext16Header.Instance;
            extHeaderDecoders[MessagePackCode.Ext32] = Decoders.Ext32Header.Instance;


            readNextDecoders[MessagePackCode.FixExt1] = Decoders.ReadNext3.Instance;
            readNextDecoders[MessagePackCode.FixExt2] = Decoders.ReadNext4.Instance;
            readNextDecoders[MessagePackCode.FixExt4] = Decoders.ReadNext6.Instance;
            readNextDecoders[MessagePackCode.FixExt8] = Decoders.ReadNext10.Instance;
            readNextDecoders[MessagePackCode.FixExt16] = Decoders.ReadNext18.Instance;
            readNextDecoders[MessagePackCode.Ext8] = Decoders.ReadNextExt8.Instance;
            readNextDecoders[MessagePackCode.Ext16] = Decoders.ReadNextExt16.Instance;
            readNextDecoders[MessagePackCode.Ext32] = Decoders.ReadNextExt32.Instance;

            // DateTime
            dateTimeDecoders[MessagePackCode.FixExt4] = Decoders.FixExt4DateTime.Instance;
            dateTimeDecoders[MessagePackCode.FixExt8] = Decoders.FixExt8DateTime.Instance;
            dateTimeDecoders[MessagePackCode.Ext8] = Decoders.Ext8DateTime.Instance;
        }

#if NETSTANDARD1_4
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
                if (num < current * 2)
                {
                    num = current * 2;
                }

                FastResize(ref bytes, num);
            }
        }

        // Buffer.BlockCopy version of Array.Resize
#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static MessagePackType GetMessagePackType(byte[] bytes, int offset)
        {
            return MessagePackCode.ToMessagePackType(bytes[offset]);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int ReadNext(byte[] bytes, int offset)
        {
            return readNextDecoders[bytes[offset]].Read(bytes, offset);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int ReadNextBlock(byte[] bytes, int offset)
        {
            switch (GetMessagePackType(bytes, offset))
            {
                case MessagePackType.Unknown:
                case MessagePackType.Integer:
                case MessagePackType.Nil:
                case MessagePackType.Boolean:
                case MessagePackType.Float:
                case MessagePackType.String:
                case MessagePackType.Binary:
                case MessagePackType.Extension:
                default:
                    return ReadNext(bytes, offset);
                case MessagePackType.Array:
                    {
                        var startOffset = offset;
                        int readSize;
                        var header = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                        offset += readSize;
                        for (int i = 0; i < header; i++)
                        {
                            offset += ReadNextBlock(bytes, offset);
                        }
                        return offset - startOffset;
                    }
                case MessagePackType.Map:
                    {
                        var startOffset = offset;
                        int readSize;
                        var header = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
                        offset += readSize;
                        for (int i = 0; i < header; i++)
                        {
                            offset += ReadNextBlock(bytes, offset); // read key block
                            offset += ReadNextBlock(bytes, offset); // read value block
                        }
                        return offset - startOffset;
                    }
            }
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteNil(ref byte[] bytes, int offset)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = MessagePackCode.Nil;
            return 1;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsNil(byte[] bytes, int offset)
        {
            return bytes[offset] == MessagePackCode.Nil;
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackRange.MaxFixMapCount(15), can use this method.
        /// </summary>
        /// <returns></returns>
#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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
        /// Return map count.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int ReadMapHeader(byte[] bytes, int offset, out int readSize)
        {
            checked
            {
                return (int)mapHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
            }
        }

        /// <summary>
        /// Return map count.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static uint ReadMapHeaderRaw(byte[] bytes, int offset, out int readSize)
        {
            return mapHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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
        /// Return array count.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int ReadArrayHeader(byte[] bytes, int offset, out int readSize)
        {
            checked
            {
                return (int)arrayHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
            }
        }

        /// <summary>
        /// Return array count.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static uint ReadArrayHeaderRaw(byte[] bytes, int offset, out int readSize)
        {
            return arrayHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteBoolean(ref byte[] bytes, int offset, bool value)
        {
            EnsureCapacity(ref bytes, offset, 1);

            bytes[offset] = (value ? MessagePackCode.True : MessagePackCode.False);
            return 1;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool ReadBoolean(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return booleanDecoders[bytes[offset]].Read();
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteByteForceByteBlock(ref byte[] bytes, int offset, byte value)
        {
            EnsureCapacity(ref bytes, offset, 2);
            bytes[offset] = MessagePackCode.UInt8;
            bytes[offset + 1] = value;
            return 2;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static byte ReadByte(byte[] bytes, int offset, out int readSize)
        {
            return byteDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static byte[] ReadBytes(byte[] bytes, int offset, out int readSize)
        {
            return bytesDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteSByteForceSByteBlock(ref byte[] bytes, int offset, sbyte value)
        {
            EnsureCapacity(ref bytes, offset, 2);
            bytes[offset] = MessagePackCode.Int8;
            bytes[offset + 1] = unchecked((byte)(value));
            return 2;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static sbyte ReadSByte(byte[] bytes, int offset, out int readSize)
        {
            return sbyteDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static float ReadSingle(byte[] bytes, int offset, out int readSize)
        {
            return singleDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static double ReadDouble(byte[] bytes, int offset, out int readSize)
        {
            return doubleDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static short ReadInt16(byte[] bytes, int offset, out int readSize)
        {
            return int16Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        /// <summary>
        /// Unsafe. If value is guranteed 0 ~ MessagePackCode.MaxFixInt(127), can use this method.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WritePositiveFixedIntUnsafe(ref byte[] bytes, int offset, int value)
        {
            EnsureCapacity(ref bytes, offset, 1);
            bytes[offset] = (byte)value;
            return 1;
        }

#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int ReadInt32(byte[] bytes, int offset, out int readSize)
        {
            return int32Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static long ReadInt64(byte[] bytes, int offset, out int readSize)
        {
            return int64Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static ushort ReadUInt16(byte[] bytes, int offset, out int readSize)
        {
            return uint16Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static uint ReadUInt32(byte[] bytes, int offset, out int readSize)
        {
            return uint32Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static ulong ReadUInt64(byte[] bytes, int offset, out int readSize)
        {
            return uint64Decoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static int WriteChar(ref byte[] bytes, int offset, char value)
        {
            return WriteUInt16(ref bytes, offset, (ushort)value);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static char ReadChar(byte[] bytes, int offset, out int readSize)
        {
            return (char)ReadUInt16(bytes, offset, out readSize);
        }

        /// <summary>
        /// Unsafe. If value is guranteed length is 0 ~ 31, can use this method.
        /// </summary>
#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static string ReadString(byte[] bytes, int offset, out int readSize)
        {
            return stringDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

#if NETSTANDARD1_4
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
#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static ExtensionResult ReadExtensionFormat(byte[] bytes, int offset, out int readSize)
        {
            return extDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        /// <summary>
        /// return byte length of ExtensionFormat.
        /// </summary>
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static ExtensionHeader ReadExtensionFormatHeader(byte[] bytes, int offset, out int readSize)
        {
            return extHeaderDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }

        // Timestamp spec
        // https://github.com/msgpack/msgpack/pull/209
        // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
        // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

#if NETSTANDARD1_4
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static DateTime ReadDateTime(byte[] bytes, int offset, out int readSize)
        {
            return dateTimeDecoders[bytes[offset]].Read(bytes, offset, out readSize);
        }
    }

    public struct ExtensionResult
    {
        public sbyte TypeCode { get; private set; }
        public byte[] Data { get; private set; }

        public ExtensionResult(sbyte typeCode, byte[] data)
        {
            TypeCode = typeCode;
            Data = data;
        }
    }

    public struct ExtensionHeader
    {
        public sbyte TypeCode { get; private set; }
        public uint Length { get; private set; }

        public ExtensionHeader(sbyte typeCode, uint length)
        {
            TypeCode = typeCode;
            Length = length;
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DateTimeConstants
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal const long BclSecondsAtUnixEpoch = 62135596800;
        internal const int NanosecondsPerTick = 100;
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
                return (uint)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
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
                return (uint)((bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 8) | bytes[offset + 4]);
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
                return (uint)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
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
                return (uint)((bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 8) | bytes[offset + 4]);
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
            var length = (bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 8) | (bytes[offset + 4]);
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

    internal class UInt8Int16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new UInt8Int16();

        UInt8Int16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((short)(byte)(bytes[offset + 1]));
        }
    }

    internal class UInt16Int16 : IInt16Decoder
    {
        internal static readonly IInt16Decoder Instance = new UInt16Int16();

        UInt16Int16()
        {

        }

        public Int16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            return checked((Int16)((bytes[offset + 1] << 8) + (bytes[offset + 2])));
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
                return (short)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
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

    internal class UInt8Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new UInt8Int32();

        UInt8Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((int)(byte)(bytes[offset + 1]));
        }
    }
    internal class UInt16Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new UInt16Int32();

        UInt16Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            return (Int32)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
        }
    }

    internal class UInt32Int32 : IInt32Decoder
    {
        internal static readonly IInt32Decoder Instance = new UInt32Int32();

        UInt32Int32()
        {

        }

        public Int32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            checked
            {
                return (Int32)((UInt32)(bytes[offset + 1] << 24) | (UInt32)(bytes[offset + 2] << 16) | (UInt32)(bytes[offset + 3] << 8) | (UInt32)bytes[offset + 4]);
            }
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
                return (int)(short)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
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
                return (int)((bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 8) | bytes[offset + 4]);
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

    internal class UInt8Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new UInt8Int64();

        UInt8Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((int)(byte)(bytes[offset + 1]));
        }
    }
    internal class UInt16Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new UInt16Int64();

        UInt16Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            return (Int64)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
        }
    }

    internal class UInt32Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new UInt32Int64();

        UInt32Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            return unchecked((Int64)((uint)(bytes[offset + 1] << 24) | ((uint)bytes[offset + 2] << 16) | ((uint)bytes[offset + 3] << 8) | (uint)bytes[offset + 4]));
        }
    }

    internal class UInt64Int64 : IInt64Decoder
    {
        internal static readonly IInt64Decoder Instance = new UInt64Int64();

        UInt64Int64()
        {

        }

        public Int64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 9;
            checked
            {
                return (Int64)bytes[offset + 1] << 56 | (Int64)bytes[offset + 2] << 48 | (Int64)bytes[offset + 3] << 40 | (Int64)bytes[offset + 4] << 32
                     | (Int64)bytes[offset + 5] << 24 | (Int64)bytes[offset + 6] << 16 | (Int64)bytes[offset + 7] << 8 | (Int64)bytes[offset + 8];
            }
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
                return (long)(short)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
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
                return (long)((long)(bytes[offset + 1] << 24) + (long)(bytes[offset + 2] << 16) + (long)(bytes[offset + 3] << 8) + (long)bytes[offset + 4]);
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

    internal interface IUInt16Decoder
    {
        UInt16 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixUInt16 : IUInt16Decoder
    {
        internal static readonly IUInt16Decoder Instance = new FixUInt16();

        FixUInt16()
        {

        }

        public UInt16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((UInt16)bytes[offset]);
        }
    }

    internal class UInt8UInt16 : IUInt16Decoder
    {
        internal static readonly IUInt16Decoder Instance = new UInt8UInt16();

        UInt8UInt16()
        {

        }

        public UInt16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((UInt16)(bytes[offset + 1]));
        }
    }

    internal class UInt16UInt16 : IUInt16Decoder
    {
        internal static readonly IUInt16Decoder Instance = new UInt16UInt16();

        UInt16UInt16()
        {

        }

        public UInt16 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (UInt16)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
            }
        }
    }

    internal class InvalidUInt16 : IUInt16Decoder
    {
        internal static readonly IUInt16Decoder Instance = new InvalidUInt16();

        InvalidUInt16()
        {

        }

        public UInt16 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IUInt32Decoder
    {
        UInt32 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixUInt32 : IUInt32Decoder
    {
        internal static readonly IUInt32Decoder Instance = new FixUInt32();

        FixUInt32()
        {

        }

        public UInt32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((UInt32)bytes[offset]);
        }
    }

    internal class UInt8UInt32 : IUInt32Decoder
    {
        internal static readonly IUInt32Decoder Instance = new UInt8UInt32();

        UInt8UInt32()
        {

        }

        public UInt32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((UInt32)(bytes[offset + 1]));
        }
    }

    internal class UInt16UInt32 : IUInt32Decoder
    {
        internal static readonly IUInt32Decoder Instance = new UInt16UInt32();

        UInt16UInt32()
        {

        }

        public UInt32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (UInt32)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
            }
        }
    }

    internal class UInt32UInt32 : IUInt32Decoder
    {
        internal static readonly IUInt32Decoder Instance = new UInt32UInt32();

        UInt32UInt32()
        {

        }

        public UInt32 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (UInt32)((UInt32)(bytes[offset + 1] << 24) | (UInt32)(bytes[offset + 2] << 16) | (UInt32)(bytes[offset + 3] << 8) | (UInt32)bytes[offset + 4]);
            }
        }
    }

    internal class InvalidUInt32 : IUInt32Decoder
    {
        internal static readonly IUInt32Decoder Instance = new InvalidUInt32();

        InvalidUInt32()
        {

        }

        public UInt32 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IUInt64Decoder
    {
        UInt64 Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixUInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new FixUInt64();

        FixUInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return unchecked((UInt64)bytes[offset]);
        }
    }

    internal class UInt8UInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new UInt8UInt64();

        UInt8UInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            return unchecked((UInt64)(bytes[offset + 1]));
        }
    }

    internal class UInt16UInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new UInt16UInt64();

        UInt16UInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            unchecked
            {
                return (UInt64)((bytes[offset + 1] << 8) | (bytes[offset + 2]));
            }
        }
    }

    internal class UInt32UInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new UInt32UInt64();

        UInt32UInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 5;
            unchecked
            {
                return (UInt64)(((UInt64)bytes[offset + 1] << 24) + (ulong)(bytes[offset + 2] << 16) + (UInt64)(bytes[offset + 3] << 8) + (UInt64)bytes[offset + 4]);
            }
        }
    }

    internal class UInt64UInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new UInt64UInt64();

        UInt64UInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 9;
            unchecked
            {
                return (UInt64)bytes[offset + 1] << 56 | (UInt64)bytes[offset + 2] << 48 | (UInt64)bytes[offset + 3] << 40 | (UInt64)bytes[offset + 4] << 32
                     | (UInt64)bytes[offset + 5] << 24 | (UInt64)bytes[offset + 6] << 16 | (UInt64)bytes[offset + 7] << 8 | (UInt64)bytes[offset + 8];
            }
        }
    }

    internal class InvalidUInt64 : IUInt64Decoder
    {
        internal static readonly IUInt64Decoder Instance = new InvalidUInt64();

        InvalidUInt64()
        {

        }

        public UInt64 Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IStringDecoder
    {
        String Read(byte[] bytes, int offset, out int readSize);
    }

    internal class NilString : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new NilString();

        NilString()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 1;
            return null;
        }
    }

    internal class FixString : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new FixString();

        FixString()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            var length = bytes[offset] & 0x1F;
            readSize = length + 1;
            return StringEncoding.UTF8.GetString(bytes, offset + 1, length);
        }
    }

    internal class Str8String : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new Str8String();

        Str8String()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            var length = (int)bytes[offset + 1];
            readSize = length + 2;
            return StringEncoding.UTF8.GetString(bytes, offset + 2, length);
        }
    }

    internal class Str16String : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new Str16String();

        Str16String()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (bytes[offset + 1] << 8) + (bytes[offset + 2]);
                readSize = length + 3;
                return StringEncoding.UTF8.GetString(bytes, offset + 3, length);
            }
        }
    }

    internal class Str32String : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new Str32String();

        Str32String()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (int)((uint)(bytes[offset + 1] << 24) | (uint)(bytes[offset + 2] << 16) | (uint)(bytes[offset + 3] << 8) | (uint)bytes[offset + 4]);
                readSize = length + 5;
                return StringEncoding.UTF8.GetString(bytes, offset + 5, length);
            }
        }
    }

    internal class InvalidString : IStringDecoder
    {
        internal static readonly IStringDecoder Instance = new InvalidString();

        InvalidString()
        {

        }

        public String Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IExtDecoder
    {
        ExtensionResult Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixExt1 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new FixExt1();

        FixExt1()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 3;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            var body = new byte[1] { bytes[offset + 2] }; // make new bytes is overhead?
            return new ExtensionResult(typeCode, body);
        }
    }

    internal class FixExt2 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new FixExt2();

        FixExt2()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 4;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            var body = new byte[2]
            {
                bytes[offset + 2],
                bytes[offset + 3],
            };
            return new ExtensionResult(typeCode, body);
        }
    }

    internal class FixExt4 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new FixExt4();

        FixExt4()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 6;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            var body = new byte[4]
            {
                bytes[offset + 2],
                bytes[offset + 3],
                bytes[offset + 4],
                bytes[offset + 5],
            };
            return new ExtensionResult(typeCode, body);
        }
    }

    internal class FixExt8 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new FixExt8();

        FixExt8()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 10;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            var body = new byte[8]
            {
                bytes[offset + 2],
                bytes[offset + 3],
                bytes[offset + 4],
                bytes[offset + 5],
                bytes[offset + 6],
                bytes[offset + 7],
                bytes[offset + 8],
                bytes[offset + 9],
            };
            return new ExtensionResult(typeCode, body);
        }
    }

    internal class FixExt16 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new FixExt16();

        FixExt16()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 18;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            var body = new byte[16]
            {
                bytes[offset + 2],
                bytes[offset + 3],
                bytes[offset + 4],
                bytes[offset + 5],
                bytes[offset + 6],
                bytes[offset + 7],
                bytes[offset + 8],
                bytes[offset + 9],
                bytes[offset + 10],
                bytes[offset + 11],
                bytes[offset + 12],
                bytes[offset + 13],
                bytes[offset + 14],
                bytes[offset + 15],
                bytes[offset + 16],
                bytes[offset + 17]
            };
            return new ExtensionResult(typeCode, body);
        }
    }

    internal class Ext8 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new Ext8();

        Ext8()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = bytes[offset + 1];
                var typeCode = unchecked((sbyte)bytes[offset + 2]);

                var body = new byte[length];
                readSize = (int)length + 3;
                Buffer.BlockCopy(bytes, offset + 3, body, 0, (int)length);
                return new ExtensionResult(typeCode, body);
            }
        }
    }

    internal class Ext16 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new Ext16();

        Ext16()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (int)((UInt16)(bytes[offset + 1] << 8) | (UInt16)bytes[offset + 2]);
                var typeCode = unchecked((sbyte)bytes[offset + 3]);

                var body = new byte[length];
                readSize = length + 4;
                Buffer.BlockCopy(bytes, offset + 4, body, 0, (int)length);
                return new ExtensionResult(typeCode, body);
            }
        }
    }

    internal class Ext32 : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new Ext32();

        Ext32()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (UInt32)((UInt32)(bytes[offset + 1] << 24) | (UInt32)(bytes[offset + 2] << 16) | (UInt32)(bytes[offset + 3] << 8) | (UInt32)bytes[offset + 4]);
                var typeCode = unchecked((sbyte)bytes[offset + 5]);

                var body = new byte[length];
                checked
                {
                    readSize = (int)length + 6;
                    Buffer.BlockCopy(bytes, offset + 6, body, 0, (int)length);
                }
                return new ExtensionResult(typeCode, body);
            }
        }
    }

    internal class InvalidExt : IExtDecoder
    {
        internal static readonly IExtDecoder Instance = new InvalidExt();

        InvalidExt()
        {

        }

        public ExtensionResult Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }






    internal interface IExtHeaderDecoder
    {
        ExtensionHeader Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixExt1Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new FixExt1Header();

        FixExt1Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            return new ExtensionHeader(typeCode, 1);
        }
    }

    internal class FixExt2Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new FixExt2Header();

        FixExt2Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            return new ExtensionHeader(typeCode, 2);
        }
    }

    internal class FixExt4Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new FixExt4Header();

        FixExt4Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            return new ExtensionHeader(typeCode, 4);
        }
    }

    internal class FixExt8Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new FixExt8Header();

        FixExt8Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            return new ExtensionHeader(typeCode, 8);
        }
    }

    internal class FixExt16Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new FixExt16Header();

        FixExt16Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            readSize = 2;
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            return new ExtensionHeader(typeCode, 16);
        }
    }

    internal class Ext8Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new Ext8Header();

        Ext8Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = bytes[offset + 1];
                var typeCode = unchecked((sbyte)bytes[offset + 2]);

                readSize = 3;
                return new ExtensionHeader(typeCode, length);
            }
        }
    }

    internal class Ext16Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new Ext16Header();

        Ext16Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (UInt32)((UInt16)(bytes[offset + 1] << 8) | (UInt16)bytes[offset + 2]);
                var typeCode = unchecked((sbyte)bytes[offset + 3]);

                readSize = 4;
                return new ExtensionHeader(typeCode, length);
            }
        }
    }

    internal class Ext32Header : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new Ext32Header();

        Ext32Header()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            unchecked
            {
                var length = (UInt32)((UInt32)(bytes[offset + 1] << 24) | (UInt32)(bytes[offset + 2] << 16) | (UInt32)(bytes[offset + 3] << 8) | (UInt32)bytes[offset + 4]);
                var typeCode = unchecked((sbyte)bytes[offset + 5]);

                readSize = 6;
                return new ExtensionHeader(typeCode, length);
            }
        }
    }

    internal class InvalidExtHeader : IExtHeaderDecoder
    {
        internal static readonly IExtHeaderDecoder Instance = new InvalidExtHeader();

        InvalidExtHeader()
        {

        }

        public ExtensionHeader Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IDateTimeDecoder
    {
        DateTime Read(byte[] bytes, int offset, out int readSize);
    }

    internal class FixExt4DateTime : IDateTimeDecoder
    {
        internal static readonly IDateTimeDecoder Instance = new FixExt4DateTime();

        FixExt4DateTime()
        {

        }

        public DateTime Read(byte[] bytes, int offset, out int readSize)
        {
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            if (typeCode != ReservedMessagePackExtensionTypeCode.DateTime)
            {
                throw new InvalidOperationException(string.Format("typeCode is invalid. typeCode:{0}", typeCode));
            }

            unchecked
            {
                var seconds = (UInt32)((UInt32)(bytes[offset + 2] << 24) | (UInt32)(bytes[offset + 3] << 16) | (UInt32)(bytes[offset + 4] << 8) | (UInt32)bytes[offset + 5]);

                readSize = 6;
                return DateTimeConstants.UnixEpoch.AddSeconds(seconds);
            }
        }
    }

    internal class FixExt8DateTime : IDateTimeDecoder
    {
        internal static readonly IDateTimeDecoder Instance = new FixExt8DateTime();

        FixExt8DateTime()
        {

        }

        public DateTime Read(byte[] bytes, int offset, out int readSize)
        {
            var typeCode = unchecked((sbyte)bytes[offset + 1]);
            if (typeCode != ReservedMessagePackExtensionTypeCode.DateTime)
            {
                throw new InvalidOperationException(string.Format("typeCode is invalid. typeCode:{0}", typeCode));
            }

            var data64 = (UInt64)bytes[offset + 2] << 56 | (UInt64)bytes[offset + 3] << 48 | (UInt64)bytes[offset + 4] << 40 | (UInt64)bytes[offset + 5] << 32
                       | (UInt64)bytes[offset + 6] << 24 | (UInt64)bytes[offset + 7] << 16 | (UInt64)bytes[offset + 8] << 8 | (UInt64)bytes[offset + 9];

            var nanoseconds = (long)(data64 >> 34);
            var seconds = data64 & 0x00000003ffffffffL;

            readSize = 10;
            return DateTimeConstants.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
        }
    }

    internal class Ext8DateTime : IDateTimeDecoder
    {
        internal static readonly IDateTimeDecoder Instance = new Ext8DateTime();

        Ext8DateTime()
        {

        }

        public DateTime Read(byte[] bytes, int offset, out int readSize)
        {
            var length = checked((byte)bytes[offset + 1]);
            var typeCode = unchecked((sbyte)bytes[offset + 2]);
            if (length != 12 || typeCode != ReservedMessagePackExtensionTypeCode.DateTime)
            {
                throw new InvalidOperationException(string.Format("typeCode is invalid. typeCode:{0}", typeCode));
            }

            var nanoseconds = (UInt32)((UInt32)(bytes[offset + 3] << 24) | (UInt32)(bytes[offset + 4] << 16) | (UInt32)(bytes[offset + 5] << 8) | (UInt32)bytes[offset + 6]);
            unchecked
            {
                var seconds = (long)bytes[offset + 7] << 56 | (long)bytes[offset + 8] << 48 | (long)bytes[offset + 9] << 40 | (long)bytes[offset + 10] << 32
                            | (long)bytes[offset + 11] << 24 | (long)bytes[offset + 12] << 16 | (long)bytes[offset + 13] << 8 | (long)bytes[offset + 14];

                readSize = 15;
                return DateTimeConstants.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
            }
        }
    }

    internal class InvalidDateTime : IDateTimeDecoder
    {
        internal static readonly IDateTimeDecoder Instance = new InvalidDateTime();

        InvalidDateTime()
        {

        }

        public DateTime Read(byte[] bytes, int offset, out int readSize)
        {
            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }

    internal interface IReadNextDecoder
    {
        int Read(byte[] bytes, int offset);
    }

    internal class ReadNext1 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext1();
        ReadNext1()
        {

        }
        public int Read(byte[] bytes, int offset) { return 1; }
    }

    internal class ReadNext2 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext2();
        ReadNext2()
        {

        }
        public int Read(byte[] bytes, int offset) { return 2; }

    }
    internal class ReadNext3 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext3();
        ReadNext3()
        {

        }
        public int Read(byte[] bytes, int offset) { return 3; }
    }
    internal class ReadNext4 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext4();
        ReadNext4()
        {

        }
        public int Read(byte[] bytes, int offset) { return 4; }
    }
    internal class ReadNext5 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext5();
        ReadNext5()
        {

        }
        public int Read(byte[] bytes, int offset) { return 5; }
    }
    internal class ReadNext6 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext6();
        ReadNext6()
        {

        }
        public int Read(byte[] bytes, int offset) { return 6; }
    }

    internal class ReadNext9 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext9();
        ReadNext9()
        {

        }
        public int Read(byte[] bytes, int offset) { return 9; }
    }
    internal class ReadNext10 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext10();
        ReadNext10()
        {

        }
        public int Read(byte[] bytes, int offset) { return 10; }
    }
    internal class ReadNext18 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNext18();
        ReadNext18()
        {

        }
        public int Read(byte[] bytes, int offset) { return 18; }
    }

    internal class ReadNextMap : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextMap();
        ReadNextMap()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var startOffset = offset;
            int readSize;
            var length = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;
            for (int i = 0; i < length; i++)
            {
                offset += MessagePackBinary.ReadNext(bytes, offset); // key
                offset += MessagePackBinary.ReadNext(bytes, offset); // value
            }
            return offset - startOffset;
        }
    }

    internal class ReadNextArray : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextArray();
        ReadNextArray()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var startOffset = offset;
            int readSize;
            var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            for (int i = 0; i < length; i++)
            {
                offset += MessagePackBinary.ReadNext(bytes, offset);
            }
            return offset - startOffset;
        }
    }

    internal class ReadNextFixStr : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextFixStr();
        ReadNextFixStr()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = bytes[offset] & 0x1F;
            return length + 1;
        }
    }

    internal class ReadNextStr8 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextStr8();
        ReadNextStr8()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = (int)bytes[offset + 1];
            return length + 2;
        }
    }

    internal class ReadNextStr16 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextStr16();
        ReadNextStr16()
        {

        }
        public int Read(byte[] bytes, int offset)
        {

            var length = (bytes[offset + 1] << 8) | (bytes[offset + 2]);
            return length + 3;
        }
    }

    internal class ReadNextStr32 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextStr32();
        ReadNextStr32()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = (int)((uint)(bytes[offset + 1] << 24) | (uint)(bytes[offset + 2] << 16) | (uint)(bytes[offset + 3] << 8) | (uint)bytes[offset + 4]);
            return length + 5;
        }
    }

    internal class ReadNextBin8 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextBin8();
        ReadNextBin8()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = bytes[offset + 1];
            return length + 2;
        }
    }

    internal class ReadNextBin16 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextBin16();
        ReadNextBin16()
        {

        }
        public int Read(byte[] bytes, int offset)
        {

            var length = (bytes[offset + 1] << 8) | (bytes[offset + 2]);
            return length + 3;
        }
    }

    internal class ReadNextBin32 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextBin32();
        ReadNextBin32()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = (bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 8) | (bytes[offset + 4]);
            return length + 5;
        }
    }

    internal class ReadNextExt8 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextExt8();
        ReadNextExt8()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = bytes[offset + 1];
            return (int)length + 3;
        }
    }

    internal class ReadNextExt16 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextExt16();
        ReadNextExt16()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = (int)((UInt16)(bytes[offset + 1] << 8) | (UInt16)bytes[offset + 2]);
            return length + 4;
        }
    }

    internal class ReadNextExt32 : IReadNextDecoder
    {
        internal static readonly IReadNextDecoder Instance = new ReadNextExt32();
        ReadNextExt32()
        {

        }
        public int Read(byte[] bytes, int offset)
        {
            var length = (UInt32)((UInt32)(bytes[offset + 1] << 24) | (UInt32)(bytes[offset + 2] << 16) | (UInt32)(bytes[offset + 3] << 8) | (UInt32)bytes[offset + 4]);
            return (int)length + 6;
        }
    }
}
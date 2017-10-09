#if NETSTANDARD

using System;

namespace MessagePack.Formatters
{
    public sealed class BinaryGuidFormatter : IMessagePackFormatter<Guid>
    {
        /// <summary>
        /// Unsafe binary Guid formatter. this is only allows on LittleEndian environment.
        /// </summary>
        public static readonly IMessagePackFormatter<Guid> Instance = new BinaryGuidFormatter();

        BinaryGuidFormatter()
        {
        }

        // Guid's underlying _a,...,_k field is sequential and same layuout as .NET Framework and Mono(Unity).
        // But target machines must be same endian so restrict only for little endian.

        public unsafe int Serialize(ref byte[] bytes, int offset, Guid value, IFormatterResolver formatterResolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            MessagePackBinary.EnsureCapacity(ref bytes, offset, 18);
            fixed (byte* dst = &bytes[offset])
            {
                var src = &value;

                dst[0] = MessagePackCode.Bin8;
                dst[1] = 16;

                *(Guid*)(dst + 2) = *src;
            }

            return 18;
        }

        public unsafe Guid Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            if (!(offset + 18 <= bytes.Length))
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (byte* src = &bytes[offset])
            {
                if (src[0] != MessagePackCode.Bin8)
                {
                    throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
                }
                if (src[1] != 16)
                {
                    throw new InvalidOperationException("Invalid Guid Size.");
                }

                var target = *(Guid*)(src + 2);
                readSize = 18;
                return target;
            }
        }
    }

    public sealed class BinaryDecimalFormatter : IMessagePackFormatter<Decimal>
    {
        /// <summary>
        /// Unsafe binary Decimal formatter. this is only allows on LittleEndian environment.
        /// </summary>
        public static readonly IMessagePackFormatter<Decimal> Instance = new BinaryDecimalFormatter();

        BinaryDecimalFormatter()
        {
        }

        // decimal underlying "flags, hi, lo, mid" fields are sequential and same layuout with .NET Framework and Mono(Unity)
        // But target machines must be same endian so restrict only for little endian.

        public unsafe int Serialize(ref byte[] bytes, int offset, Decimal value, IFormatterResolver formatterResolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            MessagePackBinary.EnsureCapacity(ref bytes, offset, 18);
            fixed (byte* dst = &bytes[offset])
            {
                var src = &value;

                dst[0] = MessagePackCode.Bin8;
                dst[1] = 16;

                *(Decimal*)(dst + 2) = *src;
            }

            return 18;
        }

        public unsafe Decimal Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryDecimalFormatter only allows on little endian env.");

            if (!(offset + 18 <= bytes.Length))
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (byte* src = &bytes[offset])
            {
                if (src[0] != MessagePackCode.Bin8)
                {
                    throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
                }
                if (src[1] != 16)
                {
                    throw new InvalidOperationException("Invalid Guid Size.");
                }

                var target = *(Decimal*)(src + 2);
                readSize = 18;
                return target;
            }
        }
    }
}

#endif
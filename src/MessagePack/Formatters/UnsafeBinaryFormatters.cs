#if !UNITY

using System;
using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class BinaryGuidFormatter : IMessagePackFormatter<Guid>
    {
        /// <summary>
        /// Unsafe binary Guid formatter. this is only allowed on LittleEndian environment.
        /// </summary>
        public static readonly IMessagePackFormatter<Guid> Instance = new BinaryGuidFormatter();

        BinaryGuidFormatter()
        {
        }

        // Guid's underlying _a,...,_k field is sequential and same layout as .NET Framework and Mono(Unity).
        // But target machines must be same endian so restrict only for little endian.

        public unsafe void Serialize(ref MessagePackWriter writer, Guid value, IFormatterResolver resolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Guid));
            writer.Write(valueSpan);
        }

        public unsafe Guid Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            var valueSequence = reader.ReadBytes();
            if (valueSequence.Length != sizeof(Guid))
            {
                throw new InvalidOperationException("Invalid Guid Size.");
            }

            Guid result;
            var resultSpan = new Span<byte>(&result, sizeof(Guid));
            valueSequence.CopyTo(resultSpan);
            return result;
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

        public unsafe void Serialize(ref MessagePackWriter writer, Decimal value, IFormatterResolver resolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryGuidFormatter only allows on little endian env.");

            var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Decimal));
            writer.Write(valueSpan);
        }

        public unsafe Decimal Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (!BitConverter.IsLittleEndian) throw new Exception("BinaryDecimalFormatter only allows on little endian env.");

            var valueSequence = reader.ReadBytes();
            if (valueSequence.Length != sizeof(decimal))
            {
                throw new InvalidOperationException("Invalid decimal Size.");
            }

            decimal result;
            var resultSpan = new Span<byte>(&result, sizeof(decimal));
            valueSequence.CopyTo(resultSpan);
            return result;
        }
    }
}

#endif
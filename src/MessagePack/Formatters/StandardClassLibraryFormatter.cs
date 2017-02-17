using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MessagePack.Formatters
{
    // string, decimal, TimeSpan, DateTimeOffset, Guid, Uri, Version, KeyValuePair
    // NET40 -> BigInteger, Complex, Tuple

    public class NullableStringFormatter : IMessagePackFormatter<String>
    {
        public int Serialize(ref byte[] bytes, int offset, String value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteString(ref bytes, offset, value);
            }
        }

        public String Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadString(bytes, offset, out readSize);
            }
        }
    }

    public class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        public int Serialize(ref byte[] bytes, int offset, decimal value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value.ToString(CultureInfo.InvariantCulture));
        }

        public decimal Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return decimal.Parse(MessagePackBinary.ReadString(bytes, offset, out readSize));
        }
    }

    public class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        public int Serialize(ref byte[] bytes, int offset, TimeSpan value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt64(ref bytes, offset, value.Ticks);
        }

        public TimeSpan Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return new TimeSpan(MessagePackBinary.ReadInt64(bytes, offset, out readSize));
        }
    }

    public class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        public int Serialize(ref byte[] bytes, int offset, DateTimeOffset value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteDateTime(ref bytes, offset, value.UtcDateTime);
            offset += MessagePackBinary.WriteInt64(ref bytes, offset, value.Offset.Ticks);
            return offset - startOffset;
        }

        public DateTimeOffset Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            if (count != 2) throw new InvalidOperationException("Invalid DateTimeOffset format.");

            var utc = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
            offset += readSize;

            var dtOffsetTicks = MessagePackBinary.ReadInt64(bytes, offset, out readSize);
            offset += readSize;

            readSize = offset - startOffset;

            return new DateTimeOffset(utc, new TimeSpan(dtOffsetTicks));
        }
    }

    public class GuidFormatter : IMessagePackFormatter<Guid>
    {
        public int Serialize(ref byte[] bytes, int offset, Guid value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value.ToString());
        }

        public Guid Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return Guid.Parse(MessagePackBinary.ReadString(bytes, offset, out readSize));
        }
    }

    public class UriFormatter : IMessagePackFormatter<Uri>
    {
        public int Serialize(ref byte[] bytes, int offset, Uri value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteString(ref bytes, offset, value.ToString());
            }
        }

        public Uri Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return new Uri(MessagePackBinary.ReadString(bytes, offset, out readSize));
            }
        }
    }

    public class VersionFormatter : IMessagePackFormatter<Version>
    {
        public int Serialize(ref byte[] bytes, int offset, Version value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteString(ref bytes, offset, value.ToString());
            }
        }

        public Version Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return new Version(MessagePackBinary.ReadString(bytes, offset, out readSize));
            }
        }
    }

    public class KeyValuePairFormatter<TKey, TValue> : IMessagePackFormatter<KeyValuePair<TKey, TValue>>
    {
        public int Serialize(ref byte[] bytes, int offset, KeyValuePair<TKey, TValue> value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
            offset += formatterResolver.GetFormatterWithVerify<TKey>().Serialize(ref bytes, offset, value.Key, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<TValue>().Serialize(ref bytes, offset, value.Value, formatterResolver);
            return offset - startOffset;
        }

        public KeyValuePair<TKey, TValue> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            if (count != 2) throw new InvalidOperationException("Invalid KeyValuePair format.");

            var key = formatterResolver.GetFormatterWithVerify<TKey>().Deserialize(bytes, offset, formatterResolver, out readSize);
            offset += readSize;

            var value = formatterResolver.GetFormatterWithVerify<TValue>().Deserialize(bytes, offset, formatterResolver, out readSize);
            offset += readSize;

            readSize = offset - startOffset;
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    public class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        public int Serialize(ref byte[] bytes, int offset, System.Numerics.BigInteger value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBytes(ref bytes, offset, value.ToByteArray());
        }

        public System.Numerics.BigInteger Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return new System.Numerics.BigInteger(MessagePackBinary.ReadBytes(bytes, offset, out readSize));
        }
    }

    public class ComplexFormatter : IMessagePackFormatter<System.Numerics.Complex>
    {
        public int Serialize(ref byte[] bytes, int offset, System.Numerics.Complex value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteDouble(ref bytes, offset, value.Real);
            offset += MessagePackBinary.WriteDouble(ref bytes, offset, value.Imaginary);
            return offset - startOffset;
        }

        public System.Numerics.Complex Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            if (count != 2) throw new InvalidOperationException("Invalid Complex format.");

            var real = MessagePackBinary.ReadDouble(bytes, offset, out readSize);
            offset += readSize;

            var imaginary = MessagePackBinary.ReadDouble(bytes, offset, out readSize);
            offset += readSize;

            readSize = offset - startOffset;
            return new System.Numerics.Complex(real, imaginary);
        }
    }
}
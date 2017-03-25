using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if NETSTANDARD1_4
using System.Threading.Tasks;
#endif

namespace MessagePack.Formatters
{
    // NET40 -> BigInteger, Complex, Tuple

    // byte[] is special. represents bin type.
    public class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        ByteArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, byte[] value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBytes(ref bytes, offset, value);
        }

        public byte[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadBytes(bytes, offset, out readSize);
        }
    }

    public class NullableStringFormatter : IMessagePackFormatter<String>
    {
        public static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        NullableStringFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, String value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value);
        }

        public String Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadString(bytes, offset, out readSize);
        }
    }

    public class NullableStringArrayFormatter : IMessagePackFormatter<String[]>
    {
        public static readonly NullableStringArrayFormatter Instance = new NullableStringArrayFormatter();

        NullableStringArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, String[] value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteString(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public String[] Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                var array = new String[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = MessagePackBinary.ReadString(bytes, offset, out readSize);
                    offset += readSize;
                }
                readSize = offset - startOffset;
                return array;
            }
        }
    }

    public class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        public static readonly DecimalFormatter Instance = new DecimalFormatter();

        DecimalFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, decimal value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value.ToString(CultureInfo.InvariantCulture));
        }

        public decimal Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return decimal.Parse(MessagePackBinary.ReadString(bytes, offset, out readSize), CultureInfo.InvariantCulture);
        }
    }

    public class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        public static readonly IMessagePackFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        TimeSpanFormatter()
        {

        }

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
        public static readonly IMessagePackFormatter<DateTimeOffset> Instance = new DateTimeOffsetFormatter();

        DateTimeOffsetFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, DateTimeOffset value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
            offset += MessagePackBinary.WriteDateTime(ref bytes, offset, new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
            offset += MessagePackBinary.WriteInt16(ref bytes, offset, (short)value.Offset.TotalMinutes); // offset is normalized in minutes
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

            var dtOffsetMinutes = MessagePackBinary.ReadInt16(bytes, offset, out readSize);
            offset += readSize;

            readSize = offset - startOffset;

            return new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
        }
    }

    public class GuidFormatter : IMessagePackFormatter<Guid>
    {
        public static readonly IMessagePackFormatter<Guid> Instance = new GuidFormatter();


        GuidFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Guid value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value.ToString());
        }

        public Guid Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return new Guid(MessagePackBinary.ReadString(bytes, offset, out readSize));
        }
    }

    public class UriFormatter : IMessagePackFormatter<Uri>
    {
        public static readonly IMessagePackFormatter<Uri> Instance = new UriFormatter();


        UriFormatter()
        {

        }

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
        public static readonly IMessagePackFormatter<Version> Instance = new VersionFormatter();

        VersionFormatter()
        {

        }

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

    public class StringBuilderFormatter : IMessagePackFormatter<StringBuilder>
    {
        public static readonly IMessagePackFormatter<StringBuilder> Instance = new StringBuilderFormatter();

        StringBuilderFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, StringBuilder value, IFormatterResolver formatterResolver)
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

        public StringBuilder Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return new StringBuilder(MessagePackBinary.ReadString(bytes, offset, out readSize));
            }
        }
    }

    public class BitArrayFormatter : IMessagePackFormatter<BitArray>
    {
        public static readonly IMessagePackFormatter<BitArray> Instance = new BitArrayFormatter();

        BitArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, BitArray value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                var len = value.Length;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, len);
                for (int i = 0; i < len; i++)
                {
                    offset += MessagePackBinary.WriteBoolean(ref bytes, offset, value.Get(i));
                }

                return offset - startOffset;
            }
        }

        public BitArray Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;

                var array = new BitArray(len);
                for (int i = 0; i < len; i++)
                {
                    array[i] = MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
                    offset += readSize;
                }

                readSize = offset - startOffset;
                return array;
            }
        }
    }

#if NETSTANDARD1_4

    public class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        public static readonly IMessagePackFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        BigIntegerFormatter()
        {

        }

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
        public static readonly IMessagePackFormatter<System.Numerics.Complex> Instance = new ComplexFormatter();

        ComplexFormatter()
        {

        }

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

    public class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, Lazy<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);
            }
        }

        public Lazy<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                // deserialize immediately(no delay, because capture byte[] causes memory leak)
                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
                return new Lazy<T>(() => v);
            }
        }
    }

    public class TaskUnitFormatter : IMessagePackFormatter<Task>
    {
        public static readonly IMessagePackFormatter<Task> Instance = new TaskUnitFormatter();

        TaskUnitFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Task value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                value.Wait(); // wait...!
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
        }

        public Task Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (!MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("Invalid input");
            }
            else
            {
                readSize = 1;
                return Task.CompletedTask;
            }
        }
    }

    public class TaskValueFormatter<T> : IMessagePackFormatter<Task<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, Task<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                // value.Result -> wait...!
                return formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Result, formatterResolver);
            }
        }

        public Task<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
                return Task.FromResult(v);
            }
        }
    }

    public class ValueTaskFormatter<T> : IMessagePackFormatter<ValueTask<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ValueTask<T> value, IFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Result, formatterResolver);
        }

        public ValueTask<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
            return new ValueTask<T>(v);
        }
    }

#endif
}
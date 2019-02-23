using MessagePack.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if !UNITY
using System.Threading.Tasks;
#endif

namespace MessagePack.Formatters
{
    // NET40 -> BigInteger, Complex, Tuple

    // byte[] is special. represents bin type.
    public sealed class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        ByteArrayFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, byte[] value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBytes(ref bytes, offset, value);
        }

        public byte[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return reader.ReadBytes().ToArray();
            }
        }
    }

    public sealed class NullableStringFormatter : IMessagePackFormatter<String>
    {
        public static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        NullableStringFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, String value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value);
        }

        public String Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return reader.ReadString();
            }
        }
    }

    public sealed class NullableStringArrayFormatter : IMessagePackFormatter<String[]>
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

        public String[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new String[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadString();
                }
                return array;
            }
        }
    }

    public sealed class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        public static readonly DecimalFormatter Instance = new DecimalFormatter();

        DecimalFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, decimal value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteString(ref bytes, offset, value.ToString(CultureInfo.InvariantCulture));
        }

        public decimal Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
        }
    }

    public sealed class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        public static readonly IMessagePackFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        TimeSpanFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, TimeSpan value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt64(ref bytes, offset, value.Ticks);
        }

        public TimeSpan Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }

    public sealed class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
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

        public DateTimeOffset Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2) throw new InvalidOperationException("Invalid DateTimeOffset format.");

            var utc = reader.ReadDateTime();

            var dtOffsetMinutes = reader.ReadInt16();


            return new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
        }
    }

    public sealed class GuidFormatter : IMessagePackFormatter<Guid>
    {
        public static readonly IMessagePackFormatter<Guid> Instance = new GuidFormatter();

        GuidFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Guid value, IFormatterResolver formatterResolver)
        {
            MessagePackBinary.EnsureCapacity(ref bytes, offset, 38);

            bytes[offset] = MessagePackCode.Str8;
            bytes[offset + 1] = unchecked((byte)36);
            new GuidBits(ref value).Write(bytes.AsSpan(offset + 2));
            return 38;
        }

        public Guid Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var segment = reader.ReadStringSegment();
            if (segment.Length != 36)
            {
                throw new InvalidOperationException("Unexpected length of string.");
            }

            GuidBits result;
            if (segment.IsSingleSegment)
            {
                result = new GuidBits(segment.First.Span);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[36];
                segment.CopyTo(bytes);
                result = new GuidBits(bytes);
            }

            return result.Value;
        }
    }

    public sealed class UriFormatter : IMessagePackFormatter<Uri>
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

        public Uri Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Uri(reader.ReadString(), UriKind.RelativeOrAbsolute);
            }
        }
    }

    public sealed class VersionFormatter : IMessagePackFormatter<Version>
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

        public Version Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Version(reader.ReadString());
            }
        }
    }

    public sealed class KeyValuePairFormatter<TKey, TValue> : IMessagePackFormatter<KeyValuePair<TKey, TValue>>
    {
        public int Serialize(ref byte[] bytes, int offset, KeyValuePair<TKey, TValue> value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
            offset += formatterResolver.GetFormatterWithVerify<TKey>().Serialize(ref bytes, offset, value.Key, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<TValue>().Serialize(ref bytes, offset, value.Value, formatterResolver);
            return offset - startOffset;
        }

        public KeyValuePair<TKey, TValue> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2) throw new InvalidOperationException("Invalid KeyValuePair format.");

            var key = resolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, resolver);
            var value = resolver.GetFormatterWithVerify<TValue>().Deserialize(ref reader, resolver);
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    public sealed class StringBuilderFormatter : IMessagePackFormatter<StringBuilder>
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

        public StringBuilder Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new StringBuilder(reader.ReadString());
            }
        }
    }

    public sealed class BitArrayFormatter : IMessagePackFormatter<BitArray>
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

        public BitArray Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                var array = new BitArray(len);
                for (int i = 0; i < len; i++)
                {
                    array[i] = reader.ReadBoolean();
                }

                return array;
            }
        }
    }

#if !UNITY

    public sealed class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        public static readonly IMessagePackFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        BigIntegerFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, System.Numerics.BigInteger value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBytes(ref bytes, offset, value.ToByteArray());
        }

        public System.Numerics.BigInteger Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var bytes = reader.ReadBytes();
#if NETCOREAPP2_1
            if (bytes.IsSingleSegment)
            {
                return new System.Numerics.BigInteger(bytes.First.Span);
            }
            else
            {
                byte[] bytesArray = ArrayPool<byte>.Shared.Rent((int)bytes.Length);
                try
                {
                    bytes.CopyTo(bytesArray);
                    return new System.Numerics.BigInteger(bytesArray.AsSpan(0, (int)bytes.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytesArray);
                }
            }
#else
            return new System.Numerics.BigInteger(bytes.ToArray());
#endif
        }
    }

    public sealed class ComplexFormatter : IMessagePackFormatter<System.Numerics.Complex>
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

        public System.Numerics.Complex Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2) throw new InvalidOperationException("Invalid Complex format.");

            var real = reader.ReadDouble();

            var imaginary = reader.ReadDouble();

            return new System.Numerics.Complex(real, imaginary);
        }
    }

    public sealed class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>>
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

        public Lazy<T> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                // deserialize immediately(no delay, because capture byte[] causes memory leak)
                var v = resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
                return new Lazy<T>(() => v);
            }
        }
    }

#pragma warning disable VSTHRD002 // This section will be removed when https://github.com/AArnott/MessagePack-CSharp/issues/29 is fixed

    public sealed class TaskUnitFormatter : IMessagePackFormatter<Task>
    {
        public static readonly IMessagePackFormatter<Task> Instance = new TaskUnitFormatter();
        static readonly Task CompletedTask = Task.FromResult<object>(null);

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

        public Task Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return CompletedTask;
            }
            else
            {
                throw new InvalidOperationException("Invalid input");
            }
        }
    }

    public sealed class TaskValueFormatter<T> : IMessagePackFormatter<Task<T>>
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

        public Task<T> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var v = resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
                return Task.FromResult(v);
            }
        }
    }

    public sealed class ValueTaskFormatter<T> : IMessagePackFormatter<ValueTask<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ValueTask<T> value, IFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Result, formatterResolver);
        }

        public ValueTask<T> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            var v = resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
            return new ValueTask<T>(v);
        }
    }

#pragma warning restore VSTHRD002

#endif
}
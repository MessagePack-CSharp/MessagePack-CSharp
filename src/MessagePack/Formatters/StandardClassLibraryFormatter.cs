﻿using MessagePack.Internal;
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
    internal sealed class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        internal static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        ByteArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, byte[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value);
            }
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

    internal sealed class NullableStringFormatter : IMessagePackFormatter<String>
    {
        internal static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        NullableStringFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, String value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value);
            }
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

    internal sealed class NullableStringArrayFormatter : IMessagePackFormatter<String[]>
    {
        internal static readonly NullableStringArrayFormatter Instance = new NullableStringArrayFormatter();

        NullableStringArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, String[] value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        internal static readonly DecimalFormatter Instance = new DecimalFormatter();

        DecimalFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, decimal value, IFormatterResolver resolver)
        {
            writer.Write(value.ToString(CultureInfo.InvariantCulture));
            return;
        }

        public decimal Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
        }
    }

    internal sealed class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        internal static readonly IMessagePackFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        TimeSpanFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, TimeSpan value, IFormatterResolver resolver)
        {
            writer.Write(value.Ticks);
            return;
        }

        public TimeSpan Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }

    internal sealed class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        internal static readonly IMessagePackFormatter<DateTimeOffset> Instance = new DateTimeOffsetFormatter();

        DateTimeOffsetFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, IFormatterResolver resolver)
        {
            writer.WriteArrayHeader(2);
            writer.Write(new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
            writer.Write((short)value.Offset.TotalMinutes); // offset is normalized in minutes
            return;
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

    internal sealed class GuidFormatter : IMessagePackFormatter<Guid>
    {
        internal static readonly IMessagePackFormatter<Guid> Instance = new GuidFormatter();

        GuidFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, Guid value, IFormatterResolver resolver)
        {
            byte* pBytes = stackalloc byte[36];
            Span<byte> bytes = new Span<byte>(pBytes, 36);
            new GuidBits(ref value).Write(bytes);
            writer.WriteString(bytes);
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

    internal sealed class UriFormatter : IMessagePackFormatter<Uri>
    {
        internal static readonly IMessagePackFormatter<Uri> Instance = new UriFormatter();


        UriFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Uri value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
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

    internal sealed class VersionFormatter : IMessagePackFormatter<Version>
    {
        internal static readonly IMessagePackFormatter<Version> Instance = new VersionFormatter();

        VersionFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Version value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
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

    internal sealed class KeyValuePairFormatter<TKey, TValue> : IMessagePackFormatter<KeyValuePair<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, KeyValuePair<TKey, TValue> value, IFormatterResolver resolver)
        {
            writer.WriteArrayHeader(2);
            resolver.GetFormatterWithVerify<TKey>().Serialize(ref writer, value.Key, resolver);
            resolver.GetFormatterWithVerify<TValue>().Serialize(ref writer, value.Value, resolver);
            return;
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

    internal sealed class StringBuilderFormatter : IMessagePackFormatter<StringBuilder>
    {
        internal static readonly IMessagePackFormatter<StringBuilder> Instance = new StringBuilderFormatter();

        StringBuilderFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, StringBuilder value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
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

    internal sealed class BitArrayFormatter : IMessagePackFormatter<BitArray>
    {
        internal static readonly IMessagePackFormatter<BitArray> Instance = new BitArrayFormatter();

        BitArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, BitArray value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var len = value.Length;
                writer.WriteArrayHeader(len);
                for (int i = 0; i < len; i++)
                {
                    writer.Write(value.Get(i));
                }

                return;
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

    internal sealed class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        internal static readonly IMessagePackFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        BigIntegerFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.BigInteger value, IFormatterResolver resolver)
        {
            writer.Write(value.ToByteArray());
            return;
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

    internal sealed class ComplexFormatter : IMessagePackFormatter<System.Numerics.Complex>
    {
        internal static readonly IMessagePackFormatter<System.Numerics.Complex> Instance = new ComplexFormatter();

        ComplexFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.Complex value, IFormatterResolver resolver)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Real);
            writer.Write(value.Imaginary);
            return;
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

    internal sealed class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>>
    {
        public void Serialize(ref MessagePackWriter writer, Lazy<T> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, resolver);
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

    internal sealed class TaskUnitFormatter : IMessagePackFormatter<Task>
    {
        internal static readonly IMessagePackFormatter<Task> Instance = new TaskUnitFormatter();
        static readonly Task CompletedTask = Task.FromResult<object>(null);

        TaskUnitFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Task value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                value.Wait(); // wait...!
                writer.WriteNil();
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

    internal sealed class TaskValueFormatter<T> : IMessagePackFormatter<Task<T>>
    {
        public void Serialize(ref MessagePackWriter writer, Task<T> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                // value.Result -> wait...!
                resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Result, resolver);
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

    internal sealed class ValueTaskFormatter<T> : IMessagePackFormatter<ValueTask<T>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTask<T> value, IFormatterResolver resolver)
        {
            resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Result, resolver);
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

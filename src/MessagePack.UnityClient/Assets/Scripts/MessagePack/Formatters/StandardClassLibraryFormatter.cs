// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MessagePack.Internal;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    // NET40 -> BigInteger, Complex, Tuple

    // byte[] is special. represents bin type.
    public sealed class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        private ByteArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, byte[] value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public byte[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadBytes()?.ToArray();
        }
    }

    public sealed class NullableStringFormatter : IMessagePackFormatter<String>
    {
        public static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        private NullableStringFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString();
        }
    }

    public sealed class NullableStringArrayFormatter : IMessagePackFormatter<String[]>
    {
        public static readonly NullableStringArrayFormatter Instance = new NullableStringArrayFormatter();

        private NullableStringArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, String[] value, MessagePackSerializerOptions options)
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

        public String[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var len = reader.ReadArrayHeader();
            if (len == 0)
            {
                return Array.Empty<String>();
            }

            var array = new String[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadString();
            }

            return array;
        }
    }

    public sealed class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        public static readonly DecimalFormatter Instance = new DecimalFormatter();

        private DecimalFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, decimal value, MessagePackSerializerOptions options)
        {
            var dest = writer.GetSpan(MessagePackRange.MaxFixStringLength);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(value, dest.Slice(1), out var written))
            {
                // write header
                dest[0] = (byte)(MessagePackCode.MinFixStr | written);
                writer.Advance(written + 1);
            }
            else
            {
                // reset writer's span previously acquired that does not use
                writer.Advance(0);
                writer.Write(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public decimal Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!(reader.ReadStringSequence() is ReadOnlySequence<byte> sequence))
            {
                throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", MessagePackCode.Nil, MessagePackCode.ToFormatName(MessagePackCode.Nil)));
            }

            if (sequence.IsSingleSegment)
            {
                var span = sequence.First.Span;
                if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                {
                    if (span.Length != bytesConsumed)
                    {
                        throw new MessagePackSerializationException("Unexpected length of string.");
                    }

                    return result;
                }
            }
            else
            {
                // sequence.Length is not free
                var seqLen = (int)sequence.Length;
                if (seqLen < 128)
                {
                    Span<byte> span = stackalloc byte[seqLen];
                    sequence.CopyTo(span);
                    if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                    {
                        if (seqLen != bytesConsumed)
                        {
                            throw new MessagePackSerializationException("Unexpected length of string.");
                        }

                        return result;
                    }
                }
                else
                {
                    var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
                    try
                    {
                        sequence.CopyTo(rentArray);
                        if (System.Buffers.Text.Utf8Parser.TryParse(rentArray.AsSpan(0, seqLen), out decimal result, out var bytesConsumed))
                        {
                            if (seqLen != bytesConsumed)
                            {
                                throw new MessagePackSerializationException("Unexpected length of string.");
                            }

                            return result;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentArray);
                    }
                }
            }

            throw new MessagePackSerializationException("Can't parse to decimal, input string was not in a correct format.");
        }
    }

    public sealed class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        public static readonly IMessagePackFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        private TimeSpanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, TimeSpan value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Ticks);
            return;
        }

        public TimeSpan Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }

    public sealed class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        public static readonly IMessagePackFormatter<DateTimeOffset> Instance = new DateTimeOffsetFormatter();

        private DateTimeOffsetFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
            writer.Write((short)value.Offset.TotalMinutes); // offset is normalized in minutes
            return;
        }

        public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
            }

            DateTime utc = reader.ReadDateTime();

            var dtOffsetMinutes = reader.ReadInt16();

            return new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
        }
    }

    public sealed class GuidFormatter : IMessagePackFormatter<Guid>
    {
        public static readonly IMessagePackFormatter<Guid> Instance = new GuidFormatter();

        private GuidFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, Guid value, MessagePackSerializerOptions options)
        {
            byte* pBytes = stackalloc byte[36];
            Span<byte> bytes = new Span<byte>(pBytes, 36);
            new GuidBits(ref value).Write(bytes);
            writer.WriteString(bytes);
        }

        public Guid Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ReadOnlySequence<byte> segment = reader.ReadStringSequence().Value;
            if (segment.Length != 36)
            {
                throw new MessagePackSerializationException("Unexpected length of string.");
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

        private UriFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Uri value, MessagePackSerializerOptions options)
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

        public Uri Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

        private VersionFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Version value, MessagePackSerializerOptions options)
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

        public Version Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
        public void Serialize(ref MessagePackWriter writer, KeyValuePair<TKey, TValue> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<TKey>().Serialize(ref writer, value.Key, options);
            resolver.GetFormatterWithVerify<TValue>().Serialize(ref writer, value.Value, options);
            return;
        }

        public KeyValuePair<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid KeyValuePair format.");
            }

            IFormatterResolver resolver = options.Resolver;
            options.Security.DepthStep(ref reader);
            try
            {
                TKey key = resolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, options);
                TValue value = resolver.GetFormatterWithVerify<TValue>().Deserialize(ref reader, options);
                return new KeyValuePair<TKey, TValue>(key, value);
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public sealed class StringBuilderFormatter : IMessagePackFormatter<StringBuilder>
    {
        public static readonly IMessagePackFormatter<StringBuilder> Instance = new StringBuilderFormatter();

        private StringBuilderFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, StringBuilder value, MessagePackSerializerOptions options)
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

        public StringBuilder Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

        private BitArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, BitArray value, MessagePackSerializerOptions options)
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

        public BitArray Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

    public sealed class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        public static readonly IMessagePackFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        private BigIntegerFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.BigInteger value, MessagePackSerializerOptions options)
        {
#if NETCOREAPP2_1
            if (!writer.OldSpec)
            {
                // try to get bin8 buffer.
                var span = writer.GetSpan(byte.MaxValue);
                if (value.TryWriteBytes(span.Slice(2), out var written))
                {
                    span[0] = MessagePackCode.Bin8;
                    span[1] = (byte)written;

                    writer.Advance(written + 2);
                    return;
                }
                else
                {
                    // reset writer's span previously acquired that does not use
                    writer.Advance(0);
                }
            }
#endif

            writer.Write(value.ToByteArray());
            return;
        }

        public System.Numerics.BigInteger Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ReadOnlySequence<byte> bytes = reader.ReadBytes().Value;
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

        private ComplexFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.Complex value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Real);
            writer.Write(value.Imaginary);
            return;
        }

        public System.Numerics.Complex Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid Complex format.");
            }

            var real = reader.ReadDouble();

            var imaginary = reader.ReadDouble();

            return new System.Numerics.Complex(real, imaginary);
        }
    }

    public sealed class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>>
    {
        public void Serialize(ref MessagePackWriter writer, Lazy<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IFormatterResolver resolver = options.Resolver;
                resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public Lazy<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                options.Security.DepthStep(ref reader);
                try
                {
                    // deserialize immediately(no delay, because capture byte[] causes memory leak)
                    IFormatterResolver resolver = options.Resolver;
                    T v = resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
                    return new Lazy<T>(() => v);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    /// <summary>
    /// Serializes any instance of <see cref="Type"/> by its <see cref="Type.AssemblyQualifiedName"/> value.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> class itself or a derived type.</typeparam>
    public sealed class TypeFormatter<T> : IMessagePackFormatter<T>
        where T : Type
    {
        public static readonly IMessagePackFormatter<T> Instance = new TypeFormatter<T>();

        private TypeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.AssemblyQualifiedName);
            }
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            return (T)Type.GetType(reader.ReadString(), throwOnError: true);
        }
    }
}

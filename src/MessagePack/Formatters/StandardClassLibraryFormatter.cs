// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using MessagePack.Internal;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    // NET40 -> BigInteger, Complex, Tuple

    /// <summary>
    /// Serializes a <see cref="byte"/> array as a bin type.
    /// Deserializes a bin type or an array of byte-sized integers into a <see cref="byte"/> array.
    /// </summary>
    public sealed class ByteArrayFormatter : IMessagePackFormatter<byte[]?>
    {
        public static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        private ByteArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, byte[]? value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public byte[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.NextMessagePackType == MessagePackType.Array)
            {
                int len = reader.ReadArrayHeader();
                if (len == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] array = new byte[len];
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int i = 0; i < len; i++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        array[i] = reader.ReadByte();
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return array;
            }
            else
            {
                return reader.ReadBytes()?.ToArray();
            }
        }
    }

    public sealed class NullableStringFormatter : IMessagePackFormatter<string?>
    {
        public static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        private NullableStringFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, string? value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public string? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString();
        }
    }

    public sealed class NullableStringArrayFormatter : IMessagePackFormatter<string?[]?>
    {
        public static readonly NullableStringArrayFormatter Instance = new NullableStringArrayFormatter();

        private NullableStringArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, string?[]? value, MessagePackSerializerOptions options)
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

        public string?[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var len = reader.ReadArrayHeader();
            if (len == 0)
            {
                return Array.Empty<string?>();
            }

            var array = new string?[len];
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
            ReadOnlySequence<byte> segment = reader.ReadStringSequence() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<Guid>();
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

    public sealed class UriFormatter : IMessagePackFormatter<Uri?>
    {
        public static readonly IMessagePackFormatter<Uri?> Instance = new UriFormatter();

        private UriFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Uri? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.OriginalString);
            }
        }

        public Uri? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString() is string value
                ? new Uri(value, UriKind.RelativeOrAbsolute)
                : null;
        }
    }

    public sealed class VersionFormatter : IMessagePackFormatter<Version?>
    {
        public static readonly IMessagePackFormatter<Version?> Instance = new VersionFormatter();

        private VersionFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Version? value, MessagePackSerializerOptions options)
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

        public Version? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString() is string value
                ? new Version(value)
                : null;
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

    public sealed class StringBuilderFormatter : IMessagePackFormatter<StringBuilder?>
    {
        public static readonly IMessagePackFormatter<StringBuilder?> Instance = new StringBuilderFormatter();

        private StringBuilderFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, StringBuilder? value, MessagePackSerializerOptions options)
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

        public StringBuilder? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

    public sealed class BitArrayFormatter : IMessagePackFormatter<BitArray?>
    {
        public static readonly IMessagePackFormatter<BitArray?> Instance = new BitArrayFormatter();

        private BitArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, BitArray? value, MessagePackSerializerOptions options)
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

        public BitArray? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

    public sealed class BigIntegerFormatter : IMessagePackFormatter<BigInteger>
    {
        public static readonly IMessagePackFormatter<BigInteger> Instance = new BigIntegerFormatter();

        private BigIntegerFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, BigInteger value, MessagePackSerializerOptions options)
        {
#if NETCOREAPP
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

        public BigInteger Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ReadOnlySequence<byte> bytes = reader.ReadBytes() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<BigInteger>();
#if NETCOREAPP
            if (bytes.IsSingleSegment)
            {
                return new BigInteger(bytes.First.Span);
            }
            else
            {
                byte[] bytesArray = ArrayPool<byte>.Shared.Rent((int)bytes.Length);
                try
                {
                    bytes.CopyTo(bytesArray);
                    return new BigInteger(bytesArray.AsSpan(0, (int)bytes.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytesArray);
                }
            }
#else
            return new BigInteger(bytes.ToArray());
#endif
        }
    }

    public sealed class ComplexFormatter : IMessagePackFormatter<Complex>
    {
        public static readonly IMessagePackFormatter<Complex> Instance = new ComplexFormatter();

        private ComplexFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Complex value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Real);
            writer.Write(value.Imaginary);
            return;
        }

        public Complex Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid Complex format.");
            }

            var real = reader.ReadDouble();

            var imaginary = reader.ReadDouble();

            return new Complex(real, imaginary);
        }
    }

    public sealed class Vector2Formatter : IMessagePackFormatter<Vector2>
    {
        public static readonly IMessagePackFormatter<Vector2> Instance = new Vector2Formatter();

        private Vector2Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Vector2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public Vector2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 2)
            {
                throw new MessagePackSerializationException("Invalid Vector2 data.");
            }

            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class Vector3Formatter : IMessagePackFormatter<Vector3>
    {
        public static readonly IMessagePackFormatter<Vector3> Instance = new Vector3Formatter();

        private Vector3Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 3)
            {
                throw new MessagePackSerializationException("Invalid Vector3 data.");
            }

            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class Vector4Formatter : IMessagePackFormatter<Vector4>
    {
        public static readonly IMessagePackFormatter<Vector4> Instance = new Vector4Formatter();

        private Vector4Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Vector4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Vector4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 4)
            {
                throw new MessagePackSerializationException("Invalid Vector4 data.");
            }

            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class QuaternionFormatter : IMessagePackFormatter<Quaternion>
    {
        public static readonly IMessagePackFormatter<Quaternion> Instance = new QuaternionFormatter();

        private QuaternionFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Quaternion value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 4)
            {
                throw new MessagePackSerializationException("Invalid Quaternion data.");
            }

            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class Matrix3x2Formatter : IMessagePackFormatter<Matrix3x2>
    {
        public static readonly IMessagePackFormatter<Matrix3x2> Instance = new Matrix3x2Formatter();

        private Matrix3x2Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Matrix3x2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(6);
            writer.Write(value.M11);
            writer.Write(value.M12);
            writer.Write(value.M21);
            writer.Write(value.M22);
            writer.Write(value.M31);
            writer.Write(value.M32);
        }

        public Matrix3x2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 6)
            {
                throw new MessagePackSerializationException("Invalid Matrix3x2 data.");
            }

            return new Matrix3x2(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class Matrix4x4Formatter : IMessagePackFormatter<Matrix4x4>
    {
        public static readonly IMessagePackFormatter<Matrix4x4> Instance = new Matrix4x4Formatter();

        private Matrix4x4Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Matrix4x4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(16);
            writer.Write(value.M11);
            writer.Write(value.M12);
            writer.Write(value.M13);
            writer.Write(value.M14);
            writer.Write(value.M21);
            writer.Write(value.M22);
            writer.Write(value.M23);
            writer.Write(value.M24);
            writer.Write(value.M31);
            writer.Write(value.M32);
            writer.Write(value.M33);
            writer.Write(value.M34);
            writer.Write(value.M41);
            writer.Write(value.M42);
            writer.Write(value.M43);
            writer.Write(value.M44);
        }

        public Matrix4x4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.ReadArrayHeader() != 16)
            {
                throw new MessagePackSerializationException("Invalid Matrix4x4 data.");
            }

            return new Matrix4x4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public sealed class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>?>
    {
        public void Serialize(ref MessagePackWriter writer, Lazy<T>? value, MessagePackSerializerOptions options)
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

        public Lazy<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
    public sealed class TypeFormatter<T> : IMessagePackFormatter<T?>
        where T : Type
    {
        public static readonly IMessagePackFormatter<T?> Instance = new TypeFormatter<T>();

        private TypeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
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

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString() is string value
                ? (T?)Type.GetType(value, throwOnError: true)
                : null;
        }
    }

#if NET5_0_OR_GREATER

    public sealed class HalfFormatter : IMessagePackFormatter<Half>
    {
        public static readonly IMessagePackFormatter<Half> Instance = new HalfFormatter();

        private HalfFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Half value, MessagePackSerializerOptions options)
        {
            writer.Write((float)value);
        }

        public Half Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return (Half)reader.ReadSingle();
        }
    }

#endif

}

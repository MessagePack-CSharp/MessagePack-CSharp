// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MessagePack.Internal;

#if HARDWARE_INTRINSICS_X86
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

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

    public sealed class SByteArrayFormatter : IMessagePackFormatter<sbyte[]>
    {
        public static readonly SByteArrayFormatter Instance = new SByteArrayFormatter();

        private SByteArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, sbyte[] value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var inputLength = value.Length;
            writer.WriteArrayHeader(inputLength);
            if (inputLength == 0)
            {
                return;
            }

            fixed (sbyte* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;

#if HARDWARE_INTRINSICS_X86
                const int ShiftCount = 4;
                const int Stride = 1 << ShiftCount;
                // We enter the SIMD mode when there are more than the Stride after alignment adjustment.
                if (Popcnt.IsSupported && inputLength >= Stride * 2)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(inputIterator);
                        inputLength -= offset;
                        var offsetEnd = inputIterator + offset;
                        while (inputIterator != offsetEnd)
                        {
                            writer.Write(*inputIterator++);
                        }
                    }

                    var table = IntegerArrayFormatterHelper.SByteShuffleTable;
                    var maskTable = IntegerArrayFormatterHelper.StoreMaskTable;
                    fixed (byte* tablePointer = &table[0])
                    fixed (byte* maskTablePointer = &maskTable[0])
                    {
                        var vectorMinFixNegInt = Vector128.Create((sbyte)MessagePackRange.MinFixNegativeInt);
                        var vectorMessagePackCodeInt8 = Vector128.Create(MessagePackCode.Int8);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Sse2.LoadVector128(inputIterator);
                            var index = unchecked((uint)Sse2.MoveMask(Sse2.CompareGreaterThan(vectorMinFixNegInt, current)));

                            if (index == 0)
                            {
                                // When all 32 input values are in the FixNum range.
                                var span = writer.GetSpan(Stride);
                                Sse2.Store((sbyte*)Unsafe.AsPointer(ref span[0]), current);

                                writer.Advance(Stride);
                                continue;
                            }

                            unchecked
                            {
                                var index0 = (byte)index;
                                var index1 = (byte)(index >> 8);
                                var count0 = (int)(Popcnt.PopCount(index0) + 8);
                                var count1 = (int)(Popcnt.PopCount(index1) + 8);
                                var countTotal = count0 + count1;
                                var destination = writer.GetSpan(countTotal);
                                fixed (byte* pDestination = &destination[0])
                                {
                                    var tempDestination = pDestination;
                                    var shuffle0 = Sse2.LoadVector128(tablePointer + (index0 << 4));
                                    var shuffled0 = Ssse3.Shuffle(current.AsByte(), shuffle0);
                                    var answer0 = Sse41.BlendVariable(shuffled0, vectorMessagePackCodeInt8, shuffle0);
                                    Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), tempDestination);
                                    tempDestination += count0;

                                    var shuffle1 = Sse2.LoadVector128(tablePointer + (index1 << 4));
                                    var shift1 = Sse2.ShiftRightLogical128BitLane(current.AsByte(), 8);
                                    var shuffled1 = Ssse3.Shuffle(shift1, shuffle1);
                                    var answer1 = Sse41.BlendVariable(shuffled1, vectorMessagePackCodeInt8, shuffle1);
                                    Sse2.MaskMove(answer1, Sse2.LoadVector128(maskTablePointer + (count1 << 4)), tempDestination);
                                }

                                writer.Advance(countTotal);
                            }
                        }
                    }
                }
#endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public sbyte[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<sbyte>();
                }

                var array = new sbyte[len];
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadSByte();
                }

                return array;
            }
        }
    }

    public sealed class Int16ArrayFormatter : IMessagePackFormatter<short[]>
    {
        public static readonly Int16ArrayFormatter Instance = new Int16ArrayFormatter();

        private Int16ArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, short[] value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var inputLength = value.Length;
            writer.WriteArrayHeader(inputLength);
            if (inputLength == 0)
            {
                return;
            }

            fixed (short* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;

#if HARDWARE_INTRINSICS_X86
                const int ShiftCount = 3;
                const int Stride = 1 << ShiftCount;
                if (Sse41.IsSupported && inputLength >= Stride * 2)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(inputIterator);
                        if ((offset & 1) == 0)
                        {
                            offset >>= 1;
                            inputLength -= offset;
                            var offsetEnd = inputIterator + offset;
                            while (inputIterator != offsetEnd)
                            {
                                writer.Write(*inputIterator++);
                            }
                        }
                    }

                    var maskTable = IntegerArrayFormatterHelper.StoreMaskTable;
                    var table = IntegerArrayFormatterHelper.Int16ShuffleTable;
                    fixed (byte* maskTablePointer = &maskTable[0])
                    fixed (byte* tablePointer = &table[0])
                    {
                        var countPointer = (int*)(tablePointer + IntegerArrayFormatterHelper.Int16CountTableOffset);
                        var vectorSByteMinNeg1 = Vector128.Create((short)(sbyte.MinValue - 1));
                        var vectorMinFixNeg1 = Vector128.Create((short)(MessagePackRange.MinFixNegativeInt - 1));
                        var vectorSByteMax = Vector128.Create((short)sbyte.MaxValue);
                        var vectorByteMaxValue = Vector128.Create((short)byte.MaxValue);
                        var vectorM1M5M25M125 = Vector128.Create(-1, -5, -25, -125, -1, -5, -25, -125);
                        var vector02468101214 = Vector128.Create(0, 2, 4, 6, 8, 10, 12, 14, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Sse2.LoadVector128(inputIterator);

                            var isGreaterThanMinFixNeg1 = Sse2.CompareGreaterThan(current, vectorMinFixNeg1);
                            var isGreaterThanSByteMax = Sse2.CompareGreaterThan(current, vectorSByteMax);

                            if (Sse2.MoveMask(Sse2.AndNot(isGreaterThanSByteMax, isGreaterThanMinFixNeg1).AsByte()) == 0xFFFF)
                            {
                                var span = writer.GetSpan(Stride);
                                var answer = Ssse3.Shuffle(current.AsByte(), vector02468101214).AsUInt64();
                                Unsafe.As<byte, ulong>(ref span[0]) = answer.GetElement(0);
                                writer.Advance(Stride);
                                continue;
                            }

                            var countVector = Sse2.Add(isGreaterThanSByteMax, isGreaterThanMinFixNeg1);
                            var isGreaterThanSByteMinNeg1 = Sse2.CompareGreaterThan(current, vectorSByteMinNeg1);
                            countVector = Sse2.Add(countVector, isGreaterThanSByteMinNeg1);
                            var isGreaterThanByteMax = Sse2.CompareGreaterThan(current, vectorByteMaxValue);
                            countVector = Sse2.Add(countVector, isGreaterThanByteMax);

                            var indexVector = Sse2.MultiplyAddAdjacent(countVector, vectorM1M5M25M125);
                            indexVector = Ssse3.HorizontalAdd(indexVector, indexVector);

                            var index0 = indexVector.GetElement(0);
                            var index1 = indexVector.GetElement(1);
                            var count0 = countPointer[index0];
                            var count1 = countPointer[index1];
                            var countTotal = count0 + count1;
                            var destination = writer.GetSpan(countTotal);
                            fixed (byte* pDestination = &destination[0])
                            {
                                var tmpDestination = pDestination;

                                var item0 = tablePointer + (index0 << 5);
                                var shuffle0 = Sse2.LoadVector128(item0);
                                var shuffled0 = Ssse3.Shuffle(current.AsByte(), shuffle0);
                                var constant0 = Sse2.LoadVector128(item0 + 16);
                                var answer0 = Sse2.Or(shuffled0, constant0);
                                Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), tmpDestination);
                                tmpDestination += count0;

                                var item1 = tablePointer + (index1 << 5);
                                var shuffle1 = Sse2.LoadVector128(item1);
                                current = Sse2.ShiftRightLogical128BitLane(current, 8);
                                var shuffled1 = Ssse3.Shuffle(current.AsByte(), shuffle1);
                                var constant1 = Sse2.LoadVector128(item1 + 16);
                                var answer1 = Sse2.Or(shuffled1, constant1);
                                Sse2.MaskMove(answer1, Sse2.LoadVector128(maskTablePointer + (count1 << 4)), tmpDestination);
                            }

                            writer.Advance(countTotal);
                        }
                    }
                }
#endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public short[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<short>();
                }

                var array = new short[len];
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt16();
                }

                return array;
            }
        }
    }

    public sealed class Int32ArrayFormatter : IMessagePackFormatter<int[]>
    {
        public static readonly Int32ArrayFormatter Instance = new Int32ArrayFormatter();

        private Int32ArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, int[] value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var inputLength = value.Length;
            writer.WriteArrayHeader(inputLength);
            if (inputLength == 0)
            {
                return;
            }

            fixed (int* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;

#if HARDWARE_INTRINSICS_X86
                // Inline everything for the sake of the optimization.
                const int ShiftCount = 2;
                const int Stride = 1 << ShiftCount;
                if (Sse41.IsSupported && inputLength >= Stride * 2)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign32(inputIterator);
                        if ((offset & 3) == 0)
                        {
                            offset >>= 2;
                            inputLength -= offset;
                            var offsetEnd = inputIterator + offset;
                            while (inputIterator != offsetEnd)
                            {
                                writer.Write(*inputIterator++);
                            }
                        }
                    }

                    var maskTable = IntegerArrayFormatterHelper.StoreMaskTable;
                    var tableInt32 = IntegerArrayFormatterHelper.Int32ShuffleTable;
                    fixed (byte* tablePointer = &tableInt32[0])
                    fixed (byte* maskTablePointer = &maskTable[0])
                    {
                        var countPointer = (int*)(tablePointer + IntegerArrayFormatterHelper.Int32CountTableOffset);
                        var vectorShortMinValueM1 = Vector128.Create(short.MinValue - 1);
                        var vectorSByteMinValueM1 = Vector128.Create(sbyte.MinValue - 1);
                        var vectorMinFixNegIntM1 = Vector128.Create(MessagePackRange.MinFixNegativeInt - 1);
                        var vectorSByteMaxValue = Vector128.Create((int)sbyte.MaxValue);
                        var vectorByteMaxValue = Vector128.Create((int)byte.MaxValue);
                        var vectorUShortMaxValue = Vector128.Create((int)ushort.MaxValue);
                        var vectorM1M7 = Vector128.Create(-1, -7, -1, -7);
                        var vectorIn1Range = Vector128.Create(0, 4, 8, 12, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Sse2.LoadVector128(inputIterator);
                            var isGreaterThanMinFixNegIntM1 = Sse2.CompareGreaterThan(current, vectorMinFixNegIntM1);
                            var isGreaterThanSByteMaxValue = Sse2.CompareGreaterThan(current, vectorSByteMaxValue);

                            if (Sse2.MoveMask(Sse2.AndNot(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1).AsByte()) == 0xFFFF)
                            {
                                var answer = Ssse3.Shuffle(current.AsByte(), vectorIn1Range).AsUInt32();
                                var span = writer.GetSpan(Stride);
                                Unsafe.As<byte, uint>(ref span[0]) = answer.GetElement(0);
                                writer.Advance(Stride);
                                continue;
                            }

                            var indexVector = Sse2.Add(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1);
                            indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorUShortMaxValue));
                            indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorByteMaxValue));
                            indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorShortMinValueM1));
                            indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorSByteMinValueM1));
                            indexVector = Sse41.MultiplyLow(indexVector, vectorM1M7);
                            indexVector = Ssse3.HorizontalAdd(indexVector, indexVector);

                            var index0 = indexVector.GetElement(0);
                            var index1 = indexVector.GetElement(1);

                            var count0 = countPointer[index0];
                            var count1 = countPointer[index1];
                            var countTotal = count0 + count1;

                            var destination = writer.GetSpan(countTotal);
                            fixed (byte* pDestination = &destination[0])
                            {
                                var tmpDestination = pDestination;

                                var item0 = tablePointer + (index0 << 5);
                                var shuffle0 = Sse2.LoadVector128(item0);
                                var shuffled0 = Ssse3.Shuffle(current.AsByte(), shuffle0);
                                var constant0 = Sse2.LoadVector128(item0 + 16);
                                var answer0 = Sse2.Or(shuffled0, constant0);
                                Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), pDestination);
                                tmpDestination += count0;

                                var shift1 = Sse2.ShiftRightLogical128BitLane(current, 8).AsByte();
                                var item1 = tablePointer + (index1 << 5);
                                var shuffle1 = Sse2.LoadVector128(item1);
                                var shuffled1 = Ssse3.Shuffle(shift1, shuffle1);
                                var constant1 = Sse2.LoadVector128(item1 + 16);
                                var answer1 = Sse2.Or(shuffled1, constant1);
                                Sse2.MaskMove(answer1, Sse2.LoadVector128(maskTablePointer + (count1 << 4)), tmpDestination);
                            }

                            writer.Advance(countTotal);
                        }
                    }
                }
#endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public int[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<int>();
                }

                var array = new int[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt32();
                }

                return array;
            }
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
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace MessagePack.Resolvers
{
    public sealed class BuiltinResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly BuiltinResolver Instance = new BuiltinResolver();

        private BuiltinResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                // Reduce IL2CPP code generate size(don't write long code in <T>)
                Formatter = (IMessagePackFormatter<T>?)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class BuiltinResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // Primitive
            { typeof(Int16), Int16Formatter.Instance },
            { typeof(Int32), Int32Formatter.Instance },
            { typeof(Int64), Int64Formatter.Instance },
            { typeof(UInt16), UInt16Formatter.Instance },
            { typeof(UInt32), UInt32Formatter.Instance },
            { typeof(UInt64), UInt64Formatter.Instance },
            { typeof(Single), SingleFormatter.Instance },
            { typeof(Double), DoubleFormatter.Instance },
            { typeof(bool), BooleanFormatter.Instance },
            { typeof(byte), ByteFormatter.Instance },
            { typeof(sbyte), SByteFormatter.Instance },
            { typeof(DateTime), DateTimeFormatter.Instance },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), DateOnlyFormatter.Instance },
            { typeof(TimeOnly), TimeOnlyFormatter.Instance },
#endif
            { typeof(char), CharFormatter.Instance },

            // Nullable Primitive
            { typeof(Int16?), NullableInt16Formatter.Instance },
            { typeof(Int32?), NullableInt32Formatter.Instance },
            { typeof(Int64?), NullableInt64Formatter.Instance },
            { typeof(UInt16?), NullableUInt16Formatter.Instance },
            { typeof(UInt32?), NullableUInt32Formatter.Instance },
            { typeof(UInt64?), NullableUInt64Formatter.Instance },
            { typeof(Single?), NullableSingleFormatter.Instance },
            { typeof(Double?), NullableDoubleFormatter.Instance },
            { typeof(bool?), NullableBooleanFormatter.Instance },
            { typeof(byte?), NullableByteFormatter.Instance },
            { typeof(sbyte?), NullableSByteFormatter.Instance },
            { typeof(DateTime?), NullableDateTimeFormatter.Instance },
            { typeof(char?), NullableCharFormatter.Instance },

            // StandardClassLibraryFormatter
            { typeof(string), NullableStringFormatter.Instance },
            { typeof(decimal), DecimalFormatter.Instance },
            { typeof(decimal?), new StaticNullableFormatter<decimal>(DecimalFormatter.Instance) },
            { typeof(TimeSpan), TimeSpanFormatter.Instance },
            { typeof(TimeSpan?), new StaticNullableFormatter<TimeSpan>(TimeSpanFormatter.Instance) },
            { typeof(DateTimeOffset), DateTimeOffsetFormatter.Instance },
            { typeof(DateTimeOffset?), new StaticNullableFormatter<DateTimeOffset>(DateTimeOffsetFormatter.Instance) },
            { typeof(Guid), GuidFormatter.Instance },
            { typeof(Guid?), new StaticNullableFormatter<Guid>(GuidFormatter.Instance) },
            { typeof(Uri), UriFormatter.Instance },
            { typeof(Version), VersionFormatter.Instance },
            { typeof(StringBuilder), StringBuilderFormatter.Instance },
            { typeof(BitArray), BitArrayFormatter.Instance },
            { typeof(Type), TypeFormatter<Type>.Instance },

            // special primitive
            { typeof(byte[]), ByteArrayFormatter.Instance },

            // Nil
            { typeof(Nil), NilFormatter.Instance },
            { typeof(Nil?), NullableNilFormatter.Instance },

            // optimized primitive array formatter
            { typeof(Int16[]), Int16ArrayFormatter.Instance },
            { typeof(Int32[]), Int32ArrayFormatter.Instance },
            { typeof(Int64[]), Int64ArrayFormatter.Instance },
            { typeof(UInt16[]), UInt16ArrayFormatter.Instance },
            { typeof(UInt32[]), UInt32ArrayFormatter.Instance },
            { typeof(UInt64[]), UInt64ArrayFormatter.Instance },
            { typeof(Single[]), SingleArrayFormatter.Instance },
            { typeof(Double[]), DoubleArrayFormatter.Instance },
            { typeof(Boolean[]), BooleanArrayFormatter.Instance },
            { typeof(SByte[]), SByteArrayFormatter.Instance },
            { typeof(DateTime[]), DateTimeArrayFormatter.Instance },
            { typeof(Char[]), CharArrayFormatter.Instance },
            { typeof(string[]), NullableStringArrayFormatter.Instance },

            // well known collections
            { typeof(List<Int16>), new ListFormatter<Int16>() },
            { typeof(List<Int32>), new ListFormatter<Int32>() },
            { typeof(List<Int64>), new ListFormatter<Int64>() },
            { typeof(List<UInt16>), new ListFormatter<UInt16>() },
            { typeof(List<UInt32>), new ListFormatter<UInt32>() },
            { typeof(List<UInt64>), new ListFormatter<UInt64>() },
            { typeof(List<Single>), new ListFormatter<Single>() },
            { typeof(List<Double>), new ListFormatter<Double>() },
            { typeof(List<Boolean>), new ListFormatter<Boolean>() },
            { typeof(List<byte>), new ListFormatter<byte>() },
            { typeof(List<SByte>), new ListFormatter<SByte>() },
            { typeof(List<DateTime>), new ListFormatter<DateTime>() },
            { typeof(List<Char>), new ListFormatter<Char>() },
            { typeof(List<string>), new ListFormatter<string>() },

            { typeof(object[]), new ArrayFormatter<object>() },
            { typeof(List<object>), new ListFormatter<object>() },

            { typeof(Memory<byte>), ByteMemoryFormatter.Instance },
            { typeof(Memory<byte>?), new StaticNullableFormatter<Memory<byte>>(ByteMemoryFormatter.Instance) },
            { typeof(ReadOnlyMemory<byte>), ByteReadOnlyMemoryFormatter.Instance },
            { typeof(ReadOnlyMemory<byte>?), new StaticNullableFormatter<ReadOnlyMemory<byte>>(ByteReadOnlyMemoryFormatter.Instance) },
            { typeof(ReadOnlySequence<byte>), ByteReadOnlySequenceFormatter.Instance },
            { typeof(ReadOnlySequence<byte>?), new StaticNullableFormatter<ReadOnlySequence<byte>>(ByteReadOnlySequenceFormatter.Instance) },
            { typeof(ArraySegment<byte>), ByteArraySegmentFormatter.Instance },
            { typeof(ArraySegment<byte>?), new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Instance) },

            { typeof(System.Numerics.BigInteger), BigIntegerFormatter.Instance },
            { typeof(System.Numerics.BigInteger?), new StaticNullableFormatter<System.Numerics.BigInteger>(BigIntegerFormatter.Instance) },
            { typeof(System.Numerics.Complex), ComplexFormatter.Instance },
            { typeof(System.Numerics.Complex?), new StaticNullableFormatter<System.Numerics.Complex>(ComplexFormatter.Instance) },

#if NET5_0_OR_GREATER
            { typeof(System.Half), HalfFormatter.Instance },
#endif
        };

        internal static object? GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out object? formatter))
            {
                return formatter;
            }

            if (typeof(Type).IsAssignableFrom(t))
            {
                return typeof(TypeFormatter<>).MakeGenericType(t).GetField(nameof(TypeFormatter<Type>.Instance))!.GetValue(null);
            }

            return null;
        }
    }
}

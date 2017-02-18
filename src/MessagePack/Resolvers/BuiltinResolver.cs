using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MessagePack.Resolvers
{
    public class BuiltinResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new BuiltinResolver();

        BuiltinResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class BuiltinResolverGetFormatterHelper
    {
        // Reduce IL2CPP code generate size(don't write long code in <T>)
        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            // Primitive
            if (t == typeof(Int16))
            {
                return Int16Formatter.Instance;
            }
            else if (t == typeof(Int32))
            {
                return Int32Formatter.Instance;
            }
            else if (t == typeof(Int64))
            {
                return Int64Formatter.Instance;
            }
            else if (t == typeof(UInt16))
            {
                return UInt16Formatter.Instance;
            }
            else if (t == typeof(UInt32))
            {
                return UInt32Formatter.Instance;
            }
            else if (t == typeof(UInt64))
            {
                return UInt64Formatter.Instance;
            }
            else if (t == typeof(Single))
            {
                return SingleFormatter.Instance;
            }
            else if (t == typeof(Double))
            {
                return DoubleFormatter.Instance;
            }
            else if (t == typeof(bool))
            {
                return BooleanFormatter.Instance;
            }
            else if (t == typeof(byte))
            {
                return ByteFormatter.Instance;
            }
            else if (t == typeof(sbyte))
            {
                return SByteFormatter.Instance;
            }
            else if (t == typeof(DateTime))
            {
                return DateTimeFormatter.Instance;
            }
            else if (t == typeof(char))
            {
                return CharFormatter.Instance;
            }
            // Nulllable Primitive
            else if (t == typeof(Nullable<Int16>))
            {
                return NullableInt16Formatter.Instance;
            }
            else if (t == typeof(Nullable<Int32>))
            {
                return NullableInt32Formatter.Instance;
            }
            else if (t == typeof(Nullable<Int64>))
            {
                return NullableInt64Formatter.Instance;
            }
            else if (t == typeof(Nullable<UInt16>))
            {
                return NullableUInt16Formatter.Instance;
            }
            else if (t == typeof(Nullable<UInt32>))
            {
                return NullableUInt32Formatter.Instance;
            }
            else if (t == typeof(Nullable<UInt64>))
            {
                return NullableUInt64Formatter.Instance;
            }
            else if (t == typeof(Nullable<Single>))
            {
                return NullableSingleFormatter.Instance;
            }
            else if (t == typeof(Nullable<Double>))
            {
                return NullableDoubleFormatter.Instance;
            }
            else if (t == typeof(Nullable<bool>))
            {
                return NullableBooleanFormatter.Instance;
            }
            else if (t == typeof(Nullable<byte>))
            {
                return NullableByteFormatter.Instance;
            }
            else if (t == typeof(Nullable<sbyte>))
            {
                return NullableSByteFormatter.Instance;
            }
            else if (t == typeof(Nullable<DateTime>))
            {
                return NullableDateTimeFormatter.Instance;
            }
            else if (t == typeof(Nullable<char>))
            {
                return NullableCharFormatter.Instance;
            }

            // StandardClassLibraryFormatter
            else if (t == typeof(string))
            {
                return NullableStringFormatter.Instance;
            }
            else if (t == typeof(decimal))
            {
                return DecimalFormatter.Instance;
            }
            else if (t == typeof(decimal?))
            {
                return new StaticNullableFormatter<decimal>(DecimalFormatter.Instance);
            }
            else if (t == typeof(TimeSpan))
            {
                return TimeSpanFormatter.Instance;
            }
            else if (t == typeof(TimeSpan?))
            {
                return new StaticNullableFormatter<TimeSpan>(TimeSpanFormatter.Instance);
            }
            else if (t == typeof(DateTimeOffset))
            {
                return DateTimeOffsetFormatter.Instance;
            }
            else if (t == typeof(DateTimeOffset?))
            {
                return new StaticNullableFormatter<DateTimeOffset>(DateTimeOffsetFormatter.Instance);
            }
            else if (t == typeof(Guid))
            {
                return GuidFormatter.Instance;
            }
            else if (t == typeof(Guid?))
            {
                return new StaticNullableFormatter<Guid>(GuidFormatter.Instance);
            }
            else if (t == typeof(Uri))
            {
                return UriFormatter.Instance;
            }
            else if (t == typeof(Version))
            {
                return VersionFormatter.Instance;
            }

            // Nil
            else if (t == typeof(Nil))
            {
                return NilFormatter.Instance;
            }
            else if (t == typeof(Nil?))
            {
                return NullableNilFormatter.Instance;
            }

            // NET40
            else if (t == typeof(System.Numerics.BigInteger))
            {
                return BigIntegerFormatter.Instance;
            }
            else if (t == typeof(System.Numerics.BigInteger?))
            {
                return new StaticNullableFormatter<System.Numerics.BigInteger>(BigIntegerFormatter.Instance);
            }
            else if (t == typeof(System.Numerics.Complex))
            {
                return ComplexFormatter.Instance;
            }
            else if (t == typeof(System.Numerics.Complex?))
            {
                return new StaticNullableFormatter<System.Numerics.Complex>(ComplexFormatter.Instance);
            }
            else if (t == typeof(byte[]))
            {
                return ByteArrayFormatter.Instance;
            }

            return null;
        }
    }
}

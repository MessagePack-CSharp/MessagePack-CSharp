using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
                // Reduce IL2CPP code generate size(don't write long code in <T>)
                formatter = (IMessagePackFormatter<T>)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class BuiltinResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
        {
            // Primitive
            {typeof(Int16), Int16Formatter.Instance},
            {typeof(Int32), Int32Formatter.Instance},
            {typeof(Int64), Int64Formatter.Instance},
            {typeof(UInt16), UInt16Formatter.Instance},
            {typeof(UInt32), UInt32Formatter.Instance},
            {typeof(UInt64), UInt64Formatter.Instance},
            {typeof(Single), SingleFormatter.Instance},
            {typeof(Double), DoubleFormatter.Instance},
            {typeof(bool), BooleanFormatter.Instance},
            {typeof(byte), ByteFormatter.Instance},
            {typeof(sbyte), SByteFormatter.Instance},
            {typeof(DateTime), DateTimeFormatter.Instance},
            {typeof(char), CharFormatter.Instance},
            
            // Nulllable Primitive
            {typeof(Nullable<Int16>), NullableInt16Formatter.Instance},
            {typeof(Nullable<Int32>), NullableInt32Formatter.Instance},
            {typeof(Nullable<Int64>), NullableInt64Formatter.Instance},
            {typeof(Nullable<UInt16>), NullableUInt16Formatter.Instance},
            {typeof(Nullable<UInt32>), NullableUInt32Formatter.Instance},
            {typeof(Nullable<UInt64>), NullableUInt64Formatter.Instance},
            {typeof(Nullable<Single>), NullableSingleFormatter.Instance},
            {typeof(Nullable<Double>), NullableDoubleFormatter.Instance},
            {typeof(Nullable<bool>), NullableBooleanFormatter.Instance},
            {typeof(Nullable<byte>), NullableByteFormatter.Instance},
            {typeof(Nullable<sbyte>), NullableSByteFormatter.Instance},
            {typeof(Nullable<DateTime>), NullableDateTimeFormatter.Instance},
            {typeof(Nullable<char>), NullableCharFormatter.Instance},
            
            // StandardClassLibraryFormatter
            {typeof(string), NullableStringFormatter.Instance},
            {typeof(decimal), DecimalFormatter.Instance},
            {typeof(decimal?), new StaticNullableFormatter<decimal>(DecimalFormatter.Instance)},
            {typeof(TimeSpan), TimeSpanFormatter.Instance},
            {typeof(TimeSpan?), new StaticNullableFormatter<TimeSpan>(TimeSpanFormatter.Instance)},
            {typeof(DateTimeOffset), DateTimeOffsetFormatter.Instance},
            {typeof(DateTimeOffset?), new StaticNullableFormatter<DateTimeOffset>(DateTimeOffsetFormatter.Instance)},
            {typeof(Guid), GuidFormatter.Instance},
            {typeof(Guid?), new StaticNullableFormatter<Guid>(GuidFormatter.Instance)},
            {typeof(Uri), UriFormatter.Instance},
            {typeof(Version), VersionFormatter.Instance},
            {typeof(StringBuilder), StringBuilderFormatter.Instance},
            {typeof(BitArray), BitArrayFormatter.Instance},
            
            // special primitive
            {typeof(byte[]), ByteArrayFormatter.Instance},
            
            // Nil
            {typeof(Nil), NilFormatter.Instance},
            {typeof(Nil?), NullableNilFormatter.Instance},
            
            // primitive reserved collection formatter
            {typeof(Int16[]), new ArrayFormatter<Int16>()},
            {typeof(Int32[]), new ArrayFormatter<Int32>()},
            {typeof(Int64[]), new ArrayFormatter<Int64>()},
            {typeof(UInt16[]), new ArrayFormatter<UInt16>()},
            {typeof(UInt32[]), new ArrayFormatter<UInt32>()},
            {typeof(UInt64[]), new ArrayFormatter<UInt64>()},
            {typeof(Single[]), new ArrayFormatter<Single>()},
            {typeof(Double[]), new ArrayFormatter<Double>()},
            {typeof(bool[]), new ArrayFormatter<bool>()},
            {typeof(sbyte[]), new ArrayFormatter<sbyte>()},
            {typeof(DateTime[]), new ArrayFormatter<DateTime>()},
            {typeof(char[]), new ArrayFormatter<char>()},
            {typeof(string[]), new ArrayFormatter<string>()},

            {typeof(List<Int16>), new ListFormatter<Int16>()},
            {typeof(List<Int32>), new ListFormatter<Int32>()},
            {typeof(List<Int64>), new ListFormatter<Int64>()},
            {typeof(List<UInt16>), new ListFormatter<UInt16>()},
            {typeof(List<UInt32>), new ListFormatter<UInt32>()},
            {typeof(List<UInt64>), new ListFormatter<UInt64>()},
            {typeof(List<Single>), new ListFormatter<Single>()},
            {typeof(List<Double>), new ListFormatter<Double>()},
            {typeof(List<bool>), new ListFormatter<bool>()},
            {typeof(List<byte>), new ListFormatter<byte>()},
            {typeof(List<sbyte>), new ListFormatter<sbyte>()},
            {typeof(List<DateTime>), new ListFormatter<DateTime>()},
            {typeof(List<char>), new ListFormatter<char>()},
            {typeof(List<string>), new ListFormatter<string>()},

            { typeof(ArraySegment<byte>), ByteArraySegmentFormatter.Instance },
            { typeof(ArraySegment<byte>?),new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Instance) },

#if NETSTANDARD1_4
            {typeof(System.Numerics.BigInteger), BigIntegerFormatter.Instance},
            {typeof(System.Numerics.BigInteger?), new StaticNullableFormatter<System.Numerics.BigInteger>(BigIntegerFormatter.Instance)},
            {typeof(System.Numerics.Complex), ComplexFormatter.Instance},
            {typeof(System.Numerics.Complex?), new StaticNullableFormatter<System.Numerics.Complex>(ComplexFormatter.Instance)},
            {typeof(System.Threading.Tasks.Task), TaskUnitFormatter.Instance},
#endif
        };

        internal static object GetFormatter(Type t)
        {
            object formatter;
            if (formatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}
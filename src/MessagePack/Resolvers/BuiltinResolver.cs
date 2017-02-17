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
                return new Int16Formatter();
            }
            else if (t == typeof(Int32))
            {
                return new Int32Formatter();
            }
            else if (t == typeof(Int64))
            {
                return new Int64Formatter();
            }
            else if (t == typeof(UInt16))
            {
                return new UInt16Formatter();
            }
            else if (t == typeof(UInt32))
            {
                return new UInt32Formatter();
            }
            else if (t == typeof(UInt64))
            {
                return new UInt64Formatter();
            }
            else if (t == typeof(Single))
            {
                return new SingleFormatter();
            }
            else if (t == typeof(Double))
            {
                return new DoubleFormatter();
            }
            else if (t == typeof(bool))
            {
                return new BooleanFormatter();
            }
            else if (t == typeof(byte))
            {
                return new ByteFormatter();
            }
            else if (t == typeof(sbyte))
            {
                return new SByteFormatter();
            }
            else if (t == typeof(DateTime))
            {
                return new DateTimeFormatter();
            }
            else if (t == typeof(char))
            {
                return new CharFormatter();
            }
            // Nulllable Primitive
            else if (t == typeof(Nullable<Int16>))
            {
                return new NullableInt16Formatter();
            }
            else if (t == typeof(Nullable<Int32>))
            {
                return new NullableInt32Formatter();
            }
            else if (t == typeof(Nullable<Int64>))
            {
                return new NullableInt64Formatter();
            }
            else if (t == typeof(Nullable<UInt16>))
            {
                return new NullableUInt16Formatter();
            }
            else if (t == typeof(Nullable<UInt32>))
            {
                return new NullableUInt32Formatter();
            }
            else if (t == typeof(Nullable<UInt64>))
            {
                return new NullableUInt64Formatter();
            }
            else if (t == typeof(Nullable<Single>))
            {
                return new NullableSingleFormatter();
            }
            else if (t == typeof(Nullable<Double>))
            {
                return new NullableDoubleFormatter();
            }
            else if (t == typeof(Nullable<bool>))
            {
                return new NullableBooleanFormatter();
            }
            else if (t == typeof(Nullable<byte>))
            {
                return new NullableByteFormatter();
            }
            else if (t == typeof(Nullable<sbyte>))
            {
                return new NullableSByteFormatter();
            }
            else if (t == typeof(Nullable<DateTime>))
            {
                return new NullableDateTimeFormatter();
            }
            else if (t == typeof(Nullable<char>))
            {
                return new NullableCharFormatter();
            }

            // StandardClassLibraryFormatter
            else if (t == typeof(string))
            {
                return new NullableStringFormatter();
            }
            else if (t == typeof(decimal))
            {
                return new DecimalFormatter();
            }
            else if (t == typeof(decimal?))
            {
                return new StaticNullableFormatter<decimal>(BuiltinResolver.Instance.GetFormatter<decimal>());
            }
            else if (t == typeof(TimeSpan))
            {
                return new TimeSpanFormatter();
            }
            else if (t == typeof(TimeSpan?))
            {
                return new StaticNullableFormatter<TimeSpan>(BuiltinResolver.Instance.GetFormatter<TimeSpan>());
            }
            else if (t == typeof(DateTimeOffset))
            {
                return new DateTimeOffsetFormatter();
            }
            else if (t == typeof(DateTimeOffset?))
            {
                return new StaticNullableFormatter<DateTimeOffset>(BuiltinResolver.Instance.GetFormatter<DateTimeOffset>());
            }
            else if (t == typeof(Guid))
            {
                return new GuidFormatter();
            }
            else if (t == typeof(Guid?))
            {
                return new StaticNullableFormatter<Guid>(BuiltinResolver.Instance.GetFormatter<Guid>());
            }
            else if (t == typeof(Uri))
            {
                return new UriFormatter();
            }
            else if (t == typeof(Version))
            {
                return new VersionFormatter();
            }

            // Nil
            else if (t == typeof(Nil))
            {
                return new NilFormatter();
            }
            else if (t == typeof(Nil?))
            {
                return new NullableNilFormatter();
            }

            // NET40
            else if (t == typeof(System.Numerics.BigInteger))
            {
                return new BigIntegerFormatter();
            }
            else if (t == typeof(System.Numerics.BigInteger?))
            {
                return new StaticNullableFormatter<System.Numerics.BigInteger>(BuiltinResolver.Instance.GetFormatter<System.Numerics.BigInteger>());
            }
            else if (t == typeof(System.Numerics.Complex))
            {
                return new ComplexFormatter();
            }
            else if (t == typeof(System.Numerics.Complex?))
            {
                return new StaticNullableFormatter<System.Numerics.Complex>(BuiltinResolver.Instance.GetFormatter<System.Numerics.Complex>());
            }

            // TODO:Dynamic Code Generation

            else if (t.IsArray)
            {
                return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(t.GetElementType()));
            }

            // TODO:Dynamic Code Generation
            else if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                var genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsNullable();
                var nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;


                // IList...
                // List...

                // TODO:Nullable?


                if (genericType == typeof(KeyValuePair<,>))
                {
                    return Activator.CreateInstance(typeof(KeyValuePairFormatter<,>).MakeGenericType(ti.GenericTypeArguments));
                }
                else if (isNullable && nullableElementType.IsConstructedGenericType && nullableElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return Activator.CreateInstance(typeof(NullableFormatter<>).MakeGenericType(nullableElementType));
                }



                // Tuple
                else if (ti.FullName.StartsWith("System.Tuple"))
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(TupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(TupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(TupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(TupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(TupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return Activator.CreateInstance(tupleFormatterType.MakeGenericType(t));
                }


            }

            return null;
        }
    }
}

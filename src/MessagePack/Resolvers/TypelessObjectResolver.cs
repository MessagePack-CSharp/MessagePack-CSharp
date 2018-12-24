using System;
using MessagePack.Formatters;
using System.Collections.Generic;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
#if NETSTANDARD || NETFRAMEWORK

    /// <summary>
    /// Used for `object` fields/collections, ex: var arr = new object[] { 1, "a", new Model() };
    /// The runtime type of value in object field, should be covered by one of resolvers in complex/standard resolver.
    /// <see cref="TypelessObjectResolver"/> should be placed before DynamicObjectTypeFallbackResolver and <see cref="PrimitiveObjectFormatter"/> in resolvers list.
    /// Deserializer uses Namespace.TypeName, AssemblyName to get runtime type in destination app, so that combination must be present in destination app.
    /// Serialized binary is valid MessagePack binary used ext-format and custom typecode(100).
    /// Inside ext - assembly qualified type name, and serialized object
    /// </summary>
    public sealed class TypelessObjectResolver : IFormatterResolver
    {
        /// <summary>
        /// Backing field for the <see cref="Formatter"/> property.
        /// </summary>
        private TypelessFormatter formatter = new TypelessFormatter();

        /// <summary>
        /// Gets or sets the <see cref="TypelessFormatter"/> used when serializing/deserializing values typed as <see cref="object"/>/
        /// </summary>
        /// <value>A instance of a formatter. Never null.</value>
        /// <exception cref="ArgumentNullException">Thrown if assigned a value of null.</exception>
        public TypelessFormatter Formatter
        {
            get => this.formatter;
            set => this.formatter = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return typeof(T) == typeof(object)
                ? (IMessagePackFormatter<T>)(object)this.Formatter
                : null;
        }
    }

    // helpers for TypelessFormatter

    internal sealed class ForceSizePrimitiveObjectResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new ForceSizePrimitiveObjectResolver();

        ForceSizePrimitiveObjectResolver()
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
                formatter = (IMessagePackFormatter<T>)Helper.GetFormatter(typeof(T));
            }
        }

        static class Helper
        {
            static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
            {
                // Primitive
                {typeof(Int16), ForceInt16BlockFormatter.Instance},
                {typeof(Int32), ForceInt32BlockFormatter.Instance},
                {typeof(Int64), ForceInt64BlockFormatter.Instance},
                {typeof(UInt16), ForceUInt16BlockFormatter.Instance},
                {typeof(UInt32), ForceUInt32BlockFormatter.Instance},
                {typeof(UInt64), ForceUInt64BlockFormatter.Instance},
                {typeof(byte), ForceByteBlockFormatter.Instance},
                {typeof(sbyte), ForceSByteBlockFormatter.Instance},
            
                // Nulllable Primitive
                {typeof(Nullable<Int16>), NullableForceInt16BlockFormatter.Instance},
                {typeof(Nullable<Int32>), NullableForceInt32BlockFormatter.Instance},
                {typeof(Nullable<Int64>), NullableForceInt64BlockFormatter.Instance},
                {typeof(Nullable<UInt16>), NullableForceUInt16BlockFormatter.Instance},
                {typeof(Nullable<UInt32>), NullableForceUInt32BlockFormatter.Instance},
                {typeof(Nullable<UInt64>), NullableForceUInt64BlockFormatter.Instance},
                {typeof(Nullable<byte>), NullableForceByteBlockFormatter.Instance},
                {typeof(Nullable<sbyte>), NullableForceSByteBlockFormatter.Instance},
            
                // otpmitized primitive array formatter
                {typeof(Int16[]), ForceInt16BlockArrayFormatter.Instance},
                {typeof(Int32[]), ForceInt32BlockArrayFormatter.Instance},
                {typeof(Int64[]), ForceInt64BlockArrayFormatter.Instance},
                {typeof(UInt16[]), ForceUInt16BlockArrayFormatter.Instance},
                {typeof(UInt32[]), ForceUInt32BlockArrayFormatter.Instance},
                {typeof(UInt64[]), ForceUInt64BlockArrayFormatter.Instance},
                {typeof(SByte[]), ForceSByteBlockArrayFormatter.Instance},
            };

            public static object GetFormatter(Type type)
            {
                object formatter;
                return formatterMap.TryGetValue(type, out formatter)
                    ? formatter
                    : null;
            }
        }
    }

    internal sealed class TypelessFormatterFallbackResolver : IFormatterResolver
    {
        public readonly static IFormatterResolver Instance = new TypelessFormatterFallbackResolver();

        static readonly IMessagePackFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(
            ForceSizePrimitiveObjectResolver.Instance,
            ContractlessStandardResolverAllowPrivateCore.Instance);

        TypelessFormatterFallbackResolver()
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
                if (typeof(T) == typeof(object))
                {
                    formatter = (IMessagePackFormatter<T>)fallbackFormatter;
                }
            }
        }
    }

#endif
}
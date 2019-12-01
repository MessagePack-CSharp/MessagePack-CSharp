// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
#if !UNITY_2018_3_OR_NEWER

    /// <summary>
    /// Used for `object` fields/collections, ex: var arr = new object[] { 1, "a", new Model() };
    /// The runtime type of value in object field, should be covered by one of resolvers in complex/standard resolver.
    /// <see cref="TypelessObjectResolver"/> should be placed before DynamicObjectTypeFallbackResolver and <see cref="PrimitiveObjectFormatter"/> in resolvers list.
    /// Deserializer uses Namespace.TypeName, AssemblyName to get runtime type in destination app, so that combination must be present in destination app.
    /// Serialized binary is valid MessagePack binary used ext-format and custom typecode(100).
    /// Inside ext - assembly qualified type name, and serialized object.
    /// </summary>
    public sealed class TypelessObjectResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new TypelessObjectResolver();

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            ForceSizePrimitiveObjectResolver.Instance,
            ContractlessStandardResolverAllowPrivate.Instance,
        };

        private TypelessObjectResolver()
        {
        }

        /// <inheritdoc />
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(object))
            {
                return (IMessagePackFormatter<T>)TypelessFormatter.Instance;
            }
            else
            {
                foreach (IFormatterResolver item in Resolvers)
                {
                    IMessagePackFormatter<T> f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                return null;
            }
        }
    }

    /* helpers for TypelessFormatter */

    internal sealed class ForceSizePrimitiveObjectResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ForceSizePrimitiveObjectResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        static ForceSizePrimitiveObjectResolver()
        {
            Instance = new ForceSizePrimitiveObjectResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ForceSizePrimitiveObjectResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)Helper.GetFormatter(typeof(T));
            }
        }

        private static class Helper
        {
            private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
            {
                // Primitive
                { typeof(Int16), ForceInt16BlockFormatter.Instance },
                { typeof(Int32), ForceInt32BlockFormatter.Instance },
                { typeof(Int64), ForceInt64BlockFormatter.Instance },
                { typeof(UInt16), ForceUInt16BlockFormatter.Instance },
                { typeof(UInt32), ForceUInt32BlockFormatter.Instance },
                { typeof(UInt64), ForceUInt64BlockFormatter.Instance },
                { typeof(byte), ForceByteBlockFormatter.Instance },
                { typeof(sbyte), ForceSByteBlockFormatter.Instance },

                // Nullable Primitive
                { typeof(Int16?), NullableForceInt16BlockFormatter.Instance },
                { typeof(Int32?), NullableForceInt32BlockFormatter.Instance },
                { typeof(Int64?), NullableForceInt64BlockFormatter.Instance },
                { typeof(UInt16?), NullableForceUInt16BlockFormatter.Instance },
                { typeof(UInt32?), NullableForceUInt32BlockFormatter.Instance },
                { typeof(UInt64?), NullableForceUInt64BlockFormatter.Instance },
                { typeof(byte?), NullableForceByteBlockFormatter.Instance },
                { typeof(sbyte?), NullableForceSByteBlockFormatter.Instance },

                // optimized primitive array formatter
                { typeof(Int16[]), ForceInt16BlockArrayFormatter.Instance },
                { typeof(Int32[]), ForceInt32BlockArrayFormatter.Instance },
                { typeof(Int64[]), ForceInt64BlockArrayFormatter.Instance },
                { typeof(UInt16[]), ForceUInt16BlockArrayFormatter.Instance },
                { typeof(UInt32[]), ForceUInt32BlockArrayFormatter.Instance },
                { typeof(UInt64[]), ForceUInt64BlockArrayFormatter.Instance },
                { typeof(SByte[]), ForceSByteBlockArrayFormatter.Instance },
            };

            public static object GetFormatter(Type type)
            {
                object formatter;
                return FormatterMap.TryGetValue(type, out formatter)
                    ? formatter
                    : null;
            }
        }
    }

#endif
}

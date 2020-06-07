// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class PrimitiveObjectResolver : IFormatterResolver
    {
        /// <summary>
        /// A resolver that can deserialize anything as .NET primitive types, arrays and object-keyed dictionaries.
        /// </summary>
        public static readonly PrimitiveObjectResolver Instance;

        /// <summary>
        /// A resolver that can deserialize anything as .NET primitive types, arrays and <see cref="ExpandoObject"/> string-keyed dictionaries.
        /// </summary>
        public static readonly PrimitiveObjectResolver InstanceWithExpandoObject;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private readonly bool useExpandoObject;

        static PrimitiveObjectResolver()
        {
            Instance = new PrimitiveObjectResolver(useExpandoObject: false);
            InstanceWithExpandoObject = new PrimitiveObjectResolver(useExpandoObject: true);
            Options = new MessagePackSerializerOptions(Instance);
        }

        private PrimitiveObjectResolver(bool useExpandoObject)
        {
            this.useExpandoObject = useExpandoObject;
        }

        public IMessagePackFormatter<T> GetFormatter<T>() => this.useExpandoObject ? ExpandoObjectFormatterCache<T>.Formatter : FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (typeof(T) == typeof(object))
                    ? (IMessagePackFormatter<T>)PrimitiveObjectFormatter.Instance
                    : null;
            }
        }

        private static class ExpandoObjectFormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static ExpandoObjectFormatterCache()
            {
                Formatter = (typeof(T) == typeof(object))
                    ? (IMessagePackFormatter<T>)PrimitiveObjectFormatter.InstanceWithExpandoObject
                    : null;
            }
        }
    }

    ////#if !UNITY_2018_3_OR_NEWER

    ////    /// <summary>
    ////    /// In `object`, when serializing resolve by concrete type and when deserializing use primitive.
    ////    /// </summary>
    ////    public sealed class DynamicObjectTypeFallbackResolver : IFormatterResolver
    ////    {
    ////        public static readonly DynamicObjectTypeFallbackResolver Instance = new DynamicObjectTypeFallbackResolver();

    ////        DynamicObjectTypeFallbackResolver()
    ////        {

    ////        }

    ////        public IMessagePackFormatter<T> GetFormatter<T>()
    ////        {
    ////            return FormatterCache<T>.formatter;
    ////        }

    ////        static class FormatterCache<T>
    ////        {
    ////            public static readonly IMessagePackFormatter<T> formatter;

    ////            static FormatterCache()
    ////            {
    ////                formatter = (typeof(T) == typeof(object))
    ////                    ? (IMessagePackFormatter<T>)(object)DynamicObjectTypeFallbackFormatter.Instance
    ////                    : null;
    ////            }
    ////        }
    ////    }

    ////#endif
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq; // require UNITY_2018_3_OR_NEWER
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Get formatter from <see cref="MessagePackFormatterAttribute"/>.
    /// </summary>
    public sealed class AttributeFormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly AttributeFormatterResolver Instance = new AttributeFormatterResolver();

        private AttributeFormatterResolver()
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
#if UNITY_2018_3_OR_NEWER && !NETFX_CORE
                var attr = (MessagePackFormatterAttribute)typeof(T).GetCustomAttributes(typeof(MessagePackFormatterAttribute), true).FirstOrDefault();
#else
                MessagePackFormatterAttribute attr = typeof(T).GetTypeInfo().GetCustomAttribute<MessagePackFormatterAttribute>();
#endif
                if (attr == null)
                {
                    return;
                }

                if (attr.Arguments == null)
                {
                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(attr.FormatterType);
                }
                else
                {
                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(attr.FormatterType, attr.Arguments);
                }
            }
        }
    }
}

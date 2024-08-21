// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    public sealed class DynamicEnumAsStringResolver : IFormatterResolver
    {
        private readonly bool ignoreCase;
        private readonly Dictionary<Type, FormatterCache> formatterCaches = new();

        /// <summary>
        /// Case sensetive Instance
        /// <Instance>The singleton instance that can be used</Instance>.
        /// <Options>A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.</Options>
        /// </summary>
        public static readonly (DynamicEnumAsStringResolver Instance, MessagePackSerializerOptions Options) CaseSensetiveInstance;

        /// <summary>
        /// Case insensetive Instance
        /// <Instance>The singleton instance that can be used</Instance>.
        /// <Options>A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.</Options>
        /// </summary>
        public static readonly (DynamicEnumAsStringResolver Instance, MessagePackSerializerOptions Options) CaseInsensitiveInstance;

        static DynamicEnumAsStringResolver()
        {
            var instance = new DynamicEnumAsStringResolver(false);
            CaseSensetiveInstance = (instance, new MessagePackSerializerOptions(instance));
            instance = new DynamicEnumAsStringResolver(true);
            CaseInsensitiveInstance = (instance, new MessagePackSerializerOptions(instance));
        }

        private DynamicEnumAsStringResolver(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            if (formatterCaches.TryGetValue(typeof(T), out var formatter))
            {
                return ((FormatterCache<T>)formatter).Formatter;
            }

            formatter = new FormatterCache<T>(ignoreCase,
                ignoreCase ? CaseInsensitiveInstance.Instance : CaseSensetiveInstance.Instance);
            formatterCaches[typeof(T)] = formatter;
            return ((FormatterCache<T>)formatter).Formatter;
        }

        private class FormatterCache
        {
        }

        private class FormatterCache<T> : FormatterCache
        {
            private readonly DynamicEnumAsStringResolver instance;
            public readonly IMessagePackFormatter<T>? Formatter;

            public FormatterCache(bool ignoreCase, DynamicEnumAsStringResolver instance)
            {
                this.instance = instance;
                TypeInfo ti = typeof(T).GetTypeInfo();

                if (ti.IsNullable())
                {
                    // build underlying type and use wrapped formatter.
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();
                    if (!ti.IsEnum)
                    {
                        return;
                    }

                    var innerFormatter = instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }
                else if (!ti.IsEnum)
                {
                    return;
                }

                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(EnumAsStringFormatter<>).MakeGenericType(typeof(T)), new object[] { ignoreCase })!;
            }
        }
    }
}

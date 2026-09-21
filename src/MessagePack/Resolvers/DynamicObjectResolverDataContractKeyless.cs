// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using MessagePack.Formatters;
using MessagePack.Internal;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation configured to force keyless serialization when the type is decorated with
    /// <see cref="DataContractAttribute"/> and does not have a <see cref="MessagePackObjectAttribute"/>.
    /// </summary>
    public sealed class DynamicObjectResolverDataContractKeyless : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolverDataContractKeyless";

        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicObjectResolverDataContractKeyless Instance = new();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        internal static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        static DynamicObjectResolverDataContractKeyless()
        {
            Options = new MessagePackSerializerOptions(Instance);
        }

        private DynamicObjectResolverDataContractKeyless()
        {
        }

#if NETFRAMEWORK
        internal AssemblyBuilder? Save()
        {
            return DynamicAssemblyFactory.GetDynamicAssembly(type: null, allowPrivate: false)?.Save();
        }
#endif

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        internal static IMessagePackFormatter<T>? BuildFormatterHelper<T>(IFormatterResolver self, DynamicAssemblyFactory dynamicAssemblyFactory, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            TypeInfo ti = typeof(T).GetTypeInfo();

            if (ti.IsInterface || ti.IsAbstract)
            {
                return null;
            }

            DynamicAssembly? dynamicAssembly = null;
            if (ti.IsAnonymous())
            {
                forceStringKey = true;
                contractless = true;

                // For anonymous types, it's important to be able to access the internal type itself,
                // but *not* look at non-public members to avoid double-serialization of the properties
                // as well as their backing fields.
                allowPrivate = false;
                dynamicAssembly = DynamicAssemblyFactory.GetDynamicAssembly(typeof(T), true);
            }
            else if (ti.IsNullable())
            {
                ti = ti.GenericTypeArguments[0].GetTypeInfo();

                var innerFormatter = self.GetFormatterDynamic(ti.AsType());
                if (innerFormatter == null)
                {
                    return null;
                }

                return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), [innerFormatter]);
            }

            if (!forceStringKey
                && ti.GetCustomAttribute<DataContractAttribute>() is not null
                && ti.GetCustomAttribute<MessagePackObjectAttribute>() is null)
            {
                forceStringKey = true;
            }

            allowPrivate |= !contractless && typeof(T).GetCustomAttributes<MessagePackObjectAttribute>().Any(a => a.AllowPrivate);
            dynamicAssembly ??= DynamicAssemblyFactory.GetDynamicAssembly(typeof(T), allowPrivate);
            TypeInfo? formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(dynamicAssembly, typeof(T), forceStringKey, contractless, allowPrivate);
            return formatterTypeInfo is null ? null : (IMessagePackFormatter<T>)ResolverUtilities.ActivateFormatter(formatterTypeInfo.AsType());
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, false, false, false);
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation for <see cref="DataContractAttribute"/>, allow private member.
    /// </summary>
    public sealed class DynamicObjectResolverDataContractKeylessAllowPrivate : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolverDataContractKeylessAllowPrivate";

        public static readonly DynamicObjectResolverDataContractKeylessAllowPrivate Instance = new DynamicObjectResolverDataContractKeylessAllowPrivate();

        internal static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        private DynamicObjectResolverDataContractKeylessAllowPrivate()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T>? Formatter = DynamicObjectResolverDataContractKeyless.BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, false, false, true);
        }
    }
}

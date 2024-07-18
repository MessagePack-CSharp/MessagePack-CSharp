// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Default composited resolver, builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> primitive.
    /// </summary>
    public sealed class StandardResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StandardResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this resolver pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = MessagePackSerializer.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [
                ..StandardResolverHelper.DefaultResolvers,
                DynamicObjectResolver.Instance
                ];

        static StandardResolver()
        {
            Instance = new StandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private StandardResolver()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = MessagePackSerializer.AvoidDynamicCode
                        ? PrimitiveObjectResolver.Instance.GetFormatter<T>()
                        : (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }

    public sealed class ContractlessStandardResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ContractlessStandardResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this resolver pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = MessagePackSerializer.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [
                ..StandardResolverHelper.DefaultResolvers,
                DynamicObjectResolver.Instance, // Try Object
                DynamicContractlessObjectResolver.Instance, // Serializes keys as strings
                ];

        static ContractlessStandardResolver()
        {
            Instance = new ContractlessStandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ContractlessStandardResolver()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = MessagePackSerializer.AvoidDynamicCode
                        ? PrimitiveObjectResolver.Instance.GetFormatter<T>()
                        : (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }

    public sealed class StandardResolverAllowPrivate : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StandardResolverAllowPrivate Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this resolver pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = MessagePackSerializer.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [
                ..StandardResolverHelper.DefaultResolvers,
                DynamicObjectResolverAllowPrivate.Instance, // Try Object
                ];

        static StandardResolverAllowPrivate()
        {
            Instance = new StandardResolverAllowPrivate();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private StandardResolverAllowPrivate()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = MessagePackSerializer.AvoidDynamicCode
                        ? PrimitiveObjectResolver.Instance.GetFormatter<T>()
                        : (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }

    public sealed class ContractlessStandardResolverAllowPrivate : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ContractlessStandardResolverAllowPrivate Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this resolver pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = MessagePackSerializer.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [
                ..StandardResolverHelper.DefaultResolvers,
                DynamicObjectResolverAllowPrivate.Instance, // Try Object
                DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
                ];

        static ContractlessStandardResolverAllowPrivate()
        {
            Instance = new ContractlessStandardResolverAllowPrivate();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ContractlessStandardResolverAllowPrivate()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = MessagePackSerializer.AvoidDynamicCode
                        ? PrimitiveObjectResolver.Instance.GetFormatter<T>()
                        : (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class StandardResolverHelper
    {
        public static readonly IFormatterResolver[] DefaultResolvers = MessagePackSerializer.AvoidDynamicCode
            ? [
                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
                SourceGeneratedFormatterResolver.Instance, // Prefer source generated formatters over dynamic ones.
                ImmutableCollection.ImmutableCollectionResolver.Instance,
                CompositeResolver.Create(ExpandoObjectFormatter.Instance)
                ]
            : [
                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
                SourceGeneratedFormatterResolver.Instance, // Prefer source generated formatters over dynamic ones.
                ImmutableCollection.ImmutableCollectionResolver.Instance,
                CompositeResolver.Create(ExpandoObjectFormatter.Instance),
                DynamicGenericResolver.Instance, // Try Array, Tuple, Collection, Enum(Generic Fallback)
                DynamicUnionResolver.Instance, // Try Union(Interface)
            ];
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;

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
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicObjectResolver.Instance, // Try Object
#endif
        }).ToArray();

        static StandardResolver()
        {
            Instance = new StandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private StandardResolver()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
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
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicObjectResolver.Instance, // Try Object
            DynamicContractlessObjectResolver.Instance, // Serializes keys as strings
#endif
        }).ToArray();

        static ContractlessStandardResolver()
        {
            Instance = new ContractlessStandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ContractlessStandardResolver()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
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
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicObjectResolverAllowPrivate.Instance, // Try Object
#endif
        }).ToArray();

        static StandardResolverAllowPrivate()
        {
            Instance = new StandardResolverAllowPrivate();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private StandardResolverAllowPrivate()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
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
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicObjectResolverAllowPrivate.Instance, // Try Object
            DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
#endif
        }).ToArray();

        static ContractlessStandardResolverAllowPrivate()
        {
            Instance = new ContractlessStandardResolverAllowPrivate();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ContractlessStandardResolverAllowPrivate()
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
                if (typeof(T) == typeof(object))
                {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
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
        public static readonly IFormatterResolver[] DefaultResolvers = new IFormatterResolver[]
        {
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

#if UNITY_2018_3_OR_NEWER
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicEnumResolver.Instance, // Try Enum
#endif

#if !ENABLE_IL2CPP
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection, Enum(Generic Fallback)
#endif

#if !ENABLE_IL2CPP && !NET_STANDARD_2_0
            DynamicUnionResolver.Instance, // Try Union(Interface)
#endif
        };
    }
}

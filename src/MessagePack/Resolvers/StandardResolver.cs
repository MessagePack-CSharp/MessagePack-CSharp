using MessagePack.Formatters;
using System.Linq;
using MessagePack.Internal;
using MessagePack.Resolvers;

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
        public static readonly StandardResolver Instance = new StandardResolver();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

#if !UNITY
        public static readonly IMessagePackFormatter<object> ObjectFallbackFormatter = new DynamicObjectTypeFallbackFormatter(StandardResolverCore.Instance);
#endif

        StandardResolver()
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
                    // final fallback
#if !UNITY
                    formatter = (IMessagePackFormatter<T>)ObjectFallbackFormatter;
#else
                    formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    formatter = StandardResolverCore.Instance.GetFormatter<T>();
                }
            }
        }
    }

    public sealed class ContractlessStandardResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ContractlessStandardResolver Instance = new ContractlessStandardResolver();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

#if !UNITY
        public static readonly IMessagePackFormatter<object> ObjectFallbackFormatter = new DynamicObjectTypeFallbackFormatter(ContractlessStandardResolverCore.Instance);
#endif

        ContractlessStandardResolver()
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
                    // final fallback
#if !UNITY
                    formatter = (IMessagePackFormatter<T>)ObjectFallbackFormatter;
#else
                    formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    formatter = ContractlessStandardResolverCore.Instance.GetFormatter<T>();
                }
            }
        }
    }

    public sealed class StandardResolverAllowPrivate : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StandardResolverAllowPrivate Instance = new StandardResolverAllowPrivate();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

#if !UNITY
        public static readonly IMessagePackFormatter<object> ObjectFallbackFormatter = new DynamicObjectTypeFallbackFormatter(StandardResolverAllowPrivateCore.Instance);
#endif

        StandardResolverAllowPrivate()
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
                    // final fallback
#if !UNITY
                    formatter = (IMessagePackFormatter<T>)ObjectFallbackFormatter;
#else
                    formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    formatter = StandardResolverAllowPrivateCore.Instance.GetFormatter<T>();
                }
            }
        }
    }

    public sealed class ContractlessStandardResolverAllowPrivate : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ContractlessStandardResolverAllowPrivate Instance = new ContractlessStandardResolverAllowPrivate();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

#if !UNITY
        public static readonly IMessagePackFormatter<object> ObjectFallbackFormatter = new DynamicObjectTypeFallbackFormatter(ContractlessStandardResolverAllowPrivateCore.Instance);
#endif

        ContractlessStandardResolverAllowPrivate()
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
                    // final fallback
#if !UNITY
                    formatter = (IMessagePackFormatter<T>)ObjectFallbackFormatter;
#else
                    formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    formatter = ContractlessStandardResolverAllowPrivateCore.Instance.GetFormatter<T>();
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

#if UNITY
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP && !UNITY_WSA && !NET_STANDARD_2_0

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
#endif
        };
    }

    internal sealed class StandardResolverCore : IFormatterResolver
    {
        internal static readonly StandardResolverCore Instance = new StandardResolverCore();

        static readonly IFormatterResolver[] resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !UNITY_WSA && !NET_STANDARD_2_0
            DynamicObjectResolver.Instance, // Try Object
#endif
        }).ToArray();

        StandardResolverCore()
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
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

    internal sealed class ContractlessStandardResolverCore : IFormatterResolver
    {
        internal static readonly ContractlessStandardResolverCore Instance = new ContractlessStandardResolverCore();

        static readonly IFormatterResolver[] resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !UNITY_WSA && !NET_STANDARD_2_0
            DynamicObjectResolver.Instance, // Try Object
            DynamicContractlessObjectResolver.Instance, // Serializes keys as strings
#endif
        }).ToArray();


        ContractlessStandardResolverCore()
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
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

    internal sealed class StandardResolverAllowPrivateCore : IFormatterResolver
    {
        public static readonly StandardResolverAllowPrivateCore Instance = new StandardResolverAllowPrivateCore();

        static readonly IFormatterResolver[] resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !UNITY_WSA && !NET_STANDARD_2_0
            DynamicObjectResolverAllowPrivate.Instance, // Try Object
#endif
        }).ToArray();

        StandardResolverAllowPrivateCore()
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
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

    internal sealed class ContractlessStandardResolverAllowPrivateCore : IFormatterResolver
    {
        public static readonly ContractlessStandardResolverAllowPrivateCore Instance = new ContractlessStandardResolverAllowPrivateCore();

        static readonly IFormatterResolver[] resolvers = StandardResolverHelper.DefaultResolvers.Concat(new IFormatterResolver[]
        {
#if !ENABLE_IL2CPP && !UNITY_WSA && !NET_STANDARD_2_0
            DynamicObjectResolverAllowPrivate.Instance, // Try Object
            DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
#endif
        }).ToArray();


        ContractlessStandardResolverAllowPrivateCore()
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
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
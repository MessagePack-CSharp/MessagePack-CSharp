// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
    [RequiresUnreferencedCode(Constants.Wildcard)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
        private static readonly IFormatterResolver[] Resolvers = DynamicAssembly.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [.. StandardResolverHelper.DefaultResolvers, DynamicObjectResolver.Instance];

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

        [RequiresUnreferencedCode(Constants.Wildcard)]
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = DynamicAssembly.AvoidDynamicCode
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

    [RequiresUnreferencedCode(Constants.Wildcard)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
        private static readonly IFormatterResolver[] Resolvers = DynamicAssembly.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [.. StandardResolverHelper.DefaultResolvers, DynamicObjectResolver.Instance, DynamicContractlessObjectResolver.Instance];

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

        [RequiresUnreferencedCode(Constants.Wildcard)]
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = DynamicAssembly.AvoidDynamicCode
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

    [RequiresUnreferencedCode(Constants.Wildcard)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
        private static readonly IFormatterResolver[] Resolvers = DynamicAssembly.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [.. StandardResolverHelper.DefaultResolvers, DynamicObjectResolverAllowPrivate.Instance];

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

        [RequiresUnreferencedCode(Constants.Wildcard)]
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = DynamicAssembly.AvoidDynamicCode
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

    [RequiresUnreferencedCode(Constants.Wildcard)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
        private static readonly IFormatterResolver[] Resolvers = DynamicAssembly.AvoidDynamicCode
            ? StandardResolverHelper.DefaultResolvers
            : [.. StandardResolverHelper.DefaultResolvers, DynamicObjectResolverAllowPrivate.Instance, DynamicContractlessObjectResolverAllowPrivate.Instance];

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

        [RequiresUnreferencedCode(Constants.Wildcard)]
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = DynamicAssembly.AvoidDynamicCode
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
    [RequiresUnreferencedCode(Constants.Wildcard)]
    internal static class StandardResolverHelper
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = Constants.AvoidDynamicCodeRuntimeCheck)]
        public static readonly IFormatterResolver[] DefaultResolvers = DynamicAssembly.AvoidDynamicCode
            ? [BuiltinResolver.Instance, AttributeFormatterResolver.Instance, SourceGeneratedFormatterResolver.Instance, ImmutableCollection.ImmutableCollectionResolver.Instance, CompositeResolver.Create(ExpandoObjectFormatter.Instance)]
            : [BuiltinResolver.Instance, AttributeFormatterResolver.Instance, SourceGeneratedFormatterResolver.Instance, ImmutableCollection.ImmutableCollectionResolver.Instance, CompositeResolver.Create(ExpandoObjectFormatter.Instance), DynamicGenericResolver.Instance, DynamicUnionResolver.Instance];
    }
}

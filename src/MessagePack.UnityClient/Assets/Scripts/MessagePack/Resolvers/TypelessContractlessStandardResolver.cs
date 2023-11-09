// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Embeds the full name of .NET types for <see cref="object"/> typed fields/collection items
    /// Preserves .NET <see cref="DateTime"/> timezone.
    /// </summary>
    public sealed class TypelessContractlessStandardResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly TypelessContractlessStandardResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        /// <summary>
        /// A *private* list of resolvers. If we ever want to expose any of these (so the user can adjust settings, etc.)
        /// then we must make this an instance collection instead of a static collection so that each consumer can have their own settings.
        /// </summary>
        private static readonly IReadOnlyList<IFormatterResolver> Resolvers = new IFormatterResolver[]
        {
            NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
            ForceSizePrimitiveObjectResolver.Instance, // Preserve particular integer types
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
#if DYNAMIC_GENERATION
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif
            DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
            TypelessObjectResolver.Instance,
        };

        static TypelessContractlessStandardResolver()
        {
            Instance = new TypelessContractlessStandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private readonly ResolverCache resolverCache = new ResolverCache(Resolvers);

        public IMessagePackFormatter<T>? GetFormatter<T>() => this.resolverCache.GetFormatter<T>();

        private class ResolverCache : CachingFormatterResolver
        {
            private readonly IReadOnlyList<IFormatterResolver> resolvers;

            internal ResolverCache(IReadOnlyList<IFormatterResolver> resolvers)
            {
                this.resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
            }

            protected override IMessagePackFormatter<T>? GetFormatterCore<T>()
            {
                foreach (IFormatterResolver item in this.resolvers)
                {
                    IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                return null;
            }
        }
    }
}

#endif

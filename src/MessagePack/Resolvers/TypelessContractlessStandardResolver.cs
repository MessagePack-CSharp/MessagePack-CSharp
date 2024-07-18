// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using MessagePack.Formatters;

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
        private static readonly IReadOnlyList<IFormatterResolver> Resolvers;

        static TypelessContractlessStandardResolver()
        {
            Instance = new TypelessContractlessStandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
            var resolvers = new List<IFormatterResolver>()
            {
                NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
                ForceSizePrimitiveObjectResolver.Instance, // Preserve particular integer types
                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]new TypelessContractlessStandardResolver()
            };

            if (!MessagePackSerializer.AvoidDynamicCode)
            {
                resolvers.Add(DynamicEnumResolver.Instance); // Try Enum
                resolvers.Add(DynamicGenericResolver.Instance); // Try Array, Tuple, Collection
                resolvers.Add(DynamicUnionResolver.Instance); // Try Union(Interface)
                resolvers.Add(DynamicObjectResolver.Instance); // Try Object
            }

            resolvers.Add(DynamicContractlessObjectResolverAllowPrivate.Instance); // Serializes keys as strings
            resolvers.Add(TypelessObjectResolver.Instance);
            Resolvers = resolvers;
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

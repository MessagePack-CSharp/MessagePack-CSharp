#if NETSTANDARD || NETFRAMEWORK
using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Embed c# type names for `object` typed fields/collection items
    /// Preserve c# DateTime timezone
    /// </summary>
    public sealed class TypelessContractlessStandardResolver : IFormatterResolver
    {
        /// <summary>
        /// A *private* list of resolvers. If we ever want to expose any of these (so the user can adjust settings, etc.)
        /// then we must make this an instance collection instead of a static collection so that each consumer can have their own settings.
        /// </summary>
        private static readonly IReadOnlyList<IFormatterResolver> resolvers = new[]
        {
            NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
#if !ENABLE_IL2CPP
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif
            DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
            new TypelessObjectResolver(),
        };

        private readonly ResolverCache resolverCache = new ResolverCache(resolvers);

        public IMessagePackFormatter<T> GetFormatter<T>() => this.resolverCache.GetFormatter<T>();

        private class ResolverCache : CachingFormatterResolver
        {
            private readonly IReadOnlyList<IFormatterResolver> resolvers;

            internal ResolverCache(IReadOnlyList<IFormatterResolver> resolvers)
            {
                this.resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
            }

            protected override IMessagePackFormatter<T> GetFormatterCore<T>()
            {
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
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

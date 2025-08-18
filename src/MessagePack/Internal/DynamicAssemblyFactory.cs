// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#if NET
using System.Runtime.Loader;
#endif

namespace MessagePack.Internal
{
    /// <summary>
    /// This class is responsible for managing DynamicAssembly instance creation taking into account
    /// AssemblyLoadContext when running under .NET.
    /// </summary>
    internal class DynamicAssemblyFactory
    {
        private readonly string moduleName;

#if NET
        private readonly Dictionary<AssemblyLoadContext, DynamicAssembly> alcCache = new();
#endif

        private DynamicAssembly? singletonAssembly;

        private ImmutableHashSet<AssemblyName> lastCreatedDynamicAssemblySkipVisibilityChecks = SkipClrVisibilityChecks.EmptySet.Add(Assembly.GetExecutingAssembly().GetName());

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAssemblyFactory"/> class.
        /// </summary>
        /// <param name="moduleName">An arbitrary name that will be used for the module of the created dynamic assembly.</param>
        public DynamicAssemblyFactory(string moduleName)
        {
            this.moduleName = moduleName;
        }

        [return: NotNullIfNotNull("type")]
        public DynamicAssembly? GetDynamicAssembly(Type? type, bool allowPrivate)
        {
            if (type is null)
            {
                return this.singletonAssembly;
            }

#if NET
            AssemblyLoadContext? loadContext = AssemblyLoadContext.GetLoadContext(type.Assembly);
#endif

            if (allowPrivate)
            {
                ImmutableHashSet<AssemblyName>.Builder skipVisibilityAssemblies = this.lastCreatedDynamicAssemblySkipVisibilityChecks.ToBuilder();
                int originalCount = skipVisibilityAssemblies.Count;
                SkipClrVisibilityChecks.GetSkipVisibilityChecksRequirements(type, skipVisibilityAssemblies);

                lock (this)
                {
                    if (skipVisibilityAssemblies.Count > originalCount)
                    {
                        // Each set of assemblies is a proper superset of the last one, so we can just keep the last one.
                        // Make sure we take the proper union now that we're in a lock.
                        skipVisibilityAssemblies.UnionWith(this.lastCreatedDynamicAssemblySkipVisibilityChecks);
                        this.lastCreatedDynamicAssemblySkipVisibilityChecks = skipVisibilityAssemblies.ToImmutable();
                        DynamicAssembly newAssembly = NewAssembly();

#if NET
                        if (loadContext is not null)
                        {
                            return this.alcCache[loadContext] = newAssembly;
                        }
#endif

                        return this.singletonAssembly = newAssembly;
                    }
                }
            }

            lock (this)
            {
#if NET
                if (loadContext is not null)
                {
                    if (!this.alcCache.TryGetValue(loadContext, out DynamicAssembly? existing))
                    {
                        existing = NewAssembly();
                        this.alcCache[loadContext] = existing;
                    }

                    return existing;
                }
#endif

                return this.singletonAssembly ??= NewAssembly();
            }

            DynamicAssembly NewAssembly() => new(this.moduleName, this.lastCreatedDynamicAssemblySkipVisibilityChecks);
        }
    }
}

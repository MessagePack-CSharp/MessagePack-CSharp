// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace MessagePack.Internal
{
    /// <summary>
    /// This class is responsible for managing DynamicAssembly instance creation taking into account
    /// AssemblyLoadContext when running under .Net.
    /// </summary>
    internal class DynamicAssemblyFactory
    {
        private readonly string moduleName;

        private Lazy<DynamicAssembly> singletonAssembly;

#if NET6_0_OR_GREATER
        private Dictionary<AssemblyLoadContext, DynamicAssembly> alcCache;
#endif

        public DynamicAssemblyFactory(string moduleName)
        {
            this.moduleName = moduleName;
            this.singletonAssembly = new Lazy<DynamicAssembly>(() => new DynamicAssembly(this.moduleName));

#if NET6_0_OR_GREATER
            this.alcCache = new Dictionary<AssemblyLoadContext, DynamicAssembly>();
#endif
        }

#if NET6_0_OR_GREATER
        public DynamicAssembly GetDynamicAssembly(Type? type)
        {
            if (type is null || AssemblyLoadContext.GetLoadContext(type.Assembly) is not AssemblyLoadContext loadContext)
            {
                return this.singletonAssembly.Value;
            }
            else
            {
                DynamicAssembly? assembly = null;
                lock (alcCache)
                {
                    if (!this.alcCache.TryGetValue(loadContext, out assembly))
                    {
                        assembly = new DynamicAssembly(this.moduleName);
                        this.alcCache[loadContext] = assembly;
                    }

                    return assembly;
                }
            }
        }
#else
        public DynamicAssembly GetDynamicAssembly(Type? type)
        {
            return this.singletonAssembly.Value;
        }
#endif
    }
}

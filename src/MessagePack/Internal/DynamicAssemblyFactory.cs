// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        private readonly Lazy<DynamicAssembly> singletonAssembly;

#if NET
        private readonly Dictionary<AssemblyLoadContext, DynamicAssembly> alcCache = new();
#endif

        public DynamicAssemblyFactory(string moduleName)
        {
            this.moduleName = moduleName;
            this.singletonAssembly = new Lazy<DynamicAssembly>(() => new DynamicAssembly(this.moduleName));
        }

#if NET
        public DynamicAssembly GetDynamicAssembly(Type? type)
        {
            if (type is null || AssemblyLoadContext.GetLoadContext(type.Assembly) is not AssemblyLoadContext loadContext)
            {
                return this.singletonAssembly.Value;
            }
            else
            {
                DynamicAssembly? assembly = null;
                lock (this.alcCache)
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

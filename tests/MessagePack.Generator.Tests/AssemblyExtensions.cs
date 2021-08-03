// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MessagePack.Generator.Tests
{
    internal static class AssemblyExtensions
    {
        public static IFormatterResolver GetResolverInstance(this Assembly assembly, string name)
        {
            var resolverType = assembly.GetType(name);
            return (IFormatterResolver)resolverType.GetField("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }
    }
}

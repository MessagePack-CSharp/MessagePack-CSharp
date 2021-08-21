// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public static class ShouldUseFormatterResolverHelper
    {
        /// <devremarks>
        /// Keep this list in sync with DynamicObjectTypeBuilder.IsOptimizeTargetType.
        /// </devremarks>
        internal static readonly string[] PrimitiveTypes =
        {
            "short",
            "int",
            "long",
            "ushort",
            "uint",
            "ulong",
            "float",
            "double",
            "bool",
            "byte",
            "sbyte",
            "char",
            "byte[]",

            // Do not include types that resolvers are allowed to modify.
            ////"global::System.DateTime",  // OldSpec has no support, so for that and perf reasons a .NET native DateTime resolver exists.
            ////"string", // https://github.com/Cysharp/MasterMemory provides custom formatter for string interning.
        };

        public static bool ShouldUseFormatterResolver(MemberSerializationInfo[] infos)
        {
            foreach (var memberSerializationInfo in infos)
            {
                if (memberSerializationInfo.CustomFormatterTypeName == null && Array.IndexOf(PrimitiveTypes, memberSerializationInfo.Type) == -1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public static class ShouldUseFormatterResolverHelper
    {
        private static readonly string[] PrimitiveTypes =
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
            ////"global::System.DateTime",
            ////"byte[]",
            ////"string",
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

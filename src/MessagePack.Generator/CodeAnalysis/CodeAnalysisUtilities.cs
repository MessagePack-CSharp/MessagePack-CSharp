// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

internal static class CodeAnalysisUtilities
{
    internal static string NamespaceAndType(string typeName, string? @namespace)
    {
        return string.IsNullOrEmpty(@namespace) ? typeName : $"{@namespace}.{typeName}";
    }
}

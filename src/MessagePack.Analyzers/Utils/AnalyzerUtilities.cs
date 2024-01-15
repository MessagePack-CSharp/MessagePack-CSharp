// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.Analyzers;

public static class AnalyzerUtilities
{
    /// <devremarks>
    /// Keep this list in sync with DynamicObjectTypeBuilder.IsOptimizeTargetType.
    /// </devremarks>
    public static readonly string[] PrimitiveTypes =
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

    public static string GetFullNamespaceName(this INamespaceSymbol namespaceSymbol)
    {
        if (namespaceSymbol.IsGlobalNamespace)
        {
            return string.Empty;
        }

        string baseName = GetFullNamespaceName(namespaceSymbol.ContainingNamespace);
        return string.IsNullOrEmpty(baseName) ? namespaceSymbol.Name : baseName + "." + namespaceSymbol.Name;
    }

    public static string GetCanonicalTypeFullName(this ITypeSymbol typeSymbol) => typeSymbol.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    internal static string GetHelpLink(string diagnosticId) => $"https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/doc/analyzers/{diagnosticId}.md";
}

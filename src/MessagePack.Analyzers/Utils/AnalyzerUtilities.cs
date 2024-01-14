// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.Analyzers;

public static class AnalyzerUtilities
{
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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

// Utility and Extension methods for Roslyn
internal static class RoslynExtensions
{
    internal static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
    {
        var t = symbol;
        while (t != null)
        {
            foreach (var item in t.GetMembers())
            {
                yield return item;
            }

            t = t.BaseType;
        }
    }

    internal static bool ApproximatelyEqual(this INamedTypeSymbol? left, INamedTypeSymbol? right)
    {
        if (left is IErrorTypeSymbol || right is IErrorTypeSymbol)
        {
            return left?.ToDisplayString() == right?.ToDisplayString();
        }
        else
        {
            return SymbolEqualityComparer.Default.Equals(left, right);
        }
    }
}

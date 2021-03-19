// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MessagePackCompiler
{
    // Utility and Extension methods for Roslyn
    internal static class RoslynExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(this Compilation compilation)
        {
            return compilation.SyntaxTrees.SelectMany(syntaxTree =>
            {
                var semModel = compilation.GetSemanticModel(syntaxTree);
                return syntaxTree.GetRoot()
                    .DescendantNodes()
                    .Select(x => semModel.GetDeclaredSymbol(x))
                    .OfType<INamedTypeSymbol>();
            });
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
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

        public static bool ApproximatelyEqual(this INamedTypeSymbol? left, INamedTypeSymbol? right)
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
}

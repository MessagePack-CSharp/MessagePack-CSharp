// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator;

public static class RoslynAnalyzerExtensions
{
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

    public static IEnumerable<INamedTypeSymbol> EnumerateBaseType(this ITypeSymbol symbol)
    {
        INamedTypeSymbol? t = symbol.BaseType;
        while (t != null)
        {
            yield return t;
            t = t.BaseType;
        }
    }

    public static AttributeData FindAttribute(this IEnumerable<AttributeData> attributeDataList, string typeName)
    {
        return attributeDataList
            .Where(x => x.AttributeClass?.ToDisplayString() == typeName)
            .FirstOrDefault();
    }

    public static AttributeData FindAttributeShortName(this IEnumerable<AttributeData> attributeDataList, string typeName)
    {
        return attributeDataList
            .Where(x => x.AttributeClass?.Name == typeName)
            .FirstOrDefault();
    }

    public static AttributeData? FindAttributeIncludeBasePropertyShortName(this IPropertySymbol property, string typeName)
    {
        IPropertySymbol? loopingProperty = property;
        do
        {
            AttributeData data = FindAttributeShortName(loopingProperty.GetAttributes(), typeName);
            if (data != null)
            {
                return data;
            }

            loopingProperty = loopingProperty.OverriddenProperty;
        }
        while (loopingProperty != null);

        return null;
    }

    public static AttributeSyntax FindAttribute(this BaseTypeDeclarationSyntax typeDeclaration, SemanticModel model, string typeName)
    {
        return typeDeclaration.AttributeLists
            .SelectMany(x => x.Attributes)
            .Where(x => model.GetTypeInfo(x).Type?.ToDisplayString() == typeName)
            .FirstOrDefault();
    }

    public static INamedTypeSymbol FindBaseTargetType(this ITypeSymbol symbol, string typeName)
    {
        return symbol.EnumerateBaseType()
            .Where(x => x.OriginalDefinition?.ToDisplayString() == typeName)
            .FirstOrDefault();
    }

    public static object? GetSingleNamedArgumentValue(this AttributeData attribute, string key)
    {
        foreach (KeyValuePair<string, TypedConstant> item in attribute.NamedArguments)
        {
            if (item.Key == key)
            {
                return item.Value.Value;
            }
        }

        return null;
    }

    public static bool IsNullable(this INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType)
        {
            if (symbol.ConstructUnboundGenericType().ToDisplayString() == "T?")
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
    {
        ITypeSymbol? t = symbol;
        while (t != null)
        {
            foreach (ISymbol item in t.GetMembers())
            {
                yield return item;
            }

            t = t.BaseType;
        }
    }

    public static IEnumerable<ISymbol> GetAllInterfaceMembers(this ITypeSymbol symbol)
    {
        return symbol.GetMembers()
            .Concat(symbol.AllInterfaces.SelectMany(x => x.GetMembers()));
    }
}

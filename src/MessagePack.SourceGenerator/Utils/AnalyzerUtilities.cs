﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using static MessagePack.SourceGenerator.Constants;

namespace MessagePack.SourceGenerator;

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

    internal static AnalyzerOptions? ParseGeneratorAttribute(ImmutableArray<AttributeData> attributes, ISymbol targetSymbol, CancellationToken cancellationToken)
    {
        AttributeData? generatorAttribute = attributes.SingleOrDefault(ad =>
            ad.AttributeClass?.Name == GeneratedMessagePackResolverAttributeName &&
            ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace);

        if (generatorAttribute is null)
        {
            return null;
        }

        FormattersOptions formattersOptions = new()
        {
            UsesMapMode = generatorAttribute.GetSingleNamedArgumentValue("UseMapMode") is true,
        };

        ResolverOptions resolverOptions = new()
        {
            Name = targetSymbol.Name,
            Namespace = targetSymbol.ContainingNamespace.GetFullNamespaceName(),
        };

        GeneratorOptions generatorOptions = new()
        {
            Formatters = formattersOptions,
            Resolver = resolverOptions,
        };

        AnalyzerOptions options = new()
        {
            Generator = generatorOptions,
            IsGeneratingSource = true,
        };

        return options;
    }

    internal static ImmutableDictionary<string, ImmutableHashSet<string>> ParseKnownFormatterAttribute(ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>();
        foreach (AttributeData ad in attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ad.AttributeClass?.Name == MessagePackKnownFormatterAttributeName && ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace)
            {
                if (ad.ConstructorArguments[0].Value is INamedTypeSymbol formatter)
                {
                    var formattableTypes = ImmutableHashSet.CreateBuilder<string>();
                    foreach (INamedTypeSymbol iface in formatter.AllInterfaces)
                    {
                        if (iface is { IsGenericType: true, MetadataName: "IMessagePackFormatter`1", ContainingNamespace.Name: "Formatters", ContainingNamespace.ContainingNamespace.Name: "MessagePack" })
                        {
                            formattableTypes.Add(iface.TypeArguments[0].GetCanonicalTypeFullName());
                        }
                    }

                    if (formattableTypes.Count > 0)
                    {
                        builder.Add(formatter.GetCanonicalTypeFullName(), formattableTypes.ToImmutable());
                    }
                }
            }
        }

        return builder.ToImmutable();
    }

    internal static ImmutableArray<string> ParseAssumedFormattableAttribute(ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
    {
        return ImmutableArray.CreateRange(
            from ad in attributes
            where ad.AttributeClass?.Name == MessagePackAssumedFormattableAttributeName && ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace
            let type = (INamedTypeSymbol?)ad.ConstructorArguments[0].Value
            where type is not null
            select type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }
}

// Copyright (c) All contributors. All rights reserved.
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

    public static string? GetFullNamespaceName(this INamespaceSymbol? namespaceSymbol)
    {
        if (namespaceSymbol is null or { IsGlobalNamespace: true })
        {
            return null;
        }

        string? baseName = GetFullNamespaceName(namespaceSymbol.ContainingNamespace);
        return baseName is null ? namespaceSymbol.Name : baseName + "." + namespaceSymbol.Name;
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
            Name = new QualifiedNamedTypeName((INamedTypeSymbol)targetSymbol, ImmutableStack<GenericTypeParameterInfo>.Empty),
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

    internal static ImmutableHashSet<FormatterDescriptor> ParseKnownFormatterAttribute(ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
    {
        var builder = ImmutableHashSet<FormatterDescriptor>.Empty.ToBuilder();
        foreach (AttributeData ad in attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ad.AttributeClass?.Name == MessagePackKnownFormatterAttributeName && ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace)
            {
                if (ad.ConstructorArguments[0].Value is INamedTypeSymbol formatterType && FormatterDescriptor.TryCreate(formatterType, out FormatterDescriptor? formatter))
                {
                    builder.Add(formatter);
                }
            }
        }

        return builder.ToImmutable();
    }

    internal static ImmutableArray<FormattableType> ParseAssumedFormattableAttribute(ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
    {
        return ImmutableArray.CreateRange(
            from ad in attributes
            where ad.AttributeClass?.Name == MessagePackAssumedFormattableAttributeName && ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace
            let type = (INamedTypeSymbol?)ad.ConstructorArguments[0].Value
            where type is not null
            select new FormattableType(type, null));
    }

    internal static IEnumerable<string> ResolverSymbolToInstanceExpression(SemanticModel semanticModel, IEnumerable<INamedTypeSymbol?> formatterAndResolverTypes)
    {
        return formatterAndResolverTypes
            .Where(r => r is not null &&
                (r.GetAttributes().Any(x => x.AttributeClass?.Name == GeneratedMessagePackResolverAttributeName && x.AttributeClass?.ContainingNamespace.GetFullNamespaceName() == AttributeNamespace) ||
                r.AllInterfaces.Any(x => x.Name == IFormatterResolverInterfaceName && x.ContainingNamespace.GetFullNamespaceName() == AttributeNamespace)))
            .Select(r =>
            {
                // Prefer to get the resolver by its static Instance property/field, if available.
                if (r!.GetMembers("Instance").FirstOrDefault() is ISymbol { IsStatic: true } instanceMember)
                {
                    if (instanceMember is IFieldSymbol or IPropertySymbol && semanticModel.IsAccessible(0, instanceMember))
                    {
                        return $"{r.GetCanonicalTypeFullName()}.Instance";
                    }
                }

                // If the resolver has GeneratedMessagePackResolverAttribute, it has static Instance property
                if (r.GetAttributes().Any(x => x.AttributeClass?.Name == GeneratedMessagePackResolverAttributeName && x.AttributeClass?.ContainingNamespace.GetFullNamespaceName() == AttributeNamespace))
                {
                    return $"{r.GetCanonicalTypeFullName()}.Instance";
                }

                // Fallback to instantiating the resolver, if a constructor is available.
                if (r.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 0) is IMethodSymbol ctor)
                {
                    if (semanticModel.IsAccessible(0, ctor))
                    {
                        return $"new {r.GetCanonicalTypeFullName()}()";
                    }
                }

                // No way to access an instance of the resolver. Produce something that will error out with direction for the user.
                return $"#error No accessible default constructor or static Instance member on {r}.";
            });
    }

    internal static IEnumerable<string> FormatterSymbolToInstanceExpression(SemanticModel semanticModel, IEnumerable<INamedTypeSymbol?> formatterAndResolverTypes)
    {
        return formatterAndResolverTypes
            .Where(r => r is not null && r.AllInterfaces.Any(x => x.Name == IMessagePackFormatterInterfaceName && x.ContainingNamespace.GetFullNamespaceName() == IMessagePackFormatterInterfaceNamespace))
            .Select(r =>
            {
                // Prefer to get the resolver by its static Instance property/field, if available.
                if (r!.GetMembers("Instance").FirstOrDefault() is ISymbol { IsStatic: true } instanceMember)
                {
                    if (instanceMember is IFieldSymbol or IPropertySymbol && semanticModel.IsAccessible(0, instanceMember))
                    {
                        return $"{r.GetCanonicalTypeFullName()}.Instance";
                    }
                }

                // Fallback to instantiating the resolver, if a constructor is available.
                if (r.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 0) is IMethodSymbol ctor)
                {
                    if (semanticModel.IsAccessible(0, ctor))
                    {
                        return $"new {r.GetCanonicalTypeFullName()}()";
                    }
                }

                // No way to access an instance of the resolver. Produce something that will error out with direction for the user.
                return $"#error No accessible default constructor or static Instance member on {r}.";
            });
    }

    internal static IEnumerable<INamedTypeSymbol> SearchTypeForFormatterImplementations(INamedTypeSymbol symbol)
    {
        foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
        {
            if (iface.IsGenericType && iface.TypeArguments.Length == 1 &&
                iface.Name == IMessagePackFormatterInterfaceName && iface.ContainingNamespace.GetFullNamespaceName() == IMessagePackFormatterInterfaceNamespace &&
                iface.TypeArguments[0] is INamedTypeSymbol formattedType)
            {
                yield return formattedType;
            }
        }
    }
}

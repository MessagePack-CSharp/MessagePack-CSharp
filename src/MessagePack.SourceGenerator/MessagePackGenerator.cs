// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AnalyzerOptions = MessagePack.SourceGenerator.CodeAnalysis.AnalyzerOptions;

namespace MessagePack.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class MessagePackGenerator : IIncrementalGenerator
{
    public const string AttributeNamespace = "MessagePack";
    public const string GeneratedMessagePackResolverAttributeName = "GeneratedMessagePackResolverAttribute";
    public const string MessagePackKnownFormatterAttributeName = "MessagePackKnownFormatterAttribute";
    public const string MessagePackAssumedFormattableAttributeName = "MessagePackAssumedFormattableAttribute";
    public const string MessagePackObjectAttributeName = "MessagePackObjectAttribute";
    public const string MessagePackUnionAttributeName = "UnionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO: Consider auto-detect formatters declared in this compilation
        // so attributes are only required to use formatters from other assemblies.
        var customFormatters = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackKnownFormatterAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => ParseKnownFormatterAttribute(context, ct)).Collect();

        var customFormattedTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackAssumedFormattableAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => ParseAssumedFormattableAttribute(context, ct)).SelectMany((a, ct) => a).Collect();

        var resolverOptions = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{GeneratedMessagePackResolverAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => ParseGeneratorAttribute(context, ct)).Collect().Select((a, ct) => a.SingleOrDefault());

        var options = resolverOptions.Combine(customFormattedTypes).Combine(customFormatters).Select(static (input, ct) =>
        {
            AnalyzerOptions? options = input.Left.Left ?? new();

            var formattableTypes = input.Left.Right;
            var formatterTypes = input.Right.Aggregate(
                ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
                (first, second) => first.AddRange(second));

            options = options with
            {
                AssumedFormattableTypes = ImmutableHashSet.CreateRange(formattableTypes).Union(formatterTypes.SelectMany(t => t.Value)),
                KnownFormatters = formatterTypes,
            };

            return options;
        });

        var messagePackObjectTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackObjectAttributeName}",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);

        var unionTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackUnionAttributeName}",
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);

        var combined =
            messagePackObjectTypes.Collect().Combine(unionTypes.Collect());

        var source = combined
            .Combine(context.CompilationProvider)
            .Combine(options)
            .Select(static (s, ct) =>
            {
                AnalyzerOptions options = s.Right;

                if (!ReferenceSymbols.TryCreate(s.Left.Right, out ReferenceSymbols? referenceSymbols))
                {
                    return null;
                }

                List<FullModel> modelPerType = new();
                void Collect(TypeDeclarationSyntax typeDecl)
                {
                    if (TypeCollector.Collect(s.Left.Right, options, referenceSymbols, reportDiagnostic: null, typeDecl, ct) is FullModel model)
                    {
                        modelPerType.Add(model);
                    }
                }

                foreach (TypeDeclarationSyntax typeDecl in s.Left.Left.Left)
                {
                    Collect(typeDecl);
                }

                foreach (TypeDeclarationSyntax typeDecl in s.Left.Left.Right)
                {
                    Collect(typeDecl);
                }

                return FullModel.Combine(modelPerType.ToImmutableArray());
            });

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            if (source is { Options.IsGeneratingSource: true })
            {
                GenerateResolver(new GeneratorContext(context), source);
            }
        });

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            if (source is not null)
            {
                foreach (Diagnostic diagnostic in source.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }

                if (source.Options.IsGeneratingSource)
                {
                    Generate(new GeneratorContext(context), source);
                }
            }
        });
    }

    private static AnalyzerOptions ParseGeneratorAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        AttributeData? generatorAttribute = context.Attributes.Single(ad =>
            ad.AttributeClass?.Name == GeneratedMessagePackResolverAttributeName &&
            ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace);

        FormattersOptions formattersOptions = new()
        {
            UsesMapMode = generatorAttribute.GetSingleNamedArgumentValue("UseMapMode") is true,
        };

        ResolverOptions resolverOptions = new()
        {
            Name = context.TargetSymbol.Name,
            Namespace = context.TargetSymbol.ContainingNamespace.GetFullNamespaceName(),
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

    private static ImmutableDictionary<string, ImmutableHashSet<string>> ParseKnownFormatterAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>();
        foreach (AttributeData ad in context.Attributes)
        {
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

    private static ImmutableArray<string> ParseAssumedFormattableAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        return ImmutableArray.CreateRange(
            from ad in context.Attributes
            where ad.AttributeClass?.Name == MessagePackAssumedFormattableAttributeName && ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace
            let type = (INamedTypeSymbol?)ad.ConstructorArguments[0].Value
            where type is not null
            select type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new Comparer();

        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
        {
            return x.Item1.Equals(y.Item1);
        }

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        private SourceProductionContext context;

        public GeneratorContext(SourceProductionContext context)
        {
            this.context = context;
        }

        public CancellationToken CancellationToken => context.CancellationToken;

        public void AddSource(string hintName, string source) => context.AddSource(hintName, source);

        public void ReportDiagnostic(Diagnostic diagnostic) => context.ReportDiagnostic(diagnostic);
    }
}

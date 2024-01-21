// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MessagePack.SourceGenerator.Constants;
using AnalyzerOptions = MessagePack.SourceGenerator.CodeAnalysis.AnalyzerOptions;

namespace MessagePack.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class MessagePackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO: Consider auto-detect formatters declared in this compilation
        // so attributes are only required to use formatters from other assemblies.
        var customFormatters = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackKnownFormatterAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseKnownFormatterAttribute(context.Attributes, ct)).Collect();

        var customFormattedTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackAssumedFormattableAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseAssumedFormattableAttribute(context.Attributes, ct)).SelectMany((a, ct) => a).Collect();

        var resolverOptions = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{GeneratedMessagePackResolverAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseGeneratorAttribute(context.Attributes, context.TargetSymbol, ct)).Collect().Select((a, ct) => a.SingleOrDefault(ao => ao is not null));

        var options = resolverOptions.Combine(customFormattedTypes).Combine(customFormatters).Select(static (input, ct) =>
        {
            AnalyzerOptions? options = input.Left.Left ?? new();

            var formattableTypes = input.Left.Right;
            var formatterTypes = input.Right.Aggregate(
                ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
                (first, second) => first.AddRange(second));

            options = options.WithFormatterTypes(formattableTypes, formatterTypes);

            return options;
        });

        var messagePackObjectTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackObjectAttributeName}",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (context, _) => (ITypeSymbol)context.TargetSymbol);

        var unionTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackUnionAttributeName}",
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (context, _) => (ITypeSymbol)context.TargetSymbol);

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
                    return default;
                }

                List<FullModel> modelPerType = new();
                void Collect(ITypeSymbol typeSymbol)
                {
                    if (TypeCollector.Collect(s.Left.Right, options, referenceSymbols, reportAnalyzerDiagnostic: null, typeSymbol) is FullModel model)
                    {
                        modelPerType.Add(model);
                    }
                }

                foreach (var typeSymbol in s.Left.Left.Left)
                {
                    Collect(typeSymbol);
                }

                foreach (var typeSymbol in s.Left.Left.Right)
                {
                    Collect(typeSymbol);
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
            if (source is { Options.IsGeneratingSource: true })
            {
                Generate(new GeneratorContext(context), source);
            }
        });
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

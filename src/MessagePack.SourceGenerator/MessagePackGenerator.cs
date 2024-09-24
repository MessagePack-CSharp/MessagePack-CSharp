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
        // Search for [assembly: MessagePackKnownFormatter(typeof(SomeFormatter))]
        var customFormatters = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackKnownFormatterAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseKnownFormatterAttribute(context.Attributes, ct)).Collect();

        // Search for [assembly: MessagePackAssumedFormattable(typeof(SomeCustomType))]
        var customFormattedTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{MessagePackAssumedFormattableAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseAssumedFormattableAttribute(context.Attributes, ct)).SelectMany((a, ct) => a).Collect();

        // Search for all implementations of IMessagePackFormatter<T> in the compilation.
        var customFormattersInThisCompilation = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, ct) => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
            transform: (ctxt, ct) =>
            {
                return ctxt.SemanticModel.GetDeclaredSymbol(ctxt.Node, ct) is INamedTypeSymbol symbol
                    && FormatterDescriptor.TryCreate(symbol, out FormatterDescriptor? formatter)
                    && !formatter.ExcludeFromSourceGeneratedResolver
                    ? formatter
                    : null;
            }).Collect();

        // Search for an [GeneratedMessagePackResolver] attribute (presumably on a partial class).
        var resolverOptions = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{GeneratedMessagePackResolverAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseGeneratorAttribute(context.Attributes, context.TargetSymbol, ct)).Collect().Select((a, ct) => a.SingleOrDefault(ao => ao is not null));

        // Assembly an aggregating AnalyzerOptions object from the attributes and intefrace implementations that we've found.
        var options = resolverOptions.Combine(customFormattedTypes).Combine(customFormatters).Combine(customFormattersInThisCompilation)
            .Select(static (input, ct) =>
        {
            AnalyzerOptions? options = input.Left.Left.Left ?? new() { IsGeneratingSource = true };
            ImmutableArray<FormatterDescriptor?> formatterImplementations = input.Right;

            ImmutableArray<FormattableType> formattableTypes = input.Left.Left.Right;
            ImmutableHashSet<FormatterDescriptor> formatterTypes = input.Left.Right.Aggregate(
                ImmutableHashSet<FormatterDescriptor>.Empty,
                (first, second) => first.Union(second));

            // Merge the formatters discovered through attributes (which need only reference formatters from other assemblies),
            // with formatters discovered in the project being compiled.
            formatterTypes = formatterImplementations.Aggregate(
                formatterTypes,
                (first, second) => second is not null ? first.Add(second) : first);

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

                if (!ReferenceSymbols.TryCreate(s.Left.Right, out ReferenceSymbols? referenceSymbols) || referenceSymbols.MessagePackFormatter is null)
                {
                    return default;
                }

                List<FullModel> modelPerType = new();
                void Collect(ITypeSymbol typeSymbol)
                {
                    if (TypeCollector.Collect(s.Left.Right, options, referenceSymbols, reportAnalyzerDiagnostic: null, typeSymbol, ct) is FullModel model)
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

                if (!options.KnownFormatters.IsEmpty)
                {
                    var customFormatterInfos = FullModel.Empty.CustomFormatterInfos.Union(
                        from known in options.KnownFormatters
                        where known.InaccessibleDescriptor is null
                        from formatted in known.FormattableTypes
                        where !options.GetCollidingFormatterDataTypes(known.Name).Contains(formatted) // skip formatters with colliding types to avoid non-deterministic code generation
                        select new CustomFormatterRegisterInfo
                        {
                            Formatter = known.Name,
                            DataType = formatted.Name,
                            CustomFormatter = known,
                        });
                    modelPerType.Add(FullModel.Empty with { CustomFormatterInfos = customFormatterInfos, Options = options });
                }

                return FullModel.Combine(modelPerType.ToImmutableArray());
            });

        var splittedSources = source
            .SelectMany(static (s, ct) =>
            {
                if (s is null)
                {
                    return ImmutableArray<FullModel>.Empty;
                }

                var models = new List<FullModel>();
                models.AddRange(s.ArrayFormatterInfos.Select(i => FullModel.Empty with { Options = s.Options, ArrayFormatterInfos = ImmutableSortedSet.Create(i) }));
                models.AddRange(s.ObjectInfos.Select(i => FullModel.Empty with { Options = s.Options, ObjectInfos = ImmutableSortedSet.Create(i) }));
                models.AddRange(s.EnumInfos.Select(i => FullModel.Empty with { Options = s.Options, EnumInfos = ImmutableSortedSet.Create(i) }));
                models.AddRange(s.UnionInfos.Select(i => FullModel.Empty with { Options = s.Options, UnionInfos = ImmutableSortedSet.Create(i) }));

                return models.ToImmutableArray();
            });

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            if (source is { Options.IsGeneratingSource: true })
            {
                GenerateResolver(new GeneratorContext(context), source);
            }
        });

        context.RegisterSourceOutput(splittedSources, static (context, source) =>
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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using MessagePack.Analyzers.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using AnalyzerOptions = MessagePack.Analyzers.CodeAnalysis.AnalyzerOptions;

namespace MessagePack.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class MessagePackGenerator : IIncrementalGenerator
{
    public const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";
    public const string MessagePackUnionAttributeFullName = "MessagePack.UnionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AdditionalTextsProvider.Collect().Combine(context.AnalyzerConfigOptionsProvider).Select(
            ((ImmutableArray<AdditionalText> AdditionalFiles, AnalyzerConfigOptionsProvider Options) t, CancellationToken ct) => AnalyzerOptions.Parse(t.Options.GlobalOptions, t.AdditionalFiles) with { IsGeneratingSource = true });

        var messagePackObjectTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            MessagePackObjectAttributeFullName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);

        var unionTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            MessagePackUnionAttributeFullName,
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);

        var combined =
            messagePackObjectTypes.Collect().Combine(unionTypes.Collect());

        var source = combined
            .Combine(context.CompilationProvider)
            .Combine(options)
            .Select(static (s, ct) =>
            {
                if (!ReferenceSymbols.TryCreate(s.Left.Right, out ReferenceSymbols? referenceSymbols))
                {
                    return null;
                }

                List<FullModel> modelPerType = new();
                void Collect(TypeDeclarationSyntax typeDecl)
                {
                    if (TypeCollector.Collect(s.Left.Right, s.Right, referenceSymbols, reportDiagnostic: null, typeDecl, ct) is FullModel model)
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
            if (source is not null)
            {
                Generate(new GeneratorContext(context), source);
            }
        });

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            if (source is not null)
            {
                GenerateResolver(new GeneratorContext(context), source);
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

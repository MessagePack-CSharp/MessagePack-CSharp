// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator;

[Generator]
public partial class MessagePackGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver || receiver.TypeDeclarations.Count == 0)
        {
            return;
        }

        CSharpCompilation compilation = (CSharpCompilation)context.Compilation;
        if (!ReferenceSymbols.TryCreate(compilation, out ReferenceSymbols? referenceSymbols) || referenceSymbols.MessagePackFormatter is null)
        {
            return;
        }

        // Search for a resolver generator attribute, which may be applied to any type in the compilation.
        AnalyzerOptions? options = new() { IsGeneratingSource = true };
        foreach (var typeDeclByDocument in receiver.TypeDeclarations.GroupBy(td => td.SyntaxTree))
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(typeDeclByDocument.Key, ignoreAccessibility: true);
            foreach (TypeDeclarationSyntax typeDecl in typeDeclByDocument)
            {
                if (semanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken) is INamedTypeSymbol typeSymbol)
                {
                    if (AnalyzerUtilities.ParseGeneratorAttribute(typeSymbol.GetAttributes(), typeSymbol, context.CancellationToken) is AnalyzerOptions resolverOptions)
                    {
                        options = resolverOptions;
                        break;
                    }
                }
            }
        }

        // Collect and apply the assembly-level attributes to the options.
        options = options.WithAssemblyAttributes(compilation.Assembly.GetAttributes(), context.CancellationToken);

        List<FullModel> modelPerType = new();
        foreach (var syntax in receiver.TypeDeclarations)
        {
            if (TypeCollector.Collect(compilation, options, referenceSymbols, reportAnalyzerDiagnostic: null, syntax, context.CancellationToken) is FullModel model)
            {
                modelPerType.Add(model);
            }
        }

        FullModel fullModel = FullModel.Combine(modelPerType.ToImmutableArray());

        if (options.IsGeneratingSource)
        {
            GeneratorContext generateContext = new(context);
            Generate(generateContext, fullModel);
            GenerateResolver(generateContext, fullModel);
        }
    }

    private class SyntaxContextReceiver : ISyntaxReceiver
    {
        internal static ISyntaxReceiver Create()
        {
            return new SyntaxContextReceiver();
        }

        public HashSet<TypeDeclarationSyntax> TypeDeclarations { get; } = new();

        public HashSet<AttributeSyntax> AssemblyLevelAttributes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode context)
        {
            switch (context)
            {
                // Any type with attributes warrants a review when we have the semantic model available.
                case TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeSyntax:
                    this.TypeDeclarations.Add(typeSyntax);
                    break;
                case AttributeSyntax { Parent: AttributeListSyntax { Parent: CompilationUnitSyntax } } attributeSyntax:
                    this.AssemblyLevelAttributes.Add(attributeSyntax);
                    break;
            }
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        private GeneratorExecutionContext context;

        public GeneratorContext(GeneratorExecutionContext context)
        {
            this.context = context;
        }

        public CancellationToken CancellationToken => context.CancellationToken;

        public void AddSource(string hintName, string source) => context.AddSource(hintName, source);

        public void ReportDiagnostic(Diagnostic diagnostic) => context.ReportDiagnostic(diagnostic);
    }
}

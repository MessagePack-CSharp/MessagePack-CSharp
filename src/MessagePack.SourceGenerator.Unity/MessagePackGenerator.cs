// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using MessagePack.SourceGenerator.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator;

[Generator]
public partial class MessagePackGenerator : ISourceGenerator
{
    public const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarations.Count == 0)
        {
            return;
        }

        Compilation compilation = context.Compilation;
        GeneratorContext generateContext = new(context);
        AnalyzerOptions options = AnalyzerOptions.Parse(context.AnalyzerConfigOptions.GlobalOptions);

        List<FullModel> modelPerType = new();
        foreach (var syntax in receiver.ClassDeclarations)
        {
            if (TypeCollector.Collect(compilation, options, syntax, generateContext, context.CancellationToken) is FullModel model)
            {
                modelPerType.Add(model);
            }
        }

        FullModel fullModel = FullModel.Combine(modelPerType.ToImmutableArray());
        Generate(generateContext, fullModel);
        GenerateResolver(generateContext, fullModel);
    }

    private class SyntaxContextReceiver : ISyntaxReceiver
    {
        internal static ISyntaxReceiver Create()
        {
            return new SyntaxContextReceiver();
        }

        public HashSet<TypeDeclarationSyntax> ClassDeclarations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode context)
        {
            if (context is TypeDeclarationSyntax typeSyntax)
            {
                if (typeSyntax.AttributeLists.Count > 0)
                {
                    var hasAttribute = typeSyntax.AttributeLists
                        .SelectMany(x => x.Attributes)
                        .Any(x => x.Name.ToString() is "MessagePackObject"
                            or "MessagePackObjectAttribute"
                            or "MessagePack.MessagePackObject"
                            or "MessagePack.MessagePackObjectAttribute"
                            or "Union"
                            or "UnionAttribute"
                            or "MessagePack.Union"
                            or "MessagePack.UnionAttribute");
                    if (hasAttribute)
                    {
                        ClassDeclarations.Add(typeSyntax);
                    }
                }
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

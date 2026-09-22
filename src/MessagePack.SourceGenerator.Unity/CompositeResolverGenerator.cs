// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using MessagePack.SourceGenerator.Transforms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MessagePack.SourceGenerator.Constants;

namespace MessagePack.SourceGenerator;

[Generator]
public class CompositeResolverGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver { ClassDeclarationSyntaxes: { Count: > 0 } } receiver)
        {
            return;
        }

        // Search for a resolver generator attribute, which may be applied to any type in the compilation.
        AnalyzerOptions? options = new() { IsGeneratingSource = true };
        foreach (var classDeclByDocument in receiver.ClassDeclarationSyntaxes.GroupBy(td => td.SyntaxTree))
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclByDocument.Key, ignoreAccessibility: true);
            foreach (TypeDeclarationSyntax typeDecl in classDeclByDocument)
            {
                if (semanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken) is INamedTypeSymbol typeSymbol)
                {
                    if (ParseCompositeResolverAttribute(typeSymbol.GetAttributes()) is { } resolverTypes)
                    {
                        CompositeResolverTemplate generator = new()
                        {
                            ResolverName = typeSymbol.Name,
                            ResolverNamespace = typeSymbol.ContainingNamespace.Name,
                            ResolverInstanceExpressions = AnalyzerUtilities.ResolverSymbolToInstanceExpression(semanticModel, resolverTypes).ToArray(),
                        };
                        context.AddSource(generator.FileName, generator.TransformText());
                    }
                }
            }
        }
    }

    private static INamedTypeSymbol?[]? ParseCompositeResolverAttribute(ImmutableArray<AttributeData> attributes)
    {
        AttributeData? attribute = attributes.SingleOrDefault(ad =>
            ad.AttributeClass?.Name == CompositeResolverAttributeName &&
            ad.AttributeClass?.ContainingNamespace.Name == AttributeNamespace);
        if (attribute?.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
        {
            return attribute.ConstructorArguments[0].Values.Select(tc => tc.Value as INamedTypeSymbol).ToArray();
        }

        return null;
    }

    private class SyntaxReceiver : ISyntaxReceiver
    {
        internal List<ClassDeclarationSyntax> ClassDeclarationSyntaxes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl)
            {
                this.ClassDeclarationSyntaxes.Add(classDecl);
            }
        }
    }
}

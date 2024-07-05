// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Transforms;
using Microsoft.CodeAnalysis;
using static MessagePack.SourceGenerator.Constants;

namespace MessagePack.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class CompositeResolverGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeData = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{CompositeResolverAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => (
                ResolverNamespace: context.TargetSymbol.ContainingNamespace.GetFullNamespaceName() ?? string.Empty,
                ResolverName: context.TargetSymbol.Name,
                Attribute: context.Attributes.Single()));

        var resolvers = attributeData.Combine(context.CompilationProvider).Select((leftRight, cancellationToken) =>
        {
            var source = leftRight.Left;
            var compilation = leftRight.Right;

            if (source.Attribute.ConstructorArguments.Length > 0 && source.Attribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                // Get the semantic model we'll use for accessibility checks.
                // We'll be accessing these members from a new source file, so it doesn't matter
                // which existing syntax tree's semantic model we use to test for accessibility.
                SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
                string[] creationExpressions = AnalyzerUtilities.ResolverSymbolToInstanceExpression(
                    semanticModel,
                    source.Attribute.ConstructorArguments[0].Values.Select(tc => tc.Value as INamedTypeSymbol)).ToArray();

                return (source.ResolverName, source.ResolverNamespace, CreationExpressions: creationExpressions);
            }
            else
            {
                return (source.ResolverName, source.ResolverNamespace, CreationExpressions: Array.Empty<string>());
            }
        });

        context.RegisterSourceOutput(resolvers, (context, source) =>
        {
            CompositeResolverTemplate generator = new()
            {
                ResolverName = source.ResolverName,
                ResolverNamespace = source.ResolverNamespace,
                ResolverInstanceExpressions = source.CreationExpressions,
            };
            context.AddSource(generator.FileName, generator.TransformText());
        });
    }
}

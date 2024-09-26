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
                ResolverName: new QualifiedNamedTypeName((INamedTypeSymbol)context.TargetSymbol),
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
                string[] resolverCreationExpressions = AnalyzerUtilities.ResolverSymbolToInstanceExpression(
                    semanticModel,
                    source.Attribute.ConstructorArguments[0].Values.Select(tc => tc.Value as INamedTypeSymbol)).ToArray();
                string[] formatterCreationExpressions = AnalyzerUtilities.FormatterSymbolToInstanceExpression(
                    semanticModel,
                    source.Attribute.ConstructorArguments[0].Values.Select(tc => tc.Value as INamedTypeSymbol)).ToArray();

                return (source.ResolverName, ResolverCreationExpressions: resolverCreationExpressions, FormatterCreationExpressions: formatterCreationExpressions);
            }
            else
            {
                return (source.ResolverName, ResolverCreationExpressions: Array.Empty<string>(), FormatterCreationExpressions: Array.Empty<string>());
            }
        });

        context.RegisterSourceOutput(resolvers, (context, source) =>
        {
            CompositeResolverTemplate generator = new()
            {
                ResolverName = source.ResolverName,
                ResolverInstanceExpressions = source.ResolverCreationExpressions,
                FormatterInstanceExpressions = source.FormatterCreationExpressions,
            };
            context.AddSource(generator.FileName, generator.TransformText());
        });
    }
}

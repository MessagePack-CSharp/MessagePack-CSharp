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

        IncrementalValueProvider<AnalyzerOptions> options = GeneratorUtilities.GetAnalyzerOption(context);

        var resolvers = attributeData.Combine(context.CompilationProvider).Select((leftRight, cancellationToken) =>
        {
            var source = leftRight.Left;
            var compilation = leftRight.Right;

            bool includeLocalFormatters = source.Attribute.NamedArguments.FirstOrDefault(kv => kv.Key == CompositeResolverAttributeIncludeLocalFormattersPropertyName).Value.Value is true;

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

                return (source.ResolverName, ResolverCreationExpressions: resolverCreationExpressions, FormatterCreationExpressions: formatterCreationExpressions, IncludeLocalFormatters: includeLocalFormatters);
            }
            else
            {
                return (source.ResolverName, ResolverCreationExpressions: Array.Empty<string>(), FormatterCreationExpressions: Array.Empty<string>(), IncludeLocalFormatters: includeLocalFormatters);
            }
        });

        context.RegisterSourceOutput(resolvers.Combine(options), (context, source) =>
        {
            AnalyzerOptions options = source.Right;

            string[] formatterCreationExpressions = source.Left.FormatterCreationExpressions;
            string[] resolverCreationExpressions = source.Left.ResolverCreationExpressions;
            if (source.Left.IncludeLocalFormatters)
            {
                HashSet<string> allFormatters = new(formatterCreationExpressions, StringComparer.Ordinal);
                allFormatters.UnionWith(options.KnownFormatters.Select(f => f.InstanceExpression));
                formatterCreationExpressions = allFormatters.ToArray();

                HashSet<string> allResolvers = new(resolverCreationExpressions, StringComparer.Ordinal);
                allResolvers.Add($"{options.Generator.Resolver.Name.GetQualifiedName()}.Instance");
                resolverCreationExpressions = allResolvers.ToArray();
            }

            CompositeResolverTemplate generator = new()
            {
                ResolverName = source.Left.ResolverName,
                ResolverInstanceExpressions = resolverCreationExpressions,
                FormatterInstanceExpressions = formatterCreationExpressions,
            };
            context.AddSource(generator.FileName, generator.TransformText());
        });
    }
}

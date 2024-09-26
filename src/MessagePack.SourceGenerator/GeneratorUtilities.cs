// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MessagePack.SourceGenerator.Constants;

namespace MessagePack.SourceGenerator;

internal static class GeneratorUtilities
{
    internal static IncrementalValueProvider<AnalyzerOptions> GetAnalyzerOption(IncrementalGeneratorInitializationContext context)
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

        // Search for a [GeneratedMessagePackResolver] attribute (presumably on a partial class).
        var resolverOptions = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{AttributeNamespace}.{GeneratedMessagePackResolverAttributeName}",
            predicate: static (node, ct) => true,
            transform: static (context, ct) => AnalyzerUtilities.ParseGeneratorAttribute(context.Attributes, context.TargetSymbol, ct)).Collect().Select((a, ct) => a.SingleOrDefault(ao => ao is not null));

        // Assembly an aggregating AnalyzerOptions object from the attributes and interface implementations that we've found.
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

        return options;
    }
}

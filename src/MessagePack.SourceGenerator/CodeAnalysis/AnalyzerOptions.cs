// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Options for the analyzer and source generator.
/// </summary>
/// <remarks>
/// These options are typically gathered from attributes in the compilation.
/// </remarks>
public record AnalyzerOptions
{
    /// <summary>
    /// Gets the set fully qualified names of types that are assumed to have custom formatters written that will be included by a resolver by the program.
    /// </summary>
    public ImmutableHashSet<string> AssumedFormattableTypes { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Gets the set of custom formatters that should be considered by the analyzer and included in the generated resolver.
    /// </summary>
    public ImmutableHashSet<CustomFormatter> KnownFormatters { get; init; } = ImmutableHashSet<CustomFormatter>.Empty;

    public GeneratorOptions Generator { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the analyzer is generating source code.
    /// </summary>
    public bool IsGeneratingSource { get; init; }

    internal AnalyzerOptions WithFormatterTypes(ImmutableArray<string> formattableTypes, ImmutableHashSet<CustomFormatter> customFormatters)
    {
        return this with
        {
            AssumedFormattableTypes = ImmutableHashSet.CreateRange(formattableTypes).Union(customFormatters.SelectMany(t => t.FormattableTypes)),
            KnownFormatters = customFormatters,
        };
    }

    /// <summary>
    /// Modifies these options based on the attributes on the assembly being compiled.
    /// </summary>
    /// <param name="assemblyAttributes">The assembly-level attributes.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The modified set of options.</returns>
    internal AnalyzerOptions WithAssemblyAttributes(ImmutableArray<AttributeData> assemblyAttributes, CancellationToken cancellationToken)
    {
        ImmutableHashSet<CustomFormatter> customFormatters = AnalyzerUtilities.ParseKnownFormatterAttribute(assemblyAttributes, cancellationToken).Union(this.KnownFormatters);
        ImmutableArray<string> customFormattedTypes = this.AssumedFormattableTypes.Union(AnalyzerUtilities.ParseAssumedFormattableAttribute(assemblyAttributes, cancellationToken)).ToImmutableArray();
        return this.WithFormatterTypes(customFormattedTypes, customFormatters);
    }
}

/// <summary>
/// Customizes aspects of source generated formatters.
/// </summary>
public record FormattersOptions
{
    /// <summary>
    /// Gets a value indicating whether types will be serialized with their property names as well as their values in a key=value dictionary, as opposed to an array of values.
    /// </summary>
    public bool UsesMapMode { get; init; }
}

/// <summary>
/// Describes the generated resolver.
/// </summary>
public record ResolverOptions
{
    /// <summary>
    /// Gets the name to use for the resolver.
    /// </summary>
    public string Name { get; init; } = "GeneratedMessagePackResolver";

    /// <summary>
    /// Gets the namespace the source generated resolver will be emitted into.
    /// </summary>
    public string? Namespace { get; init; } = "MessagePack";
}

/// <summary>
/// Customizes AOT source generation of formatters for custom types.
/// </summary>
public record GeneratorOptions
{
    /// <summary>
    /// Gets options for the generated resolver.
    /// </summary>
    public ResolverOptions Resolver { get; init; } = new();

    /// <summary>
    /// Gets options for the generated formatter.
    /// </summary>
    public FormattersOptions Formatters { get; init; } = new();
}

/// <summary>
/// Describes a custom formatter.
/// </summary>
/// <param name="FormatterFullName">The full name of the type that implements at least one <c>IMessagePackFormatter</c> interface.</param>
/// <param name="FormattableTypes">The type arguments that appear in each implemented <c>IMessagePackFormatter</c> interface.</param>
/// <param name="Arity">The number of generic type parameters on the formatter. This must match the generic type parameters on all the <paramref name="FormattableTypes"/> too.</param>
public record CustomFormatter(string FormatterFullName, ImmutableHashSet<string> FormattableTypes, int Arity);

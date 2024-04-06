// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public ImmutableHashSet<FormattableType> AssumedFormattableTypes { get; init; } = ImmutableHashSet<FormattableType>.Empty;

    /// <summary>
    /// Gets the set of custom formatters that should be considered by the analyzer and included in the generated resolver.
    /// </summary>
    public ImmutableHashSet<CustomFormatter> KnownFormatters { get; init; } = ImmutableHashSet<CustomFormatter>.Empty;

    public GeneratorOptions Generator { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the analyzer is generating source code.
    /// </summary>
    public bool IsGeneratingSource { get; init; }

    internal AnalyzerOptions WithFormatterTypes(ImmutableArray<FormattableType> formattableTypes, ImmutableHashSet<CustomFormatter> customFormatters)
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
        ImmutableArray<FormattableType> customFormattedTypes = this.AssumedFormattableTypes.Union(AnalyzerUtilities.ParseAssumedFormattableAttribute(assemblyAttributes, cancellationToken)).ToImmutableArray();
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
/// <param name="Name">The name (without namespace) of the type that implements at least one <c>IMessagePackFormatter</c> interface. If the formatter is a generic type, this should <em>not</em> include any generic type parameters.</param>
/// <param name="FormattableTypes">The type arguments that appear in each implemented <c>IMessagePackFormatter</c> interface. When generic, these should be the full name of their type definitions.</param>
public record CustomFormatter(QualifiedTypeName Name, ImmutableHashSet<FormattableType> FormattableTypes)
{
    public static bool TryCreate(INamedTypeSymbol type, [NotNullWhen(true)] out CustomFormatter? formatter)
    {
        var formattedTypes =
            AnalyzerUtilities.SearchTypeForFormatterImplementations(type)
            .Select(i => new FormattableType(i))
            .ToImmutableHashSet();
        if (formattedTypes.IsEmpty)
        {
            formatter = null;
            return false;
        }

        formatter = new CustomFormatter(new QualifiedTypeName(type), formattedTypes);
        return true;
    }
}

/// <summary>
/// Describes a formattable type.
/// </summary>
/// <param name="Name">The name of the formattable type.</param>
public record FormattableType(QualifiedTypeName Name)
{
    public FormattableType(ITypeSymbol type)
        : this(new QualifiedTypeName(type))
    {
    }
}

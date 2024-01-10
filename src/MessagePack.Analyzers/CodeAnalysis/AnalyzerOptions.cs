// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.Analyzers.CodeAnalysis;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Options for the analyzer and source generator, which may be deserialized from a MessagePackAnalyzer.json file.
/// </summary>
public record AnalyzerOptions
{
    public const string RootNamespace = "build_property.RootNamespace";
    public const string JsonOptionsFileName = "MessagePackAnalyzer.json";

    public static readonly AnalyzerOptions Default = new AnalyzerOptions();

    /// <summary>
    /// Gets an array of fully-qualified names of types that are included in serialized object graphs but are assumed to have custom formatters registered already.
    /// </summary>
    public ImmutableHashSet<string> CustomFormattedTypes { get; init; } = ImmutableHashSet<string>.Empty;

    public GeneratorOptions Generator { get; init; } = new();

    public string FormatterNamespace => this.Generator.Formatters.Namespace;

    /// <summary>
    /// Gets a value indicating whether the analyzer is generating source code.
    /// </summary>
    public bool IsGeneratingSource { get; init; }

    public static AnalyzerOptions Parse(AnalyzerConfigOptions options, ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken)
    {
        // The default namespace for the resolver comes from the project root namespace.
        AnalyzerOptions result = Default;

        if (additionalTexts.FirstOrDefault(x => string.Equals(Path.GetFileName(x.Path), JsonOptionsFileName, StringComparison.OrdinalIgnoreCase))?.GetText(cancellationToken)?.ToString() is string configJson)
        {
            try
            {
                result = JsonSerializer.Deserialize<AnalyzerOptions>(
                    configJson,
                    new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true,
                        MaxDepth = 5,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                    }) ?? Default;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Can't load MessagePackAnalyzer.json: " + ex);
            }
        }

        if (result.Generator.Resolver.Namespace is null)
        {
            if (!options.TryGetValue(RootNamespace, out string? resolverNamespace))
            {
                resolverNamespace = "MessagePack";
            }

            result = result with { Generator = result.Generator with { Resolver = result.Generator.Resolver with { Namespace = resolverNamespace } } };
        }

        return result;
    }
}

/// <summary>
/// Customizes aspects of source generated formatters.
/// </summary>
public record FormattersOptions
{
    /// <summary>
    /// The default options.
    /// </summary>
    public static readonly FormattersOptions Default = new();

    /// <summary>
    /// Gets the root namespace into which formatters are emitted.
    /// </summary>
    public string Namespace { get; init; } = "Formatters";
}

/// <summary>
/// Describes the generated resolver.
/// </summary>
public record ResolverOptions
{
    /// <summary>
    /// The default options.
    /// </summary>
    public static readonly ResolverOptions Default = new();

    /// <summary>
    /// Gets a value indicating whether the generated resolver should be public (as opposed to internal).
    /// A public resolver is appropriate when developing a library that may be used by another assembly that needs to aggregate this generated resolver with others.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Public { get; init; }

    /// <summary>
    /// Gets the name to use for the resolver.
    /// </summary>
    public string Name { get; init; } = "GeneratedMessagePackResolver";

    /// <summary>
    /// Gets the namespace the source generated resolver will be emitted into.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Namespace { get; init; }
}

/// <summary>
/// Customizes AOT source generation of formatters for custom types.
/// </summary>
public record GeneratorOptions
{
    /// <summary>
    /// The default options.
    /// </summary>
    public static readonly GeneratorOptions Default = new();

    /// <summary>
    /// Gets a value indicating whether types will be serialized with their property names as well as their values in a key=value dictionary, as opposed to an array of values.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UsesMapMode { get; init; }

    /// <summary>
    /// Gets options for the generated resolver.
    /// </summary>
    public ResolverOptions Resolver { get; init; } = new();

    /// <summary>
    /// Gets options for the generated formatter.
    /// </summary>
    public FormattersOptions Formatters { get; init; } = new();
}

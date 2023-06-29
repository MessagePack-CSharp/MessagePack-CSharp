// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.Analyzers.CodeAnalysis;

public record AnalyzerOptions(
    string ResolverNamespace = "MessagePack",
    string ResolverName = "GeneratedMessagePackResolver",
    string ProjectRootNamespace = "",
    bool PublicResolver = false,
    bool UsesMapMode = false,
    ImmutableHashSet<string>? AdditionalAllowTypes = null)
{
    public const string RootNamespace = "build_property.RootNamespace";
    public const string PublicMessagePackGeneratedResolver = "build_property.PublicMessagePackGeneratedResolver";
    public const string MessagePackGeneratedResolverNamespace = "build_property.MessagePackGeneratedResolverNamespace";
    public const string MessagePackGeneratedResolverName = "build_property.MessagePackGeneratedResolverName";
    public const string MessagePackGeneratedUsesMapMode = "build_property.MessagePackGeneratedUsesMapMode";
    public const string JsonOptionsFileName = "MessagePack.json";
    public const string JsonAdditionalAllowTypesOptionsFileName = "MessagePackAnalyzer.json";

    public static readonly AnalyzerOptions Default = new AnalyzerOptions();

    public string FormatterNamespace => "Formatters";

    /// <summary>
    /// Gets a value indicating whether the analyzer is generating source code.
    /// </summary>
    public bool IsGeneratingSource { get; init; }

    public static AnalyzerOptions Parse(AnalyzerConfigOptions options, ImmutableArray<AdditionalText> additionalTexts)
    {
        var additionalOptions = ParseAdditionalTexts(additionalTexts);

        if (!options.TryGetValue(RootNamespace, out var projectRootNamespace) && !additionalOptions.TryGetValue(nameof(RootNamespace), out projectRootNamespace))
        {
            projectRootNamespace = Default.ProjectRootNamespace;
        }

        if (!options.TryGetValue(MessagePackGeneratedResolverNamespace, out var resolverNamespace) && !additionalOptions.TryGetValue(nameof(MessagePackGeneratedResolverNamespace), out resolverNamespace))
        {
            resolverNamespace = Default.ResolverNamespace;
        }

        if (!options.TryGetValue(MessagePackGeneratedResolverName, out var resolverName) && !additionalOptions.TryGetValue(nameof(MessagePackGeneratedResolverName), out resolverName))
        {
            resolverName = Default.ResolverName;
        }

        if (!options.TryGetValue(MessagePackGeneratedUsesMapMode, out var usesMapMode) && !additionalOptions.TryGetValue(nameof(MessagePackGeneratedUsesMapMode), out usesMapMode))
        {
            usesMapMode = Default.UsesMapMode ? "true" : "false";
        }

        if (!options.TryGetValue(PublicMessagePackGeneratedResolver, out var publicResolver) && !additionalOptions.TryGetValue(nameof(PublicMessagePackGeneratedResolver), out publicResolver))
        {
            publicResolver = Default.PublicResolver ? "true" : "false";
        }

        return new AnalyzerOptions(
            ResolverNamespace: resolverNamespace,
            ResolverName: resolverName,
            ProjectRootNamespace: projectRootNamespace,
            PublicResolver: string.Equals(publicResolver, "true", StringComparison.OrdinalIgnoreCase),
            UsesMapMode: string.Equals(usesMapMode, "true", StringComparison.OrdinalIgnoreCase),
            AdditionalAllowTypes: GetAdditionalAllowTypes(additionalTexts));
    }

    private static ImmutableDictionary<string, string> ParseAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
    {
        var config = additionalTexts.FirstOrDefault(x => string.Equals(Path.GetFileName(x.Path), JsonOptionsFileName, StringComparison.OrdinalIgnoreCase));
        if (config is null)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        try
        {
            var json = JsonDocument.Parse(config.GetText()?.ToString() ?? string.Empty, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, MaxDepth = 5 });
            var options = ImmutableDictionary.CreateBuilder<string, string>();
            if (json.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var element in json.RootElement.EnumerateObject())
                {
                    if (element.Name.Length > 0 && element.Value.ToString() is var value && value?.Length > 0)
                    {
                        options.Add(element.Name, value);
                    }
                }
            }

            return options.ToImmutable();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Can't load MessagePackAnalyzer.json: " + ex);
            return ImmutableDictionary<string, string>.Empty;
        }
    }

    private static ImmutableHashSet<string> GetAdditionalAllowTypes(ImmutableArray<AdditionalText> additionalTexts)
    {
        AdditionalText? config = additionalTexts.FirstOrDefault(x => string.Equals(Path.GetFileName(x.Path), JsonAdditionalAllowTypesOptionsFileName, StringComparison.OrdinalIgnoreCase));
        if (config is null)
        {
            return ImmutableHashSet<string>.Empty;
        }

        try
        {
            JsonDocument json = JsonDocument.Parse(config.GetText()?.ToString() ?? string.Empty, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, MaxDepth = 5 });
            var allowTypes = ImmutableHashSet.CreateBuilder<string>();
            if (json.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in json.RootElement.EnumerateArray())
                {
                    if (element.GetString() is string { Length: > 0 } allowType)
                    {
                        allowTypes.Add(allowType);
                    }
                }
            }

            return allowTypes.ToImmutable();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Can't load MessagePackAnalyzer.json: " + ex);
            return ImmutableHashSet<string>.Empty;
        }
    }
}

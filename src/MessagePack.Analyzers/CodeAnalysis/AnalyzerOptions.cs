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
    public const string JsonOptionsFileName = "MessagePackAnalyzer.json";

    public static readonly AnalyzerOptions Default = new AnalyzerOptions();

    public string FormatterNamespace => "Formatters";

    public static AnalyzerOptions Parse(AnalyzerConfigOptions options, ImmutableArray<AdditionalText> additionalTexts)
    {
        if (!options.TryGetValue(RootNamespace, out string? projectRootNamespace))
        {
            projectRootNamespace = Default.ProjectRootNamespace;
        }

        if (!options.TryGetValue(MessagePackGeneratedResolverNamespace, out string? resolverNamespace))
        {
            resolverNamespace = Default.ResolverNamespace;
        }

        if (!options.TryGetValue(MessagePackGeneratedResolverName, out string? resolverName))
        {
            resolverName = Default.ResolverName;
        }

        if (!options.TryGetValue(MessagePackGeneratedUsesMapMode, out string? usesMapMode))
        {
            usesMapMode = Default.UsesMapMode ? "true" : "false";
        }

        if (!options.TryGetValue(PublicMessagePackGeneratedResolver, out string? publicResolver))
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

    private static ImmutableHashSet<string> GetAdditionalAllowTypes(ImmutableArray<AdditionalText> additionalTexts)
    {
        Microsoft.CodeAnalysis.AdditionalText? config = additionalTexts.FirstOrDefault(x => string.Equals(Path.GetFileName(x.Path), JsonOptionsFileName, StringComparison.OrdinalIgnoreCase));
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

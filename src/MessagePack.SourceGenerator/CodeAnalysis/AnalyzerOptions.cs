// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record AnalyzerOptions(
    string ResolverNamespace = "MessagePack",
    string ResolverName = "GeneratedMessagePackResolver",
    string ProjectRootNamespace = "",
    bool PublicResolver = false,
    bool UsesMapMode = false,
    IReadOnlyCollection<string>? IgnoreTypeNames = null)
{
    public const string RootNamespace = "build_property.RootNamespace";
    public const string PublicMessagePackGeneratedResolver = "build_property.PublicMessagePackGeneratedResolver";
    public const string MessagePackGeneratedResolverNamespace = "build_property.MessagePackGeneratedResolverNamespace";
    public const string MessagePackGeneratedResolverName = "build_property.MessagePackGeneratedResolverName";
    public const string MessagePackGeneratedUsesMapMode = "build_property.MessagePackGeneratedUsesMapMode";

    public static readonly AnalyzerOptions Default = new AnalyzerOptions();

    public string FormatterNamespace => "Formatters";

    public static AnalyzerOptions Parse(AnalyzerConfigOptions options)
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
            UsesMapMode: string.Equals(usesMapMode, "true", StringComparison.OrdinalIgnoreCase));
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.Generator.CodeAnalysis;

public record AnalyzerOptions(
    string Namespace = "MessagePack",
    string ResolverName = "GeneratedResolver",
    bool UsesMapMode = false,
    bool DisallowInternal = false,
    IReadOnlyCollection<string>? IgnoreTypeNames = null)
{
    public const string MessagePackGeneratedResolverNamespace = "build_property.MessagePackGeneratedResolverNamespace";
    public const string MessagePackGeneratedResolverName = "build_property.MessagePackGeneratedResolverName";
    public const string MessagePackGeneratedUsesMapMode = "build_property.MessagePackGeneratedUsesMapMode";

    public static readonly AnalyzerOptions Default = new AnalyzerOptions();

    public string ResolverNamespace => $"{Namespace}.Resolvers";

    public string FormatterNamespace => $"{Namespace}.Formatters";

    public static AnalyzerOptions Parse(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(MessagePackGeneratedResolverNamespace, out string? @namespace))
        {
            @namespace = Default.Namespace;
        }

        if (!options.TryGetValue(MessagePackGeneratedResolverName, out string? resolverName))
        {
            resolverName = Default.ResolverName;
        }

        if (!options.TryGetValue(MessagePackGeneratedUsesMapMode, out string? usesMapMode))
        {
            usesMapMode = Default.UsesMapMode ? "true" : "false";
        }

        return new AnalyzerOptions(@namespace, resolverName, string.Equals(usesMapMode, "true", StringComparison.OrdinalIgnoreCase));
    }
}

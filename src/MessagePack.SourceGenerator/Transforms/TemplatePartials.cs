// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

namespace MessagePack.SourceGenerator.Transforms;

public partial class FormatterTemplate : IFormatterTemplate
{
    public FormatterTemplate(AnalyzerOptions options, ObjectSerializationInfo info)
    {
        this.Options = options;
        this.Info = info;
    }

    public AnalyzerOptions Options { get; }

    public string? FormattedTypeNamespace => this.Info.Namespace;

    public string ResolverNamespace => this.Options.Generator.Resolver.Namespace ?? string.Empty;

    public string ResolverName => this.Options.Generator.Resolver.Name;

    public ObjectSerializationInfo Info { get; }

    public string FileName => $"{this.Info.FileNameHint}.g.cs";
}

public partial class StringKeyFormatterTemplate : IFormatterTemplate
{
    public StringKeyFormatterTemplate(AnalyzerOptions options, ObjectSerializationInfo info)
    {
        this.Options = options;
        this.Info = info;
    }

    public AnalyzerOptions Options { get; }

    public string? FormattedTypeNamespace => this.Info.Namespace;

    public string ResolverNamespace => this.Options.Generator.Resolver.Namespace ?? string.Empty;

    public string ResolverName => this.Options.Generator.Resolver.Name;

    public ObjectSerializationInfo Info { get; }

    public string FileName => $"{this.Info.FileNameHint}.g.cs";
}

public partial class ResolverTemplate
{
    public ResolverTemplate(AnalyzerOptions options, IReadOnlyList<IResolverRegisterInfo> registerInfos)
    {
        this.Options = options;
        this.RegisterInfos = registerInfos;
    }

    public AnalyzerOptions Options { get; init; }

    public string ResolverNamespace => this.Options.Generator.Resolver.Namespace ?? string.Empty;

    public string ResolverName => this.Options.Generator.Resolver.Name;

    public IReadOnlyList<IResolverRegisterInfo> RegisterInfos { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.ResolverName, this.ResolverNamespace)}.g.cs";
}

public partial class EnumTemplate : IFormatterTemplate
{
    public EnumTemplate(AnalyzerOptions options, EnumSerializationInfo info)
    {
        this.Options = options;
        this.Info = info;
    }

    public AnalyzerOptions Options { get; }

    public string? FormattedTypeNamespace => this.Info.Namespace;

    public string ResolverNamespace => this.Options.Generator.Resolver.Namespace ?? string.Empty;

    public string ResolverName => this.Options.Generator.Resolver.Name;

    public EnumSerializationInfo Info { get; }

    public string FileName => $"{this.Info.FileNameHint}.g.cs";
}

public partial class UnionTemplate : IFormatterTemplate
{
    public UnionTemplate(AnalyzerOptions options, UnionSerializationInfo info)
    {
        this.Options = options;
        this.Info = info;
    }

    public AnalyzerOptions Options { get; }

    public string? FormattedTypeNamespace => this.Info.Namespace;

    public string ResolverNamespace => this.Options.Generator.Resolver.Namespace ?? string.Empty;

    public string ResolverName => this.Options.Generator.Resolver.Name;

    public UnionSerializationInfo Info { get; }

    public string FileName => $"{this.Info.FileNameHint}.g.cs";
}

public partial class CompositeResolverTemplate : IFormatterTemplate
{
    public string FileName => $"{this.ResolverName}.g.cs";

    public string? FormattedTypeNamespace => null;

    public required string ResolverNamespace { get; init; }

    public required string ResolverName { get; init; }

    public required string[] ResolverInstanceExpressions { get; init; }
}

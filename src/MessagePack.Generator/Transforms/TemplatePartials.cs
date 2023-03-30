// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using MessagePack.Generator.CodeAnalysis;

namespace MessagePack.Generator.Transforms;

public partial class FormatterTemplate : IFormatterTemplate
{
    public FormatterTemplate(AnalyzerOptions options, ObjectSerializationInfo info)
    {
        this.Namespace = CodeAnalysisUtilities.AppendNameToNamespace(options.FormatterNamespace, info.Namespace);
        this.Options = options;
        this.Info = info;
    }

    public string Namespace { get; }

    public AnalyzerOptions Options { get; }

    public ObjectSerializationInfo Info { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Info.FormatterName, this.Options.FormatterNamespace)}.g.cs";
}

public partial class StringKeyFormatterTemplate : IFormatterTemplate
{
    public StringKeyFormatterTemplate(AnalyzerOptions options, ObjectSerializationInfo info)
    {
        this.Namespace = CodeAnalysisUtilities.AppendNameToNamespace(options.FormatterNamespace, info.Namespace);
        this.Options = options;
        this.Info = info;
    }

    public string Namespace { get; }

    public AnalyzerOptions Options { get; }

    public ObjectSerializationInfo Info { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Info.FormatterName, this.Options.FormatterNamespace)}.g.cs";
}

public partial class ResolverTemplate
{
    public ResolverTemplate(AnalyzerOptions options, IReadOnlyList<IResolverRegisterInfo> registerInfos)
    {
        this.Options = options;
        this.RegisterInfos = registerInfos;
    }

    public AnalyzerOptions Options { get; init; }

    public string ResolverNamespace => this.Options.ResolverNamespace;

    public string FormatterNamespace => this.Options.FormatterNamespace;

    public string ResolverName => this.Options.ResolverName;

    public bool PublicResolver => this.Options.PublicResolver;

    public IReadOnlyList<IResolverRegisterInfo> RegisterInfos { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Options.ResolverName, this.Options.ResolverNamespace)}.g.cs";
}

public partial class EnumTemplate
{
    public EnumTemplate(AnalyzerOptions options, EnumSerializationInfo info)
    {
        this.Namespace = CodeAnalysisUtilities.AppendNameToNamespace(options.FormatterNamespace, info.Namespace);
        this.Options = options;
        this.Info = info;
    }

    public string Namespace { get; }

    public AnalyzerOptions Options { get; }

    public EnumSerializationInfo Info { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Info.FormatterName, this.Options.FormatterNamespace)}.g.cs";
}

public partial class UnionTemplate
{
    public UnionTemplate(AnalyzerOptions options, UnionSerializationInfo info)
    {
        this.Namespace = CodeAnalysisUtilities.AppendNameToNamespace(options.FormatterNamespace, info.Namespace);
        this.Options = options;
        this.Info = info;
    }

    public string Namespace { get; }

    public AnalyzerOptions Options { get; }

    public UnionSerializationInfo Info { get; }

    public string FileName => $"{CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Info.FormatterName, this.Options.FormatterNamespace)}.g.cs";
}

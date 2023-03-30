// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using MessagePack.Generator.CodeAnalysis;

namespace MessagePack.Generator.Transforms;

public partial class FormatterTemplate : IFormatterTemplate
{
    public FormatterTemplate(string @namespace, ObjectSerializationInfo info)
    {
        Namespace = @namespace;
        Info = info;
    }

    public string Namespace { get; }

    public ObjectSerializationInfo Info { get; }
}

public partial class StringKeyFormatterTemplate : IFormatterTemplate
{
    public StringKeyFormatterTemplate(string @namespace, ObjectSerializationInfo info)
    {
        Namespace = @namespace;
        Info = info;
    }

    public string Namespace { get; }

    public ObjectSerializationInfo Info { get; }
}

public partial class ResolverTemplate
{
    required public AnalyzerOptions Options { get; init; }

    public string ResolverNamespace => this.Options.ResolverNamespace;

    public string FormatterNamespace => this.Options.FormatterNamespace;

    public string ResolverName => this.Options.ResolverName;

    public bool PublicResolver => this.Options.PublicResolver;

    required public IReadOnlyList<IResolverRegisterInfo> RegisterInfos { get; init; }
}

public partial class EnumTemplate
{
    public EnumTemplate(string @namespace, EnumSerializationInfo info)
    {
        Namespace = @namespace;
        Info = info;
    }

    public string Namespace { get; }

    public EnumSerializationInfo Info { get; }
}

public partial class UnionTemplate
{
    public UnionTemplate(string @namespace, UnionSerializationInfo info)
    {
        Namespace = @namespace;
        Info = info;
    }

    public string Namespace { get; }

    public UnionSerializationInfo Info { get; }
}

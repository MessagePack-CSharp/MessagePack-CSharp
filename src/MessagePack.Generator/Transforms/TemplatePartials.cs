﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using MessagePack.Generator.CodeAnalysis;

namespace MessagePack.Generator.Transforms;

public partial class FormatterTemplate : IFormatterTemplate
{
    public FormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos)
    {
        Namespace = @namespace;
        ObjectSerializationInfos = objectSerializationInfos;
    }

    public string Namespace { get; }

    public ObjectSerializationInfo[] ObjectSerializationInfos { get; }
}

public partial class StringKeyFormatterTemplate : IFormatterTemplate
{
    public StringKeyFormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos)
    {
        Namespace = @namespace;
        ObjectSerializationInfos = objectSerializationInfos;
    }

    public string Namespace { get; }

    public ObjectSerializationInfo[] ObjectSerializationInfos { get; }
}

public partial class ResolverTemplate
{
    public ResolverTemplate(string resolverNamespace, string formatterNamespace, string resolverName, IReadOnlyList<IResolverRegisterInfo> registerInfos)
    {
        ResolverNamespace = resolverNamespace;
        FormatterNamespace = formatterNamespace;
        ResolverName = resolverName;
        RegisterInfos = registerInfos;
    }

    public string ResolverNamespace { get; }

    public string FormatterNamespace { get; }

    public string ResolverName { get; }

    public IReadOnlyList<IResolverRegisterInfo> RegisterInfos { get; }
}

public partial class EnumTemplate
{
    public EnumTemplate(string @namespace, EnumSerializationInfo[] enumSerializationInfos)
    {
        Namespace = @namespace;
        EnumSerializationInfos = enumSerializationInfos;
    }

    public string Namespace { get; }

    public EnumSerializationInfo[] EnumSerializationInfos { get; }
}

public partial class UnionTemplate
{
    public UnionTemplate(string @namespace, UnionSerializationInfo[] unionSerializationInfos)
    {
        Namespace = @namespace;
        UnionSerializationInfos = unionSerializationInfos;
    }

    public string Namespace { get; }

    public UnionSerializationInfo[] UnionSerializationInfos { get; }
}
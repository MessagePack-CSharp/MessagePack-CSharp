// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public record UnionSerializationInfo(
    string? Namespace,
    string Name,
    string FullName,
    UnionSubTypeInfo[] SubTypes) : IResolverRegisterInfo
{
    public string FormatterName => CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.Name + "Formatter", this.Namespace);
}

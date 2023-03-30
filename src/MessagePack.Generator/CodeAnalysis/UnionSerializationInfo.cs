// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public record UnionSerializationInfo(
    string? Namespace,
    string Name,
    string FullName,
    UnionSubTypeInfo[] SubTypes) : IResolverRegisterInfo
{
    public string FileNameHint => $"{CodeAnalysisUtilities.AppendNameToNamespace("Formatters", this.Namespace)}.{this.FormatterNameWithoutNamespace}";

    public string FormatterName => CodeAnalysisUtilities.QualifyWithOptionalNamespace(FormatterNameWithoutNamespace, $"Formatters::{this.Namespace}");

    public string FormatterNameWithoutNamespace => this.Name + "Formatter";
}

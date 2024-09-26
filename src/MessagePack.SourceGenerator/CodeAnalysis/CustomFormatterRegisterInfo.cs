// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record CustomFormatterRegisterInfo : ResolverRegisterInfo
{
    public required FormatterDescriptor CustomFormatter { get; init; }

    public required FormattableType FormattableDataType { get; init; }

    public override string GetFormatterInstanceForResolver()
    {
        return this.CustomFormatter.InstanceProvidingMember == ".ctor"
            ? base.GetFormatterInstanceForResolver()
            : $"{this.GetFormatterNameForResolver()}.{this.CustomFormatter.InstanceProvidingMember}";
    }
}

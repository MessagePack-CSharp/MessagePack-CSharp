// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public class CustomFormatterRegisterInfo : IResolverRegisterInfo
{
    public required string FullName { get; init; }

    public required string FormatterName { get; init; }

    public required string? Namespace { get; init; }
}

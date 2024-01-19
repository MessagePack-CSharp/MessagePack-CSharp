// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public interface IResolverRegisterInfo
{
    string FullName { get; }

    string FormatterName { get; }

    string? Namespace { get; }

    IReadOnlyCollection<Diagnostic> Diagnostics { get; }
}

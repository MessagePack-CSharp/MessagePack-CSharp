// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public sealed record GenericSerializationInfo(string FullName, string FormatterName, string? Namespace, bool IsOpenGenericType) : IResolverRegisterInfo
{
    public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

    public bool Equals(GenericSerializationInfo? other)
    {
        return this.FullName.Equals(other?.FullName);
    }

    public override int GetHashCode()
    {
        return this.FullName.GetHashCode();
    }
}

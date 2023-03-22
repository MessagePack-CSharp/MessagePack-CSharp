// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public sealed record GenericSerializationInfo(string FullName, string FormatterName, bool IsOpenGenericType) : IResolverRegisterInfo
{
    public bool Equals(GenericSerializationInfo? other)
    {
        return this.FullName.Equals(other?.FullName);
    }

    public override int GetHashCode()
    {
        return this.FullName.GetHashCode();
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public sealed record UnionSerializationInfo : ResolverRegisterInfo
{
    public required ImmutableArray<UnionSubTypeInfo> SubTypes { get; init; }

    public static UnionSerializationInfo Create(INamedTypeSymbol dataType, ImmutableArray<UnionSubTypeInfo> subTypes)
    {
        ResolverRegisterInfo basicInfo = Create(dataType);
        return new UnionSerializationInfo
        {
            DataType = basicInfo.DataType,
            Formatter = basicInfo.Formatter,
            SubTypes = subTypes,
        };
    }

    public bool Equals(UnionSerializationInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other)
            && SubTypes.SequenceEqual(other.SubTypes);
    }

    public override int GetHashCode() => base.GetHashCode();
}

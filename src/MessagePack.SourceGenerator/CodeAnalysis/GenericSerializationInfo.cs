// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes a constructed generic type (one that has known type arguments)
/// that must be serializable.
/// </summary>
public sealed record GenericSerializationInfo : ResolverRegisterInfo
{
    public override bool IsUnboundGenericType => isUnboundGenericType;

    private bool isUnboundGenericType;

    public static GenericSerializationInfo Create(ITypeSymbol dataType, ResolverOptions resolverOptions, bool isUnboundGenericType = false, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        ResolverRegisterInfo basicInfo = ResolverRegisterInfo.Create(dataType, resolverOptions, formatterLocation);
        ImmutableArray<string> typeArguments = CodeAnalysisUtilities.GetTypeArguments(dataType);
        return new GenericSerializationInfo
        {
            DataType = basicInfo.DataType with { TypeParameters = typeArguments },
            Formatter = basicInfo.Formatter with { TypeParameters = typeArguments },
            isUnboundGenericType = isUnboundGenericType,
        };
    }

    public bool Equals(GenericSerializationInfo? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare all the properties by value
        return base.Equals(other)
            && this.isUnboundGenericType == other.isUnboundGenericType;
    }

    public override int GetHashCode() => throw new NotImplementedException();
}

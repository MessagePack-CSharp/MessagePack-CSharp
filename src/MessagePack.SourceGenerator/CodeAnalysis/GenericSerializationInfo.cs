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
    public override bool IsUnboundGenericType => false;

    public static new GenericSerializationInfo Create(INamedTypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        ResolverRegisterInfo basicInfo = ResolverRegisterInfo.Create(dataType, resolverOptions, formatterLocation);
        ImmutableArray<string> typeArguments = CodeAnalysisUtilities.GetTypeArguments(dataType);
        return new GenericSerializationInfo
        {
            DataType = ((QualifiedNamedTypeName)basicInfo.DataType) with { TypeParameters = typeArguments },
            Formatter = basicInfo.Formatter with { TypeParameters = typeArguments },
        };
    }
}

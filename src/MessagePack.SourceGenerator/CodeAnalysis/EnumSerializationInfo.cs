// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public sealed record EnumSerializationInfo : ResolverRegisterInfo
{
    public required string UnderlyingTypeName { get; init; }

    public string UnderlyingTypeKeyword => this.UnderlyingTypeName switch
    {
        "SByte" => "sbyte",
        "Byte" => "byte",
        "Int16" => "short",
        "UInt16" => "ushort",
        "Int32" => "int",
        "UInt32" => "uint",
        "Int64" => "long",
        "UInt64" => "ulong",
        _ => this.UnderlyingTypeName,
    };

    public static EnumSerializationInfo Create(INamedTypeSymbol dataType, ISymbol enumUnderlyingType)
    {
        ResolverRegisterInfo basicInfo = ResolverRegisterInfo.Create(dataType);
        return new EnumSerializationInfo
        {
            DataType = basicInfo.DataType,
            Formatter = basicInfo.Formatter,
            UnderlyingTypeName = enumUnderlyingType.ToDisplayString(BinaryWriteFormat),
        };
    }
}

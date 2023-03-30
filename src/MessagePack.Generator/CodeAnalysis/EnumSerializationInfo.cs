// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public sealed record EnumSerializationInfo(string? Namespace, string Name, string FullName, string UnderlyingTypeName) : IResolverRegisterInfo
{
    public string FormatterName => CodeAnalysisUtilities.NamespaceAndType(this.Name + "Formatter", this.Namespace);

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
}

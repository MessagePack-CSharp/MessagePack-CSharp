// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public sealed record EnumSerializationInfo(string? Namespace, string Name, string FullName, string UnderlyingTypeName) : IResolverRegisterInfo
{
    public string FileNameHint => $"{CodeAnalysisUtilities.AppendNameToNamespace("Formatters", this.Namespace)}.{this.FormatterName}";

    public string FormatterName => this.Name + "Formatter";

    public int UnboundArity => 0;

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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public class EnumSerializationInfo : IResolverRegisterInfo, INamespaceInfo
{
    public EnumSerializationInfo(string? @namespace, string name, string fullName, string underlyingType)
    {
        Namespace = @namespace;
        Name = name;
        FullName = fullName;
        UnderlyingType = underlyingType;
    }

    public string? Namespace { get; }

    public string Name { get; }

    public string FullName { get; }

    public string UnderlyingType { get; }

    public string FormatterName => (this.Namespace == null ? this.Name : this.Namespace + "." + this.Name) + "Formatter";
}

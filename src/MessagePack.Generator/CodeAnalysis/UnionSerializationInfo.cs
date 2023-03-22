// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public class UnionSerializationInfo : IResolverRegisterInfo
{
    public string? Namespace { get; }

    public string Name { get; }

    public string FullName { get; }

    public string FormatterName => (this.Namespace == null ? this.Name : this.Namespace + "." + this.Name) + "Formatter";

    public UnionSubTypeInfo[] SubTypes { get; }

    public UnionSerializationInfo(string? @namespace, string name, string fullName, UnionSubTypeInfo[] subTypes)
    {
        Namespace = @namespace;
        Name = name;
        FullName = fullName;
        SubTypes = subTypes;
    }
}

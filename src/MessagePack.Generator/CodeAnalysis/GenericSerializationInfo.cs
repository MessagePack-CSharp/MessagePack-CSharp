// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public class GenericSerializationInfo : IResolverRegisterInfo, IEquatable<GenericSerializationInfo>
{
    public string FullName { get; }

    public string FormatterName { get; }

    public bool IsOpenGenericType { get; }

    public bool Equals(GenericSerializationInfo? other)
    {
        return this.FullName.Equals(other?.FullName);
    }

    public override int GetHashCode()
    {
        return this.FullName.GetHashCode();
    }

    public GenericSerializationInfo(string fullName, string formatterName, bool isOpenGenericType)
    {
        FullName = fullName;
        FormatterName = formatterName;
        IsOpenGenericType = isOpenGenericType;
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public class UnionSubTypeInfo
{
    public UnionSubTypeInfo(int key, string type)
    {
        Key = key;
        Type = type;
    }

    public int Key { get; }

    public string Type { get; }
}

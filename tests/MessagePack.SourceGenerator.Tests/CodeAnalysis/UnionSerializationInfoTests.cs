// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

public class UnionSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        UnionSerializationInfo info1a = new()
        {
            DataType = new("full", TypeKind.Class, "name"),
            Formatter = new("some", TypeKind.Class, "other"),
            SubTypes = ImmutableArray.Create(new UnionSubTypeInfo(1, "hey")),
        };
        UnionSerializationInfo info1b = info1a with
        {
            SubTypes = ImmutableArray.Create(new UnionSubTypeInfo(1, "hey")),
        };
        UnionSerializationInfo info2 = info1a with { SubTypes = ImmutableArray.Create(new UnionSubTypeInfo(1, "String")) };

        Assert.Equal(info1a, info1b);
        Assert.NotEqual(info1a, info2);
    }
}

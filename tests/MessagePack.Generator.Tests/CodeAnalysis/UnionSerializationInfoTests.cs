// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class UnionSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        UnionSerializationInfo info1a = new(null, "name", "full.name", new UnionSubTypeInfo[0]);
        UnionSerializationInfo info1b = new(null, "name", "full.name", new UnionSubTypeInfo[0]);
        UnionSerializationInfo info2 = new(null, "name", "full.name", new UnionSubTypeInfo[] { new(1, "String") });

        Assert.Equal(info1a, info1b);
        Assert.NotEqual(info1a, info2);
    }
}

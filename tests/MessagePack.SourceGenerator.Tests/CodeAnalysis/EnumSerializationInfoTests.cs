// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class EnumSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        EnumSerializationInfo info1a = new(null, "name", "full.name", "System.Int32");
        EnumSerializationInfo info1b = new(null, "name", "full.name", "System.Int32");
        EnumSerializationInfo info2 = new(null, "name", "full.name", "System.Int16");

        Assert.Equal(info1a, info1b);
        Assert.NotEqual(info1a, info2);
    }
}

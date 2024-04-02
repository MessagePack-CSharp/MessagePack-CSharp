// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public class GenericSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        GenericSerializationInfo info1a = new("full.name", "FullNameFormatter", null, 0);
        GenericSerializationInfo info1b = new("full.name", "FullNameFormatter", null, 0);
        GenericSerializationInfo info2 = new("full.Name", "FullNameFormatter", null, 0);

        Assert.Equal(info1b, info1a);
        Assert.NotEqual(info2, info1a);
    }
}

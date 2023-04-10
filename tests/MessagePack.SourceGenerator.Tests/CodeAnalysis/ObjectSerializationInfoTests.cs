// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class ObjectSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        // Initialize an object with non-default data.
        ObjectSerializationInfo info1a = new(
            true,
            true,
            new GenericTypeParameterInfo[0],
            new MemberSerializationInfo[0],
            false,
            new MemberSerializationInfo[0],
            "name",
            "full.name",
            null,
            false,
            false,
            false);
        ObjectSerializationInfo info1b = new(
            true,
            true,
            new GenericTypeParameterInfo[0],
            new MemberSerializationInfo[0],
            false,
            new MemberSerializationInfo[0],
            "name",
            "full.name",
            null,
            false,
            false,
            false);

        ObjectSerializationInfo info2 = new(
            true,
            false,
            new GenericTypeParameterInfo[0],
            new MemberSerializationInfo[0],
            false,
            new MemberSerializationInfo[0],
            "name",
            "full.name",
            null,
            false,
            false,
            false);

        Assert.Equal(info1a, info1b);
        Assert.NotEqual(info1a, info2);
    }
}

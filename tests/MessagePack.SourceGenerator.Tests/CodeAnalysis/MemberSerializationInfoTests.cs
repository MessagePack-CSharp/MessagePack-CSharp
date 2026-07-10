// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public class MemberSerializationInfoTests
{
    [Fact]
    public void Equals_ByValue()
    {
        MemberSerializationInfo info1a = new(
            true,
            false,
            true,
            false,
            false,
            1,
            "Hi",
            "name",
            "SomeType",
            "shortName",
            false,
            null)
        {
            DeclaringType = null,
        };
        MemberSerializationInfo info1b = new(
           true,
           false,
           true,
           false,
           false,
           1,
           "Hi",
           "name",
           "SomeType",
           "shortName",
           false,
           null)
        {
            DeclaringType = null,
        };

        MemberSerializationInfo info2 = new(
           false,
           false,
           true,
           false,
           false,
           1,
           "Hi",
           "name",
           "SomeType",
           "shortName",
           false,
           null)
        {
            DeclaringType = null,
        };

        Assert.Equal(info1b, info1a);
        Assert.NotEqual(info2, info1a);
    }
}

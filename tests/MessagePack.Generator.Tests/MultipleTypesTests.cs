// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.Tests;

public class MultipleTypesTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public MultipleTypesTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task TwoTypes()
    {
        string testSource = """
using MessagePack;

[MessagePackObject]
public class Object1
{
}

[MessagePackObject]
public class Object2
{
}
""";
        await VerifyCS.Test.RunDefaultAsync(testSource);
    }
}

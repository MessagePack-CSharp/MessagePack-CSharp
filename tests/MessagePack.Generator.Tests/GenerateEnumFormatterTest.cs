// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class GenerateEnumFormatterTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenerateEnumFormatterTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task EnumFormatter()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace MyTestNamespace;

[MessagePackObject]
public class MyMessagePackObject
{
    [Key(0)]
    public MyEnum EnumValue { get; set; }
}

public enum MyEnum
{
    A, B, C
}
""";
        await VerifyCS.Test.RunDefaultAsync(testSource);
    }
}

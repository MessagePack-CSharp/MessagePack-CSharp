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
    public async Task EnumFormatter_InNamespace()
    {
        string testSource = """
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

    [Fact]
    public async Task EnumFormatter_Nested()
    {
        string testSource = """
using MessagePack;

public class Outer
{
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
}
""";
        await VerifyCS.Test.RunDefaultAsync(testSource);
    }

    [Fact]
    public async Task EnumFormatter_NoNamespace()
    {
        string testSource = """
using MessagePack;

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

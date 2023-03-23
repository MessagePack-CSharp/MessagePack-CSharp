// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.Tests;

public class GenerateEnumFormatterTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenerateEnumFormatterTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory, CombinatorialData]
    public async Task EnumFormatter(ContainerKind container)
    {
        string testSource = """
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
        testSource = TestUtilities.WrapTestSource(testSource, container);

        await VerifyCS.Test.RunDefaultAsync(testSource, testMethod: $"{nameof(EnumFormatter)}({container})");
    }
}

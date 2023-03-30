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

    [Theory, PairwiseData]
    public async Task EnumFormatter(ContainerKind container, bool usesMapMode)
    {
        string testSource = """
[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal MyEnum EnumValue { get; set; }
}

internal enum MyEnum
{
    A, B, C
}
""";
        testSource = TestUtilities.WrapTestSource(testSource, container);

        await VerifyCS.Test.RunDefaultAsync(testSource, options: AnalyzerOptions.Default with { UsesMapMode = usesMapMode }, testMethod: $"{nameof(EnumFormatter)}({container}, {usesMapMode})");
    }
}

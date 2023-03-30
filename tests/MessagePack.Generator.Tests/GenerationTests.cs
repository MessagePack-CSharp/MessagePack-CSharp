// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.Tests;

public class GenerationTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenerationTests(ITestOutputHelper testOutputHelper)
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

    [Theory, PairwiseData]
    public async Task UnionFormatter(ContainerKind container)
    {
        string testSource = """
[Union(0, typeof(Derived1))]
[Union(1, typeof(Derived2))]
internal interface IMyType
{
}

[MessagePackObject]
internal class Derived1 : IMyType {}

[MessagePackObject]
internal class Derived2 : IMyType {}

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal IMyType UnionValue { get; set; }
}
""";
        testSource = TestUtilities.WrapTestSource(testSource, container);

        await VerifyCS.Test.RunDefaultAsync(testSource, testMethod: $"{nameof(UnionFormatter)}({container})");
    }
}

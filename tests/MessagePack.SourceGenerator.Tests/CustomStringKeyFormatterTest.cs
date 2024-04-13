// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;
using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class CustomStringKeyFormatterTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public CustomStringKeyFormatterTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RecordWithPrimaryConstructor()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public record MyMessagePackRecord([property: Key("p")] string PhoneNumber, [property: Key("c")] int Count);
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}

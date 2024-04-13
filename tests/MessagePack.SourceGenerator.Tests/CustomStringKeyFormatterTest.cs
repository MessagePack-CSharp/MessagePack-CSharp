// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public record MyMessagePackObject([property: Key("p")] string PhoneNumber, [property: Key("c")] int Count);
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task RecordWithWithInitOnlyProps()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyMessagePackObject
    {
        public MyMessagePackObject(string phoneNumber, int count)
        {
            PhoneNumber = phoneNumber;
            Count = count;
        }

        [Key("p")]
        public string PhoneNumber { get; set; }

        [Key("c")]
        public int Count { get; set; }
    };
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}

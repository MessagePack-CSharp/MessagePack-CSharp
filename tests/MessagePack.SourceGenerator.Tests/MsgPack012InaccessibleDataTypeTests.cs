// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class MsgPack012InaccessibleDataTypeTests(ITestOutputHelper logger)
{
    [Fact]
    public async Task InaccessibleNestedDataType()
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            class Outer
            {
                [MessagePackObject]
                class {|MsgPack012:MyObject|}
                {
                    [Key(0)]
                    public int Value { get; set; }
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(logger, testSource);
    }

    [Fact]
    public async Task InaccessibleNestedDataType_OuterNesting()
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            class Outer
            {
                class {|MsgPack012:Middle|}
                {
                    [MessagePackObject]
                    internal class MyObject
                    {
                        [Key(0)]
                        public int Value { get; set; }
                    }
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(logger, testSource);
    }
}

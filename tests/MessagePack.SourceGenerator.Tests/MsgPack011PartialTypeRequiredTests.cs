// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.FormatterCodeFixProvider>;

public class MsgPack011PartialTypeRequiredTests
{
    [Fact]
    public async Task CodeFixOffered()
    {
        string testSource = """
            #pragma warning disable CS0169
            using MessagePack;

            [MessagePackObject]
            class {|MsgPack011:MyObject|}
            {
                [Key(0)]
                private int value;
            }
            """;

        string fixedSource = """
            #pragma warning disable CS0169
            using MessagePack;

            [MessagePackObject]
            partial class MyObject
            {
                [Key(0)]
                private int value;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(testSource, fixedSource);
    }
}

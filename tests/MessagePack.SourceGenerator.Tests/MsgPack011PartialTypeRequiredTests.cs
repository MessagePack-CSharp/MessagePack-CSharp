// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.FormatterCodeFixProvider>;

public class MsgPack011PartialTypeRequiredTests
{
    [Fact]
    public async Task CodeFixOffered()
    {
        string testSource = """
            #pragma warning disable CS0169
            using MessagePack;

            [{|MsgPack015:MessagePackObject|}]
            public class {|MsgPack011:MyObject|}
            {
                [Key(0)]
                private int value;
            }
            """;

        string fixedSource = """
            #pragma warning disable CS0169
            using MessagePack;

            [{|MsgPack015:MessagePackObject|}]
            public partial class MyObject
            {
                [Key(0)]
                private int value;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(testSource, fixedSource);
    }

    [Fact]
    public async Task NestedPartialTypeRequiresPartialSelfAndDeclaringType()
    {
        string testSource = """
            using MessagePack;

            [MessagePackObject]
            public class {|#1:Outer|}
            {
                [{|MsgPack015:MessagePackObject|}]
                internal class {|#0:Inner|}
                {
                    [Key(0)]
                    private Outer Value { get; set; }
                }
            }
            """;

        string fixedSource = """
            using MessagePack;

            [MessagePackObject]
            public partial class Outer
            {
                [{|MsgPack015:MessagePackObject|}]
                internal partial class Inner
                {
                    [Key(0)]
                    private Outer Value { get; set; }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError(MsgPack00xMessagePackAnalyzer.PartialTypeRequired.Id)
                        .WithLocation(0)
                        .WithLocation(1),
                },
            },
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task NestedPartialTypeRequiresPartialDeclaringType()
    {
        string testSource = """
            using MessagePack;

            [MessagePackObject]
            public class {|#1:Outer|}
            {
                [{|MsgPack015:MessagePackObject|}]
                public partial class {|#0:Inner|}
                {
                    [Key(0)]
                    private Outer Value { get; set; }
                }
            }
            """;

        string fixedSource = """
            using MessagePack;

            [MessagePackObject]
            public partial class Outer
            {
                [{|MsgPack015:MessagePackObject|}]
                public partial class Inner
                {
                    [Key(0)]
                    private Outer Value { get; set; }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError(MsgPack00xMessagePackAnalyzer.PartialTypeRequired.Id)
                        .WithLocation(0)
                        .WithLocation(1),
                },
            },
            FixedCode = fixedSource,
        }.RunAsync();
    }
}

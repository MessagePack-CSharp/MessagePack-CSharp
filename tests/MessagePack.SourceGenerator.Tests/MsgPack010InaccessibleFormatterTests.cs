// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class MsgPack010InaccessibleFormatterTests
{
    [Fact]
    public async Task InaccessibleCustomFormatterType()
    {
        string testSource = """
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                class {|#0:F|} : IMessagePackFormatter<A> {
                    public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                    public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
                }
            }
            """;
        await new VerifyCS.Test()
        {
            TestState =
            {
                Sources = { testSource },
            },
            ExpectedDiagnostics =
            {
                new DiagnosticResult(MsgPack00xMessagePackAnalyzer.InaccessibleFormatterType).WithLocation(0),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task InaccessibleCustomFormatterConstructor()
    {
        string testSource = """
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                internal class {|#0:F|} : IMessagePackFormatter<A> {
                    private F() {}
                    public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                    public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
                }
            }
            """;
        await new VerifyCS.Test()
        {
            TestState =
            {
                Sources = { testSource },
            },
            ExpectedDiagnostics =
            {
                new DiagnosticResult(MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstance).WithLocation(0),
            },
        }.RunAsync();
    }
}

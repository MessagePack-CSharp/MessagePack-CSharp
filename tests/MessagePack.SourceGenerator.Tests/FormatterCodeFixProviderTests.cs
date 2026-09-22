// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.FormatterCodeFixProvider>;

public class FormatterCodeFixProviderTests
{
    [Theory]
    [InlineData("", "internal ")]
    [InlineData("private ", "internal ")]
    [InlineData("protected ", "protected internal ")]
    [InlineData("private protected ", "internal ")]
    public async Task InaccessibleCustomFormatterType(string initialModifiers, string expectedModifiers)
    {
        string testSource = $$"""
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                {{initialModifiers}}class {|#0:F|} : IMessagePackFormatter<A> {
                    public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                    public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
                }
            }
            """;
        string fixedSource = $$"""
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                {{expectedModifiers}}class F : IMessagePackFormatter<A> {
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
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(MsgPack00xMessagePackAnalyzer.InaccessibleFormatterType).WithLocation(0),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                InheritanceMode = StateInheritanceMode.Explicit,
            },
        }.RunAsync();
    }

    [Theory]
    [InlineData("", "internal ")]
    [InlineData("private ", "internal ")]
    [InlineData("protected ", "protected internal ")]
    [InlineData("private protected ", "internal ")]
    public async Task InaccessibleCustomFormatterConstructor(string initialModifiers, string expectedModifiers)
    {
        string testSource = $$"""
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                internal class {|#0:F|} : IMessagePackFormatter<A> {
                    {{initialModifiers}}F() {}
                    public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                    public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
                }
            }
            """;
        string fixedSource = $$"""
            using MessagePack;
            using MessagePack.Formatters;
            class A {
                internal class F : IMessagePackFormatter<A> {
                    {{expectedModifiers}}F() {}
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
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstance).WithLocation(0),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                InheritanceMode = StateInheritanceMode.Explicit,
            },
        }.RunAsync();
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.FormatterCodeFixProvider>;

public class MsgPack014NullableRefTypeFormatterTests
{
    [Fact]
    public async Task RefTypeWithoutNullableGetsFixed()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            #nullable enable

            public class A { }

            public class AFormatter : IMessagePackFormatter<{|MsgPack014:A|}>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new System.NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new System.NotImplementedException();
            }
            """;

        string fixedSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            #nullable enable

            public class A { }

            public class AFormatter : IMessagePackFormatter<A?>
            {
                public void Serialize(ref MessagePackWriter writer, A? value, MessagePackSerializerOptions options) => throw new System.NotImplementedException();
                public A? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new System.NotImplementedException();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(testSource, fixedSource);
    }

    [Fact]
    public async Task RefTypeWithNullableAnnotation()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            public class A { }

            #nullable enable

            public class AFormatter : IMessagePackFormatter<A?>
            {
                public void Serialize(ref MessagePackWriter writer, A? value, MessagePackSerializerOptions options) { }
                public A? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testSource);
    }

    [Fact]
    public async Task ValueTypeWithoutNullableAnnotation()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            public struct A { }

            #nullable enable

            public class AFormatter : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) { }
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testSource);
    }

    [Fact]
    public async Task RefTypeWithoutNullableAnnotationsEnabled()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            public class A { }

            #nullable disable

            public class AFormatter : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) { }
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testSource);
    }
}

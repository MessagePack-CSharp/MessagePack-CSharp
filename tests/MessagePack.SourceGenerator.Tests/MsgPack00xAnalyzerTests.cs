// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Analyzers.CodeFixes;
using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.MessagePackCodeFixProvider>;

public class MsgPack00xAnalyzerTests
{
    private const string Preamble = @"
using MessagePack;
";

    [Fact]
    public async Task NoMessagePackReference()
    {
        string input = @"
public class Foo
{
}
";

        await VerifyCS.VerifyAnalyzerWithoutMessagePackReferenceAsync(input);
    }

    [Fact]
    public async Task MessageFormatterAttribute()
    {
        string input = Preamble + @"using MessagePack.Formatters;

public class FooFormatter : IMessagePackFormatter<Foo> {
    public void Serialize(ref MessagePackWriter writer, Foo value, MessagePackSerializerOptions options) {}
    public Foo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
}


[MessagePackFormatter(typeof(FooFormatter))]
public struct Foo
{
}

[MessagePackObject]
public class SomeClass {
    [Key(0)]
    public Foo SomeFoo { get; set; }
}
";

        await VerifyCS.VerifyAnalyzerAsync(input);
    }

    [Fact]
    public async Task InvalidMessageFormatterType()
    {
        string input = Preamble + @"using MessagePack.Formatters;

public class InvalidMessageFormatter { }

[MessagePackFormatter({|MsgPack006:typeof(InvalidMessageFormatter)|})]
public struct Foo
{
}

[MessagePackObject]
public class SomeClass {
    [Key(0)]
    public Foo SomeFoo { get; set; }
}
";

        await VerifyCS.VerifyAnalyzerAsync(input);
    }

    [Fact]
    public async Task NullStringKey()
    {
        string input = Preamble + @"
[MessagePackObject]
public class Foo
{
    [Key(null)]
    public string {|MsgPack005:Member|} { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembers()
    {
        string input = Preamble + @"
[MessagePackObject]
public class Foo
{
    public string {|MsgPack004:Member1|} { get; set; }
    public string {|MsgPack004:Member2|} { get; set; }
}
";

        string output = Preamble + @"
[MessagePackObject]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    public string Member2 { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that only public members require attributes (so long as no non-public member is already attributed.)
    /// </summary>
    [Fact]
    public async Task AddAttributesToMembers_PublicOnly()
    {
        string input = Preamble + @"
[MessagePackObject]
public class Foo
{
    public string {|MsgPack004:Member1|} { get; set; }
    internal string Member2 { get; set; }
}
";

        string output = Preamble + @"
[MessagePackObject]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    internal string Member2 { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that all members require attributes if AllowPrivate = true is set on the attribute.
    /// </summary>
    [Fact]
    public async Task AddAttributesToMembers_AllowPrivateAttribute()
    {
        string input = Preamble + @"
[MessagePackObject(AllowPrivate = true)]
public class Foo
{
    public string {|MsgPack004:Member1|} { get; set; }
    internal string {|MsgPack004:Member2|} { get; set; }
}
";

        string output = Preamble + @"
[MessagePackObject(AllowPrivate = true)]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    internal string Member2 { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that our annotated member analyzer automatically starts enforcing that
    /// non-public members are annotated after the first non-public member is annotated.
    /// </summary>
    [Fact]
    public async Task AddAttributesToMembers_MixedVisibility()
    {
        string input = Preamble + @"
[{|MsgPack015:MessagePackObject|}]
public class Foo
{
    private string {|MsgPack004:member4|};
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    internal string Member2 { get; set; }
    public string {|MsgPack004:Member3|} { get; set; }
}
";

        string output = Preamble + @"
[{|MsgPack015:MessagePackObject|}]
public class {|MsgPack011:Foo|}
{
    [Key(2)]
    private string member4;
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    internal string Member2 { get; set; }
    [Key(3)]
    public string Member3 { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact]
    public async Task AddIgnoreAttributesToMembers()
    {
        string input = Preamble + @"
[MessagePackObject]
public class Foo
{
    public string {|MsgPack004:member1 = null|};
    public string {|MsgPack004:member2 = null|};
    [Key(0)]
    public string Member3 { get; set; }
}
";

        string output = Preamble + @"
[MessagePackObject]
public class Foo
{
    [IgnoreMember]
    public string member1 = null;
    [IgnoreMember]
    public string member2 = null;
    [Key(0)]
    public string Member3 { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddIgnoreMemberAttributeEquivalenceKey,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributeToField_WithComment()
    {
        string input = Preamble + @"
[MessagePackObject]
public class Foo
{
    // comment
    public string {|MsgPack004:Member1|};
}
";

        string output = Preamble + @"
[MessagePackObject]
public class Foo
{
    // comment
    [Key(0)]
    public string Member1;
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToType_Properties()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = /* lang=c#-test */ """
            public class Foo
            {
                public string Member { get; set; }
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo {|MsgPack003:Member|} { get; set; }
            }
            """;

        string output = /* lang=c#-test */ """
            [MessagePack.MessagePackObject]
            public class Foo
            {
                [MessagePack.Key(0)]
                public string Member { get; set; }
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo Member { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToType_Fields()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = /* lang=c#-test */ """
            public class Foo
            {
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo {|MsgPack003:Member|};
            }
            """;

        string output = /* lang=c#-test */ """
            [MessagePack.MessagePackObject]
            public class Foo
            {
                [MessagePack.Key(0)]
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo Member;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToType_Nullable()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = /* lang=c#-test */ """
            public struct Foo
            {
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo? {|MsgPack003:Member|};
            }
            """;

        string output = /* lang=c#-test */ """
            [MessagePack.MessagePackObject]
            public struct Foo
            {
                [MessagePack.Key(0)]
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public Foo? Member;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToType_Generic()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = /* lang=c#-test */ """
            public struct Foo
            {
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public System.Collections.Generic.List<Foo> {|MsgPack003:Member|};
            }
            """;

        string output = /* lang=c#-test */ """
            [MessagePack.MessagePackObject]
            public struct Foo
            {
                [MessagePack.Key(0)]
                public string Member;
            }

            [MessagePack.MessagePackObject]
            public class Bar
            {
                [MessagePack.Key(0)]
                public System.Collections.Generic.List<Foo> Member;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToTypeForRecord1()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = @"
public class Foo
{
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar
{
    [MessagePack.Key(0)]
    public Foo {|MsgPack003:Member|} { get; set; }
}
";

        string output = @"
[MessagePack.MessagePackObject]
public class Foo
{
    [MessagePack.Key(0)]
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar
{
    [MessagePack.Key(0)]
    public Foo Member { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToTypeForRecord2()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = @"
public record Foo
{
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar
{
    [MessagePack.Key(0)]
    public Foo {|MsgPack003:Member|} { get; set; }
}
";

        string output = @"
[MessagePack.MessagePackObject]
public record Foo
{
    [MessagePack.Key(0)]
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar
{
    [MessagePack.Key(0)]
    public Foo Member { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToTypeForRecordPrimaryConstructor()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = @"
public class Foo
{
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar([property: MessagePack.Key(0)] Foo {|MsgPack003:Member|});

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
";

        string output = @"
[MessagePack.MessagePackObject]
public class Foo
{
    [MessagePack.Key(0)]
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar([property: MessagePack.Key(0)] Foo Member);

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact]
    public async Task CodeFixAppliesAcrossFiles()
    {
        string source1 = @"
public class Foo
{
    public int Member1 { get; set; }
}
";

        string source2 = @"using MessagePack;

[MessagePackObject]
public class Bar : {|MsgPack004:Foo|}
{
    public int {|MsgPack004:Member2|} { get; set; }
}
";

        string output1 = @"
public class Foo
{
    [MessagePack.Key(1)]
    public int Member1 { get; set; }
}
";
        string output2 = @"using MessagePack;

[MessagePackObject]
public class Bar : Foo
{
    [Key(0)]
    public int Member2 { get; set; }
}
";

        await new VerifyCS.Test(ReferencesSet.MessagePackAnnotations)
        {
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck, // BUGBUG: move diagnostic to `Foo` reference in Bar's base type list.
            TestState =
            {
                Sources = { source1, source2 },
            },
            FixedState =
            {
                Sources = { output1, output2 },
            },
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact(Skip = "need to change this test infra.")]
    public async Task AddAttributeToGenericType()
    {
        string input = Preamble + /* lang=c#-test */ """
            public class Foo<T>
            {
                public T Member { get; set; }
            }

            [MessagePackObject]
            public class Bar
            {
                [Key(0)]
                public Foo<int> {|MsgPack003:MemberUserGeneric|} { get; set; }

                [Key(1)]
                public System.Collections.Generic.List<int> MemberKnownGeneric { get; set; }
            }
            """;

        string output = Preamble + /* lang=c#-test */ """

            [MessagePackObject]
            public class Foo<T>
            {
                [Key(0)]
                public T Member { get; set; }
            }

            [MessagePackObject]
            public class Bar
            {
                [Key(0)]
                public Foo<int> MemberUserGeneric { get; set; }

                [Key(1)]
                public System.Collections.Generic.List<int> MemberKnownGeneric { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CodeActionEquivalenceKey = MessagePackCodeFixProvider.AddKeyAttributeEquivanceKey,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecord()
    {
        string input = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record Foo
            {
                public string {|MsgPack004:Member1|} { get; set; }
                public string {|MsgPack004:Member2|} { get; set; }
            }
            """;

        string output = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record Foo
            {
                [Key(0)]
                public string Member1 { get; set; }
                [Key(1)]
                public string Member2 { get; set; }
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            SolutionTransforms =
            {
                static (solution, projectId) =>
                {
                    return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp11));
                },
            },
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecordStruct()
    {
        string input = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record struct Foo
            {
                public string {|MsgPack004:Member1|} { get; set; }
                public string {|MsgPack004:Member2|} { get; set; }
            }
            """;

        string output = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record struct Foo
            {
                [Key(0)]
                public string Member1 { get; set; }
                [Key(1)]
                public string Member2 { get; set; }
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            SolutionTransforms =
            {
                static (solution, projectId) =>
                {
                    return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp11));
                },
            },
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecordWithPrimaryCtor()
    {
        string input = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record {|MsgPack007:Foo|}(
                string {|MsgPack004:Member1|},
                string {|MsgPack004:Member2|});
            """;

        string output = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record Foo(
                [property: Key(0)] string Member1,
                [property: Key(1)] string Member2);
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            SolutionTransforms =
            {
                static (solution, projectId) =>
                {
                    return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp11));
                },
            },
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecordStructWithPrimaryCtor()
    {
        string input = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record struct Foo(
                string {|MsgPack004:Member1|},
                string {|MsgPack004:Member2|});
            """;

        string output = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public record struct Foo(
                [property: Key(0)] string Member1,
                [property: Key(1)] string Member2);
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            SolutionTransforms =
            {
                static (solution, projectId) =>
                {
                    return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp11));
                },
            },
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotAddAttributesToClassWithPrimaryCtor()
    {
        string input = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public class {|MsgPack007:Foo|}(
                string Member1,
                string Member2);
            """;

        string output = Preamble + /* lang=c#-test */ """
            [MessagePackObject]
            public class {|MsgPack007:Foo|}(
                string Member1,
                string Member2);
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            SolutionTransforms =
            {
                static (solution, projectId) =>
                {
                    return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp12));
                },
            },
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task DerivedAttributes()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject]
            public class A
            {
                [{|MsgPack016:CompositeKey(0, 1)|}]
                public string Prop1 { get; set; }

                [{|MsgPack016:CompositeKey(0, 2)|}]
                public string Field2;
            }

            [MessagePackObject]
            public class B : {|MsgPack016:A|}
            {
                [{|MsgPack016:CompositeKey(1, 1)|}]
                public string Prop3 { get; set; }

                [{|MsgPack016:CompositeKey(1, 2)|}]
                public string Field4;
            }

            public class CompositeKeyAttribute : KeyAttribute
            {
                public CompositeKeyAttribute(byte level, int index)
                    : base(CreateKey(level, index)) { }

                public static string CreateKey(byte level, int index)
                {
                    var c = (char)('A' + level);
                    return c + index.ToString("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DerivedAttributes_SourceGenerationDisabled()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject(SuppressSourceGeneration = true)]
            public class A
            {
                [CompositeKey(0, 1)]
                public string Prop1 { get; set; }

                [CompositeKey(0, 2)]
                public string Field2;
            }

            [MessagePackObject(SuppressSourceGeneration = true)]
            public class B : A
            {
                [CompositeKey(1, 1)]
                public string Prop3 { get; set; }

                [CompositeKey(1, 2)]
                public string Field4;
            }

            public class CompositeKeyAttribute : KeyAttribute
            {
                public CompositeKeyAttribute(byte level, int index)
                    : base(CreateKey(level, index)) { }

                public static string CreateKey(byte level, int index)
                {
                    var c = (char)('A' + level);
                    return c + index.ToString("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InitPropertyInitializer()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject]
            public class A
            {
                [Key(0)]
                public string Prop1 { get; init; } {|MsgPack017:= "some default"|};

                [Key(1)]
                public string Prop2 { get; set; } = "some default";
            }


            [MessagePackObject(SuppressSourceGeneration = true)]
            public class B
            {
                [Key(0)]
                public string Prop1 { get; init; } = "some default";

                [Key(1)]
                public string Prop2 { get; set; } = "some default";
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PrivateMembersInBaseClassNeedNoAttribution()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject]
            public class A
            {
                private int somePrivateField;

                [Key(0)]
                public int SomePublicProp { get; set; }

                private int SomePrivateProp { get; set; }
            }

            [MessagePackObject]
            public class B : A
            {
                [Key(1)]
                public int SomePublicProp2 { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NameCollisionsInForceMapMode()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject(true)]
            public class Base
            {
                public string Prop { get; set; }

                public string Field;

                public string TwoFaced { get; set; } // derived declares a *field* by the same name.
            }

            [MessagePackObject(true)]
            public class Derived : Base
            {
                public new string {|MsgPack018:Prop|} { get; set; }

                public new string {|MsgPack018:Field|};

                public new string {|MsgPack018:TwoFaced|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AbstractFormatterDoesNotAttractAttention()
    {
        string test = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            public abstract class AbstractFormatter1 : IMessagePackFormatter<object>
            {
                public abstract void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);
                public abstract object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
            }

            public abstract class AbstractFormatter2 : IMessagePackFormatter<object>
            {
                public abstract void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);
                public abstract object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Union()
    {
        string input = Preamble + @"
[MessagePack.Union(0, typeof(Foo))]
public interface IUnionTest
{
}

public class {|MsgPack003:Foo|}
{
    public int MyProperty { get; set; }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
        }.RunAsync();
    }
}

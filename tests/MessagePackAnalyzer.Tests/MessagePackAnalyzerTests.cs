// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS =
    CSharpCodeFixVerifier<MessagePackAnalyzer.MessagePackAnalyzer, MessagePackAnalyzer.MessagePackCodeFixProvider>;

public class MessagePackAnalyzerTests
{
    private const string Preamble = /* lang=c#-test */ @"
using MessagePack;
";

    [Fact]
    public async Task NoMessagePackReference()
    {
        string input = /* lang=c#-test */ @"
public class Foo
{
}
";

        await VerifyCS.VerifyAnalyzerWithoutMessagePackReferenceAsync(input);
    }

    [Fact]
    public async Task MessageFormatterAttribute()
    {
        string input = Preamble + /* lang=c#-test */ @"using MessagePack.Formatters;

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
        string input = Preamble + /* lang=c#-test */ @"using MessagePack.Formatters;

public class InvalidMessageFormatter { }

[{|MsgPack006:MessagePackFormatter(typeof(InvalidMessageFormatter))|}]
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
        string input = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public class Foo
{
    [Key(null)]
    public string {|MsgPack005:Member|} { get; set; }
}
";

        await VerifyCS.VerifyAnalyzerAsync(input);
    }

    [Fact]
    public async Task AddAttributesToMembers()
    {
        string input = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public class Foo
{
    public string {|MsgPack004:Member1|} { get; set; }
    public string {|MsgPack004:Member2|} { get; set; }
}
";

        string output = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    public string Member2 { get; set; }
}
";

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
    public async Task AddAttributeToType()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = /* lang=c#-test */ @"
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
";

        string output = /* lang=c#-test */ @"
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
";

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
    public async Task CodeFixAppliesAcrossFiles()
    {
        var inputs = new string[]
        {
            /* lang=c#-test */ @"
public class Foo
{
    public int {|MsgPack004:Member1|} { get; set; }
}
",
            /* lang=c#-test */ @"using MessagePack;

[MessagePackObject]
public class Bar : Foo
{
    public int {|MsgPack004:Member2|} { get; set; }
}
",
        };
        var outputs = new string[]
        {
            /* lang=c#-test */ @"
public class Foo
{
    [MessagePack.Key(1)]
    public int Member1 { get; set; }
}
",
            /* lang=c#-test */ @"using MessagePack;

[MessagePackObject]
public class Bar : Foo
{
    [Key(0)]
    public int Member2 { get; set; }
}
",
        };

        await VerifyCS.VerifyCodeFixAsync(inputs, outputs);
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecord()
    {
        string input = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public record Foo
{
    public string {|MsgPack004:Member1|} { get; set; }
    public string {|MsgPack004:Member2|} { get; set; }
}
";

        string output = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public record Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    public string Member2 { get; set; }
}
";

        var context = new CSharpCodeFixTest<MessagePackAnalyzer.MessagePackAnalyzer, MessagePackAnalyzer.MessagePackCodeFixProvider, DefaultVerifier>();
        context.ReferenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(new PackageIdentity("MessagePack", "2.0.335"))); ;
        context.CompilerDiagnostics = CompilerDiagnostics.Errors;
        context.SolutionTransforms.Add(static (solution, projectId) =>
        {
            return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp9));
        });

        context.TestCode = input;
        context.FixedCode = output;

        await context.RunAsync();
    }

    [Fact]
    public async Task AddAttributesToMembersOfRecordWithPrimaryCtor()
    {
        string input = Preamble + /* lang=c#-test */ @"
[MessagePackObject]
public record Foo(
    string {|MsgPack004:Member1|},
    string {|MsgPack004:Member2|});
";

        string output = input; // No fix for this

        var context = new CSharpCodeFixTest<MessagePackAnalyzer.MessagePackAnalyzer, MessagePackAnalyzer.MessagePackCodeFixProvider, DefaultVerifier>();
        context.ReferenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(new PackageIdentity("MessagePack", "2.0.335"))); ;
        context.CompilerDiagnostics = CompilerDiagnostics.Errors;
        context.SolutionTransforms.Add(static (solution, projectId) =>
        {
            return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp11));
        });

        context.TestCode = input;
        context.FixedCode = output;

        await context.RunAsync();
    }
}

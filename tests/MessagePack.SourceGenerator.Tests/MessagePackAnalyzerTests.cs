﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.MessagePackCodeFixProvider>;

public class MessagePackAnalyzerTests
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

        await VerifyCS.VerifyAnalyzerAsync(input);
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

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
    public async Task AddAttributeToType()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = @"
public class Foo
{
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public class Bar
{
    [MessagePack.Key(0)]
    public {|MsgPack003:Foo|} Member { get; set; }
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
public class Bar
{
    [MessagePack.Key(0)]
    public Foo Member { get; set; }
}
";

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
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
    public {|MsgPack003:Foo|} Member { get; set; }
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

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
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
    public {|MsgPack003:Foo|} Member { get; set; }
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

        await VerifyCS.VerifyCodeFixAsync(input, output);
    }

    [Fact]
    public async Task AddAttributeToTypeForRecordPrimaryConstructor()
    {
        // Don't use Preamble because we want to test that it works without a using statement at the top.
        string input = @"
public class Foo
{
    public string Member { get; set; }
}

[MessagePack.MessagePackObject]
public record Bar([property: MessagePack.Key(0)] {|MsgPack003:Foo|} Member);

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

        await VerifyCS.VerifyCodeFixAsync(input, output);
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
        }.RunAsync();
    }
}

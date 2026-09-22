// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpCodeFixVerifier<MessagePack.SourceGenerator.Analyzers.MsgPack00xMessagePackAnalyzer, MessagePack.Analyzers.CodeFixes.MsgPack015CodeFixProvider>;

public class MsgPack015AllowPrivateRequiredTests
{
    private const string Preamble = @"
using MessagePack;
";

    [Fact]
    public async Task AddWhenNonPublicPropertyIsAnnotated()
    {
        string input = Preamble + @"
[{|MsgPack015:MessagePackObject|}]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    internal string Member2 { get; set; }
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
        }.RunAsync();
    }

    [Fact]
    public async Task SetWhenNonPublicPropertyIsAnnotated()
    {
        string input = Preamble + @"
[MessagePackObject(AllowPrivate = {|MsgPack015:false|})]
public class Foo
{
    [Key(0)]
    public string Member1 { get; set; }
    [Key(1)]
    internal string Member2 { get; set; }
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
        }.RunAsync();
    }

    [Fact]
    public async Task AddForNonPublicType()
    {
        string input = Preamble + @"
[MessagePackObject(AllowPrivate = {|MsgPack015:false|})]
class Foo
{
}
";

        string output = Preamble + @"
[MessagePackObject(AllowPrivate = true)]
class Foo
{
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task AddForPublicTypeNestedUnderNonPublicType()
    {
        string input = Preamble + @"
class Outer
{
    [{|MsgPack015:MessagePackObject|}]
    public class Foo
    {
    }
}
";

        string output = Preamble + @"
class Outer
{
    [MessagePackObject(AllowPrivate = true)]
    public class Foo
    {
    }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }

    [Fact]
    public async Task AddForNonPublicDeserializingConstructor()
    {
        string input = Preamble + @"
[MessagePackObject(AllowPrivate = {|MsgPack015:false|})]
public class Foo
{
    internal Foo() { }
}
";

        string output = Preamble + @"
[MessagePackObject(AllowPrivate = true)]
public class Foo
{
    internal Foo() { }
}
";

        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = output,
        }.RunAsync();
    }
}

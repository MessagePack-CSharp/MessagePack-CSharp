// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using MessagePackAnalyzer;
using Microsoft.CodeAnalysis;
using Xunit;
using VerifyCS = CSharpCodeFixVerifier<MessagePackAnalyzer.MsgPack002UseConstantOptionsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = VisualBasicCodeFixVerifier<MessagePackAnalyzer.MsgPack002UseConstantOptionsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class MsgPack002UseConstantOptionsAnalyzerTests
{
    [Fact]
    public async Task Invocation_WithImmutableStaticOptions()
    {
        string test = @"
using MessagePack;

class Test {
    void Foo() {
        MessagePackSerializer.Serialize(5, MessagePackSerializerOptions.Standard);
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Invocation_WithImmutableStaticOptions_VB()
    {
        string test = @"
Imports MessagePack

Public Class Test
    Public Sub Foo()
        MessagePackSerializer.Serialize(5, MessagePackSerializerOptions.Standard)
    End Sub
End Class
";

        await VerifyVB.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Invocation_WithMutableStaticOptions()
    {
        string test = @"
using MessagePack;

class Test {
    void Foo() {
        MessagePackSerializer.Serialize(5, [|MessagePackSerializer.DefaultOptions|]);
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Members_BasedOnStaticOptions()
    {
        string test = @"
using MessagePack;

class Test {
    MessagePackSerializerOptions field1 = [|MessagePackSerializer.DefaultOptions|];
    MessagePackSerializerOptions property1 { get; } = [|MessagePackSerializer.DefaultOptions|];
    MessagePackSerializerOptions field2 = MessagePackSerializerOptions.Standard;
    MessagePackSerializerOptions property2 { get; } = MessagePackSerializerOptions.Standard;
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Fields()
    {
        string test = @"
using MessagePack;

class Test {
    internal static MessagePackSerializerOptions MyReadWriteOptions = MessagePackSerializerOptions.Standard;
    internal static readonly MessagePackSerializerOptions MyReadOnlyOptions = MessagePackSerializerOptions.Standard;
    static MessagePackSerializerOptions MyPrivateOptions = MessagePackSerializerOptions.Standard;
    internal MessagePackSerializerOptions MyInstanceOptions = MessagePackSerializerOptions.Standard;

    void Foo() {
        var options1 = [|MyReadWriteOptions|];
        var options2 = MyReadOnlyOptions;
        var options3 = MyPrivateOptions;
        var options4 = MyInstanceOptions;
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Properties()
    {
        string test = @"
using MessagePack;

class Test {
    internal static MessagePackSerializerOptions MyReadWriteOptions { get; set; } = MessagePackSerializerOptions.Standard;
    internal static MessagePackSerializerOptions MyReadPrivateWriteOptions { get; private set; } = MessagePackSerializerOptions.Standard;
    internal static MessagePackSerializerOptions MyReadOnlyOptions { get; } = MessagePackSerializerOptions.Standard;
    internal MessagePackSerializerOptions MyInstanceOptions { get; } = MessagePackSerializerOptions.Standard;

    void Foo() {
        var options1 = [|MyReadWriteOptions|];
        var options2 = MyReadPrivateWriteOptions;
        var options3 = MyReadOnlyOptions;
        var options4 = MyInstanceOptions;
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}

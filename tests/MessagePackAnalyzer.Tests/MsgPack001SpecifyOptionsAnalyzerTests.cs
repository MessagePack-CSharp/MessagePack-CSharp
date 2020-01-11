// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using MessagePackAnalyzer;
using Microsoft.CodeAnalysis;
using Xunit;
using VerifyCS = CSharpCodeFixVerifier<MessagePackAnalyzer.MsgPack001SpecifyOptionsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = VisualBasicCodeFixVerifier<MessagePackAnalyzer.MsgPack001SpecifyOptionsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class MsgPack001SpecifyOptionsAnalyzerTests
{
    [Fact]
    public async Task Invocation_OmittedOptionalParameter()
    {
        string test = @"
using MessagePack;

class Test {
    void Foo() {
        [|MessagePackSerializer.Serialize(5)|];
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Invocation_OmittedOptionalParameter_VB()
    {
        string test = @"
Imports MessagePack

Public Class Test
    Public Sub Foo()
        [|MessagePackSerializer.Serialize|](5)
    End Sub
End Class
";

        await VerifyVB.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Invocation_ExplicitlyNull()
    {
        string test = @"
using MessagePack;

class Test {
    void Foo() {
        MessagePackSerializer.Serialize(5, options: [|null|]);
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InvocationWithOptions_LocalVariable()
    {
        string test = @"
using MessagePack;

class Test {
    void Foo() {
        MessagePackSerializerOptions myOptions = MessagePackSerializerOptions.Standard;
        MessagePackSerializer.Serialize(5, myOptions);
    }
}
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}

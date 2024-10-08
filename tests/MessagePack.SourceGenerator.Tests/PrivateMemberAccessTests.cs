// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class PrivateMemberAccessTests(ITestOutputHelper logger)
{
    [Theory]
    [InlineData("class", LanguageVersion.CSharp7_3)]
    [InlineData("record", LanguageVersion.CSharp9)]
    [InlineData("struct", LanguageVersion.CSharp7_3)]
    [InlineData("record struct", LanguageVersion.CSharp10)]
    public async Task FormatterForClassWithPrivateMembers(string type, LanguageVersion version)
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            [MessagePackObject(AllowPrivate = true)]
            partial {{type}} MyObject
            {
                [Key(0)]
                private int value;
                [IgnoreMember]
                public int Value { get => value; set => this.value = value; }
            }
            """;
        await new VerifyCS.Test(testMethod: $"{nameof(FormatterForClassWithPrivateMembers)}({type})")
        {
            TestState =
            {
                Sources = { testSource },
            },
            LanguageVersion = version,
        }.RunDefaultAsync(logger);
    }

    [Fact]
    public async Task FormatterForClassWithPrivateMembers_WithNamespace()
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            namespace A
            {
                [MessagePackObject(AllowPrivate = true)]
                partial class MyObject
                {
                    [Key(0)]
                    private int value;
                    [IgnoreMember]
                    public int Value { get => value; set => this.value = value; }
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(logger, testSource);
    }

    [Theory]
    [InlineData("class", LanguageVersion.CSharp7_3)]
    [InlineData("record", LanguageVersion.CSharp9)]
    [InlineData("struct", LanguageVersion.CSharp7_3)]
    public async Task FormatterForClassWithPrivateMembers_NonPartialTypes(string type, LanguageVersion languageVersion)
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            [MessagePackObject(AllowPrivate = true)]
            {{type}} {|MsgPack011:MyObject|}
            {
                [Key(0)]
                private int value;
                [IgnoreMember]
                public int Value { get => value; set => this.value = value; }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(logger, testSource, languageVersion: languageVersion, testMethod: $"{nameof(FormatterForClassWithPrivateMembers_NonPartialTypes)}({type})");
    }

    [Theory]
    [InlineData("class", LanguageVersion.CSharp7_3)]
    [InlineData("record", LanguageVersion.CSharp9)]
    [InlineData("struct", LanguageVersion.CSharp7_3)]
    public async Task FormatterForClassWithPrivateMembers_Nested_NonPartialTypes(string type, LanguageVersion languageVersion)
    {
        string testSource = $$"""
            using System;
            using MessagePack;

            {{type}} Outer
            {
                [MessagePackObject(AllowPrivate = true)]
                internal partial {{type}} {|MsgPack011:MyObject|}
                {
                    [Key(0)]
                    private int value;
                    [IgnoreMember]
                    public int Value { get => value; set => this.value = value; }
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(logger, testSource, languageVersion: languageVersion, testMethod: $"{nameof(FormatterForClassWithPrivateMembers_Nested_NonPartialTypes)}({type})");
    }
}

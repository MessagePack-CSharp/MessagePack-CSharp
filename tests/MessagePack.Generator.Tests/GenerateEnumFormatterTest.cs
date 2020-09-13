// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Generator.Tests
{
    public class GenerateEnumFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateEnumFormatterTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task EnumFormatter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyMessagePackObject
    {
        [Key(0)]
        public MyEnum EnumValue { get; set; }
    }

    public enum MyEnum
    {
        A, B, C
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.GetOutputCompilation().Compilation,
                tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

            var compilation = tempWorkarea.GetOutputCompilation();
            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();
            symbols.Should().Contain(x => x.Name == "MyEnumFormatter");
        }
    }
}

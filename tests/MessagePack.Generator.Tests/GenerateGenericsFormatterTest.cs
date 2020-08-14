// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Generator.Tests
{
    public class GenerateGenericsFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateGenericsFormatterTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task GenericsUnionFormatter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create(false);
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    [Union(0, typeof(Wrapper<string>))]
    [Union(1, typeof(Wrapper<int[]>))]
    [Union(2, typeof(Wrapper<IEnumerable<Guid>>))]
    public class Wrapper<T>
    {
        [Key(0)]
        public T Content { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();
            var formatters = symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).ToArray();
            formatters.Should().Contain(x => x == "MessagePack.Formatters.IMessagePackFormatter<TempProject.Wrapper<T>>");
        }

        [Fact]
        public async Task GenericsOfTFormatter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyGenericObject<T>
    {
        [Key(0)]
        public T Content { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();

            var types = symbols.Select(x => x.ToDisplayString()).ToArray();
            types.Should().Contain("TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T>");

            var formatters = symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).ToArray();
            formatters.Should().Contain(x => x == "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>");
        }

        [Fact]
        public async Task GenericsOfT1T2Formatter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyGenericObject<T1, T2>
    {
        [Key(0)]
        public T1 ValueA { get; set; }
        [Key(1)]
        public T2 ValueB { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();

            var types = symbols.Select(x => x.ToDisplayString()).ToArray();
            types.Should().Contain("TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T1, T2>");

            var formatters = symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).ToArray();
            formatters.Should().Contain(x => x == "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T1, T2>>");
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Generator.Tests
{
    public class GenerateUnionFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateUnionFormatterTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Union_Defined_In_TargetProject(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var defineContents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    public class Dummy
    {
    }
}
";
            tempWorkarea.AddFileToReferencedProject("Dummy.cs", defineContents);

            var usageContents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public IUnionObject Value { get; set; }
    }

    [Union(0, typeof(UnionDerived1))]
    [Union(1, typeof(UnionDerived2))]
    public interface IUnionObject
    {
        string Name { get; }
    }

    [MessagePackObject]
    public class UnionDerived1 : IUnionObject
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string Name { get; set; }
    }

    [MessagePackObject]
    public class UnionDerived2 : IUnionObject
    {
        [Key(0)]
        public string Name { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToTargetProject("MyObject.cs", usageContents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.GetOutputCompilation().Compilation,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();

            symbols.Select(x => x.ToDisplayString()).Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.IUnionObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived1Formatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived2Formatter",
            });

            symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).Should().Contain(new[]
            {
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.IUnionObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.UnionDerived1>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.UnionDerived2>",
            });

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.IUnionObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived1Formatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived2Formatter",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Union_Defined_In_ReferencedProject(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var defineContents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [Union(0, typeof(UnionDerived1))]
    [Union(1, typeof(UnionDerived2))]
    public interface IUnionObject
    {
        string Name { get; }
    }

    [MessagePackObject]
    public class UnionDerived1 : IUnionObject
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string Name { get; set; }
    }

    [MessagePackObject]
    public class UnionDerived2 : IUnionObject
    {
        [Key(0)]
        public string Name { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToReferencedProject("IUnionObject.cs", defineContents);

            var usageContents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public IUnionObject Value { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToTargetProject("MyObject.cs", usageContents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.GetOutputCompilation().Compilation,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();

            symbols.Select(x => x.ToDisplayString()).Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.IUnionObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived1Formatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived2Formatter",
            });

            symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).Should().Contain(new[]
            {
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.IUnionObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.UnionDerived1>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.UnionDerived2>",
            });

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.IUnionObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived1Formatter",
                "TempProject.Generated.Formatters.TempProject.UnionDerived2Formatter",
            });
        }
    }
}

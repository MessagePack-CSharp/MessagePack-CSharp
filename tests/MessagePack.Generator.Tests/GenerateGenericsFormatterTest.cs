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
    public class GenerateGenericsFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateGenericsFormatterTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NullableFormatter(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create(false);
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public int? ValueNullableInt { get; set; }
        [Key(1)]
        public MyEnum? ValueNullableEnum { get; set; }
        [Key(2)]
        public ValueTuple<int, long>? ValueNullableStruct { get; set; }
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
                tempWorkarea.CsProjectPath,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "global::MessagePack.Formatters.NullableFormatter<(int, long)>",
                "global::MessagePack.Formatters.NullableFormatter<global::TempProject.MyEnum>",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WellKnownGenericsFormatter(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create(false);
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public List<int> ValueList { get; set; }
        [Key(1)]
        public List<List<int>> ValueListNested { get; set; }
        [Key(2)]
        public ValueTuple<int, string, long> ValueValueTuple { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "global::MessagePack.Formatters.ListFormatter<int>",
                "global::MessagePack.Formatters.ListFormatter<global::System.Collections.Generic.List<int>>",
                "global::MessagePack.Formatters.ValueTupleFormatter<int, string, long>",
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenericsUnionFormatter(bool isSingleFileOutput)
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
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).Should().BeEmpty();

            var symbols = compilation.GetNamedTypeSymbolsFromGenerated();
            var formatters = symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).ToArray();
            formatters.Should().Contain("MessagePack.Formatters.IMessagePackFormatter<TempProject.Wrapper<T>>");

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<global::System.Collections.Generic.IEnumerable<global::System.Guid>>",
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<int[]>",
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<string>",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenericsOfTFormatter(bool isSingleFileOutput)
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

    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public MyGenericObject<int> Value { get; set; }
    }

    [MessagePackObject]
    public class MyObjectNested
    {
        [Key(0)]
        public MyGenericObject<MyGenericObject<int>> Value { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
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
            formatters.Should().Contain("MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>");

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<global::TempProject.MyGenericObject<int>>",
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<int>",
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.MyObjectNestedFormatter",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenericsOfT1T2Formatter(bool isSingleFileOutput)
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

    [MessagePackObject]
    public class MyObject
    {
        [Key(0)]
        public MyGenericObject<int, string> Value { get; set; }
    }

    [MessagePackObject]
    public class MyObjectNested
    {
        [Key(0)]
        public MyGenericObject<MyGenericObject<int, string>, MyGenericObject<int, string>> Value { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
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
            formatters.Should().Contain("MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T1, T2>>");

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<global::TempProject.MyGenericObject<int, string>, global::TempProject.MyGenericObject<int, string>>",
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<int, string>",
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.MyObjectNestedFormatter",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenericsOfTFormatter_FormatterOnly(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    // This type is not used by the project.
    // It may be referenced by other projects.
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
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
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
            formatters.Should().Contain("MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>");

            // The generated resolver doesn't know closed-type generic formatter.
            compilation.GetResolverKnownFormatterTypes().Should().BeEmpty();
        }
    }
}

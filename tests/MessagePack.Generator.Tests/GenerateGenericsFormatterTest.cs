// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

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
                tempWorkarea.GetOutputCompilation().Compilation,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

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
                tempWorkarea.GetOutputCompilation().Compilation,
                isSingleFileOutput ? Path.Combine(tempWorkarea.OutputDirectory, "Generated.cs") : tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

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
        public async Task GenericsUnionFormatter_Nested(bool isSingleFileOutput)
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
        public List<T> Content1 { get; set; }
        [Key(1)]
        public MyGenericObject<T> Content2 { get; set; }
    }

    [MessagePackObject]
    public class MyGenericObject<T>
    {
        [Key(0)]
        public MyInnerGenericObject<T> Content { get; set; }
    }

    [MessagePackObject]
    public class MyInnerGenericObject<T>
    {
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
            formatters.Should().Contain(new[]
            {
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.Wrapper<T>>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyInnerGenericObject<T>>",
            });

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<global::System.Collections.Generic.IEnumerable<global::System.Guid>>",
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<int[]>",
                "TempProject.Generated.Formatters.TempProject.WrapperFormatter<string>",
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<global::System.Collections.Generic.IEnumerable<global::System.Guid>>",
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<int[]>",
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<string>",
                "TempProject.Generated.Formatters.TempProject.MyInnerGenericObjectFormatter<global::System.Collections.Generic.IEnumerable<global::System.Guid>>",
                "TempProject.Generated.Formatters.TempProject.MyInnerGenericObjectFormatter<int[]>",
                "TempProject.Generated.Formatters.TempProject.MyInnerGenericObjectFormatter<string>",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NestedGenericTypes(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create(false);
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject : Wrapper<MyObject2>
    {
    }

    [MessagePackObject]
    public class MyObject2
    {}

    [MessagePackObject]
    public class Wrapper<T>
    {
        [Key(0)]
        public List<T> Content1 { get; set; }
        [Key(1)]
        public MyGenericObject<T> Content2 { get; set; }
    }

    [MessagePackObject]
    public class MyGenericObject<T>
    {
        [Key(0)]
        public MyInnerGenericObject<T> Content { get; set; }
        [Key(1)]
        public T[] Content2 { get; set; }
    }

    [MessagePackObject]
    public class MyInnerGenericObject<T>
    {
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
            formatters.Should().Contain(new[]
            {
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject2>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.Wrapper<T>>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyInnerGenericObject<T>>",
            });

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                // "TempProject.Generated.Formatters.TempProject.WrapperFormatter<global::TempProject.MyObject2>", // Wrapper<T> is not used as a property/field in the code. The generated resolver can ignore it.
                "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<global::TempProject.MyObject2>",
                "TempProject.Generated.Formatters.TempProject.MyInnerGenericObjectFormatter<global::TempProject.MyObject2>",
                "global::MessagePack.Formatters.ListFormatter<global::TempProject.MyObject2>",
                "global::MessagePack.Formatters.ArrayFormatter<global::TempProject.MyObject2>",
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenericsOfTFormatter_WithKnownTypes(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create(false);
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyObject : MyGenericObject<MyObject2>
    {
    }

    [MessagePackObject]
    public class MyObject2
    { }

    [MessagePackObject]
    public class MyGenericObject<T>
    {
        [Key(0)]
        public List<T> Content1 { get; set; }
        [Key(1)]
        public T[] Content2 { get; set; }
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
            formatters.Should().Contain(new[]
            {
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyObject2>",
                "MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>",
            });

            compilation.GetResolverKnownFormatterTypes().Should().Contain(new[]
            {
                "global::MessagePack.Formatters.ListFormatter<global::TempProject.MyObject2>",
                "global::MessagePack.Formatters.ArrayFormatter<global::TempProject.MyObject2>",
                // "TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<string>", // MyGenericObjectFormatter<T> is not used as a property/field in the code. The generated resolver can ignore it.
                "TempProject.Generated.Formatters.TempProject.MyObjectFormatter",
                "TempProject.Generated.Formatters.TempProject.MyObject2Formatter",
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

            var types = symbols.Select(x => x.ToDisplayString()).ToArray();
            types.Should().Contain("TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T>");

            var formatters = symbols.SelectMany(x => x.Interfaces).Select(x => x.ToDisplayString()).ToArray();
            formatters.Should().Contain("MessagePack.Formatters.IMessagePackFormatter<TempProject.MyGenericObject<T>>");

            // The generated resolver doesn't know closed-type generic formatter.
            compilation.GetResolverKnownFormatterTypes().Should().BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Generics_Constraints_Type(bool isSingleFileOutput)
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
        where T : IDisposable
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

            var formatterType = symbols.FirstOrDefault(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T>");
            formatterType.Should().NotBeNull();
            // IDisposable
            formatterType.TypeParameters[0].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasValueTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasConstructorConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasNotNullConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasUnmanagedTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.IDisposable");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Generics_Constraints_NullableReferenceType(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyGenericObject<T1, T2, T3, T4>
        where T1 : MyClass?
        where T2 : MyClass
        where T3 : MyGenericClass<MyGenericClass<MyClass?>?>?
        where T4 : MyClass, IMyInterface?
    {
        [Key(0)]
        public T1 Content { get; set; }
    }

    public class MyClass {}
    public class MyGenericClass<T> {}
    public interface IMyInterface {}
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

            var formatterType = symbols.FirstOrDefault(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T1, T2, T3, T4>");
            formatterType.Should().NotBeNull();
            // MyClass?
            formatterType.TypeParameters[0].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.MyClass");
            formatterType.TypeParameters[0].ConstraintNullableAnnotations[0].Should().Be(NullableAnnotation.Annotated);
            // MyClass
            formatterType.TypeParameters[1].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.MyClass");
            formatterType.TypeParameters[1].ConstraintNullableAnnotations[0].Should().Be(NullableAnnotation.None);
            // MyGenericClass<MyGenericClass<MyClass?>?>?
            formatterType.TypeParameters[2].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)) == "global::TempProject.MyGenericClass<global::TempProject.MyGenericClass<global::TempProject.MyClass?>?>");
            formatterType.TypeParameters[2].ConstraintNullableAnnotations[0].Should().Be(NullableAnnotation.Annotated);
            // MyClass, IMyInterface?
            formatterType.TypeParameters[3].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.MyClass");
            formatterType.TypeParameters[3].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.IMyInterface");
            formatterType.TypeParameters[3].ConstraintNullableAnnotations[0].Should().Be(NullableAnnotation.None);
            formatterType.TypeParameters[3].ConstraintNullableAnnotations[1].Should().Be(NullableAnnotation.Annotated);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Generics_Constraints_Struct(bool isSingleFileOutput)
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
        where T : struct
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

            var formatterType = symbols.FirstOrDefault(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T>");
            formatterType.Should().NotBeNull();
            // struct
            formatterType.TypeParameters[0].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasValueTypeConstraint.Should().BeTrue();
            formatterType.TypeParameters[0].HasConstructorConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasNotNullConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasUnmanagedTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].ConstraintTypes.Should().BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Generics_Constraints_Multiple(bool isSingleFileOutput)
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyGenericObject<T1, T2, T3, T4>
        where T1 : struct
        where T2 : IDisposable, new()
        where T3 : notnull
        where T4 : unmanaged
    {
        [Key(0)]
        public T1 Content1 { get; set; }
        [Key(1)]
        public T2 Content2 { get; set; }
        [Key(2)]
        public T3 Content3 { get; set; }
        [Key(3)]
        public T4 Content4 { get; set; }
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

            var formatterType = symbols.FirstOrDefault(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T1, T2, T3, T4>");
            formatterType.Should().NotBeNull();
            // struct
            formatterType.TypeParameters[0].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasValueTypeConstraint.Should().BeTrue();
            formatterType.TypeParameters[0].HasConstructorConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasNotNullConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].HasUnmanagedTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[0].ConstraintTypes.Should().BeEmpty();
            // IDisposable, new()
            formatterType.TypeParameters[1].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[1].HasValueTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[1].HasConstructorConstraint.Should().BeTrue();
            formatterType.TypeParameters[1].HasNotNullConstraint.Should().BeFalse();
            formatterType.TypeParameters[1].HasUnmanagedTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[1].ConstraintTypes.Should().Contain(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.IDisposable");
            // notnull
            formatterType.TypeParameters[2].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[2].HasValueTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[2].HasConstructorConstraint.Should().BeFalse();
            formatterType.TypeParameters[2].HasNotNullConstraint.Should().BeTrue();
            formatterType.TypeParameters[2].HasUnmanagedTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[2].ConstraintTypes.Should().BeEmpty();
            // unmanaged
            formatterType.TypeParameters[3].HasReferenceTypeConstraint.Should().BeFalse();
            formatterType.TypeParameters[3].HasValueTypeConstraint.Should().BeTrue(); // unmanaged constraint includes value-type constraint
            formatterType.TypeParameters[3].HasConstructorConstraint.Should().BeFalse();
            formatterType.TypeParameters[3].HasNotNullConstraint.Should().BeFalse();
            formatterType.TypeParameters[3].HasUnmanagedTypeConstraint.Should().BeTrue();
            formatterType.TypeParameters[3].ConstraintTypes.Should().BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Generics_Constraints_ReferenceType_Nullable(bool isSingleFileOutput)
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
        where T1 : class?
        where T2 : class
    {
        [Key(0)]
        public T1 Content1 { get; set; }
        [Key(1)]
        public T2 Content2 { get; set; }
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

            var formatterType = symbols.FirstOrDefault(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TempProject.Generated.Formatters.TempProject.MyGenericObjectFormatter<T1, T2>");
            formatterType.Should().NotBeNull();
            // class?
            formatterType.TypeParameters[0].HasReferenceTypeConstraint.Should().BeTrue();
            formatterType.TypeParameters[0].ConstraintTypes.Should().BeEmpty();
            formatterType.TypeParameters[0].ReferenceTypeConstraintNullableAnnotation.Should().Be(NullableAnnotation.Annotated);
            // class
            formatterType.TypeParameters[1].HasReferenceTypeConstraint.Should().BeTrue();
            formatterType.TypeParameters[1].ConstraintTypes.Should().BeEmpty();
            formatterType.TypeParameters[1].ReferenceTypeConstraintNullableAnnotation.Should().Be(NullableAnnotation.None);
        }
    }
}

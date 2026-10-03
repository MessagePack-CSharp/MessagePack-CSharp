// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class GenericsFormatterTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenericsFormatterTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task NullableFormatter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NullableReferenceTypeInListFormatter()
    {
        string testSource = /* lang=c#-test */ """
#nullable enable
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class SomeClass
    {
        [Key(0)]
        public required List<SomeClass?> Values { get; set; }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, languageVersion: LanguageVersion.CSharp11);
    }

    [Fact]
    public async Task WellKnownGenericsFormatter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsUnionFormatter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsUnionFormatter_Nested()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NestedGenericTypes()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsOfTFormatter_WithKnownTypes()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsOfTFormatter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsOfT1T2Formatter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericsOfTFormatter_FormatterOnly()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task Generics_Constraints_Type()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task Generics_Constraints_NullableReferenceType()
    {
        string testSource = """
#nullable enable

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
        public T1? Content { get; set; }
    }

    public class MyClass {}
    public class MyGenericClass<T> {}
    public interface IMyInterface {}
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, languageVersion: LanguageVersion.CSharp9);
    }

    [Fact]
    public async Task Generics_Constraints_Struct()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task Generics_Constraints_Multiple()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, languageVersion: LanguageVersion.CSharp8);
    }

    [Fact]
    public async Task Generics_Constraints_ReferenceType_Nullable()
    {
        string testSource = """
#nullable enable

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
        public T1? Content1 { get; set; }
        [Key(1)]
        public T2? Content2 { get; set; }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, languageVersion: LanguageVersion.CSharp9);
    }

    [Fact]
    public async Task Generics_Defined_In_ReferencedProject()
    {
        string defineSource = """
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

    [GeneratedMessagePackResolver]
    partial class MyResolver { }
}
""";

        string usageSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
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
""";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { usageSource },
                AdditionalProjects =
                {
                    ["DefiningProject"] = { Sources = { defineSource } },
                },
                AdditionalProjectReferences = { "DefiningProject" },
            },
        }.RunAsync();
    }

    /// <summary>
    /// Asserts that the generator does not crash on a closed generic <em>value type</em> (a struct) that is
    /// defined in a referenced assembly. Regression test for
    /// https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/2283.
    /// </summary>
    [Fact]
    public async Task Generics_Defined_In_ReferencedProject_Struct()
    {
        // The bug this guards against (#2283) only reproduces when the generic value type is loaded from
        // *compiled metadata*, i.e. its members have no syntax in the consuming compilation. A same-solution
        // AdditionalProjects reference (as used by Generics_Defined_In_ReferencedProject above) still exposes
        // source symbols with real DeclaringSyntaxReferences, so it would not have caught this. Instead, compile
        // a tiny library to IL first and reference the resulting assembly, exactly as a NuGet package or a built
        // ProjectReference would be referenced.
        string librarySource = """
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public partial record struct Item<T>([property: Key(0)] string Key);
}
""";

        ImmutableArray<MetadataReference> frameworkReferences = await ReferencesHelper.DefaultTargetFrameworkReferences.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        CSharpCompilation libraryCompilation = CSharpCompilation.Create(
            "TempProject.Lib",
            new[] { CSharpSyntaxTree.ParseText(librarySource, new CSharpParseOptions(LanguageVersion.CSharp10)) },
            frameworkReferences.Concat(ReferencesHelper.GetReferences(ReferencesSet.MessagePack)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream libraryStream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitResult = libraryCompilation.Emit(libraryStream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        libraryStream.Position = 0;
        MetadataReference libraryReference = MetadataReference.CreateFromStream(libraryStream);

        string usageSource = """
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public sealed partial record Cmd([property: Key(0)] string Scope)
    {
        [Key(1)]
        public Item<int> Member { get; init; }
    }
}
""";
        await new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp10,
            TestState =
            {
                Sources = { usageSource },
                AdditionalReferences = { libraryReference },
            },
        }.RunAsync();
    }

    /// <summary>
    /// Asserts that the generator can handle directly recursive generic constraints.
    /// </summary>
    [Fact]
    public async Task RecursiveGenerics_Direct()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using MessagePack;

            [MessagePackObject]
            public class A<T>
                where T : IEquatable<A<T>>
            {
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}

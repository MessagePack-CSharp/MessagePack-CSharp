// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class GenerationTests
{
    private const string Preamble = @"
using MessagePack;
";

    private readonly ITestOutputHelper testOutputHelper;

    public GenerationTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory, PairwiseData]
    public async Task EnumFormatter(ContainerKind container, bool usesMapMode)
    {
        string testSource = """
[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal MyEnum EnumValue { get; set; }
}

internal enum MyEnum
{
    A, B, C
}
""";
        testSource = TestUtilities.WrapTestSource(testSource, container);

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, options: new() { Generator = new() { Formatters = new() { UsesMapMode = usesMapMode } } }, testMethod: $"{nameof(EnumFormatter)}({container}, {usesMapMode})");
    }

    [Fact]
    public async Task EnumFormatter_CollidingTypeNames()
    {
        string testSource = """
using MessagePack;

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal NS1.MyEnum EnumValue1 { get; set; }

    [Key(1)]
    internal NS2.MyEnum EnumValue2 { get; set; }
}

namespace NS1 {
    internal enum MyEnum
    {
        A, B, C
    }
}

namespace NS2 {
    internal enum MyEnum
    {
        D, E
    }
}
""";

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericType_CollidingTypeNames()
    {
        string testSource = """
using MessagePack;

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal NS1.MyType<int> Value1 { get; set; }

    [Key(1)]
    internal NS2.MyType<int> Value2 { get; set; }
}

namespace NS1 {
    [MessagePackObject]
    internal class MyType<T>
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}

namespace NS2 {
    [MessagePackObject]
    internal class MyType<T>
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}
""";

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NonGenericType_CollidingTypeNames()
    {
        string testSource = """
using MessagePack;

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal NS1.MyType Value1 { get; set; }

    [Key(1)]
    internal NS2.MyType Value2 { get; set; }
}

namespace NS1 {
    [MessagePackObject]
    internal class MyType
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}

namespace NS2 {
    [MessagePackObject]
    internal class MyType
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}
""";

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task MixType_CollidingTypeNames()
    {
        string testSource = """
using MessagePack;

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal NS1.MyType Value1 { get; set; }

    [Key(1)]
    internal NS2.MyType Value2 { get; set; }

    [Key(2)]
    internal NS3.MyType<int> Value3 { get; set; }
}

namespace NS1 {
    internal enum MyType
    {
        A, B
    }
}

namespace NS2 {
    [MessagePackObject]
    internal class MyType
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}

namespace NS3 {
    [MessagePackObject]
    internal class MyType<T>
    {
        [Key(0)]
        internal string Foo { get; set; }
    }
}
""";

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Theory, PairwiseData]
    public async Task CustomFormatterViaAttributeOnProperty(bool usesMapMode)
    {
        string testSource = """
using MessagePack;
using MessagePack.Formatters;

[MessagePackObject]
internal record HasPropertyWithCustomFormatterAttribute
{
    [Key(0), MessagePackFormatter(typeof(UnserializableRecordFormatter))]
    internal UnserializableRecord CustomValue { get; set; }
}

record UnserializableRecord
{
    internal int Value { get; set; }
}

class UnserializableRecordFormatter : IMessagePackFormatter<UnserializableRecord>
{
    public void Serialize(ref MessagePackWriter writer, UnserializableRecord value, MessagePackSerializerOptions options)
    {
        writer.WriteInt32(value.Value);
    }

    public UnserializableRecord Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new UnserializableRecord { Value = reader.ReadInt32() };
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, options: new() { Generator = new() { Formatters = new() { UsesMapMode = usesMapMode } } }, languageVersion: LanguageVersion.CSharp9, testMethod: $"{nameof(CustomFormatterViaAttributeOnProperty)}({usesMapMode})");
    }

    [Theory, PairwiseData]
    public async Task UnionFormatter(ContainerKind container)
    {
        string testSource = """
[Union(0, typeof(Derived1))]
[Union(1, typeof(Derived2))]
internal interface IMyType
{
}

[MessagePackObject]
internal class Derived1 : IMyType {}

[MessagePackObject]
internal class Derived2 : IMyType {}

[MessagePackObject]
internal class MyMessagePackObject
{
    [Key(0)]
    internal IMyType UnionValue { get; set; }
}
""";
        testSource = TestUtilities.WrapTestSource(testSource, container);

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, testMethod: $"{nameof(UnionFormatter)}({container})");
    }

    [Fact]
    public async Task ArrayTypedProperty()
    {
        string testSource = """
using MessagePack;
using System.Collections.Generic;

[MessagePackObject]
internal class ContainerObject
{
    [Key(0)]
    internal SubObject[] ArrayOfCustomObjects { get; set; }

    [Key(1)]
    internal List<SubObject>[] ArrayOfCustomObjectList { get; set; }
}

[MessagePackObject]
internal class SubObject
{
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task Array2DJaggedProperty()
    {
        string testSource = """
using MessagePack;
using System.Collections.Generic;

[MessagePackObject]
internal class ContainerObject
{
    [Key(0)]
    internal int[][] TwoDimensionalJaggedIntArray { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericType()
    {
        string testSource = """
using MessagePack;
using System;

[MessagePackObject]
internal class ContainerObject
{
    [Key(0)]
    internal MyGenericType<int> TupleProperty { get; set; }
}

[MessagePackObject]
internal class MyGenericType<T>
{
    [Key(0)]
    internal T Value { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericTypeArg()
    {
        string testSource = """
using MessagePack;
using System;

[MessagePackObject]
public class GenericClass<T1, T2>
{
    [Key(0)]
    public T1 MyProperty0 { get; set; }

    [Key(1)]
    public T2 MyProperty1 { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task AdditionalAllowTypes()
    {
        string testSource = Preamble + """
class MyCustomType { }

[MessagePackObject]
class TypeWithAutoGeneratedFormatter
{
    [Key(0)]
    public MyCustomType Value { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(
            this.testOutputHelper,
            testSource,
            options: new()
            {
                AssumedFormattableTypes = ImmutableHashSet<FormattableType>.Empty
                    .Add(new FormattableType(new QualifiedNamedTypeName(TypeKind.Class) { Name = "MyCustomType" }, false)),
            });
    }

    [Fact]
    public async Task AdditionalFormatterTypes()
    {
        string testSource = Preamble + """
[assembly: MessagePackKnownFormatterAttribute(typeof(MyCustomTypeFormatter))]

[MessagePackObject]
class TypeWithAutoGeneratedFormatter
{
    [Key(0)]
    public MyCustomType Value { get; set; }
}
""";

        string defineSource = Preamble + """
        public class MyCustomType { }
        
        public class MyCustomTypeFormatter : MessagePack.Formatters.IMessagePackFormatter<MyCustomType>
        {
            public void Serialize(ref MessagePackWriter writer, MyCustomType value, MessagePackSerializerOptions options)
            {
                throw new System.NotImplementedException();
            }
        
            public MyCustomType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                throw new System.NotImplementedException();
            }
        }
        """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testSource },
                AdditionalProjects =
                {
                    ["DefiningProject"] = { Sources = { defineSource } },
                },
                AdditionalProjectReferences = { "DefiningProject" },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task AdditionalFormatterTypes_RedundantAttibute()
    {
        string testSource = Preamble + """
[assembly: MessagePackKnownFormatterAttribute(typeof(MyCustomTypeFormatter))]

class MyCustomType { }

class MyCustomTypeFormatter : MessagePack.Formatters.IMessagePackFormatter<MyCustomType>
{
    public void Serialize(ref MessagePackWriter writer, MyCustomType value, MessagePackSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    public MyCustomType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }
}

[MessagePackObject]
class TypeWithAutoGeneratedFormatter
{
    [Key(0)]
    public MyCustomType Value { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task ImplicitFormatterTypes()
    {
        string testSource = Preamble + """
class MyCustomType { }

class MyCustomTypeFormatter : MessagePack.Formatters.IMessagePackFormatter<MyCustomType>
{
    public void Serialize(ref MessagePackWriter writer, MyCustomType value, MessagePackSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    public MyCustomType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }
}

[MessagePackObject]
class TypeWithAutoGeneratedFormatter
{
    [Key(0)]
    public MyCustomType Value { get; set; }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NoGenerationForAnnotationOnly()
    {
        string testSource = """
using MessagePack;
using System;

[MessagePackObject]
internal class ContainerObject
{
    [Key(0)]
    internal MyGenericType<int> TupleProperty { get; set; }
}

[MessagePackObject]
internal class MyGenericType<T>
{
    [Key(0)]
    internal T Value { get; set; }
}
""";
        await new VerifyCS.Test(ReferencesSet.MessagePackAnnotations)
        {
            TestState =
            {
                Sources = { testSource },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task NestedMessagePackObjects()
    {
        string testSource = """
            using MessagePack;
            using System;

            namespace A
            {
                [MessagePackObject]
                internal class B
                {
                    [MessagePackObject]
                    internal class C
                    {
                    }
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NestedMessagePackObjects_Array()
    {
        string testSource = """
            using MessagePack;
            using System;

            namespace A
            {
                [MessagePackObject]
                internal class B
                {
                    [MessagePackObject]
                    internal class C
                    {
                    }

                    [Key(0)]
                    internal C[] array;
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task CustomFormatterWithSingletonPattern()
    {
        string testSource = """
            using MessagePack;
            using MessagePack.Formatters;
            class A {}
            class F : IMessagePackFormatter<A> {
                public static readonly IMessagePackFormatter<A> Instance = new F();
                private F() {}
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            [MessagePackObject]
            class G {
                [Key(0), MessagePackFormatter(typeof(F))]
                internal A a;
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task SpecifyFormatterFromStockSet()
    {
        string testSource = """
            using MessagePack;
            using MessagePack.Formatters;
            [MessagePackObject]
            class G {
                [Key(0), MessagePackFormatter(typeof(StringInterningFormatter))]
                internal string a;
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task ExcludeFormatterFromSourceGeneratedResolver()
    {
        string testSource = """
            using MessagePack;
            using MessagePack.Formatters;
            class A {}
            [ExcludeFormatterFromSourceGeneratedResolver]
            class F : IMessagePackFormatter<A> {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            """;

        // This test is *not* expected to produce any generated files.
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NonDefaultDeserializingConstructor_Private()
    {
        string testSource = """
            using MessagePack;
            [MessagePackObject]
            partial class A {
                [SerializationConstructor]
                A(int x) => this.X = x;

                [Key(0)]
                internal int X { get; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task DeserializingConstructorParameterMemberTypeAssignability_MemberAssignsToParamType()
    {
        string testSource = """
            using MessagePack;
            using System.Linq;
            using System.Collections.Generic;
            [MessagePackObject]
            class A {
                [SerializationConstructor]
                internal A(IReadOnlyList<int> x) => this.X = x.ToList();

                [Key(0)]
                internal List<int> X { get; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task DeserializingConstructorParameterMemberTypeAssignability_Incompatible()
    {
        string testSource = """
            using MessagePack;
            using System.Linq;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            [MessagePackObject]
            class A {
                [SerializationConstructor]
                internal A({|#0:ImmutableList<int> x|}) => this.X = x.ToList();

                [Key(0)]
                internal List<int> X { get; }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testSource },
            },
            ExpectedDiagnostics = { DiagnosticResult.CompilerError("MsgPack007").WithLocation(0) },
        }.RunDefaultAsync(this.testOutputHelper);
    }
}

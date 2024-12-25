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
public class MyMessagePackObject
{
    [Key(0)]
    public MyEnum EnumValue { get; set; }
}

public enum MyEnum
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
public class MyMessagePackObject
{
    [Key(0)]
    public NS1.MyEnum EnumValue1 { get; set; }

    [Key(1)]
    public NS2.MyEnum EnumValue2 { get; set; }
}

namespace NS1 {
    public enum MyEnum
    {
        A, B, C
    }
}

namespace NS2 {
    public enum MyEnum
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
public class MyMessagePackObject
{
    [Key(0)]
    public NS1.MyType<int> Value1 { get; set; }

    [Key(1)]
    public NS2.MyType<int> Value2 { get; set; }
}

namespace NS1 {
    [MessagePackObject]
    public class MyType<T>
    {
        [Key(0)]
        public string Foo { get; set; }
    }
}

namespace NS2 {
    [MessagePackObject]
    public class MyType<T>
    {
        [Key(0)]
        public string Foo { get; set; }
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
public class MyMessagePackObject
{
    [Key(0)]
    public NS1.MyType Value1 { get; set; }

    [Key(1)]
    public NS2.MyType Value2 { get; set; }
}

namespace NS1 {
    [MessagePackObject]
    public class MyType
    {
        [Key(0)]
        public string Foo { get; set; }
    }
}

namespace NS2 {
    [MessagePackObject]
    public class MyType
    {
        [Key(0)]
        public string Foo { get; set; }
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
public class MyMessagePackObject
{
    [Key(0)]
    public NS1.MyType Value1 { get; set; }

    [Key(1)]
    public NS2.MyType Value2 { get; set; }

    [Key(2)]
    public NS3.MyType<int> Value3 { get; set; }
}

namespace NS1 {
    public enum MyType
    {
        A, B
    }
}

namespace NS2 {
    [MessagePackObject]
    public class MyType
    {
        [Key(0)]
        public string Foo { get; set; }
    }
}

namespace NS3 {
    [MessagePackObject]
    public class MyType<T>
    {
        [Key(0)]
        public string Foo { get; set; }
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
public record HasPropertyWithCustomFormatterAttribute
{
    [Key(0), MessagePackFormatter(typeof(UnserializableRecordFormatter))]
    public UnserializableRecord CustomValue { get; set; }
}

public record UnserializableRecord
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
    public async Task CustomFormatterViaAttributeOnField(bool usesMapMode)
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject]
            public record HasFieldWithCustomFormatterAttribute
            {
                [Key(0), MessagePackFormatter(typeof(UnserializableRecordFormatter))]
                public UnserializableRecord CustomValue;
            }

            public record UnserializableRecord
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
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, options: new() { Generator = new() { Formatters = new() { UsesMapMode = usesMapMode } } }, languageVersion: LanguageVersion.CSharp9, testMethod: $"{nameof(CustomFormatterViaAttributeOnField)}({usesMapMode})");
    }

    [Theory, PairwiseData]
    public async Task UnionFormatter(ContainerKind container)
    {
        string testSource = """
[Union(0, typeof(Derived1))]
[Union(1, typeof(Derived2))]
public interface IMyType
{
}

[MessagePackObject]
public class Derived1 : IMyType {}

[MessagePackObject]
public class Derived2 : IMyType {}

[MessagePackObject]
public class MyMessagePackObject
{
    [Key(0)]
    public IMyType UnionValue { get; set; }
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
public class ContainerObject
{
    [Key(0)]
    public SubObject[] ArrayOfCustomObjects { get; set; }

    [Key(1)]
    public List<SubObject>[] ArrayOfCustomObjectList { get; set; }
}

[MessagePackObject]
public class SubObject
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
public class ContainerObject
{
    [Key(0)]
    public int[][] TwoDimensionalJaggedIntArray { get; set; }
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
public class ContainerObject
{
    [Key(0)]
    public MyGenericType<int> TupleProperty { get; set; }
}

[MessagePackObject]
public class MyGenericType<T>
{
    [Key(0)]
    public T Value { get; set; }
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
public class MyCustomType { }

[MessagePackObject]
public class TypeWithAutoGeneratedFormatter
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
public class TypeWithAutoGeneratedFormatter
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
    public async Task AdditionalFormatterTypes_RedundantAttribute()
    {
        string testSource = Preamble + """
[assembly: MessagePackKnownFormatterAttribute(typeof(MyCustomTypeFormatter))]

public class MyCustomType { }

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
public class TypeWithAutoGeneratedFormatter
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
public class MyCustomType { }

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
public class TypeWithAutoGeneratedFormatter
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
public class ContainerObject
{
    [Key(0)]
    public MyGenericType<int> TupleProperty { get; set; }
}

[MessagePackObject]
public class MyGenericType<T>
{
    [Key(0)]
    public T Value { get; set; }
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
                public class B
                {
                    [MessagePackObject]
                    public class C
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
                public class B
                {
                    [MessagePackObject]
                    public class C
                    {
                    }

                    [Key(0)]
                    public C[] array;
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
            public class A {}
            class F : IMessagePackFormatter<A> {
                public static readonly IMessagePackFormatter<A> Instance = new F();
                private F() {}
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) {}
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => default;
            }
            [MessagePackObject]
            public class G {
                [Key(0), MessagePackFormatter(typeof(F))]
                public A a;
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
            public class G {
                [Key(0), MessagePackFormatter(typeof(StringInterningFormatter))]
                public string a;
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
            [MessagePackObject(AllowPrivate = true)]
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
            public class A {
                [SerializationConstructor]
                public A(IReadOnlyList<int> x) => this.X = x.ToList();

                [Key(0)]
                public List<int> X { get; }
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
                internal A({|#0:ImmutableList<int?> x|}) => this.X = x.ToList();

                [Key(0)]
                internal List<int?> X { get; }
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

    [Fact]
    public async Task NoPrivateAccessNeeded()
    {
        // Test case came from https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/1993
        string testSource = /* lang=c#-test */ """
            using MessagePack;

            [MessagePackObject]
            public class Test3
            {
                [Key(0)]
                public string Name { get; private set; }

                [SerializationConstructor]
                public Test3(string name) => Name = name;
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task NoPrivateMemberCollected()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using System.Collections.Generic;

            [MessagePackObject]
            public class Test
            {
                protected struct Info
                {
                }

                [Key(0)]
                public string A { get; set; }

                protected List<Info> B;

                protected List<Info> C { get; set; }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericDataTypeWithPrivateMember()
    {
        string testSource = /* lang=c#-test */ """
            #pragma warning disable CS0169 // unused field
            using MessagePack;

            [MessagePackObject(AllowPrivate = true)]
            internal partial class HasPrivateSerializedMemberAndIsGenericOrdinal<T>
                where T : struct
            {
                [Key(0)]
                private int value;
            }

            [MessagePackObject(true, AllowPrivate = true)]
            internal partial class HasPrivateSerializedMemberAndIsGenericNamed<T>
                where T : struct
            {
                private int value;
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericWithConstraintsThatReferenceTypeParameter()
    {
        string testSource = /* lang=c#-test */ """
            using System.Collections.Generic;
            using MessagePack;

            [MessagePackObject]
            public class GenericConstrainedClassIntKey<T1, T2>
                where T1 : class
                where T2 : class, IEqualityComparer<T1>
            {
                [Key(0)]
                public T1 MyProperty0 { get; set; }

                [Key(1)]
                public T2 Comparer { get; set; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact(Skip = "We need to refactor the unpleasant Resources-based tests.")]
    public async Task InterfaceUnionUsedWithNullableRefAnnotation()
    {
        string testSource = /* lang=c#-test */ """
            #nullable enable
            using MessagePack;

            [Union(0, typeof(Derived1))]
            [Union(1, typeof(Derived2))]
            public interface IMyType
            {
            }

            [MessagePackObject]
            public class Derived1 : IMyType {}

            [MessagePackObject]
            public class Derived2 : IMyType {}

            [MessagePackObject]
            public class UnionContainer
            {
                [Key(0)]
                public IMyType? Value { get; set; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, languageVersion: LanguageVersion.CSharp11);
    }

    [Fact]
    public async Task NoFieldWithCustomFormatterCollected()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject]
            public class Test
            {
                [Key(0)]
                [MessagePackFormatter(typeof(ListAFormatter))]
                public List<A> A;
            }

            public class A
            {
            }

            [ExcludeFormatterFromSourceGeneratedResolver]
            class ListAFormatter : IMessagePackFormatter<List<A>>
            {
                public void Serialize(ref MessagePackWriter writer, List<A> value, MessagePackSerializerOptions options)
                {
                }

                public List<A> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    throw new NotImplementedException();
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task GenericCustomFormatter()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject(AllowPrivate = true)]
            public partial class SampleObject
            {
                [Key(0)]
                [MessagePackFormatter(typeof(ValueTupleFormatter<string>))]
                private ValueTuple<string> TupleTest;
            }

            public sealed class ValueTupleFormatter<T1> : IMessagePackFormatter<ValueTuple<T1>>
            {
                public void Serialize(ref MessagePackWriter writer, ValueTuple<T1> value, MessagePackSerializerOptions options)
                {
                    throw new NotImplementedException();
                }

                public ValueTuple<T1> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task SimplifiedNameForKeywordsAndSyntaxSugar()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject(AllowPrivate = true)]
            public partial class SampleObject
            {
                [Key(0)]
                public (bool, byte, sbyte) Value1;

                [Key(1)]
                public (short, ushort, int, uint) Value2;

                [Key(2)]
                public (long, ulong, float, double) Value3;

                [Key(3)]
                public (decimal, char, string, object) Value4;

                [Key(4)]
                public ValueTuple<int?> Value5;
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task SystemCollectionsObjectModelCollection()
    {
        string testSource = /* lang=c#-test */ """
            using MessagePack;
            using System.Collections.ObjectModel;

            [MessagePackObject]
            public class A
            {
                [Key(0)]
                public Collection<int> SampleCollection { get; set; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task CustomFormatterForGenericWithConstraints()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject]
            public partial class SampleObject
            {
                [Key(0)]
                public MyGenericObject<string> Value;
            }

            public class MyGenericObject<T> where T : ICloneable { }

            public sealed class MyGenericObjectFormatter<T> : IMessagePackFormatter<MyGenericObject<T>> where T : ICloneable
            {
                public void Serialize(ref MessagePackWriter writer, MyGenericObject<T> value, MessagePackSerializerOptions options)
                {
                    throw new NotImplementedException();
                }

                public MyGenericObject<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task EmbeddedTypes()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using System.Buffers;
            using System.Collections.Generic;
            using System.Numerics;
            using MessagePack;

            [MessagePackObject]
            public class A
            {
                [Key(0)]
                public Memory<byte> Value1 { get; set; }

                [Key(1)]
                public Memory<byte>? Value2 { get; set; }

                [Key(2)]
                public ReadOnlyMemory<byte> Value3 { get; set; }

                [Key(3)]
                public ReadOnlyMemory<byte>? Value4 { get; set; }

                [Key(4)]
                public ReadOnlySequence<byte> Value5 { get; set; }

                [Key(5)]
                public ReadOnlySequence<byte>? Value6 { get; set; }

                [Key(6)]
                public List<Int16> Value7 { get; set; }

                [Key(7)]
                public List<Int32> Value8 { get; set; }

                [Key(8)]
                public List<Int64> Value9 { get; set; }

                [Key(9)]
                public List<UInt16> Value10 { get; set; }

                [Key(10)]
                public List<UInt32> Value11 { get; set; }

                [Key(11)]
                public List<UInt64> Value12 { get; set; }

                [Key(12)]
                public List<Single> Value13 { get; set; }

                [Key(13)]
                public List<Double> Value14 { get; set; }

                [Key(14)]
                public List<Boolean> Value15 { get; set; }

                [Key(15)]
                public List<byte> Value16 { get; set; }

                [Key(16)]
                public List<SByte> Value17 { get; set; }

                [Key(17)]
                public List<Char> Value18 { get; set; }

                [Key(18)]
                public List<DateTime> Value19 { get; set; }

                [Key(19)]
                public List<string> Value20 { get; set; }

                [Key(20)]
                public List<object> Value21 { get; set; }

                [Key(21)]
                public object[] Value22 { get; set; }

                [Key(22)]
                public BigInteger Value23 { get; set; }

                [Key(23)]
                public BigInteger? Value24 { get; set; }

                [Key(24)]
                public Complex Value25 { get; set; }

                [Key(25)]
                public Complex? Value26 { get; set; }

                [Key(26)]
                public Vector2 Value27 { get; set; }

                [Key(27)]
                public Vector2? Value28 { get; set; }

                [Key(28)]
                public Vector3 Value29 { get; set; }

                [Key(29)]
                public Vector3? Value30 { get; set; }

                [Key(30)]
                public Vector4 Value31 { get; set; }

                [Key(31)]
                public Vector4? Value32 { get; set; }

                [Key(32)]
                public Quaternion Value33 { get; set; }

                [Key(33)]
                public Quaternion? Value34 { get; set; }

                [Key(34)]
                public Matrix3x2 Value135 { get; set; }

                [Key(35)]
                public Matrix3x2? Value36 { get; set; }

                [Key(36)]
                public Matrix4x4 Value137 { get; set; }

                [Key(37)]
                public Matrix4x4? Value38 { get; set; }

                [Key(38)]
                public ArraySegment<byte> Value39 { get; set; }

                [Key(39)]
                public ArraySegment<byte>? Value40 { get; set; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task SystemMemoryTypes()
    {
        string testSource = /* lang=c#-test */ """
            using System;
            using System.Buffers;
            using MessagePack;

            [MessagePackObject]
            public class A
            {
                [Key(0)]
                public Memory<int> Value1 { get; set; }

                [Key(1)]
                public ReadOnlyMemory<int> Value2 { get; set; }

                [Key(2)]
                public ReadOnlySequence<int> Value3 { get; set; }
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}

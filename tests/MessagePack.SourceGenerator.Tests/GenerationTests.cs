// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Tests;
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
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, options: new() { Generator = new() { Formatters = new() { UsesMapMode = usesMapMode } } }, testMethod: $"{nameof(CustomFormatterViaAttributeOnProperty)}({usesMapMode})");
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

[MessagePackObject]
internal class ContainerObject
{
    [Key(0)]
    internal SubObject[] ArrayOfCustomObjects { get; set; }
}

[MessagePackObject]
internal class SubObject
{
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
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource, options: new() { AssumedFormattableTypes = ImmutableHashSet<string>.Empty.Add("MyCustomType") });
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
}

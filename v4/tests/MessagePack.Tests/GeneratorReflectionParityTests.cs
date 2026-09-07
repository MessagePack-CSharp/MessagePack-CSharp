extern alias V3;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The generated and reflection interpretations of one annotated type must agree: a project
// that turns the generator on (or off) must not change its wire. Each case here was a
// divergence found by the 2026-09-04 pre-release review; the types live in this assembly so
// the generator covers them, the reflection chain below re-reads the same declarations, and
// the v3 annotations (extern alias) let the MessagePack-CSharp oracle see them too.
public class GeneratorReflectionParityTests
{
    static readonly MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static void AssertTiersAgree<T>(T value, string expectedJson)
    {
        var generated = V4.Serialize(value);
        var reflection = V4.Serialize(value, reflectionOptions);
        Assert.Equal(expectedJson, V4.ConvertToJson(generated));
        Assert.Equal(generated, reflection);
    }

    // map mode ignores a stray int [Key] (v3: KeyAsPropertyName wins); the generator used
    // to refuse the type as mixing key kinds
    [Fact]
    public void MapMode_StrayIntKey_IsIgnoredByBothTiers()
    {
        var value = new ParityMapIntKey { Value = 7 };
        AssertTiersAgree(value, "{\"Value\":7}");
        Assert.Equal(Oracle.Serialize(value), V4.Serialize(value));
    }

    // map mode drops a name-defaulted member that is neither writable nor consumed by the
    // constructor (v3's dynamic rule, the reflection tier's filter): a computed getter-only
    // property is never touched. v3's mpc wrote such members; the generator now follows the
    // runtime rule so the tiers agree. An explicit [Key] or a constructor binding keeps one.
    [Fact]
    public void MapMode_ComputedGetterOnly_IsExcludedByBothTiers()
    {
        AssertTiersAgree(new ParityComputedMap { Value = 7 }, "{\"Value\":7}");
        AssertTiersAgree(new ParityComputedMapWithConstructor(7), "{\"Value\":7}");
        AssertTiersAgree(new ParityConstructorConsumedGetterOnly(7), "{\"Value\":7}");

        // an explicit [Key] on a getter-only property is a serialize-only contract in both tiers
        // (the generator's MsgPack007 warns about the skipped read, so the type is compiled here)
        var explicitKey = AnalyzerTestHost.CompileAndLoad("""
            using MessagePack;
            [MessagePackObject(true)]
            public class ExplicitKeyGetterOnly
            {
                public ExplicitKeyGetterOnly(int value) => Value = value;
                public int Value { get; }
                [Key("Computed")] public int Computed => 99;
            }
            """, withGenerator: true).GetType("ExplicitKeyGetterOnly")!;
        var instance = Activator.CreateInstance(explicitKey, 7)!;
        var generated = V4.Serialize(explicitKey, instance);
        Assert.Equal("{\"Value\":7,\"Computed\":99}", V4.ConvertToJson(generated));
        Assert.Equal(generated, V4.Serialize(explicitKey, instance, reflectionOptions));
    }

    // a [SerializationConstructor] parameter binds a member of an assignable type (object
    // takes a string, an interface takes an implementation), the runtime's IsAssignableFrom
    // rule; the generator used to demand the exact type
    [Fact]
    public void Constructor_AssignableParameterType_BindsInBothTiers()
    {
        var value = new ParityAssignableConstructor("hello", [1, 2]);
        AssertTiersAgree(value, "[\"hello\",[1,2]]");
        Assert.Equal(Oracle.Serialize(value), V4.Serialize(value));

        var back = V4.Deserialize<ParityAssignableConstructor>(V4.Serialize(value))!;
        Assert.Equal("hello", back.Value);
        Assert.Equal([1, 2], back.Items);
    }

    // an override that declares a different [Key] than the declaration it overrides keys
    // the same storage twice: MsgPack021 from the generator, the same refusal at runtime
    [Fact]
    public void Override_ChangingTheKey_IsRefusedByBothTiers()
    {
        const string source = """
            using MessagePack;
            [MessagePackObject] public class B { [Key(0)] public virtual int P { get; set; } }
            [MessagePackObject] public class D : B { [Key(1)] public override int P { get; set; } }
            """;
        var result = RunGenerator(source, out var compilation);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack021");

        var derived = LoadWithoutGenerator(compilation).GetType("D")!;
        var serialize = typeof(V4).GetMethod(nameof(V4.Serialize), [Type.MakeGenericMethodParameter(0), typeof(MessagePackSerializerOptions)])!.MakeGenericMethod(derived);
        var thrown = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => serialize.Invoke(null, [Activator.CreateInstance(derived), reflectionOptions]));
        Assert.IsType<MessagePackSerializationException>(thrown.InnerException);
        Assert.Contains("different [Key]", thrown.InnerException!.Message);
    }

    // AllowPrivate reaches the declaring type's privates through the nested formatter, not a
    // base type's; that shape used to produce CS0122 in the generated code, now it is skipped
    // (MsgPack005) and left to the reflection tier, which reads it
    [Fact]
    public void AllowPrivate_BasePrivateMember_IsSkippedWithMsgPack005()
    {
        const string source = """
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)] public partial class B { [Key(0)] private int first = 1; }
            [MessagePackObject(AllowPrivate = true)] public partial class D : B { [Key(1)] private int second = 2; }
            """;
        var result = RunGenerator(source, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("not accessible from"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    // a [NonSerialized] base field must not reserve the plain name from a derived same-named
    // field: exclusion happens before shadow grouping, so both tiers write {"Value":7}
    [Fact]
    public void NonSerializedBaseField_DoesNotQualifyTheDerivedMapKey()
    {
        AssertTiersAgree(new ParityNonSerializedShadowDerived { Value = 7 }, "{\"Value\":7}");
        Assert.Equal(7, V4.Deserialize<ParityNonSerializedShadowDerived>(V4.Serialize(new ParityNonSerializedShadowDerived { Value = 7 }), reflectionOptions)!.Value);
    }

    // keyword-named declaring types and type parameters are escaped in the emitted
    // declarations, type-parameter lists and where clauses
    [Fact]
    public void KeywordNamedTypesAndTypeParameters_Compile()
    {
        var assembly = AnalyzerTestHost.CompileAndLoad("""
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)] public partial class @event { [Key(0)] private int value = 3; [IgnoreMember] public int Value => value; }
            [MessagePackObject] public class Model<@class> where @class : class { [Key(0)] public @class? Item { get; set; } }
            [MessagePackObject] public class Holder { [Key(0)] public Model<string>? Model { get; set; } }
            """, withGenerator: true);
        var keywordType = assembly.GetType("event")!;
        var bytes = V4.Serialize(keywordType, Activator.CreateInstance(keywordType));
        Assert.Equal("[3]", V4.ConvertToJson(bytes));
        var holder = assembly.GetType("Holder")!;
        Assert.Equal("[null]", V4.ConvertToJson(V4.Serialize(holder, Activator.CreateInstance(holder))));
    }

    // a public base property with a private accessor is as unreachable from the nested
    // formatter as a private member: skipped (MsgPack005), served by the reflection tier
    [Theory]
    [InlineData("[Key(0)] public int First { get; private set; } = 1;")]
    [InlineData("[Key(0)] public int First { private get; set; } = 1;")]
    public void AllowPrivate_BasePrivateAccessor_IsSkippedWithMsgPack005(string baseMember)
    {
        var source = $$"""
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)] public partial class B { {{baseMember}} }
            [MessagePackObject(AllowPrivate = true)] public partial class D : B { [Key(1)] private int second = 2; }
            """;
        var result = RunGenerator(source, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("not accessible from"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    // a shadowed base member is reached through a base cast, `((Base)value).Value`; a protected
    // member is accessible through the derived type but not through that cast (CS1540), so the
    // judgement uses the emitted expression's static type and the type is skipped
    [Fact]
    public void AllowPrivate_ShadowedProtectedBaseMember_IsSkippedWithMsgPack005()
    {
        const string source = """
            using MessagePack;
            public class Base { [Key(0)] protected int Value = 7; }
            [MessagePackObject(AllowPrivate = true)] public partial class Model : Base { [Key(1)] public new int Value = 8; }
            """;
        var result = RunGenerator(source, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("not accessible from"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        // the reflection tier writes both declarations
        var model = LoadWithoutGenerator(updated).GetType("Model")!;
        Assert.Equal("[7,8]", V4.ConvertToJson(V4.Serialize(model, Activator.CreateInstance(model), reflectionOptions)));
    }

    // AllowPrivate over a base type from another assembly: the compilation never sees that
    // base's private members (reference assemblies strip them, implementation assemblies are
    // imported without them), so the generator cannot build a trustworthy table and leaves
    // the type to the reflection tier, which reads them like v3 did
    [Fact]
    public void AllowPrivate_ExternalBase_IsSkippedWithMsgPack005_AndReflectionReadsIt()
    {
        var libraryBytes = Emit("""
            using MessagePack;
            public class ExternalBase { [Key(0)] private int first = 7; public void SetFirst(int value) => first = value; public int ReadFirst() => first; }
            """);
        var library = System.Reflection.Assembly.Load(libraryBytes);
        // byte-loaded assemblies are not found by name: hand the consumer's reference to it back
        System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == library.GetName().Name ? library : null;
        var consumerSource = """
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)] public partial class Derived : ExternalBase { [Key(1)] public int Second { get; set; } = 8; }
            """;
        var result = RunGenerator(consumerSource, out var updated, MetadataReference.CreateFromImage(libraryBytes));
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("another assembly"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        var derived = LoadWithoutGenerator(updated).GetType("Derived")!;
        var instance = Activator.CreateInstance(derived)!;
        library.GetType("ExternalBase")!.GetMethod("SetFirst")!.Invoke(instance, [42]);
        var bytes = V4.Serialize(derived, instance, reflectionOptions);
        Assert.Equal("[42,8]", V4.ConvertToJson(bytes));
        var back = V4.Deserialize(derived, bytes, reflectionOptions)!;
        Assert.Equal(42, library.GetType("ExternalBase")!.GetMethod("ReadFirst")!.Invoke(back, null));
    }

    static byte[] Emit(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "ParityLibrary" + Guid.NewGuid().ToString("N"));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        return stream.ToArray();
    }

    // the emitted construction must bind the SELECTED constructor: with an object parameter
    // bound to a string member, C# overload resolution would otherwise pick a string overload
    [Fact]
    public void Constructor_SelectedOverload_IsTheOneCalled()
    {
        var back = V4.Deserialize<ParityOverloadedConstructor>(V4.Serialize(new ParityOverloadedConstructor("x")))!;
        Assert.Equal("selected-object", back.Chosen);
        Assert.Equal("selected-object", V4.Deserialize<ParityOverloadedConstructor>(V4.Serialize(new ParityOverloadedConstructor("x")), reflectionOptions)!.Chosen);
    }

    static GeneratorDriverRunResult RunGenerator(string source, out Compilation updated, params MetadataReference[] additionalReferences)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "ParityProbe" + Guid.NewGuid().ToString("N")).AddReferences(additionalReferences);
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out updated, out _, CancellationToken.None).GetRunResult();
    }

    // the plain compilation (generator output removed) so the runtime tier is the only one
    // that can serve the type
    static System.Reflection.Assembly LoadWithoutGenerator(Compilation compilation)
    {
        var plain = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(compilation.SyntaxTrees.First());
        using var stream = new MemoryStream();
        var emitted = plain.Emit(stream);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        return System.Reflection.Assembly.Load(stream.ToArray());
    }
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityMapIntKey
{
    [V3::MessagePack.Key(0)] public int Value { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityComputedMap
{
    public int Value { get; set; }
    public int Computed => 99;
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityComputedMapWithConstructor
{
    public ParityComputedMapWithConstructor(int value) => Value = value;
    public int Value { get; }
    public int Computed => 99;
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityConstructorConsumedGetterOnly
{
    public ParityConstructorConsumedGetterOnly(int value) => Value = value;
    public int Value { get; }
}

[V3::MessagePack.MessagePackObject]
public class ParityAssignableConstructor
{
    [V3::MessagePack.SerializationConstructor]
    public ParityAssignableConstructor(object value, IEnumerable<int> items)
    {
        Value = (string?)value;
        Items = [.. items];
    }

    [V3::MessagePack.Key(0)] public string? Value { get; }
    [V3::MessagePack.Key(1)] public List<int> Items { get; }
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityNonSerializedShadowBase
{
    [NonSerialized] public int Value;
}

[V3::MessagePack.MessagePackObject(true)]
public class ParityNonSerializedShadowDerived : ParityNonSerializedShadowBase
{
    public new int Value;
}

[V3::MessagePack.MessagePackObject]
public class ParityOverloadedConstructor
{
    [V3::MessagePack.SerializationConstructor]
    public ParityOverloadedConstructor(object value)
    {
        Value = (string?)value;
        Chosen = "selected-object";
    }

    public ParityOverloadedConstructor(string value)
    {
        Value = value;
        Chosen = "other-string";
    }

    [V3::MessagePack.Key(0)] public string? Value { get; }
    [V3::MessagePack.IgnoreMember] public string Chosen { get; }
}

using System.Reflection;
using System.Reflection.Emit;
using MessagePack;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;
using Ultra = UltraMessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// The attributed half of ReflectionObjectFormatter: [MessagePackObject] types served at
// runtime with the keyed wire forms. The verification anchor is byte-exact equality
// against BOTH the source-generated formatter for the same type (SourceGenerated tier
// deliberately omitted from the chain below, so the reflection tier claims types the
// generator already covered) and MessagePack-CSharp as the cross-library oracle.
public class ReflectionObjectFormatterTests
{
    static readonly UltraMessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static void AssertReflectionMatchesOracle<T>(T value)
    {
        var reflection = Ultra.Serialize(value, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), reflection);

        var back = Ultra.Deserialize<T>(reflection, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), Oracle.Serialize(back));
    }

    static void AssertReflectionMatchesGenerated<T>(T value)
    {
        // options-less = Default chain, whose SourceGenerated tier serves these types
        Assert.Equal(Ultra.Serialize(value), Ultra.Serialize(value, reflectionOptions));
        AssertReflectionMatchesOracle(value);
    }

    [Fact]
    public void IntKey_ByteIdenticalToGeneratedFormatter()
    {
        AssertReflectionMatchesGenerated(new GenIntKeyPoco { Id = 42, Name = "山岡士郎", Score = 98.5, Flag = true, Ticks = 5_000_000_000L, Ignored = 999 });
        AssertReflectionMatchesGenerated(new GenIntKeyPoco { Id = -1, Name = null, Score = double.NaN, Flag = false, Ticks = long.MinValue });
        AssertReflectionMatchesGenerated<GenIntKeyPoco?>(null);
    }

    [Fact]
    public void IntKey_HolesSerializeAsNil()
    {
        var value = new GenHolePoco { A = 100000, B = "b", C = 255 };
        AssertReflectionMatchesGenerated(value);

        var bytes = Ultra.Serialize(value, reflectionOptions);
        Assert.Equal(0x98, bytes[0]); // fixarray(8): max key 7
        Assert.Equal(0xc0, bytes[1]); // hole at key 0
    }

    [Fact]
    public void StringKey_UsesDeclaredKeys()
    {
        var value = new GenStringKeyPoco { Id = 7, Name = "name", F = 1.25f };
        AssertReflectionMatchesGenerated(value);

        var map = Ultra.Deserialize<Dictionary<string, object?>>(Ultra.Serialize(value, reflectionOptions), reflectionOptions)!;
        Assert.Equal(["f", "id", "name"], map.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void KeyAsPropertyName_UsesMemberNames()
    {
        var value = new GenAutoKeyPoco { X = 9, Y = "auto" };
        AssertReflectionMatchesGenerated(value);

        var map = Ultra.Deserialize<Dictionary<string, object?>>(Ultra.Serialize(value, reflectionOptions), reflectionOptions)!;
        Assert.Equal(["X", "Y"], map.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void IgnoreMember_StaysOffTheWire()
    {
        var value = new GenIntKeyPoco { Id = 1, Ignored = 12345 };
        var back = Ultra.Deserialize<GenIntKeyPoco>(Ultra.Serialize(value, reflectionOptions), reflectionOptions)!;
        Assert.Equal(1, back.Id);
        Assert.Equal(0, back.Ignored);
    }

    [Fact]
    public void Nested_ResolvesMemberFormattersThroughTheChain()
    {
        AssertReflectionMatchesGenerated(new GenNestedPoco
        {
            Child = new GenIntKeyPoco { Id = 1, Name = "c", Score = 2.5, Flag = true, Ticks = 3 },
            Numbers = [1, -1, 128, -129, 70000, int.MinValue],
            Tags = ["a", "こんにちは", ""],
            Map = new Dictionary<string, int> { ["one"] = 1, ["big"] = 100000 },
            Stamp = new DateTime(2026, 7, 21, 1, 2, 3, 456, DateTimeKind.Utc),
        });
        AssertReflectionMatchesGenerated(new GenNestedPoco());
    }

    [Fact]
    public void Struct_RoundtripsAndRejectsNil()
    {
        AssertReflectionMatchesGenerated(new GenStructPoco { A = 42, B = -1.5 });
        Assert.Throws<UltraMessagePack.MessagePackSerializationException>(
            () => Ultra.Deserialize<GenStructPoco>([0xc0], reflectionOptions));
    }

    [Fact]
    public void VersionTolerance_ExtraAndMissingTrailingEntries()
    {
        // newer writer, older reader: keys 5/6 are skipped
        var newer = Ultra.Serialize(new GenIntKeyPocoV2 { Id = 42, Name = "n", Score = 1.5, Extra = "x", ExtraList = [1, 2] }, reflectionOptions);
        var old = Ultra.Deserialize<GenIntKeyPoco>(newer, reflectionOptions)!;
        Assert.Equal(42, old.Id);
        Assert.Equal("n", old.Name);

        // older writer, newer reader: entries past the payload keep their defaults
        var older = Ultra.Serialize(new GenIntKeyPocoV0 { Id = 7, Name = "v0" }, reflectionOptions);
        var current = Ultra.Deserialize<GenIntKeyPoco>(older, reflectionOptions)!;
        Assert.Equal(7, current.Id);
        Assert.Equal("v0", current.Name);
        Assert.Equal(0, current.Ticks);
    }

    [Fact]
    public void AttributedRecord_ConstructsThroughTheArgumentPath()
    {
        // generic: the UMP generator skips the shape, so even the Default chain would land
        // on reflection; the positional record has no parameterless constructor, proving
        // the [Key]-ordered array payload drives the constructor-argument path
        var value = new ReflectionRecordPerson<object>(31, "rec");
        var bytes = Ultra.Serialize(value, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(value, Ultra.Deserialize<ReflectionRecordPerson<object>>(bytes, reflectionOptions));
    }

    [Fact]
    public void DefaultChain_FallsBackToReflectionForGeneratorSkippedTypes()
    {
        // options-less entry points: SourceGenerated has nothing for the closed generic,
        // BuiltIn/Generic decline, the annotated reflection tail serves it
        var value = new ReflectionGenericHolder<object> { Value = 77 };
        var bytes = Ultra.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(77, Ultra.Deserialize<ReflectionGenericHolder<object>>(bytes)!.Value);
    }

    [Fact]
    public void AnnotatedOnly_DeclinesUnannotatedTypes()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Ultra.Serialize(new PlainPoco { Value = 1 }, reflectionOptions));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void DefaultChainOrder_RegistryStillWinsOverReflection()
    {
        // a private registry stands in for "the generator covered this type": it must win
        // over the reflection tail exactly like the real Default composition
        var registry = new SourceGeneratedFormatterFactory();
        var chain = new UltraMessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            registry,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        // annotated resolves through the tail, unannotated still throws
        Assert.Equal(5, Ultra.Deserialize<GenIntKeyPoco>(Ultra.Serialize(new GenIntKeyPoco { Id = 5 }, chain), chain)!.Id);
        Assert.Throws<InvalidOperationException>(() => Ultra.Serialize(new PlainPoco(), chain));
    }

    [Fact]
    public void CatchAllMode_StillHonorsAnnotations()
    {
        // v3 ContractlessStandardResolver semantics: the contractless catch-all serializes
        // annotated types in their KEYED wire form, not as a member-name map
        var catchAll = new UltraMessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(),
        ]));

        var value = new GenIntKeyPoco { Id = 3, Name = "k" };
        Assert.Equal(Oracle.Serialize(value), Ultra.Serialize(value, catchAll)); // array format

        // and attribute-free types keep the contractless map form
        var map = Ultra.Deserialize<Dictionary<string, object?>>(Ultra.Serialize(new PlainPoco { Value = 8 }, catchAll), catchAll)!;
        Assert.Equal(8, Convert.ToInt32(map["Value"]));
    }

    // ---- invalid shapes: the source generator rejects these at compile time (UMP001/2/3/8),
    // so they are built at runtime and must fail with the equivalent runtime error ----

    [Fact]
    public void MissingKeyOnPublicMember_Throws()
    {
        var type = BuildAnnotatedType("MissingKey", keyAsPropertyName: false, [("Id", typeof(int), 0), ("Loose", typeof(int), null)]);
        var ex = AssertInitializeThrows(type);
        Assert.Contains("needs [Key] or [IgnoreMember]", ex.Message);
    }

    [Fact]
    public void MixedIntAndStringKeys_Throws()
    {
        var type = BuildAnnotatedType("MixedKeys", keyAsPropertyName: false, [("A", typeof(int), 0), ("B", typeof(int), "b")]);
        var ex = AssertInitializeThrows(type);
        Assert.Contains("mixes int keys and string keys", ex.Message);
    }

    [Fact]
    public void DuplicateKey_Throws()
    {
        var type = BuildAnnotatedType("DuplicateKey", keyAsPropertyName: false, [("A", typeof(int), 1), ("B", typeof(int), 1)]);
        var ex = AssertInitializeThrows(type);
        Assert.Contains("more than once", ex.Message);
    }

    [Fact]
    public void NegativeKey_Throws()
    {
        var type = BuildAnnotatedType("NegativeKey", keyAsPropertyName: false, [("A", typeof(int), -1)]);
        var ex = AssertInitializeThrows(type);
        Assert.Contains("negative key", ex.Message);
    }

    [Fact]
    public void AllowPrivateAttributeProperty_WidensDiscovery()
    {
        if (typeof(MessagePackObjectAttribute).GetProperty("AllowPrivate") is null)
        {
            return; // attribute version without AllowPrivate: nothing to widen
        }

        var type = BuildAnnotatedType("PrivateKeyed", keyAsPropertyName: false,
            [("Pub", typeof(int), 0), ("priv", typeof(int), 1)],
            privateFields: ["priv"],
            namedArguments: [("AllowPrivate", true)]);

        var value = Activator.CreateInstance(type)!;
        type.GetField("Pub")!.SetValue(value, 10);
        type.GetField("priv", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(value, 20);

        var bytes = Serialize(value, type);
        Assert.Equal(0x92, bytes[0]); // fixarray(2): the private member is on the wire

        var back = Deserialize(bytes, type)!;
        Assert.Equal(10, type.GetField("Pub")!.GetValue(back));
        Assert.Equal(20, type.GetField("priv", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(back));
    }

    static byte[] Serialize(object? value, Type type)
        => Ultra.Serialize(type, value, reflectionOptions);

    static object? Deserialize(byte[] bytes, Type type)
        => Ultra.Deserialize(type, bytes, reflectionOptions);

    static UltraMessagePack.MessagePackSerializationException AssertInitializeThrows(Type type)
    {
        var exception = Record.Exception(() => Serialize(Activator.CreateInstance(type), type));
        while (exception is TargetInvocationException { InnerException: { } inner })
        {
            exception = inner;
        }
        return Assert.IsType<UltraMessagePack.MessagePackSerializationException>(exception);
    }

    // Runtime-built [MessagePackObject] types out of the source generators' sight (both
    // UMP's and MessagePack-CSharp's analyzers reject the invalid shapes at compile time).
    // Fields only: TypeBuilder fields take custom attributes without accessor plumbing.
    static Type BuildAnnotatedType(
        string name,
        bool keyAsPropertyName,
        (string Name, Type Type, object? Key)[] fields,
        string[]? privateFields = null,
        (string Name, object Value)[]? namedArguments = null)
    {
        var module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("ReflectionObjectFormatterTests.Dynamic"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("main");
        var typeBuilder = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);

        var objectCtor = typeof(MessagePackObjectAttribute).GetConstructor([typeof(bool)])!;
        var namedProperties = (namedArguments ?? []).Select(a => typeof(MessagePackObjectAttribute).GetProperty(a.Name)!).ToArray();
        var namedValues = (namedArguments ?? []).Select(a => a.Value).ToArray();
        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(objectCtor, [keyAsPropertyName], namedProperties, namedValues));

        foreach (var (fieldName, fieldType, key) in fields)
        {
            var visibility = privateFields is not null && privateFields.Contains(fieldName) ? FieldAttributes.Private : FieldAttributes.Public;
            var field = typeBuilder.DefineField(fieldName, fieldType, visibility);
            if (key is int intKey)
            {
                field.SetCustomAttribute(new CustomAttributeBuilder(typeof(KeyAttribute).GetConstructor([typeof(int)])!, [intKey]));
            }
            else if (key is string stringKey)
            {
                field.SetCustomAttribute(new CustomAttributeBuilder(typeof(KeyAttribute).GetConstructor([typeof(string)])!, [stringKey]));
            }
        }

        return typeBuilder.CreateType()!;
    }
}

public class PlainPoco
{
    public int Value { get; set; }
}

// generic shapes: skipped by the UMP source generator (UMP005, NoWarn'd in the csproj —
// generator diagnostics ignore #pragma), which is exactly what makes them exercise the
// reflection tier through the real Default chain
[MessagePackObject]
public record ReflectionRecordPerson<TUnused>([property: Key(0)] int Age, [property: Key(1)] string? Name);

[MessagePackObject]
public class ReflectionGenericHolder<TUnused>
{
    [Key(0)] public int Value { get; set; }
}

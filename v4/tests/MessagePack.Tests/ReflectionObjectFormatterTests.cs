extern alias V3;
using System.Reflection;
using System.Reflection.Emit;
using SerializerFoundation;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The attributed half of ReflectionObjectFormatter: [MessagePackObject] types served at
// runtime with the keyed wire forms. The verification anchor is byte-exact equality
// against BOTH the source-generated formatter for the same type (SourceGenerated tier
// deliberately omitted from the chain below, so the reflection tier claims types the
// generator already covered) and MessagePack-CSharp as the cross-library oracle.
public class ReflectionObjectFormatterTests
{
    static readonly MessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static void AssertReflectionMatchesOracle<T>(T value)
    {
        var reflection = V4.Serialize(value, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), reflection);

        var back = V4.Deserialize<T>(reflection, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), Oracle.Serialize(back));
    }

    static void AssertReflectionMatchesGenerated<T>(T value)
    {
        // options-less = Default chain, whose SourceGenerated tier serves these types
        Assert.Equal(V4.Serialize(value), V4.Serialize(value, reflectionOptions));
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
    public void StringKey_DuplicateMapKeys_AreRejected()
    {
        // fixmap(2) { "id": 1, "id": 2 } — duplicate KNOWN member keys are data errors
        // on both the generated and the reflection map paths (decision 2026-08-20)
        byte[] duplicate = [0x82, 0xA2, (byte)'i', (byte)'d', 0x01, 0xA2, (byte)'i', (byte)'d', 0x02];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<GenStringKeyPoco>(duplicate));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<GenStringKeyPoco>(duplicate, reflectionOptions));

        // UNKNOWN keys may repeat: version tolerance keeps skipping them
        byte[] unknownDuplicate = [0x82, 0xA2, (byte)'z', (byte)'z', 0x01, 0xA2, (byte)'z', (byte)'z', 0x02];
        Assert.NotNull(V4.Deserialize<GenStringKeyPoco>(unknownDuplicate));
        Assert.NotNull(V4.Deserialize<GenStringKeyPoco>(unknownDuplicate, reflectionOptions));
    }

    [Fact]
    public void IntAndStringMembers_SpecializeToDirectSlots()
    {
        // white-box guard for the Initialize-time slot swap: built-in int/string members
        // must land on the direct primitive slots, other members stay on the generic slot.
        // If a built-in formatter rename ever breaks the exact-type test, this catches the
        // silent fall-back to interface dispatch.
        var formatter = reflectionOptions.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, GenIntKeyPoco>();
        var slotsField = formatter.GetType().GetField("slots", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var slotNames = ((Array)slotsField.GetValue(formatter)!).Cast<object>().Select(s => s.GetType().Name).ToArray();

        // GenIntKeyPoco: Id(int), Name(string), Score(double), Flag(bool), Ticks(long)
        Assert.Equal("DirectInt32Slot", slotNames[0]);
        Assert.Equal("DirectStringSlot", slotNames[1]);
        Assert.StartsWith("ClassPropertySlot", slotNames[2]);
    }

    [Fact]
    public void IntKey_HolesSerializeAsNil()
    {
        var value = new GenHolePoco { A = 100000, B = "b", C = 255 };
        AssertReflectionMatchesGenerated(value);

        var bytes = V4.Serialize(value, reflectionOptions);
        Assert.Equal(0x98, bytes[0]); // fixarray(8): max key 7
        Assert.Equal(0xc0, bytes[1]); // hole at key 0
    }

    [Fact]
    public void StringKey_UsesDeclaredKeys()
    {
        var value = new GenStringKeyPoco { Id = 7, Name = "name", F = 1.25f };
        AssertReflectionMatchesGenerated(value);

        var map = V4.Deserialize<Dictionary<string, object?>>(V4.Serialize(value, reflectionOptions), reflectionOptions)!;
        Assert.Equal(["f", "id", "name"], map.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void KeyAsPropertyName_UsesMemberNames()
    {
        var value = new GenAutoKeyPoco { X = 9, Y = "auto" };
        AssertReflectionMatchesGenerated(value);

        var map = V4.Deserialize<Dictionary<string, object?>>(V4.Serialize(value, reflectionOptions), reflectionOptions)!;
        Assert.Equal(["X", "Y"], map.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void IgnoreMember_StaysOffTheWire()
    {
        var value = new GenIntKeyPoco { Id = 1, Ignored = 12345 };
        var back = V4.Deserialize<GenIntKeyPoco>(V4.Serialize(value, reflectionOptions), reflectionOptions)!;
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
        Assert.Throws<MessagePack.MessagePackSerializationException>(
            () => V4.Deserialize<GenStructPoco>([0xc0], reflectionOptions));
    }

    [Fact]
    public void VersionTolerance_ExtraAndMissingTrailingEntries()
    {
        // newer writer, older reader: keys 5/6 are skipped
        var newer = V4.Serialize(new GenIntKeyPocoV2 { Id = 42, Name = "n", Score = 1.5, Extra = "x", ExtraList = [1, 2] }, reflectionOptions);
        var old = V4.Deserialize<GenIntKeyPoco>(newer, reflectionOptions)!;
        Assert.Equal(42, old.Id);
        Assert.Equal("n", old.Name);

        // older writer, newer reader: entries past the payload keep their defaults
        var older = V4.Serialize(new GenIntKeyPocoV0 { Id = 7, Name = "v0" }, reflectionOptions);
        var current = V4.Deserialize<GenIntKeyPoco>(older, reflectionOptions)!;
        Assert.Equal(7, current.Id);
        Assert.Equal("v0", current.Name);
        Assert.Equal(0, current.Ticks);
    }

    // ---- tier parity on inheritance (2026-08-30) --------------------------------------
    // The generated and reflection formatters must emit the SAME bytes for the same type;
    // inheritance is where they can silently drift (member order, shadowing, overrides).
    // The reflection tier's order is the oracle-proven v3 wire: base declarations first
    // (properties then fields per level), then a stable [DataMember(Order)] sort - the
    // generator mirrors it in ObjectParser. Contractless `new` shadowing stays EXCLUDED:
    // there the tiers diverge as v3's own did (dynamic qualified the colliding key,
    // mpc demanded an explicit re-key; v4's generator errors MsgPack003).

    [Fact]
    public void Inherited_StringKey_SameWireOrder_BothTiers()
    {
        AssertReflectionMatchesGenerated(new GenTierDerivedString { BaseProp = 1, BaseField = "bf", DerivedProp = 2, DerivedField = "df" });

        // explicit-key wire order is KIND-major (oracle-probed v3): every property
        // base-first, then every field base-first - NOT per-level grouping
        Assert.Equal(
            """{"bp":0,"dp":0,"bf":null,"df":null}""",
            V4.ConvertToJson(V4.Serialize(new GenTierDerivedString())));
    }

    [Fact]
    public void Inherited_IntKey_KeysSplitAcrossLevels_BothTiers()
    {
        AssertReflectionMatchesGenerated(new GenTierDerivedInt { BaseId = 10, DerivedId = 20 });
    }

    [Fact]
    public void Shadowed_IntKey_EveryDeclarationSerializes_BothTiers()
    {
        var value = new GenTierShadowIntDerived { Value = 2 };
        ((GenTierShadowIntBase)value).Value = 1;
        AssertReflectionMatchesGenerated(value);

        var back = V4.Deserialize<GenTierShadowIntDerived>(V4.Serialize(value, reflectionOptions))!;
        Assert.Equal(2, back.Value);
        Assert.Equal(1, ((GenTierShadowIntBase)back).Value);
    }

    [Fact]
    public void Shadowed_ExplicitStringKeys_SameWireOrder_BothTiers()
    {
        var value = new GenTierShadowStringDerived { Tag = "derived" };
        ((GenTierShadowStringBase)value).Tag = "base";
        AssertReflectionMatchesGenerated(value);
    }

    [Fact]
    public void Override_OneSlot_KeyInheritedFromBaseDeclaration_BothTiers()
    {
        AssertReflectionMatchesGenerated(new GenTierOverrideBase { Name = "x" });
        AssertReflectionMatchesGenerated(new GenTierOverrideDerived { Name = "y" });
        Assert.Equal(0x91, V4.Serialize(new GenTierOverrideDerived { Name = "z" })[0]); // fixarray(1)
    }

    [Fact]
    public void MapMode_ShadowedField_QualifiedKey_BothTiers()
    {
        // the non-basemost declaration's default map key is "{DeclaringType.FullName}.{Name}"
        // (raw, never policy-shaped) - v3's rule, oracle-compared
        var value = new GenTierMapShadowFieldDerived { Val = "derived" };
        ((GenTierMapShadowFieldBase)value).Val = "base";
        AssertReflectionMatchesGenerated(value);
        Assert.Equal(
            """{"Val":"base","MessagePack.Tests.GenTierMapShadowFieldDerived.Val":"derived"}""",
            V4.ConvertToJson(V4.Serialize(value)));
    }

    [Fact]
    public void MapMode_ShadowedProperty_QualifiedKey_TiersAgree()
    {
        // ratified deviation, so no oracle comparison: v3's dynamic resolver silently
        // DROPPED a property shadowed by another property (a GetRuntimeProperties
        // flattening artifact - shadowed fields it qualified and kept, and its mpc kept
        // both too); v4 treats every declaration uniformly on both tiers
        var value = new GenTierMapShadowPropDerived { Tag = "derived" };
        ((GenTierMapShadowPropBase)value).Tag = "base";
        Assert.Equal(V4.Serialize(value), V4.Serialize(value, reflectionOptions));

        var back = V4.Deserialize<GenTierMapShadowPropDerived>(V4.Serialize(value))!;
        Assert.Equal("derived", back.Tag);
        Assert.Equal("base", ((GenTierMapShadowPropBase)back).Tag);
    }

    [Fact]
    public void DataMemberOrder_ReordersTheMap_BothTiers()
    {
        AssertReflectionMatchesGenerated(new GenTierDataMemberOrderPoco { A = 1, B = 2, C = 3 });

        // Order 1 -> Order 2 -> no attribute (int.MaxValue)
        Assert.Equal(
            """{"B":0,"A":0,"C":0}""",
            V4.ConvertToJson(V4.Serialize(new GenTierDataMemberOrderPoco())));
    }

    [Fact]
    public void AttributedRecord_ConstructsThroughTheArgumentPath()
    {
        // SuppressSourceGeneration: even the Default chain lands on reflection; the
        // positional record has no parameterless constructor, proving the [Key]-ordered
        // array payload drives the constructor-argument path
        var value = new ReflectionRecordPerson<object>(31, "rec");
        var bytes = V4.Serialize(value, reflectionOptions);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(value, V4.Deserialize<ReflectionRecordPerson<object>>(bytes, reflectionOptions));
    }

    [Fact]
    public void DefaultChain_FallsBackToReflectionForGeneratorSkippedTypes()
    {
        // options-less entry points: SuppressSourceGeneration keeps the type out of the
        // registry, BuiltIn/Generic decline, the annotated reflection tail serves it
        var value = new ReflectionGenericHolder<object> { Value = 77 };
        var bytes = V4.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), bytes);
        Assert.Equal(77, V4.Deserialize<ReflectionGenericHolder<object>>(bytes)!.Value);
    }

    [Fact]
    public void AnnotatedOnly_DeclinesUnannotatedTypes()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => V4.Serialize(new PlainPoco { Value = 1 }, reflectionOptions));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void DefaultChainOrder_RegistryStillWinsOverReflection()
    {
        // a private registry stands in for "the generator covered this type": it must win
        // over the reflection tail exactly like the real Default composition
        var registry = new SourceGeneratedFormatterFactory();
        var chain = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            registry,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        // annotated resolves through the tail, unannotated still throws
        Assert.Equal(5, V4.Deserialize<GenIntKeyPoco>(V4.Serialize(new GenIntKeyPoco { Id = 5 }, chain), chain)!.Id);
        Assert.Throws<InvalidOperationException>(() => V4.Serialize(new PlainPoco(), chain));
    }

    [Fact]
    public void CatchAllMode_StillHonorsAnnotations()
    {
        // v3 ContractlessStandardResolver semantics: the contractless catch-all serializes
        // annotated types in their KEYED wire form, not as a member-name map
        var catchAll = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(),
        ]));

        var value = new GenIntKeyPoco { Id = 3, Name = "k" };
        Assert.Equal(Oracle.Serialize(value), V4.Serialize(value, catchAll)); // array format

        // and attribute-free types keep the contractless map form
        var map = V4.Deserialize<Dictionary<string, object?>>(V4.Serialize(new PlainPoco { Value = 8 }, catchAll), catchAll)!;
        Assert.Equal(8, Convert.ToInt32(map["Value"]));
    }

    // ---- invalid shapes: the source generator rejects these at compile time (MsgPack001/2/3/8),
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
        => V4.Serialize(type, value, reflectionOptions);

    static object? Deserialize(byte[] bytes, Type type)
        => V4.Deserialize(type, bytes, reflectionOptions);

    static MessagePack.MessagePackSerializationException AssertInitializeThrows(Type type)
    {
        var exception = Record.Exception(() => Serialize(Activator.CreateInstance(type), type));
        while (exception is TargetInvocationException { InnerException: { } inner })
        {
            exception = inner;
        }
        return Assert.IsType<MessagePack.MessagePackSerializationException>(exception);
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

// SuppressSourceGeneration keeps these OUT of the generated registry on purpose (the
// generator handles generic shapes now), which is exactly what makes them exercise the
// reflection tier through the real Default chain
[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public record ReflectionRecordPerson<TUnused>([property: V3::MessagePack.Key(0)] int Age, [property: V3::MessagePack.Key(1)] string? Name);

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class ReflectionGenericHolder<TUnused>
{
    [V3::MessagePack.Key(0)] public int Value { get; set; }
}

// ---- tier-parity inheritance types (generated formatter AND reflection serve these; the
// tests byte-compare the two against each other and the v3 oracle) ----------------------

[V3::MessagePack.MessagePackObject]
public class GenTierBaseString
{
    [V3::MessagePack.Key("bp")] public int BaseProp { get; set; }
    [V3::MessagePack.Key("bf")] public string? BaseField;
}

[V3::MessagePack.MessagePackObject]
public class GenTierDerivedString : GenTierBaseString
{
    [V3::MessagePack.Key("dp")] public int DerivedProp { get; set; }
    [V3::MessagePack.Key("df")] public string? DerivedField;
}

[V3::MessagePack.MessagePackObject]
public class GenTierBaseInt
{
    [V3::MessagePack.Key(0)] public int BaseId { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierDerivedInt : GenTierBaseInt
{
    [V3::MessagePack.Key(1)] public int DerivedId { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierShadowIntBase
{
    [V3::MessagePack.Key(0)] public int Value { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierShadowIntDerived : GenTierShadowIntBase
{
    [V3::MessagePack.Key(1)] public new int Value { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierShadowStringBase
{
    [V3::MessagePack.Key("t")] public string? Tag { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierShadowStringDerived : GenTierShadowStringBase
{
    [V3::MessagePack.Key("t2")] public new string? Tag { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierOverrideBase
{
    [V3::MessagePack.Key(0)] public virtual string? Name { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenTierOverrideDerived : GenTierOverrideBase
{
    public override string? Name { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class GenTierMapShadowFieldBase
{
    public string? Val;
}

[V3::MessagePack.MessagePackObject(true)]
public class GenTierMapShadowFieldDerived : GenTierMapShadowFieldBase
{
    public new string? Val;
}

[V3::MessagePack.MessagePackObject(true)]
public class GenTierMapShadowPropBase
{
    public string? Tag { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class GenTierMapShadowPropDerived : GenTierMapShadowPropBase
{
    public new string? Tag { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class GenTierDataMemberOrderPoco
{
    [System.Runtime.Serialization.DataMember(Order = 2)] public int A { get; set; }
    [System.Runtime.Serialization.DataMember(Order = 1)] public int B { get; set; }
    public int C { get; set; }
}

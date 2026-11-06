using System.Diagnostics.CodeAnalysis;
using MessagePack;
using MessagePack.Formatters;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Deserialization value contracts, resolver-bound (the flags are part of the formatter
// graph's identity, not per-call options):
//   required  - a member with the C# required modifier, or a no-default constructor
//               parameter, must be PRESENT in the payload. On by default
//               (validateRequiredMembers: false relaxes it).
//   nullable  - a member declared non-nullable must not receive an explicit nil (deserialization:
//               the setter, field or bound constructor parameter's contract) nor hold null
//               (serialization: the getter's contract). [AllowNull]/[DisallowNull] and
//               [MaybeNull]/[NotNull] adjust each side as they do for the compiler. On by default
//               (validateNullableAnnotations: false relaxes it).
// Unknown keys and extra array slots keep being skipped either way: structural evolution
// stays tolerated, the strictness applies to the values of KNOWN members only.
// Both the source-generated and the reflection tier must agree on every case.
public class RequiredAndNullableValidationTests
{
    static MessagePackSerializerOptions Options(bool validateRequired = true, bool validateNull = true) => new(
        new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { ValidateRequiredMembers = validateRequired, ValidateNullableAnnotations = validateNull });

    // SourceGenerated tier deliberately omitted: the reflection tier claims these types
    static MessagePackSerializerOptions ReflectionOptions(bool validateRequired = true, bool validateNull = true) => new(
        new MessagePackFormatterResolver(
            [
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory(annotatedOnly: true),
            ]) { ValidateRequiredMembers = validateRequired, ValidateNullableAnnotations = validateNull });

    // ---- required: presence ----

    [Fact]
    public void RequiredModifier_IntKey_PresentRoundtrips()
    {
        var bytes = V4.Serialize(new ReqModifierArrayPoco { Id = 1, Name = "n" });
        Assert.Equal("n", V4.Deserialize<ReqModifierArrayPoco>(bytes)!.Name);
        Assert.Equal("n", V4.Deserialize<ReqModifierArrayPoco>(bytes, ReflectionOptions())!.Name);
    }

    [Fact]
    public void RequiredModifier_IntKey_ShorterArrayThrows()
    {
        // an older writer's fixarray(1) [Id]: key 1 (required Name) is positionally absent
        var old = V4.Serialize(new ReqArrayPocoV0 { Id = 5 });

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqModifierArrayPoco>(old));
        Assert.Contains("'Name'", ex.Message);
        Assert.Contains("validateRequiredMembers", ex.Message);

        var reflectionEx = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqModifierArrayPoco>(old, ReflectionOptions()));
        Assert.Contains("'Name'", reflectionEx.Message);
    }

    [Fact]
    public void RequiredModifier_IntKey_OptOutAcceptsTheShorterArray()
    {
        var old = V4.Serialize(new ReqArrayPocoV0 { Id = 5 });

        var value = V4.Deserialize<ReqModifierArrayPoco>(old, Options(validateRequired: false))!;
        Assert.Equal(5, value.Id);
        Assert.Null(value.Name); // the member keeps its default; that is the opt-out's contract

        var reflection = V4.Deserialize<ReqModifierArrayPoco>(old, ReflectionOptions(validateRequired: false))!;
        Assert.Equal(5, reflection.Id);
        Assert.Null(reflection.Name);
    }

    [Fact]
    public void RequiredModifier_IntKey_ExplicitNilSlotCountsAsPresent()
    {
        // fixarray(2) [1, nil]: the slot exists, so the REQUIRED check passes (nil values
        // are the nullable check's business, relaxed here to isolate the presence rule)
        byte[] payload = [0x92, 0x01, 0xc0];
        Assert.Null(V4.Deserialize<ReqModifierArrayPoco>(payload, Options(validateNull: false))!.Name);
        Assert.Null(V4.Deserialize<ReqModifierArrayPoco>(payload, ReflectionOptions(validateNull: false))!.Name);

        // with the defaults the nil is refused by the nullable check, not reported as missing
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqModifierArrayPoco>(payload));
        Assert.Contains("validateNullableAnnotations", ex.Message);
    }

    [Fact]
    public void RequiredModifier_IntKey_PopulatingAnExistingInstanceStillEnforcesPresence()
    {
        var old = V4.Serialize(new ReqArrayPocoV0 { Id = 5 });
        var target = new ReqModifierArrayPoco { Id = 9, Name = "have" };
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize(old, ref target));
    }

    [Fact]
    public void RequiredModifier_StringKey_MissingKeyThrows()
    {
        var old = V4.Serialize(new ReqMapPocoV0 { Id = 7 });

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqMapPoco>(old));
        Assert.Contains("'Name'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqMapPoco>(old, ReflectionOptions()));

        Assert.Equal(7, V4.Deserialize<ReqMapPoco>(old, Options(validateRequired: false))!.Id);
        Assert.Equal(7, V4.Deserialize<ReqMapPoco>(old, ReflectionOptions(validateRequired: false))!.Id);

        var roundtrip = V4.Serialize(new ReqMapPoco { Id = 7, Name = "n" });
        Assert.Equal("n", V4.Deserialize<ReqMapPoco>(roundtrip)!.Name);
        Assert.Equal("n", V4.Deserialize<ReqMapPoco>(roundtrip, ReflectionOptions())!.Name);
    }

    [Fact]
    public void ConstructorParameterWithoutDefault_IsRequired()
    {
        // fixarray(0): the constructor-fed A is positionally absent
        byte[] empty = [0x90];

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqCtorPoco>(empty));
        Assert.Contains("'A'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ReqCtorPoco>(empty, ReflectionOptions()));

        var lenient = V4.Deserialize<ReqCtorPoco>(empty, Options(validateRequired: false))!;
        Assert.Equal(0, lenient.A);

        var value = V4.Deserialize<ReqCtorPoco>(V4.Serialize(new ReqCtorPoco(3) { B = 1.5 }))!;
        Assert.Equal(3, value.A);
        Assert.Equal(1.5, value.B);
    }

    [Fact]
    public void ConstructorParameterWithDefault_IsNotRequired()
    {
        byte[] empty = [0x90];
        // both tiers pass default(int) for the absent argument (not the declared default:
        // the argument state starts from default, the declared 42 never enters play)
        Assert.Equal(0, V4.Deserialize<OptCtorPoco>(empty)!.A);
        Assert.Equal(0, V4.Deserialize<OptCtorPoco>(empty, ReflectionOptions())!.A);
    }

    // ---- required: contractless (map of member names, reflection only) ----

    [Fact]
    public void RequiredModifier_Contractless_MissingKeyThrows()
    {
        var contractless = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: false)]));
        var lenient = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: false)]) { ValidateRequiredMembers = false });

        var old = V4.Serialize(new ContractlessReqPocoV0 { Id = 3 }, contractless);
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ContractlessReqPoco>(old, contractless));
        Assert.Contains("'Name'", ex.Message);
        Assert.Equal(3, V4.Deserialize<ContractlessReqPoco>(old, lenient)!.Id);

        var bytes = V4.Serialize(new ContractlessReqPoco { Id = 3, Name = "n" }, contractless);
        Assert.Equal("n", V4.Deserialize<ContractlessReqPoco>(bytes, contractless)!.Name);
    }

    // ---- nullable: explicit nil into a non-nullable member (deserialization) ----

    [Fact]
    public void NullableAnnotations_OptOut_NilIsAccepted()
    {
        // [nil, "nick"] written through the all-nullable twin
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = null, Nick = "nick" });
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, Options(validateNull: false))!.Name);
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions(validateNull: false))!.Name);
    }

    [Fact]
    public void NullableAnnotations_OnByDefault_NilIntoNonNullableThrows()
    {
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = null, Nick = "nick" });

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullArrayPoco>(bytes));
        Assert.Contains("'Name'", ex.Message);
        Assert.Contains("validateNullableAnnotations", ex.Message);
        Assert.Contains("the payload contains nil", ex.Message);

        // reflection tier: string members specialize to the direct slot, which must
        // carry the same check
        var reflectionEx = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions()));
        Assert.Contains("'Name'", reflectionEx.Message);
    }

    [Fact]
    public void NullableAnnotations_NilIntoNullableIsFine()
    {
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = "n", Nick = null });
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes)!.Nick);
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions())!.Nick);
    }

    [Fact]
    public void NullableAnnotations_GuardsNonStringReferenceMembersToo()
    {
        // [nil]: int[] goes through the generic formatter path, not the direct slots
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NullDataPoco>(payload, Options(validateNull: false))!.Data);

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullDataPoco>(payload));
        Assert.Contains("'Data'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullDataPoco>(payload, ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_ConstructionPathIsGuarded()
    {
        // [nil] feeding the non-nullable constructor parameter
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NullCtorPoco>(payload, Options(validateNull: false))!.Name);

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullCtorPoco>(payload));
        Assert.Contains("'Name'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullCtorPoco>(payload, ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_ConstructorParameterAnnotationIsTheDeserializationContract()
    {
        // the payload value goes to the parameter, so its annotation decides, not the member's
        byte[] payload = [0x91, 0xc0];
        Assert.Equal("", V4.Deserialize<CtorAllowsNullPoco>(payload)!.Name);
        Assert.Equal("", V4.Deserialize<CtorAllowsNullPoco>(payload, ReflectionOptions())!.Name);

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<CtorRejectsNullPoco>(payload));
        Assert.Contains("'Name'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<CtorRejectsNullPoco>(payload, ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_RequiredMemberBoundToAConstructor_MustMeetBothContracts()
    {
        // without [SetsRequiredMembers] the generated code is `new RequiredCtorPoco(v) { Name = v }`
        // (CS9035), so a nil the parameter accepts would still reach the non-nullable setter: both
        // tiers refuse it, whichever of the two contracts rejects null
        byte[] nil = [0x91, 0xc0];
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<RequiredCtorPoco>(nil));
        Assert.Contains("'Name'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<RequiredCtorPoco>(nil, ReflectionOptions()));

        // relaxed: both tiers reassign the member after the constructor, so both hold the payload's nil
        Assert.Null(V4.Deserialize<RequiredCtorPoco>(nil, Options(validateNull: false))!.Name);
        Assert.Null(V4.Deserialize<RequiredCtorPoco>(nil, ReflectionOptions(validateNull: false))!.Name);

        // values still bind
        byte[] named = [0x91, 0xa1, (byte)'n'];
        Assert.Equal("n", V4.Deserialize<RequiredCtorPoco>(named)!.Name);
        Assert.Equal("n", V4.Deserialize<RequiredCtorPoco>(named, ReflectionOptions())!.Name);

        // with [SetsRequiredMembers] there is no initializer assignment: the parameter alone decides,
        // and the constructor's normalization survives in both tiers
        Assert.Equal("", V4.Deserialize<RequiredSetsMembersCtorPoco>(nil)!.Name);
        Assert.Equal("", V4.Deserialize<RequiredSetsMembersCtorPoco>(nil, ReflectionOptions())!.Name);
    }

    [Fact]
    public void RequiredMemberBoundToAConstructor_PayloadValueWinsInBothTiers()
    {
        // `new RequiredTrimCtorPoco(v) { Name = v }`: the generated code must assign the required member through the
        // object initializer (CS9035), as any C# caller must, so the constructor's Trim is overwritten; the
        // reflection tier reassigns the same way (MsgPack032 reports the shape at compile time)
        var padded = V4.Serialize(new object[] { "  x  " });
        Assert.Equal("  x  ", V4.Deserialize<RequiredTrimCtorPoco>(padded)!.Name);
        Assert.Equal("  x  ", V4.Deserialize<RequiredTrimCtorPoco>(padded, ReflectionOptions())!.Name);

        // [SetsRequiredMembers]: no initializer assignment, the constructor's result stays in both tiers
        Assert.Equal("x", V4.Deserialize<RequiredTrimSetsMembersCtorPoco>(padded)!.Name);
        Assert.Equal("x", V4.Deserialize<RequiredTrimSetsMembersCtorPoco>(padded, ReflectionOptions())!.Name);

        // an absent argument (required validation relaxed) reassigns the member's default after the constructor,
        // a value type through the direct int slot included
        byte[] empty = [0x90];
        Assert.Equal(0, V4.Deserialize<RequiredCountCtorPoco>(empty, Options(validateRequired: false))!.Count);
        Assert.Equal(0, V4.Deserialize<RequiredCountCtorPoco>(empty, ReflectionOptions(validateRequired: false))!.Count);
        byte[] seven = [0x91, 0x07];
        Assert.Equal(7, V4.Deserialize<RequiredCountCtorPoco>(seven)!.Count);
        Assert.Equal(7, V4.Deserialize<RequiredCountCtorPoco>(seven, ReflectionOptions())!.Count);
    }

    [Fact]
    public void NullableAnnotations_NullableValueIntoABoxingConstructorParameter()
    {
        // int? read directly, handed to `object value`: the parameter rejects null
        byte[] nil = [0x91, 0xc0];
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<BoxedCtorPoco>(nil));
        Assert.Contains("'Value'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<BoxedCtorPoco>(nil, ReflectionOptions()));

        Assert.Null(V4.Deserialize<BoxedCtorPoco>(nil, Options(validateNull: false))!.Value);
        Assert.Null(V4.Deserialize<BoxedCtorPoco>(nil, ReflectionOptions(validateNull: false))!.Value);

        byte[] five = [0x91, 0x05];
        Assert.Equal(5, V4.Deserialize<BoxedCtorPoco>(five)!.Value);
        Assert.Equal(5, V4.Deserialize<BoxedCtorPoco>(five, ReflectionOptions())!.Value);

        // a non-nullable struct member bound the same way can never be null: no check is emitted
        // (`v is null` would not compile on Guid), and the value round-trips
        var id = Guid.NewGuid();
        var bytes = V4.Serialize(new BoxedGuidCtorPoco(id));
        Assert.Equal(id, V4.Deserialize<BoxedGuidCtorPoco>(bytes)!.Id);
        Assert.Equal(id, V4.Deserialize<BoxedGuidCtorPoco>(bytes, ReflectionOptions())!.Id);
    }

    [Fact]
    public void NullableAnnotations_ObliviousMembersAreNotChecked()
    {
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<ObliviousPoco>(payload)!.Name);
        Assert.Null(V4.Deserialize<ObliviousPoco>(payload, ReflectionOptions())!.Name);

        var nil = V4.Serialize(new ObliviousPoco { Name = null });
        Assert.Equal(payload, nil);
        Assert.Equal(payload, V4.Serialize(new ObliviousPoco { Name = null }, ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_MembersTypedByATypeParameterAreNotChecked()
    {
        // `T Value` of a generic declaration: the use site's annotation (string? here) is not
        // in metadata for the reflection tier, so both tiers leave the member alone
        byte[] nil = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NullGenericDerived>(nil)!.Value);
        Assert.Null(V4.Deserialize<NullGenericDerived>(nil, ReflectionOptions())!.Value);
        Assert.Equal(nil, V4.Serialize(new NullGenericDerived()));
        Assert.Equal(nil, V4.Serialize(new NullGenericDerived(), ReflectionOptions()));

        // a closed generic root: T stays unchecked, its other members keep their contract
        byte[] valueNil = [0x92, 0xc0, 0xa0]; // [nil, ""]
        Assert.Null(V4.Deserialize<NullGenericHolder<string>>(valueNil)!.Value);
        Assert.Null(V4.Deserialize<NullGenericHolder<string>>(valueNil, ReflectionOptions())!.Value);
        byte[] nameNil = [0x92, 0xa0, 0xc0]; // ["", nil]
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullGenericHolder<string>>(nameNil));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullGenericHolder<string>>(nameNil, ReflectionOptions()));
    }

    // ---- nullable: a non-nullable member holding null (serialization) ----

    [Fact]
    public void NullableAnnotations_SerializingNullFromNonNullableThrows()
    {
        var value = new NullArrayPoco { Name = null!, Nick = null };

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(value));
        Assert.Contains("'Name'", ex.Message);
        Assert.Contains("its value is null", ex.Message);
        Assert.Contains("validateNullableAnnotations", ex.Message);
        var reflectionEx = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(value, ReflectionOptions()));
        Assert.Contains("'Name'", reflectionEx.Message);

        // generic formatter path, field slot, struct slot
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NullDataPoco { Data = null! }));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NullDataPoco { Data = null! }, ReflectionOptions()));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NullFieldPoco { Name = null! }));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NullFieldPoco { Name = null! }, ReflectionOptions()));
        // a default struct's reference members are null although declared non-nullable
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(default(NullStructPoco)));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(default(NullStructPoco), ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_OptOut_SerializesNullAsNil()
    {
        byte[] expected = [0x92, 0xc0, 0xc0];
        var value = new NullArrayPoco { Name = null!, Nick = null };
        Assert.Equal(expected, V4.Serialize(value, Options(validateNull: false)));
        Assert.Equal(expected, V4.Serialize(value, ReflectionOptions(validateNull: false)));

        // so what a lenient writer produced, a lenient reader takes back
        Assert.Null(V4.Deserialize<NullArrayPoco>(expected, Options(validateNull: false))!.Name);
    }

    [Fact]
    public void NullableAnnotations_SerializeSeesTheStateAfterOnBeforeSerialize()
    {
        var value = new CallbackNormalizingPoco { Name = null! };
        Assert.Equal(new byte[] { 0x91, 0xa0 }, V4.Serialize(value)); // [""]
        value.Name = null!;
        Assert.Equal(new byte[] { 0x91, 0xa0 }, V4.Serialize(value, ReflectionOptions()));
    }

    // ---- nullable: flow attributes ----

    [Fact]
    public void NullableAnnotations_AllowNull_AcceptsNilOnTheInputSide()
    {
        byte[] payload = [0x91, 0xc0];
        Assert.Equal("", V4.Deserialize<AllowNullPoco>(payload)!.Name);
        Assert.Equal("", V4.Deserialize<AllowNullPoco>(payload, ReflectionOptions())!.Name);
        Assert.Equal("", V4.Deserialize<AllowNullOnSetterPoco>(payload)!.Name);
        Assert.Equal("", V4.Deserialize<AllowNullOnSetterPoco>(payload, ReflectionOptions())!.Name);
    }

    [Fact]
    public void NullableAnnotations_AllowNull_IsTheRouteForOnAfterDeserializeNormalization()
    {
        // the check runs as each member is read, before OnAfterDeserialize: a plain non-nullable
        // member the callback would repair is refused first
        byte[] payload = [0x91, 0xc0];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<CallbackNormalizingPoco>(payload));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<CallbackNormalizingPoco>(payload, ReflectionOptions()));

        // declaring the input side [AllowNull] lets the callback do its work
        Assert.Equal("", V4.Deserialize<CallbackAllowNullPoco>(payload)!.Name);
        Assert.Equal("", V4.Deserialize<CallbackAllowNullPoco>(payload, ReflectionOptions())!.Name);
    }

    [Fact]
    public void NullableAnnotations_DisallowNull_RejectsNilIntoANullableMember()
    {
        byte[] payload = [0x91, 0xc0];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<DisallowNullPoco>(payload));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<DisallowNullPoco>(payload, ReflectionOptions()));

        // the getter stays nullable: null still serializes
        Assert.Equal(payload, V4.Serialize(new DisallowNullPoco()));
        Assert.Equal(payload, V4.Serialize(new DisallowNullPoco(), ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_MaybeNull_RelaxesOnlyTheOutputSide()
    {
        byte[] payload = [0x91, 0xc0];
        Assert.Equal(payload, V4.Serialize(new MaybeNullPoco { Name = null! }));
        Assert.Equal(payload, V4.Serialize(new MaybeNullPoco { Name = null! }, ReflectionOptions()));

        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<MaybeNullPoco>(payload));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<MaybeNullPoco>(payload, ReflectionOptions()));
    }

    [Fact]
    public void NullableAnnotations_NotNull_TightensOnlyTheOutputSide()
    {
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NotNullPoco>(payload)!.Name);
        Assert.Null(V4.Deserialize<NotNullPoco>(payload, ReflectionOptions())!.Name);

        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NotNullPoco { Name = null }));
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new NotNullPoco { Name = null }, ReflectionOptions()));
    }
}

[MessagePackObject]
public class ReqModifierArrayPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public required string Name { get; set; }
}

[MessagePackObject]
public class ReqArrayPocoV0
{
    [Key(0)] public int Id { get; set; }
}

[MessagePackObject(true)]
public class ReqMapPoco
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

[MessagePackObject(true)]
public class ReqMapPocoV0
{
    public int Id { get; set; }
}

[MessagePackObject]
public class ReqCtorPoco
{
    [Key(0)] public int A { get; }
    [Key(1)] public double B { get; set; }

    public ReqCtorPoco(int a)
    {
        A = a;
    }
}

[MessagePackObject]
public class OptCtorPoco
{
    [Key(0)] public int A { get; }

    public OptCtorPoco(int a = 42)
    {
        A = a;
    }
}

public class ContractlessReqPoco
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class ContractlessReqPocoV0
{
    public int Id { get; set; }
}

[MessagePackObject]
public class NullArrayPoco
{
    [Key(0)] public string Name { get; set; } = "";
    [Key(1)] public string? Nick { get; set; }
}

// wire-compatible twin whose members are all nullable, for writing nil payloads
[MessagePackObject]
public class NullArrayPocoWriter
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public string? Nick { get; set; }
}

[MessagePackObject]
public class NullDataPoco
{
    [Key(0)] public int[] Data { get; set; } = [];
}

[MessagePackObject]
public class NullCtorPoco
{
    [Key(0)] public string Name { get; }

    public NullCtorPoco(string name)
    {
        Name = name;
    }
}

[MessagePackObject]
public class NullFieldPoco
{
    [Key(0)] public string Name = "";
}

[MessagePackObject]
public struct NullStructPoco
{
    [Key(0)] public string Name { get; set; }
}

[MessagePackObject]
public class CtorAllowsNullPoco
{
    [Key(0)] public string Name { get; }

    public CtorAllowsNullPoco(string? name)
    {
        Name = name ?? "";
    }
}

[MessagePackObject]
public class CtorRejectsNullPoco
{
    [Key(0)] public string? Name { get; }

    public CtorRejectsNullPoco(string name)
    {
        Name = name;
    }
}

#nullable disable
[MessagePackObject]
public class ObliviousPoco
{
    [Key(0)] public string Name { get; set; }
}
#nullable restore

[MessagePackObject]
public class AllowNullPoco
{
    string name = "";

    [Key(0)]
    [AllowNull]
    public string Name { get => name; set => name = value ?? ""; }
}

[MessagePackObject]
public class AllowNullOnSetterPoco
{
    string name = "";

    [Key(0)]
    public string Name { get => name; [param: AllowNull] set => name = value ?? ""; }
}

[MessagePackObject]
public class CallbackNormalizingPoco : IMessagePackSerializationCallbackReceiver
{
    [Key(0)] public string Name { get; set; } = "";

    public void OnBeforeSerialize() => Name ??= "";

    public void OnAfterDeserialize() => Name ??= "";
}

[MessagePackObject]
public class CallbackAllowNullPoco : IMessagePackSerializationCallbackReceiver
{
    [Key(0)] [AllowNull] public string Name { get; set; } = "";

    public void OnBeforeSerialize() => Name ??= "";

    public void OnAfterDeserialize() => Name ??= "";
}

[MessagePackObject]
public class DisallowNullPoco
{
    [Key(0)] [DisallowNull] public string? Name { get; set; }
}

[MessagePackObject]
public class MaybeNullPoco
{
    [Key(0)] [MaybeNull] public string Name { get; set; } = "";
}

[MessagePackObject]
public class NotNullPoco
{
    [Key(0)] [NotNull] public string? Name { get; set; } = "";
}

[MessagePackObject]
public class NullGenericBase<T>
{
    [Key(0)] public T Value { get; set; } = default!;
}

[MessagePackObject]
public class NullGenericDerived : NullGenericBase<string?>
{
}

[MessagePackObject]
public class NullGenericHolder<T>
{
    [Key(0)] public T Value { get; set; } = default!;
    [Key(1)] public string Name { get; set; } = "";
}

[MessagePackObject]
public class RequiredCtorPoco
{
    [Key(0)]
    public required string Name { get; set; }

    public RequiredCtorPoco(string? name)
    {
        Name = name ?? "";
    }
}

[MessagePackObject]
public class RequiredSetsMembersCtorPoco
{
    [Key(0)]
    public required string Name { get; set; }

    [SetsRequiredMembers]
    public RequiredSetsMembersCtorPoco(string? name)
    {
        Name = name ?? "";
    }
}

[MessagePackObject]
public class BoxedCtorPoco
{
    [Key(0)]
    public int? Value { get; }

    public BoxedCtorPoco(object value)
    {
        Value = (int?)value;
    }
}

[MessagePackObject]
public class BoxedGuidCtorPoco
{
    [Key(0)]
    public Guid Id { get; }

    public BoxedGuidCtorPoco(object id)
    {
        Id = (Guid)id;
    }
}

[MessagePackObject]
public class RequiredTrimCtorPoco
{
    [Key(0)]
    public required string Name { get; set; }

    public RequiredTrimCtorPoco(string name)
    {
        Name = name.Trim();
    }
}

[MessagePackObject]
public class RequiredTrimSetsMembersCtorPoco
{
    [Key(0)]
    public required string Name { get; set; }

    [SetsRequiredMembers]
    public RequiredTrimSetsMembersCtorPoco(string name)
    {
        Name = name.Trim();
    }
}

[MessagePackObject]
public class RequiredCountCtorPoco
{
    [Key(0)]
    public required int Count { get; set; }

    public RequiredCountCtorPoco(int count)
    {
        Count = count == 0 ? -1 : count; // the constructor's own reading of "absent", overwritten by the reassignment
    }
}

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
//   nullable  - a non-nullable reference-typed member must not receive an explicit nil.
//               Off by default (validateNullableAnnotations: true enables it).
// Unknown keys and extra array slots keep being skipped either way: structural evolution
// stays tolerated, the strictness applies to the values of KNOWN members only.
// Both the source-generated and the reflection tier must agree on every case.
public class RequiredAndNullableValidationTests
{
    static MessagePackSerializerOptions Options(bool validateRequired = true, bool validateNull = false) => new(
        new MessagePackFormatterResolver(MessagePackFormatterFactory.Default, validateRequiredMembers: validateRequired, validateNullableAnnotations: validateNull));

    // SourceGenerated tier deliberately omitted: the reflection tier claims these types
    static MessagePackSerializerOptions ReflectionOptions(bool validateRequired = true, bool validateNull = false) => new(
        new MessagePackFormatterResolver(
            [
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory(annotatedOnly: true),
            ],
            validateRequiredMembers: validateRequired,
            validateNullableAnnotations: validateNull));

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
        // are the nullable check's business, off by default)
        byte[] payload = [0x92, 0x01, 0xc0];
        Assert.Null(V4.Deserialize<ReqModifierArrayPoco>(payload)!.Name);
        Assert.Null(V4.Deserialize<ReqModifierArrayPoco>(payload, ReflectionOptions())!.Name);
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
            [BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory()]));
        var lenient = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory()],
            validateRequiredMembers: false));

        var old = V4.Serialize(new ContractlessReqPocoV0 { Id = 3 }, contractless);
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ContractlessReqPoco>(old, contractless));
        Assert.Contains("'Name'", ex.Message);
        Assert.Equal(3, V4.Deserialize<ContractlessReqPoco>(old, lenient)!.Id);

        var bytes = V4.Serialize(new ContractlessReqPoco { Id = 3, Name = "n" }, contractless);
        Assert.Equal("n", V4.Deserialize<ContractlessReqPoco>(bytes, contractless)!.Name);
    }

    // ---- nullable: explicit nil into a non-nullable member ----

    [Fact]
    public void NullableAnnotations_OffByDefault_NilIsAccepted()
    {
        // [nil, "nick"] written through the all-nullable twin
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = null, Nick = "nick" });
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes)!.Name);
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions())!.Name);
    }

    [Fact]
    public void NullableAnnotations_OptIn_NilIntoNonNullableThrows()
    {
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = null, Nick = "nick" });

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullArrayPoco>(bytes, Options(validateNull: true)));
        Assert.Contains("'Name'", ex.Message);
        Assert.Contains("validateNullableAnnotations", ex.Message);

        // reflection tier: string members specialize to the direct slot, which must
        // carry the same check
        var reflectionEx = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions(validateNull: true)));
        Assert.Contains("'Name'", reflectionEx.Message);
    }

    [Fact]
    public void NullableAnnotations_OptIn_NilIntoNullableIsFine()
    {
        var bytes = V4.Serialize(new NullArrayPocoWriter { Name = "n", Nick = null });
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, Options(validateNull: true))!.Nick);
        Assert.Null(V4.Deserialize<NullArrayPoco>(bytes, ReflectionOptions(validateNull: true))!.Nick);
    }

    [Fact]
    public void NullableAnnotations_OptIn_GuardsNonStringReferenceMembersToo()
    {
        // [nil]: int[] goes through the generic formatter path, not the direct slots
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NullDataPoco>(payload)!.Data);

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullDataPoco>(payload, Options(validateNull: true)));
        Assert.Contains("'Data'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullDataPoco>(payload, ReflectionOptions(validateNull: true)));
    }

    [Fact]
    public void NullableAnnotations_OptIn_ConstructionPathIsGuarded()
    {
        // [nil] feeding the non-nullable constructor parameter
        byte[] payload = [0x91, 0xc0];
        Assert.Null(V4.Deserialize<NullCtorPoco>(payload)!.Name);

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullCtorPoco>(payload, Options(validateNull: true)));
        Assert.Contains("'Name'", ex.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<NullCtorPoco>(payload, ReflectionOptions(validateNull: true)));
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

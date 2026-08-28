extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Constructor-driven deserialization in the source generator: getter-only and init-only
// members reach the instance through a matched constructor / object initializer instead
// of setters. Byte-compat anchor stays the v3 oracle serializing the same attributed
// types; constructor SELECTION parity is with ReflectionObjectFormatter's rule
// ([SerializationConstructor] wins, else most parameters with full name+type match).
public class GeneratedConstructorTests
{
    static void AssertBytesAndRoundtrip<T>(T value)
    {
        var ours = V4.Serialize(value);
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, ours);

        var back = V4.Deserialize<T>(ours);
        Assert.Equal(oracle, Oracle.Serialize(back));
        var fromOracle = V4.Deserialize<T>(oracle);
        Assert.Equal(oracle, Oracle.Serialize(fromOracle));
    }

    [Fact]
    public void GetterOnlyMembers_ConstructThroughTheMatchedConstructor()
    {
        AssertBytesAndRoundtrip(new GenCtorPoco(42, "浦沢") { Score = 1.5 });
        AssertBytesAndRoundtrip(new GenCtorPoco(-1, null) { Score = double.NaN });

        var back = V4.Deserialize<GenCtorPoco>(V4.Serialize(new GenCtorPoco(7, "n") { Score = 2.5 }))!;
        Assert.Equal(7, back.Id);
        Assert.Equal("n", back.Name);
        Assert.Equal(2.5, back.Score);
    }

    [Fact]
    public void PositionalRecord_RoundtripsWithoutParameterlessConstructor()
    {
        AssertBytesAndRoundtrip(new GenPointRecord(3, -4));
        Assert.Equal(new GenPointRecord(3, -4), V4.Deserialize<GenPointRecord>(V4.Serialize(new GenPointRecord(3, -4))));
    }

    [Fact]
    public void InitOnlyMembers_AssignThroughTheObjectInitializer()
    {
        AssertBytesAndRoundtrip(new GenInitPoco { A = 5, B = "init" });

        var back = V4.Deserialize<GenInitPoco>(V4.Serialize(new GenInitPoco { A = 5, B = "init" }))!;
        Assert.Equal(5, back.A);
        Assert.Equal("init", back.B);
    }

    [Fact]
    public void SerializationConstructor_WinsOverTheWiderConstructor()
    {
        // the two-parameter constructor mangles B; only the attributed one-parameter
        // constructor plus the setter reproduces the value
        var value = new GenAttributedCtorPoco(10) { B = 20 };
        AssertBytesAndRoundtrip(value);

        var back = V4.Deserialize<GenAttributedCtorPoco>(V4.Serialize(value))!;
        Assert.Equal(10, back.A);
        Assert.Equal(20, back.B);
    }

    [Fact]
    public void StructConstructor_Roundtrips()
    {
        AssertBytesAndRoundtrip(new GenCtorStruct(1, 2));
        var back = V4.Deserialize<GenCtorStruct>(V4.Serialize(new GenCtorStruct(9, -9)));
        Assert.Equal(9, back.X);
        Assert.Equal(-9, back.Y);
    }

    [Fact]
    public void StringKeyConstructor_MatchesByParameterName()
    {
        AssertBytesAndRoundtrip(new GenStringKeyCtorPoco(11, "map"));
        var back = V4.Deserialize<GenStringKeyCtorPoco>(V4.Serialize(new GenStringKeyCtorPoco(11, "map")))!;
        Assert.Equal(11, back.Id);
        Assert.Equal("map", back.Name);
    }

    [Fact]
    public void VersionTolerance_OlderPayloadLeavesMissingConstructorArgumentsDefault()
    {
        // GenIntKeyPocoV0 writes fixarray(2) [Id, Name]: keys 0/1 of GenCtorPoco with
        // key 2 (Score) absent — the constructor still runs, Score stays default
        var bytes = Oracle.Serialize(new GenIntKeyPocoV0 { Id = 5, Name = "old" });
        var value = V4.Deserialize<GenCtorPoco>(bytes)!;
        Assert.Equal(5, value.Id);
        Assert.Equal("old", value.Name);
        Assert.Equal(0.0, value.Score);
    }

    [Fact]
    public void Populate_ExistingInstanceKeepsGetterOnlyMembersAndAssignsSettable()
    {
        // ReflectionObjectFormatter's Arguments-mode rule, mirrored by the generated
        // code: a caller-supplied class instance takes the populate path, where
        // getter-only payloads are skipped and settable members are assigned
        var bytes = V4.Serialize(new GenCtorPoco(1, "wire") { Score = 3.5 });
        var target = new GenCtorPoco(999, "keep");
        var result = target;
        V4.Deserialize(bytes, ref result);
        Assert.Same(target, result);
        Assert.Equal(999, result.Id);       // getter-only: payload skipped
        Assert.Equal("keep", result.Name);  // getter-only: payload skipped
        Assert.Equal(3.5, result.Score);    // settable: assigned
    }
}

[V3::MessagePack.MessagePackObject]
public class GenCtorPoco
{
    [V3::MessagePack.Key(0)] public int Id { get; }
    [V3::MessagePack.Key(1)] public string? Name { get; }
    [V3::MessagePack.Key(2)] public double Score { get; set; }

    public GenCtorPoco(int id, string? name)
    {
        Id = id;
        Name = name;
    }
}

[V3::MessagePack.MessagePackObject]
public record GenPointRecord([property: V3::MessagePack.Key(0)] int X, [property: V3::MessagePack.Key(1)] int Y);

[V3::MessagePack.MessagePackObject]
public class GenInitPoco
{
    [V3::MessagePack.Key(0)] public int A { get; init; }
    [V3::MessagePack.Key(1)] public string? B { get; init; }
}

[V3::MessagePack.MessagePackObject]
public class GenAttributedCtorPoco
{
    [V3::MessagePack.Key(0)] public int A { get; }
    [V3::MessagePack.Key(1)] public int B { get; set; }

    // most parameters, but NOT the serialization contract: mangles B so a wrong pick
    // shows up as a value mismatch
    public GenAttributedCtorPoco(int a, int b)
    {
        A = a;
        B = b + 1000;
    }

    [V3::MessagePack.SerializationConstructor]
    public GenAttributedCtorPoco(int a)
    {
        A = a;
    }
}

[V3::MessagePack.MessagePackObject]
public struct GenCtorStruct
{
    [V3::MessagePack.Key(0)] public int X { get; }
    [V3::MessagePack.Key(1)] public int Y { get; }

    public GenCtorStruct(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[V3::MessagePack.MessagePackObject]
public class GenStringKeyCtorPoco
{
    [V3::MessagePack.Key("id")] public int Id { get; }
    [V3::MessagePack.Key("name")] public string? Name { get; }

    public GenStringKeyCtorPoco(int id, string? name)
    {
        Id = id;
        Name = name;
    }
}

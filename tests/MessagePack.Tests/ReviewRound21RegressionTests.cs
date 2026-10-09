using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

[MessagePackObject]
public struct Pair
{
    [Key(0)] public int A;

    [Key(1)] public int B;
}

// [SerializationConstructor] on the dynamic overload: the generated `new` must pick it at compile time (a
// `(dynamic)` argument would re-resolve the overload at run time and take the string one)
[MessagePackObject]
public class DynamicCtor
{
    [SerializationConstructor]
    public DynamicCtor(dynamic value)
    {
        Value = (string)value;
        Chosen = "dynamic";
    }

    public DynamicCtor(string value)
    {
        Value = value;
        Chosen = "string";
    }

    [Key(0)] public string Value { get; }

    [IgnoreMember] public string Chosen { get; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class DynamicCtorReflection
{
    [SerializationConstructor]
    public DynamicCtorReflection(dynamic value)
    {
        Value = (string)value;
        Chosen = "dynamic";
    }

    public DynamicCtorReflection(string value)
    {
        Value = value;
        Chosen = "string";
    }

    [Key(0)] public string Value { get; }

    [IgnoreMember] public string Chosen { get; }
}

// a required member the formatter never reads, with a constant initializer: the generated object initializer
// (which the compiler demands) re-emits the constant instead of default
[MessagePackObject]
public class Labeled
{
    [Key(0)] public int X { get; set; }

    [IgnoreMember] public required string Label { get; set; } = "initial";

    [IgnoreMember] public required double Ratio { get; set; } = 1.5;

    [IgnoreMember] public required DayOfWeek Day { get; set; } = DayOfWeek.Friday;

    [IgnoreMember] public required decimal Price = 2.25m;

    [IgnoreMember] public required object Boxed { get; set; } = DayOfWeek.Friday; // the constant's type, not the member's

    [IgnoreMember] public required long Wide { get; set; } = 5; // an int constant widened by the assignment
}

public class ReviewRound21RegressionTests
{
    [Fact]
    public void NullableStruct_PopulatesFromTheExistingValue()
    {
        var oldSchema = V4.Serialize(new[] { 7 }); // one element: A only
        Pair plain = new() { A = 1, B = 9 };
        V4.Deserialize(oldSchema, ref plain);
        Assert.Equal((7, 9), (plain.A, plain.B));
        Pair? nullable = new Pair { A = 1, B = 9 };
        V4.Deserialize(oldSchema, ref nullable);
        Assert.Equal((7, 9), (nullable!.Value.A, nullable.Value.B));
    }

    [Fact]
    public void DynamicConstructorParameter_KeepsTheSerializationConstructor()
    {
        var bytes = V4.Serialize(new DynamicCtor("v"));
        Assert.Equal("dynamic", V4.Deserialize<DynamicCtor>(bytes)!.Chosen);
        Assert.Equal("dynamic", V4.Deserialize<DynamicCtorReflection>(bytes)!.Chosen);
    }

    [Fact]
    public void UnreadRequiredMember_KeepsItsConstantInitializer()
    {
        var back = V4.Deserialize<Labeled>(V4.Serialize(new Labeled { X = 3, Label = "x", Ratio = 9, Day = DayOfWeek.Monday, Price = 0, Boxed = 1, Wide = 0 }))!;
        Assert.Equal(DayOfWeek.Friday, back.Boxed);
        Assert.Equal(5L, back.Wide);
        Assert.Equal(3, back.X);
        Assert.Equal("initial", back.Label);
        Assert.Equal(1.5, back.Ratio);
        Assert.Equal(DayOfWeek.Friday, back.Day);
        Assert.Equal(2.25m, back.Price);
    }
}

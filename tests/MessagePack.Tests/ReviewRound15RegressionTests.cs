using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// a value object with user-defined == overloads: the generated null check must not bind to them (CS0034 with
// `== null` against both ==(TextValue, TextValue) and ==(TextValue, string)); compiling is the first assertion
[MessagePackObject]
public sealed class TextValue
{
    [Key(0)] public string? Text { get; set; }

    public static bool operator ==(TextValue? left, TextValue? right) => left?.Text == right?.Text;

    public static bool operator !=(TextValue? left, TextValue? right) => !(left == right);

    public static bool operator ==(TextValue? left, string? right) => left?.Text == right;

    public static bool operator !=(TextValue? left, string? right) => !(left == right);

    public override bool Equals(object? obj) => obj is TextValue other && other == this;

    public override int GetHashCode() => Text?.GetHashCode() ?? 0;
}

[MessagePackObject]
[UnionTag(typeof(TextValue), 0)]
[UnionTag(typeof(int), 1)]
public union TextOrNumber(TextValue, int);

// a struct whose explicit parameterless [SerializationConstructor] initializes a member the wire does not carry
[MessagePackObject]
public struct VersionedPoint
{
    [SerializationConstructor]
    public VersionedPoint()
    {
        Version = 42;
    }

    [Key(0)] public int X { get; set; }

    [IgnoreMember] public int Version { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public struct VersionedPointReflection
{
    [SerializationConstructor]
    public VersionedPointReflection()
    {
        Version = 42;
    }

    [Key(0)] public int X { get; set; }

    [IgnoreMember] public int Version { get; set; }
}

public class ReviewRound15RegressionTests
{
    [Fact]
    public void UserDefinedEqualityOperators_DoNotBreakTheGeneratedNullCheck()
    {
        Assert.Equal("t", V4.Deserialize<TextValue>(V4.Serialize(new TextValue { Text = "t" }))!.Text);
        Assert.Null(V4.Deserialize<TextValue>(V4.Serialize<TextValue?>(null)));
        TextOrNumber union = new TextValue { Text = "u" };
        Assert.Equal("u", Assert.IsType<TextValue>(V4.Deserialize<TextOrNumber>(V4.Serialize(union)).Value).Text);
    }

    [Fact]
    public void StructSerializationConstructor_Runs()
    {
        var bytes = V4.Serialize(new VersionedPoint { X = 3 });
        var generated = V4.Deserialize<VersionedPoint>(bytes);
        Assert.Equal(3, generated.X);
        Assert.Equal(42, generated.Version);
        var reflected = V4.Deserialize<VersionedPointReflection>(bytes);
        Assert.Equal(3, reflected.X);
        Assert.Equal(42, reflected.Version);
    }
}

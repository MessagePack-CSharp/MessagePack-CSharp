using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// a member hidden by a `new` declaration in a GENERIC base: the qualified map key must be spelled the same by the
// generator (which cannot know the closed CLR name) and the reflection tier (which used the closed FullName with
// assembly-qualified type arguments), or each tier reads the other's key as unknown
public class ShadowBase
{
    public int X { get; set; }
}

public class ShadowMiddle<T> : ShadowBase
{
    public int Doubled => X * 2; // getter-only: dropped from the map, and must not shift the legacy alias of X

    public new int X { get; set; }

    public T? Value { get; set; }
}

[MessagePackObject(true)]
public class ShadowLeaf : ShadowMiddle<int>
{
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class ShadowLeafReflection : ShadowMiddle<int>
{
}

// AllowPrivate: a private nested enum as a member type must resolve on the source-generated-only chain
[MessagePackObject(AllowPrivate = true)]
public partial class PrivateEnumHolder
{
    private enum Mode
    {
        A,
        B,
    }

    [Key(0)] private Mode mode = Mode.B;

    [Key(1)] private Mode? optional;

    [Key(2)] private Mode[] modes = [];

    [Key(3)] private List<Mode> list = [];

    [Key(4)] private Dictionary<string, Mode> map = new();

    [Key(5)] private HashSet<Mode> set = [];

    [Key(6)] private PrivateBox<Mode>? boxed;

    [IgnoreMember] public bool IsB => mode == Mode.B;

    [IgnoreMember] public bool BoxedIsA => boxed?.Value == Mode.A;

    [IgnoreMember] public bool HasOptional => optional is not null;

    [IgnoreMember] public int CollectionCount => modes.Length + list.Count + map.Count + set.Count;

    public void SetA()
    {
        mode = Mode.A;
        optional = Mode.A;
        modes = [Mode.A, Mode.B];
        list = [Mode.B];
        map["k"] = Mode.A;
        set.Add(Mode.B);
        boxed = new PrivateBox<Mode> { Value = Mode.A };
    }
}

// a public generic model closed over a private type inside an AllowPrivate holder: the holder's nested factory
// closes the generated open formatter
[MessagePackObject]
public class PrivateBox<T>
{
    [Key(0)] public T? Value { get; set; }
}

public class ReviewRound7RegressionTests
{
    [Fact]
    public void ShadowedMemberInGenericBase_HasTheSameKeyInBothTiers()
    {
        var generated = new ShadowLeaf { X = 2, Value = 3 };
        ((ShadowBase)generated).X = 1;
        var reflected = new ShadowLeafReflection { X = 2, Value = 3 };
        ((ShadowBase)reflected).X = 1;

        var bytes = V4.Serialize(generated);
        Assert.Equal(bytes, V4.Serialize(reflected));
        Assert.Contains("ShadowMiddle`1.X", System.Text.Encoding.UTF8.GetString(bytes)); // the definition's name, no type arguments

        var back = V4.Deserialize<ShadowLeaf>(V4.Serialize(reflected))!;
        Assert.Equal(1, ((ShadowBase)back).X);
        Assert.Equal(2, back.X);
        var backReflected = V4.Deserialize<ShadowLeafReflection>(bytes)!;
        Assert.Equal(1, ((ShadowBase)backReflected).X);
        Assert.Equal(2, backReflected.X);

        // v3 spelled the qualifier as the closed FullName (assembly-qualified type arguments): both tiers still read it
        var v3Key = typeof(ShadowMiddle<int>).FullName + ".X";
        Assert.Contains("[[System.Int32, ", v3Key);
        var v3Payload = V4.Serialize(new Dictionary<string, int> { ["X"] = 1, [v3Key] = 2, ["Value"] = 3 });
        var fromV3 = V4.Deserialize<ShadowLeaf>(v3Payload)!;
        Assert.Equal(1, ((ShadowBase)fromV3).X);
        Assert.Equal(2, fromV3.X);
        Assert.Equal(3, fromV3.Value);
        var fromV3Reflected = V4.Deserialize<ShadowLeafReflection>(v3Payload)!;
        Assert.Equal(1, ((ShadowBase)fromV3Reflected).X);
        Assert.Equal(2, fromV3Reflected.X);
        Assert.Equal(3, fromV3Reflected.Value);
    }

    [Fact]
    public void PrivateNestedEnum_ResolvesOnTheAotChain()
    {
        var aot = MessagePackSerializerOptions.DefaultAot;
        var value = new PrivateEnumHolder();
        value.SetA();
        var back = V4.Deserialize<PrivateEnumHolder>(V4.Serialize(value, aot), aot)!;
        Assert.False(back.IsB);
        Assert.True(back.HasOptional);
        Assert.Equal(5, back.CollectionCount); // Mode[], List<Mode>, Dictionary<string, Mode> and HashSet<Mode> resolve on the AOT chain too
        Assert.True(back.BoxedIsA); // and PrivateBox<Mode>, a user generic closed over the private enum
        Assert.True(V4.Deserialize<PrivateEnumHolder>(V4.Serialize(new PrivateEnumHolder(), aot), aot)!.IsB);
    }
}

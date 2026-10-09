using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// A construction-shaped child (constructor-bound, init-only) held in a parent's initialized member: the parent's
// populate loop hands the child formatter the initializer's instance, which cannot be refilled (its members are set
// at construction only), so the child is constructed afresh from the wire, as a collection that cannot be cleared
// would be. Before this rule the initializer's instance was "populated" with the constructor-bound payload skipped,
// and Child(42) read back as Child(0).
[MessagePackObject]
public class CtorChild
{
    [SerializationConstructor]
    public CtorChild(int x)
    {
        X = x;
    }

    [Key(0)] public int X { get; }
}

[MessagePackObject]
public class CtorParent
{
    [Key(0)] public CtorChild Child { get; set; } = new CtorChild(0);
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class CtorChildReflection
{
    [SerializationConstructor]
    public CtorChildReflection(int x)
    {
        X = x;
    }

    [Key(0)] public int X { get; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class CtorParentReflection
{
    [Key(0)] public CtorChildReflection Child { get; set; } = new CtorChildReflection(0);
}

[MessagePackObject]
public class InitChild
{
    [Key(0)] public int X { get; init; }
}

[MessagePackObject]
public class InitParent
{
    [Key(0)] public InitChild Child { get; set; } = new InitChild { X = 0 };
}

// a settable child keeps populate semantics: the initializer's instance is refilled in place
[MessagePackObject]
public class SettableChild
{
    [Key(0)] public int X { get; set; }
}

[MessagePackObject]
public class SettableParent
{
    [Key(0)] public SettableChild Child { get; set; } = new SettableChild();
}

public class InitializedChildTests
{
    [Fact]
    public void ConstructionShapedChild_IsConstructedFromTheWire()
    {
        Assert.Equal(42, V4.Deserialize<CtorParent>(V4.Serialize(new CtorParent { Child = new CtorChild(42) }))!.Child.X);
        Assert.Equal(42, V4.Deserialize<CtorParentReflection>(V4.Serialize(new CtorParentReflection { Child = new CtorChildReflection(42) }))!.Child.X);
        Assert.Equal(42, V4.Deserialize<InitParent>(V4.Serialize(new InitParent { Child = new InitChild { X = 42 } }))!.Child.X);

        // through the root populate entry too: the parent is refilled, its child replaced
        var parent = new CtorParent { Child = new CtorChild(1) };
        var same = parent;
        V4.Deserialize(V4.Serialize(new CtorParent { Child = new CtorChild(9) }), ref parent);
        Assert.Same(same, parent);
        Assert.Equal(9, parent.Child.X);
    }

    [Fact]
    public void SettableChild_IsRefilledInPlace()
    {
        var parent = new SettableParent();
        var child = parent.Child;
        V4.Deserialize(V4.Serialize(new SettableParent { Child = new SettableChild { X = 42 } }), ref parent);
        Assert.Same(child, parent.Child);
        Assert.Equal(42, child.X);
    }
}

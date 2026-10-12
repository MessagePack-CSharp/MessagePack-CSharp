using MessagePack;
using MessagePack.Unity;
using UnityEngine;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// DefaultAot.WithUnity() (IL2CPP, Native AOT) has no generic or enum tier to close the wrapper forms at run time: the
// Unity factory spells them out itself, so T?[], List<T?> and the WrapMode / GradientMode enums serialize as roots, as
// v3's UnityResolver allowed (they used to be "Type ... is not found" on that chain).
public class UnityAotRootsTests
{
    static readonly MessagePackSerializerOptions Aot = new(new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot.WithUnity()));
    static readonly MessagePackSerializerOptions Jit = new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithUnity()));

    static void AssertRoot<T>(T value)
    {
        var bytes = V4.Serialize(value, Aot);
        Assert.Equal(V4.Serialize(value, Jit), bytes); // the same wire as the generic and enum tiers produce
        Assert.Equal(value, V4.Deserialize<T>(bytes, Aot));
    }

    [Fact]
    public void NullableArraysAndLists_AreRoots()
    {
        AssertRoot<Vector3?[]>([new Vector3(1, 2, 3), null]);
        AssertRoot(new List<Vector2?> { null, new Vector2(4, 5) });
        AssertRoot<Color?[]>([new Color(1, 0, 0, 1), null]);
    }

    [Fact]
    public void CurveAndGradientEnums_AreRoots()
    {
        AssertRoot(WrapMode.Loop);
        AssertRoot(GradientMode.Fixed);
        AssertRoot<WrapMode?>(WrapMode.ClampForever);
        AssertRoot<WrapMode[]>([WrapMode.Once, WrapMode.PingPong]);
        AssertRoot(new List<GradientMode> { GradientMode.Blend });
        Assert.Equal(V4.Serialize((int)WrapMode.Loop), V4.Serialize(WrapMode.Loop, Aot)); // the integer form
    }
}

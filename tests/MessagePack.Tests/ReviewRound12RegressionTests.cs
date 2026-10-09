using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// IgnoreMemberAttribute is not sealed: a derived attribute must opt a member out in the generated formatter as it
// does in the reflection tier
public sealed class MyIgnoreAttribute : IgnoreMemberAttribute
{
}

[MessagePackObject(true)]
public class DerivedIgnoreModel
{
    public int A { get; set; }

    [MyIgnore] public int B { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class DerivedIgnoreReflectionModel
{
    public int A { get; set; }

    [MyIgnore] public int B { get; set; }
}

public class ReviewRound12RegressionTests
{
    [Fact]
    public void DerivedIgnoreMemberAttribute_OptsOutInBothTiers()
    {
        var generated = V4.Serialize(new DerivedIgnoreModel { A = 1, B = 2 });
        Assert.Equal(V4.Serialize(new DerivedIgnoreReflectionModel { A = 1, B = 2 }), generated);
        Assert.Equal(new byte[] { 0x81, 0xA1, (byte)'A', 0x01 }, generated); // {"A": 1}
    }
}

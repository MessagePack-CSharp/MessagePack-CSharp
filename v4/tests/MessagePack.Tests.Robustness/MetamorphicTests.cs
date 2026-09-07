extern alias V3;
using CsCheck;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V3Ser = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.Robustness;

// Metamorphic layer: for any generated value of a built-in-formatter type, v4 and the real
// v3.1.8 oracle must agree on the wire, and each must read the other's bytes back to the
// original. This is a different axis from the malformed-input contract (RobustnessTests) -
// it property-tests v4's forward wire COMPATIBILITY with v3, an intentional design goal
// (this library is MessagePack-CSharp v4). CsCheck drives the values and, on any divergence,
// shrinks to the minimal value that differs and prints a reproduction seed.
//
// Scope is deliberately built-in types only (primitives + BCL collections): v3 and v4 both
// serialize these through their own built-in formatters with no annotations, so nothing
// needs a source generator. Annotated POCOs can't participate - v3 and v4 both own the
// MessagePack namespace, so their generators can't run in one compilation - and their wire
// compatibility is covered separately by MessagePack.Tests.V3Compat.
public class MetamorphicTests
{
    // These are cheap (no malformed-input hang risk, no per-attempt thread), so a higher
    // default iteration count than RobustnessTests. ROBUSTNESS_SECONDS switches to a soak.
    const int DefaultIter = 1000;

    [Fact] public void SByte() => WireIdentical(Gen.SByte);
    [Fact] public void Byte() => WireIdentical(Gen.Byte);
    [Fact] public void Short() => WireIdentical(Gen.Short);
    [Fact] public void UShort() => WireIdentical(Gen.UShort);
    [Fact] public void Int() => WireIdentical(Gen.Int);
    [Fact] public void UInt() => WireIdentical(Gen.UInt);
    [Fact] public void Long() => WireIdentical(Gen.Long);
    [Fact] public void ULong() => WireIdentical(Gen.ULong);
    [Fact] public void Float() => WireIdentical(Gen.Float);
    [Fact] public void Double() => WireIdentical(Gen.Double);
    [Fact] public void Bool() => WireIdentical(Gen.Bool);
    [Fact] public void String() => WireIdentical(Gen.String);
    [Fact] public void ByteArray() => WireIdentical(Gen.Byte.Array);
    [Fact] public void IntArray() => WireIdentical(Gen.Int.Array);
    [Fact] public void StringArray() => WireIdentical(Gen.String.Array);
    [Fact] public void NestedIntArray() => WireIdentical(Gen.Int.Array.Array);
    [Fact] public void IntList() => WireIdentical(Gen.Int.Array.Select(a => a.ToList()));
    [Fact] public void StringIntMap() => WireIdentical(Gen.Dictionary(Gen.String, Gen.Int));

    static void WireIdentical<T>(Gen<T> gen)
    {
        var soak = Environment.GetEnvironmentVariable("ROBUSTNESS_SECONDS");
        if (int.TryParse(soak, out var seconds) && seconds > 0)
        {
            gen.Sample(AssertWireIdentical, time: seconds);
        }
        else
        {
            gen.Sample(AssertWireIdentical, iter: DefaultIter);
        }
    }

    static bool AssertWireIdentical<T>(T value)
    {
        var v4Bytes = V4.Serialize(value);
        var v3Bytes = V3Ser.Serialize(value);
        if (!v4Bytes.AsSpan().SequenceEqual(v3Bytes))
        {
            throw new WireDivergenceException(
                $"{typeof(T).Name} wire differs: v4={RobustnessHarness.Hex(v4Bytes)} v3={RobustnessHarness.Hex(v3Bytes)}");
        }

        // each side reads the other's bytes back to the original value
        Assert.Equal(value, V4.Deserialize<T>(v3Bytes));
        Assert.Equal(value, V3Ser.Deserialize<T>(v4Bytes));
        return true;
    }

    sealed class WireDivergenceException(string message) : Exception(message);
}

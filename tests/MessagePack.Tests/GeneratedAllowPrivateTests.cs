extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// AllowPrivate through the source generator: the formatter is emitted as a class NESTED
// inside the (partial) target type, which is what grants private-member access with zero
// reflection. Byte-compat anchor is v3's AllowPrivate handling of the same types.
public class GeneratedAllowPrivateTests
{
    static void AssertBytesAndRoundtrip<T>(T value)
    {
        var ours = V4.Serialize(value);
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, ours);

        var back = V4.Deserialize<T>(ours);
        Assert.Equal(oracle, Oracle.Serialize(back));
    }

    [Fact]
    public void PrivateFieldAndPrivateSetter_Roundtrip()
    {
        AssertBytesAndRoundtrip(GenPrivatePoco.Create(42, 7));

        var back = V4.Deserialize<GenPrivatePoco>(V4.Serialize(GenPrivatePoco.Create(42, 7)))!;
        Assert.Equal(42, back.SecretView);
        Assert.Equal(7, back.Half);
    }

    [Fact]
    public void PrivateConstructor_IsReachableFromTheNestedFormatter()
    {
        AssertBytesAndRoundtrip(GenPrivateCtorPoco.Create(5, "内緒"));

        var back = V4.Deserialize<GenPrivateCtorPoco>(V4.Serialize(GenPrivateCtorPoco.Create(5, "内緒")))!;
        Assert.Equal(5, back.Id);
        Assert.Equal("内緒", back.Label);
    }

    [Fact]
    public void GeneratedRegistryServesTheType()
    {
        // proof the NESTED formatter (not the reflection tail) is what the default chain
        // resolves: the generated factory answers for the type
        var formatter = MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance
            .CreateFormatter<SerializerFoundation.CompatibleArrayPoolListWriteBuffer, SerializerFoundation.CompatibleReadOnlySequenceReadBuffer>(typeof(GenPrivatePoco));
        Assert.NotNull(formatter);
        Assert.Contains("GeneratedMessagePackFormatter", formatter!.GetType().Name);
    }
}

[V3::MessagePack.MessagePackObject(AllowPrivate = true)]
public partial class GenPrivatePoco
{
    [V3::MessagePack.Key(0)] int secret;
    [V3::MessagePack.Key(1)] public int Half { get; private set; }

    public static GenPrivatePoco Create(int secret, int half) => new() { secret = secret, Half = half };

    [V3::MessagePack.IgnoreMember] public int SecretView => secret;
}

[V3::MessagePack.MessagePackObject(AllowPrivate = true)]
public partial class GenPrivateCtorPoco
{
    [V3::MessagePack.Key(0)] public int Id { get; }
    [V3::MessagePack.Key(1)] public string? Label { get; }

    GenPrivateCtorPoco(int id, string? label)
    {
        Id = id;
        Label = label;
    }

    public static GenPrivateCtorPoco Create(int id, string? label) => new(id, label);
}

extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// IMessagePackSerializationCallbackReceiver: OnBeforeSerialize runs before any member is
// read, OnAfterDeserialize after all members are populated — in the source-generated
// formatters AND the reflection tier. The callback types implement BOTH the v3 and v4
// interfaces (one public method pair satisfies both), so the oracle fires them too and
// byte equality stays meaningful.
public class SerializationCallbackTests
{
    [Fact]
    public void Class_OnBeforeSerialize_RunsBeforeMembersAreRead()
    {
        // Normalized is only correct if the callback ran before the member was written
        var value = new GenCallbackPoco { Raw = 21 };
        var ours = V4.Serialize(value);
        Assert.Equal(Oracle.Serialize(new GenCallbackPoco { Raw = 21 }), ours);

        var back = V4.Deserialize<GenCallbackPoco>(ours)!;
        Assert.Equal(21, back.Raw);
        Assert.Equal(42, back.Normalized);
        Assert.True(back.Restored); // OnAfterDeserialize ran ([IgnoreMember], so not from the wire)
    }

    [Fact]
    public void Struct_ExplicitImplementation_MutationsSurvive()
    {
        // explicit interface implementation on a struct: the generated code goes through
        // SourceGeneratorHelper's ref-taking constrained-call bridge (no box), so the
        // checksum computed in OnBeforeSerialize reaches the wire and the flag set in
        // OnAfterDeserialize reaches the returned value
        var value = new GenCallbackStruct { A = 3, B = 4 };
        var ours = V4.Serialize(value);

        var back = V4.Deserialize<GenCallbackStruct>(ours);
        Assert.Equal(3, back.A);
        Assert.Equal(4, back.B);
        Assert.Equal(7, back.Checksum); // written by OnBeforeSerialize, on the wire
        Assert.True(back.Restored);     // written by OnAfterDeserialize, after populate
    }

    [Fact]
    public void ReflectionTier_InvokesCallbacksToo()
    {
        // SuppressSourceGeneration: the default chain's reflection tail serves the type,
        // and it must fire the same callbacks the generated formatters do
        var registry = MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance
            .CreateFormatter<SerializerFoundation.CompatibleArrayPoolListWriteBuffer, SerializerFoundation.CompatibleReadOnlySequenceReadBuffer>(typeof(GenSuppressedCallbackPoco));
        Assert.Null(registry); // proof the generator skipped it

        var bytes = V4.Serialize(new GenSuppressedCallbackPoco { Raw = 5 });
        var back = V4.Deserialize<GenSuppressedCallbackPoco>(bytes)!;
        Assert.Equal(5, back.Raw);
        Assert.Equal(10, back.Normalized);
        Assert.True(back.Restored);
    }
}

[V3::MessagePack.MessagePackObject]
public class GenCallbackPoco : IMessagePackSerializationCallbackReceiver, V3::MessagePack.IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int Raw { get; set; }
    [V3::MessagePack.Key(1)] public int Normalized { get; set; }
    [V3::MessagePack.IgnoreMember] public bool Restored { get; set; }

    public void OnBeforeSerialize() => Normalized = Raw * 2;

    public void OnAfterDeserialize() => Restored = true;
}

[V3::MessagePack.MessagePackObject]
public struct GenCallbackStruct : IMessagePackSerializationCallbackReceiver, V3::MessagePack.IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int A { get; set; }
    [V3::MessagePack.Key(1)] public int B { get; set; }
    [V3::MessagePack.Key(2)] public int Checksum { get; set; }
    [V3::MessagePack.IgnoreMember] public bool Restored { get; set; }

    void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize() => Checksum = A + B;

    void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize() => Restored = true;

    void V3::MessagePack.IMessagePackSerializationCallbackReceiver.OnBeforeSerialize() => Checksum = A + B;

    void V3::MessagePack.IMessagePackSerializationCallbackReceiver.OnAfterDeserialize() => Restored = true;
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class GenSuppressedCallbackPoco : IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int Raw { get; set; }
    [V3::MessagePack.Key(1)] public int Normalized { get; set; }
    [V3::MessagePack.IgnoreMember] public bool Restored { get; set; }

    public void OnBeforeSerialize() => Normalized = Raw * 2;

    public void OnAfterDeserialize() => Restored = true;
}

// ---- v3-built assemblies: the interface from MessagePack.Annotations 3.x is a different Type with the same name ----

// The reflection tier matches attributes by name so a v3-built DLL works as is; the callback interface used to be
// checked by identity, so a type implementing ONLY v3's interface had its callbacks skipped without a word (and wrote
// different bytes than v3 did for the same object).
public class LegacyCallbackInterfaceTests
{
    [Fact]
    public void V3OnlyInterface_CallbacksRun_AndBytesMatchTheOracle()
    {
        var bytes = V4.Serialize(new V3OnlyCallbackPoco { Raw = 21 });
        Assert.Equal(Oracle.Serialize(new V3OnlyCallbackPoco { Raw = 21 }), bytes); // [21, 42]: the oracle fires its own callbacks

        var back = V4.Deserialize<V3OnlyCallbackPoco>(bytes)!;
        Assert.Equal(42, back.Normalized);
        Assert.True(back.Restored);
    }

    [Fact]
    public void V3OnlyInterface_ExplicitImplementationOnAStruct_MutatesThroughTheBox()
    {
        var bytes = V4.Serialize(new V3OnlyCallbackStruct { A = 2, B = 3 });
        var back = V4.Deserialize<V3OnlyCallbackStruct>(bytes);
        Assert.Equal(5, back.Checksum);
        Assert.True(back.Restored);
    }

    [Fact]
    public void V3OnlyInterface_HiddenMethodDoesNotWin()
    {
        // dispatch goes through the interface slot: the base implementation that satisfies the interface runs
        var back = V4.Deserialize<V3OnlyHidingDerived>(V4.Serialize(new V3OnlyHidingDerived { Raw = 1 }))!;
        Assert.True(back.Restored);
        Assert.False(back.HiddenRan);
    }

    [Fact]
    public void NoInterface_SameNamedMethods_DoNotRun()
    {
        var back = V4.Deserialize<LookalikeCallbackPoco>(V4.Serialize(new LookalikeCallbackPoco { Raw = 1 }))!;
        Assert.Equal(0, back.Normalized);
        Assert.False(back.Restored);
    }

    [Fact]
    public void V3OnlyInterface_CallbackException_SurfacesUnwrapped()
    {
        var bytes = V4.Serialize(new V3OnlyThrowingPoco());
        Assert.Throws<InvalidOperationException>(() => V4.Deserialize<V3OnlyThrowingPoco>(bytes));
    }
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class V3OnlyCallbackPoco : V3::MessagePack.IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int Raw { get; set; }
    [V3::MessagePack.Key(1)] public int Normalized { get; set; }
    [V3::MessagePack.IgnoreMember] public bool Restored { get; set; }

    public void OnBeforeSerialize() => Normalized = Raw * 2;

    public virtual void OnAfterDeserialize() => Restored = true;
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class V3OnlyHidingDerived : V3OnlyCallbackPoco
{
    [V3::MessagePack.IgnoreMember] public bool HiddenRan { get; set; }

    public new void OnAfterDeserialize() => HiddenRan = true;
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public struct V3OnlyCallbackStruct : V3::MessagePack.IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int A;
    [V3::MessagePack.Key(1)] public int B;
    [V3::MessagePack.Key(2)] public int Checksum;
    [V3::MessagePack.IgnoreMember] public bool Restored;

    void V3::MessagePack.IMessagePackSerializationCallbackReceiver.OnBeforeSerialize() => Checksum = A + B;

    void V3::MessagePack.IMessagePackSerializationCallbackReceiver.OnAfterDeserialize() => Restored = true;
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class LookalikeCallbackPoco
{
    [V3::MessagePack.Key(0)] public int Raw { get; set; }
    [V3::MessagePack.Key(1)] public int Normalized { get; set; }
    [V3::MessagePack.IgnoreMember] public bool Restored { get; set; }

    public void OnBeforeSerialize() => Normalized = Raw * 2;

    public void OnAfterDeserialize() => Restored = true;
}

[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class V3OnlyThrowingPoco : V3::MessagePack.IMessagePackSerializationCallbackReceiver
{
    [V3::MessagePack.Key(0)] public int Raw { get; set; }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() => throw new InvalidOperationException("from the callback");
}

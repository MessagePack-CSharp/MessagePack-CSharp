extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Generic [MessagePackObject] types: the generator emits ONE open formatter (generic over
// the value type's own parameters) and registers the open value-type definition; the
// factory closes the formatter over the runtime type arguments via MakeGenericType.
public class GeneratedGenericTests
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
    public void ClosedInstantiations_Roundtrip()
    {
        AssertBytesAndRoundtrip(new GenWrapper<int> { Value = 42, Count = 1 });
        AssertBytesAndRoundtrip(new GenWrapper<string> { Value = "中身", Count = 2 });
        AssertBytesAndRoundtrip(new GenWrapper<GenIntKeyPoco> { Value = new GenIntKeyPoco { Id = 1, Name = "n" }, Count = 3 });
        AssertBytesAndRoundtrip(new GenWrapper<GenWrapper<int>> { Value = new GenWrapper<int> { Value = 9 }, Count = 4 });
    }

    [Fact]
    public void TwoParameters_WithConstraints_Roundtrip()
    {
        AssertBytesAndRoundtrip(new GenPair<string, int> { First = "a", Second = 5 });

        var back = V4.Deserialize<GenPair<string, int>>(V4.Serialize(new GenPair<string, int> { First = "a", Second = 5 }))!;
        Assert.Equal("a", back.First);
        Assert.Equal(5, back.Second);
    }

    [Fact]
    public void GeneratedRegistryServesClosedGenerics()
    {
        // the open definition is registered; a closed type must resolve through it (and
        // NOT fall through to the reflection tail)
        var formatter = MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance
            .CreateFormatter<SerializerFoundation.CompatibleArrayPoolListWriteBuffer, SerializerFoundation.CompatibleReadOnlySequenceReadBuffer>(typeof(GenWrapper<int>));
        Assert.NotNull(formatter);
        Assert.Contains("GenWrapper", formatter!.GetType().Name);
    }

    [Fact]
    public void GenericMemberTypedByTypeParameter_UsesTheResolvedFormatter()
    {
        // T-typed member goes through the formatter field; DateTime through the chain
        var stamp = new DateTime(2026, 8, 23, 1, 2, 3, DateTimeKind.Utc);
        AssertBytesAndRoundtrip(new GenWrapper<DateTime> { Value = stamp, Count = 1 });
    }
}

[V3::MessagePack.MessagePackObject]
public class GenWrapper<T>
{
    [V3::MessagePack.Key(0)] public T? Value { get; set; }
    [V3::MessagePack.Key(1)] public int Count { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenPair<TFirst, TSecond>
    where TFirst : class
    where TSecond : struct
{
    [V3::MessagePack.Key(0)] public TFirst? First { get; set; }
    [V3::MessagePack.Key(1)] public TSecond Second { get; set; }
}

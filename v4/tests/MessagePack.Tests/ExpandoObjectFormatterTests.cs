extern alias V3;
#pragma warning disable CS0618 // deprecated ExpandoObject support and typeless usage below are the consenting caller

using System.Dynamic;
using MessagePack.Formatters;

namespace MessagePack.Tests;

public class ExpandoObjectFormatterTests
{
    // the opt-in surface: ExpandoObject left every default chain, only this factory serves it
    static readonly MessagePackSerializerOptions OptIn = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Combine(
            new ExpandoObjectFormatterFactory(),
            MessagePackFormatterFactory.Default)]));

    static readonly MessagePackSerializerOptions OptInTypeless = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Combine(
                new ExpandoObjectFormatterFactory(),
                MessagePackFormatterFactory.Default)
            .WithContractless()
            .WithTypeless(TypelessTypeLoader.LoadAnyType())]));

    [Fact]
    public void DefaultChain_NoLongerServes()
    {
        dynamic expando = new ExpandoObject();
        expando.Name = "expando";

        var serializeMiss = Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize((ExpandoObject)expando));
        Assert.Contains(nameof(ExpandoObject), serializeMiss.Message);

        var payload = MessagePackSerializer.Serialize((ExpandoObject)expando, OptIn);
        var deserializeMiss = Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Deserialize<ExpandoObject>(payload));
        Assert.Contains(nameof(ExpandoObject), deserializeMiss.Message);
    }

    [Fact]
    public void ContractlessChain_RefusesInsteadOfEmptyMap()
    {
        // ExpandoObject has no public members: a contractless claim would serialize an
        // empty map (silent data loss), so the reflection tier must decline it too
        var contractless = new MessagePackSerializerOptions(
            new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

        dynamic expando = new ExpandoObject();
        expando.Name = "expando";

        var miss = Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize((ExpandoObject)expando, contractless));
        Assert.Contains(nameof(ExpandoObject), miss.Message);
    }

    [Fact]
    public void RoundTrip_OptInChain()
    {
        dynamic expando = new ExpandoObject();
        expando.Name = "expando";
        expando.Age = 42;
        dynamic nested = new ExpandoObject();
        nested.X = 1;
        expando.Nested = nested;

        var payload = MessagePackSerializer.Serialize((ExpandoObject)expando, OptIn);
        dynamic back = MessagePackSerializer.Deserialize<ExpandoObject>(payload, OptIn)!;

        Assert.Equal("expando", back.Name);
        Assert.Equal(42, (int)back.Age);
        // v3 rule: only the statically-requested root is an ExpandoObject; nested maps
        // come back per the object formatter (Dictionary<object, object?>)
        var nestedBack = Assert.IsType<Dictionary<object, object?>>((object)back.Nested);
        Assert.Equal(1, Assert.IsType<int>(nestedBack["X"])); // forced-width int32 write reads back as Int32
    }

    [Fact]
    public void RoundTrip_ReadableAsPlainMap()
    {
        dynamic expando = new ExpandoObject();
        expando.A = 1;
        expando.B = "b";

        // the recommended migration target reads the identical wire form on the default chain
        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(
            MessagePackSerializer.Serialize((ExpandoObject)expando, OptIn))!;
        Assert.Equal(2, map.Count);
        Assert.Equal("b", map["B"]);
    }

    [Fact]
    public void Null_RoundTrips()
    {
        Assert.Null(MessagePackSerializer.Deserialize<ExpandoObject?>(MessagePackSerializer.Serialize<ExpandoObject?>(null, OptIn), OptIn));
    }

    public class ExpandoPerson
    {
        public int Id { get; set; }
    }

    [Fact]
    public void TypelessChain_EmbedsMemberTypes()
    {
        dynamic expando = new ExpandoObject();
        expando.Person = new ExpandoPerson { Id = 7 };
        expando.Plain = 1;

        // through an object slot the root itself rides typeless and comes back typed
        dynamic back = MessagePackSerializer.Deserialize<object?>(
            MessagePackSerializer.Serialize<object?>((object)expando, OptInTypeless), OptInTypeless)!;
        Assert.IsType<ExpandoObject>((object)back);
        Assert.Equal(7, Assert.IsType<ExpandoPerson>((object)back.Person).Id);
        Assert.Equal(1, (int)back.Plain);
    }

    [Fact]
    public void CrossCompatible_WithV3_BothDirections()
    {
        dynamic expando = new ExpandoObject();
        expando.Name = "x";
        expando.Age = 3;

        // v3 standard bytes -> UMP
        dynamic fromV3 = MessagePackSerializer.Deserialize<ExpandoObject>(
            V3::MessagePack.MessagePackSerializer.Serialize((ExpandoObject)expando), OptIn)!;
        Assert.Equal("x", fromV3.Name);
        Assert.Equal(3, (int)fromV3.Age);

        // UMP bytes -> v3
        dynamic fromUmp = V3::MessagePack.MessagePackSerializer.Deserialize<ExpandoObject>(
            MessagePackSerializer.Serialize((ExpandoObject)expando, OptIn))!;
        Assert.Equal("x", fromUmp.Name);
        Assert.Equal(3, (int)fromUmp.Age);

        // and the typeless form: v3 wraps ExpandoObject in ext100; UMP must reconstruct
        dynamic fromV3Typeless = MessagePackSerializer.Deserialize<object?>(
            V3::MessagePack.MessagePackSerializer.Typeless.Serialize((object)expando),
            OptInTypeless)!;
        Assert.IsType<ExpandoObject>((object)fromV3Typeless);
        Assert.Equal("x", fromV3Typeless.Name);
    }
}

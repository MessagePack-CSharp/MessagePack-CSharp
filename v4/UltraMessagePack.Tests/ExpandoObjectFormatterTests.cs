#pragma warning disable CS0618 // typeless usage below is the consenting caller

using System.Dynamic;
using UltraMessagePack.Formatters;

namespace UltraMessagePack.Tests;

public class ExpandoObjectFormatterTests
{
    [Fact]
    public void RoundTrip_DefaultChain()
    {
        dynamic expando = new ExpandoObject();
        expando.Name = "expando";
        expando.Age = 42;
        dynamic nested = new ExpandoObject();
        nested.X = 1;
        expando.Nested = nested;

        var payload = MessagePackSerializer.Serialize((ExpandoObject)expando);
        dynamic back = MessagePackSerializer.Deserialize<ExpandoObject>(payload)!;

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

        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(
            MessagePackSerializer.Serialize((ExpandoObject)expando))!;
        Assert.Equal(2, map.Count);
        Assert.Equal("b", map["B"]);
    }

    [Fact]
    public void Null_RoundTrips()
    {
        Assert.Null(MessagePackSerializer.Deserialize<ExpandoObject?>(MessagePackSerializer.Serialize<ExpandoObject?>(null)));
    }

    public class ExpandoPerson
    {
        public int Id { get; set; }
    }

    [Fact]
    public void TypelessChain_EmbedsMemberTypes()
    {
        var typeless = new MessagePackSerializerOptions(
            new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())]));

        dynamic expando = new ExpandoObject();
        expando.Person = new ExpandoPerson { Id = 7 };
        expando.Plain = 1;

        // through an object slot the root itself rides typeless and comes back typed
        dynamic back = MessagePackSerializer.Deserialize<object?>(
            MessagePackSerializer.Serialize<object?>((object)expando, typeless), typeless)!;
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
            MessagePack.MessagePackSerializer.Serialize((ExpandoObject)expando))!;
        Assert.Equal("x", fromV3.Name);
        Assert.Equal(3, (int)fromV3.Age);

        // UMP bytes -> v3
        dynamic fromUmp = MessagePack.MessagePackSerializer.Deserialize<ExpandoObject>(
            MessagePackSerializer.Serialize((ExpandoObject)expando))!;
        Assert.Equal("x", fromUmp.Name);
        Assert.Equal(3, (int)fromUmp.Age);

        // and the typeless form: v3 wraps ExpandoObject in ext100; UMP must reconstruct
        dynamic fromV3Typeless = MessagePackSerializer.Deserialize<object?>(
            MessagePack.MessagePackSerializer.Typeless.Serialize((object)expando),
            new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())])))!;
        Assert.IsType<ExpandoObject>((object)fromV3Typeless);
        Assert.Equal("x", fromV3Typeless.Name);
    }
}

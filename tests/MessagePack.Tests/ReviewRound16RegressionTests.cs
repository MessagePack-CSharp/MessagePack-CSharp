using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

public class ReviewRound16RegressionTests
{
    static readonly MessagePackSerializerOptions Typeless = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(Guid)))));

    // a plain object[] / Dictionary<object, object> written with Typeless enabled holds Typeless exts as elements:
    // reading the outer value as `object` must hand the elements to the chain's object formatter (Typeless), not to
    // the primitive switch (which would take the ext for a timestamp)
    [Fact]
    public void ObjectReader_DelegatesElementsToTheChainsObjectFormatter()
    {
        var guid = Guid.NewGuid();
        var array = V4.Serialize(new object[] { guid, 1 }, Typeless);
        var backArray = Assert.IsType<object?[]>(V4.Deserialize<object>(array, Typeless));
        Assert.Equal(guid, backArray[0]);

        var map = V4.Serialize(new Dictionary<string, object> { ["g"] = guid }, Typeless);
        var backMap = Assert.IsType<Dictionary<object, object?>>(V4.Deserialize<object>(map, Typeless));
        Assert.Equal(guid, backMap["g"]);

        // without Typeless the primitive reader is its own element reader, as before
        var plain = V4.Serialize(new object[] { 1, "s" });
        Assert.Equal("s", Assert.IsType<object?[]>(V4.Deserialize<object>(plain))[1]);
    }
}

using MessagePack;
using MessagePack.Formatters;

namespace MessagePack.Tests.NetFx;

// IMessagePackSurrogate on the netstandard2.0 asset: the conversions are instance members
// (no static abstract interface members), so the feature exists downlevel at all. The
// generator auto-registers the target on this TFM too, and the explicit-chain factory
// takes the downlevel CreateFormatter signature.
public class DownlevelSurrogateTests
{
    [Fact]
    public void AutoRegisteredSurrogateRoundtrips()
    {
        var bytes = MessagePackSerializer.Serialize(new DownlevelUser(42, "realm"));
        Assert.Equal(MessagePackSerializer.Serialize(new DownlevelUserSurrogate(42, "realm")), bytes);

        var back = MessagePackSerializer.Deserialize<DownlevelUser>(bytes)!;
        Assert.Equal(42, back.Value);
        Assert.Equal("realm", back.Realm);

        Assert.Null(MessagePackSerializer.Deserialize<DownlevelUser>(MessagePackSerializer.Serialize<DownlevelUser?>(null)));

        // a payload the constructor rejects must throw, not materialize an invalid instance
        var poisoned = MessagePackSerializer.Serialize(new DownlevelUserSurrogate(-1, "realm"));
        Assert.ThrowsAny<Exception>(() => MessagePackSerializer.Deserialize<DownlevelUser>(poisoned));
    }

    [Fact]
    public void FactoryComposesIntoExplicitChain()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new SurrogateFormatterFactory<DownlevelUser, DownlevelUserSurrogate>(),
            MessagePackFormatterFactory.Default,
        ]));
        var back = MessagePackSerializer.Deserialize<DownlevelUser>(MessagePackSerializer.Serialize(new DownlevelUser(7, "chain"), options), options)!;
        Assert.Equal(7, back.Value);
        Assert.Equal("chain", back.Realm);
    }
}

public class DownlevelUser
{
    public int Value { get; }
    public string Realm { get; }

    public DownlevelUser(int value, string realm)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
        Realm = realm;
    }
}

[MessagePackObject]
public readonly struct DownlevelUserSurrogate : IMessagePackSurrogate<DownlevelUser, DownlevelUserSurrogate>
{
    [Key(0)]
    public int Value { get; }

    [Key(1)]
    public string Realm { get; }

    public DownlevelUserSurrogate(int value, string realm)
    {
        Value = value;
        Realm = realm;
    }

    public DownlevelUserSurrogate ToSurrogate(DownlevelUser value) => new(value.Value, value.Realm);

    public DownlevelUser ToTarget() => new(Value, Realm);
}

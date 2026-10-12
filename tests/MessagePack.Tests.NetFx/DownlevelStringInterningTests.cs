using MessagePack;

namespace MessagePack.Tests.NetFx;

// The netstandard2.0 build had no StringInterningFormatterFactory at all (its HashSet<T> has no TryGetValue, so the
// canonical instance could not be retrieved); the pool there is a Dictionary mapping each string to itself, on the
// library's flooding-resistant string comparer. Only the parameterless constructor exists on this build.
public class DownlevelStringInterningTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Combine(new StringInterningFormatterFactory(), MessagePackFormatterFactory.Default)));

    [Fact]
    public void RepeatedStrings_ShareOneInstance()
    {
        var source = new[] { "column", "column", "other", "" };
        var back = MessagePackSerializer.Deserialize<string[]>(MessagePackSerializer.Serialize(source, Options), Options)!;

        Assert.Equal(source, back);
        Assert.Same(back[0], back[1]);
        Assert.NotSame(back[0], back[2]);
        Assert.Same(string.Empty, back[3]);

        // the pool lives in the factory, so a later message shares the instance too
        var again = MessagePackSerializer.Deserialize<string>(MessagePackSerializer.Serialize("column", Options), Options);
        Assert.Same(back[0], again);

        // and nil stays null
        Assert.Null(MessagePackSerializer.Deserialize<string>(new byte[] { 0xC0 }, Options));
    }
}

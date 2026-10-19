using MessagePack;
using Xunit;

namespace MessagePack.Tests;

// MessagePackSerializerOptions.Resolver is init-only: a `with` expression points a copy of the options at another resolver,
// while the constructor still guarantees a non-null one and the resolver's own settings stay fixed for its lifetime.
public class OptionsWithResolverTests
{
    public class Unannotated
    {
        public int Value { get; set; }
    }

    [Fact]
    public void With_ReplacesTheResolver_AndKeepsThePerCallSettings()
    {
        var original = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default)) { MaxDepth = 7, MaxBufferedMessageSize = 1234 };
        var other = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless());

        var copy = original with { Resolver = other };

        Assert.Same(other, copy.Resolver);
        Assert.Equal(7, copy.MaxDepth);
        Assert.Equal(1234, copy.MaxBufferedMessageSize);
        Assert.NotSame(other, original.Resolver); // the source options are untouched
    }

    [Fact]
    public void With_TheNewResolverServesTheCalls()
    {
        var contractless = MessagePackSerializerOptions.Default with { Resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless()) };

        var bytes = MessagePackSerializer.Serialize(new Unannotated { Value = 42 }, contractless);
        Assert.Equal(42, MessagePackSerializer.Deserialize<Unannotated>(bytes, contractless).Value);

        // the default chain has no formatter for the unannotated type, so the swap is what made the round trip work
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize(new Unannotated(), MessagePackSerializerOptions.Default));
    }

    [Fact]
    public void With_NullResolver_IsRejected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => MessagePackSerializerOptions.DefaultAot with { Resolver = null! });
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullResolver_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MessagePackSerializerOptions(null!));
    }
}

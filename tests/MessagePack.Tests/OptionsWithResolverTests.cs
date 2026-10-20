using MessagePack;
using Xunit;

namespace MessagePack.Tests;

// MessagePackSerializerOptions.Resolver is init-only: a `with` expression points a copy of the options at another resolver,
// while the constructor still guarantees a non-null one and the resolver's own settings stay fixed for its lifetime.
public partial class OptionsWithResolverTests
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
        Assert.Throws<ArgumentNullException>(() => new MessagePackSerializerOptions((MessagePackFormatterResolver)null!));
    }

    // the factory constructor creates the resolver with its default settings; the explicit resolver constructor is for
    // changing those settings
    [Fact]
    public void FactoryConstructor_CreatesAResolverWithDefaultSettings()
    {
        var factory = MessagePackFormatterFactory.Default.WithContractless();

        var options = new MessagePackSerializerOptions(factory);

        Assert.True(options.Resolver.HashFloodingResistant);
        Assert.True(options.Resolver.ValidateRequiredMembers);
        Assert.False(options.Resolver.ValidateNullableAnnotations);
        Assert.Equal(500, options.MaxDepth);
        var bytes = MessagePackSerializer.Serialize(new Unannotated { Value = 7 }, options);
        Assert.Equal(7, MessagePackSerializer.Deserialize<Unannotated>(bytes, options).Value);
        Assert.NotSame(options.Resolver, new MessagePackSerializerOptions(factory).Resolver); // one resolver (one cache) per call
    }

    [Fact]
    public void FactoryConstructor_NullFactory_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MessagePackSerializerOptions((MessagePackFormatterFactory)null!));
    }

    // the array constructor is Combine: the first factory that serves a type wins
    [Fact]
    public void FactoriesConstructor_CombinesInOrder()
    {
        var overriding = new ConstantIntFactory();

        var overridden = new MessagePackSerializerOptions([overriding, MessagePackFormatterFactory.Default]);
        var shadowed = new MessagePackSerializerOptions([MessagePackFormatterFactory.Default, overriding]);

        Assert.Equal(new byte[] { 0x2a }, MessagePackSerializer.Serialize(1, overridden)); // 42: the override came first
        Assert.Equal(new byte[] { 0x01 }, MessagePackSerializer.Serialize(1, shadowed));   // Default served int first
        Assert.True(overridden.Resolver.HashFloodingResistant);
    }

    [Fact]
    public void FactoriesConstructor_EmptyOrNull_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new MessagePackSerializerOptions(Array.Empty<MessagePackFormatterFactory>()));
        Assert.Throws<ArgumentNullException>(() => new MessagePackSerializerOptions((MessagePackFormatterFactory[])null!));
    }

    public sealed partial class ConstantIntFactory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
            => type == typeof(int) ? new ConstantIntFormatter<TWriteBuffer, TReadBuffer>() : null;
    }

    public sealed partial class ConstantIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32(42);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = buffer.ReadInt32();
    }
}

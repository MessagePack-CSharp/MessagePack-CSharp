using System.Buffers;
using System.IO.Pipelines;

namespace MessagePack.Tests;

public class NonGenericSerializerTests
{
    // the non-generic path must be byte-identical to the generic path and round-trip
    // through the boxed Deserialize
    static void AssertMatchesGeneric<T>(T value)
    {
        var expectedBytes = MessagePackSerializer.Serialize(value);
        var actualBytes = MessagePackSerializer.Serialize(typeof(T), value);
        Assert.Equal(expectedBytes, actualBytes);

        var roundTripped = MessagePackSerializer.Deserialize(typeof(T), actualBytes);
        Assert.Equal((object?)value, roundTripped);
    }

    [Fact]
    public void RoundTrip_MatchesGenericBytes()
    {
        AssertMatchesGeneric(42);
        AssertMatchesGeneric("hello");
        AssertMatchesGeneric(new[] { 1, 2, 3 });
        AssertMatchesGeneric(new List<string> { "a", "b" });
        AssertMatchesGeneric(new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 });
        AssertMatchesGeneric(Guid.NewGuid());
        AssertMatchesGeneric((int?)5);
        AssertMatchesGeneric((int?)null);
        AssertMatchesGeneric((string?)null);
        AssertMatchesGeneric((List<int>?)null);
    }

    [Fact]
    public void NullValue_TypeFirstBindingEdge()
    {
        // typeof(T) stands where <T> stands (v3's order). The one binding edge: a BARE
        // null second argument prefers the GENERIC entry (T = Type, null = options),
        // which the options null-guard turns into an immediate ArgumentNullException;
        // an object-typed null binds the non-generic entry as intended.
        var bytes = MessagePackSerializer.Serialize(typeof(string), (object?)null);
        Assert.Equal(MessagePackSerializer.Serialize<string?>(null), bytes);

        Assert.Throws<ArgumentNullException>("options", () => MessagePackSerializer.Serialize(typeof(string), null));
    }

    [Fact]
    public void InterfaceDeclaredType_SerializesAsDeclared()
    {
        // declared-type semantics: the runtime Type argument picks the formatter, exactly
        // like the generic parameter does
        IReadOnlyList<int> value = new List<int> { 1, 2 };
        var expected = MessagePackSerializer.Serialize<IReadOnlyList<int>>(value);
        var actual = MessagePackSerializer.Serialize(typeof(IReadOnlyList<int>), value);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BufferWriterOverload_MatchesGenericBytes()
    {
        var value = new List<int> { 10, 20, 30 };
        var expected = MessagePackSerializer.Serialize(value);

        var writer = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(typeof(List<int>), writer, value);
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void SequenceOverload_RoundTrips()
    {
        var bytes = MessagePackSerializer.Serialize(typeof(string), "sequence");
        var sequence = new ReadOnlySequence<byte>(bytes);
        Assert.Equal("sequence", MessagePackSerializer.Deserialize(typeof(string), in sequence));
    }

    [Fact]
    public void OptionsFlowThrough()
    {
        var guid = Guid.NewGuid();
        var optimized = MessagePackSerializer.Serialize(typeof(Guid), guid, MessagePackSerializerOptions.DotNetOptimized);

        Assert.Equal(MessagePackSerializer.Serialize(guid, MessagePackSerializerOptions.DotNetOptimized), optimized);
        Assert.NotEqual(MessagePackSerializer.Serialize(typeof(Guid), guid), optimized);
        Assert.Equal(guid, MessagePackSerializer.Deserialize(typeof(Guid), optimized, MessagePackSerializerOptions.DotNetOptimized));
    }

    [Fact]
    public void ValueNotInstanceOfType_ThrowsBeforeWriting()
    {
        Assert.Throws<ArgumentException>("value", () => MessagePackSerializer.Serialize(typeof(int), "not an int"));

        var writer = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentException>("value", () => MessagePackSerializer.Serialize(typeof(int), writer, "not an int"));
        Assert.Equal(0, writer.WrittenCount);
    }

    [Fact]
    public void NullForNonNullableValueType_Throws()
    {
        Assert.Throws<ArgumentException>("value", () => MessagePackSerializer.Serialize(typeof(int), (object?)null));
    }

    [Fact]
    public void OpenGenericType_Throws()
    {
        Assert.Throws<ArgumentException>("type", () => MessagePackSerializer.Serialize(typeof(List<>), (object?)null));
        Assert.Throws<ArgumentException>("type", () => MessagePackSerializer.Deserialize(typeof(Dictionary<,>), [0xC0]));
    }

    [Fact]
    public void NullType_Throws()
    {
        Assert.Throws<ArgumentNullException>("type", () => MessagePackSerializer.Serialize((Type)null!, (object?)1));
        Assert.Throws<ArgumentNullException>("type", () => MessagePackSerializer.Deserialize((Type)null!, [0xC0]));
    }

    [Fact]
    public void StreamPair_RoundTrips()
    {
        var stream = new MemoryStream();
        MessagePackSerializer.Serialize(typeof(List<int>), stream, new List<int> { 4, 5 });

        stream.Position = 0;
        Assert.Equal(new List<int> { 4, 5 }, MessagePackSerializer.Deserialize(typeof(List<int>), stream));
    }

    [Fact]
    public async Task StreamAsyncPair_RoundTrips()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(typeof(string), stream, "streamed");

        stream.Position = 0;
        Assert.Equal("streamed", await MessagePackSerializer.DeserializeAsync(typeof(string), stream));
    }

    [Fact]
    public async Task AsyncPipePair_RoundTrips()
    {
        var value = new List<int> { 1, 2, 3 };
        var pipe = new Pipe();

        await MessagePackSerializer.SerializeAsync(typeof(List<int>), pipe.Writer, value);
        await pipe.Writer.CompleteAsync();

        var result = await MessagePackSerializer.DeserializeAsync(typeof(List<int>), pipe.Reader);
        Assert.Equal(value, result);
    }

    [Fact]
    public void CompatibilityRouting_IsInherited()
    {
        // the bridge calls the public generic entries, so a downlevel-only formatter
        // graph must reroute over the Compatible buffers exactly as the generic path does
        var resolver = new MessagePackFormatterResolver(
            [new CompatiblePairOnlyFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]);
        var fallbackTypes = new List<Type>();
        resolver.CompatibilityFallback += fallbackTypes.Add;
        var options = new MessagePackSerializerOptions(resolver);

        var payload = MessagePackSerializer.Serialize(typeof(RoutedValue), new RoutedValue { X = 7 }, options);
        var result = (RoutedValue)MessagePackSerializer.Deserialize(typeof(RoutedValue), payload, options)!;

        Assert.Equal(7, result.X);
        Assert.Contains(typeof(RoutedValue), fallbackTypes);
    }
}

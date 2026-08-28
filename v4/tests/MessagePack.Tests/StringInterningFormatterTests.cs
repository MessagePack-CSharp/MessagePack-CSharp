extern alias V3;
using V3::MessagePack.Resolvers;
using SerializerFoundation;
using System.Buffers;
using MessagePack.Formatters;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

public class StringInterningFormatterTests
{
    // fresh factory per options: each test owns its intern set, so reference assertions are deterministic
    static MessagePackSerializerOptions CreateOptions() => new(new MessagePackFormatterResolver(
        [new StringInterningFormatterFactory(), MessagePackFormatterFactory.Default]));

    // the oracle is the v3 formatter this implementation mirrors
    static V3::MessagePack.MessagePackSerializerOptions OracleOptions() =>
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                [new V3::MessagePack.Formatters.StringInterningFormatter()],
                [StandardResolver.Instance]));

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<byte> Split(byte[] data, int chunkSize)
    {
        if (data.Length <= chunkSize)
        {
            return new ReadOnlySequence<byte>(data);
        }
        var first = new Segment(data.AsMemory(0, chunkSize));
        var last = first;
        for (int i = chunkSize; i < data.Length; i += chunkSize)
        {
            last = last.Append(data.AsMemory(i, Math.Min(chunkSize, data.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("こんにちは世界")]
    public void WireBytes_MatchNonInterningAndV3Oracle(string? value)
    {
        var options = CreateOptions();
        var ours = MessagePackSerializer.Serialize(value, options);

        // interning changes nothing on the wire
        Assert.Equal(MessagePackSerializer.Serialize(value), ours);
        Assert.Equal(Oracle.Serialize(value, OracleOptions()), ours);

        Assert.Equal(value, MessagePackSerializer.Deserialize<string?>(ours, options));
        Assert.Equal(value, Oracle.Deserialize<string>(ours, OracleOptions()));
    }

    [Fact]
    public void RepeatedStrings_ShareOneInstance()
    {
        var options = CreateOptions();
        var payload = MessagePackSerializer.Serialize(new[] { "abc", "abc", "xyz" }, options);

        // within one payload
        var array = MessagePackSerializer.Deserialize<string[]>(payload, options)!;
        Assert.Same(array[0], array[1]);
        Assert.NotEqual(array[0], array[2]);

        // and across payloads through the same options
        var again = MessagePackSerializer.Deserialize<string[]>(payload, options)!;
        Assert.Same(array[0], again[0]);
        Assert.Same(array[2], again[2]);
    }

    [Fact]
    public void NilReadsAsNull_EmptyReadsAsTheEmptyInstance()
    {
        var options = CreateOptions();
        Assert.Null(MessagePackSerializer.Deserialize<string?>(new byte[] { 0xc0 }, options));
        Assert.Same(string.Empty, MessagePackSerializer.Deserialize<string?>(new byte[] { 0xa0 }, options));
    }

    [Fact]
    public void LargeString_TakesTheRentedPathAndStillInterns()
    {
        var options = CreateOptions();
        var value = string.Concat(Enumerable.Repeat("0123456789", 100)); // 1000 UTF-8 bytes > 256 stackalloc cap
        var payload = MessagePackSerializer.Serialize(value, options);

        var first = MessagePackSerializer.Deserialize<string?>(payload, options)!;
        var second = MessagePackSerializer.Deserialize<string?>(payload, options)!;
        Assert.Equal(value, first);
        Assert.Same(first, second);
    }

    [Fact]
    public void SplitSequence_StitchesAndSharesTheSetAcrossEntryPoints()
    {
        var options = CreateOptions();
        var value = "straddling string payload こんにちは";
        var payload = MessagePackSerializer.Serialize(value, options);

        var fromSequence = MessagePackSerializer.Deserialize<string?>(Split(payload, 3), options)!;
        Assert.Equal(value, fromSequence);

        // the factory shares one intern set, so the contiguous entry point returns the same instance
        var fromArray = MessagePackSerializer.Deserialize<string?>(payload, options)!;
        Assert.Same(fromSequence, fromArray);
    }

    [Fact]
    public void CallerSuppliedSet_SharesAcrossFactoriesAndAcceptsSeeds()
    {
        var sharedSet = new HashSet<string>();
        var seed = new string("seeded".ToCharArray()); // fresh instance, not the literal
        sharedSet.Add(seed);

        var optionsA = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new StringInterningFormatterFactory(sharedSet), MessagePackFormatterFactory.Default]));
        var optionsB = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new StringInterningFormatterFactory(sharedSet), MessagePackFormatterFactory.Default]));

        // a pre-seeded string comes back as exactly the seeded instance
        var seedPayload = MessagePackSerializer.Serialize("seeded", optionsA);
        Assert.Same(seed, MessagePackSerializer.Deserialize<string?>(seedPayload, optionsA));

        // two factories over one set intern into the same pool
        var payload = MessagePackSerializer.Serialize("crossFactory", optionsA);
        var fromA = MessagePackSerializer.Deserialize<string?>(payload, optionsA)!;
        var fromB = MessagePackSerializer.Deserialize<string?>(payload, optionsB)!;
        Assert.Same(fromA, fromB);
        Assert.Contains("crossFactory", sharedSet);
    }

    sealed class NonAlternateOrdinalComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y);
        public int GetHashCode(string value) => StringComparer.Ordinal.GetHashCode(value);
    }

    [Fact]
    public void ComparerWithoutAlternateLookup_ThrowsAtCompositionTime()
    {
        // this comparer lacks IAlternateEqualityComparer<ReadOnlySpan<char>, string>, which the
        // span lookup requires; fail fast in the ctor rather than mid-deserialization
        var set = new HashSet<string>(new NonAlternateOrdinalComparer());
        Assert.Throws<ArgumentException>(() => new StringInterningFormatterFactory(set));
        Assert.Throws<ArgumentException>(() => new StringInterningFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer>(set));
    }
}

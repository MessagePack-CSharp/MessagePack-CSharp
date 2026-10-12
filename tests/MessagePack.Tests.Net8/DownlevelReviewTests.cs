using System.Collections.Concurrent;
using MessagePack;

namespace MessagePack.Tests.Net8;

// Fixes from the preview1 ship review that only the netstandard2.1 core (what a net8.0 consumer runs) can show.
public class DownlevelReviewTests
{
    // netstandard's ConcurrentDictionary has no Comparer property, so a reused instance on the default comparer could
    // not be detected and HashFloodingResistant was bypassed; the net8 runtime type exposes it for the assertion
    [Fact]
    public void ConcurrentDictionary_PopulatedInstance_GetsTheResistantComparer()
    {
        var bytes = MessagePackSerializer.Serialize(new ConcurrentHolder { Map = new() { [1] = 10, [2] = 20 } });
        var target = new ConcurrentHolder();

        MessagePackSerializer.Deserialize(bytes, ref target);

        Assert.Equal(10, target.Map[1]);
        Assert.Equal(20, target.Map[2]);
        Assert.NotSame(EqualityComparer<long>.Default, target.Map.Comparer);

        var fresh = MessagePackSerializer.Deserialize<ConcurrentHolder>(bytes)!;
        Assert.NotSame(EqualityComparer<long>.Default, fresh.Map.Comparer);
    }

    // the netstandard2.1 build has no formatter for these net9-era BCL types; WithContractless used to claim them as
    // member maps (Half and Int128 have no public members: written as {} and read back as 0, no error)
    [Fact]
    public void Net9OnlyBclTypes_FailLoudly_UnderDefaultAndContractless()
    {
        var contractless = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless()));
        foreach (var options in new[] { MessagePackSerializerOptions.Default, contractless })
        {
            AssertNet9Only(() => MessagePackSerializer.Serialize((Half)1.5f, options));
            AssertNet9Only(() => MessagePackSerializer.Serialize((Int128)42, options));
            AssertNet9Only(() => MessagePackSerializer.Serialize((UInt128)42, options));
            AssertNet9Only(() => MessagePackSerializer.Serialize(new DateOnly(2026, 10, 11), options));
            AssertNet9Only(() => MessagePackSerializer.Serialize(new TimeOnly(1, 2, 3), options));
            AssertNet9Only(() => MessagePackSerializer.Serialize(new System.Text.Rune('a'), options));
        }

        static void AssertNet9Only(Action action)
        {
            var ex = Assert.Throws<InvalidOperationException>(action);
            Assert.Contains("net9.0+ build", ex.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void IndexAndRange_AreServedByTheNetStandard21Build()
    {
        // same wire as the net9 build: from-end one's-complemented
        Assert.Equal(MessagePackSerializer.Serialize(-3), MessagePackSerializer.Serialize(^2));
        Assert.Equal(^2, MessagePackSerializer.Deserialize<Index>(MessagePackSerializer.Serialize(^2)));
        Assert.Equal(1..^1, MessagePackSerializer.Deserialize<Range>(MessagePackSerializer.Serialize(1..^1)));
    }
}

[MessagePackObject]
public class ConcurrentHolder
{
    [Key(0)] public ConcurrentDictionary<long, int> Map { get; set; } = new();
}

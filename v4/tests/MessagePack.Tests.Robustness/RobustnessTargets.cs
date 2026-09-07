using MessagePack;

namespace MessagePack.Tests.Robustness;

// The shared attack surface: every (target shape x resolver tier) deserialize thunk, plus
// the valid-payload seed corpus. Extracted from RobustnessTests so the coverage-guided
// SharpFuzz harness (MessagePack.Tests.Fuzz) drives the exact same surface: the randomized
// property tests and the fuzzer stay in lockstep by construction, not by copy-paste.
// Public because this file is compile-linked into MessagePack.Tests.Fuzz.Target, whose
// consumers live in a different assembly.
public static class RobustnessTargets
{
    // ---- resolver tiers under test ----
    // Default = source-generated formatters (in-box tier)
    static readonly MessagePackSerializerOptions Generated = MessagePackSerializerOptions.Default;

    // reflection tier: SuppressSourceGeneration is per-type, so instead force the whole
    // graph onto reflection by resolving through a contractless-augmented chain whose
    // types are the sample models - the generated formatters still win by registration, so
    // to genuinely hit reflection we use the models declared without generation below.
    // Practically, Contractless exercises the map-by-name reflection path for ANY type.
    static readonly MessagePackSerializerOptions Contractless = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

    static readonly MessagePackSerializerOptions Lenient = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default], validateRequiredMembers: false));

    static readonly MessagePackSerializerOptions Strict = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default], validateNullableAnnotations: true));

    // LZ4 decode is a distinct attack surface: the MessageProcessor.TryDecode path runs
    // before any formatter, so malformed block headers / decompression bombs must be
    // rejected by the ext-envelope parse and the MaxDecompressedSize cap, again only ever
    // as MessagePackSerializationException. A small cap makes the bomb guard reachable
    // within the generated payload sizes.
    static readonly MessagePackSerializerOptions Lz4Block = MessagePackSerializerOptions.Default.WithLz4Block(maxDecompressedSize: 1 << 20);
    static readonly MessagePackSerializerOptions Lz4Array = MessagePackSerializerOptions.Default.WithLz4BlockArray(maxDecompressedSize: 1 << 20);

    // deserialize thunks: one per (target type, tier). Each takes raw bytes and attempts a
    // full deserialize, discarding the result. The caller classifies whatever happens.
    public static readonly (string Name, Action<byte[]> Deserialize)[] Targets = BuildTargets();

    static (string, Action<byte[]>)[] BuildTargets()
    {
        var list = new List<(string, Action<byte[]>)>();
        void Add<T>(string tier, MessagePackSerializerOptions opt)
            => list.Add(($"{typeof(T).Name}/{tier}", bytes => MessagePackSerializer.Deserialize<T>(bytes, opt)));

        foreach (var (tier, opt) in new[] { ("gen", Generated), ("contractless", Contractless), ("lenient", Lenient), ("strict", Strict), ("lz4block", Lz4Block), ("lz4array", Lz4Array) })
        {
            Add<SampleArrayPoco>(tier, opt);
            Add<SampleMapPoco>(tier, opt);
            Add<SampleCtorPoco>(tier, opt);
            Add<SampleUnknownPoco>(tier, opt);
            Add<ISampleShape>(tier, opt);
            Add<SampleTriangle>(tier, opt);
            Add<int[]>(tier, opt);
            Add<int[,]>(tier, opt); // multi-dim: dimension-length x element-count product is attacker-controlled
            Add<int[,,]>(tier, opt);
            Add<List<SampleArrayPoco>>(tier, opt);
            Add<Dictionary<string, SampleMapPoco>>(tier, opt);
            Add<string>(tier, opt);
            Add<object[]>(tier, opt);
        }
        return list.ToArray();
    }

    // A representative valid payload per target, used as the mutation/truncation seed.
    public static byte[][] SeedCorpus()
    {
        var poco = new SampleArrayPoco
        {
            Id = 42,
            Name = "hello",
            Score = 3.14,
            Flag = true,
            Color = SampleColor.Blue,
            When = new DateTime(2026, 8, 30, 1, 2, 3, DateTimeKind.Utc),
            Numbers = [1, 2, 3, -1, int.MaxValue, int.MinValue],
            Tags = ["a", "bb", "ccc"],
            Child = new SampleArrayPoco { Id = 7, Name = "child", Numbers = [] },
            Map = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 },
        };
        var map = new SampleMapPoco { Id = 1, Name = "m", Score = 2.5, Color = SampleColor.Green, Numbers = [4, 5, 6], Child = new SampleMapPoco { Id = 2 } };
        var ctor = new SampleCtorPoco(9, "ctor", SampleColor.Red) { Extra = 100 };
        ISampleShape shape = new SampleTriangle { Base = 1, Height = 2, Inscribed = new SampleCircle { Radius = 0.5 } };

        return
        [
            MessagePackSerializer.Serialize(poco),
            MessagePackSerializer.Serialize(map),
            MessagePackSerializer.Serialize(ctor),
            MessagePackSerializer.Serialize(shape),
            MessagePackSerializer.Serialize<int[]>([1, 2, 3, 4, 5]),
            MessagePackSerializer.Serialize(new List<SampleArrayPoco> { poco, poco }),
            MessagePackSerializer.Serialize<object[]>([1, "two", 3.0, true, null!]),
            // valid LZ4 envelopes, so mutation/truncation exercises the decode path with
            // a real block header to corrupt (not just random bytes that fail the ext parse)
            MessagePackSerializer.Serialize(new List<SampleArrayPoco> { poco, poco, poco, poco }, Lz4Block),
            MessagePackSerializer.Serialize(new List<SampleArrayPoco> { poco, poco, poco, poco }, Lz4Array),
        ];
    }
}

extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// The attributed half of ReflectionObjectFormatter (the Default chain's fallback for
// [MessagePackObject] types the source generator did not cover) vs the source-generated
// formatter for the SAME type (the ceiling) vs MessagePack-CSharp's DynamicObjectResolver
// (v3's IL.Emit attributed path, the incumbent this tier replaces) vs Nerdbank.MessagePack
// (PolyType shape, [Key]-indexed). All four write the identical array-format wire, verified
// in Setup. The question this benchmark answers: what does the un-generated fallback cost
// against the generated formatter, and does rejecting whole-formatter Emit stay competitive
// with v3's emitted formatters and Nerdbank's shape-driven converter?
//
// MEASURED (i7-13700KF, MediumRun 2 launches x 15 iters, single
// BenchPerson{int,string,double}, ns/op, 2026-08-20):
//   Serialize:   Generated 21.1 | Reflection 26.6 (1.26x) | v3 Emit 42.7 (2.03x) | Nerdbank 61.2 (2.91x)
//   Deserialize: Generated 26.6 | Reflection 33.0 (1.24x) | v3 Emit 50.6 (1.90x) | Nerdbank 77.5 (2.92x)
// VERDICT: the fallback pays ~6ns per object over the generated formatter (the delegate +
// interface dispatch per member and the lost fused reservation) and still beats v3's
// emitted attributed path by ~1.5-1.6x both ways on the same wire, so a v3 app whose
// types miss the generator gets faster, not slower, by migrating. Nerdbank's shape-driven
// converter trails the generated formatter ~2.9x on this micro-shape. ShortRun has been
// unreliable for the Reflection candidates specifically: single-launch runs flipped
// 29<->50ns serialize / 41<->65ns deserialize on an earlier machine (per-launch PGO
// devirtualization luck on the slot dispatch); conclusions need the multi-launch medium job.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ReflectionObjectBenchmark
{
    BenchPerson person = default!;
    byte[] payload = default!;
    MessagePack.MessagePackSerializerOptions reflection = default!;
    V3::MessagePack.MessagePackSerializerOptions v3Emit = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbank = new() { SerializeDefaultValues = Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always };

    [GlobalSetup]
    public void Setup()
    {
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };

        // SourceGenerated omitted so the reflection tier claims BenchPerson; the
        // options-less entry points below resolve the GENERATED formatter (module init)
        reflection = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        // v3's runtime IL.Emit path, forced explicitly. Since the rename the mpcs source
        // generator is excluded from this project (its generated code cannot bind to the
        // extern-aliased v3 assembly), so plain StandardResolver would reach the same
        // resolvers; the explicit composite keeps the row's meaning pinned regardless
        v3Emit = V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            V3::MessagePack.Resolvers.CompositeResolver.Create(
                V3::MessagePack.Resolvers.BuiltinResolver.Instance,
                V3::MessagePack.Resolvers.DynamicObjectResolver.Instance));

        payload = MessagePack.MessagePackSerializer.Serialize(person);

        // verify: all three paths must be byte-identical (same array wire) and roundtrip
        if (!MessagePack.MessagePackSerializer.Serialize(person, reflection).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: reflection bytes vs generated");
        if (!V3::MessagePack.MessagePackSerializer.Serialize(person, v3Emit).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 emit bytes vs generated");
        var back = MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(payload, reflection)!;
        if (back.Id != person.Id || back.Name != person.Name || back.Score != person.Score) throw new InvalidOperationException("verify failed: reflection roundtrip");
        var v3Back = V3::MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(payload, v3Emit);
        if (v3Back.Id != person.Id || v3Back.Name != person.Name || v3Back.Score != person.Score) throw new InvalidOperationException("verify failed: v3 emit roundtrip");
        if (!nerdbank.Serialize(person).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: nerdbank bytes vs generated");
        var nbBack = nerdbank.Deserialize<BenchPerson>(payload)!;
        if (nbBack.Id != person.Id || nbBack.Name != person.Name || nbBack.Score != person.Score) throw new InvalidOperationException("verify failed: nerdbank roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeGenerated() => MessagePack.MessagePackSerializer.Serialize(person);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeReflection() => MessagePack.MessagePackSerializer.Serialize(person, reflection);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3Emit() => V3::MessagePack.MessagePackSerializer.Serialize(person, v3Emit);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nerdbank.Serialize(person);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public BenchPerson DeserializeGenerated() => MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public BenchPerson DeserializeReflection() => MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(payload, reflection)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public BenchPerson DeserializeV3Emit() => V3::MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(payload, v3Emit);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public BenchPerson DeserializeNerdbank() => nerdbank.Deserialize<BenchPerson>(payload)!;
}

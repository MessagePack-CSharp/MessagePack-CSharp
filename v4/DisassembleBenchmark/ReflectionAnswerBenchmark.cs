using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using UltraMessagePack;

// ReflectionObjectBenchmark's comparison (Ultra source-generated / Ultra reflection
// fallback / MessagePack-CSharp v3 source generator via StandardResolver / v3
// DynamicObjectResolver IL.Emit / Nerdbank.MessagePack)
// moved onto the realistically nested Answer graph from AnswerBenchmark: Answer ->
// ShallowUser -> BadgeCount, Answer -> List<Comment> -> ShallowUser, byte enums, DateTimes,
// ~1.65KB payload. The single-BenchPerson run prices the per-member dispatch overhead;
// this one asks whether that overhead compounds or washes out when the time goes to
// strings and nested objects. The reflection tier only claims the four object types;
// List/enum/primitive members resolve through the same built-in chain as the generated
// path, and v3's composite gets DynamicEnum/DynamicGeneric for the same reason.
//
// Ultra generated / Ultra reflection / v3 emit write the byte-identical array wire
// (verified in Setup). Nerdbank is allowed to differ in encoding (same policy as
// AnswerBenchmark); every candidate's roundtrip is verified by re-serializing through
// the MessagePack-CSharp oracle and demanding byte identity.
//
// MEASURED (i7-13700KF, MediumRun 2 launches x 15 iters, ~1.65KB payload, ns/op, 2026-08-20,
// 5-way rerun with v3's source-gen resolver added):
//   Serialize:   Generated 381 | Reflection  735 (1.93x) | v3 SourceGen  785 (2.06x)
//                | v3 Emit 1108 (2.91x) | Nerdbank 1176 (3.08x)
//   Deserialize: Generated 833 | Reflection 1069 (1.28x) | v3 SourceGen 1358 (1.63x)
//                | v3 Emit 1530 (1.84x) | Nerdbank 1854 (2.23x)
// VERDICT: the fallback's per-member cost does compound on serialize — the graph carries
// ~126 members per op and the +354ns delta is ~2.8ns/member (the contractless round's
// known dispatch price), so the ratio widens from BenchPerson's 1.26x to 1.93x. It still
// beats v3's emitted path ~1.4-1.5x both ways on the same wire, and even edges v3's
// SOURCE-GENERATED resolver (serialize -6%, deserialize -21%): the un-generated fallback
// is faster than v3's best path, not just its emit path. Deserialize compounds less
// (1.28x, ~1.9ns/member): the read side's time is dominated by string decoding and
// object construction, which both tiers share.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ReflectionAnswerBenchmark
{
    Answer answer = default!;
    byte[] payload = default!;
    byte[] nbPayload = default!;
    UltraMessagePack.MessagePackSerializerOptions reflection = default!;
    MessagePack.MessagePackSerializerOptions v3Emit = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbank = new();

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();

        // SourceGenerated omitted so the reflection tier claims the Answer graph's object
        // types; the options-less entry points resolve the GENERATED formatters (module init)
        reflection = new UltraMessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        // v3's runtime IL.Emit path, forced explicitly (the mpcs source generator also runs
        // on this project); DynamicEnum/DynamicGeneric cover UserType/PostType and List<T>
        v3Emit = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.BuiltinResolver.Instance,
                MessagePack.Resolvers.DynamicEnumResolver.Instance,
                MessagePack.Resolvers.DynamicGenericResolver.Instance,
                MessagePack.Resolvers.DynamicObjectResolver.Instance));

        payload = UltraMessagePack.MessagePackSerializer.Serialize(answer);
        nbPayload = nerdbank.Serialize(answer);

        if (!UltraMessagePack.MessagePackSerializer.Serialize(answer, reflection).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: reflection bytes vs generated");
        if (!MessagePack.MessagePackSerializer.Serialize(answer).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 source-gen bytes vs generated");
        if (!MessagePack.MessagePackSerializer.Serialize(answer, v3Emit).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 emit bytes vs generated");

        VerifyRoundtrip(UltraMessagePack.MessagePackSerializer.Deserialize<Answer>(payload)!, "generated");
        VerifyRoundtrip(UltraMessagePack.MessagePackSerializer.Deserialize<Answer>(payload, reflection)!, "reflection");
        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(payload), "v3 source-gen");
        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, v3Emit), "v3 emit");
        VerifyRoundtrip(nerdbank.Deserialize<Answer>(nbPayload)!, "nerdbank");
    }

    void VerifyRoundtrip(Answer back, string label)
    {
        // field-by-field equality via the oracle: the re-serialized roundtripped object
        // must be byte-identical to the original wire (mpcs standard == Ultra generated)
        var bytes = MessagePack.MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeGenerated() => UltraMessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeReflection() => UltraMessagePack.MessagePackSerializer.Serialize(answer, reflection);

    // v3's own source generator path: the mpc-generated formatters register into
    // StandardResolver, so the default options are the source-gen resolver
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3SourceGen() => MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3Emit() => MessagePack.MessagePackSerializer.Serialize(answer, v3Emit);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nerdbank.Serialize(answer);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Answer DeserializeGenerated() => UltraMessagePack.MessagePackSerializer.Deserialize<Answer>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeReflection() => UltraMessagePack.MessagePackSerializer.Deserialize<Answer>(payload, reflection)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeV3SourceGen() => MessagePack.MessagePackSerializer.Deserialize<Answer>(payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeV3Emit() => MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, v3Emit);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNerdbank() => nerdbank.Deserialize<Answer>(nbPayload)!;
}

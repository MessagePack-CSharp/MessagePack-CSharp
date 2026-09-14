extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// ReflectionObjectBenchmark's comparison (V4 source-generated / V4 reflection
// fallback / MessagePack-CSharp v3 StandardResolver / v3 DynamicObjectResolver
// IL.Emit / Nerdbank.MessagePack)
// NOTE (2026-08-22 rename): the v3 source generator no longer runs in this project (its
// generated code cannot bind to the extern-aliased v3 assembly), so the StandardResolver
// row now measures v3's default DYNAMIC path. The MEASURED numbers below predate that
// and their "v3 SourceGen" rows really are the mpc-generated formatters.
// moved onto the realistically nested Answer graph from AnswerBenchmark: Answer ->
// ShallowUser -> BadgeCount, Answer -> List<Comment> -> ShallowUser, byte enums, DateTimes,
// ~1.65KB payload. The single-BenchPerson run prices the per-member dispatch overhead;
// this one asks whether that overhead compounds or washes out when the time goes to
// strings and nested objects. The reflection tier only claims the four object types;
// List/enum/primitive members resolve through the same built-in chain as the generated
// path, and v3's composite gets DynamicEnum/DynamicGeneric for the same reason.
//
// V4 generated / V4 reflection / v3 emit write the byte-identical array wire
// (verified in Setup). Nerdbank is allowed to differ in encoding (same policy as
// AnswerBenchmark); every candidate's roundtrip is verified by re-serializing through
// the MessagePack-CSharp oracle and demanding byte identity.
//
// MEASURED (i7-13700KF, MediumRun 2 launches x 15 iters, ~1.65KB payload, ns/op, 2026-08-20,
// 6-way rerun adding Nerdbank over PolyType's ReflectionTypeShapeProvider):
//   Serialize:   Generated 420 | Reflection  776 (1.85x) | v3 SourceGen  830 (1.98x)
//                | v3 Emit 1161 (2.77x) | Nerdbank 1211 (2.88x) | Nerdbank reflection shape 1287 (3.06x)
//   Deserialize: Generated 874 | Reflection 1116 (1.28x) | v3 SourceGen 1454 (1.66x)
//                | v3 Emit 1579 (1.81x) | Nerdbank 1933 (2.21x) | Nerdbank reflection shape 2009 (2.30x)
// The reflection shape costs Nerdbank only ~4-6% over [GenerateShape]: the shape drives
// converter CONSTRUCTION, the steady state runs the same converter with DynamicMethod
// accessors instead of source-generated ones. Runtime tier vs runtime tier, V4's
// reflection fallback is ~1.7-1.8x faster than Nerdbank's, and still beats Nerdbank's
// source-gen shape outright.
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
    byte[] nbReflectionPayload = default!;
    MessagePack.MessagePackSerializerOptions reflection = default!;
    V3::MessagePack.MessagePackSerializerOptions v3Emit = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbank = new();
    // Nerdbank's runtime tier: PolyType's reflection shape provider instead of [GenerateShape],
    // the fair opponent for V4's reflection fallback and v3's emit. A separate serializer
    // instance keeps the converter caches per shape source. 1.2.36's byte[] entries are
    // IShapeable-constrained, so the candidates below replicate the library's own byte[]
    // entry steps (writer over a buffer, shape-taking core, ToArray) around the explicit shape.
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbankReflectionShape = new();
    readonly PolyType.ITypeShape<Answer> answerReflectionShape = PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default.GetTypeShape<Answer>();
    readonly System.Buffers.ArrayBufferWriter<byte> reflectionShapeWriter = new();

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();

        // SourceGenerated omitted so the reflection tier claims the Answer graph's object
        // types; the options-less entry points resolve the GENERATED formatters (module init)
        reflection = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        // v3's runtime IL.Emit path, forced explicitly (see the class comment on the v3
        // source generator); DynamicEnum/DynamicGeneric cover UserType/PostType and List<T>
        v3Emit = V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            V3::MessagePack.Resolvers.CompositeResolver.Create(
                V3::MessagePack.Resolvers.BuiltinResolver.Instance,
                V3::MessagePack.Resolvers.DynamicEnumResolver.Instance,
                V3::MessagePack.Resolvers.DynamicGenericResolver.Instance,
                V3::MessagePack.Resolvers.DynamicObjectResolver.Instance));

        payload = MessagePack.MessagePackSerializer.Serialize(answer);
        nbPayload = nerdbank.Serialize(answer);
        nbReflectionPayload = SerializeNerdbankReflectionShape();

        if (!MessagePack.MessagePackSerializer.Serialize(answer, reflection).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: reflection bytes vs generated");
        if (!V3::MessagePack.MessagePackSerializer.Serialize(answer).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 standard bytes vs generated");
        if (!V3::MessagePack.MessagePackSerializer.Serialize(answer, v3Emit).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 emit bytes vs generated");

        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(payload)!, "generated");
        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, reflection)!, "reflection");
        VerifyRoundtrip(V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(payload), "v3 standard");
        VerifyRoundtrip(V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, v3Emit), "v3 emit");
        VerifyRoundtrip(nerdbank.Deserialize<Answer>(nbPayload)!, "nerdbank");
        VerifyRoundtrip(DeserializeNerdbankReflectionShape(), "nerdbank reflection shape");
    }

    void VerifyRoundtrip(Answer back, string label)
    {
        // field-by-field equality via the oracle: the re-serialized roundtripped object
        // must be byte-identical to the original wire (mpcs standard == V4 generated)
        var bytes = V3::MessagePack.MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeGenerated() => MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeReflection() => MessagePack.MessagePackSerializer.Serialize(answer, reflection);

    // v3's default options: StandardResolver, which since the rename resolves through
    // its runtime path here (see the class comment)
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3Standard() => V3::MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3Emit() => V3::MessagePack.MessagePackSerializer.Serialize(answer, v3Emit);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nerdbank.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbankReflectionShape()
    {
        reflectionShapeWriter.ResetWrittenCount();
        var writer = new Nerdbank.MessagePack.MessagePackWriter(reflectionShapeWriter);
        nerdbankReflectionShape.Serialize(ref writer, answer, answerReflectionShape);
        writer.Flush();
        return reflectionShapeWriter.WrittenSpan.ToArray();
    }

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Answer DeserializeGenerated() => MessagePack.MessagePackSerializer.Deserialize<Answer>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeReflection() => MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, reflection)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeV3Standard() => V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeV3Emit() => V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(payload, v3Emit);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNerdbank() => nerdbank.Deserialize<Answer>(nbPayload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNerdbankReflectionShape()
    {
        var reader = new Nerdbank.MessagePack.MessagePackReader(nbReflectionPayload);
        return nerdbankReflectionShape.Deserialize(ref reader, answerReflectionShape)!;
    }
}

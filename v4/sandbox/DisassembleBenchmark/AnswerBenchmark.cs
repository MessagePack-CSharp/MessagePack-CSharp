extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using System.Buffers;

// The Stack Overflow API `Answer` model from MessagePack-CSharp's SerializerBenchmark
// (benchmark/SerializerBenchmark/Models) — the classic "realistically nested" poco:
// Answer -> ShallowUser -> BadgeCount, Answer -> List<Comment> -> Comment -> ShallowUser.
// The original declares every member nullable (int?/bool?/DateTime?/enum?), which pushes
// every primitive through the Nullable resolver path — an atypical shape. This port
// strips Nullable from all value types so primitives take the direct (batched) write
// path like a normal poco; reference members stay `string?` etc. per the BenchPerson
// idiom (the nullable annotation doesn't change their formatter classification).
// Three-way comparison: MessagePack (source-generated formatters) vs
// MessagePack-CSharp (source-generated resolver) vs Nerdbank.MessagePack (PolyType shapes).
//
// Setup cross-checks V4 bytes against the MessagePack-CSharp oracle (must be
// byte-identical) and verifies every library's roundtrip by re-serializing the
// deserialized object with the oracle and comparing bytes. Each library deserializes
// its OWN serialized payload (Nerdbank is allowed to differ in encoding).
//
// MEASURED (i7-13700KF, ShortRun, ~1.65KB payload: 3 comments / 3 users / 4 tags, ns/op):
//   Serialize:   V4  396 | MessagePack-CSharp  894 (2.26x) | Nerdbank 1186 (3.00x)
//   Deserialize: V4  866 | MessagePack-CSharp 1484 (1.71x) | Nerdbank 1904 (2.20x)
// (all-Nullable variant of the same graph measured first: V4 517/968, ratios
//  2.13x/1.97x — de-nulling bought V4 -23% serialize because primitives fold into
//  batched single-reservation writes instead of per-member NullableFormatter hops.)
// Allocated identical (payload-driven, 4.37KB deserialize / 1.65KB serialize; Nerdbank
// serialize 1.73KB — its encoding differs slightly). VERDICT: the generated-formatter
// advantage holds on a realistically nested graph, roughly 2x over MessagePack-CSharp
// on both directions — same shape as the flat BenchPerson result, so the win is not
// an artifact of tiny payloads.
//
// MEASURED round 2 (Ryzen AI 9 HX 470, ShortRun): first 7-way run, INVALIDATED for
// cross-format comparison — CreateAnswer() shared the editor/commenter ShallowUser
// instances, and Orleans' wire protocol dedupes duplicate references (verified: it also
// rehydrates them as SHARED instances), so Orleans was effectively measured on less
// logical payload (wire 1622 B vs msgpack 1658 B, deserialize alloc 3.98 vs 4.37 KB,
// deserialize 1.58x beating mpcs' 1.83x). Kept as the record of why the data was
// de-shared into per-occurrence fresh instances.
//
// MEASURED round 3 (same machine, per-occurrence fresh instances, 7-way): confirmed
// de-sharing moved Orleans from "beats mpcs on deserialize" (1.58x) to just behind it —
// the round-2 edge was partly the back-reference artifact. Superseded by round 4.
//
// MEASURED round 4 (Ryzen AI 9 HX 470, ShortRun, 9-way, de-shared data; wire sizes
// msgpack 1658 / Orleans 1706 / protobuf 1708 (protobuf-net == Google.Protobuf, same
// schema sanity check) / STJ JSON 3674 / Json.NET 3532 — Json.NET is SMALLER than STJ
// because STJ's default HTML-safe escaping expands <,>,& in the body; ns/op, vs V4):
//   Serialize:   V4  521 | mpcs 1092 (2.10x) | Google.Protobuf 1285 (2.47x)
//                | Orleans 1333 (2.56x) | Nerdbank 1499 (2.88x) | protobuf-net 2033 (3.91x)
//                | STJ source-gen 2843 (5.46x) | STJ 3565 (6.85x) | Json.NET 6905 (13.3x)
//   Deserialize: V4  806 | Google.Protobuf 1342 (1.66x) | mpcs 1498 (1.86x)
//                | Orleans 1983 (2.46x) | protobuf-net 2007 (2.49x) | Nerdbank 2013 (2.50x)
//                | STJ 5270 (6.54x) | STJ source-gen 5348 (6.63x) | Json.NET 12441 (15.4x)
// Google.Protobuf caveat: measured on its pre-mapped generated message (the official
// runtime cannot serialize the shared POCO; AnswerProtoMap exists for verification
// only). Its parser is NOT positional — protobuf wire is tag-dispatched (MergeFrom is a
// tag loop + switch, same dispatch class as mpcs' for+switch, which round 9 measured
// equal to straight-line). It beats protobuf-net ~1.5x on the SAME wire bytes and edges
// mpcs via (a) a span-based ParseContext vs mpcs' ReadOnlySequence-based reader paying
// segment bookkeeping per read, (b) zero formatter/resolver indirection (MergeFrom is
// fully static), (c) constructing its own simple message types. Json.NET pays its
// string-first design twice (UTF-16 intermediate: 21.6KB serialize / 15.7KB deserialize
// allocs). Other notes unchanged (protobuf-net MemoryStream shape, STJ source-gen is
// serialize-only, JSON payload 2.1-2.2x msgpack).
//
// MEASURED round 5 (same machine, ShortRun, 11-way): adds V4's DotNetOptimized tier.
// It only reaches this graph through DateTime (8 members: 5 on Answer, 1 per Comment) —
// Guid/decimal/DateTimeOffset/BitArray do not appear — and it only reaches it AT ALL
// because the source generator stopped emitting DateTime as an inlined UnsafeWriteTimestamp
// (see DirectKind): before that, the tier was a silent no-op for generated types.
// Its wire is 3 B/member LARGER here (forced int64 9 B vs timestamp32's 6 B, so 1682 vs
// 1658) — it buys Kind fidelity and a branch-free codec, not size. ns/op, vs V4:
//   Serialize:   V4  380 | V4 DotNetOptimized  382 (1.01x) | mpcs  896 (2.36x)
//                | Google.Protobuf 1257 (3.31x) | Nerdbank 1342 (3.53x) | Orleans 1537 (4.05x)
//                | protobuf-net 2400 (6.32x) | STJ source-gen 2723 (7.17x) | STJ 3702 (9.75x)
//                | Json.NET 7325 (19.3x)
//   Deserialize: V4 DotNetOptimized  785 (0.95x) | V4  824 | Google.Protobuf 1108 (1.34x)
//                | mpcs 1484 (1.80x) | Orleans 1608 (1.95x) | protobuf-net 1952 (2.37x)
//                | Nerdbank 2052 (2.49x) | STJ source-gen 5215 (6.33x) | STJ 5304 (6.44x)
//                | Json.NET 10594 (12.85x)
// VERDICT on the tier: a wash on serialize, ~5% on deserialize. Eight timestamps out of a
// 1.65KB graph is simply not where this payload's time goes (strings are), so DotNetOptimized
// is a correctness/Kind-fidelity choice here, not a speed one. Its speed case has to be made
// on Guid/decimal/BitArray-heavy shapes, which this poco has none of.
//
// MEASURED round 6 (i7-13700KF, ShortRun, 11-way; Nerdbank.MessagePack 1.2.36 -> 1.3.85,
// the "optimize object-as-array serialization" release, same-session A/B): Nerdbank
// serialize 1141 -> 873 ns (-24%), deserialize 1819 -> 1532 ns (-16%), code size ~72KB ->
// ~55KB both directions. Serialize moves it to the head of the non-V4 field (mpcs 937 /
// mpcs source-gen 933 / Google.Protobuf 951 all sit inside the 10% ShortRun tie band);
// deserialize now ties mpcs source-gen (1513) but stays behind Google.Protobuf 962 /
// mpcs 1329 / Orleans 1410. V4's lead narrows to serialize 2.25x, deserialize 1.78x
// (round-5 cross-machine reference was 3.53x / 2.49x). The tiny 2-field poco
// (NerdbankOfficialBenchmark, same A/B) gains far more: serialize 55.9 -> 21.2 ns,
// beating v3 source-gen outright, V4 keeping 1.4x — this string-heavy graph dilutes the
// per-object envelope savings, and strings are where V4's remaining margin lives.
//
// MEASURED round 7 (i7-13700KF, ShortRun, 11-way; model restored to Jil's ORIGINAL
// StackExchange shape — every value-type member nullable (int?/DateTime?/bool?/enum?),
// answer.proto scalars became `optional`, and three data members are now genuinely nil
// (Comment3.upvoted, Editor/Commenter.accept_rate). ns/op vs round 6's non-nullable run:
//   V4 is FLAT both ways (serialize 388 -> 389, deserialize 862 -> 865; the 4.37 -> 4.65KB
//   deserialize alloc is Nullable field layout, not a path change) — the Nullable
//   direct-emit tier keeps nil-or-value inside the same fused reservation, so the
//   original shape costs it nothing. Everyone else pays: mpcs 937 -> 1029 / 1329 -> 1582
//   (+10%/+19%), mpcs source-gen 933 -> 1371 / 1513 -> 2054 (+47%/+36% — per-member
//   NullableFormatter double dispatch), Nerdbank 873 -> 940 / 1532 -> 1689 (+8%/+10%),
//   Orleans +19%/+18%, STJ source-gen serialize +49% (3458, now BEHIND its own
//   reflection path's 2996), Google.Protobuf +4% both (proto presence is a bitfield,
//   not Nullable<T>). Ratios vs V4: mpcs 2.65x/1.83x, Nerdbank 2.42x/1.95x,
//   Google.Protobuf 2.54x/1.16x. The nullable restoration WIDENS V4's lead everywhere.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerBenchmark
{
    Answer answer = default!;
    // v3 source-gen twin (V3GeneratedModels): separate type identity on purpose - v3's
    // generator can only run in a v4-free satellite compilation - materialized from the
    // oracle payload so the data is identical by construction
    V3GeneratedModels.Answer answerV3SourceGen = default!;
    byte[] mpcsPayload = default!;
    byte[] v4Payload = default!;
    byte[] v4DnoPayload = default!;
    byte[] nbPayload = default!;
    byte[] pbPayload = default!;
    byte[] stjPayload = default!;
    byte[] stjSgPayload = default!;
    byte[] orleansPayload = default!;
    byte[] newtonsoftPayload = default!;
    byte[] gpbPayload = default!;
    AnswerProto.Answer protoAnswer = default!; // Google.Protobuf serializes only its generated types (see answer.proto)
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();
    // the .NET-to-.NET tier: only DateTime is re-mapped on this graph (8 members —
    // 5 on Answer, 1 per Comment), Guid/decimal/DateTimeOffset/BitArray do not appear
    static readonly MessagePack.MessagePackSerializerOptions dno =
        MessagePack.MessagePackSerializerOptions.DotNetOptimized;
    // Orleans.Serialization standalone: the Serializer comes out of a minimal DI container
    readonly Orleans.Serialization.Serializer orleans =
        new ServiceCollection().AddSerializer().BuildServiceProvider().GetRequiredService<Orleans.Serialization.Serializer>();

    [GlobalSetup]
    public void Setup()
    {
        answer = CreateAnswer();

        mpcsPayload = V3::MessagePack.MessagePackSerializer.Serialize(answer);
        v4Payload = MessagePack.MessagePackSerializer.Serialize(answer);
        v4DnoPayload = MessagePack.MessagePackSerializer.Serialize(answer, dno);
        nbPayload = nb.Serialize(answer);
        pbPayload = SerializeProtobufNet();
        stjPayload = SerializeSystemTextJson();
        stjSgPayload = SerializeSystemTextJsonSourceGen();
        orleansPayload = SerializeOrleans();
        newtonsoftPayload = SerializeNewtonsoftJson();
        protoAnswer = AnswerProtoMap.ToProto(answer);
        gpbPayload = SerializeGoogleProtobuf();

        // the source-gen twin roundtrips the oracle payload through v3's GENERATED
        // resolver; byte identity proves the generated formatters are live and correct
        answerV3SourceGen = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);
        if (!V3::MessagePack.MessagePackSerializer.Serialize(answerV3SourceGen, V3GeneratedModels.V3SourceGen.Options).AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException("verify failed: MessagePack-CSharp source-gen twin roundtrip");

        if (!v4Payload.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: V4 bytes ({v4Payload.Length}) != MessagePack-CSharp oracle ({mpcsPayload.Length})");
        // the optimized tier is a DIFFERENT wire (DateTime as ToBinary int64, not
        // timestamp ext); identity with the oracle would mean it never took effect
        if (v4DnoPayload.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException("verify failed: DotNetOptimized bytes are identical to the default wire — the tier is not reaching the generated formatter's DateTime members");
        // reflection and source-gen STJ share the default options: same JSON expected
        if (!stjSgPayload.AsSpan().SequenceEqual(stjPayload)) throw new InvalidOperationException("verify failed: System.Text.Json source-gen bytes != reflection bytes");

        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(v4Payload)!, "V4");
        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(v4DnoPayload, dno)!, "V4 DotNetOptimized");
        VerifyRoundtrip(V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(mpcsPayload), "MessagePack-CSharp");
        VerifyRoundtrip(nb.Deserialize<Answer>(new ReadOnlySequence<byte>(nbPayload))!, "Nerdbank");
        VerifyRoundtrip(DeserializeProtobufNet(), "protobuf-net");
        VerifyRoundtrip(DeserializeSystemTextJson(), "System.Text.Json");
        VerifyRoundtrip(DeserializeSystemTextJsonSourceGen(), "System.Text.Json source-gen");
        VerifyRoundtrip(DeserializeOrleans(), "Orleans");
        VerifyRoundtrip(DeserializeNewtonsoftJson(), "Newtonsoft.Json");
        VerifyRoundtrip(AnswerProtoMap.ToPoco(DeserializeGoogleProtobuf()), "Google.Protobuf");
    }

    void VerifyRoundtrip(Answer back, string label)
    {
        // field-by-field equality via the oracle: re-serialize the roundtripped object
        // with MessagePack-CSharp and demand byte identity with the original payload
        var bytes = V3::MessagePack.MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV4DotNetOptimized() => MessagePack.MessagePackSerializer.Serialize(answer, dno);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcsSourceGen() => V3::MessagePack.MessagePackSerializer.Serialize(answerV3SourceGen, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nb.Serialize(answer);

    // idiomatic protobuf-net byte[] shape: stream in, ToArray out
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeProtobufNet()
    {
        var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, answer);
        return stream.ToArray();
    }

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeSystemTextJson() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeSystemTextJsonSourceGen() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(answer, AnswerJsonContext.Default.Answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeOrleans() => orleans.SerializeToArray(answer);

    // Json.NET is string-first; the UTF-8 conversion is the honest cost of getting bytes
    // out of it (same convention as MessagePack-CSharp's SerializerBenchmark)
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNewtonsoftJson() => System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(answer));

    // official protobuf runtime: measured on its pre-mapped generated message — the
    // shared-POCO principle does not apply to this entry (see answer.proto)
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeGoogleProtobuf() => Google.Protobuf.MessageExtensions.ToByteArray(protoAnswer);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Answer DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<Answer>(v4Payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeV4DotNetOptimized() => MessagePack.MessagePackSerializer.Deserialize<Answer>(v4DnoPayload, dno)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(mpcsPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public V3GeneratedModels.Answer DeserializeMpcsSourceGen() => V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNerdbank() => nb.Deserialize<Answer>(nbPayload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeProtobufNet() => ProtoBuf.Serializer.Deserialize<Answer>((ReadOnlySpan<byte>)pbPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeSystemTextJson() => System.Text.Json.JsonSerializer.Deserialize<Answer>(stjPayload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeSystemTextJsonSourceGen() => System.Text.Json.JsonSerializer.Deserialize(stjSgPayload, AnswerJsonContext.Default.Answer)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeOrleans() => orleans.Deserialize<Answer>(orleansPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNewtonsoftJson() => Newtonsoft.Json.JsonConvert.DeserializeObject<Answer>(System.Text.Encoding.UTF8.GetString(newtonsoftPayload))!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerProto.Answer DeserializeGoogleProtobuf() => AnswerProto.Answer.Parser.ParseFrom(gpbPayload);

    internal static Answer CreateAnswer()
    {
        // every occurrence is a FRESH instance: reference-tracking wire formats (Orleans)
        // would otherwise write shared subobjects as back-references and rehydrate them as
        // shared instances, quietly winning wire size and deserialize allocations against
        // copy-semantics formats (msgpack/protobuf/JSON) on data they did not really beat
        static ShallowUser Owner() => new()
        {
            user_id = 22656,
            display_name = "Jon Skeet",
            reputation = 1_400_000,
            user_type = UserType.registered,
            profile_image = "https://i.sstatic.net/8kEbo.jpg?s=256",
            link = "https://stackoverflow.com/users/22656/jon-skeet",
            accept_rate = 86,
            badge_counts = new BadgeCount { gold = 880, silver = 9211, bronze = 9401 },
        };
        static ShallowUser Editor() => new()
        {
            user_id = 23354,
            display_name = "Marc Gravell",
            reputation = 1_000_000,
            user_type = UserType.moderator,
            link = "https://stackoverflow.com/users/23354/marc-gravell",
            badge_counts = new BadgeCount { gold = 426, silver = 3568, bronze = 4826 },
        };
        static ShallowUser Commenter() => new()
        {
            user_id = 1_144_035,
            display_name = "neuecc",
            reputation = 5000,
            user_type = UserType.registered,
            badge_counts = new BadgeCount { gold = 3, silver = 17, bronze = 34 },
        };



        return new Answer
        {
            question_id = 2_366_718,
            answer_id = 2_366_744,
            // DateTime values are real UTC on purpose: Kind.Unspecified would be
            // normalized differently by the three timestamp mappings
            locked_date = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            community_owned_date = new DateTime(2015, 8, 1, 12, 0, 0, DateTimeKind.Utc),
            creation_date = new DateTime(2010, 3, 2, 15, 26, 41, DateTimeKind.Utc),
            last_edit_date = new DateTime(2019, 6, 3, 7, 12, 8, DateTimeKind.Utc),
            last_activity_date = new DateTime(2019, 6, 3, 7, 12, 8, DateTimeKind.Utc),
            score = 5842,
            is_accepted = true,
            accepted = true,
            body = "<p>Strings are immutable in C#. When you use the += operator on a string inside a loop, a new string instance is allocated for every concatenation, copying the entire accumulated contents each time — the classic Schlemiel the Painter's algorithm, O(n²) overall.</p><p>Use <code>StringBuilder</code> instead: it maintains a growable buffer so appends are amortized O(1), and you pay for a single <code>ToString()</code> at the end.</p>",
            body_markdown = "Strings are **immutable** in C#. When you use the `+=` operator on a string inside a loop, a new string instance is allocated for every concatenation... Use `StringBuilder` instead.",
            title = "Why is string concatenation in a loop slow?",
            link = "https://stackoverflow.com/a/2366744",
            share_link = "https://stackoverflow.com/a/2366744/22656",
            up_vote_count = 5920,
            down_vote_count = 78,
            upvoted = true,
            downvoted = false,
            owner = Owner(),
            last_editor = Editor(),
            comment_count = 3,
            comments =
            [
                new Comment
                {
                    comment_id = 2_401_811,
                    post_id = 2_366_744,
                    creation_date = new DateTime(2010, 3, 2, 16, 2, 33, DateTimeKind.Utc),
                    post_type = PostType.answer,
                    score = 312,
                    edited = false,
                    body = "Worth noting that the compiler folds compile-time constant concatenations, so this only bites for runtime values.",
                    owner = Commenter(),
                    upvoted = false,
                },
                new Comment
                {
                    comment_id = 2_402_690,
                    post_id = 2_366_744,
                    creation_date = new DateTime(2010, 3, 2, 17, 44, 5, DateTimeKind.Utc),
                    post_type = PostType.answer,
                    score = 45,
                    edited = true,
                    body = "And for joining a known collection, string.Join (or string.Concat) beats a manual StringBuilder loop.",
                    body_markdown = "And for joining a known collection, `string.Join` (or `string.Concat`) beats a manual StringBuilder loop.",
                    owner = Editor(),
                    reply_to_user = Commenter(),
                    upvoted = true,
                },
                new Comment
                {
                    comment_id = 55_412_907,
                    post_id = 2_366_744,
                    creation_date = new DateTime(2019, 6, 3, 7, 15, 30, DateTimeKind.Utc),
                    post_type = PostType.answer,
                    score = 8,
                    edited = false,
                    body = "Does span-based interpolation in modern C# change this answer?",
                    owner = Commenter(),
                },
            ],
            tags = ["c#", ".net", "string", "performance"],
        };
    }
}

#pragma warning disable IDE1006 // naming matches the Stack Overflow API wire format

// Model mirrors Jil's benchmark models (Benchmark/Models in kevin-montrose/Jil, the
// StackExchange API shapes): every value-type member is nullable, as in the original —
// restored 2026-08-27 after a period as plain value types.
// ProtoMember = Key + 1 (proto field numbers are 1-based). CompatibilityLevel240 makes
// DateTime members use google.protobuf.Timestamp: the default (Level200) bcl format
// drops DateTimeKind on deserialize, which would shift the value when the roundtrip
// verification re-serializes through the msgpack oracle (ToUniversalTime on an
// Unspecified value assumes local time); Timestamp reads back as Kind.Utc.
[V3::MessagePack.MessagePackObject]
[PolyType.GenerateShape]
[ProtoBuf.ProtoContract]
[Orleans.GenerateSerializer]
[ProtoBuf.CompatibilityLevel(ProtoBuf.CompatibilityLevel.Level240)]
public partial class Answer
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0), ProtoBuf.ProtoMember(1), Orleans.Id(0)]
    public int? question_id { get; set; }
    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1), ProtoBuf.ProtoMember(2), Orleans.Id(1)]
    public int? answer_id { get; set; }
    [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2), ProtoBuf.ProtoMember(3), Orleans.Id(2)]
    public DateTime? locked_date { get; set; }
    [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3), ProtoBuf.ProtoMember(4), Orleans.Id(3)]
    public DateTime? creation_date { get; set; }
    [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4), ProtoBuf.ProtoMember(5), Orleans.Id(4)]
    public DateTime? last_edit_date { get; set; }
    [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5), ProtoBuf.ProtoMember(6), Orleans.Id(5)]
    public DateTime? last_activity_date { get; set; }
    [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6), ProtoBuf.ProtoMember(7), Orleans.Id(6)]
    public int? score { get; set; }
    [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7), ProtoBuf.ProtoMember(8), Orleans.Id(7)]
    public DateTime? community_owned_date { get; set; }
    [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8), ProtoBuf.ProtoMember(9), Orleans.Id(8)]
    public bool? is_accepted { get; set; }
    [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9), ProtoBuf.ProtoMember(10), Orleans.Id(9)]
    public string? body { get; set; }
    [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10), ProtoBuf.ProtoMember(11), Orleans.Id(10)]
    public ShallowUser? owner { get; set; }
    [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11), ProtoBuf.ProtoMember(12), Orleans.Id(11)]
    public string? title { get; set; }
    [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12), ProtoBuf.ProtoMember(13), Orleans.Id(12)]
    public int? up_vote_count { get; set; }
    [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13), ProtoBuf.ProtoMember(14), Orleans.Id(13)]
    public int? down_vote_count { get; set; }
    [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14), ProtoBuf.ProtoMember(15), Orleans.Id(14)]
    public List<Comment>? comments { get; set; }
    [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15), ProtoBuf.ProtoMember(16), Orleans.Id(15)]
    public string? link { get; set; }
    [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16), ProtoBuf.ProtoMember(17), Orleans.Id(16)]
    public List<string>? tags { get; set; }
    [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17), ProtoBuf.ProtoMember(18), Orleans.Id(17)]
    public bool? upvoted { get; set; }
    [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18), ProtoBuf.ProtoMember(19), Orleans.Id(18)]
    public bool? downvoted { get; set; }
    [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19), ProtoBuf.ProtoMember(20), Orleans.Id(19)]
    public bool? accepted { get; set; }
    [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20), ProtoBuf.ProtoMember(21), Orleans.Id(20)]
    public ShallowUser? last_editor { get; set; }
    [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21), ProtoBuf.ProtoMember(22), Orleans.Id(21)]
    public int? comment_count { get; set; }
    [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22), ProtoBuf.ProtoMember(23), Orleans.Id(22)]
    public string? body_markdown { get; set; }
    [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23), ProtoBuf.ProtoMember(24), Orleans.Id(23)]
    public string? share_link { get; set; }
}

[V3::MessagePack.MessagePackObject]
[ProtoBuf.ProtoContract]
[Orleans.GenerateSerializer]
[ProtoBuf.CompatibilityLevel(ProtoBuf.CompatibilityLevel.Level240)]
public partial class Comment
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0), ProtoBuf.ProtoMember(1), Orleans.Id(0)]
    public int? comment_id { get; set; }
    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1), ProtoBuf.ProtoMember(2), Orleans.Id(1)]
    public int? post_id { get; set; }
    [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2), ProtoBuf.ProtoMember(3), Orleans.Id(2)]
    public DateTime? creation_date { get; set; }
    [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3), ProtoBuf.ProtoMember(4), Orleans.Id(3)]
    public PostType? post_type { get; set; }
    [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4), ProtoBuf.ProtoMember(5), Orleans.Id(4)]
    public int? score { get; set; }
    [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5), ProtoBuf.ProtoMember(6), Orleans.Id(5)]
    public bool? edited { get; set; }
    [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6), ProtoBuf.ProtoMember(7), Orleans.Id(6)]
    public string? body { get; set; }
    [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7), ProtoBuf.ProtoMember(8), Orleans.Id(7)]
    public ShallowUser? owner { get; set; }
    [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8), ProtoBuf.ProtoMember(9), Orleans.Id(8)]
    public ShallowUser? reply_to_user { get; set; }
    [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9), ProtoBuf.ProtoMember(10), Orleans.Id(9)]
    public string? link { get; set; }
    [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10), ProtoBuf.ProtoMember(11), Orleans.Id(10)]
    public string? body_markdown { get; set; }
    [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11), ProtoBuf.ProtoMember(12), Orleans.Id(11)]
    public bool? upvoted { get; set; }
}

[V3::MessagePack.MessagePackObject]
[ProtoBuf.ProtoContract]
[Orleans.GenerateSerializer]
public partial class ShallowUser
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0), ProtoBuf.ProtoMember(1), Orleans.Id(0)]
    public int? user_id { get; set; }
    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1), ProtoBuf.ProtoMember(2), Orleans.Id(1)]
    public string? display_name { get; set; }
    [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2), ProtoBuf.ProtoMember(3), Orleans.Id(2)]
    public int? reputation { get; set; }
    [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3), ProtoBuf.ProtoMember(4), Orleans.Id(3)]
    public UserType? user_type { get; set; }
    [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4), ProtoBuf.ProtoMember(5), Orleans.Id(4)]
    public string? profile_image { get; set; }
    [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5), ProtoBuf.ProtoMember(6), Orleans.Id(5)]
    public string? link { get; set; }
    [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6), ProtoBuf.ProtoMember(7), Orleans.Id(6)]
    public int? accept_rate { get; set; }
    [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7), ProtoBuf.ProtoMember(8), Orleans.Id(7)]
    public BadgeCount? badge_counts { get; set; }
}

[V3::MessagePack.MessagePackObject]
[ProtoBuf.ProtoContract]
[Orleans.GenerateSerializer]
public partial class BadgeCount
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0), ProtoBuf.ProtoMember(1), Orleans.Id(0)]
    public int? gold { get; set; }
    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1), ProtoBuf.ProtoMember(2), Orleans.Id(1)]
    public int? silver { get; set; }
    [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2), ProtoBuf.ProtoMember(3), Orleans.Id(2)]
    public int? bronze { get; set; }
}

public enum UserType : byte
{
    unregistered = 2,
    registered = 3,
    moderator = 4,
    does_not_exist = 255,
}

public enum PostType : byte
{
    question = 1,
    answer = 2,
}

// System.Text.Json source generator context; default options to match the reflection
// path exactly (Setup asserts byte identity between the two)
[System.Text.Json.Serialization.JsonSerializable(typeof(Answer))]
public partial class AnswerJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

// POCO <-> protoc-generated message mapping for the Google.Protobuf entry: the official
// runtime serializes only its generated types, so the benchmark measures the pre-mapped
// message and this mapper exists solely for the Setup roundtrip verification.
// `optional string` carries presence (HasX), so null strings survive; DateTime maps via
// google.protobuf.Timestamp (Kind.Utc both ways); enums map by numeric cast.
static class AnswerProtoMap
{
    static Google.Protobuf.WellKnownTypes.Timestamp? Ts(DateTime? value) => value is { } v ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(v) : null;

    static AnswerProto.BadgeCount? ToProto(BadgeCount? b)
    {
        if (b == null) return null;
        var p = new AnswerProto.BadgeCount();
        if (b.gold is { } gold) p.Gold = gold;
        if (b.silver is { } silver) p.Silver = silver;
        if (b.bronze is { } bronze) p.Bronze = bronze;
        return p;
    }

    static BadgeCount? ToPoco(AnswerProto.BadgeCount? p)
    {
        if (p == null) return null;
        return new BadgeCount
        {
            gold = p.HasGold ? p.Gold : null,
            silver = p.HasSilver ? p.Silver : null,
            bronze = p.HasBronze ? p.Bronze : null,
        };
    }

    static AnswerProto.ShallowUser? ToProto(ShallowUser? u)
    {
        if (u == null) return null;
        var p = new AnswerProto.ShallowUser { BadgeCounts = ToProto(u.badge_counts) };
        if (u.user_id is { } userId) p.UserId = userId;
        if (u.display_name != null) p.DisplayName = u.display_name;
        if (u.reputation is { } reputation) p.Reputation = reputation;
        if (u.user_type is { } userType) p.UserType = (AnswerProto.UserType)(int)userType;
        if (u.profile_image != null) p.ProfileImage = u.profile_image;
        if (u.link != null) p.Link = u.link;
        if (u.accept_rate is { } acceptRate) p.AcceptRate = acceptRate;
        return p;
    }

    static ShallowUser? ToPoco(AnswerProto.ShallowUser? p)
    {
        if (p == null) return null;
        return new ShallowUser
        {
            user_id = p.HasUserId ? p.UserId : null,
            display_name = p.HasDisplayName ? p.DisplayName : null,
            reputation = p.HasReputation ? p.Reputation : null,
            user_type = p.HasUserType ? (UserType)(int)p.UserType : null,
            profile_image = p.HasProfileImage ? p.ProfileImage : null,
            link = p.HasLink ? p.Link : null,
            accept_rate = p.HasAcceptRate ? p.AcceptRate : null,
            badge_counts = ToPoco(p.BadgeCounts),
        };
    }

    public static AnswerProto.Answer ToProto(Answer a)
    {
        var p = new AnswerProto.Answer
        {
            LockedDate = Ts(a.locked_date),
            CreationDate = Ts(a.creation_date),
            LastEditDate = Ts(a.last_edit_date),
            LastActivityDate = Ts(a.last_activity_date),
            CommunityOwnedDate = Ts(a.community_owned_date),
            Owner = ToProto(a.owner),
            LastEditor = ToProto(a.last_editor),
        };
        if (a.question_id is { } questionId) p.QuestionId = questionId;
        if (a.answer_id is { } answerId) p.AnswerId = answerId;
        if (a.score is { } score) p.Score = score;
        if (a.is_accepted is { } isAccepted) p.IsAccepted = isAccepted;
        if (a.up_vote_count is { } upVoteCount) p.UpVoteCount = upVoteCount;
        if (a.down_vote_count is { } downVoteCount) p.DownVoteCount = downVoteCount;
        if (a.upvoted is { } upvoted) p.Upvoted = upvoted;
        if (a.downvoted is { } downvoted) p.Downvoted = downvoted;
        if (a.accepted is { } accepted) p.Accepted = accepted;
        if (a.comment_count is { } commentCount) p.CommentCount = commentCount;
        if (a.body != null) p.Body = a.body;
        if (a.title != null) p.Title = a.title;
        if (a.link != null) p.Link = a.link;
        if (a.body_markdown != null) p.BodyMarkdown = a.body_markdown;
        if (a.share_link != null) p.ShareLink = a.share_link;
        if (a.tags != null) p.Tags.AddRange(a.tags);
        if (a.comments != null)
        {
            foreach (var c in a.comments)
            {
                var pc = new AnswerProto.Comment
                {
                    CreationDate = Ts(c.creation_date),
                    Owner = ToProto(c.owner),
                    ReplyToUser = ToProto(c.reply_to_user),
                };
                if (c.comment_id is { } commentId) pc.CommentId = commentId;
                if (c.post_id is { } postId) pc.PostId = postId;
                if (c.post_type is { } postType) pc.PostType = (AnswerProto.PostType)(int)postType;
                if (c.score is { } commentScore) pc.Score = commentScore;
                if (c.edited is { } edited) pc.Edited = edited;
                if (c.upvoted is { } commentUpvoted) pc.Upvoted = commentUpvoted;
                if (c.body != null) pc.Body = c.body;
                if (c.link != null) pc.Link = c.link;
                if (c.body_markdown != null) pc.BodyMarkdown = c.body_markdown;
                p.Comments.Add(pc);
            }
        }
        return p;
    }

    public static Answer ToPoco(AnswerProto.Answer p)
    {
        return new Answer
        {
            question_id = p.HasQuestionId ? p.QuestionId : null,
            answer_id = p.HasAnswerId ? p.AnswerId : null,
            locked_date = p.LockedDate?.ToDateTime(),
            creation_date = p.CreationDate?.ToDateTime(),
            last_edit_date = p.LastEditDate?.ToDateTime(),
            last_activity_date = p.LastActivityDate?.ToDateTime(),
            score = p.HasScore ? p.Score : null,
            community_owned_date = p.CommunityOwnedDate?.ToDateTime(),
            is_accepted = p.HasIsAccepted ? p.IsAccepted : null,
            body = p.HasBody ? p.Body : null,
            owner = ToPoco(p.Owner),
            title = p.HasTitle ? p.Title : null,
            up_vote_count = p.HasUpVoteCount ? p.UpVoteCount : null,
            down_vote_count = p.HasDownVoteCount ? p.DownVoteCount : null,
            comments = p.Comments.Count == 0 ? null : p.Comments.Select(c => new Comment
            {
                comment_id = c.HasCommentId ? c.CommentId : null,
                post_id = c.HasPostId ? c.PostId : null,
                creation_date = c.CreationDate?.ToDateTime(),
                post_type = c.HasPostType ? (PostType)(int)c.PostType : null,
                score = c.HasScore ? c.Score : null,
                edited = c.HasEdited ? c.Edited : null,
                body = c.HasBody ? c.Body : null,
                owner = ToPoco(c.Owner),
                reply_to_user = ToPoco(c.ReplyToUser),
                link = c.HasLink ? c.Link : null,
                body_markdown = c.HasBodyMarkdown ? c.BodyMarkdown : null,
                upvoted = c.HasUpvoted ? c.Upvoted : null,
            }).ToList(),
            link = p.HasLink ? p.Link : null,
            tags = p.Tags.Count == 0 ? null : p.Tags.ToList(),
            upvoted = p.HasUpvoted ? p.Upvoted : null,
            downvoted = p.HasDownvoted ? p.Downvoted : null,
            accepted = p.HasAccepted ? p.Accepted : null,
            last_editor = ToPoco(p.LastEditor),
            comment_count = p.HasCommentCount ? p.CommentCount : null,
            body_markdown = p.HasBodyMarkdown ? p.BodyMarkdown : null,
            share_link = p.HasShareLink ? p.ShareLink : null,
        };
    }
}

#pragma warning restore IDE1006

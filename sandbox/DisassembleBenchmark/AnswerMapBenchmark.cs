extern alias V3;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// The Answer graph in MAP wire form (property-name keys): v3/v4 through
// [MessagePackObject(keyAsPropertyName: true)] twins, Nerdbank through PolyType shapes
// with no [Key] attributes (its property-name map mode). This is where v4's string-key
// automata and the pre-encoded key blobs earn their keep on a realistic payload; the
// JSON serializers are inherently map-shaped and already measured in AnswerBenchmark.
// protobuf/Orleans have no map mode and are out of scope here.
//
// The map twins are bridged from AnswerBenchmark.CreateAnswer() through System.Text.Json
// (attribute-neutral, deterministic: identical property names, UTC DateTimes survive via
// ISO 'Z', enums as numbers) instead of duplicating the data literals.
//
// Nerdbank runs with SerializeDefaultValues = Always PINNED explicitly. Always is also
// the library default (verified in SerializerConfiguration), so this only guards the
// comparison against a future default change: every row must emit the same 24-key map,
// and non-Always policies additionally disqualify Nerdbank's dense writer table
// (ObjectArrayConverter builds denseWriters only under Always).
// MEASURED round 1 (i7-13700KF, ShortRun, Nerdbank 1.3.85, nullable Jil-shape graph;
// map payload 2.84KB vs the array form's 1.65KB). ns/op vs V4, with the array-form
// (AnswerBenchmark round 7) delta in parens:
//   Serialize:   V4 477 (+23% over its array 389) | mpcs 1383 (2.90x, +34%)
//                | mpcs source-gen 1589 (3.33x) | Nerdbank 1689 (3.54x, +80%)
//   Deserialize: V4 1140 (+32% over its array 865) | mpcs 2647 (2.32x, +67%)
//                | mpcs source-gen 2903 (2.55x) | Nerdbank 3355 (2.94x, +99%)
// V4's map tax is the smallest by far (pre-encoded key blobs fused into the write
// reservations; length-first automata on read), so map is where its lead peaks.
// Nerdbank flips from best-of-the-rest (array) to last (map): the 1.3.84
// object-as-array optimization does not cover its map mode, whose cost roughly
// DOUBLES over array on both directions. v3 dynamic beats v3 source-gen on maps.
//
// MEASURED round 2 (Ryzen AI 9 HX 470, ShortRun, 5-way): adds ShapeShift.MsgPack
// 0.1.1049-alpha on its DEFAULT contract, which is exactly this map wire (Setup asserts
// map16(24) at the root; wire 3025 B = the 2877 B norm + the same 148 B value-encoding
// delta as its array form: timestamp96 and enum-as-string). ns/op, vs V4:
//   Serialize:   V4 652 | mpcs 1815 (2.8x) | mpcs source-gen 1955 (3.0x)
//                | Nerdbank 2093 (3.2x) | ShapeShift map 6455 (9.9x)
//   Deserialize: V4 1430 | mpcs 3515 (2.5x) | mpcs source-gen 3837 (2.7x)
//                | Nerdbank 4659 (3.3x) | ShapeShift map 10928 (7.6x)
// ShapeShift's map mode costs ~2.6-2.8x its OWN array contract (round 8 of
// AnswerBenchmark: 2342/4173), a far steeper map tax than anyone else's, and its
// allocation balloons to 15.13 KB serialize / 28.47 KB deserialize (the other rows are
// payload-only 2.84 KB / graph-only 4.65 KB), so the property-name path is allocating
// per-key scratch on top of the per-value indirection already seen in array form.
//
// MEASURED round 3 (i7-13700KF, ShortRun, 5-way, ShapeShift 0.1.1068-alpha; different
// machine from round 2, compare ratios). ns/op, vs V4:
//   Serialize:   V4 480 | mpcs 1379 (2.9x) | mpcs source-gen 1525 (3.2x)
//                | Nerdbank 1632 (3.4x) | ShapeShift map 2397 (5.0x)
//   Deserialize: V4 1136 | mpcs 2644 (2.3x) | mpcs source-gen 3040 (2.7x)
//                | Nerdbank 3442 (3.0x) | ShapeShift map 6612 (5.8x)
// The 1068 allocation work reached map mode too: serialize 15.13 -> 3.09 KB (payload is
// 3.03 KB, so the per-key scratch is gone), deserialize 28.47 -> 14.08 KB. The map tax
// over its own array contract shrank from ~2.7x to 1.5x serialize / 1.9x deserialize
// (AnswerBenchmark round 9: 1560/3424), and the ratio to V4 from 9.9x/7.6x to 5.0x/5.8x.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerMapBenchmark
{
    AnswerMap answerMap = default!;
    V3GeneratedModels.AnswerMap answerMapV3SourceGen = default!;
    byte[] mpcsPayload = default!;
    byte[] v4Payload = default!;
    byte[] nbPayload = default!;
    byte[] ssPayload = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new() { SerializeDefaultValues = Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always };
    // ShapeShift's DEFAULT contract is exactly this map wire (property-name keys), so the
    // map twins need no ShapeShift attributes. Always is its library default too, pinned
    // for the same reason as the Nerdbank row: every row must emit the same 24-key map.
    readonly ShapeShift.MsgPack.MsgPackSerializer ssMsgPack = new() { SerializeDefaultValues = ShapeShift.SerializeDefaultValuesPolicy.Always };

    [GlobalSetup]
    public void Setup()
    {
        var answer = AnswerBenchmark.CreateAnswer();
        answerMap = System.Text.Json.JsonSerializer.Deserialize<AnswerMap>(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(answer))!;

        mpcsPayload = V3::MessagePack.MessagePackSerializer.Serialize(answerMap);
        v4Payload = MessagePack.MessagePackSerializer.Serialize(answerMap);
        nbPayload = nb.Serialize(answerMap);
        ssPayload = ssMsgPack.Serialize(answerMap);
        // the map-form claim must hold: map16(24) at the root, not fixmap or array
        if (ssPayload is not [0xde, 0x00, 0x18, ..]) throw new InvalidOperationException("verify failed: ShapeShift default contract did not produce map16(24)");

        // the map bridge itself must not have lost data: the map twin re-keyed through
        // the ARRAY oracle must reproduce the array payload exactly
        var arrayOracle = V3::MessagePack.MessagePackSerializer.Serialize(answer);
        var backToArray = System.Text.Json.JsonSerializer.Deserialize<Answer>(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(answerMap))!;
        if (!V3::MessagePack.MessagePackSerializer.Serialize(backToArray).AsSpan().SequenceEqual(arrayOracle)) throw new InvalidOperationException("verify failed: STJ bridge lost data");

        if (!v4Payload.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: V4 map bytes ({v4Payload.Length}) != MessagePack-CSharp oracle ({mpcsPayload.Length})");

        answerMapV3SourceGen = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.AnswerMap>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);
        if (!V3::MessagePack.MessagePackSerializer.Serialize(answerMapV3SourceGen, V3GeneratedModels.V3SourceGen.Options).AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException("verify failed: MessagePack-CSharp source-gen map twin roundtrip");

        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(v4Payload)!, "V4");
        VerifyRoundtrip(V3::MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(mpcsPayload), "MessagePack-CSharp");
        VerifyRoundtrip(nb.Deserialize<AnswerMap>(new ReadOnlySequence<byte>(nbPayload))!, "Nerdbank");
        VerifyRoundtrip(DeserializeShapeShiftMsgPackMap(), "ShapeShift.MsgPack map");
    }

    void VerifyRoundtrip(AnswerMap back, string label)
    {
        var bytes = V3::MessagePack.MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: {label} map roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(answerMap);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(answerMap);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcsSourceGen() => V3::MessagePack.MessagePackSerializer.Serialize(answerMapV3SourceGen, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nb.Serialize(answerMap);

    // "Map" in the name states the wire form explicitly (the array-contract counterpart
    // lives in AnswerBenchmark as SerializeShapeShiftMsgPackArray)
    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeShapeShiftMsgPackMap() => ssMsgPack.Serialize(answerMap);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public AnswerMap DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(v4Payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerMap DeserializeMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(mpcsPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public V3GeneratedModels.AnswerMap DeserializeMpcsSourceGen() => V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.AnswerMap>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerMap DeserializeNerdbank() => nb.Deserialize<AnswerMap>(nbPayload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerMap DeserializeShapeShiftMsgPackMap() => ssMsgPack.Deserialize<AnswerMap>(ssPayload)!;
}

#pragma warning disable IDE1006 // naming matches the Stack Overflow API wire format

// map twins of the Answer graph: same members (all value types nullable, the Jil
// original shape), keyAsPropertyName for v3/v4, no [Key] so PolyType/Nerdbank emit
// property-name maps too. Enums are shared with the primary model.
[V3::MessagePack.MessagePackObject(keyAsPropertyName: true)]
[PolyType.GenerateShape]
public partial class AnswerMap
{
    public int? question_id { get; set; }
    public int? answer_id { get; set; }
    public DateTime? locked_date { get; set; }
    public DateTime? creation_date { get; set; }
    public DateTime? last_edit_date { get; set; }
    public DateTime? last_activity_date { get; set; }
    public int? score { get; set; }
    public DateTime? community_owned_date { get; set; }
    public bool? is_accepted { get; set; }
    public string? body { get; set; }
    public ShallowUserMap? owner { get; set; }
    public string? title { get; set; }
    public int? up_vote_count { get; set; }
    public int? down_vote_count { get; set; }
    public List<CommentMap>? comments { get; set; }
    public string? link { get; set; }
    public List<string>? tags { get; set; }
    public bool? upvoted { get; set; }
    public bool? downvoted { get; set; }
    public bool? accepted { get; set; }
    public ShallowUserMap? last_editor { get; set; }
    public int? comment_count { get; set; }
    public string? body_markdown { get; set; }
    public string? share_link { get; set; }
}

[V3::MessagePack.MessagePackObject(keyAsPropertyName: true)]
public partial class CommentMap
{
    public int? comment_id { get; set; }
    public int? post_id { get; set; }
    public DateTime? creation_date { get; set; }
    public PostType? post_type { get; set; }
    public int? score { get; set; }
    public bool? edited { get; set; }
    public string? body { get; set; }
    public ShallowUserMap? owner { get; set; }
    public ShallowUserMap? reply_to_user { get; set; }
    public string? link { get; set; }
    public string? body_markdown { get; set; }
    public bool? upvoted { get; set; }
}

[V3::MessagePack.MessagePackObject(keyAsPropertyName: true)]
public partial class ShallowUserMap
{
    public int? user_id { get; set; }
    public string? display_name { get; set; }
    public int? reputation { get; set; }
    public UserType? user_type { get; set; }
    public string? profile_image { get; set; }
    public string? link { get; set; }
    public int? accept_rate { get; set; }
    public BadgeCountMap? badge_counts { get; set; }
}

[V3::MessagePack.MessagePackObject(keyAsPropertyName: true)]
public partial class BadgeCountMap
{
    public int? gold { get; set; }
    public int? silver { get; set; }
    public int? bronze { get; set; }
}

#pragma warning restore IDE1006

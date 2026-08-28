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
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerMapBenchmark
{
    AnswerMap answerMap = default!;
    V3GeneratedModels.AnswerMap answerMapV3SourceGen = default!;
    byte[] mpcsPayload = default!;
    byte[] v4Payload = default!;
    byte[] nbPayload = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new() { SerializeDefaultValues = Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always };

    [GlobalSetup]
    public void Setup()
    {
        var answer = AnswerBenchmark.CreateAnswer();
        answerMap = System.Text.Json.JsonSerializer.Deserialize<AnswerMap>(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(answer))!;

        mpcsPayload = V3::MessagePack.MessagePackSerializer.Serialize(answerMap);
        v4Payload = MessagePack.MessagePackSerializer.Serialize(answerMap);
        nbPayload = nb.Serialize(answerMap);

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

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public AnswerMap DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(v4Payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerMap DeserializeMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<AnswerMap>(mpcsPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public V3GeneratedModels.AnswerMap DeserializeMpcsSourceGen() => V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.AnswerMap>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public AnswerMap DeserializeNerdbank() => nb.Deserialize<AnswerMap>(nbPayload)!;
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

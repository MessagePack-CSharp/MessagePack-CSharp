using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Buffers;

// The Stack Overflow API `Answer` model from MessagePack-CSharp's SerializerBenchmark
// (benchmark/SerializerBenchmark/Models) — the classic "realistically nested" poco:
// Answer -> ShallowUser -> BadgeCount, Answer -> List<Comment> -> Comment -> ShallowUser.
// The original declares every member nullable (int?/bool?/DateTime?/enum?), which pushes
// every primitive through the Nullable resolver path — an atypical shape. This port
// strips Nullable from all value types so primitives take the direct (batched) write
// path like a normal poco; reference members stay `string?` etc. per the BenchPerson
// idiom (the nullable annotation doesn't change their formatter classification).
// Three-way comparison: UltraMessagePack (source-generated formatters) vs
// MessagePack-CSharp (source-generated resolver) vs Nerdbank.MessagePack (PolyType shapes).
//
// Setup cross-checks Ultra bytes against the MessagePack-CSharp oracle (must be
// byte-identical) and verifies every library's roundtrip by re-serializing the
// deserialized object with the oracle and comparing bytes. Each library deserializes
// its OWN serialized payload (Nerdbank is allowed to differ in encoding).
//
// MEASURED (i7-13700KF, ShortRun, ~1.65KB payload: 3 comments / 3 users / 4 tags, ns/op):
//   Serialize:   Ultra  396 | MessagePack-CSharp  894 (2.26x) | Nerdbank 1186 (3.00x)
//   Deserialize: Ultra  866 | MessagePack-CSharp 1484 (1.71x) | Nerdbank 1904 (2.20x)
// (all-Nullable variant of the same graph measured first: Ultra 517/968, ratios
//  2.13x/1.97x — de-nulling bought Ultra -23% serialize because primitives fold into
//  batched single-reservation writes instead of per-member NullableFormatter hops.)
// Allocated identical (payload-driven, 4.37KB deserialize / 1.65KB serialize; Nerdbank
// serialize 1.73KB — its encoding differs slightly). VERDICT: the generated-formatter
// advantage holds on a realistically nested graph, roughly 2x over MessagePack-CSharp
// on both directions — same shape as the flat BenchPerson result, so the win is not
// an artifact of tiny payloads.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerBenchmark
{
    Answer answer = default!;
    byte[] mpcsPayload = default!;
    byte[] ultraPayload = default!;
    byte[] nbPayload = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();

    [GlobalSetup]
    public void Setup()
    {
        answer = CreateAnswer();

        mpcsPayload = MessagePack.MessagePackSerializer.Serialize(answer);
        ultraPayload = UltraMessagePack.MessagePackSerializer.Default.Serialize(answer);
        nbPayload = nb.Serialize(answer);

        if (!ultraPayload.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: Ultra bytes ({ultraPayload.Length}) != MessagePack-CSharp oracle ({mpcsPayload.Length})");

        VerifyRoundtrip(UltraMessagePack.MessagePackSerializer.Default.Deserialize<Answer>(ultraPayload)!, "Ultra");
        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<Answer>(mpcsPayload), "MessagePack-CSharp");
        VerifyRoundtrip(nb.Deserialize<Answer>(new ReadOnlySequence<byte>(nbPayload))!, "Nerdbank");
    }

    void VerifyRoundtrip(Answer back, string label)
    {
        // field-by-field equality via the oracle: re-serialize the roundtripped object
        // with MessagePack-CSharp and demand byte identity with the original payload
        var bytes = MessagePack.MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(mpcsPayload)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeUltra() => UltraMessagePack.MessagePackSerializer.Default.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcs() => MessagePack.MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nb.Serialize(answer);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Answer DeserializeUltra() => UltraMessagePack.MessagePackSerializer.Default.Deserialize<Answer>(ultraPayload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeMpcs() => MessagePack.MessagePackSerializer.Deserialize<Answer>(mpcsPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeNerdbank() => nb.Deserialize<Answer>(new ReadOnlySequence<byte>(nbPayload))!;

    internal static Answer CreateAnswer()
    {
        var owner = new ShallowUser
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
        var editor = new ShallowUser
        {
            user_id = 23354,
            display_name = "Marc Gravell",
            reputation = 1_000_000,
            user_type = UserType.moderator,
            link = "https://stackoverflow.com/users/23354/marc-gravell",
            badge_counts = new BadgeCount { gold = 426, silver = 3568, bronze = 4826 },
        };
        var commenter = new ShallowUser
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
            // non-nullable DateTime members must carry a real UTC value: default(DateTime)
            // is Kind.Unspecified, which the three libraries normalize differently
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
            owner = owner,
            last_editor = editor,
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
                    owner = commenter,
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
                    owner = editor,
                    reply_to_user = commenter,
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
                    owner = commenter,
                },
            ],
            tags = ["c#", ".net", "string", "performance"],
        };
    }
}

#pragma warning disable IDE1006 // naming matches the Stack Overflow API wire format

[MessagePack.MessagePackObject]
[PolyType.GenerateShape]
public partial class Answer
{
    [MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int question_id { get; set; }
    [MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public int answer_id { get; set; }
    [MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
    public DateTime locked_date { get; set; }
    [MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
    public DateTime creation_date { get; set; }
    [MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
    public DateTime last_edit_date { get; set; }
    [MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
    public DateTime last_activity_date { get; set; }
    [MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
    public int score { get; set; }
    [MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
    public DateTime community_owned_date { get; set; }
    [MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
    public bool is_accepted { get; set; }
    [MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
    public string? body { get; set; }
    [MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
    public ShallowUser? owner { get; set; }
    [MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
    public string? title { get; set; }
    [MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
    public int up_vote_count { get; set; }
    [MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
    public int down_vote_count { get; set; }
    [MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
    public List<Comment>? comments { get; set; }
    [MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
    public string? link { get; set; }
    [MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
    public List<string>? tags { get; set; }
    [MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
    public bool upvoted { get; set; }
    [MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
    public bool downvoted { get; set; }
    [MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
    public bool accepted { get; set; }
    [MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
    public ShallowUser? last_editor { get; set; }
    [MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
    public int comment_count { get; set; }
    [MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
    public string? body_markdown { get; set; }
    [MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
    public string? share_link { get; set; }
}

[MessagePack.MessagePackObject]
public partial class Comment
{
    [MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int comment_id { get; set; }
    [MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public int post_id { get; set; }
    [MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
    public DateTime creation_date { get; set; }
    [MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
    public PostType post_type { get; set; }
    [MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
    public int score { get; set; }
    [MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
    public bool edited { get; set; }
    [MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
    public string? body { get; set; }
    [MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
    public ShallowUser? owner { get; set; }
    [MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
    public ShallowUser? reply_to_user { get; set; }
    [MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
    public string? link { get; set; }
    [MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
    public string? body_markdown { get; set; }
    [MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
    public bool upvoted { get; set; }
}

[MessagePack.MessagePackObject]
public partial class ShallowUser
{
    [MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int user_id { get; set; }
    [MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public string? display_name { get; set; }
    [MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
    public int reputation { get; set; }
    [MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
    public UserType user_type { get; set; }
    [MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
    public string? profile_image { get; set; }
    [MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
    public string? link { get; set; }
    [MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
    public int accept_rate { get; set; }
    [MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
    public BadgeCount? badge_counts { get; set; }
}

[MessagePack.MessagePackObject]
public partial class BadgeCount
{
    [MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int gold { get; set; }
    [MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public int silver { get; set; }
    [MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
    public int bronze { get; set; }
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

#pragma warning restore IDE1006

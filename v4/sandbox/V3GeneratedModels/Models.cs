using MessagePack;

namespace V3GeneratedModels;

// Twins of the benchmark model types, msgpack-attributed ONLY (no PolyType/protobuf/
// Orleans - those serializers are measured on the primary copies). Same class names,
// same [Key] layout, so the wire is byte-identical to the primary types; the benchmarks
// materialize instances by deserializing the oracle payload as these types (the
// "serialize bridge"), which both avoids duplicating the data literals and verifies the
// generated resolver end to end. Type identity is intentionally separate: the v3
// SourceGen rows measure these twins, exactly the way the protobuf/Orleans rows measure
// their own generated types.

#pragma warning disable IDE1006 // naming matches the Stack Overflow API wire format

[MessagePackObject]
public class Answer
{
    [Key(0)] public int? question_id { get; set; }
    [Key(1)] public int? answer_id { get; set; }
    [Key(2)] public DateTime? locked_date { get; set; }
    [Key(3)] public DateTime? creation_date { get; set; }
    [Key(4)] public DateTime? last_edit_date { get; set; }
    [Key(5)] public DateTime? last_activity_date { get; set; }
    [Key(6)] public int? score { get; set; }
    [Key(7)] public DateTime? community_owned_date { get; set; }
    [Key(8)] public bool? is_accepted { get; set; }
    [Key(9)] public string? body { get; set; }
    [Key(10)] public ShallowUser? owner { get; set; }
    [Key(11)] public string? title { get; set; }
    [Key(12)] public int? up_vote_count { get; set; }
    [Key(13)] public int? down_vote_count { get; set; }
    [Key(14)] public List<Comment>? comments { get; set; }
    [Key(15)] public string? link { get; set; }
    [Key(16)] public List<string>? tags { get; set; }
    [Key(17)] public bool? upvoted { get; set; }
    [Key(18)] public bool? downvoted { get; set; }
    [Key(19)] public bool? accepted { get; set; }
    [Key(20)] public ShallowUser? last_editor { get; set; }
    [Key(21)] public int? comment_count { get; set; }
    [Key(22)] public string? body_markdown { get; set; }
    [Key(23)] public string? share_link { get; set; }
}

[MessagePackObject]
public class Comment
{
    [Key(0)] public int? comment_id { get; set; }
    [Key(1)] public int? post_id { get; set; }
    [Key(2)] public DateTime? creation_date { get; set; }
    [Key(3)] public PostType? post_type { get; set; }
    [Key(4)] public int? score { get; set; }
    [Key(5)] public bool? edited { get; set; }
    [Key(6)] public string? body { get; set; }
    [Key(7)] public ShallowUser? owner { get; set; }
    [Key(8)] public ShallowUser? reply_to_user { get; set; }
    [Key(9)] public string? link { get; set; }
    [Key(10)] public string? body_markdown { get; set; }
    [Key(11)] public bool? upvoted { get; set; }
}

[MessagePackObject]
public class ShallowUser
{
    [Key(0)] public int? user_id { get; set; }
    [Key(1)] public string? display_name { get; set; }
    [Key(2)] public int? reputation { get; set; }
    [Key(3)] public UserType? user_type { get; set; }
    [Key(4)] public string? profile_image { get; set; }
    [Key(5)] public string? link { get; set; }
    [Key(6)] public int? accept_rate { get; set; }
    [Key(7)] public BadgeCount? badge_counts { get; set; }
}

[MessagePackObject]
public class BadgeCount
{
    [Key(0)] public int? gold { get; set; }
    [Key(1)] public int? silver { get; set; }
    [Key(2)] public int? bronze { get; set; }
}

// map twins (keyAsPropertyName): the AnswerMapBenchmark rows for v3's generated
// string-key formatters; same members as the array twins above
[MessagePackObject(keyAsPropertyName: true)]
public class AnswerMap
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

[MessagePackObject(keyAsPropertyName: true)]
public class CommentMap
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

[MessagePackObject(keyAsPropertyName: true)]
public class ShallowUserMap
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

[MessagePackObject(keyAsPropertyName: true)]
public class BadgeCountMap
{
    public int? gold { get; set; }
    public int? silver { get; set; }
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

#pragma warning restore IDE1006

[MessagePackObject]
public class BenchPerson
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class NbPocoMap
{
    public int SomeInt { get; set; }

    public string? SomeString { get; set; }
}

[MessagePackObject]
public class NbPocoAsArray
{
    [Key(0)] public int SomeInt { get; set; }

    [Key(1)] public string? SomeString { get; set; }
}

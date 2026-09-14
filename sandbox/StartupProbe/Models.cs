using MessagePack;

namespace StartupProbe.V4Models;

// v4-attributed twins of V3GeneratedModels' array-key Answer graph (same layout, so
// both serializers do equivalent first-call work over an identical shape). Compiled
// against the product MessagePack.dll, so the in-box source generator emits formatters
// for these and registers them via module initializer before Main.

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

// second, unrelated graph for the marginal-cost split: resolving this AFTER Answer
// tells whether the first resolve's cost is one-time machinery warmup or per-type
[MessagePackObject]
public class MarginalPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
    [Key(3)] public List<MarginalChild>? Children { get; set; }
}

[MessagePackObject]
public class MarginalChild
{
    [Key(0)] public long Value { get; set; }
    [Key(1)] public bool Flag { get; set; }
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

using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Resolvers;
using BcsSharp.Core.Unions;

#pragma warning disable IDE1006 // naming matches the Stack Overflow API wire format

// BcsSharp twin of the Answer graph. BCS (Binary Canonical Serialization, the Sui/Move wire
// format) is positional and untagged like msgpack-as-array, but it has NO null: a null
// string/List/class member throws at serialize time, and the format's optional is
// Option<T> (0x00 = None, 0x01 + payload = Some). The shared POCO's nullable reference
// members (string?, ShallowUser?, List<T>?) therefore cannot be annotated in place; each
// becomes Option<T> here, which is also the faithful mapping (the data has genuinely null
// strings and a null reply_to_user). Value-type Nullable<T> maps 1:1 (same Option wire).
// BCS has no timestamp type either; DateTime rides a registered custom formatter as i64
// UTC ticks (8 B, the same idea as V4's DotNetOptimized ToBinary int64). Enums go on the
// wire as the ULEB128 of their DECLARATION index (serde semantics), not their value.
// Like the Google.Protobuf row, BcsSharp is measured on this pre-mapped twin; AnswerBcsMap
// exists only so Setup can roundtrip the result through the msgpack oracle.

[BcsStruct]
public sealed class AnswerBcs
{
    [BcsField(0)] public int? question_id { get; set; }
    [BcsField(1)] public int? answer_id { get; set; }
    [BcsField(2)] public DateTime? locked_date { get; set; }
    [BcsField(3)] public DateTime? creation_date { get; set; }
    [BcsField(4)] public DateTime? last_edit_date { get; set; }
    [BcsField(5)] public DateTime? last_activity_date { get; set; }
    [BcsField(6)] public int? score { get; set; }
    [BcsField(7)] public DateTime? community_owned_date { get; set; }
    [BcsField(8)] public bool? is_accepted { get; set; }
    [BcsField(9)] public Option<string> body { get; set; } = None.Instance;
    [BcsField(10)] public Option<ShallowUserBcs> owner { get; set; } = None.Instance;
    [BcsField(11)] public Option<string> title { get; set; } = None.Instance;
    [BcsField(12)] public int? up_vote_count { get; set; }
    [BcsField(13)] public int? down_vote_count { get; set; }
    [BcsField(14)] public Option<List<CommentBcs>> comments { get; set; } = None.Instance;
    [BcsField(15)] public Option<string> link { get; set; } = None.Instance;
    [BcsField(16)] public Option<List<string>> tags { get; set; } = None.Instance;
    [BcsField(17)] public bool? upvoted { get; set; }
    [BcsField(18)] public bool? downvoted { get; set; }
    [BcsField(19)] public bool? accepted { get; set; }
    [BcsField(20)] public Option<ShallowUserBcs> last_editor { get; set; } = None.Instance;
    [BcsField(21)] public int? comment_count { get; set; }
    [BcsField(22)] public Option<string> body_markdown { get; set; } = None.Instance;
    [BcsField(23)] public Option<string> share_link { get; set; } = None.Instance;
}

[BcsStruct]
public sealed class CommentBcs
{
    [BcsField(0)] public int? comment_id { get; set; }
    [BcsField(1)] public int? post_id { get; set; }
    [BcsField(2)] public DateTime? creation_date { get; set; }
    [BcsField(3)] public PostType? post_type { get; set; }
    [BcsField(4)] public int? score { get; set; }
    [BcsField(5)] public bool? edited { get; set; }
    [BcsField(6)] public Option<string> body { get; set; } = None.Instance;
    [BcsField(7)] public Option<ShallowUserBcs> owner { get; set; } = None.Instance;
    [BcsField(8)] public Option<ShallowUserBcs> reply_to_user { get; set; } = None.Instance;
    [BcsField(9)] public Option<string> link { get; set; } = None.Instance;
    [BcsField(10)] public Option<string> body_markdown { get; set; } = None.Instance;
    [BcsField(11)] public bool? upvoted { get; set; }
}

[BcsStruct]
public sealed class ShallowUserBcs
{
    [BcsField(0)] public int? user_id { get; set; }
    [BcsField(1)] public Option<string> display_name { get; set; } = None.Instance;
    [BcsField(2)] public int? reputation { get; set; }
    [BcsField(3)] public UserType? user_type { get; set; }
    [BcsField(4)] public Option<string> profile_image { get; set; } = None.Instance;
    [BcsField(5)] public Option<string> link { get; set; } = None.Instance;
    [BcsField(6)] public int? accept_rate { get; set; }
    [BcsField(7)] public Option<BadgeCountBcs> badge_counts { get; set; } = None.Instance;
}

[BcsStruct]
public sealed class BadgeCountBcs
{
    [BcsField(0)] public int? gold { get; set; }
    [BcsField(1)] public int? silver { get; set; }
    [BcsField(2)] public int? bronze { get; set; }
}

// DateTime as i64 UTC ticks: BCS has no time type, so this is an application convention,
// registered process-wide (CustomFormatterResolver sits first in the default chain and
// NullableResolver picks it up for DateTime? without further registration)
sealed class BcsDateTimeFormatter : IBcsFormatter<DateTime>
{
    public void Serialize(ref BcsWriter writer, DateTime value) => writer.Write(value.ToUniversalTime().Ticks);

    public DateTime Deserialize(ref BcsReader reader) => new(reader.ReadI64(), DateTimeKind.Utc);
}

static class AnswerBcsMap
{
    static AnswerBcsMap()
    {
        CustomFormatterResolver.Instance.Register<DateTime>(new BcsDateTimeFormatter());
        // CompositeResolver memoises misses; clearing makes the registration effective even
        // if something resolved a DateTime-bearing type before this class was touched
        BcsSerializer.ClearFormatterCache();
    }

    static Option<string> Opt(string? s)
    {
        if (s is null) return None.Instance;
        return s;
    }

    static string? Str(Option<string> o) => o.Value as string;

    static Option<BadgeCountBcs> ToBcs(BadgeCount? b)
    {
        if (b is null) return None.Instance;
        return new BadgeCountBcs { gold = b.gold, silver = b.silver, bronze = b.bronze };
    }

    static BadgeCount? ToPoco(Option<BadgeCountBcs> o)
    {
        if (o.Value is not BadgeCountBcs b) return null;
        return new BadgeCount { gold = b.gold, silver = b.silver, bronze = b.bronze };
    }

    static Option<ShallowUserBcs> ToBcs(ShallowUser? u)
    {
        if (u is null) return None.Instance;
        return new ShallowUserBcs
        {
            user_id = u.user_id,
            display_name = Opt(u.display_name),
            reputation = u.reputation,
            user_type = u.user_type,
            profile_image = Opt(u.profile_image),
            link = Opt(u.link),
            accept_rate = u.accept_rate,
            badge_counts = ToBcs(u.badge_counts),
        };
    }

    static ShallowUser? ToPoco(Option<ShallowUserBcs> o)
    {
        if (o.Value is not ShallowUserBcs u) return null;
        return new ShallowUser
        {
            user_id = u.user_id,
            display_name = Str(u.display_name),
            reputation = u.reputation,
            user_type = u.user_type,
            profile_image = Str(u.profile_image),
            link = Str(u.link),
            accept_rate = u.accept_rate,
            badge_counts = ToPoco(u.badge_counts),
        };
    }

    static Option<List<CommentBcs>> ToBcs(List<Comment>? comments)
    {
        if (comments is null) return None.Instance;
        return comments.Select(c => new CommentBcs
        {
            comment_id = c.comment_id,
            post_id = c.post_id,
            creation_date = c.creation_date,
            post_type = c.post_type,
            score = c.score,
            edited = c.edited,
            body = Opt(c.body),
            owner = ToBcs(c.owner),
            reply_to_user = ToBcs(c.reply_to_user),
            link = Opt(c.link),
            body_markdown = Opt(c.body_markdown),
            upvoted = c.upvoted,
        }).ToList();
    }

    static List<Comment>? ToPoco(Option<List<CommentBcs>> o)
    {
        if (o.Value is not List<CommentBcs> comments) return null;
        return comments.Select(c => new Comment
        {
            comment_id = c.comment_id,
            post_id = c.post_id,
            creation_date = c.creation_date,
            post_type = c.post_type,
            score = c.score,
            edited = c.edited,
            body = Str(c.body),
            owner = ToPoco(c.owner),
            reply_to_user = ToPoco(c.reply_to_user),
            link = Str(c.link),
            body_markdown = Str(c.body_markdown),
            upvoted = c.upvoted,
        }).ToList();
    }

    static Option<List<string>> ToBcs(List<string>? tags)
    {
        if (tags is null) return None.Instance;
        return tags;
    }

    public static AnswerBcs ToBcs(Answer a) => new()
    {
        question_id = a.question_id,
        answer_id = a.answer_id,
        locked_date = a.locked_date,
        creation_date = a.creation_date,
        last_edit_date = a.last_edit_date,
        last_activity_date = a.last_activity_date,
        score = a.score,
        community_owned_date = a.community_owned_date,
        is_accepted = a.is_accepted,
        body = Opt(a.body),
        owner = ToBcs(a.owner),
        title = Opt(a.title),
        up_vote_count = a.up_vote_count,
        down_vote_count = a.down_vote_count,
        comments = ToBcs(a.comments),
        link = Opt(a.link),
        tags = ToBcs(a.tags),
        upvoted = a.upvoted,
        downvoted = a.downvoted,
        accepted = a.accepted,
        last_editor = ToBcs(a.last_editor),
        comment_count = a.comment_count,
        body_markdown = Opt(a.body_markdown),
        share_link = Opt(a.share_link),
    };

    public static Answer ToPoco(AnswerBcs a) => new()
    {
        question_id = a.question_id,
        answer_id = a.answer_id,
        locked_date = a.locked_date,
        creation_date = a.creation_date,
        last_edit_date = a.last_edit_date,
        last_activity_date = a.last_activity_date,
        score = a.score,
        community_owned_date = a.community_owned_date,
        is_accepted = a.is_accepted,
        body = Str(a.body),
        owner = ToPoco(a.owner),
        title = Str(a.title),
        up_vote_count = a.up_vote_count,
        down_vote_count = a.down_vote_count,
        comments = ToPoco(a.comments),
        link = Str(a.link),
        tags = a.tags.Value as List<string>,
        upvoted = a.upvoted,
        downvoted = a.downvoted,
        accepted = a.accepted,
        last_editor = ToPoco(a.last_editor),
        comment_count = a.comment_count,
        body_markdown = Str(a.body_markdown),
        share_link = Str(a.share_link),
    };
}

#pragma warning restore IDE1006

using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;

// Where AnswerBenchmark.DeserializeV4's time goes, by subtraction. A sampling profile of the same call
// (Program.cs --profile-answer-de, EventPipe) attributes time to the formatter bodies but cannot split them further:
// the allocation helpers are not managed frames and the primitive reads are inlined, so both land in the caller.
//
// Leave-one-out rows deserialize the same graph with one kind of data nulled (the member is still read, as a nil):
//   Full        - the AnswerBenchmark payload
//   NoStrings   - every string member and the tag list null
//   NoDates     - every DateTime? null
//   NoBadges    - every ShallowUser.badge_counts null
//   NoUsers     - every ShallowUser null (owner, last_editor, each comment's owner / reply_to_user)
//   NoComments  - comments null
//   Empty       - new Answer(): 24 nils, the fixed cost of one call plus one object
//   NoInts / NoBools / NoEnums - every int? / bool? / enum? null
//   LeavesNil   - every leaf null (strings, tags, dates, ints, bools, enums) with every object and the comment list
//                 kept: the cost of the structure alone (headers, formatter calls, Enter / Exit, object allocations)
//   NilRoot     - the payload is one nil: the public entry and nothing else
// Component rows isolate what the subtraction cannot:
//   SkipAll          - buffer.Skip() over the full payload: the token walk with nothing materialized
//   StringsDecode    - ReadString over a blob holding exactly the payload's strings
//   StringsAllocOnly - a zeroed string of each of those lengths: StringsDecode minus this is the transcode
//   TypedWalk        - the same tokens read with the same primitives in member order, straight-line, direct
//                      NoInlining calls per object, nothing allocated or transcoded (a string is header + Advance):
//                      the decode alone, without the generated loop, the interface calls and the bookkeeping
//   AllocGraph       - the same object graph allocated and filled from an in-memory template, strings as
//                      zeroed allocations: AllocGraph minus StringsAllocOnly is the objects and lists
//
// MEASURED (i7-13700KF, .NET 11, ShortRun x2, ns; payload 1658 B, 23 strings = 1373 chars = 1413 B on the wire):
//   Full 899 / 998 (the second run's Full was noisy, StdDev 116; the hot loop reads 920)   4760 B allocated
//   NoStrings 444 / 439   NoDates 877 / 861   NoBadges 806 / 800   NoUsers 485 / 499   NoComments 430 / 433
//   Empty 57 / 57   SkipAll 255 / 255   StringsDecode 328 / 342   StringsAllocOnly 123 / 125   AllocGraph 255 / 249
// Reading it against Full ~900:
//   allocation                 ~250 (28%)  strings ~124 (3328 B), objects and lists ~128 (1432 B)
//   utf8 -> utf16              ~210 (23%)  StringsDecode - StringsAllocOnly; the sampling profile agrees
//                                          (TranscodeToUtf16 12.7% + GetPointerToFirstInvalidByte 5.9% + ReadString 1.8%)
//   everything else            ~440 (49%)  token decode, nil checks, headers, formatter calls, Enter / Exit
// Not additive: strings cost 455 in context (Full - NoStrings) but 335 alone (StringsDecode), and the per-kind
// deltas overlap (a user's strings are in both NoUsers and NoStrings). By data: 6 users ~410 (68 each, with their
// strings and badges), 3 comments ~470 (with their 4 users), 6 badge counts ~97 (16 each), 8 dates ~25-40.
// SkipAll: walking the ~147 tokens with nothing materialized is 255.
//
// MEASURED, second round (same machine; run 3 not elevated for time, one elevated run for hardware counters):
//   time, ns:        NilRoot 6   Empty 62   LeavesNil 327   TypedWalk 311   (Full 902 in that run)
//   instructions/op: Full 20,177   TypedWalk 6,788   SkipAll 6,296   AllocGraph 3,701   StringsDecode 8,803
//                    StringsAllocOnly 1,907   LeavesNil 7,727   Empty 1,446   NilRoot 156
//                    NoStrings 10,944   NoInts 17,953   NoBadges 18,335   NoDates 19,699   NoEnums 19,879   NoBools 19,969
//   branches/op 3,959, mispredicted 10 (one fixed payload: the predictor has memorized it, real traffic will miss more)
// The "everything else" half splits into the decode itself and what the generated shape adds around it:
//   utf8 -> utf16                  6,896 instr (34%)  ~215 ns
//   typed token decode (TypedWalk) 6,788 instr (34%)  ~311 ns   ~46 instructions per token
//   allocation                     3,701 instr (18%)  ~250 ns   (fewer instructions per ns: memory-bound)
//   calls, loop / switch, Enter / Exit, budget, entry   2,792 instr (14%)  ~125 ns (by subtraction)
// Instruction counts add up exactly where times do not, so the shares are taken from them. Per member, against a
// nil in the same slot: int? +44 instructions (51 of them), bool? +21 (10), enum? +33 (9), DateTime? +60 (8);
// a nil member itself is ~50 (Empty: 1,290 over 24 members and one object). TotalCycles came back NA on this CPU,
// so there is no measured IPC; from time at ~5 GHz it is about 4, which is not what a latency-bound chain looks like:
// the cost is the number of instructions. Elevated times run ~40% high under counter collection, use the ratios.
[MemoryDiagnoser]
public class AnswerDeserializeBreakdownBenchmark
{
    byte[] full = null!;
    byte[] noStrings = null!;
    byte[] noDates = null!;
    byte[] noBadges = null!;
    byte[] noUsers = null!;
    byte[] noComments = null!;
    byte[] empty = null!;
    byte[] noInts = null!;
    byte[] noBools = null!;
    byte[] noEnums = null!;
    byte[] leavesNil = null!;
    byte[] nilRoot = null!;
    byte[] stringsBlob = null!;
    int[] stringLengths = null!;
    Answer template = null!;

    [GlobalSetup]
    public void Setup()
    {
        template = AnswerBenchmark.CreateAnswer();
        full = MessagePackSerializer.Serialize(template);
        noStrings = MessagePackSerializer.Serialize(Variant(NullStrings));
        noDates = MessagePackSerializer.Serialize(Variant(NullDates));
        noBadges = MessagePackSerializer.Serialize(Variant(static a =>
        {
            foreach (var u in Users(a))
            {
                u.badge_counts = null;
            }
        }));
        noUsers = MessagePackSerializer.Serialize(Variant(static a =>
        {
            a.owner = a.last_editor = null;
            foreach (var c in a.comments!)
            {
                c.owner = c.reply_to_user = null;
            }
        }));
        noComments = MessagePackSerializer.Serialize(Variant(static a => a.comments = null));
        empty = MessagePackSerializer.Serialize(new Answer());
        noInts = MessagePackSerializer.Serialize(Variant(NullInts));
        noBools = MessagePackSerializer.Serialize(Variant(NullBools));
        noEnums = MessagePackSerializer.Serialize(Variant(NullEnums));
        leavesNil = MessagePackSerializer.Serialize(Variant(static a =>
        {
            NullStrings(a);
            NullDates(a);
            NullInts(a);
            NullBools(a);
            NullEnums(a);
        }));
        nilRoot = MessagePackSerializer.Serialize<Answer?>(null);

        var strings = Strings(template).ToArray();
        stringLengths = strings.Select(static s => s.Length).ToArray();
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            foreach (var s in strings)
            {
                buffer.WriteString(s);
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        stringsBlob = writer.WrittenSpan.ToArray();

        Console.WriteLine($"// payload {full.Length} B; {strings.Length} strings, {strings.Sum(static s => s.Length)} chars, {stringsBlob.Length} B on the wire");
        Console.WriteLine($"// noInts {noInts.Length} B, noBools {noBools.Length} B, noEnums {noEnums.Length} B, leavesNil {leavesNil.Length} B, nilRoot {nilRoot.Length} B");
        if (NilRoot() is not null)
        {
            throw new InvalidOperationException("NilRoot did not deserialize to null");
        }
        var walkBuffer = new ReadOnlySpanReadBuffer(full);
        WalkAnswer(ref walkBuffer);
        if (walkBuffer.BytesRemaining != 0)
        {
            throw new InvalidOperationException($"TypedWalk left {walkBuffer.BytesRemaining} bytes");
        }
        Console.WriteLine($"// noStrings {noStrings.Length} B, noDates {noDates.Length} B, noBadges {noBadges.Length} B, noUsers {noUsers.Length} B, noComments {noComments.Length} B, empty {empty.Length} B");
        if (SkipAll() != full.Length)
        {
            throw new InvalidOperationException("Skip did not consume the payload");
        }
        if (StringsDecode() != strings.Sum(static s => s.Length))
        {
            throw new InvalidOperationException("StringsDecode length mismatch");
        }
        // with its strings nulled, the allocated graph must be the NoStrings graph: same objects, same scalars
        var rebuilt = AllocGraph();
        NullStrings(rebuilt);
        if (!MessagePackSerializer.Serialize(rebuilt).AsSpan().SequenceEqual(noStrings))
        {
            throw new InvalidOperationException("AllocGraph does not rebuild the template's shape");
        }
    }

    static void NullStrings(Answer a)
    {
        a.body = a.title = a.link = a.body_markdown = a.share_link = null;
        a.tags = null;
        foreach (var c in a.comments!)
        {
            c.body = c.link = c.body_markdown = null;
        }
        foreach (var u in Users(a))
        {
            u.display_name = u.profile_image = u.link = null;
        }
    }

    static void NullDates(Answer a)
    {
        a.locked_date = a.creation_date = a.last_edit_date = a.last_activity_date = a.community_owned_date = null;
        foreach (var c in a.comments!)
        {
            c.creation_date = null;
        }
    }

    static void NullInts(Answer a)
    {
        a.question_id = a.answer_id = a.score = a.up_vote_count = a.down_vote_count = a.comment_count = null;
        foreach (var c in a.comments!)
        {
            c.comment_id = c.post_id = c.score = null;
        }
        foreach (var u in Users(a))
        {
            u.user_id = u.reputation = u.accept_rate = null;
            if (u.badge_counts is { } badges)
            {
                badges.gold = badges.silver = badges.bronze = null;
            }
        }
    }

    static void NullBools(Answer a)
    {
        a.is_accepted = a.upvoted = a.downvoted = a.accepted = null;
        foreach (var c in a.comments!)
        {
            c.edited = c.upvoted = null;
        }
    }

    static void NullEnums(Answer a)
    {
        foreach (var c in a.comments!)
        {
            c.post_type = null;
        }
        foreach (var u in Users(a))
        {
            u.user_type = null;
        }
    }

    static Answer Variant(Action<Answer> mutate)
    {
        var answer = AnswerBenchmark.CreateAnswer();
        mutate(answer);
        return answer;
    }

    static IEnumerable<ShallowUser> Users(Answer a)
    {
        if (a.owner is not null) yield return a.owner;
        if (a.last_editor is not null) yield return a.last_editor;
        foreach (var c in a.comments ?? [])
        {
            if (c.owner is not null) yield return c.owner;
            if (c.reply_to_user is not null) yield return c.reply_to_user;
        }
    }

    static IEnumerable<string> Strings(Answer a)
    {
        var all = new List<string?> { a.body, a.title, a.link, a.body_markdown, a.share_link };
        all.AddRange(a.tags ?? []);
        foreach (var c in a.comments ?? [])
        {
            all.AddRange([c.body, c.link, c.body_markdown]);
        }
        foreach (var u in Users(a))
        {
            all.AddRange([u.display_name, u.profile_image, u.link]);
        }
        return all.Where(static s => s is not null)!;
    }

    // ---- leave-one-out ----------------------------------------------------

    [Benchmark(Baseline = true)]
    public Answer Full() => MessagePackSerializer.Deserialize<Answer>(full)!;

    [Benchmark]
    public Answer NoStrings() => MessagePackSerializer.Deserialize<Answer>(noStrings)!;

    [Benchmark]
    public Answer NoDates() => MessagePackSerializer.Deserialize<Answer>(noDates)!;

    [Benchmark]
    public Answer NoBadges() => MessagePackSerializer.Deserialize<Answer>(noBadges)!;

    [Benchmark]
    public Answer NoUsers() => MessagePackSerializer.Deserialize<Answer>(noUsers)!;

    [Benchmark]
    public Answer NoComments() => MessagePackSerializer.Deserialize<Answer>(noComments)!;

    [Benchmark]
    public Answer Empty() => MessagePackSerializer.Deserialize<Answer>(empty)!;

    [Benchmark]
    public Answer NoInts() => MessagePackSerializer.Deserialize<Answer>(noInts)!;

    [Benchmark]
    public Answer NoBools() => MessagePackSerializer.Deserialize<Answer>(noBools)!;

    [Benchmark]
    public Answer NoEnums() => MessagePackSerializer.Deserialize<Answer>(noEnums)!;

    [Benchmark]
    public Answer LeavesNil() => MessagePackSerializer.Deserialize<Answer>(leavesNil)!;

    [Benchmark]
    public Answer? NilRoot() => MessagePackSerializer.Deserialize<Answer>(nilRoot);

    // ---- components -------------------------------------------------------

    [Benchmark]
    public long TypedWalk()
    {
        var buffer = new ReadOnlySpanReadBuffer(full);
        return WalkAnswer(ref buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long Int(ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? 0 : b.ReadInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long Bool(ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? 0 : b.ReadBoolean() ? 1 : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long Date(ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? 0 : b.ReadTimestamp().Ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long Enum(ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? 0 : b.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long Str(ref ReadOnlySpanReadBuffer b)
    {
        if (b.TryReadNil()) return 0;
        var byteCount = b.ReadStringHeader();
        b.Advance(byteCount);
        return byteCount;
    }

    static void Expect(int count, int expected)
    {
        if (count != expected) throw new InvalidOperationException($"TypedWalk expected {expected} members, the header says {count}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WalkAnswer(ref ReadOnlySpanReadBuffer b)
    {
        Expect(b.ReadArrayHeader(), 24);
        long sum = Int(ref b) + Int(ref b);
        sum += Date(ref b) + Date(ref b) + Date(ref b) + Date(ref b);
        sum += Int(ref b);
        sum += Date(ref b);
        sum += Bool(ref b);
        sum += Str(ref b);
        sum += WalkUser(ref b);
        sum += Str(ref b);
        sum += Int(ref b) + Int(ref b);
        if (!b.TryReadNil())
        {
            var comments = b.ReadArrayHeader();
            for (int i = 0; i < comments; i++) sum += WalkComment(ref b);
        }
        sum += Str(ref b);
        if (!b.TryReadNil())
        {
            var tags = b.ReadArrayHeader();
            for (int i = 0; i < tags; i++) sum += Str(ref b);
        }
        sum += Bool(ref b) + Bool(ref b) + Bool(ref b);
        sum += WalkUser(ref b);
        sum += Int(ref b);
        sum += Str(ref b) + Str(ref b);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WalkComment(ref ReadOnlySpanReadBuffer b)
    {
        if (b.TryReadNil()) return 0;
        Expect(b.ReadArrayHeader(), 12);
        long sum = Int(ref b) + Int(ref b);
        sum += Date(ref b);
        sum += Enum(ref b);
        sum += Int(ref b);
        sum += Bool(ref b);
        sum += Str(ref b);
        sum += WalkUser(ref b) + WalkUser(ref b);
        sum += Str(ref b) + Str(ref b);
        sum += Bool(ref b);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WalkUser(ref ReadOnlySpanReadBuffer b)
    {
        if (b.TryReadNil()) return 0;
        Expect(b.ReadArrayHeader(), 8);
        long sum = Int(ref b);
        sum += Str(ref b);
        sum += Int(ref b);
        sum += Enum(ref b);
        sum += Str(ref b) + Str(ref b);
        sum += Int(ref b);
        sum += WalkBadge(ref b);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WalkBadge(ref ReadOnlySpanReadBuffer b)
    {
        if (b.TryReadNil()) return 0;
        Expect(b.ReadArrayHeader(), 3);
        return Int(ref b) + Int(ref b) + Int(ref b);
    }

    [Benchmark]
    public long SkipAll()
    {
        var buffer = new ReadOnlySpanReadBuffer(full);
        buffer.Skip();
        return full.Length - buffer.BytesRemaining;
    }

    [Benchmark]
    public int StringsDecode()
    {
        var buffer = new ReadOnlySpanReadBuffer(stringsBlob);
        var chars = 0;
        var lengths = stringLengths;
        for (int i = 0; i < lengths.Length; i++)
        {
            chars += buffer.ReadString()!.Length;
        }
        return chars;
    }

    [Benchmark]
    public int StringsAllocOnly()
    {
        var chars = 0;
        var lengths = stringLengths;
        for (int i = 0; i < lengths.Length; i++)
        {
            chars += new string('\0', lengths[i]).Length;
        }
        return chars;
    }

    [Benchmark]
    public Answer AllocGraph() => ZeroedStrings(template);

    // a deep copy whose strings are fresh zeroed allocations of the same length: every allocation Deserialize makes
    // (objects, lists presized to their count, strings) and the member stores, without reading a byte
    static Answer ZeroedStrings(Answer s)
    {
        return new Answer
        {
            question_id = s.question_id,
            answer_id = s.answer_id,
            locked_date = s.locked_date,
            creation_date = s.creation_date,
            last_edit_date = s.last_edit_date,
            last_activity_date = s.last_activity_date,
            score = s.score,
            community_owned_date = s.community_owned_date,
            is_accepted = s.is_accepted,
            body = Zeroed(s.body),
            owner = Copy(s.owner),
            title = Zeroed(s.title),
            up_vote_count = s.up_vote_count,
            down_vote_count = s.down_vote_count,
            comments = CopyComments(s.comments),
            link = Zeroed(s.link),
            tags = CopyTags(s.tags),
            upvoted = s.upvoted,
            downvoted = s.downvoted,
            accepted = s.accepted,
            last_editor = Copy(s.last_editor),
            comment_count = s.comment_count,
            body_markdown = Zeroed(s.body_markdown),
            share_link = Zeroed(s.share_link),
        };

        static string? Zeroed(string? value) => value is null ? null : new string('\0', value.Length);

        static List<string>? CopyTags(List<string>? tags)
        {
            if (tags is null) return null;
            var list = new List<string>(tags.Count);
            foreach (var t in tags) list.Add(Zeroed(t)!);
            return list;
        }

        static List<Comment>? CopyComments(List<Comment>? comments)
        {
            if (comments is null) return null;
            var list = new List<Comment>(comments.Count);
            foreach (var c in comments)
            {
                list.Add(new Comment
                {
                    comment_id = c.comment_id,
                    post_id = c.post_id,
                    creation_date = c.creation_date,
                    post_type = c.post_type,
                    score = c.score,
                    edited = c.edited,
                    body = Zeroed(c.body),
                    owner = Copy(c.owner),
                    reply_to_user = Copy(c.reply_to_user),
                    link = Zeroed(c.link),
                    body_markdown = Zeroed(c.body_markdown),
                    upvoted = c.upvoted,
                });
            }
            return list;
        }

        static ShallowUser? Copy(ShallowUser? u) => u is null ? null : new ShallowUser
        {
            user_id = u.user_id,
            display_name = Zeroed(u.display_name),
            reputation = u.reputation,
            user_type = u.user_type,
            profile_image = Zeroed(u.profile_image),
            link = Zeroed(u.link),
            accept_rate = u.accept_rate,
            badge_counts = u.badge_counts is null ? null : new BadgeCount
            {
                gold = u.badge_counts.gold,
                silver = u.badge_counts.silver,
                bronze = u.badge_counts.bronze,
            },
        };
    }
}

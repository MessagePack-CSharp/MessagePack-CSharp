extern alias V3;
using System.Diagnostics;

// Time-to-first-serialize probe: one measurement per PROCESS, mode picked by argv, so
// every number is a genuinely cold path (fresh JIT, fresh resolver state). The parent
// loop (startup-probe.ps1 style, or a plain shell loop) runs each mode N times and
// aggregates; BenchmarkDotNet deliberately not used - its pipeline measures steady
// state, and even ColdStart strategy shares a process across iterations.
// Output CSV: mode,firstSerializeUs,secondSerializeUs,firstDeserializeUs,secondDeserializeUs,mainToDoneUs
// Modes:
//   v4    - product serializer, Default options (source-generated formatters,
//           registered by module initializer before Main)
//   v3dyn - v3 oracle, StandardResolver (DynamicObjectResolver: Emit codegen on first
//           use - the default v3 experience)
//   v3sg  - v3 oracle, v3's real source-generated resolver first (the AOT composition
//           its docs prescribe), over the same model twins
//   noop  - baseline for parent-measured process wall time (host + runtime startup)

var t0 = Stopwatch.GetTimestamp();
var mode = args.Length > 0 ? args[0] : "v4";

switch (mode)
{
    case "noop":
        Console.WriteLine($"noop,0,0,0,0,{Us(t0, Stopwatch.GetTimestamp()):F1}");
        break;

    case "v4":
    {
        var answer = V4Data();
        var tA = Stopwatch.GetTimestamp();
        var bytes = MessagePack.MessagePackSerializer.Serialize(answer);
        var tB = Stopwatch.GetTimestamp();
        MessagePack.MessagePackSerializer.Serialize(answer);
        var tC = Stopwatch.GetTimestamp();
        var back = MessagePack.MessagePackSerializer.Deserialize<StartupProbe.V4Models.Answer>(bytes);
        var tD = Stopwatch.GetTimestamp();
        MessagePack.MessagePackSerializer.Deserialize<StartupProbe.V4Models.Answer>(bytes);
        var tE = Stopwatch.GetTimestamp();
        Verify(bytes, back!.comments!.Count);
        Console.WriteLine($"v4,{Us(tA, tB):F1},{Us(tB, tC):F1},{Us(tC, tD):F1},{Us(tD, tE):F1},{Us(t0, tE):F1}");
        break;
    }

    case "v4resolve":
    {
        // attribution split: formatter-graph resolution (factory chain probing +
        // Initialize graph) timed apart from the first Serialize call (JIT-dominated).
        // The pair matches the byte[] Serialize entry (MessagePackSerializer.cs).
        var answer = V4Data();
        var tR0 = Stopwatch.GetTimestamp();
        MessagePack.MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, StartupProbe.V4Models.Answer>();
        var tR1 = Stopwatch.GetTimestamp();
        var tA = Stopwatch.GetTimestamp();
        var bytes = MessagePack.MessagePackSerializer.Serialize(answer);
        var tB = Stopwatch.GetTimestamp();
        MessagePack.MessagePackSerializer.Serialize(answer);
        var tC = Stopwatch.GetTimestamp();
        Verify(bytes, answer.comments!.Count);
        var tM0 = Stopwatch.GetTimestamp();
        MessagePack.MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, StartupProbe.V4Models.MarginalPoco>();
        var tM1 = Stopwatch.GetTimestamp();
        // CSV reuse: firstDeserialize column carries the Answer resolve time, and
        // secondDeserialize the marginal resolve of an unrelated second graph
        Console.WriteLine($"v4resolve,{Us(tA, tB):F1},{Us(tB, tC):F1},{Us(tR0, tR1):F1},{Us(tM0, tM1):F1},{Us(t0, tC):F1}");
        break;
    }

    case "v3dyn":
    case "v3sg":
    {
        var options = mode == "v3sg"
            ? V3GeneratedModels.V3SourceGen.Options
            : V3::MessagePack.MessagePackSerializerOptions.Standard;
        var answer = V3Data();
        var tA = Stopwatch.GetTimestamp();
        var bytes = V3::MessagePack.MessagePackSerializer.Serialize(answer, options);
        var tB = Stopwatch.GetTimestamp();
        V3::MessagePack.MessagePackSerializer.Serialize(answer, options);
        var tC = Stopwatch.GetTimestamp();
        var back = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(bytes, options);
        var tD = Stopwatch.GetTimestamp();
        V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(bytes, options);
        var tE = Stopwatch.GetTimestamp();
        Verify(bytes, back!.comments!.Count);
        Console.WriteLine($"{mode},{Us(tA, tB):F1},{Us(tB, tC):F1},{Us(tC, tD):F1},{Us(tD, tE):F1},{Us(t0, tE):F1}");
        break;
    }

    default:
        throw new ArgumentException($"unknown mode '{mode}'");
}

return 0;

static double Us(long from, long to) => (to - from) * 1_000_000.0 / Stopwatch.Frequency;

static void Verify(byte[] bytes, int commentCount)
{
    if (bytes.Length == 0 || commentCount != 2)
    {
        throw new InvalidOperationException("round-trip verify failed");
    }
}

static StartupProbe.V4Models.Answer V4Data() => new()
{
    question_id = 100,
    answer_id = 4242,
    creation_date = new DateTime(2026, 8, 30, 1, 2, 3, DateTimeKind.Utc),
    last_activity_date = new DateTime(2026, 8, 30, 4, 5, 6, DateTimeKind.Utc),
    score = 17,
    is_accepted = true,
    body = "answer body with a moderately long text payload for realism",
    owner = new() { user_id = 7, display_name = "alice", reputation = 5000, user_type = StartupProbe.V4Models.UserType.registered, badge_counts = new() { gold = 1, silver = 2, bronze = 3 } },
    title = "How do I measure time to first serialize?",
    up_vote_count = 20,
    down_vote_count = 3,
    comments =
    [
        new() { comment_id = 1, post_id = 4242, score = 2, body = "first comment", owner = new() { user_id = 8, display_name = "bob" }, post_type = StartupProbe.V4Models.PostType.answer },
        new() { comment_id = 2, post_id = 4242, score = 5, body = "second comment", edited = true },
    ],
    link = "https://example.com/a/4242",
    tags = ["dotnet", "serialization", "benchmark"],
    comment_count = 2,
};

static V3GeneratedModels.Answer V3Data() => new()
{
    question_id = 100,
    answer_id = 4242,
    creation_date = new DateTime(2026, 8, 30, 1, 2, 3, DateTimeKind.Utc),
    last_activity_date = new DateTime(2026, 8, 30, 4, 5, 6, DateTimeKind.Utc),
    score = 17,
    is_accepted = true,
    body = "answer body with a moderately long text payload for realism",
    owner = new() { user_id = 7, display_name = "alice", reputation = 5000, user_type = V3GeneratedModels.UserType.registered, badge_counts = new() { gold = 1, silver = 2, bronze = 3 } },
    title = "How do I measure time to first serialize?",
    up_vote_count = 20,
    down_vote_count = 3,
    comments =
    [
        new() { comment_id = 1, post_id = 4242, score = 2, body = "first comment", owner = new() { user_id = 8, display_name = "bob" }, post_type = V3GeneratedModels.PostType.answer },
        new() { comment_id = 2, post_id = 4242, score = 5, body = "second comment", edited = true },
    ],
    link = "https://example.com/a/4242",
    tags = ["dotnet", "serialization", "benchmark"],
    comment_count = 2,
};

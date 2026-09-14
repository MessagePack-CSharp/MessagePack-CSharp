using System.Runtime.InteropServices;
using MessagePack.Tests.Robustness;
using SharpFuzz;

// Coverage-guided fuzzing (SharpFuzz + libFuzzer) over the shared robustness surface.
// This exe stays UNinstrumented; the code under test (MessagePack, MessagePack.LZ4,
// SerializerFoundation, and the Fuzz.Target assembly holding the generated formatters)
// gets IL-instrumented by the sharpfuzz tool. fuzz.ps1 orchestrates the whole thing.
//
// Input format: byte[0] selects the (target type x resolver tier) thunk from
// RobustnessTargets (mod the active set), the rest is the payload handed to Deserialize.
// One corpus therefore explores every tier at once, and libFuzzer learns the selector
// byte like any other. FUZZ_TARGET (exact name, name substring, or index) narrows the
// active set for a focused campaign; the selector byte is still consumed either way, so
// corpus files mean the same thing in every mode.
//
// Crash oracle = RobustnessHarness.IsSanctioned, same contract as the Robustness suite:
// sanctioned deserialization failures are swallowed here, anything else escapes into
// Fuzzer.LibFuzzer.Run and gets recorded as a crash with its input. Hangs and OOM are
// covered by libFuzzer itself (-timeout / -rss_limit_mb), so RunAttempt's
// thread-per-attempt deadline machinery is deliberately not used on this path.
//
// Modes:
//   (no args)       fuzzing loop under libfuzzer-dotnet (what fuzz.ps1 launches)
//   <file>          reproduce a finding: SharpFuzz's no-libFuzzer fallback runs the one
//                   input and lets the exception fly (set FUZZ_TARGET as in the campaign)
//   --seeds <dir>   write the multiplexed seed corpus and exit

// Instrumented branches write through SharpFuzz.Common.Trace.SharedMem, which stays a
// null pointer until Fuzzer.LibFuzzer.Run attaches the real shared memory. Everything
// this process does before/outside Run (--seeds mode, eager target resolution below)
// executes instrumented code too, so park the pointer on a dummy map first. Run replaces
// it with the real one; the 64KB is intentionally leaked for the process lifetime.
unsafe
{
    SharpFuzz.Common.Trace.SharedMem = (byte*)NativeMemory.AllocZeroed(65536);
}

if (args.Length >= 2 && args[0] == "--seeds")
{
    WriteSeeds(args[1]);
    return;
}

var active = ResolveActiveTargets();

Fuzzer.LibFuzzer.Run(span =>
{
    if (span.Length == 0)
    {
        return;
    }
    var (name, deserialize) = active[span[0] % active.Length];
    var payload = span[1..].ToArray();
    try
    {
        deserialize(payload);
    }
    catch (Exception ex) when (RobustnessHarness.IsSanctioned(ex) || ReportUnsanctioned(name, payload, ex))
    {
        // sanctioned failure: the contract allows it, swallow and move on.
        // ReportUnsanctioned always returns false, so an unsanctioned exception logs its
        // target context here and still propagates for libFuzzer to record as a crash.
    }
});

static (string Name, Action<byte[]> Deserialize)[] ResolveActiveTargets()
{
    var targets = RobustnessTargets.Targets;
    var spec = Environment.GetEnvironmentVariable("FUZZ_TARGET");
    if (string.IsNullOrEmpty(spec))
    {
        return targets;
    }
    if (int.TryParse(spec, out var index) && index >= 0 && index < targets.Length)
    {
        return [targets[index]];
    }
    var matches = targets.Where(t => t.Name.Contains(spec, StringComparison.OrdinalIgnoreCase)).ToArray();
    if (matches.Length == 0)
    {
        Console.Error.WriteLine($"FUZZ_TARGET '{spec}' matches nothing. Targets:");
        for (var i = 0; i < targets.Length; i++)
        {
            Console.Error.WriteLine($"  [{i}] {targets[i].Name}");
        }
        Environment.Exit(1);
    }
    return matches;
}

static bool ReportUnsanctioned(string name, byte[] payload, Exception ex)
{
    Console.Error.WriteLine($"UNSANCTIONED {ex.GetType().Name} on {name} payload {RobustnessHarness.Hex(payload)}");
    return false;
}

static void WriteSeeds(string dir)
{
    Directory.CreateDirectory(dir);
    var targets = RobustnessTargets.Targets;
    var seeds = RobustnessTargets.SeedCorpus();
    var written = 0;
    for (var t = 0; t < targets.Length; t++)
    {
        for (var s = 0; s < seeds.Length; s++)
        {
            byte[] input = [(byte)t, .. seeds[s]];
            File.WriteAllBytes(Path.Combine(dir, $"seed-t{t:D2}-s{s}.bin"), input);
            written++;
        }
    }
    Console.WriteLine($"{written} seed inputs ({targets.Length} targets x {seeds.Length} payloads) -> {dir}");
}

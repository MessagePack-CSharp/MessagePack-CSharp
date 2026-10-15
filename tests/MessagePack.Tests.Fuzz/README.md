# MessagePack.Tests.Fuzz

Coverage-guided fuzzing of the deserializer with SharpFuzz + libFuzzer.
This is the escalation tier above `MessagePack.Tests.Robustness`: same security contract, same target surface, same seed corpus (shared via compile-link from `RobustnessTargets`), but with real execution feedback steering input generation instead of blind randomization.

## Run

```powershell
# everything, until Ctrl+C (long soak is the point; hours are normal)
pwsh tests/MessagePack.Tests.Fuzz/fuzz.ps1

# bounded run (e.g. pre-release smoke, CI-ish)
pwsh tests/MessagePack.Tests.Fuzz/fuzz.ps1 -MaxTotalTimeSec 600

# focused campaign: FUZZ_TARGET is an exact target name, a substring, or an index
pwsh tests/MessagePack.Tests.Fuzz/fuzz.ps1 -Target lz4
pwsh tests/MessagePack.Tests.Fuzz/fuzz.ps1 -Target "SampleMapPoco/contractless"
```

First run downloads `libfuzzer-dotnet-windows.exe` (the native libFuzzer bridge, github.com/Metalnem/libfuzzer-dotnet) next to the script and restores the `sharpfuzz` local dotnet tool.

## What it does

1. `dotnet publish` the harness (Release) into `publish/`.
2. `sharpfuzz` IL-instruments the code under test: `MessagePack.dll`, `MessagePack.LZ4.dll`, `MessagePack.Zstandard.dll`, `SerializerFoundation.dll`, and `MessagePack.Tests.Fuzz.Target.dll` (which holds the sample models and their source-generated formatters). The harness exe and the SharpFuzz runtime stay uninstrumented.
3. Generates the seed corpus into `corpus/` if it is empty: every `RobustnessTargets.SeedCorpus()` payload, once per target, with the one-byte target selector prepended.
4. `libfuzzer-dotnet-windows.exe` drives the harness in persistent mode with `msgpack.dict` as the token dictionary.

Input format: `input[0]` selects the deserialize thunk, `input[1..]` is the payload. The thunks (`RobustnessTargets.Targets`) are every target type under every tier (generated, contractless, lenient, strict, DotNetOptimized, Typeless with AllowedTypes, the LZ4 Block / BlockArray envelopes, the LZ4 and Zstandard frames, WithFraming), plus the other entry points for five of the types: `DeserializeAsync(PipeReader)` and the message and element streams over a 5-byte-segmented pipe (the boundary scanner's resume paths), the non-seekable `Stream` entry, and `ConvertToJson`. A finding is any input whose deserialization escapes the sanctioned exception set (`RobustnessHarness.IsSanctioned`), hangs past `-timeout`, or blows `-rss_limit_mb`.

## Findings

libFuzzer writes `crash-*` / `timeout-*` / `oom-*` files into `findings/`; the harness also logs `UNSANCTIONED <ExceptionType> on <target> payload [hex]` to stderr just before the crash is recorded. Reproduce under a debugger-friendly single run:

```powershell
dotnet tests/MessagePack.Tests.Fuzz/publish/MessagePack.Tests.Fuzz.dll findings/crash-<hash>
```

Use the same `FUZZ_TARGET` environment value the campaign ran with (the selector byte indexes into the *filtered* target set). After fixing a bug, keep the crash input as a deterministic regression test in `MessagePack.Tests.Robustness`, like the nil-dictionary-key case.

`publish/`, `corpus/`, `findings/`, and the downloaded exe are gitignored; the corpus regenerates deterministically and grows per-machine.

## Campaign log

| Date | HEAD | Duration | Targets | Executions | Findings |
|---|---|---|---|---|---|
| 2026-10-14 | the F134 commit (expanded surface: 11 tiers x 13 types, pipe/messages/elements/stream entries for 5 types, JSON; 214 thunks) | 601 s bounded (`-MaxTotalTimeSec 600`), Windows x64, .NET 10 | all | 10,784,293 (about 18k exec/s), corpus grew to 3,095 inputs | none. The expanded targets' first *deterministic* run (RobustnessTests) had already found the double-advance in `DeserializeAsync(PipeReader)` after a parse failure, fixed in the same commit and pinned in RegressionTests. |
| 2026-10-15 | the nested-header bomb commit (SampleTreePoco target, bomb seeds plain and inside LZ4 / Zstandard / framing; 239 thunks x 26 seeds) | 421 s bounded, Windows x64, .NET 10 | all | 5,634,245 (about 13k exec/s), corpus 2,741 inputs | none. The deterministic pass (RegressionTests) already showed every thunk rejecting the bombs on the declared-element budget. |

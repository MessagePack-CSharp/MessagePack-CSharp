using MessagePack;
using NativeCompressions;
using Xunit;

#pragma warning disable CS0618 // v3 LZ4 envelopes are obsolete for new data; they are the subject here

namespace MessagePack.Tests.Robustness;

// Deterministic regression cases for contract breaks the randomized/coverage-guided passes
// have found and the source has since fixed. Each pins one specific malformed input so the
// fix cannot silently regress. New entries come from a SharpFuzz finding (see
// MessagePack.Tests.Fuzz): keep the crashing bytes, assert the input now fails only through
// the sanctioned exception set instead of a raw framework throw / OOM / hang.
public class RegressionTests
{
    static readonly MessagePackSerializerOptions Lz4Block = MessagePackSerializerOptions.Default.WithLz4Block(maxDecompressedSize: 1 << 20);

    // RobustnessTests (the pipe targets added 2026-10-14), first run. Five bytes that are not a framing envelope, read
    // with WithFraming through DeserializeAsync(PipeReader): the boundary scan found the value, the formatter refused
    // it, and the entry threw ArgumentOutOfRangeException('position') instead. The advance after the failed parse sat
    // in a finally, and the outer `catch when (!advanced)` filter is evaluated in the exception's first pass, before
    // inner finally blocks run, so it saw `advanced` still false and advanced the read a second time. Fix: a catch that
    // advances and rethrows, so the filter's second pass sees the flag.
    // Nerdbank.MessagePack GHSA-grf3-jgmf-2g5j (2026-09-17): nested array/map headers each claiming the remaining bytes
    // pass the per-header bound at every level and preallocate per level before the depth check stops the descent.
    // DeclaredElementBudgetTest pins the fix (the declared-element budget) on the Default chain's byte[] entry; this
    // runs the same bombs through every (type x tier x entry) thunk: each must fail sanctioned, and the synchronous,
    // processor-less ones must do so without amplifying (allocation measured on this thread, 16x the payload at most,
    // where the amplified path cost thousands of times the payload)
    [Fact]
    public void NestedHeaderBombs_AreRejectedByEveryTarget_WithoutAmplifying()
    {
        byte[][] bombs = [RobustnessTargets.NestedArrayBomb(4096, 200), RobustnessTargets.NestedMapBomb(4096, 200), RobustnessTargets.RecursiveTreeBomb(4096, 200)];
        string[] asyncOrStream = ["/pipe", "/messages", "/elements", "/stream"];
        string[] processorTiers = ["/lz4block", "/lz4array", "/lz4frame", "/zstd", "/framing"];
        foreach (var bomb in bombs)
        {
            foreach (var (name, deserialize) in RobustnessTargets.Targets)
            {
                var violation = RobustnessHarness.RunAttempt($"{name} {RobustnessHarness.Hex(bomb)}", () => deserialize(bomb));
                Assert.Null(violation);

                if (asyncOrStream.Any(name.Contains) || processorTiers.Any(name.Contains) || name == "json")
                {
                    continue; // continuations and pooled decode buffers are not this thread's allocation
                }
                Record.Exception(() => deserialize([MessagePackCode.Nil])); // warm the formatter chain
                var before = GC.GetAllocatedBytesForCurrentThread();
                var ex = Record.Exception(() => deserialize(bomb));
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.NotNull(ex);
                Assert.True(allocated < 16L * bomb.Length, $"{name}: rejected bomb allocated {allocated:N0} bytes for a {bomb.Length:N0} byte payload");
            }
        }
    }

    [Fact]
    public void NestedHeaderBomb_InsideAFrame_IsRejectedOnTheDecodedBudget()
    {
        // the budget is the decoded message's length, not the compressed frame's: a 4 KB bomb inside a frame must be
        // rejected the same way once decoded
        var bomb = RobustnessTargets.NestedArrayBomb(4096, 200);
        (string Tier, byte[] Payload)[] wrapped =
        [
            ("/framing", RobustnessTargets.Framed(bomb)),
            ("/lz4frame", LZ4.Compress(bomb, new LZ4CompressionOptions { ContentSize = 4096 })),
            ("/zstd", Zstandard.Compress(bomb, new ZstandardCompressionOptions { CompressionLevel = 3 })),
        ];
        foreach (var (tier, payload) in wrapped)
        {
            foreach (var (name, deserialize) in RobustnessTargets.Targets.Where(t => t.Name.EndsWith(tier, StringComparison.Ordinal) && (t.Name.StartsWith("Object[]/") || t.Name.StartsWith("SampleTreePoco/"))))
            {
                var ex = Record.Exception(() => deserialize(payload));
                Assert.NotNull(ex);
                Assert.True(RobustnessHarness.IsSanctioned(ex), $"{name}: {ex}");
                Assert.Contains("declared", ex.Message);
            }
        }
    }

    [Fact]
    public void FramingPipe_NonEnvelopeInput_FailsSanctioned()
    {
        byte[] payload = [0x0a, 0xe8, 0xc6, 0x68, 0xf7];
        var target = RobustnessTargets.Targets.Single(static t => t.Name == "SampleArrayPoco/framing/pipe");
        var ex = Record.Exception(() => target.Deserialize(payload));
        Assert.NotNull(ex);
        Assert.True(RobustnessHarness.IsSanctioned(ex), $"expected sanctioned, got {ex}");
    }

    // SharpFuzz, 2026-08-30. An LZ4 BlockArray envelope (ext 98) whose outer array32 header
    // claims ~934M blocks with a 4-byte sizes region behind it. TryDecode sized the per-block
    // owner array straight from the header count - `new byte[]?[blockCount]` - a ~7.5GB
    // allocation reached BEFORE the decode loop could reject the payload; under the memory
    // pressure it surfaced non-deterministically as a NullReferenceException from
    // ArrayPool.Return (pool state corrupted), an unsanctioned contract break. Fix: reject
    // blockCount > sizesLength (each block needs a >= 1-byte size prefix) before allocating.
    //
    // The guard is asserted by allocation, not by timing: a machine with enough address space
    // committed the 7.5GB array lazily and still threw a *sanctioned* InvalidEnvelope from the
    // decode loop within a couple of seconds, so "fails sanctioned, fast enough" passed on the
    // buggy code. What the bug actually is - a header-count-sized heap allocation on a rejected
    // input - is exactly what GC.GetAllocatedBytesForCurrentThread measures: the pre-fix path
    // requests ~7.5GB (counted whether or not it is physically committed) or OOMs; the fixed
    // path rejects the envelope having allocated essentially nothing.
    [Fact]
    public void Lz4BlockArray_HugeBlockCount_RejectedBeforeAllocating()
    {
        // exact bytes as found (the selector byte the fuzz harness prepends is stripped)
        byte[] payload =
        [
            0xdd, 0x37, 0xa7, 0x92, 0x4e, // array32, count = 933,728,846 -> blockCount ~934M
            0xc7, 0x04, 0x62,             // ext8, length 4, type 98 (Lz4BlockArray)
            0xcd, 0x04, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xa3, 0xdf, 0x55, 0x00, 0x6d,
        ];

        // measured on this thread (GetAllocatedBytesForCurrentThread is per-thread), so the
        // deserialize runs here, not on a RunAttempt background thread.
        var before = GC.GetAllocatedBytesForCurrentThread();
        var ex = Record.Exception(() => MessagePackSerializer.Deserialize<string>(payload, Lz4Block));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.NotNull(ex);
        Assert.True(RobustnessHarness.IsSanctioned(ex), $"expected sanctioned, got {ex}");
        // fixed path allocates a few KB of reader/options scaffolding; the bug allocated ~7.5GB.
        // 16MB is orders of magnitude above the former and below the latter.
        Assert.True(allocated < 16 * 1024 * 1024, $"rejected input allocated {allocated:N0} bytes (allocation-bomb guard breached)");
    }
}

using MessagePack;
using Xunit;

namespace MessagePack.Tests.Robustness;

// Deterministic regression cases for contract breaks the randomized/coverage-guided passes
// have found and the source has since fixed. Each pins one specific malformed input so the
// fix cannot silently regress. New entries come from a SharpFuzz finding (see
// MessagePack.Tests.Fuzz): keep the crashing bytes, assert the input now fails only through
// the sanctioned exception set instead of a raw framework throw / OOM / hang.
public class RegressionTests
{
    static readonly MessagePackSerializerOptions Lz4Block = MessagePackSerializerOptions.Default.WithLz4Block(maxDecompressedSize: 1 << 20);

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

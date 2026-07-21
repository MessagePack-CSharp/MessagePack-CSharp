using BenchmarkDotNet.Attributes;

// Representative micro for the narrow integer targets (byte/sbyte/uint16/int16). These
// were narrowing wrappers over the gate-5 hosts (TryReadUInt32/TryReadInt32) with a
// post-hoc fit compare; the dedicated treatment folds the range into the table body's
// single ok compare — (ulong)v <= max for unsigned (rejects negatives and overflow
// together), (ulong)(v - min) <= max - min for signed — via two shared AggressiveInlining
// cores specialized by constant arguments. TryReadUInt32 itself becomes the unsigned
// core at max = uint.MaxValue, deduplicating the body written in the previous round.
//   FixPos     - 0..127: fast-path dominant
//   UInt16     - 256..65535: single wide class
//   FieldCycle - fix -> uint8 -> uint16 cycle (POCO-like stable field classes)
//   Mixed      - random class mix
//
// MEASURED round 1 (wrapper-over-UInt32-host -> constant-specialized gate-5 core):
//   FieldCycle 1.93 -> 1.92 ns    FixPos 0.61 -> 0.54 ns (-11%)
//   Mixed      3.69 -> 3.59 ns    UInt16 2.64 -> 2.61 ns
// MEASURED round 2 (gate-5 core -> natural gate-3 mini-table, ExactWindow added):
//   Stream: FixPos 0.57 -> 0.58, FieldCycle 1.97 -> 1.97, Mixed 3.61 -> 3.69,
//           UInt16 2.54 -> 2.83 (+11%, noisy iteration — band-edge)
//   Exact:  Mixed 7.88 -> 7.13 (-10%), UInt16 2.59 -> 2.13 (-18%),
//           FixPos 0.96 -> 1.03, FieldCycle 2.50 -> 2.62 (len-2 windows bridge cold)
// VERDICT: adopted — stream regime is parity, exact-window (trailing fields, sequence
// exact-requirement) wins where it was worst. PITFALL re-learned on the way: the first
// mini-table draft bias-checked fixint via cmov, making tokenSize data-dependent —
// FixPos went 0.57 -> 2.24 (4x, the consumed->next-address chain). The fast path's
// tokenSize must be a CONSTANT: unsigned targets check code <= 0x7f, signed use the
// wrap check, both with unconditional Success.
// MEASURED round 3 (checked BinaryPrimitives.ReadXxxBigEndian in the cascades/cold
// paths -> UnsafeRead*BigEndian, the guard-dominated unchecked loads):
//   Stream: FieldCycle 1.97 -> 1.84, FixPos 0.58 -> 0.55, Mixed 3.69 -> 3.91 (band),
//           UInt16 2.83 -> 2.58 (round-2's band-edge +11% dissolved)
//   Exact:  FieldCycle 2.62 -> 2.16 (-18%), FixPos 1.03 -> 0.77 (-25%),
//           UInt16 2.13 -> 1.93 (-9%), Mixed 7.13 -> 7.27 (band)
// The cold cascades are hit by every message's trailing fields, so their internal
// length checks (uneliminable through Slice, see TryReadSingle) were a real cost.
public class TryReadUInt16Benchmark
{
    const int Count = 100_000;

    [Params("FixPos", "UInt16", "FieldCycle", "Mixed")]
    public string Class = "FieldCycle";

    byte[] buffer = default!;
    int[] offsets = default!;
    int[] lengths = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        int i = 0;
        ushort Next() => Class switch
        {
            "FixPos" => (ushort)rand.Next(0, 128),
            "UInt16" => (ushort)rand.Next(256, 65536),
            "FieldCycle" => (i++ % 3) switch
            {
                0 => (ushort)rand.Next(0, 128),
                1 => (ushort)rand.Next(128, 256),
                _ => (ushort)rand.Next(256, 65536),
            },
            _ => rand.Next(3) switch
            {
                0 => (ushort)rand.Next(0, 128),
                1 => (ushort)rand.Next(128, 256),
                _ => (ushort)rand.Next(256, 65536),
            },
        };

        buffer = new byte[Count * 3 + 8];
        offsets = new int[Count];
        lengths = new int[Count];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            UltraMessagePack.MessagePackPrimitives.TryWriteUInt16(buffer.AsSpan(offset), Next(), out var written);
            offsets[n] = offset;
            lengths[n] = written;
            offset += written;
        }

        // library and MessagePack-CSharp must agree on every message
        int o0 = 0, o1 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadUInt16(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = MessagePack.MessagePackPrimitives.TryReadUInt16(buffer.AsSpan(o1), out var v1, out var c1);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != MessagePack.MessagePackPrimitives.DecodeResult.Success
                || v0 != v1 || c0 != c1)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1;
        }

        // contract spot-checks across the whole narrow family
        Span<byte> probe = stackalloc byte[9];
        probe.Clear();
        probe[0] = 0xcd; probe[1] = 0x01; // uint16(256)
        if (UltraMessagePack.MessagePackPrimitives.TryReadByte(probe, out _, out var tb) != UltraMessagePack.DecodeResult.TokenMismatch || tb != 0)
        {
            throw new InvalidOperationException("verify failed: byte overflow");
        }
        probe[0] = 0xe0; // -32
        if (UltraMessagePack.MessagePackPrimitives.TryReadSByte(probe, out var sv, out var ts) != UltraMessagePack.DecodeResult.Success || sv != -32 || ts != 1)
        {
            throw new InvalidOperationException("verify failed: sbyte negative fixint");
        }
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt16(probe, out _, out var tu) != UltraMessagePack.DecodeResult.TokenMismatch || tu != 0)
        {
            throw new InvalidOperationException("verify failed: uint16 negative");
        }
        probe[0] = 0xcf; probe[1] = 0; probe[8] = 200; // uint64(200): wide but fits byte
        probe.Slice(1, 7).Clear();
        if (UltraMessagePack.MessagePackPrimitives.TryReadByte(probe, out var wb, out var wt) != UltraMessagePack.DecodeResult.Success || wb != 200 || wt != 9)
        {
            throw new InvalidOperationException("verify failed: wide byte");
        }
        probe[0] = 0xd1; // int16 needs 3
        for (int cut = 1; cut < 3; cut++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadInt16(probe[..cut], out _, out var tc) != UltraMessagePack.DecodeResult.InsufficientBuffer || tc != 3)
            {
                throw new InvalidOperationException($"verify failed: truncation cut={cut}");
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public uint Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        uint sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt16(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    // every token read from a window of exactly its own size — the trailing-field /
    // exact-requirement regime the big-buffer loop cannot observe
    [Benchmark(OperationsPerInvoke = Count)]
    public uint UltraExact()
    {
        ReadOnlySpan<byte> buf = buffer;
        uint sum = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt16(buf.Slice(offsets[n], lengths[n]), out var v, out _) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint Mpcs()
    {
        ReadOnlySpan<byte> buf = buffer;
        uint sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadUInt16(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}

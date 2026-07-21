using BenchmarkDotNet.Attributes;

// Can TryReadUInt64 (currently a plain compare cascade) reuse the Int64ReadTable shape?
// The bit16 flag marks the uint64(cf) format; the Int64 target uses it as "must decode
// non-negative" while a UInt64 target flips the predicate to "always valid" — everything
// else (8-byte unconditional load, rawShift, sign-extension) is shared. Fast path is
// positive fixint only (code <= 0x7f); negative fixint falls to the range check, which
// rejects it for free. TryReadByte/UInt16/UInt32/Char all narrow over TryReadUInt64, so
// they inherit whatever this measures.
//   FixPos     - 0..127: fast-path dominant
//   UInt32     - values 65536..uint.Max: single wide class
//   FieldCycle - fix -> uint8 -> uint16 -> uint32 cycle (POCO-like stable field classes)
//   Mixed      - adversarial class mix incl. uint64
//
// MEASURED (Zen 5, ShortRun; Ultra cascade -> Ultra table, Mpcs for context):
//   FieldCycle 2.50 -> 2.22 ns (-11%; Mpcs 2.17)
//   FixPos     1.77 -> 0.47 ns (3.7x;  Mpcs 0.73)
//   Mixed      9.81 -> 3.26 ns (3.0x;  Mpcs 9.64)
//   UInt32     2.31 -> 2.85 ns (+24%;  Mpcs 0.97)
// VERDICT: adopted. Same trade the Int32 round settled: the flat table loses only on
// CONTIGUOUS mono-class streams (UInt32 row — a predicted cascade speculates consumed,
// the table's consumed is chain-bound at ~14cyc), and wins everywhere data varies.
// TryReadByte/UInt16/UInt32/Char narrow over this and inherit the gains.
public class TryReadUInt64Benchmark
{
    const int Count = 100_000;

    [Params("FixPos", "UInt32", "FieldCycle", "Mixed")]
    public string Class = "FieldCycle";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        int i = 0;
        ulong Next() => Class switch
        {
            "FixPos" => (ulong)rand.Next(0, 128),
            "UInt32" => (ulong)rand.Next(65536, int.MaxValue) * 2,
            "FieldCycle" => (i++ & 3) switch
            {
                0 => (ulong)rand.Next(0, 128),
                1 => (ulong)rand.Next(128, 256),
                2 => (ulong)rand.Next(256, 65536),
                _ => (ulong)rand.Next(65536, int.MaxValue),
            },
            _ => rand.Next(5) switch
            {
                0 => (ulong)rand.Next(0, 128),
                1 => (ulong)rand.Next(128, 256),
                2 => (ulong)rand.Next(256, 65536),
                3 => (ulong)rand.Next(65536, int.MaxValue),
                _ => ulong.MaxValue - (ulong)rand.Next(0, 1 << 20),
            },
        };

        buffer = new byte[Count * 9 + 8];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            UltraMessagePack.MessagePackPrimitives.TryWriteUInt64(buffer.AsSpan(offset), Next(), out var written);
            offset += written;
        }

        // library and MessagePack-CSharp must agree on every message
        int o0 = 0, o1 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadUInt64(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = MessagePack.MessagePackPrimitives.TryReadUInt64(buffer.AsSpan(o1), out var v1, out var c1);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != MessagePack.MessagePackPrimitives.DecodeResult.Success
                || v0 != v1 || c0 != c1)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1;
        }

        // contract spot-checks: negative encodings never fit, truncation reports exactly
        Span<byte> probe = stackalloc byte[9];
        probe.Clear();
        probe[0] = 0xe0; // negative fixint
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt64(probe, out _, out var tn) != UltraMessagePack.DecodeResult.TokenMismatch || tn != 0)
        {
            throw new InvalidOperationException("verify failed: negative fixint");
        }
        probe[0] = 0xd0; // int8
        probe[1] = 0xff; // -1
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt64(probe, out _, out var ti) != UltraMessagePack.DecodeResult.TokenMismatch || ti != 0)
        {
            throw new InvalidOperationException("verify failed: negative int8");
        }
        probe[0] = 0xcf; // uint64 needs 9
        for (int cut = 1; cut < 9; cut++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt64(probe[..cut], out _, out var tc) != UltraMessagePack.DecodeResult.InsufficientBuffer || tc != 9)
            {
                throw new InvalidOperationException($"verify failed: truncation cut={cut}");
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public ulong Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        ulong sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt64(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ulong Mpcs()
    {
        ReadOnlySpan<byte> buf = buffer;
        ulong sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadUInt64(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}

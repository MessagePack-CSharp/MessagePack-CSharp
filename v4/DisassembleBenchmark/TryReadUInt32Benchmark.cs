using BenchmarkDotNet.Attributes;

// TryReadUInt32 was a narrowing wrapper over TryReadUInt64 — which gates at Length >= 9,
// so 5..8-byte windows (buffer tails, exact-requirement sequence windows) dropped to the
// cascade even though a complete 5-byte uint32 token was present. The fix mirrors the
// Int64/UInt64 pairing: reuse Int32ReadTable wholesale (4-byte load, >= 5 gate, R32W8
// routes 9-byte formats to a cold path) with the validity predicate flipped to just
// v >= 0 — inline formats can never exceed uint.MaxValue, so rejecting negatives is the
// whole check. The nine-byte cold path is raw <= uint.MaxValue, covering cf (raw bits
// are the value) and d3 (negatives alias to huge ulongs) in one compare.
// TryReadByte/UInt16 narrow over this (gate 5 host); TryReadSByte/Int16 over TryReadInt32.
//
// MEASURED (Zen 5, ShortRun; Ultra narrowing-over-UInt64 -> dedicated gate-5 body):
//   FieldCycle 2.14 -> 2.02 ns (-5%)    FixPos 0.56 -> 0.45 ns (-19%)
//   Mixed      3.40 -> 3.13 ns (-8%)    UInt32 2.67 -> 2.64 ns (-1%)
// VERDICT: adopted — modest micro wins on top of the structural fix (correct fast-path
// coverage of 5..8-byte windows, which a large-buffer micro bench cannot even observe;
// the 8-byte load and the post-hoc fit compare are also gone from the inline path).
// The UInt32 mono-class row keeps the known flat-table trade (Mpcs cascade speculates
// consumed at 0.85; see TryReadUInt64Benchmark's verdict).
public class TryReadUInt32Benchmark
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
        uint Next() => Class switch
        {
            "FixPos" => (uint)rand.Next(0, 128),
            "UInt32" => (uint)rand.Next(65536, int.MaxValue) * 2,
            "FieldCycle" => (i++ & 3) switch
            {
                0 => (uint)rand.Next(0, 128),
                1 => (uint)rand.Next(128, 256),
                2 => (uint)rand.Next(256, 65536),
                _ => (uint)rand.Next(65536, int.MaxValue),
            },
            _ => rand.Next(4) switch
            {
                0 => (uint)rand.Next(0, 128),
                1 => (uint)rand.Next(128, 256),
                2 => (uint)rand.Next(256, 65536),
                _ => uint.MaxValue - (uint)rand.Next(0, 1 << 20),
            },
        };

        buffer = new byte[Count * 5 + 8];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            UltraMessagePack.MessagePackPrimitives.TryWriteUInt32(buffer.AsSpan(offset), Next(), out var written);
            offset += written;
        }

        // library and MessagePack-CSharp must agree on every message
        int o0 = 0, o1 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadUInt32(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = MessagePack.MessagePackPrimitives.TryReadUInt32(buffer.AsSpan(o1), out var v1, out var c1);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != MessagePack.MessagePackPrimitives.DecodeResult.Success
                || v0 != v1 || c0 != c1)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1;
        }

        // contract spot-checks: negatives rejected, wide encodings accepted in range,
        // truncation reports exactly
        Span<byte> probe = stackalloc byte[9];
        probe.Clear();
        probe[0] = 0xe0; // negative fixint
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt32(probe, out _, out var tn) != UltraMessagePack.DecodeResult.TokenMismatch || tn != 0)
        {
            throw new InvalidOperationException("verify failed: negative fixint");
        }
        probe[0] = 0xcf; // uint64(42): wide but fits
        probe[8] = 42;
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt32(probe, out var wv, out var wt) != UltraMessagePack.DecodeResult.Success || wv != 42 || wt != 9)
        {
            throw new InvalidOperationException("verify failed: wide in-range");
        }
        probe[0] = 0xd3; // int64(-1): negative
        probe.Slice(1).Fill(0xff);
        if (UltraMessagePack.MessagePackPrimitives.TryReadUInt32(probe, out _, out var dt) != UltraMessagePack.DecodeResult.TokenMismatch || dt != 0)
        {
            throw new InvalidOperationException("verify failed: wide negative");
        }
        probe.Clear();
        probe[0] = 0xce; // uint32 needs 5
        for (int cut = 1; cut < 5; cut++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt32(probe[..cut], out _, out var tc) != UltraMessagePack.DecodeResult.InsufficientBuffer || tc != 5)
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
            if (UltraMessagePack.MessagePackPrimitives.TryReadUInt32(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
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
            if (MessagePack.MessagePackPrimitives.TryReadUInt32(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}

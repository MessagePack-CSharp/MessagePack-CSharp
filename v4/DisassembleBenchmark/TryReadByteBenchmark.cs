using BenchmarkDotNet.Attributes;

// The narrow targets' natural encoding domain is tiny — a minimal writer emits ONLY
// fixint/uint8 for byte values — so the gate-5 core "rounds up": any window of 2..4
// bytes holding a COMPLETE uint8 token still drops to the ShortBuffer cascade. That is
// not a rare tail: a trailing byte field of any message sees remaining == 2 every time.
// The dedicated shape gates at the natural width instead: fixint fast path, then
// Length >= 2 && code == 0xcc inline (no range check needed — uint8 ⊆ byte), everything
// else (wide compat encodings, true tails) through a NoInlining bridge to the gate-5
// core. Stream = big-buffer regime; ExactWindow = every token read from a window of
// exactly its own size (trailing-field / exact-requirement regime).
//
// MEASURED (Zen 5, ShortRun; gate-5 core -> natural gate-2 shape):
//   Stream Random 4.05 -> 3.19 ns (-21%)   Stream UInt8 2.59 -> 0.55 ns (4.7x)
//   Exact  Random 4.96 -> 3.90 ns (-21%)   Exact  UInt8 2.35 -> 0.92 ns (2.6x)
// VERDICT: adopted, wins everywhere. The single code compare replaces the whole table
// machinery for the dominant class, and gate 2 keeps trailing byte fields inline.
public class TryReadByteBenchmark
{
    const int Count = 100_000;

    [Params("Random", "UInt8")]
    public string Class = "Random";

    byte[] buffer = default!;
    int[] offsets = default!;
    int[] lengths = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        byte Next() => Class switch
        {
            "UInt8" => (byte)rand.Next(128, 256),
            _ => (byte)rand.Next(0, 256),
        };

        buffer = new byte[Count * 2 + 8];
        offsets = new int[Count];
        lengths = new int[Count];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            UltraMessagePack.MessagePackPrimitives.TryWriteByte(buffer.AsSpan(offset), Next(), out var written);
            offsets[n] = offset;
            lengths[n] = written;
            offset += written;
        }

        int o0 = 0, o1 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadByte(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = MessagePack.MessagePackPrimitives.TryReadByte(buffer.AsSpan(o1), out var v1, out var c1);
            var r2 = UltraMessagePack.MessagePackPrimitives.TryReadByte(buffer.AsSpan(offsets[n], lengths[n]), out var v2, out var c2);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != MessagePack.MessagePackPrimitives.DecodeResult.Success
                || r2 != UltraMessagePack.DecodeResult.Success || v0 != v1 || v0 != v2 || c0 != c1 || c0 != c2)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public uint Stream()
    {
        ReadOnlySpan<byte> buf = buffer;
        uint sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadByte(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint ExactWindow()
    {
        ReadOnlySpan<byte> buf = buffer;
        uint sum = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadByte(buf.Slice(offsets[n], lengths[n]), out var v, out _) == UltraMessagePack.DecodeResult.Success)
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
            if (MessagePack.MessagePackPrimitives.TryReadByte(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}

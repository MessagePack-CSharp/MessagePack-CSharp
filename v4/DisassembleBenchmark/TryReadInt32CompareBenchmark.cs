using BenchmarkDotNet.Attributes;

// UltraMessagePack.MessagePackPrimitives.TryReadInt32 vs the ecosystem's primitives:
// MessagePack-CSharp v3 (MessagePack.MessagePackPrimitives.TryReadInt32) and
// Nerdbank.MessagePack (MessagePackPrimitives.TryRead(out int)) — all three are
// DecodeResult-returning span primitives, so this is an apples-to-apples decode-loop
// comparison (offset += consumed serial chain, 100k messages so the predictor cannot
// memorize; the buffer keeps >= 8 bytes of tail slack so every read sees the fast-path
// window). Homogeneous per-class streams (fix/int8/int16/int32, signed and unsigned
// halves) show the best case for cascade-style decoders (perfectly predicted class
// branch); Mixed shows the branchless table's home turf.
//
// MEASURED (Zen 5, calm run): the two competitors are byte-identical twins (~same asm),
//   homogeneous classes: MPCS/Nerdbank 0.82-1.00ns vs Ultra flat 2.57ns — cascade WINS 3x
//   Mixed:               MPCS 9.18 / Nerdbank 9.90 vs Ultra 2.58ns  — Ultra WINS 3.5-3.8x
// The deep reason: a PREDICTED class branch lets the CPU speculate bytesConsumed and break
// the consumed->next-address serial chain (0.85ns ~= 4cyc/msg!); the branchless table can
// never speculate — it carries the full ~13cyc data dependency on every message, hot or
// not. So "15cyc/element" was the floor of the BRANCHLESS design, not of decoding. Real
// POCO streams are per-field-callsite class-stable (closer to homogeneous), so the pure
// branchless winner deserves a rematch against cascade/hybrid shapes under per-callsite
// conditions before it keeps its crown.
public class TryReadInt32CompareBenchmark
{
    const int Count = 100_000;

    [Params("FixPos", "FixNeg", "Int8", "UInt8", "Int16", "UInt16", "Int32", "UInt32", "Mixed")]
    public string Class = "Mixed";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        int Next() => Class switch
        {
            "FixPos" => rand.Next(0, 128),
            "FixNeg" => rand.Next(-32, 0),
            "Int8" => rand.Next(-128, -32),
            "UInt8" => rand.Next(128, 256),
            "Int16" => rand.Next(-32768, -128),
            "UInt16" => rand.Next(256, 65536),
            "Int32" => rand.Next(int.MinValue, -32768),
            "UInt32" => rand.Next(65536, int.MaxValue),
            _ => rand.Next(8) switch
            {
                0 => rand.Next(0, 128),
                1 => rand.Next(-32, 0),
                2 => rand.Next(-128, -32),
                3 => rand.Next(128, 256),
                4 => rand.Next(-32768, -128),
                5 => rand.Next(256, 65536),
                6 => rand.Next(int.MinValue, -32768),
                _ => rand.Next(65536, int.MaxValue),
            },
        };

        buffer = new byte[Count * 5 + 8]; // tail slack keeps every message inside the >= 5 fast window
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryWriteInt32Cascade(buffer.AsSpan(offset), Next(), out var written);
            offset += written;
        }

        // the three implementations must agree on every message before speed means anything
        int ou = 0, om = 0, on = 0;
        for (int i = 0; i < Count; i++)
        {
            var ru = UltraMessagePack.MessagePackPrimitives.TryReadInt32(buffer.AsSpan(ou), out var vu, out var cu);
            var rm = MessagePack.MessagePackPrimitives.TryReadInt32(buffer.AsSpan(om), out var vm, out var cm);
            var rn = global::Nerdbank.MessagePack.MessagePackPrimitives.TryRead(buffer.AsSpan(on), out int vn, out var cn);
            if (ru != UltraMessagePack.DecodeResult.Success || rm != MessagePack.MessagePackPrimitives.DecodeResult.Success || rn != Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult.Success
                || vu != vm || vu != vn || cu != cm || cu != cn)
            {
                throw new InvalidOperationException($"verify failed at message {i}: ultra=({ru},{vu},{cu}) mpcs=({rm},{vm},{cm}) nb=({rn},{vn},{cn})");
            }
            ou += cu;
            om += cm;
            on += cn;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadInt32(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int MessagePackCSharp()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt32(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int NerdbankMsgPack()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            if (global::Nerdbank.MessagePack.MessagePackPrimitives.TryRead(buf.Slice(offset), out int v, out var consumed) == Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}

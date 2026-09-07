using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using static MessagePack.MessagePackPrimitives;

// Round 10: hoisting the write-buffer guard ACROSS array elements.
// Round 9 settled the per-object shape (one GetReference(sum-of-max) + offset chain + one Advance, -11..16%)
// and the ObjectEmitter ships it, so a T[] serialize today still touches buffer state twice per element
// (GetReference + Advance). The remaining hypothesis is amortizing that residual over element runs:
// a type-level worst case (all-primitive POCO => const 16B/element) lets one reservation cover a whole
// chunk of elements, collapsing 2 buffer touches per element to 2 per chunk. Type-level bounds stay
// mutation-safe under concurrent writers (an int field can never encode past 5 bytes no matter how it is
// torn), unlike value-dependent sizes, so the unchecked run needs no re-validation.
//   PerObject (baseline) - the shipped generated shape: per element GetReference(16) + chain + Advance
//   Chunk64 / Chunk256   - one GetReference(n*16) + one Advance per chunk (256 elems = 4KB reservation)
//   WholeArray           - one reservation for header + every element (100k elems = 1.6MB sizeHint:
//                          measures the ceiling and the cost of forcing a full-size rent upfront)
// Distributions (element count 100k per CLAUDE.md's predictor pitfall):
//   Fixint     - every field 0..127: 1-byte writes, width branches fully predictable
//   MixedWidth - fields uniformly drawn from the 1/2/3/5-byte encoding classes: width branches random
// Setup is self-verifying (every candidate byte-identical to the checked public-API oracle);
// VerifyCandidates() sweeps boundary element counts on both distributions.
//
// MEASURED (ShortRun x2, consistent): Fixint Chunk64/256 0.88-0.89, WholeArray 0.83; MixedWidth
// everything 0.96-1.01 (tie band). The per-element GetReference+Advance residual is ~0.23ns/elem
// (1.36 -> 1.13ns/elem at the WholeArray ceiling) and only shows when width branches predict;
// under random widths mispredicts (~750us vs ~136us for the same byte count) swamp the bookkeeping
// entirely. The first run's Chunk 0.95 was the values[i + k] bounds check, not the hoist: the span
// slice recovered it. Disposition: the mechanism is real but capped at ~-12% in the best case,
// zero in the adversarial one, and needs a new element-formatter surface (raw write-at-ref that
// returns size) plus a fixed-max-size type gate; the per-OBJECT fused run (round 9, shipped in
// ObjectEmitter) already banked the bulk of this family's win. Not adopted.
public class ElementGuardHoistBenchmark
{
    [Params("Fixint", "MixedWidth")]
    public string Distribution = "Fixint";

    const int ElementCount = 100_000;
    const int ElemMax = 1 + 3 * MaxInt32Length; // fixarray(3) header + 3 int worst cases

    HoistPoint3[] data = null!;

    [GlobalSetup]
    public void Setup()
    {
        data = BuildData(Distribution, ElementCount);

        var expected = SerializeOracle(data);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(PerObject), Run(SerializePerObject, data)),
            (nameof(Chunk64), Run((ref ArrayPoolListWriteBuffer b, HoistPoint3[] v) => SerializeChunked(ref b, v, 64), data)),
            (nameof(Chunk256), Run((ref ArrayPoolListWriteBuffer b, HoistPoint3[] v) => SerializeChunked(ref b, v, 256), data)),
            (nameof(WholeArray), Run(SerializeWhole, data)),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException($"{name} wire mismatch on {Distribution}");
            }
        }
    }

    static HoistPoint3[] BuildData(string distribution, int count)
    {
        var rand = new Random(42);
        int Next() => distribution switch
        {
            "Fixint" => rand.Next(0, 128),
            // one value per encoding class, uniformly: fixint(1B), uint8/int8(2B), uint16/int16(3B), uint32/int32(5B)
            "MixedWidth" => rand.Next(8) switch
            {
                0 => rand.Next(0, 128),
                1 => rand.Next(-32, 0),
                2 => rand.Next(128, 256),
                3 => rand.Next(-128, -32),
                4 => rand.Next(256, 65536),
                5 => rand.Next(-32768, -128),
                6 => rand.Next(65536, int.MaxValue),
                _ => rand.Next(int.MinValue, -32768),
            },
            _ => throw new InvalidOperationException(distribution),
        };
        var data = new HoistPoint3[count];
        for (int i = 0; i < count; i++)
        {
            data[i] = new HoistPoint3 { X = Next(), Y = Next(), Z = Next() };
        }
        return data;
    }

    // ---- benchmarks -------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long PerObject()
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            SerializePerObject(ref buffer, data);
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Benchmark]
    public long Chunk64()
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            SerializeChunked(ref buffer, data, 64);
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Benchmark]
    public long Chunk256()
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            SerializeChunked(ref buffer, data, 256);
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Benchmark]
    public long WholeArray()
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            SerializeWhole(ref buffer, data);
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    // ---- shapes -----------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SerializePerObject(ref ArrayPoolListWriteBuffer buffer, HoistPoint3[] values)
    {
        buffer.WriteArrayHeader(values.Length);
        foreach (var v in values)
        {
            ref var d = ref buffer.GetReference(ElemMax);
            var w = UnsafeWriteFixArrayHeader(ref d, 3);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.X);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Y);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Z);
            buffer.Advance(w);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SerializeChunked(ref ArrayPoolListWriteBuffer buffer, HoistPoint3[] values, int chunkElems)
    {
        buffer.WriteArrayHeader(values.Length);
        int i = 0;
        while (i < values.Length)
        {
            int n = Math.Min(chunkElems, values.Length - i);
            ref var d = ref buffer.GetReference(n * ElemMax);
            var w = 0;
            // span slice kills the values[i + k] bounds check the JIT cannot fold across the chunk arithmetic
            foreach (var v in values.AsSpan(i, n))
            {
                w += UnsafeWriteFixArrayHeader(ref Unsafe.Add(ref d, w), 3);
                w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.X);
                w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Y);
                w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Z);
            }
            buffer.Advance(w);
            i += n;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SerializeWhole(ref ArrayPoolListWriteBuffer buffer, HoistPoint3[] values)
    {
        ref var d = ref buffer.GetReference(MaxArrayHeaderLength + values.Length * ElemMax);
        var w = UnsafeWriteArrayHeader(ref d, values.Length);
        foreach (var v in values)
        {
            w += UnsafeWriteFixArrayHeader(ref Unsafe.Add(ref d, w), 3);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.X);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Y);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), v.Z);
        }
        buffer.Advance(w);
    }

    // ---- verification -----------------------------------------------------

    internal delegate void SerializeShape(ref ArrayPoolListWriteBuffer buffer, HoistPoint3[] values);

    internal static byte[] Run(SerializeShape shape, HoistPoint3[] values)
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            shape(ref buffer, values);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    // independent oracle through the checked public write API
    static byte[] SerializeOracle(HoistPoint3[] values)
    {
        Span<byte> scratch = stackalloc byte[512];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            buffer.WriteArrayHeader(values.Length);
            foreach (var v in values)
            {
                buffer.WriteArrayHeader(3);
                buffer.WriteInt32(v.X);
                buffer.WriteInt32(v.Y);
                buffer.WriteInt32(v.Z);
            }
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public static void VerifyCandidates()
    {
        foreach (var distribution in new[] { "Fixint", "MixedWidth" })
        {
            // chunk boundaries (64/256) and fixarray/array16/array32 header boundaries
            foreach (var count in new[] { 0, 1, 15, 16, 17, 63, 64, 65, 255, 256, 257, 65535, 65536 })
            {
                var values = BuildData(distribution, count);
                var expected = SerializeOracle(values);
                foreach (var (name, shape) in new (string, SerializeShape)[]
                {
                    (nameof(SerializePerObject), SerializePerObject),
                    ("SerializeChunked64", (ref ArrayPoolListWriteBuffer b, HoistPoint3[] v) => SerializeChunked(ref b, v, 64)),
                    ("SerializeChunked256", (ref ArrayPoolListWriteBuffer b, HoistPoint3[] v) => SerializeChunked(ref b, v, 256)),
                    (nameof(SerializeWhole), SerializeWhole),
                })
                {
                    var actual = Run(shape, values);
                    if (!actual.AsSpan().SequenceEqual(expected))
                    {
                        throw new InvalidOperationException($"{name} mismatch: {distribution} count={count}");
                    }
                }
            }
        }
    }
}

public sealed class HoistPoint3
{
    public int X;
    public int Y;
    public int Z;
}

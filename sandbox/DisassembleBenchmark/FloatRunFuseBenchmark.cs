using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;
using SerializerFoundation;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Read-side fusion over a run of FIXED-WIDTH tokens (float members of an int-key POCO).
// PocoPerValueVsBatchBenchmark round 9 found the read batch flat for int members (1.01 / 1.02): token sizes are
// data-dependent there, so the consumed -> next-address chain survives batching. A float32 token is always
// [0xca][4B BE] from a conforming writer, so a run of N float members has a compile-time layout: one window check
// (N * 5 bytes), one folded code check ((s[0] ^ 0xca) | (s[5] ^ 0xca) | ...) == 0, N loads at constant offsets with
// no dependency between them, one Advance. Anything else (short window, a member coded as float64 / int by a
// foreign writer, a different arity) takes the shipped per-value path, which nothing has consumed from yet.
// The equivalence is exact: on the hit path every per-value read would have taken its Float32 arm and consumed 5.
//
//   Loop (baseline) - the shipped generated shape: ReadArrayHeader + for / switch (i) with ReadSingle per member
//   FusedExact      - `count == N` + fused run, anything else falls back out of line
//   FusedThenLoop   - `count >= N` + fused run, then the shared for / switch continues from i = N (extras are
//                     skipped inline); a miss runs the same loop from i = 0. The shape a generator would emit.
//
// Arity 3 (Vector3-like) and 8. Distributions (>= 180k tokens per run, distinct payloads, see the predictor
// pitfall the other benchmarks cite):
//   SameVersion  - every object has exactly N members
//   RareMismatch - 1/64 objects carry one appended member (rolling upgrade)
//   MixedVersion - random N-1 / N / N+1 per object: adversarial for the count branch
// Setup cross-checks every shape's checksum; VerifyCandidates() (wired into --verify) sweeps arity 0..N+3,
// special values, members coded as float64 / int, and every truncation length.
//
// MEASURED (i7-13700KF, .NET 11, ShortRun x2, ratio to Loop):
//   F3 SameVersion  FusedExact 0.50 / 0.48   FusedThenLoop 0.52 / 0.51   (4.6 -> 2.3 ns/object)
//   F3 RareMismatch FusedExact 0.48 / 0.49   FusedThenLoop 0.49 / 0.48
//   F3 MixedVersion FusedExact 1.01 / 1.01   FusedThenLoop 0.90 / 0.86
//   F8 SameVersion  FusedExact 0.34 / 0.36   FusedThenLoop 0.35 / 0.37   (10.8 -> 3.7 ns/object)
//   F8 RareMismatch FusedExact 0.34 / 0.37   FusedThenLoop 0.34 / 0.36
//   F8 MixedVersion FusedExact 0.90 / 0.91   FusedThenLoop 0.63 / 0.67
// The per-value loop costs ~1.1-1.3 ns per float although its tokenSize is already a constant: what is left is the
// buffer's consumed store -> load chain plus one window check and one Advance per member (CursorReadProbeBenchmark's
// address-exposure note), and the fused run removes all of it. FusedThenLoop never loses, FusedExact gives the win
// back under version drift (the count branch mispredicts and the fallback is a call). End-to-end numbers, where the
// entry sequence and the allocation dilute this, are in FloatRunFuseEndToEndBenchmark.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FloatRunFuseBenchmark
{
    [Params("SameVersion", "RareMismatch", "MixedVersion")]
    public string Distribution = "SameVersion";

    const int ObjectCount3 = 50_000;
    const int ObjectCount8 = 20_000;

    byte[] blob3 = null!;
    byte[] blob8 = null!;
    readonly Float3 poco3 = new();
    readonly Float8 poco8 = new();

    [GlobalSetup]
    public void Setup()
    {
        blob3 = BuildBlob(Distribution, 3, ObjectCount3);
        blob8 = BuildBlob(Distribution, 8, ObjectCount8);
        var expected3 = F3_Loop();
        var expected8 = F8_Loop();
        foreach (var (name, sum, expected) in new (string, long, long)[]
        {
            (nameof(F3_FusedExact), F3_FusedExact(), expected3),
            (nameof(F3_FusedThenLoop), F3_FusedThenLoop(), expected3),
            (nameof(F8_FusedExact), F8_FusedExact(), expected8),
            (nameof(F8_FusedThenLoop), F8_FusedThenLoop(), expected8),
        })
        {
            if (sum != expected)
            {
                throw new InvalidOperationException($"{name} checksum mismatch on {Distribution}: {sum} (expected {expected})");
            }
        }
    }

    static byte[] BuildBlob(string distribution, int arity, int objectCount)
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            for (int o = 0; o < objectCount; o++)
            {
                var count = distribution switch
                {
                    "SameVersion" => arity,
                    "RareMismatch" => o % 64 == 63 ? arity + 1 : arity,
                    "MixedVersion" => arity - 1 + rand.Next(3),
                    _ => throw new InvalidOperationException(distribution),
                };
                buffer.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    buffer.WriteSingle((float)(rand.NextDouble() * 2000.0 - 1000.0));
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    // ---- benchmarks -------------------------------------------------------

    [BenchmarkCategory("F3"), Benchmark(Baseline = true)]
    public long F3_Loop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob3);
        var value = poco3;
        long sum = 0;
        for (int o = 0; o < ObjectCount3; o++)
        {
            ReadLoop3(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("F3"), Benchmark]
    public long F3_FusedExact()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob3);
        var value = poco3;
        long sum = 0;
        for (int o = 0; o < ObjectCount3; o++)
        {
            ReadFusedExact3(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("F3"), Benchmark]
    public long F3_FusedThenLoop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob3);
        var value = poco3;
        long sum = 0;
        for (int o = 0; o < ObjectCount3; o++)
        {
            ReadFusedThenLoop3(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("F8"), Benchmark(Baseline = true)]
    public long F8_Loop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob8);
        var value = poco8;
        long sum = 0;
        for (int o = 0; o < ObjectCount8; o++)
        {
            ReadLoop8(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("F8"), Benchmark]
    public long F8_FusedExact()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob8);
        var value = poco8;
        long sum = 0;
        for (int o = 0; o < ObjectCount8; o++)
        {
            ReadFusedExact8(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("F8"), Benchmark]
    public long F8_FusedThenLoop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob8);
        var value = poco8;
        long sum = 0;
        for (int o = 0; o < ObjectCount8; o++)
        {
            ReadFusedThenLoop8(ref buffer, value);
            sum += value.Checksum();
        }
        return sum;
    }

    // ---- shapes -----------------------------------------------------------

    const int F32 = MessagePackCode.Float32;
    const int TokenSize = 5;

    // the payload of the float32 token whose code byte sits at tokenOffset
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Payload(ref byte s, int tokenOffset) =>
        BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, tokenOffset + 1))));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool AllFloat32x3(ref byte s) =>
        ((s ^ F32) | (Unsafe.Add(ref s, 5) ^ F32) | (Unsafe.Add(ref s, 10) ^ F32)) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool AllFloat32x8(ref byte s) =>
        ((s ^ F32) | (Unsafe.Add(ref s, 5) ^ F32) | (Unsafe.Add(ref s, 10) ^ F32) | (Unsafe.Add(ref s, 15) ^ F32)
            | (Unsafe.Add(ref s, 20) ^ F32) | (Unsafe.Add(ref s, 25) ^ F32) | (Unsafe.Add(ref s, 30) ^ F32) | (Unsafe.Add(ref s, 35) ^ F32)) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoop3(ref ReadOnlySpanReadBuffer buffer, Float3 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.X = buffer.ReadSingle(); break;
                case 1: value.Y = buffer.ReadSingle(); break;
                case 2: value.Z = buffer.ReadSingle(); break;
                default: buffer.Skip(); break; // version tolerance: unknown trailing member
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFusedExact3(ref ReadOnlySpanReadBuffer buffer, Float3 value)
    {
        var count = buffer.ReadArrayHeader();
        var span = buffer.GetUnreadSpan();
        if (count == 3 && span.Length >= 3 * TokenSize)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            if (AllFloat32x3(ref s))
            {
                value.X = Payload(ref s, 0);
                value.Y = Payload(ref s, 5);
                value.Z = Payload(ref s, 10);
                buffer.Advance(3 * TokenSize);
                return;
            }
        }
        ReadBody3Fallback(ref buffer, value, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFusedThenLoop3(ref ReadOnlySpanReadBuffer buffer, Float3 value)
    {
        var count = buffer.ReadArrayHeader();
        var i = 0;
        var span = buffer.GetUnreadSpan();
        if (count >= 3 && span.Length >= 3 * TokenSize)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            if (AllFloat32x3(ref s))
            {
                value.X = Payload(ref s, 0);
                value.Y = Payload(ref s, 5);
                value.Z = Payload(ref s, 10);
                buffer.Advance(3 * TokenSize);
                i = 3;
            }
        }
        for (; i < count; i++)
        {
            switch (i)
            {
                case 0: value.X = buffer.ReadSingle(); break;
                case 1: value.Y = buffer.ReadSingle(); break;
                case 2: value.Z = buffer.ReadSingle(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    // fallbacks stay out of line: 9fd6f1d showed an unexecuted fast-path block taxing the sibling loop it shares a method with
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ReadBody3Fallback(ref ReadOnlySpanReadBuffer buffer, Float3 value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.X = buffer.ReadSingle(); break;
                case 1: value.Y = buffer.ReadSingle(); break;
                case 2: value.Z = buffer.ReadSingle(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoop8(ref ReadOnlySpanReadBuffer buffer, Float8 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadSingle(); break;
                case 1: value.B = buffer.ReadSingle(); break;
                case 2: value.C = buffer.ReadSingle(); break;
                case 3: value.D = buffer.ReadSingle(); break;
                case 4: value.E = buffer.ReadSingle(); break;
                case 5: value.F = buffer.ReadSingle(); break;
                case 6: value.G = buffer.ReadSingle(); break;
                case 7: value.H = buffer.ReadSingle(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFusedExact8(ref ReadOnlySpanReadBuffer buffer, Float8 value)
    {
        var count = buffer.ReadArrayHeader();
        var span = buffer.GetUnreadSpan();
        if (count == 8 && span.Length >= 8 * TokenSize)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            if (AllFloat32x8(ref s))
            {
                value.A = Payload(ref s, 0);
                value.B = Payload(ref s, 5);
                value.C = Payload(ref s, 10);
                value.D = Payload(ref s, 15);
                value.E = Payload(ref s, 20);
                value.F = Payload(ref s, 25);
                value.G = Payload(ref s, 30);
                value.H = Payload(ref s, 35);
                buffer.Advance(8 * TokenSize);
                return;
            }
        }
        ReadBody8Fallback(ref buffer, value, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFusedThenLoop8(ref ReadOnlySpanReadBuffer buffer, Float8 value)
    {
        var count = buffer.ReadArrayHeader();
        var i = 0;
        var span = buffer.GetUnreadSpan();
        if (count >= 8 && span.Length >= 8 * TokenSize)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            if (AllFloat32x8(ref s))
            {
                value.A = Payload(ref s, 0);
                value.B = Payload(ref s, 5);
                value.C = Payload(ref s, 10);
                value.D = Payload(ref s, 15);
                value.E = Payload(ref s, 20);
                value.F = Payload(ref s, 25);
                value.G = Payload(ref s, 30);
                value.H = Payload(ref s, 35);
                buffer.Advance(8 * TokenSize);
                i = 8;
            }
        }
        for (; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadSingle(); break;
                case 1: value.B = buffer.ReadSingle(); break;
                case 2: value.C = buffer.ReadSingle(); break;
                case 3: value.D = buffer.ReadSingle(); break;
                case 4: value.E = buffer.ReadSingle(); break;
                case 5: value.F = buffer.ReadSingle(); break;
                case 6: value.G = buffer.ReadSingle(); break;
                case 7: value.H = buffer.ReadSingle(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ReadBody8Fallback(ref ReadOnlySpanReadBuffer buffer, Float8 value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadSingle(); break;
                case 1: value.B = buffer.ReadSingle(); break;
                case 2: value.C = buffer.ReadSingle(); break;
                case 3: value.D = buffer.ReadSingle(); break;
                case 4: value.E = buffer.ReadSingle(); break;
                case 5: value.F = buffer.ReadSingle(); break;
                case 6: value.G = buffer.ReadSingle(); break;
                case 7: value.H = buffer.ReadSingle(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    // ---- verification -----------------------------------------------------

    internal delegate void ReadShape<T>(ref ReadOnlySpanReadBuffer buffer, T value);

    // how one member is coded on the wire: the fused run only takes Float32, the others must reach the compat arms
    enum Coding
    {
        Float32,
        Float64,
        Int,
    }

    public static void VerifyCandidates()
    {
        VerifyArity(3,
            [(nameof(ReadFusedExact3), ReadFusedExact3), (nameof(ReadFusedThenLoop3), ReadFusedThenLoop3)],
            ReadLoop3, static () => new Float3(), static v => v.Bits());
        VerifyArity(8,
            [(nameof(ReadFusedExact8), ReadFusedExact8), (nameof(ReadFusedThenLoop8), ReadFusedThenLoop8)],
            ReadLoop8, static () => new Float8(), static v => v.Bits());
    }

    static void VerifyArity<T>(int arity, (string, ReadShape<T>)[] shapes, ReadShape<T> oracle, Func<T> create, Func<T, int[]> bits)
    {
        var testValues = new[]
        {
            0f, -0f, 1f, -1f, float.Epsilon, float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity,
            float.NaN, 3.14159274f, 16777216f, -123456.789f,
        };
        for (int count = 0; count <= arity + 3; count++)
        {
            for (int seed = 0; seed < testValues.Length; seed++)
            {
                var values = Enumerable.Range(0, count).Select(i => testValues[(seed + i) % testValues.Length]).ToArray();
                var allFloat32 = Enumerable.Repeat(Coding.Float32, count).ToArray();
                VerifyPayload(arity, shapes, oracle, create, bits, BuildSingle(values, allFloat32));

                // one member at a time coded as float64 / int: the folded code check must miss and the per-value
                // path must decode it exactly as the oracle does
                for (int drift = 0; drift < count; drift++)
                {
                    foreach (var coding in new[] { Coding.Float64, Coding.Int })
                    {
                        var codings = (Coding[])allFloat32.Clone();
                        codings[drift] = coding;
                        VerifyPayload(arity, shapes, oracle, create, bits, BuildSingle(values, codings));
                    }
                }
            }
        }
    }

    static byte[] BuildSingle(float[] values, Coding[] codings)
    {
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            buffer.WriteArrayHeader(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                switch (codings[i])
                {
                    case Coding.Float64:
                        buffer.WriteDouble(values[i]);
                        break;
                    case Coding.Int when float.IsFinite(values[i]) && MathF.Abs(values[i]) < 1e9f:
                        buffer.WriteInt32((int)values[i]);
                        break;
                    default:
                        buffer.WriteSingle(values[i]);
                        break;
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    // the whole payload, then every truncation of it: fields, stale sentinels, consumption and the failure
    // (exception type and the fields assigned before it) must all match the shipped loop
    static void VerifyPayload<T>(int arity, (string, ReadShape<T>)[] shapes, ReadShape<T> oracle, Func<T> create, Func<T, int[]> bits, byte[] payload)
    {
        for (int length = payload.Length; length >= 0; length--)
        {
            var cut = payload.AsSpan(0, length).ToArray();
            var (expectedBits, expectedRemaining, expectedError) = Run(oracle, create, bits, cut);
            foreach (var (name, shape) in shapes)
            {
                var (actualBits, actualRemaining, actualError) = Run(shape, create, bits, cut);
                if (actualError != expectedError)
                {
                    throw new InvalidOperationException($"{name} (arity {arity}) outcome mismatch at length {length}/{payload.Length}: {actualError ?? "success"} (expected {expectedError ?? "success"})");
                }
                if (!actualBits.AsSpan().SequenceEqual(expectedBits))
                {
                    throw new InvalidOperationException($"{name} (arity {arity}) field mismatch at length {length}/{payload.Length}");
                }
                if (actualRemaining != expectedRemaining)
                {
                    throw new InvalidOperationException($"{name} (arity {arity}) consumed differently at length {length}/{payload.Length}: {actualRemaining} left (expected {expectedRemaining})");
                }
            }
        }
    }

    static (int[] Bits, long Remaining, string? Error) Run<T>(ReadShape<T> shape, Func<T> create, Func<T, int[]> bits, byte[] payload)
    {
        var value = create();
        var buffer = new ReadOnlySpanReadBuffer(payload);
        string? error = null;
        try
        {
            shape(ref buffer, value);
        }
        catch (MessagePackSerializationException ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
        }
        return (bits(value), buffer.BytesRemaining, error);
    }
}

public sealed class Float3
{
    // sentinels, so a member the payload never reached is distinguishable from one it set
    public float X = -11f;
    public float Y = -12f;
    public float Z = -13f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => (long)BitConverter.SingleToInt32Bits(X) + BitConverter.SingleToInt32Bits(Y) + BitConverter.SingleToInt32Bits(Z);

    public int[] Bits() => [BitConverter.SingleToInt32Bits(X), BitConverter.SingleToInt32Bits(Y), BitConverter.SingleToInt32Bits(Z)];
}

public sealed class Float8
{
    public float A = -11f;
    public float B = -12f;
    public float C = -13f;
    public float D = -14f;
    public float E = -15f;
    public float F = -16f;
    public float G = -17f;
    public float H = -18f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() =>
        (long)BitConverter.SingleToInt32Bits(A) + BitConverter.SingleToInt32Bits(B) + BitConverter.SingleToInt32Bits(C) + BitConverter.SingleToInt32Bits(D)
        + BitConverter.SingleToInt32Bits(E) + BitConverter.SingleToInt32Bits(F) + BitConverter.SingleToInt32Bits(G) + BitConverter.SingleToInt32Bits(H);

    public int[] Bits() =>
    [
        BitConverter.SingleToInt32Bits(A), BitConverter.SingleToInt32Bits(B), BitConverter.SingleToInt32Bits(C), BitConverter.SingleToInt32Bits(D),
        BitConverter.SingleToInt32Bits(E), BitConverter.SingleToInt32Bits(F), BitConverter.SingleToInt32Bits(G), BitConverter.SingleToInt32Bits(H),
    ];
}

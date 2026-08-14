// float32/float64 array serialize + deserialize: byte-shuffle weaves vs the scalar loops.
//
// Background. Tokens are constant width ([0xca][4B] / [0xcb][8B]), so no classify chain
// exists; the question per direction is only "is the shuffle worth it". Serialize uses a
// single permute that reverses payloads and leaves code slots, which a masked OR fills
// with the constant code. Deserialize is the inverse gather, structurally the int32
// wide-superlane decode with a constant code compare.
// Tier candidates (512/256/128) are separately measurable on this machine because Zen 5
// supports all of them; each candidate calls its tier's intrinsics directly.
//
// Candidates are direct-emit/decode statics (ref byte), isolating the loop from buffer
// machinery. Serializer rows give end-to-end context. Values are random full-range bit
// patterns, so NaN payloads and subnormals are included; wires and decoded values must be
// bit-identical to the scalar reference.
//
// RESULTS round 1 (Zen 5, ShortRun, ns/element, serialize only):
//   float  N=1000    RegionScalar 0.46   Weave13 0.047   (9.8x)
//   float  N=100000  RegionScalar 0.43   Weave13 0.057   (7.6x)
//   double N=1000    RegionScalar 0.43   Weave7  0.068   (6.3x)
//   double N=100000  RegionScalar 0.44   Weave7  0.130   (3.4x, output bandwidth shows)
// VERDICT round 1: 512-tier weave adopted into Single/DoubleElementCodec.
// Round 2 adds the 256/128 serialize tiers (also adopted; verified per tier by running
// the test suite with DOTNET_EnableAVX512F=0 / EnableAVX2=0 / EnableSSSE3=0) and the
// decode gathers. Pitfall in the 128 double tier: token1's last payload byte goes scalar
// and MUST be scaled by the element index, not read at a fixed offset.
//
// RESULTS round 2 (Zen 5, ShortRun, ns/element, N=1000 / N=100000):
//   float  ser   scalar 0.42/0.44   W512 0.054/0.061   W256 0.080/0.060   W128 0.153/0.148
//   double ser   scalar 0.43/0.43   W512 0.067/0.129   W256 0.160/0.146   W128 0.367/0.386
//   float  deser fused  0.75/0.85   W512 0.064/0.064   (11.8x / 13.3x)
//   double deser fused  0.81/0.79   W512 0.113/0.128   (7.2x / 6.2x)
// VERDICT round 2: every tier beats scalar (float 128 still 2.8x; double 128 a modest
// ~1.15x but positive), tier ordering 512 > 256 > 128 confirmed. Decode gathers adopted
// (the fused-scalar decode was latency-bound on ReverseEndianness + validation, the
// gather removes it entirely). End-to-end context after adoption: float[] serialize
// 218ns vs Nerdbank 338ns at N=1000, deserialize 200ns vs Nerdbank 3997ns.
//
// Round 3 candidates, adopted into the codecs ahead of measurement (2026-08-12).
// 512Fused folds the mask-and-OR pass into a two-source vpermi2b whose second source is
// a vector filled with the code byte (code slots use index 64, byte 0 of that source).
// 256Unroll2 unrolls the float 256 weave x2 to amortize the loop overhead; the first
// store's 2 overhang bytes are immediately overwritten by the second store.
// CAUTION the local i7-13700KF has no AVX-512, so every 512 row falls back to scalar
// here and both verify and measurement of the fused rows need AVX-512 hardware.
//
// RESULTS round 3 (Zen 5 with AVX-512VBMI, ShortRun, ns/element, N=1000 / N=100000):
//   float  ser  W512 0.056/0.057   W512Fused 0.053/0.054   W256 0.090/0.079   W256Unroll2 0.065/0.065
//   double ser  W512 0.072/0.140   W512Fused 0.069/0.130
// VERDICT round 3: both pre-adopted candidates validated on AVX-512 hardware. Fused is
// a consistent ~5% win with smaller code (181B vs 186B); Unroll2 cuts the 256 tier by
// 17-27%. Verify (bit-identical wires, NaN payloads included) passed on this machine.
//
// RESULTS round 4, the "unroll to 320 bytes" TODO (Zen 5, ShortRun, float ser ns):
//   N=1000    Fused 46.4   Unroll320 48.0   (wash, slightly worse)
//   N=100000  Fused 5441   Unroll320 8156   (1.5x WORSE)   code 181B vs 491B
// VERDICT round 4: REJECTED. 320 = LCM(5, 64) does tile 64 tokens into five seamless
// output vectors (4 loads, 2 one-source permutes with codeFill, 3 two-source permutes
// with mask+or blends) and cuts uops per token by a third, but the 13-token fused loop
// was never frontend-bound: it is store-side-bound, so full lane utilization buys
// nothing at L1 and the 5-wide store burst plus 12 resident table registers loses badly
// once the output spills past L1. The candidate stays here as the record.
//
// Round 5 fills the two gaps that round 2 left open: non-VBMI decode weaves (the codecs
// fell back to fused scalar below AVX-512) and Unroll2 for the remaining serialize tiers.
// Decode 256 float pulls 6 tokens per 32B window with one in-lane vpshufb (lane 0 holds
// tokens 0-2, lane 1 the payloads of 3-5; halves store 12B apart, garbage self-heals).
// Decode 256 double needs a vpermq second source for token 1's lane-straddling payload.
// Decode 128 double is one token per iteration; the win is only the vector byte-reverse.
//
// RESULTS round 5 (Zen 5, ShortRun, ns/element, N=1000 / N=100000):
//   float  deser fused 0.67/0.67   W256 0.127/0.121 (5.3x)   W128 0.267/0.202 (2.5-3.3x)
//   double deser fused 0.68/0.68   W256 0.215/0.218 (3.1x)   W128 0.500/0.508 (1.35x)
//   float  ser  W128 0.141/0.096   W128Unroll2 0.092/0.088   (35% / 9% faster)
//   double ser  W256 0.141/0.142   W256Unroll2 0.107/0.127   (24% / 10% faster)
//   double ser  W128 0.334/0.338   W128Unroll2 0.327/0.332   (~2%, wash)
// VERDICT round 5: decode weaves adopted for all three sub-512 tiers (256 float/double,
// 128 float/double); serialize Unroll2 adopted for float 128 and double 256. Double 128
// Unroll2 REJECTED: the exact 18B pair layout has no overhang for the second store to
// absorb, so the unroll only halves loop bookkeeping and that is not the bottleneck.
// Verified per tier with DOTNET_EnableAVX512F=0 / EnableAVX2=0 / EnableSSSE3=0.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using UltraMessagePack;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FloatWeaveBenchmark
{
    [Params(1000, 100_000)]
    public int N = 1000;

    float[] fdata = default!;
    double[] ddata = default!;
    byte[] fbuf = default!;
    byte[] dbuf = default!;
    byte[] fElems = default!;   // element tokens only (no array header), decode candidate input
    byte[] dElems = default!;
    float[] fdst = default!;
    double[] ddst = default!;
    byte[] fpayload = default!; // full payload for the end-to-end deserializer rows
    byte[] dpayload = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        fdata = new float[N];
        ddata = new double[N];
        for (int i = 0; i < N; i++)
        {
            fdata[i] = BitConverter.Int32BitsToSingle(rand.Next(int.MinValue, int.MaxValue));
            ddata[i] = BitConverter.Int64BitsToDouble(rand.NextInt64(long.MinValue, long.MaxValue));
        }
        fbuf = new byte[5 * N + 64];
        dbuf = new byte[9 * N + 64];
        fdst = new float[N];
        ddst = new double[N];
        fpayload = MessagePackSerializer.Serialize(fdata);
        dpayload = MessagePackSerializer.Serialize(ddata);

        fElems = new byte[5 * N];
        FloatWeaveCandidates.WriteFloatRegionScalar(ref MemoryMarshal.GetArrayDataReference(fElems), ref MemoryMarshal.GetArrayDataReference(fdata), N);
        dElems = new byte[9 * N];
        FloatWeaveCandidates.WriteDoubleRegionScalar(ref MemoryMarshal.GetArrayDataReference(dElems), ref MemoryMarshal.GetArrayDataReference(ddata), N);

        // self-verify: every candidate must be bit-identical to the scalar reference
        if (!FloatWeaveVerify.VerifyAll(fdata, ddata)) throw new InvalidOperationException("verify failed: weave candidates");
        var oracle = MessagePack.MessagePackSerializer.Serialize(fdata);
        if (!fpayload.AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException("verify failed: float serializer vs oracle");
    }

    // ---- serialize ----

    [BenchmarkCategory("FloatSer"), Benchmark(Baseline = true)]
    public int FloatRegionScalar() => FloatWeaveCandidates.WriteFloatRegionScalar(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave512() => FloatWeaveCandidates.WriteFloatWeave512(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave512Fused() => FloatWeaveCandidates.WriteFloatWeave512Fused(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave512Unroll320() => FloatWeaveCandidates.WriteFloatWeave512Unroll320(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave256() => FloatWeaveCandidates.WriteFloatWeave256(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave256Unroll2() => FloatWeaveCandidates.WriteFloatWeave256Unroll2(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave128() => FloatWeaveCandidates.WriteFloatWeave128(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public int FloatWeave128Unroll2() => FloatWeaveCandidates.WriteFloatWeave128Unroll2(ref MemoryMarshal.GetArrayDataReference(fbuf), ref MemoryMarshal.GetArrayDataReference(fdata), N);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public byte[] FloatSerializerCurrent() => MessagePackSerializer.Serialize(fdata);

    [BenchmarkCategory("FloatSer"), Benchmark]
    public byte[] FloatNerdbank() => nb.Serialize<float[], NbFloatArrayWitness>(fdata);

    [BenchmarkCategory("DoubleSer"), Benchmark(Baseline = true)]
    public int DoubleRegionScalar() => FloatWeaveCandidates.WriteDoubleRegionScalar(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave512() => FloatWeaveCandidates.WriteDoubleWeave512(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave512Fused() => FloatWeaveCandidates.WriteDoubleWeave512Fused(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave256() => FloatWeaveCandidates.WriteDoubleWeave256(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave256Unroll2() => FloatWeaveCandidates.WriteDoubleWeave256Unroll2(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave128() => FloatWeaveCandidates.WriteDoubleWeave128(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public int DoubleWeave128Unroll2() => FloatWeaveCandidates.WriteDoubleWeave128Unroll2(ref MemoryMarshal.GetArrayDataReference(dbuf), ref MemoryMarshal.GetArrayDataReference(ddata), N);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public byte[] DoubleSerializerCurrent() => MessagePackSerializer.Serialize(ddata);

    [BenchmarkCategory("DoubleSer"), Benchmark]
    public byte[] DoubleNerdbank() => nb.Serialize<double[], NbDoubleArrayWitness>(ddata);

    // ---- deserialize ----

    [BenchmarkCategory("FloatDeser"), Benchmark(Baseline = true)]
    public bool FloatDecodeFused() => FloatWeaveCandidates.ReadFloatFused(ref MemoryMarshal.GetArrayDataReference(fElems), ref MemoryMarshal.GetArrayDataReference(fdst), N);

    [BenchmarkCategory("FloatDeser"), Benchmark]
    public bool FloatDecodeWeave512() => FloatWeaveCandidates.ReadFloatWeave512(ref MemoryMarshal.GetArrayDataReference(fElems), ref MemoryMarshal.GetArrayDataReference(fdst), N);

    [BenchmarkCategory("FloatDeser"), Benchmark]
    public bool FloatDecodeWeave256() => FloatWeaveCandidates.ReadFloatWeave256(ref MemoryMarshal.GetArrayDataReference(fElems), ref MemoryMarshal.GetArrayDataReference(fdst), N);

    [BenchmarkCategory("FloatDeser"), Benchmark]
    public bool FloatDecodeWeave128() => FloatWeaveCandidates.ReadFloatWeave128(ref MemoryMarshal.GetArrayDataReference(fElems), ref MemoryMarshal.GetArrayDataReference(fdst), N);

    [BenchmarkCategory("FloatDeser"), Benchmark]
    public float[] FloatDeserializerCurrent() => MessagePackSerializer.Deserialize<float[]>(fpayload)!;

    [BenchmarkCategory("FloatDeser"), Benchmark]
    public float[] FloatNerdbankDeser() => nb.Deserialize<float[], NbFloatArrayWitness>(fpayload)!;

    [BenchmarkCategory("DoubleDeser"), Benchmark(Baseline = true)]
    public bool DoubleDecodeFused() => FloatWeaveCandidates.ReadDoubleFused(ref MemoryMarshal.GetArrayDataReference(dElems), ref MemoryMarshal.GetArrayDataReference(ddst), N);

    [BenchmarkCategory("DoubleDeser"), Benchmark]
    public bool DoubleDecodeWeave512() => FloatWeaveCandidates.ReadDoubleWeave512(ref MemoryMarshal.GetArrayDataReference(dElems), ref MemoryMarshal.GetArrayDataReference(ddst), N);

    [BenchmarkCategory("DoubleDeser"), Benchmark]
    public bool DoubleDecodeWeave256() => FloatWeaveCandidates.ReadDoubleWeave256(ref MemoryMarshal.GetArrayDataReference(dElems), ref MemoryMarshal.GetArrayDataReference(ddst), N);

    [BenchmarkCategory("DoubleDeser"), Benchmark]
    public bool DoubleDecodeWeave128() => FloatWeaveCandidates.ReadDoubleWeave128(ref MemoryMarshal.GetArrayDataReference(dElems), ref MemoryMarshal.GetArrayDataReference(ddst), N);

    [BenchmarkCategory("DoubleDeser"), Benchmark]
    public double[] DoubleDeserializerCurrent() => MessagePackSerializer.Deserialize<double[]>(dpayload)!;

    [BenchmarkCategory("DoubleDeser"), Benchmark]
    public double[] DoubleNerdbankDeser() => nb.Deserialize<double[], NbDoubleArrayWitness>(dpayload)!;
}

[PolyType.GenerateShapeFor<float[]>]
public partial class NbFloatArrayWitness;

[PolyType.GenerateShapeFor<double[]>]
public partial class NbDoubleArrayWitness;

// tables duplicated from the library's file-scoped FloatWeaveTables/WideLaneTables
// (file-scoped types are invisible even to InternalsVisibleTo)
static class WeaveBenchTables
{
    const byte B = byte.MaxValue;
    const byte C = 64; // code slot for the two-source permute, byte 0 of the code-fill source
    const byte F32 = 0xca;
    const byte F64 = 0xcb;

    public static readonly Vector512<byte> FloatIndicesX2 = Vector512.Create(
        (byte)3, 2, 1, 0, C, 7, 6, 5, 4, C, 11, 10, 9, 8, C, 15, 14, 13, 12, C,
        19, 18, 17, 16, C, 23, 22, 21, 20, C, 27, 26, 25, 24, C, 31, 30, 29, 28, C,
        35, 34, 33, 32, C, 39, 38, 37, 36, C, 43, 42, 41, 40, C, 47, 46, 45, 44, C,
        51, 50, 49, 48);

    // 320-byte float unroll: 320 = LCM(5, 64), so 64 tokens tile five output vectors with
    // no partial-token seam. Output windows 0 and 4 draw payloads from a single input
    // vector, letting codeFill ride as the permute's second source; windows 1-3 span two
    // input vectors, so both permute sources are data and the code bytes are blended in
    // afterwards with the window's mask/code pair. Tables are generated, not hand-written:
    // output byte p is a code slot when p % 5 == 0, otherwise payload byte (3 - q) of
    // token p / 5 (BE order), i.e. global input byte 4 * (p / 5) + 3 - (p % 5 - 1).
    public static readonly Vector512<byte> F320Idx0 = BuildF320Index(0);
    public static readonly Vector512<byte> F320Idx1 = BuildF320Index(1);
    public static readonly Vector512<byte> F320Idx2 = BuildF320Index(2);
    public static readonly Vector512<byte> F320Idx3 = BuildF320Index(3);
    public static readonly Vector512<byte> F320Idx4 = BuildF320Index(4);
    public static readonly Vector512<byte> F320Mask1 = BuildF320Mask(1);
    public static readonly Vector512<byte> F320Mask2 = BuildF320Mask(2);
    public static readonly Vector512<byte> F320Mask3 = BuildF320Mask(3);
    public static readonly Vector512<byte> F320Code1 = BuildF320Code(1);
    public static readonly Vector512<byte> F320Code2 = BuildF320Code(2);
    public static readonly Vector512<byte> F320Code3 = BuildF320Code(3);

    static Vector512<byte> BuildF320Index(int window)
    {
        Span<byte> idx = stackalloc byte[64];
        int baseVec = window <= 1 ? 0 : window - 1;
        for (int j = 0; j < 64; j++)
        {
            int p = (window * 64) + j;
            if (p % 5 == 0)
            {
                idx[j] = (byte)(window is 0 or 4 ? 64 : 0); // codeFill byte 0, or don't-care (blended after)
            }
            else
            {
                int gi = (4 * (p / 5)) + 3 - ((p % 5) - 1);
                idx[j] = (byte)(gi - (64 * baseVec)); // >= 64 automatically selects the second source
            }
        }
        return Vector512.Create<byte>(idx);
    }

    static Vector512<byte> BuildF320Mask(int window)
    {
        Span<byte> mask = stackalloc byte[64];
        for (int j = 0; j < 64; j++)
        {
            mask[j] = ((window * 64) + j) % 5 == 0 ? (byte)0 : (byte)0xFF;
        }
        return Vector512.Create<byte>(mask);
    }

    static Vector512<byte> BuildF320Code(int window)
    {
        Span<byte> code = stackalloc byte[64];
        for (int j = 0; j < 64; j++)
        {
            code[j] = ((window * 64) + j) % 5 == 0 ? F32 : (byte)0;
        }
        return Vector512.Create<byte>(code);
    }
    public static readonly Vector512<byte> DoubleIndicesX2 = Vector512.Create(
        C, 7, 6, 5, 4, 3, 2, 1, 0, C, 15, 14, 13, 12, 11, 10, 9, 8,
        C, 23, 22, 21, 20, 19, 18, 17, 16, C, 31, 30, 29, 28, 27, 26, 25, 24,
        C, 39, 38, 37, 36, 35, 34, 33, 32, C, 47, 46, 45, 44, 43, 42, 41, 40,
        C, 55, 54, 53, 52, 51, 50, 49, 48, C);

    public static readonly Vector512<byte> FloatIndices512 = Vector512.Create(
        (byte)3, 2, 1, 0, B, 7, 6, 5, 4, B, 11, 10, 9, 8, B, 15, 14, 13, 12, B,
        19, 18, 17, 16, B, 23, 22, 21, 20, B, 27, 26, 25, 24, B, 31, 30, 29, 28, B,
        35, 34, 33, 32, B, 39, 38, 37, 36, B, 43, 42, 41, 40, B, 47, 46, 45, 44, B,
        51, 50, 49, 48);
    public static readonly Vector512<byte> FloatMask512 = Vector512.Create(
        B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0,
        B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0,
        B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0,
        B, B, B, B);
    public static readonly Vector512<byte> FloatCode512 = Vector512.Create(
        0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32,
        0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32,
        0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32,
        0, 0, 0, 0);
    public static readonly Vector512<byte> DoubleIndices512 = Vector512.Create(
        B, 7, 6, 5, 4, 3, 2, 1, 0, B, 15, 14, 13, 12, 11, 10, 9, 8,
        B, 23, 22, 21, 20, 19, 18, 17, 16, B, 31, 30, 29, 28, 27, 26, 25, 24,
        B, 39, 38, 37, 36, 35, 34, 33, 32, B, 47, 46, 45, 44, 43, 42, 41, 40,
        B, 55, 54, 53, 52, 51, 50, 49, 48, B);
    public static readonly Vector512<byte> DoubleMask512 = Vector512.Create(
        0, B, B, B, B, B, B, B, B, 0, B, B, B, B, B, B, B, B,
        0, B, B, B, B, B, B, B, B, 0, B, B, B, B, B, B, B, B,
        0, B, B, B, B, B, B, B, B, 0, B, B, B, B, B, B, B, B,
        0, B, B, B, B, B, B, B, B, 0);
    public static readonly Vector512<byte> DoubleCode512 = Vector512.Create(
        F64, 0, 0, 0, 0, 0, 0, 0, 0, F64, 0, 0, 0, 0, 0, 0, 0, 0,
        F64, 0, 0, 0, 0, 0, 0, 0, 0, F64, 0, 0, 0, 0, 0, 0, 0, 0,
        F64, 0, 0, 0, 0, 0, 0, 0, 0, F64, 0, 0, 0, 0, 0, 0, 0, 0,
        F64, 0, 0, 0, 0, 0, 0, 0, 0, F64);

    public static readonly Vector256<byte> FloatIndices256 = Vector256.Create(
        B, 3, 2, 1, 0, B, 7, 6, 5, 4, B, 11, 10, 9, 8, B,
        7, 6, 5, 4, B, 11, 10, 9, 8, B, 15, 14, 13, 12, B, B);
    public static readonly Vector256<byte> FloatCode256 = Vector256.Create(
        F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32,
        0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, 0, 0);
    public static readonly Vector256<byte> DoubleIndices256 = Vector256.Create(
        B, 7, 6, 5, 4, 3, 2, 1, 0, B, 15, 14, 13, 12, 11, 10,
        1, 0, B, 15, 14, 13, 12, 11, 10, 9, 8, B, B, B, B, B);
    public static readonly Vector256<byte> DoubleCode256 = Vector256.Create(
        F64, 0, 0, 0, 0, 0, 0, 0, 0, F64, 0, 0, 0, 0, 0, 0,
        0, 0, F64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static readonly Vector128<byte> FloatIndices128 = Vector128.Create(
        B, 3, 2, 1, 0, B, 7, 6, 5, 4, B, 11, 10, 9, 8, B);
    public static readonly Vector128<byte> FloatCode128 = Vector128.Create(
        F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, F32, 0, 0, 0, 0, 0);
    public static readonly Vector128<byte> DoubleIndices128 = Vector128.Create(
        7, 6, 5, 4, 3, 2, 1, 0, B, 15, 14, 13, 12, 11, 10, 9);
    public static readonly Vector128<byte> DoubleCode128 = Vector128.Create(
        0, 0, 0, 0, 0, 0, 0, 0, F64, 0, 0, 0, 0, 0, 0, 0);

    // decode shuffles for the non-VBMI tiers (Z = pshufb zeroing index). Float 256:
    // lane 0 reverses tokens 0-2 (window bytes 1-14), lane 1 reverses the payloads of
    // tokens 3-5 (window bytes 16-29, all within the lane because token 3's code sits
    // at byte 15). Double 256: A covers tokens 0 (lane 0) and 2 (lane 1); B covers
    // token 1 out of a vpermq'd q1q2 lane.
    const byte Z = 0x80;

    public static readonly Vector256<byte> FloatDecShuffle256 = Vector256.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, Z, Z, Z, Z,
        3, 2, 1, 0, 8, 7, 6, 5, 13, 12, 11, 10, Z, Z, Z, Z);
    public static readonly Vector256<byte> DoubleDecShuffleA256 = Vector256.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1, Z, Z, Z, Z, Z, Z, Z, Z,
        10, 9, 8, 7, 6, 5, 4, 3, Z, Z, Z, Z, Z, Z, Z, Z);
    public static readonly Vector256<byte> DoubleDecShuffleB256 = Vector256.Create(
        Z, Z, Z, Z, Z, Z, Z, Z, (byte)9, 8, 7, 6, 5, 4, 3, 2,
        Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z);
    public static readonly Vector128<byte> FloatDecShuffle128 = Vector128.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, Z, Z, Z, Z);
    public static readonly Vector128<byte> DoubleDecShuffle128 = Vector128.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1, Z, Z, Z, Z, Z, Z, Z, Z);

    // decode gathers: float reuses the int32 wide-lane shape ([code][4B BE] x 16 from two
    // overlapping 64B loads); double is a single-source gather of 7 tokens per 64B window
    public static readonly Vector512<byte> FloatDecodeValues = Vector512.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, 19, 18, 17, 16,
        24, 23, 22, 21, 29, 28, 27, 26, 34, 33, 32, 31, 39, 38, 37, 36,
        44, 43, 42, 41, 49, 48, 47, 46, 54, 53, 52, 51, 59, 58, 57, 56,
        112, 63, 62, 61, 117, 116, 115, 114, 122, 121, 120, 119, 127, 126, 125, 124);
    public static readonly Vector512<byte> FloatDecodeCodes = Vector512.Create(
        (byte)0, 0, 0, 0, 5, 5, 5, 5, 10, 10, 10, 10, 15, 15, 15, 15,
        20, 20, 20, 20, 25, 25, 25, 25, 30, 30, 30, 30, 35, 35, 35, 35,
        40, 40, 40, 40, 45, 45, 45, 45, 50, 50, 50, 50, 55, 55, 55, 55,
        60, 60, 60, 60, 113, 113, 113, 113, 118, 118, 118, 118, 123, 123, 123, 123);
    public static readonly Vector512<byte> DoubleDecodeValues = Vector512.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1,
        17, 16, 15, 14, 13, 12, 11, 10,
        26, 25, 24, 23, 22, 21, 20, 19,
        35, 34, 33, 32, 31, 30, 29, 28,
        44, 43, 42, 41, 40, 39, 38, 37,
        53, 52, 51, 50, 49, 48, 47, 46,
        62, 61, 60, 59, 58, 57, 56, 55,
        0, 0, 0, 0, 0, 0, 0, 0);
    public static readonly Vector512<byte> DoubleDecodeCodes = Vector512.Create(
        (byte)0, 0, 0, 0, 0, 0, 0, 0,
        9, 9, 9, 9, 9, 9, 9, 9,
        18, 18, 18, 18, 18, 18, 18, 18,
        27, 27, 27, 27, 27, 27, 27, 27,
        36, 36, 36, 36, 36, 36, 36, 36,
        45, 45, 45, 45, 45, 45, 45, 45,
        54, 54, 54, 54, 54, 54, 54, 54,
        0, 0, 0, 0, 0, 0, 0, 0);
}

public static class FloatWeaveCandidates
{
    // ---- serialize: the region-scalar reference and per-tier weaves ----

    public static int WriteFloatRegionScalar(ref byte d, ref float src, int length)
    {
        int o = 0;
        for (int i = 0; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave512(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; i + 16 <= length; i += 13, o += 65)
            {
                Unsafe.Add(ref d, o) = 0xca;
                var loaded = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                var shuffled = Avx512Vbmi.PermuteVar64x8(loaded, WeaveBenchTables.FloatIndices512);
                ((shuffled & WeaveBenchTables.FloatMask512) | WeaveBenchTables.FloatCode512).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave512Fused(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            var codeFill = Vector512.Create((byte)0xca);
            for (; length - i >= 16; i += 13, o += 65)
            {
                Unsafe.Add(ref d, o) = 0xca;
                var loaded = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(loaded, WeaveBenchTables.FloatIndicesX2, codeFill).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave512Unroll320(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            var codeFill = Vector512.Create((byte)0xca);
            for (; length - i >= 64; i += 64, o += 320)
            {
                var v0 = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                var v1 = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 16)).AsByte();
                var v2 = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 32)).AsByte();
                var v3 = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 48)).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(v0, WeaveBenchTables.F320Idx0, codeFill).StoreUnsafe(ref Unsafe.Add(ref d, o));
                ((Avx512Vbmi.PermuteVar64x8x2(v0, WeaveBenchTables.F320Idx1, v1) & WeaveBenchTables.F320Mask1) | WeaveBenchTables.F320Code1).StoreUnsafe(ref Unsafe.Add(ref d, o + 64));
                ((Avx512Vbmi.PermuteVar64x8x2(v1, WeaveBenchTables.F320Idx2, v2) & WeaveBenchTables.F320Mask2) | WeaveBenchTables.F320Code2).StoreUnsafe(ref Unsafe.Add(ref d, o + 128));
                ((Avx512Vbmi.PermuteVar64x8x2(v2, WeaveBenchTables.F320Idx3, v3) & WeaveBenchTables.F320Mask3) | WeaveBenchTables.F320Code3).StoreUnsafe(ref Unsafe.Add(ref d, o + 192));
                Avx512Vbmi.PermuteVar64x8x2(v3, WeaveBenchTables.F320Idx4, codeFill).StoreUnsafe(ref Unsafe.Add(ref d, o + 256));
            }
            // 13-token fused loop mops up 16..63 remaining, then the scalar tail
            for (; length - i >= 16; i += 13, o += 65)
            {
                Unsafe.Add(ref d, o) = 0xca;
                var loaded = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(loaded, WeaveBenchTables.FloatIndicesX2, codeFill).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave256(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
        {
            for (; i + 8 <= length; i += 6, o += 30)
            {
                var loaded = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                // vpermq imm8, 2 bits per output qword read from the LSB, so q3..q0 = src q[2,1,1,0]
                var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permuted, WeaveBenchTables.FloatIndices256) | WeaveBenchTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave256Unroll2(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
        {
            for (; length - i >= 14; i += 12, o += 60)
            {
                var a = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                var b = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 6)).AsByte();
                // vpermq imm8, 2 bits per output qword read from the LSB, so q3..q0 = src q[2,1,1,0]
                var permutedA = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsByte();
                var permutedB = Avx2.Permute4x64(b.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permutedA, WeaveBenchTables.FloatIndices256) | WeaveBenchTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
                (Avx2.Shuffle(permutedB, WeaveBenchTables.FloatIndices256) | WeaveBenchTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, o + 30));
            }
            for (; length - i >= 8; i += 6, o += 30)
            {
                var loaded = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permuted, WeaveBenchTables.FloatIndices256) | WeaveBenchTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteFloatWeave128(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; i + 4 <= length; i += 3, o += 15)
            {
                var loaded = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                (Ssse3.Shuffle(loaded, WeaveBenchTables.FloatIndices128) | WeaveBenchTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // self-healing overlap unroll (the float 256 pattern): store 1's overhang byte at
    // o + 15 is immediately rewritten by store 2
    public static int WriteFloatWeave128Unroll2(ref byte d, ref float src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; length - i >= 8; i += 6, o += 30)
            {
                var a = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                var b = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 3)).AsByte();
                (Ssse3.Shuffle(a, WeaveBenchTables.FloatIndices128) | WeaveBenchTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, o));
                (Ssse3.Shuffle(b, WeaveBenchTables.FloatIndices128) | WeaveBenchTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, o + 15));
            }
            for (; i + 4 <= length; i += 3, o += 15)
            {
                var loaded = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                (Ssse3.Shuffle(loaded, WeaveBenchTables.FloatIndices128) | WeaveBenchTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteDoubleRegionScalar(ref byte d, ref double src, int length)
    {
        int o = 0;
        for (int i = 0; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteDoubleWeave512(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; i + 8 <= length; i += 7, o += 63)
            {
                var loaded = Vector512.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                var shuffled = Avx512Vbmi.PermuteVar64x8(loaded, WeaveBenchTables.DoubleIndices512);
                ((shuffled & WeaveBenchTables.DoubleMask512) | WeaveBenchTables.DoubleCode512).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteDoubleWeave512Fused(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            var codeFill = Vector512.Create((byte)0xcb);
            for (; length - i >= 8; i += 7, o += 63)
            {
                var loaded = Vector512.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(loaded, WeaveBenchTables.DoubleIndicesX2, codeFill).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteDoubleWeave256(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
        {
            for (; i + 4 <= length; i += 3, o += 27)
            {
                var loaded = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i);
                // vpermq imm8, 2 bits per output qword read from the LSB, so q3..q0 = src q[2,1,1,0]
                var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permuted, WeaveBenchTables.DoubleIndices256) | WeaveBenchTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteDoubleWeave128(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; i + 2 <= length; i += 2, o += 18)
            {
                Unsafe.Add(ref d, o) = 0xcb;
                Unsafe.Add(ref d, o + 17) = Unsafe.As<double, byte>(ref Unsafe.Add(ref src, i + 1));
                var loaded = Vector128.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                (Ssse3.Shuffle(loaded, WeaveBenchTables.DoubleIndices128) | WeaveBenchTables.DoubleCode128).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // self-healing overlap unroll: store 1's 5 overhang bytes at o + 27 are immediately
    // rewritten by store 2; the guard consumes 6 but requires 8, so a tail always covers
    // store 2's overhang
    public static int WriteDoubleWeave256Unroll2(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
        {
            for (; length - i >= 8; i += 6, o += 54)
            {
                var a = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i);
                var b = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)(i + 3));
                var permutedA = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsByte();
                var permutedB = Avx2.Permute4x64(b.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permutedA, WeaveBenchTables.DoubleIndices256) | WeaveBenchTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
                (Avx2.Shuffle(permutedB, WeaveBenchTables.DoubleIndices256) | WeaveBenchTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, o + 27));
            }
            for (; i + 4 <= length; i += 3, o += 27)
            {
                var loaded = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i);
                var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                (Avx2.Shuffle(permuted, WeaveBenchTables.DoubleIndices256) | WeaveBenchTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, o));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // the 128 double pair is exact (18B per 2 tokens, no overhang), so this unroll only
    // halves the loop bookkeeping
    public static int WriteDoubleWeave128Unroll2(ref byte d, ref double src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; length - i >= 4; i += 4, o += 36)
            {
                Unsafe.Add(ref d, o) = 0xcb;
                Unsafe.Add(ref d, o + 17) = Unsafe.As<double, byte>(ref Unsafe.Add(ref src, i + 1));
                var a = Vector128.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                (Ssse3.Shuffle(a, WeaveBenchTables.DoubleIndices128) | WeaveBenchTables.DoubleCode128).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
                Unsafe.Add(ref d, o + 18) = 0xcb;
                Unsafe.Add(ref d, o + 35) = Unsafe.As<double, byte>(ref Unsafe.Add(ref src, i + 3));
                var b = Vector128.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)(i + 2)).AsByte();
                (Ssse3.Shuffle(b, WeaveBenchTables.DoubleIndices128) | WeaveBenchTables.DoubleCode128).StoreUnsafe(ref Unsafe.Add(ref d, o + 19));
            }
            for (; i + 2 <= length; i += 2, o += 18)
            {
                Unsafe.Add(ref d, o) = 0xcb;
                Unsafe.Add(ref d, o + 17) = Unsafe.As<double, byte>(ref Unsafe.Add(ref src, i + 1));
                var loaded = Vector128.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                (Ssse3.Shuffle(loaded, WeaveBenchTables.DoubleIndices128) | WeaveBenchTables.DoubleCode128).StoreUnsafe(ref Unsafe.Add(ref d, o + 1));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // ---- AVX2/SSSE3 decode weaves: the missing non-VBMI tiers. Float 256 pulls 6
    // tokens per 32B window: lane 0 holds tokens 0-2, lane 1 holds the payloads of
    // tokens 3-5, so one in-lane vpshufb reverses all six payloads; the two 16B halves
    // store with a 12B offset and the 4 garbage bytes self-heal under the next write.
    // Validation is one vector compare against the code byte plus a movemask test of
    // the six code positions. Double 256 needs a second source for token 1 (its payload
    // straddles the lane boundary), supplied by a vpermq of the middle qwords. ----

    public static bool ReadFloatWeave256(ref byte s, ref float dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; count - i >= 8; i += 6, o += 30)
            {
                var w = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, o));
                ok &= (Vector256.Equals(w, Vector256.Create((byte)0xca)).ExtractMostSignificantBits() & 0x2108421u) == 0x2108421u;
                var shuffled = Avx2.Shuffle(w, WeaveBenchTables.FloatDecShuffle256);
                shuffled.GetLower().StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), (nuint)(i * 4));
                shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), (nuint)((i * 4) + 12));
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= Unsafe.Add(ref s, o) == 0xca;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt32BitsToSingle(bits);
        }
        return ok;
    }

    public static bool ReadDoubleWeave256(ref byte s, ref double dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; count - i >= 4; i += 3, o += 27)
            {
                var w = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, o));
                ok &= (Vector256.Equals(w, Vector256.Create((byte)0xcb)).ExtractMostSignificantBits() & 0x40201u) == 0x40201u;
                // token 1's payload (bytes 10-17) straddles the lane boundary: vpermq
                // brings q1q2 into a lane for it, tokens 0 and 2 shuffle from the raw window
                var vB = Avx2.Permute4x64(w.AsUInt64(), 0b00_00_10_01).AsByte();
                (Avx2.Shuffle(w, WeaveBenchTables.DoubleDecShuffleA256) | Avx2.Shuffle(vB, WeaveBenchTables.DoubleDecShuffleB256))
                    .StoreUnsafe(ref Unsafe.As<double, byte>(ref dst), (nuint)(i * 8));
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= Unsafe.Add(ref s, o) == 0xcb;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt64BitsToDouble(bits);
        }
        return ok;
    }

    public static bool ReadFloatWeave128(ref byte s, ref float dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; count - i >= 4; i += 3, o += 15)
            {
                var w = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, o));
                ok &= (Vector128.Equals(w, Vector128.Create((byte)0xca)).ExtractMostSignificantBits() & 0x421u) == 0x421u;
                Ssse3.Shuffle(w, WeaveBenchTables.FloatDecShuffle128).StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), (nuint)(i * 4));
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= Unsafe.Add(ref s, o) == 0xca;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt32BitsToSingle(bits);
        }
        return ok;
    }

    public static bool ReadDoubleWeave128(ref byte s, ref double dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; count - i >= 2; i += 1, o += 9)
            {
                var w = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, o));
                ok &= Unsafe.Add(ref s, o) == 0xcb;
                Ssse3.Shuffle(w, WeaveBenchTables.DoubleDecShuffle128).StoreUnsafe(ref Unsafe.As<double, byte>(ref dst), (nuint)(i * 8));
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= Unsafe.Add(ref s, o) == 0xcb;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt64BitsToDouble(bits);
        }
        return ok;
    }

    // ---- deserialize: fused-scalar reference and 512 gathers (contiguous input) ----

    public static bool ReadFloatFused(ref byte s, ref float dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        for (; i + 16 <= count; i += 16, o += 80)
        {
            ref byte w0 = ref Unsafe.Add(ref s, o);
            for (int t = 0; t < 16; t++)
            {
                ok &= Unsafe.Add(ref w0, t * 5) == 0xca;
                var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
                Unsafe.Add(ref dst, i + t) = BitConverter.UInt32BitsToSingle(bits);
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= Unsafe.Add(ref s, o) == 0xca;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt32BitsToSingle(bits);
        }
        return ok;
    }

    public static bool ReadFloatWeave512(ref byte s, ref float dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; i + 16 <= count; i += 16, o += 80)
            {
                ref byte w0 = ref Unsafe.Add(ref s, o);
                var lo512 = Vector512.LoadUnsafe(ref w0);
                var hi512 = Vector512.LoadUnsafe(ref Unsafe.Add(ref w0, 16));
                var codes4 = Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveBenchTables.FloatDecodeCodes, hi512).AsInt32();
                ok &= Vector512.EqualsAll(codes4, Vector512.Create(unchecked((int)0xcacacaca)));
                Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveBenchTables.FloatDecodeValues, hi512).AsInt32()
                    .StoreUnsafe(ref Unsafe.As<float, int>(ref dst), (nuint)i);
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= Unsafe.Add(ref s, o) == 0xca;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt32BitsToSingle(bits);
        }
        return ok;
    }

    public static bool ReadDoubleFused(ref byte s, ref double dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        for (; i + 16 <= count; i += 16, o += 144)
        {
            ref byte w0 = ref Unsafe.Add(ref s, o);
            for (int t = 0; t < 16; t++)
            {
                ok &= Unsafe.Add(ref w0, t * 9) == 0xcb;
                var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref w0, (t * 9) + 1)));
                Unsafe.Add(ref dst, i + t) = BitConverter.UInt64BitsToDouble(bits);
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= Unsafe.Add(ref s, o) == 0xcb;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt64BitsToDouble(bits);
        }
        return ok;
    }

    public static bool ReadDoubleWeave512(ref byte s, ref double dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            // lane 7 stores a garbage qword into dst[i + 7] (guard keeps it in bounds);
            // the next iteration or the scalar tail decodes that slot properly
            for (; i + 8 <= count; i += 7, o += 63)
            {
                var w = Vector512.LoadUnsafe(ref Unsafe.Add(ref s, o));
                var codes = Avx512Vbmi.PermuteVar64x8(w, WeaveBenchTables.DoubleDecodeCodes).AsInt64();
                ok &= Vector512.EqualsAll(codes, Vector512.Create(unchecked((long)0xCBCBCBCBCBCBCBCB)));
                Avx512Vbmi.PermuteVar64x8(w, WeaveBenchTables.DoubleDecodeValues).AsInt64()
                    .StoreUnsafe(ref Unsafe.As<double, long>(ref dst), (nuint)i);
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= Unsafe.Add(ref s, o) == 0xcb;
            var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, o + 1)));
            Unsafe.Add(ref dst, i) = BitConverter.UInt64BitsToDouble(bits);
        }
        return ok;
    }
}

// shared by GlobalSetup and Program.cs --verify
public static class FloatWeaveVerify
{
    public static bool VerifyAll(float[] floats, double[] doubles)
    {
        var ok = true;
        int n = floats.Length;

        var fExpected = new byte[5 * n + 64];
        fExpected.AsSpan().Fill(0xAA);
        var fLen = FloatWeaveCandidates.WriteFloatRegionScalar(ref MemoryMarshal.GetArrayDataReference(fExpected), ref MemoryMarshal.GetArrayDataReference(floats), n);
        ok &= fLen == 5 * n;
        var fWriters = new (string Name, Func<byte[], int> Run)[]
        {
            ("FloatWeave512", buf => FloatWeaveCandidates.WriteFloatWeave512(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave512Fused", buf => FloatWeaveCandidates.WriteFloatWeave512Fused(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave512Unroll320", buf => FloatWeaveCandidates.WriteFloatWeave512Unroll320(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave256", buf => FloatWeaveCandidates.WriteFloatWeave256(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave256Unroll2", buf => FloatWeaveCandidates.WriteFloatWeave256Unroll2(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave128", buf => FloatWeaveCandidates.WriteFloatWeave128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
            ("FloatWeave128Unroll2", buf => FloatWeaveCandidates.WriteFloatWeave128Unroll2(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(floats), n)),
        };
        foreach (var (name, run) in fWriters)
        {
            var actual = new byte[5 * n + 64];
            actual.AsSpan().Fill(0xAA);
            var len = run(actual);
            // weave tiers may overhang past the final token mid-stream but never past the
            // scalar-tail-covered end; allow scratch only within the +64 slack
            if (len != fLen || !actual.AsSpan(0, fLen).SequenceEqual(fExpected.AsSpan(0, fLen)))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave {name} n={n}");
            }
        }

        var dExpected = new byte[9 * n + 64];
        var dLen = FloatWeaveCandidates.WriteDoubleRegionScalar(ref MemoryMarshal.GetArrayDataReference(dExpected), ref MemoryMarshal.GetArrayDataReference(doubles), n);
        ok &= dLen == 9 * n;
        var dWriters = new (string Name, Func<byte[], int> Run)[]
        {
            ("DoubleWeave512", buf => FloatWeaveCandidates.WriteDoubleWeave512(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
            ("DoubleWeave512Fused", buf => FloatWeaveCandidates.WriteDoubleWeave512Fused(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
            ("DoubleWeave256", buf => FloatWeaveCandidates.WriteDoubleWeave256(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
            ("DoubleWeave256Unroll2", buf => FloatWeaveCandidates.WriteDoubleWeave256Unroll2(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
            ("DoubleWeave128", buf => FloatWeaveCandidates.WriteDoubleWeave128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
            ("DoubleWeave128Unroll2", buf => FloatWeaveCandidates.WriteDoubleWeave128Unroll2(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(doubles), n)),
        };
        foreach (var (name, run) in dWriters)
        {
            var actual = new byte[9 * n + 64];
            var len = run(actual);
            if (len != dLen || !actual.AsSpan(0, dLen).SequenceEqual(dExpected.AsSpan(0, dLen)))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave {name} n={n}");
            }
        }

        // decoders: bit-exact roundtrip of the canonical element bytes
        var fReaders = new (string Name, Func<float[], bool> Run)[]
        {
            ("ReadFloatFused", dst => FloatWeaveCandidates.ReadFloatFused(ref MemoryMarshal.GetArrayDataReference(fExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadFloatWeave512", dst => FloatWeaveCandidates.ReadFloatWeave512(ref MemoryMarshal.GetArrayDataReference(fExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadFloatWeave256", dst => FloatWeaveCandidates.ReadFloatWeave256(ref MemoryMarshal.GetArrayDataReference(fExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadFloatWeave128", dst => FloatWeaveCandidates.ReadFloatWeave128(ref MemoryMarshal.GetArrayDataReference(fExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        };
        foreach (var (name, run) in fReaders)
        {
            var dst = new float[n];
            if (!run(dst) || !MemoryMarshal.AsBytes(dst.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(floats.AsSpan())))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave {name} n={n}");
            }
        }
        var dReaders = new (string Name, Func<double[], bool> Run)[]
        {
            ("ReadDoubleFused", dst => FloatWeaveCandidates.ReadDoubleFused(ref MemoryMarshal.GetArrayDataReference(dExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadDoubleWeave512", dst => FloatWeaveCandidates.ReadDoubleWeave512(ref MemoryMarshal.GetArrayDataReference(dExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadDoubleWeave256", dst => FloatWeaveCandidates.ReadDoubleWeave256(ref MemoryMarshal.GetArrayDataReference(dExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadDoubleWeave128", dst => FloatWeaveCandidates.ReadDoubleWeave128(ref MemoryMarshal.GetArrayDataReference(dExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        };
        foreach (var (name, run) in dReaders)
        {
            var dst = new double[n];
            if (!run(dst) || !MemoryMarshal.AsBytes(dst.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(doubles.AsSpan())))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave {name} n={n}");
            }
        }

        // corrupted code byte: every decoder must report failure, not silently accept
        if (n >= 20)
        {
            var corrupted = (byte[])fExpected.Clone();
            corrupted[5 * 17] = 0xcb; // token 17's code
            var dst = new float[n];
            if (FloatWeaveCandidates.ReadFloatFused(ref MemoryMarshal.GetArrayDataReference(corrupted), ref MemoryMarshal.GetArrayDataReference(dst), n)
                || FloatWeaveCandidates.ReadFloatWeave512(ref MemoryMarshal.GetArrayDataReference(corrupted), ref MemoryMarshal.GetArrayDataReference(dst), n)
                || FloatWeaveCandidates.ReadFloatWeave256(ref MemoryMarshal.GetArrayDataReference(corrupted), ref MemoryMarshal.GetArrayDataReference(dst), n)
                || FloatWeaveCandidates.ReadFloatWeave128(ref MemoryMarshal.GetArrayDataReference(corrupted), ref MemoryMarshal.GetArrayDataReference(dst), n))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave corrupted-code accepted n={n}");
            }
            var dCorrupted = (byte[])dExpected.Clone();
            dCorrupted[9 * 9] = 0xca;
            var ddst = new double[n];
            if (FloatWeaveCandidates.ReadDoubleFused(ref MemoryMarshal.GetArrayDataReference(dCorrupted), ref MemoryMarshal.GetArrayDataReference(ddst), n)
                || FloatWeaveCandidates.ReadDoubleWeave512(ref MemoryMarshal.GetArrayDataReference(dCorrupted), ref MemoryMarshal.GetArrayDataReference(ddst), n)
                || FloatWeaveCandidates.ReadDoubleWeave256(ref MemoryMarshal.GetArrayDataReference(dCorrupted), ref MemoryMarshal.GetArrayDataReference(ddst), n)
                || FloatWeaveCandidates.ReadDoubleWeave128(ref MemoryMarshal.GetArrayDataReference(dCorrupted), ref MemoryMarshal.GetArrayDataReference(ddst), n))
            {
                ok = false;
                Console.WriteLine($"NG FloatWeave double corrupted-code accepted n={n}");
            }
        }
        return ok;
    }
}

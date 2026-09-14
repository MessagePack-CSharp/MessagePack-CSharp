using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;
using MessagePack;

// Round 12: is `ref T value` on Serialize worth anything over plain `T value` or `in T value`?
// (Prompted by outside feedback that ref on the serialize side is "meaningless".)
//
// ABI hypothesis (win-x64): structs larger than 8 bytes are passed BY POINTER TO A
// CALLER-MADE COPY even "by value", so by-value never means "in registers" for real POCO
// structs — ref differs only by skipping the copy (win grows with struct size). For
// classes it is the other way around: by-value passes the reference in a register, while
// ref forces an addressable stack slot plus an indirect load in the callee (~1 mov each
// side). `in` has the same ABI as ref; its difference is language-level (defensive copies
// when the callee touches non-readonly members — not exercised here).
//
// The shapes only differ at a NON-inlined boundary: whenever the callsite inlines (GDV on
// monomorphic interface fields per round 11, or direct concrete calls), all three collapse
// to identical code. So the probe pins the boundary with [MethodImpl(NoInlining)] callees
// behind interface-typed fields (GDV may still devirtualize — fine, the calling convention
// is what's being measured, not dispatch). A Struct64 direct+AggressiveInlining pair
// demonstrates the collapse.
//
// Callee = fixed-width msgpack-ish field writes (0xd2 + 4 raw bytes each) into a byte[]
// buffer struct, small enough that the call boundary stays visible. Class values are
// spilled to a local before `ref` (mirrors generated code; also avoids ldelema's
// covariance check polluting the class-ref case). Struct refs point at array elements
// in place — the shape a `ref value.Field` chain gives source-gen for free.
//
// Measured (2 runs, ns/call): Class ref/val/in 2.79/2.77/2.55 (first run's ByRef 3.45 was
// noise; the rerun shows parity), Struct16 2.58/2.65/2.36, Struct64 8.83/9.08/8.64 — every
// non-inlined pairing is inside the 10% noise band. The asm shows the expected shapes:
// by-val structs >8B are indeed pointer-to-copy (xmm/ymm copy at the callsite), class-ref
// callees do reload the object pointer through the byref before every field (heap-alias
// conservatism) and the caller spills the reference — but none of it clears the noise
// floor against a real store-heavy callee. `in`'s edge is a GDV-lottery direct call in
// these runs, not the parameter shape. The one decisive result is the INLINED pair:
// Struct64InlineByRef 6.32 vs ByVal 7.33 (+16%) — inlining does NOT elide the by-value
// 64B copy (2x ymm load/store pairs survive in the loop) while ref reads the array in
// place. So: classes don't care, small structs don't care, and the case source-gen
// actually produces (devirtualized+inlined formatter calls, round 11) is exactly where
// `ref` wins on big structs. Keep `ref T value`; the reasons that remain are semantic
// (Deserialize symmetry, in-place `ref value.Field` chains, no `in` defensive copies)
// plus this inlined-copy elision failure — not the call-boundary ABI.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SerializeRefValueBenchmark
{
    const int N = 1000;

    ProbePoco[] classValues = default!;
    ProbeStruct16[] s16Values = default!;
    ProbeStruct64[] s64Values = default!;
    byte[] scratch = default!;

    readonly IProbeSerializeRef<ProbePoco> classRef = new ClassRefFormatter();
    readonly IProbeSerializeVal<ProbePoco> classVal = new ClassValFormatter();
    readonly IProbeSerializeIn<ProbePoco> classIn = new ClassInFormatter();
    readonly IProbeSerializeRef<ProbeStruct16> s16Ref = new Struct16RefFormatter();
    readonly IProbeSerializeVal<ProbeStruct16> s16Val = new Struct16ValFormatter();
    readonly IProbeSerializeIn<ProbeStruct16> s16In = new Struct16InFormatter();
    readonly IProbeSerializeRef<ProbeStruct64> s64Ref = new Struct64RefFormatter();
    readonly IProbeSerializeVal<ProbeStruct64> s64Val = new Struct64ValFormatter();
    readonly IProbeSerializeIn<ProbeStruct64> s64In = new Struct64InFormatter();
    readonly Struct64RefFormatter s64RefDirect = new();
    readonly Struct64ValFormatter s64ValDirect = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        classValues = new ProbePoco[N];
        s16Values = new ProbeStruct16[N];
        s64Values = new ProbeStruct64[N];
        for (int i = 0; i < N; i++)
        {
            classValues[i] = new ProbePoco { A = rand.Next(), B = rand.Next(), C = rand.Next(), D = rand.Next() };
            s16Values[i] = new ProbeStruct16 { A = rand.Next(), B = rand.Next(), C = rand.Next(), D = rand.Next() };
            var s64 = new ProbeStruct64();
            for (int f = 0; f < 16; f++) s64[f] = rand.Next();
            s64Values[i] = s64;
        }
        scratch = new byte[256];

        // every shape must produce byte-identical output over the full value set
        byte[] Run(Action<ProbeBufferRef> body)
        {
            var buf = new ProbeBufferRef { Buffer = new ProbeBuffer { Data = new byte[N * 96] } };
            body(buf);
            return buf.Buffer.Data;
        }
        var state = new SerializeState();
        var classExpected = Run(b => { for (int i = 0; i < N; i++) { var v = classValues[i]; classRef.Serialize(ref b.Buffer, ref state, ref v); } });
        Check(classExpected, Run(b => { for (int i = 0; i < N; i++) classVal.Serialize(ref b.Buffer, ref state, classValues[i]); }), "ClassVal");
        Check(classExpected, Run(b => { for (int i = 0; i < N; i++) classIn.Serialize(ref b.Buffer, ref state, in classValues[i]); }), "ClassIn");
        var s16Expected = Run(b => { for (int i = 0; i < N; i++) s16Ref.Serialize(ref b.Buffer, ref state, ref s16Values[i]); });
        Check(s16Expected, Run(b => { for (int i = 0; i < N; i++) s16Val.Serialize(ref b.Buffer, ref state, s16Values[i]); }), "Struct16Val");
        Check(s16Expected, Run(b => { for (int i = 0; i < N; i++) s16In.Serialize(ref b.Buffer, ref state, in s16Values[i]); }), "Struct16In");
        var s64Expected = Run(b => { for (int i = 0; i < N; i++) s64Ref.Serialize(ref b.Buffer, ref state, ref s64Values[i]); });
        Check(s64Expected, Run(b => { for (int i = 0; i < N; i++) s64Val.Serialize(ref b.Buffer, ref state, s64Values[i]); }), "Struct64Val");
        Check(s64Expected, Run(b => { for (int i = 0; i < N; i++) s64In.Serialize(ref b.Buffer, ref state, in s64Values[i]); }), "Struct64In");

        static void Check(byte[] expected, byte[] actual, string name)
        {
            if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name}");
        }
    }

    // verify writes sequentially through a shared buffer; class wrapper lets the local
    // functions capture it while the benchmarks below use a plain local ProbeBuffer
    sealed class ProbeBufferRef
    {
        public ProbeBuffer Buffer;
    }

    [BenchmarkCategory("Class"), Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int ClassByRef()
    {
        var f = classRef;
        var values = classValues;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            var v = values[i]; // generated code spills members to a local before ref
            f.Serialize(ref b, ref state, ref v);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Class"), Benchmark(OperationsPerInvoke = N)]
    public int ClassByVal()
    {
        var f = classVal;
        var values = classValues;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Class"), Benchmark(OperationsPerInvoke = N)]
    public int ClassByIn()
    {
        var f = classIn;
        var values = classValues;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, in values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct16"), Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int Struct16ByRef()
    {
        var f = s16Ref;
        var values = s16Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, ref values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct16"), Benchmark(OperationsPerInvoke = N)]
    public int Struct16ByVal()
    {
        var f = s16Val;
        var values = s16Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct16"), Benchmark(OperationsPerInvoke = N)]
    public int Struct16ByIn()
    {
        var f = s16In;
        var values = s16Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, in values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct64"), Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int Struct64ByRef()
    {
        var f = s64Ref;
        var values = s64Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, ref values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct64"), Benchmark(OperationsPerInvoke = N)]
    public int Struct64ByVal()
    {
        var f = s64Val;
        var values = s64Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct64"), Benchmark(OperationsPerInvoke = N)]
    public int Struct64ByIn()
    {
        var f = s64In;
        var values = s64Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.Serialize(ref b, ref state, in values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    // the collapse check: direct concrete calls with inlinable bodies — shape should not matter
    [BenchmarkCategory("Struct64Inline"), Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int Struct64InlineByRef()
    {
        var f = s64RefDirect;
        var values = s64Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.SerializeInlinable(ref b, ref state, ref values[i]);
            sum += b.Pos;
        }
        return sum;
    }

    [BenchmarkCategory("Struct64Inline"), Benchmark(OperationsPerInvoke = N)]
    public int Struct64InlineByVal()
    {
        var f = s64ValDirect;
        var values = s64Values;
        var b = new ProbeBuffer { Data = scratch };
        var state = new SerializeState();
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            b.Pos = 0;
            f.SerializeInlinable(ref b, ref state, values[i]);
            sum += b.Pos;
        }
        return sum;
    }
}

// ---- probe-local buffer (byte[] + pos; capacity is guaranteed by the harness) ----

public struct ProbeBuffer
{
    public byte[] Data;
    public int Pos;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte v)
    {
        Data[Pos] = v;
        Pos++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHeaderedInt32(int v)
    {
        var data = Data;
        var pos = Pos;
        data[pos] = 0xd2;
        Unsafe.WriteUnaligned(ref data[pos + 1], v); // capacity guaranteed by the harness
        Pos = pos + 5;
    }
}

// ---- the three interface shapes under test ----

public interface IProbeSerializeRef<T>
{
    void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ref T value);
}

public interface IProbeSerializeVal<T>
{
    void Serialize(ref ProbeBuffer buffer, ref SerializeState state, T value);
}

public interface IProbeSerializeIn<T>
{
    void Serialize(ref ProbeBuffer buffer, ref SerializeState state, in T value);
}

// ---- value types ----

public sealed class ProbePoco
{
    public int A;
    public int B;
    public int C;
    public int D;
}

public struct ProbeStruct16
{
    public int A;
    public int B;
    public int C;
    public int D;
}

[InlineArray(16)]
public struct ProbeStruct64
{
    int f0;
}

// ---- formatters (NoInlining pins the call boundary; GDV may devirtualize, which is fine) ----

public sealed class ClassRefFormatter : IProbeSerializeRef<ProbePoco>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ref ProbePoco value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class ClassValFormatter : IProbeSerializeVal<ProbePoco>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ProbePoco value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class ClassInFormatter : IProbeSerializeIn<ProbePoco>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, in ProbePoco value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class Struct16RefFormatter : IProbeSerializeRef<ProbeStruct16>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ref ProbeStruct16 value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class Struct16ValFormatter : IProbeSerializeVal<ProbeStruct16>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ProbeStruct16 value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class Struct16InFormatter : IProbeSerializeIn<ProbeStruct16>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, in ProbeStruct16 value)
    {
        buffer.WriteByte(0x94);
        buffer.WriteHeaderedInt32(value.A);
        buffer.WriteHeaderedInt32(value.B);
        buffer.WriteHeaderedInt32(value.C);
        buffer.WriteHeaderedInt32(value.D);
    }
}

public sealed class Struct64RefFormatter : IProbeSerializeRef<ProbeStruct64>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ref ProbeStruct64 value)
    {
        buffer.WriteByte(0xdc);
        for (int i = 0; i < 16; i++) buffer.WriteHeaderedInt32(value[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeInlinable(ref ProbeBuffer buffer, ref SerializeState state, ref ProbeStruct64 value)
    {
        buffer.WriteByte(0xdc);
        for (int i = 0; i < 16; i++) buffer.WriteHeaderedInt32(value[i]);
    }
}

public sealed class Struct64ValFormatter : IProbeSerializeVal<ProbeStruct64>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, ProbeStruct64 value)
    {
        buffer.WriteByte(0xdc);
        for (int i = 0; i < 16; i++) buffer.WriteHeaderedInt32(value[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeInlinable(ref ProbeBuffer buffer, ref SerializeState state, ProbeStruct64 value)
    {
        buffer.WriteByte(0xdc);
        for (int i = 0; i < 16; i++) buffer.WriteHeaderedInt32(value[i]);
    }
}

public sealed class Struct64InFormatter : IProbeSerializeIn<ProbeStruct64>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Serialize(ref ProbeBuffer buffer, ref SerializeState state, in ProbeStruct64 value)
    {
        buffer.WriteByte(0xdc);
        for (int i = 0; i < 16; i++) buffer.WriteHeaderedInt32(value[i]);
    }
}

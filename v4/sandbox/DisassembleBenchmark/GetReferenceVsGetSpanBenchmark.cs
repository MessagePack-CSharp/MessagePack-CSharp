extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static MessagePack.MessagePackPrimitives;

// Round 10: is IWriteBuffer.GetReference redundant? Every impl duplicates the GetSpan
// guard, and callers could get the same ref via MemoryMarshal.GetReference(GetSpan(n)) —
// IF the JIT promotes the inlined span and dead-codes the unused Length. On net10 the
// GetSpan fast path is MemoryMarshal.CreateSpan (no Slice range check), so the fast-path
// asm SHOULD be identical; this benchmark verifies that claim per write-buffer shape:
//   FixedSpanWriteBuffer     - pure fast path, throw-only guard: cleanest possible diff
//   ArrayPoolListWriteBuffer - real default path, guard falls back to a NoInlining
//                              GetSpanSlow returning a live span: the promotion-risky case
// If asm is byte-identical (module offsets aside), GetReference can be demoted to an
// extension method over GetSpan and dropped from the interface. Any leftover span
// materialization (store of length, span local spill) in the *_GetSpan variants kills
// the idea. Timing is confirmation only; the deliverable is the asm diff.
//
// Data note: 1000 elements repeated means the branch predictor learns the encoding-class
// pattern (CLAUDE.md pitfall) — irrelevant here because both variants share identical
// branch structure; asm identity is the question, not branch behavior.
//
// Measured (net10, x64): EQUIVALENT. The span's length half is fully dead-coded in every
// variant — no extra loads, stores, or span locals in either hot loop; the per-iteration
// instruction sequence is identical (guard reload -> movsxd+add address -> encode).
// Residual deltas are codegen jitter, not span cost:
//   Fixed 328B -> 331B: block layout only (loop-exit placement + one alignment nop)
//   Pool  763B -> 773B: regalloc pulled in r14/rbp (2 extra push/pop, rex bytes) and
//                       switched the induction variable from pointer-bump to base+index
// Mean 1.00-1.06 ratio, within ShortRun noise. Conclusion: GetReference can be demoted
// to a GetSpan-based extension method and dropped from IWriteBuffer.
//
// APPLIED: IWriteBuffer.GetReference removed; WriteBufferExtensions now derives it from
// GetSpan, so both variants below compile to the same path (kept as the regression probe).
// Post-change re-run: Pool 773B/773B byte-identical, Fixed 331B/328B with the SAME layout
// jitter now on the opposite side, extension fully inlined (no call in any hot loop).
// The 1.13-1.14 in-run Mean ratios between semantically identical code confirm alignment
// luck dominates at this scale — trust asm identity, not sub-15% Means, for this shape.
//
// Checked Advance re-measurement: the validation ((uint)bytesWritten > (uint)remaining ->
// throw) compiles to ONE cmp + predicted-not-taken jb per write, with the JIT reusing the
// remaining already computed for the GetSpan capacity guard (no extra sub). Fixed
// 331B -> 356B, Pool 873B -> 897B (cold throw block + 2 extra callee-saved pushes);
// Mean inside the established +-15% cross-run jitter band, matching round 8's SlimStrict
// finding that a predicted capacity branch per write is time-free.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class GetReferenceVsGetSpanBenchmark
{
    int[] values = default!;
    byte[] destination = default!; // FixedSpan destination AND ArrayPoolList scratch: 8KB > worst case 5B x 1000 + header, so the pool path never spills

    [GlobalSetup]
    public void Setup()
    {
        // deterministic mix over all five int32 encoding classes
        var rand = new Random(42);
        values = new int[1000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = rand.Next(5) switch
            {
                0 => rand.Next(-32, 128),                    // fixint
                1 => rand.Next(2) == 0 ? rand.Next(-128, -32) : rand.Next(128, 256),   // int8/uint8
                2 => rand.Next(2) == 0 ? rand.Next(-32768, -128) : rand.Next(256, 65536), // int16/uint16
                3 => rand.Next(2) == 0 ? rand.Next(int.MinValue, -32768) : rand.Next(65536, int.MaxValue), // int32/uint32
                _ => rand.Next(int.MinValue, int.MaxValue),  // anything
            };
        }
        destination = new byte[8192];

        // byte identity: all four paths must match the MessagePack-CSharp oracle
        var expected = V3::MessagePack.MessagePackSerializer.Serialize(values);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(Fixed_GetReference), RunFixed(viaGetSpan: false)),
            (nameof(Fixed_GetSpan), RunFixed(viaGetSpan: true)),
            (nameof(Pool_GetReference), RunPool(viaGetSpan: false)),
            (nameof(Pool_GetSpan), RunPool(viaGetSpan: true)),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name}");
        }
    }

    // ---- benchmark bodies (no try/finally: DisassemblyDiagnoser drops EH methods) ----

    [BenchmarkCategory("Fixed"), Benchmark(Baseline = true)]
    public long Fixed_GetReference()
    {
        var buffer = new SpanWriteBuffer(destination);
        WriteAllViaGetReference(ref buffer, values);
        return buffer.BytesWritten;
    }

    [BenchmarkCategory("Fixed"), Benchmark]
    public long Fixed_GetSpan()
    {
        var buffer = new SpanWriteBuffer(destination);
        WriteAllViaGetSpan(ref buffer, values);
        return buffer.BytesWritten;
    }

    [BenchmarkCategory("Pool"), Benchmark(Baseline = true)]
    public long Pool_GetReference()
    {
        var buffer = new ArrayPoolListWriteBuffer(destination);
        WriteAllViaGetReference(ref buffer, values);
        var written = buffer.BytesWritten;
        buffer.Dispose();
        return written;
    }

    [BenchmarkCategory("Pool"), Benchmark]
    public long Pool_GetSpan()
    {
        var buffer = new ArrayPoolListWriteBuffer(destination);
        WriteAllViaGetSpan(ref buffer, values);
        var written = buffer.BytesWritten;
        buffer.Dispose();
        return written;
    }

    // ---- the two access styles, in the exact generic shape of WriteBufferExtensions ----

    // current interface member: impl hands back the ref, no span ever exists
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteAllViaGetReference<TWriteBuffer>(ref TWriteBuffer buffer, int[] values)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        buffer.Advance(UnsafeWriteArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), values.Length));
        foreach (var v in values)
        {
            buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), v));
        }
    }

    // the proposed extension-method shape: derive the ref from the span, trusting
    // promotion + DCE to erase the length half
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteAllViaGetSpan<TWriteBuffer>(ref TWriteBuffer buffer, int[] values)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        buffer.Advance(UnsafeWriteArrayHeader(ref MemoryMarshal.GetReference(buffer.GetSpan(MaxArrayHeaderLength)), values.Length));
        foreach (var v in values)
        {
            buffer.Advance(UnsafeWriteInt32(ref MemoryMarshal.GetReference(buffer.GetSpan(MaxInt32Length)), v));
        }
    }

    // ---- verify helpers (Setup + Program.cs --verify) ----

    internal byte[] RunFixed(bool viaGetSpan)
    {
        var buffer = new SpanWriteBuffer(destination);
        if (viaGetSpan) WriteAllViaGetSpan(ref buffer, values);
        else WriteAllViaGetReference(ref buffer, values);
        return destination[..(int)buffer.BytesWritten];
    }

    internal byte[] RunPool(bool viaGetSpan)
    {
        var buffer = new ArrayPoolListWriteBuffer(destination);
        if (viaGetSpan) WriteAllViaGetSpan(ref buffer, values);
        else WriteAllViaGetReference(ref buffer, values);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result;
    }
}

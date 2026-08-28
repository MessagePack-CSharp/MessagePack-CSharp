using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;

// Probe for ArrayFormatter<TW,TR,T>.Deserialize's claim "AsSpan pays the array covariance
// check ONCE": taking `ref arr[i]` on a reference-type array is ldelema, which must
// type-check the array (the ref could be stored through), while Span<T>'s ctor does the
// exact-type check once and its indexer hands out unchecked refs. Serialize reads with
// plain ldelem (no store possible through a load), so it should need no AsSpan at all.
//
// The variants that matter are the GENERIC ones: ArrayFormatter's loop is shared
// (__Canon) code where the JIT cannot prove the element type, exactly like
// GenericLoops<T> below. The ExactSealed pair is a control: with a string[]-typed local
// the JIT knows nothing can derive from string, elides the check, and both shapes
// should tie — measuring only that shape would falsely acquit the ldelema cost.
// ValueType pair is the no-covariance control (specialized codegen, expect a tie).
// MEASURED (ShortRun + focused rerun, N=1000, ns/element):
//   Deser_Generic:     Array 2.07 (stable) | Span 1.64-1.93 (alignment-sensitive, always faster)
//     asm: Array pays `call CastHelpers.LdelemaRef` PER ELEMENT + checked barrier;
//     Span pays generic-dictionary lookup + one `cmp [arr],MT` upfront, then barrier only.
//     The comment's claim is real; both variants use CHECKED_ASSIGN_REF for the store.
//   Deser_ExactSealed: Array 0.94 | Span 1.42 (1.52x SLOWER!) — sealed elision kills the
//     ldelema check in the array version AND direct arr[i]=v gets the cheap plain
//     CORINFO_HELP_ASSIGN_REF, while the span store is demoted to CHECKED_ASSIGN_REF.
//     Lesson: do NOT blanket-apply AsSpan in exact-typed (non-generic) ref-storing loops.
//   Deser_ValueType:   0.225 | 0.229 — tie, no checks exist on either shape.
//   Ser_Generic:       Array 0.42 | Span 0.45 — element LOADS have no covariance check
//     (tight 40B loop); AsSpan only adds preamble. And AsSpan on the serialize side would
//     be a BUG: Span<T>'s ctor throws ArrayTypeMismatchException for covariant arrays
//     (object[] variable holding string[]), which plain reads accept.
// VERDICT: ArrayFormatter is right on both sides — AsSpan in Deserialize (real win,
// mechanism confirmed), no AsSpan in Serialize (fastest AND required for covariance).
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ArrayCovarianceBenchmark
{
    const int N = 1000;
    static readonly string Value = "v";

    string[] strings = default!;
    int[] ints = default!;

    [GlobalSetup]
    public void Setup()
    {
        strings = new string[N];
        Array.Fill(strings, "x");
        ints = new int[N];
    }

    // ---- deserialize shape (write through ref element), shared generic like the formatter ----

    [BenchmarkCategory("Deser_Generic"), Benchmark(Baseline = true)]
    public void GenericRef_Array() => GenericLoops<string>.FillViaArray(strings, Value);

    [BenchmarkCategory("Deser_Generic"), Benchmark]
    public void GenericRef_Span() => GenericLoops<string>.FillViaSpan(strings, Value);

    // ---- control: exact sealed element type (JIT can elide the check) ----

    [BenchmarkCategory("Deser_ExactSealed"), Benchmark(Baseline = true)]
    public void ExactSealed_Array() => FillExactViaArray(strings, Value);

    [BenchmarkCategory("Deser_ExactSealed"), Benchmark]
    public void ExactSealed_Span() => FillExactViaSpan(strings, Value);

    // ---- control: value type (no covariance exists) ----

    [BenchmarkCategory("Deser_ValueType"), Benchmark(Baseline = true)]
    public void ValueType_Array() => GenericLoops<int>.FillViaArray(ints, 42);

    [BenchmarkCategory("Deser_ValueType"), Benchmark]
    public void ValueType_Span() => GenericLoops<int>.FillViaSpan(ints, 42);

    // ---- serialize shape (plain element load), shared generic like the formatter ----

    [BenchmarkCategory("Ser_Generic"), Benchmark(Baseline = true)]
    public int SerializeRead_Array() => GenericLoops<string>.ReadViaArray(strings);

    [BenchmarkCategory("Ser_Generic"), Benchmark]
    public int SerializeRead_Span() => GenericLoops<string>.ReadViaSpan(strings);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FillExactViaArray(string[] arr, string value)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FillExactViaSpan(string[] arr, string value)
    {
        var span = arr.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = value;
        }
    }
}

static class GenericLoops<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FillViaArray(T[] arr, T value)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            Write(ref arr[i], value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FillViaSpan(T[] arr, T value)
    {
        var span = arr.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            Write(ref span[i], value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ReadViaArray(T[] arr)
    {
        int alive = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            alive += Consume(arr[i]);
        }
        return alive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ReadViaSpan(T[] arr)
    {
        int alive = 0;
        var span = arr.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            alive += Consume(span[i]);
        }
        return alive;
    }

    // the per-element "work": a bare store / null test, so the addressing mode
    // difference is the entire signal
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Write(ref T slot, T value) => slot = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Consume(T v) => v is null ? 0 : 1;
}

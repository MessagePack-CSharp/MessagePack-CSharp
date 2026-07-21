using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// Round 6: a perf question, not the diagnoser-NA question of rounds 1-5 — does the
// try/finally in UltraMessagePackSerializer.Serialize<T> cost anything at runtime?
// EntryShapeProbe (library-side, so it shares [SkipLocalsInit] and the internal formatter
// cache with the real entry) replays the exact entry shape with and without the EH region,
// plus a no-Dispose variant to separate Dispose cost from EH cost. Poco is the sensitive
// case (entry overhead dominates a ~20-byte payload); Array10k shows the amortized case.
// Note (round 2 finding): the try/finally variants come out as Code Size = NA in asm.md —
// capture their asm via: $env:DOTNET_JitDisasm = "*Serialize*Finally* *SerializeNoDispose*";
// dotnet run -c Release -- --jit-probe6
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DisasmProbe6Benchmark
{
    BenchPerson person = default!;
    int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        UltraMessagePack.DynamicFormatterFactory.Instance.RegisterFactory<BenchPerson>(new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };

        var rand = new Random(42);
        values = new int[10_000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = rand.Next(int.MinValue, int.MaxValue);
        }

        // all variants must produce identical bytes before speed means anything
        var expectedPoco = MessagePack.MessagePackSerializer.Serialize(person);
        var expectedArray = MessagePack.MessagePackSerializer.Serialize(values);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(PocoTryFinally), PocoTryFinally()),
            (nameof(PocoNoTryFinally), PocoNoTryFinally()),
            (nameof(PocoNoDispose), PocoNoDispose()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expectedPoco)) throw new InvalidOperationException($"verify failed: {name}");
        }
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(ArrayTryFinally), ArrayTryFinally()),
            (nameof(ArrayNoTryFinally), ArrayNoTryFinally()),
            (nameof(ArrayNoDispose), ArrayNoDispose()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expectedArray)) throw new InvalidOperationException($"verify failed: {name}");
        }
    }

    [BenchmarkCategory("Poco"), Benchmark(Baseline = true)]
    public byte[] PocoTryFinally() => EntryShapeProbe.SerializeTryFinally(person);

    [BenchmarkCategory("Poco"), Benchmark]
    public byte[] PocoNoTryFinally() => EntryShapeProbe.SerializeNoTryFinally(person);

    [BenchmarkCategory("Poco"), Benchmark]
    public byte[] PocoNoDispose() => EntryShapeProbe.SerializeNoDispose(person);

    [BenchmarkCategory("Array10k"), Benchmark(Baseline = true)]
    public byte[] ArrayTryFinally() => EntryShapeProbe.SerializeTryFinally(values);

    [BenchmarkCategory("Array10k"), Benchmark]
    public byte[] ArrayNoTryFinally() => EntryShapeProbe.SerializeNoTryFinally(values);

    [BenchmarkCategory("Array10k"), Benchmark]
    public byte[] ArrayNoDispose() => EntryShapeProbe.SerializeNoDispose(values);
}

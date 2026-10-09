using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// Deserialize cost of the collection formatters after PresizeCapacity (ReadBufferExtensions): the up-front
// allocation of a header-sized collection is capped at 8x the unread bytes, and a header past the cap grows the
// collection while reading. The honest rows (Guid, string, Dictionary) take the one-allocation path exactly as
// before plus one multiply and compare per collection; the all-nil long? row is the honest shape that does fall
// past the cap (1 wire byte, 16 in memory) and pays the growing path, so it bounds the cost of that path.
//
// RESULTS (i7-13700KF, MediumRun, net10, 10k elements each; before -> after, us):
//   Dictionary<int,Guid> 332.0 -> 337.1 | Guid[] 238.2 -> 175.7 | List<Guid> 182.7 -> 167.9 | Wide64[] 195.9 -> 188.6
//   string[] 124.7 -> 122.9 | long?[] all nil 15.4 -> 24.3 (156 -> 234 KB: the one doubling of the growing path)
// The one-allocation rows are unchanged or faster (the array loop moved into CollectionReads.ReadArray, where the
// JIT does better by the Guid[] row); only the row that honestly exceeds 8x in-memory/wire pays, and it pays one
// extra copy of half the array.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PresizeCapacityBenchmark
{
    const int N = 10_000;

    byte[] guids = default!;
    byte[] strings = default!;
    byte[] guidList = default!;
    byte[] dictionary = default!;
    byte[] allNil = default!;
    byte[] wide = default!;

    [MessagePackObject]
    public struct Wide64
    {
        [Key(0)] public long A;
        [Key(1)] public long B;
        [Key(2)] public long C;
        [Key(3)] public long D;
        [Key(4)] public long E;
        [Key(5)] public long F;
        [Key(6)] public long G;
        [Key(7)] public long H;
    }

    [GlobalSetup]
    public void Setup()
    {
        var g = new Guid[N];
        var s = new string[N];
        var d = new Dictionary<int, Guid>(N);
        var w = new Wide64[N];
        for (int i = 0; i < N; i++)
        {
            g[i] = Guid.NewGuid();
            s[i] = "item" + i;
            d[i] = g[i];
            w[i] = new Wide64 { A = i, H = -i };
        }
        guids = MessagePackSerializer.Serialize(g);
        strings = MessagePackSerializer.Serialize(s);
        guidList = MessagePackSerializer.Serialize(new List<Guid>(g));
        dictionary = MessagePackSerializer.Serialize(d);
        allNil = MessagePackSerializer.Serialize(new long?[N]);
        wide = MessagePackSerializer.Serialize(w);
    }

    [BenchmarkCategory("Guid[]"), Benchmark]
    public Guid[] GuidArray() => MessagePackSerializer.Deserialize<Guid[]>(guids);

    [BenchmarkCategory("string[]"), Benchmark]
    public string[] StringArray() => MessagePackSerializer.Deserialize<string[]>(strings);

    [BenchmarkCategory("List<Guid>"), Benchmark]
    public List<Guid> GuidList() => MessagePackSerializer.Deserialize<List<Guid>>(guidList);

    [BenchmarkCategory("Dictionary<int,Guid>"), Benchmark]
    public Dictionary<int, Guid> GuidDictionary() => MessagePackSerializer.Deserialize<Dictionary<int, Guid>>(dictionary);

    [BenchmarkCategory("Wide64[]"), Benchmark]
    public Wide64[] WideArray() => MessagePackSerializer.Deserialize<Wide64[]>(wide);

    [BenchmarkCategory("long?[] all nil (growing path)"), Benchmark]
    public long?[] AllNilArray() => MessagePackSerializer.Deserialize<long?[]>(allNil);
}

using BenchmarkDotNet.Attributes;

// LinkedListFormatter's enumeration strategy: foreach (struct Enumerator — a version
// check + Current copy per MoveNext) vs a manual node walk (First / node.Next).
// LinkedList<T> is circular internally, so BOTH pay a "next == head?" compare per step;
// the enumerator additionally pays the version guard. Per-element work here is a bare
// int add, i.e. the measured delta is the WORST-case share — inside the formatter each
// element also pays a virtual Serialize call, which dilutes whatever shows up here.
public class LinkedListWalkBenchmark
{
    LinkedList<int> data = default!;

    [Params(1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        data = new LinkedList<int>();
        for (int i = 0; i < N; i++) data.AddLast(rand.Next(0, 100));
    }

    [Benchmark(Baseline = true)]
    public int ForEachEnumerator()
    {
        int sum = 0;
        foreach (var x in data)
        {
            sum += x;
        }
        return sum;
    }

    [Benchmark]
    public int NodeWalk()
    {
        int sum = 0;
        for (var node = data.First; node != null; node = node.Next)
        {
            sum += node.Value;
        }
        return sum;
    }
}

using BenchmarkDotNet.Attributes;

// PriorityQueue deserialize tail shape: per-item Enqueue (sift-up per insert — average
// ~O(1) on random arrival but O(n log n) when priorities arrive descending, the min-heap
// worst case, and the ARRIVAL ORDER IS ATTACKER-CHOOSABLE in untrusted payloads) versus
// buffering the parsed pairs and bulk-building (Floyd bottom-up heapify, guaranteed O(n),
// at the price of a temp array + the ctor's internal copy). v3 ships the temp+ctor
// shape; v4's first cut enqueued per item. Candidates:
//   EnqueueLoop       — new PriorityQueue(count) + Enqueue per parsed pair (no temp)
//   CtorTemp          — parse into temp pairs array, new PriorityQueue(temp)
//   EnqueueRangeEmpty — parse into temp, EnqueueRange into an empty queue (also the
//                       heapify path; this is the shape the populate/reuse branch needs)
public class PriorityQueueBuildBenchmark
{
    [Params(16, 1000, 100_000)]
    public int N;

    [Params("Random", "Descending")]
    public string Dist = "";

    (int Element, int Priority)[] source = [];

    [GlobalSetup]
    public void Setup()
    {
        source = new (int, int)[N];
        var rng = new Random(42);
        for (var i = 0; i < N; i++)
        {
            var p = Dist == "Random" ? rng.Next() : N - i;
            source[i] = (i, p);
        }
        VerifyCandidates();
    }

    // all three shapes must drain to the identical (priority, element) multiset
    public static void VerifyCandidates()
    {
        var b = new PriorityQueueBuildBenchmark { N = 129, Dist = "Descending" };
        b.source = new (int, int)[129];
        for (var i = 0; i < 129; i++)
        {
            b.source[i] = (i, 129 - i);
        }

        static List<(int Priority, int Element)> Drain(PriorityQueue<int, int> q)
        {
            var drained = new List<(int, int)>();
            while (q.TryDequeue(out var e, out var p))
            {
                drained.Add((p, e));
            }
            drained.Sort(); // ties dequeue in arbitrary order — normalize
            return drained;
        }

        var a = Drain(b.EnqueueLoop());
        var c = Drain(b.CtorTemp());
        var d = Drain(b.EnqueueRangeEmpty());
        if (!a.SequenceEqual(c) || !a.SequenceEqual(d))
        {
            throw new InvalidOperationException("PriorityQueue build candidates disagree");
        }
    }

    [Benchmark(Baseline = true)]
    public PriorityQueue<int, int> EnqueueLoop()
    {
        var s = source;
        var q = new PriorityQueue<int, int>(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            q.Enqueue(s[i].Element, s[i].Priority);
        }
        return q;
    }

    [Benchmark]
    public PriorityQueue<int, int> CtorTemp()
    {
        var s = source;
        var temp = new (int, int)[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            temp[i] = s[i]; // stands in for the parse loop writing into the temp buffer
        }
        return new PriorityQueue<int, int>(temp);
    }

    [Benchmark]
    public PriorityQueue<int, int> EnqueueRangeEmpty()
    {
        var s = source;
        var temp = new (int, int)[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            temp[i] = s[i];
        }
        var q = new PriorityQueue<int, int>();
        q.EnqueueRange(temp);
        return q;
    }
}

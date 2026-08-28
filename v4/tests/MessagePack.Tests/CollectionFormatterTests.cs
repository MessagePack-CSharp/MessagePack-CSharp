extern alias V3;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// v3-parity sequence collections (CollectionFormatters.cs) + the bin-format byte views
// (PrimitiveFormatters.cs). Every case is wire-compared against MessagePack v3 AND
// roundtripped through our own reader.
public class CollectionFormatterTests
{
    static readonly int[][] Payloads = [[], [1], [1, -1, 128, -129, 70000, int.MaxValue, int.MinValue]];

    static void AssertOracleAndRoundtrip<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours));
    }

    [Fact]
    public void ConcreteSequenceCollections_MatchOracleAndRoundtrip()
    {
        foreach (var payload in Payloads)
        {
            AssertOracleAndRoundtrip(new LinkedList<int>(payload));
            AssertOracleAndRoundtrip(new Queue<int>(payload));
            AssertOracleAndRoundtrip(new Stack<int>(payload)); // xUnit enumerates: order (top-first) is asserted
            AssertOracleAndRoundtrip(new HashSet<int>(payload));
            AssertOracleAndRoundtrip(new SortedSet<int>(payload));
        }
    }

    [Fact]
    public void ObjectModelCollections_MatchOracleAndRoundtrip()
    {
        foreach (var payload in Payloads)
        {
            AssertOracleAndRoundtrip(new ReadOnlyCollection<int>(payload));
            AssertOracleAndRoundtrip(new ObservableCollection<int>(payload));
            AssertOracleAndRoundtrip(new ReadOnlyObservableCollection<int>(new ObservableCollection<int>(payload)));
        }
    }

    [Fact]
    public void ConcurrentCollections_MatchOracleAndRoundtrip()
    {
        foreach (var payload in Payloads)
        {
            AssertOracleAndRoundtrip(new ConcurrentQueue<int>(payload));
            AssertOracleAndRoundtrip(new ConcurrentStack<int>(payload));

            // a bag is unordered: wire-compare works (same instance enumerates the same
            // twice) but the roundtrip is compared as a multiset
            var bag = new ConcurrentBag<int>(payload);
            var ours = MessagePackSerializer.Serialize(bag);
            Assert.Equal(Oracle.Serialize(bag), ours);
            var back = MessagePackSerializer.Deserialize<ConcurrentBag<int>>(ours)!;
            Assert.Equal(bag.OrderBy(x => x), back.OrderBy(x => x));
        }
    }

    [Fact]
    public void ValueTypedViews_MatchOracleAndRoundtrip()
    {
        foreach (var payload in Payloads)
        {
            AssertOracleAndRoundtrip(new ArraySegment<int>(payload));

            var memory = new Memory<int>(payload);
            var memoryBytes = MessagePackSerializer.Serialize(memory);
            Assert.Equal(Oracle.Serialize(memory), memoryBytes);
            Assert.Equal(payload, MessagePackSerializer.Deserialize<Memory<int>>(memoryBytes).ToArray());

            var readOnlyMemory = new ReadOnlyMemory<int>(payload);
            var romBytes = MessagePackSerializer.Serialize(readOnlyMemory);
            Assert.Equal(Oracle.Serialize(readOnlyMemory), romBytes);
            Assert.Equal(payload, MessagePackSerializer.Deserialize<ReadOnlyMemory<int>>(romBytes).ToArray());
        }

        // views over a slice serialize only the window
        var backing = new[] { 9, 1, 2, 3, 9 };
        AssertOracleAndRoundtrip(new ArraySegment<int>(backing, 1, 3));
        Assert.Equal(
            MessagePackSerializer.Serialize(new[] { 1, 2, 3 }),
            MessagePackSerializer.Serialize(backing.AsMemory(1, 3)));

        // default ArraySegment = nil (v3 wire behavior); default Memory = empty array
        // (Assert.Equal can't enumerate a default segment, so compare wire + Array directly)
        var defaultSegmentBytes = MessagePackSerializer.Serialize(default(ArraySegment<int>));
        Assert.Equal(Oracle.Serialize(default(ArraySegment<int>)), defaultSegmentBytes);
        Assert.Null(MessagePackSerializer.Deserialize<ArraySegment<int>>(defaultSegmentBytes).Array);
        Assert.Equal(Oracle.Serialize(default(Memory<int>)), MessagePackSerializer.Serialize(default(Memory<int>)));
    }

    [Fact]
    public void ByteViews_KeepBinFormat_MatchOracleAndRoundtrip()
    {
        var payload = new byte[] { 0, 1, 0xc0, 0xff, 200 };

        var segment = new ArraySegment<byte>(payload, 1, 3);
        var segmentBytes = MessagePackSerializer.Serialize(segment);
        Assert.Equal(0xc4, segmentBytes[0]); // bin8, NOT an array of integers
        Assert.Equal(Oracle.Serialize(segment), segmentBytes);
        Assert.Equal(payload.Skip(1).Take(3), MessagePackSerializer.Deserialize<ArraySegment<byte>>(segmentBytes));

        var memory = new Memory<byte>(payload);
        var memoryBytes = MessagePackSerializer.Serialize(memory);
        Assert.Equal(0xc4, memoryBytes[0]);
        Assert.Equal(Oracle.Serialize(memory), memoryBytes);
        Assert.Equal(payload, MessagePackSerializer.Deserialize<Memory<byte>>(memoryBytes).ToArray());

        var readOnlyMemory = new ReadOnlyMemory<byte>(payload);
        var romBytes = MessagePackSerializer.Serialize(readOnlyMemory);
        Assert.Equal(0xc4, romBytes[0]);
        Assert.Equal(Oracle.Serialize(readOnlyMemory), romBytes);
        Assert.Equal(payload, MessagePackSerializer.Deserialize<ReadOnlyMemory<byte>>(romBytes).ToArray());

        // default segment = nil
        var defaultBytes = MessagePackSerializer.Serialize(default(ArraySegment<byte>));
        Assert.Equal(Oracle.Serialize(default(ArraySegment<byte>)), defaultBytes);
        Assert.Null(MessagePackSerializer.Deserialize<ArraySegment<byte>>(defaultBytes).Array);
    }

    [Fact]
    public void ByteList_WritesArray_ReadsBothArrayAndBin()
    {
        // write side: the array format on every TFM, byte-identical to v3 post-#2139
        var list = new List<byte> { 0, 1, 0xc0, 0xff, 200 };
        var ours = MessagePackSerializer.Serialize(list);
        Assert.Equal(Oracle.Serialize(list), ours);
        Assert.Equal(0x95, ours[0]); // fixarray(5), NOT bin
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<byte>>(ours));

        // read side: v3.0-era net8 builds wrote List<byte> as bin (#2134) — hand-built
        // bin payloads must deserialize, and must agree with v3's tolerant reader
        var bin8 = new byte[] { 0xc4, 5, 0, 1, 0xc0, 0xff, 200 };
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<byte>>(bin8));
        Assert.Equal(Oracle.Deserialize<List<byte>>(bin8), MessagePackSerializer.Deserialize<List<byte>>(bin8));

        var payload300 = Enumerable.Range(0, 300).Select(i => (byte)i).ToList();
        var bin16 = new byte[3 + 300];
        bin16[0] = 0xc5;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(bin16.AsSpan(1), 300);
        for (int i = 0; i < 300; i++) bin16[3 + i] = (byte)i;
        Assert.Equal(payload300, MessagePackSerializer.Deserialize<List<byte>>(bin16));
        Assert.Equal(Oracle.Deserialize<List<byte>>(bin16), MessagePackSerializer.Deserialize<List<byte>>(bin16));

        // empty bin and nil
        Assert.Equal(new List<byte>(), MessagePackSerializer.Deserialize<List<byte>>([0xc4, 0]));
        Assert.Null(MessagePackSerializer.Deserialize<List<byte>?>([0xc0]));

        // populate reuses the instance on BOTH read paths
        var target = new List<byte> { 9 };
        var before = target;
        MessagePackSerializer.Deserialize(bin8, ref target);
        Assert.Same(before, target);
        Assert.Equal(list, target);
        MessagePackSerializer.Deserialize(ours, ref target);
        Assert.Same(before, target);
        Assert.Equal(list, target);
    }

    [Fact]
    public void InterfaceCollections_MatchOracleAndRoundtrip()
    {
        foreach (var payload in Payloads)
        {
            AssertOracleAndRoundtrip<IEnumerable<int>>(payload);
            AssertOracleAndRoundtrip<ICollection<int>>(new List<int>(payload));
            AssertOracleAndRoundtrip<IList<int>>(new List<int>(payload));
            AssertOracleAndRoundtrip<IReadOnlyCollection<int>>(payload);
            AssertOracleAndRoundtrip<IReadOnlyList<int>>(payload);
            AssertOracleAndRoundtrip<ISet<int>>(new HashSet<int>(payload));
            AssertOracleAndRoundtrip<IReadOnlySet<int>>(new HashSet<int>(payload));
        }
    }

    sealed class SingleEnumerationSource : IEnumerable<int>
    {
        public int Enumerations;
        public IEnumerator<int> GetEnumerator()
        {
            Enumerations++;
            for (int i = 1; i <= 3; i++) yield return i;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void LazyEnumerable_SerializesCorrectly_EnumeratesOnce()
    {
        // a countless source must be buffered, not enumerated twice (v3 enumerates twice)
        var source = new SingleEnumerationSource();
        var bytes = MessagePackSerializer.Serialize<IEnumerable<int>>(source);
        Assert.Equal(1, source.Enumerations);
        Assert.Equal(MessagePackSerializer.Serialize(new[] { 1, 2, 3 }), bytes);
    }

    [Fact]
    public void NullCollections_SerializeAsNil()
    {
        Assert.Equal(Oracle.Serialize<LinkedList<int>?>(null), MessagePackSerializer.Serialize<LinkedList<int>?>(null));
        Assert.Null(MessagePackSerializer.Deserialize<LinkedList<int>?>(MessagePackSerializer.Serialize<LinkedList<int>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<HashSet<int>?>(MessagePackSerializer.Serialize<HashSet<int>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<IEnumerable<int>?>(MessagePackSerializer.Serialize<IEnumerable<int>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<ConcurrentQueue<int>?>(MessagePackSerializer.Serialize<ConcurrentQueue<int>?>(null)));
    }

    [Fact]
    public void Populate_MutableCollections_ReuseTheInstance()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { 10, 20 });

        var queue = new Queue<int>([1, 2, 3]);
        var queueTarget = queue;
        MessagePackSerializer.Deserialize(bytes, ref queueTarget);
        Assert.Same(queue, queueTarget);
        Assert.Equal([10, 20], queueTarget);

        var stackBytes = MessagePackSerializer.Serialize(new Stack<int>([1, 2, 3]));
        var stack = new Stack<int>([9]);
        var stackTarget = stack;
        MessagePackSerializer.Deserialize(stackBytes, ref stackTarget);
        Assert.Same(stack, stackTarget);
        Assert.Equal(new Stack<int>([1, 2, 3]), stackTarget);

        var linked = new LinkedList<int>([9]);
        var linkedTarget = linked;
        MessagePackSerializer.Deserialize(bytes, ref linkedTarget);
        Assert.Same(linked, linkedTarget);
        Assert.Equal([10, 20], linkedTarget);

        var observable = new ObservableCollection<int>([9]);
        int changes = 0;
        observable.CollectionChanged += (_, _) => changes++;
        var observableTarget = observable;
        MessagePackSerializer.Deserialize(bytes, ref observableTarget);
        Assert.Same(observable, observableTarget);
        Assert.Equal([10, 20], observableTarget);
        Assert.True(changes > 0); // populate goes through the notifying mutators

        // interface populate: mutable instance behind the interface is reused
        ICollection<int>? collection = new List<int> { 9 };
        var collectionBefore = collection;
        MessagePackSerializer.Deserialize(bytes, ref collection);
        Assert.Same(collectionBefore, collection);
        Assert.Equal([10, 20], collection);
    }

    [Fact]
    public void Populate_HashSet_KeepsComparer()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { "A", "B" });
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "x" };
        var target = set;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Assert.Same(set, target);
        Assert.Contains("a", target); // the case-insensitive comparer survived the populate
    }

    [Fact]
    public void Populate_Views_WriteThroughOnExactLength()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { 7, 8 });

        // Memory: exact length writes into the caller's backing array
        var memoryBacking = new int[2];
        Memory<int> memory = memoryBacking;
        MessagePackSerializer.Deserialize(bytes, ref memory);
        Assert.Equal([7, 8], memoryBacking);

        // ArraySegment: exact length writes through at the segment's offset
        var segmentBacking = new[] { 1, 2, 3, 4 };
        var segment = new ArraySegment<int>(segmentBacking, 1, 2);
        MessagePackSerializer.Deserialize(bytes, ref segment);
        Assert.Equal([1, 7, 8, 4], segmentBacking);
        Assert.Equal([7, 8], segment);

        // length mismatch: fresh backing store, the original is untouched
        var mismatched = new int[5];
        Memory<int> mismatchedMemory = mismatched;
        MessagePackSerializer.Deserialize(bytes, ref mismatchedMemory);
        Assert.Equal([7, 8], mismatchedMemory.ToArray());
        Assert.All(mismatched, v => Assert.Equal(0, v));
    }

    [Fact]
    public void NestedCollections_ResolveThroughTheChain()
    {
        var nested = new Queue<HashSet<int>>();
        nested.Enqueue([1, 2]);
        nested.Enqueue([]);
        AssertOracleAndRoundtrip(nested);

        var listOfStacks = new List<Stack<string>> { new(["a", "b"]), new() };
        var ours = MessagePackSerializer.Serialize(listOfStacks);
        Assert.Equal(Oracle.Serialize(listOfStacks), ours);
        var back = MessagePackSerializer.Deserialize<List<Stack<string>>>(ours)!;
        Assert.Equal(listOfStacks.Count, back.Count);
        Assert.Equal(listOfStacks[0], back[0]);
        Assert.Equal(listOfStacks[1], back[1]);
    }
}

extern alias V3;
using System.Collections;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// custom collection shapes served by the generic catch-all tier (v3
// DynamicGenericResolver's inherited-type rules): IEnumerable<T> plus a
// collection-accepting constructor, IReadOnlyCollection<T>, IReadOnlyDictionary<K,V>.
// The oracle is v3's StandardResolver, whose DynamicGenericResolver applied exactly
// these rules; the wire is a plain array/map, so byte identity is asserted throughout.
public class CustomCollectionTests
{
    static readonly V3::MessagePack.MessagePackSerializerOptions OracleStandard = V3::MessagePack.MessagePackSerializerOptions.Standard;

    [Fact]
    public void EnumerableWithCtor_RoundTrips_MatchingOracle()
    {
        var value = new EnumerableWithCtors { 1, 999, 3 };
        var bytes = V4.Serialize(value);
        Assert.Equal(Oracle.Serialize(value, OracleStandard), bytes);

        var back = V4.Deserialize<EnumerableWithCtors>(bytes)!;
        Assert.Equal([1, 999, 3], back.ToArray());
        // Activator's DefaultBinder picks the most specific applicable overload for the
        // TElement[] intermediate, never the decoy with the wrong element type
        Assert.NotEqual("decoy", back.ConstructedVia);
    }

    [Fact]
    public void ReadOnlyCollectionWithCtor_RoundTrips_MatchingOracle()
    {
        var value = new ReadOnlyCollectionWithCtor(new[] { 5, 6 });
        var bytes = V4.Serialize(value);
        Assert.Equal(Oracle.Serialize(value, OracleStandard), bytes);
        Assert.Equal([5, 6], V4.Deserialize<ReadOnlyCollectionWithCtor>(bytes)!.ToArray());
    }

    [Fact]
    public void ReadOnlyDictionaryWithCtor_RoundTrips_MatchingOracle()
    {
        var value = new ReadOnlyDictionaryWithCtor(new Dictionary<string, int> { ["a"] = 999, ["b"] = 2 });
        var bytes = V4.Serialize(value);
        Assert.Equal(Oracle.Serialize(value, OracleStandard), bytes);

        var back = V4.Deserialize<ReadOnlyDictionaryWithCtor>(bytes)!;
        Assert.Equal(999, back["a"]);
        Assert.Equal(2, back["b"]);
    }

    [Fact]
    public void ContractlessNoLongerSwallowsCollections()
    {
        // before the tier landed, contractless claimed this shape as a zero-member object
        // and wrote {} - silent data loss; now the collection tier claims it first
        var contractless = new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));
        var bytes = V4.Serialize(new EnumerableWithCtors { 9 }, contractless);
        Assert.Equal(new byte[] { 0x91, 0x09 }, bytes);
    }

    [Fact]
    public void StructEnumerableWithCtor_RoundTrips()
    {
        // the ctor-based paths have no class constraint (v3 allowed struct collections too);
        // the runtime ctor-matching tier serves it, which the coverage analyzer cannot see
#pragma warning disable MsgPack108
        var back = V4.Deserialize<StructEnumerable>(V4.Serialize(new StructEnumerable(new[] { 1, 2 })));
#pragma warning restore MsgPack108
        Assert.Equal([1, 2], back.ToArray());
    }

    public class EnumerableWithCtors : IEnumerable<int>
    {
        readonly List<int> items = new();

        public string ConstructedVia { get; private set; } = "init";

        public EnumerableWithCtors()
        {
        }

        public EnumerableWithCtors(IEnumerable<int> source)
        {
            items.AddRange(source);
            ConstructedVia = "IEnumerable<int>";
        }

        public EnumerableWithCtors(ICollection<int> source)
        {
            items.AddRange(source);
            ConstructedVia = "ICollection<int>";
        }

        public EnumerableWithCtors(IEnumerable<double> decoy)
        {
            ConstructedVia = "decoy";
        }

        public void Add(int item) => items.Add(item);

        public IEnumerator<int> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    public class ReadOnlyCollectionWithCtor : IReadOnlyCollection<int>
    {
        readonly List<int> items;

        public ReadOnlyCollectionWithCtor(IEnumerable<int> source) => items = new List<int>(source);

        public int Count => items.Count;

        public IEnumerator<int> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    public class ReadOnlyDictionaryWithCtor : IReadOnlyDictionary<string, int>
    {
        readonly Dictionary<string, int> map;

        public ReadOnlyDictionaryWithCtor(IDictionary<string, int> source) => map = new Dictionary<string, int>(source);

        public int this[string key] => map[key];

        public IEnumerable<string> Keys => map.Keys;

        public IEnumerable<int> Values => map.Values;

        public int Count => map.Count;

        public bool ContainsKey(string key) => map.ContainsKey(key);

        public bool TryGetValue(string key, out int value) => map.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => map.GetEnumerator();
    }

    public struct StructEnumerable : IEnumerable<int>
    {
        readonly int[] items;

        public StructEnumerable(IEnumerable<int> source) => items = source.ToArray();

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)(items ?? [])).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

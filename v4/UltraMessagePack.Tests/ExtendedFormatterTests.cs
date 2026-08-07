using System.Globalization;
using System.Net;
using System.Numerics;
using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// v4-only additions (no v3 oracle exists for any of these): Plane, the typed generic
// collection catch-all, Index/Range, CultureInfo, TimeZoneInfo, IPAddress/IPEndPoint.
// Wire shapes are asserted structurally instead.
public class ExtendedFormatterTests
{
    static void AssertRoundtrip<T>(T value)
    {
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value)));
    }

    [Fact]
    public void Plane_QuaternionShapedWire_AndRoundtrip()
    {
        var plane = new Plane(1f, 2f, 3f, 4f);
        // same constant shape as Quaternion: fixarray(4) + 4x float32
        Assert.Equal(MessagePackSerializer.Serialize((1f, 2f, 3f, 4f)), MessagePackSerializer.Serialize(plane));
        AssertRoundtrip(plane);
        AssertRoundtrip(new Plane(new Vector3(-0.5f, 0.25f, 1.5f), -9f));
        AssertRoundtrip((Plane?)null);
    }

    sealed class MyBag<T> : ICollection<T>
    {
        readonly List<T> items = [];
        public int Count => items.Count;
        public bool IsReadOnly => false;
        public void Add(T item) => items.Add(item);
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public bool Remove(T item) => items.Remove(item);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    sealed class MyDict : Dictionary<string, int>
    {
    }

    sealed class IntBag : ICollection<int> // NON-generic class, generic interface: must get the TYPED catch-all
    {
        readonly List<int> items = [];
        public int Count => items.Count;
        public bool IsReadOnly => false;
        public void Add(int item) => items.Add(item);
        public void Clear() => items.Clear();
        public bool Contains(int item) => items.Contains(item);
        public void CopyTo(int[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public bool Remove(int item) => items.Remove(item);
        public IEnumerator<int> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    [Fact]
    public void GenericCollectionCatchAll_TypedElements()
    {
        // custom generic collection: same wire as the equivalent List<T>
        var bag = new MyBag<int> { 1, -1, 70000 };
        var bytes = MessagePackSerializer.Serialize(bag);
        Assert.Equal(MessagePackSerializer.Serialize(new List<int> { 1, -1, 70000 }), bytes);
        Assert.Equal(bag.ToList(), MessagePackSerializer.Deserialize<MyBag<int>>(bytes)!.ToList());

        // Dictionary<K,V> SUBCLASS falls through the known definitions to the catch-all
        var dict = new MyDict { ["a"] = 1, ["b"] = 2 };
        var dictBytes = MessagePackSerializer.Serialize(dict);
        Assert.Equal(MessagePackSerializer.Serialize(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }), dictBytes);
        Assert.Equal(dict, MessagePackSerializer.Deserialize<MyDict>(dictBytes));

        // SortedSet<T> has a dedicated formatter (v3 parity); same wire either way
        var sorted = new SortedSet<int> { 3, 1, 2 };
        Assert.Equal(MessagePackSerializer.Serialize(new List<int> { 1, 2, 3 }), MessagePackSerializer.Serialize(sorted));
        Assert.Equal(sorted, MessagePackSerializer.Deserialize<SortedSet<int>>(MessagePackSerializer.Serialize(sorted)));

        // non-generic class implementing ICollection<int>: TYPED elements, not the
        // object-element tier (whose forced-width ints would produce different bytes)
        var intBag = new IntBag { 5, 6 };
        Assert.Equal(MessagePackSerializer.Serialize(new List<int> { 5, 6 }), MessagePackSerializer.Serialize(intBag));
        Assert.Equal(intBag.ToList(), MessagePackSerializer.Deserialize<IntBag>(MessagePackSerializer.Serialize(intBag))!.ToList());

        Assert.Null(MessagePackSerializer.Deserialize<MyBag<int>?>(MessagePackSerializer.Serialize<MyBag<int>?>(null)));
    }

    [Fact]
    public void ReadOnlySet_WrapsAndRoundtrips()
    {
        // same wire as the backing set (both enumerate the same storage)
        var backing = new HashSet<int> { 3, 1, 70000 };
        var wrapped = new System.Collections.ObjectModel.ReadOnlySet<int>(backing);
        Assert.Equal(MessagePackSerializer.Serialize(backing), MessagePackSerializer.Serialize(wrapped));

        var back = MessagePackSerializer.Deserialize<System.Collections.ObjectModel.ReadOnlySet<int>>(MessagePackSerializer.Serialize(wrapped))!;
        Assert.Equal(backing.OrderBy(x => x), back.OrderBy(x => x));

        Assert.Null(MessagePackSerializer.Deserialize<System.Collections.ObjectModel.ReadOnlySet<int>?>(
            MessagePackSerializer.Serialize<System.Collections.ObjectModel.ReadOnlySet<int>?>(null)));
    }

    [Fact]
    public void IndexAndRange_Roundtrip()
    {
        foreach (var index in new[] { Index.Start, Index.End, new Index(5), ^5, new Index(int.MaxValue) })
        {
            AssertRoundtrip(index);
        }
        // from-end travels one's-complemented: ^2 == -3 on the wire
        Assert.Equal(MessagePackSerializer.Serialize(-3), MessagePackSerializer.Serialize(^2));

        foreach (var range in new[] { .., 1..5, ..^1, ^5..^1, 2.. })
        {
            AssertRoundtrip(range);
        }
    }

    [Fact]
    public void CultureInfo_NameWire_AndRoundtrip()
    {
        // wire = the name string (Nerdbank.MessagePack-compatible)
        Assert.Equal(MessagePackSerializer.Serialize("ja-JP"), MessagePackSerializer.Serialize(CultureInfo.GetCultureInfo("ja-JP")));
        AssertRoundtrip(CultureInfo.GetCultureInfo("ja-JP"));
        AssertRoundtrip(CultureInfo.InvariantCulture); // "" name
        AssertRoundtrip((CultureInfo?)null);
    }

    [Fact]
    public void TimeZoneInfo_IdWire_AndRoundtrip()
    {
        AssertRoundtrip(TimeZoneInfo.Utc);
        AssertRoundtrip(TimeZoneInfo.Local);
        Assert.Equal(MessagePackSerializer.Serialize("UTC"), MessagePackSerializer.Serialize(TimeZoneInfo.Utc));
        AssertRoundtrip((TimeZoneInfo?)null);

        var bogus = MessagePackSerializer.Serialize("Not/A_Real_Zone");
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<TimeZoneInfo?>(bogus));
    }

    [Fact]
    public void IPAddress_And_IPEndPoint_Roundtrip()
    {
        foreach (var address in new[]
        {
            IPAddress.Parse("192.168.1.1"), IPAddress.Loopback, IPAddress.IPv6Loopback,
            IPAddress.Parse("fe80::1%5"), // scope id survives the string form
        })
        {
            AssertRoundtrip(address);
        }
        AssertRoundtrip((IPAddress?)null);

        AssertRoundtrip(new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8080));
        AssertRoundtrip(new IPEndPoint(IPAddress.IPv6Loopback, 65535));
        AssertRoundtrip((IPEndPoint?)null);

        // out-of-range port is a data error, not an ArgumentOutOfRangeException
        var badPort = MessagePackSerializer.Serialize(("1.2.3.4", 70000));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<IPEndPoint?>(badPort));

        var badAddress = MessagePackSerializer.Serialize(("not-an-ip", 80));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<IPEndPoint?>(badAddress));
    }
}

using System.Collections;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// object handling (PrimitiveObjectFormatter) and the non-generic collection world.
// The oracle is v3 with PrimitiveObjectResolver composed FIRST — the exact configuration
// this design mirrors (StandardResolver alone routes object through
// DynamicObjectTypeFallback, which is a different beast).
public class PrimitiveObjectFormatterTests
{
    static MessagePack.MessagePackSerializerOptions OracleOptions { get; } =
        MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.PrimitiveObjectResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance));

    [Fact]
    public void Primitives_ForcedWidth_MatchOracleAndRoundtripAsExactType()
    {
        // forced-width integer codes carry the exact .NET type through the roundtrip
        object[] values =
        [
            true, (sbyte)-1, (byte)200, (short)-2, (ushort)3, 42, 5u,
            long.MinValue, ulong.MaxValue, 1.5f, 2.5, "str",
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc),
        ];
        foreach (var value in values)
        {
            var ours = MessagePackSerializer.Serialize(value);
            Assert.Equal(Oracle.Serialize(value, OracleOptions), ours);
            var back = MessagePackSerializer.Deserialize<object>(ours);
            Assert.Equal(value, back);
            Assert.Equal(value.GetType(), back!.GetType());
        }

        // char is wire-compatible but does NOT roundtrip as char (smallest-int collapses
        // it; v3 has the same behavior)
        Assert.Equal(Oracle.Serialize((object)'x', OracleOptions), MessagePackSerializer.Serialize((object)'x'));

        // byte[] compares structurally
        var bin = MessagePackSerializer.Serialize((object)new byte[] { 1, 2, 3 });
        Assert.Equal(Oracle.Serialize((object)new byte[] { 1, 2, 3 }, OracleOptions), bin);
        Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])MessagePackSerializer.Deserialize<object>(bin)!);

        // null
        Assert.Null(MessagePackSerializer.Deserialize<object>(MessagePackSerializer.Serialize<object?>(null)));

        // boxed enum serializes as its underlying integer (comes back as int, not enum)
        var day = MessagePackSerializer.Serialize((object)DayOfWeek.Friday);
        Assert.Equal(Oracle.Serialize((object)DayOfWeek.Friday, OracleOptions), day);
        Assert.Equal((int)DayOfWeek.Friday, MessagePackSerializer.Deserialize<object>(day));
    }

    [Fact]
    public void CompactIntegerFromForeignWriter_ReadsAsByte()
    {
        // positive fixint (how every normal writer encodes small ints) reads as byte (v3 rule)
        var back = MessagePackSerializer.Deserialize<object>(MessagePackSerializer.Serialize(5));
        Assert.Equal((byte)5, back);
    }

    [Fact]
    public void ObjectGraph_RoundtripsStructurally()
    {
        var graph = new object?[]
        {
            42, "text", null, true,
            new object?[] { 1L, "nested" },
            new Dictionary<object, object?> { ["key"] = 123, ["other"] = null },
        };
        var bytes = MessagePackSerializer.Serialize<object>(graph);
        var back = (object?[])MessagePackSerializer.Deserialize<object>(bytes)!;

        Assert.Equal(42, back[0]);
        Assert.Equal("text", back[1]);
        Assert.Null(back[2]);
        Assert.Equal(true, back[3]);
        var nested = (object?[])back[4]!;
        Assert.Equal(1L, nested[0]);
        Assert.Equal("nested", nested[1]);
        var map = (Dictionary<object, object?>)back[5]!;
        Assert.Equal(123, map["key"]);
        Assert.Null(map["other"]);
    }

    [Fact]
    public void NonGenericCollections_MatchOracleAndRoundtrip()
    {
        var arrayList = new ArrayList { 1, "two", null, true };
        var ours = MessagePackSerializer.Serialize(arrayList);
        Assert.Equal(Oracle.Serialize(arrayList, OracleOptions), ours);
        var backList = MessagePackSerializer.Deserialize<ArrayList>(ours)!;
        Assert.Equal(4, backList.Count);
        Assert.Equal(1, backList[0]);
        Assert.Equal("two", backList[1]);
        Assert.Null(backList[2]);

        var hashtable = new Hashtable { ["a"] = 1 }; // single entry: Hashtable enumeration order is unspecified
        var mapBytes = MessagePackSerializer.Serialize(hashtable);
        Assert.Equal(Oracle.Serialize(hashtable, OracleOptions), mapBytes);
        var backTable = MessagePackSerializer.Deserialize<Hashtable>(mapBytes)!;
        Assert.Equal(1, backTable["a"]);

        // interfaces: IList/ICollection/IEnumerable deserialize as object[], IDictionary
        // as Dictionary<object, object?>
        var viaInterface = MessagePackSerializer.Serialize<IList>(arrayList);
        Assert.Equal(ours, viaInterface); // same wire as the concrete type
        Assert.IsType<object?[]>(MessagePackSerializer.Deserialize<IList>(viaInterface), exactMatch: false);
        Assert.IsType<object?[]>(MessagePackSerializer.Deserialize<IEnumerable>(viaInterface), exactMatch: false);
        Assert.IsType<Dictionary<object, object?>>(MessagePackSerializer.Deserialize<IDictionary>(mapBytes), exactMatch: false);

        Assert.Null(MessagePackSerializer.Deserialize<ArrayList?>(MessagePackSerializer.Serialize<ArrayList?>(null)));
    }

    [Fact]
    public void ObjectPath_StaysSelfDescribing_EvenWithSwappedFormatters()
    {
        // the object path deliberately bypasses formatter swaps: its contract is that
        // values roundtrip through SELF-DESCRIBING wire types. Under DotNetOptimized the
        // TYPED DateTime path writes ToBinary int64, but a BOXED DateTime still writes
        // the timestamp ext — and therefore still comes back as a DateTime.
        var opt = new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.DotNetOptimized]));
        var dt = new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc);

        var boxed = MessagePackSerializer.Serialize<object>(dt, opt);
        Assert.NotEqual(MessagePackSerializer.Serialize(dt, opt), boxed); // typed = int64, boxed = timestamp ext
        Assert.Equal(boxed, MessagePackSerializer.Serialize<object>(dt)); // boxed = the default self-describing form
        Assert.Equal(dt, MessagePackSerializer.Deserialize<object>(boxed, opt)); // and it roundtrips as DateTime
    }

    [Fact]
    public void UnsupportedRuntimeType_Throws()
    {
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Serialize<object>(new Uri("https://example.com")));
    }

    [Fact]
    public void DuplicateOrNilMapKeys_AreDataErrors()
    {
        // fixmap(2) { "a": 1, "a": 2 }
        var duplicate = new byte[] { 0x82, 0xA1, (byte)'a', 0x01, 0xA1, (byte)'a', 0x02 };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(duplicate));

        // fixmap(1) { nil: 1 }
        var nilKey = new byte[] { 0x81, 0xC0, 0x01 };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(nilKey));
    }
}


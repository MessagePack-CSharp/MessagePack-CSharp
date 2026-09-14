extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// generic wrapper family (KeyValuePair / ValueTuple / Tuple / Lazy): every case is
// wire-compared against MessagePack v3 AND roundtripped through our own reader.
public class WrapperFormatterTests
{
    static void AssertOracleAndRoundtrip<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours));
    }

    [Fact]
    public void KeyValuePair_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(new KeyValuePair<int, string>(1, "one"));
        AssertOracleAndRoundtrip(new KeyValuePair<string, int>("key", -42));

        // array-valued pair: compare parts (KVP.Equals would compare the array by reference)
        var pair = new KeyValuePair<string, int[]>("k", [1, 2, 3]);
        var ours = MessagePackSerializer.Serialize(pair);
        Assert.Equal(Oracle.Serialize(pair), ours);
        var back = MessagePackSerializer.Deserialize<KeyValuePair<string, int[]>>(ours);
        Assert.Equal(pair.Key, back.Key);
        Assert.Equal(pair.Value, back.Value);

        // nested in a list (the common DTO shape)
        var list = new List<KeyValuePair<int, string>> { new(1, "a"), new(2, "b") };
        var listBytes = MessagePackSerializer.Serialize(list);
        Assert.Equal(Oracle.Serialize(list), listBytes);
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<KeyValuePair<int, string>>>(listBytes));
    }

    [Fact]
    public void ValueTuple_AllArities_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(ValueTuple.Create(1));
        AssertOracleAndRoundtrip((1, "two"));
        AssertOracleAndRoundtrip((1, "two", 3.5));
        AssertOracleAndRoundtrip((1, 2, 3, 4));
        AssertOracleAndRoundtrip((1, 2, 3, 4, 5));
        AssertOracleAndRoundtrip((1, 2, 3, 4, 5, 6));
        AssertOracleAndRoundtrip((1, 2, 3, 4, 5, 6, 7));
        AssertOracleAndRoundtrip((1, 2, 3, 4, 5, 6, 7, 8));          // arity 8: Rest = ValueTuple<int>
        AssertOracleAndRoundtrip((1, 2, 3, 4, 5, 6, 7, 8, 9, 10));   // Rest = ValueTuple<int,int,int>
        AssertOracleAndRoundtrip((Item1: "mixed", Item2: (int?)null, Item3: 3.5));
    }

    [Fact]
    public void Tuple_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(Tuple.Create(1));
        AssertOracleAndRoundtrip(Tuple.Create(1, "two"));
        AssertOracleAndRoundtrip(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
        AssertOracleAndRoundtrip(new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8)));
        AssertOracleAndRoundtrip((Tuple<int, string>?)null);
    }

    [Fact]
    public void Tuple_And_ValueTuple_ShareWire()
    {
        // class and struct tuples are indistinguishable on the wire (both array[N]):
        // cross-deserialization works, matching v3
        var bytes = MessagePackSerializer.Serialize((1, "two"));
        Assert.Equal(bytes, MessagePackSerializer.Serialize(Tuple.Create(1, "two")));
        Assert.Equal(Tuple.Create(1, "two"), MessagePackSerializer.Deserialize<Tuple<int, string>>(bytes));
    }

    [Fact]
    public void Lazy_TransparentWire_AndRoundtrip()
    {
        // no Lazy trace on the wire: identical bytes to the plain value
        var lazyBytes = MessagePackSerializer.Serialize(new Lazy<int>(42));
        Assert.Equal(MessagePackSerializer.Serialize(42), lazyBytes);
        Assert.Equal(Oracle.Serialize(new Lazy<int>(42)), lazyBytes);
        Assert.Equal(42, MessagePackSerializer.Deserialize<Lazy<int>>(lazyBytes)!.Value);

        // deserialized instance is pre-evaluated (documented v3 semantics)
        Assert.True(MessagePackSerializer.Deserialize<Lazy<int>>(lazyBytes)!.IsValueCreated);

        Assert.Null(MessagePackSerializer.Deserialize<Lazy<int>?>(MessagePackSerializer.Serialize<Lazy<int>?>(null)));

        var lazyString = MessagePackSerializer.Deserialize<Lazy<string>>(MessagePackSerializer.Serialize(new Lazy<string>(() => "computed")));
        Assert.Equal("computed", lazyString!.Value);
    }

    [Fact]
    public void MalformedCounts_SurfaceAsSerializationException()
    {
        // an array of the wrong arity is malformed data for tuples and pairs
        var threeElements = MessagePackSerializer.Serialize((1, 2, 3));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<(int, int)>(threeElements));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Tuple<int, int>>(threeElements));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<KeyValuePair<int, int>>(threeElements));

        // nil is not a ValueTuple (struct), but IS a valid null Tuple (class)
        var nil = new byte[] { 0xC0 };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<(int, int)>(nil));
        Assert.Null(MessagePackSerializer.Deserialize<Tuple<int, int>>(nil));
    }
}

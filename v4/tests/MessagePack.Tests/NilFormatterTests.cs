using MessagePack;
using Xunit;

namespace MessagePack.Tests;

// Nil is the unit type for the msgpack nil token. No oracle comparison here: v3's Nil is
// a different CLR type, and the wire is trivially the single 0xc0 byte.
public class NilFormatterTests
{
    [Fact]
    public void Nil_IsTheNilByte_AndRoundtrips()
    {
        var bytes = MessagePackSerializer.Serialize(Nil.Default);
        Assert.Equal(new byte[] { 0xC0 }, bytes);
        Assert.Equal(Nil.Default, MessagePackSerializer.Deserialize<Nil>(bytes));

        // inside a collection
        var array = MessagePackSerializer.Serialize(new Nil[3]);
        Assert.Equal(new byte[] { 0x93, 0xC0, 0xC0, 0xC0 }, array);
        Assert.Equal(new Nil[3], MessagePackSerializer.Deserialize<Nil[]>(array));
    }

    [Fact]
    public void NullableNil_AlwaysDeserializesToValue_NeverNull()
    {
        // v3 semantics: "NullableNil is same as Nil" — null and Nil share the wire, and
        // reading always produces the VALUE (the generic Nullable tier would give null)
        Assert.Equal(new byte[] { 0xC0 }, MessagePackSerializer.Serialize((Nil?)Nil.Default));
        Assert.Equal(new byte[] { 0xC0 }, MessagePackSerializer.Serialize((Nil?)null));

        var back = MessagePackSerializer.Deserialize<Nil?>(new byte[] { 0xC0 });
        Assert.True(back.HasValue);
    }

    [Fact]
    public void NonNilCode_IsMalformedData()
    {
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Nil>(new byte[] { 0x01 }));
    }
}

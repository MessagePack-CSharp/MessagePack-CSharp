using MessagePack;
using MessagePack.Formatters;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// a `dynamic` member: the generated formatter reads and writes it as object, never through a dynamic call site
// (which could not take the ref struct buffers, CS1978); compiling is the first assertion
[MessagePackObject]
public class DynamicHolder
{
    [Key(0)] public dynamic? Value { get; set; }

    [Key(1)] public int Other { get; set; }

    // a factory attribute on a dynamic member: the factory is asked for object (typeof(dynamic) would be CS1962)
    [Key(2)]
    [MessagePackFormatter(typeof(DynamicSlotFactory))]
    public dynamic? Custom { get; set; }

    // and a formatter attribute matches object's formatter
    [Key(3)]
    [MessagePackFormatter(typeof(PrimitiveObjectFormatter<,>))]
    public dynamic? Primitive { get; set; }
}

public sealed class DynamicSlotFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => type == typeof(object) ? new PrimitiveObjectFormatter<TWriteBuffer, TReadBuffer>() : null;

    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
}

public class ReviewRound14RegressionTests
{
    [Fact]
    public void DynamicMember_RoundtripsAsObject()
    {
        var back = V4.Deserialize<DynamicHolder>(V4.Serialize(new DynamicHolder { Value = "text", Other = 2, Custom = 7, Primitive = "p" }))!;
        Assert.Equal("text", (string)back.Value!);
        Assert.Equal(2, back.Other);
        Assert.Equal(7, Convert.ToInt32((object)back.Custom!));
        Assert.Equal("p", (string)back.Primitive!);
        Assert.Null(V4.Deserialize<DynamicHolder>(V4.Serialize(new DynamicHolder()))!.Value);
    }

    // the bin-format views populate a same-length buffer in place, as the generic array / Memory formatters do
    [Fact]
    public void ByteBuffers_PopulateASameLengthBuffer()
    {
        var payload = V4.Serialize(new byte[] { 1, 2, 3 });

        var array = new byte[3];
        var sameArray = array;
        V4.Deserialize(payload, ref array);
        Assert.Same(sameArray, array);
        Assert.Equal([1, 2, 3], array);
        var shorter = new byte[2];
        var oldShorter = shorter;
        V4.Deserialize(payload, ref shorter);
        Assert.NotSame(oldShorter, shorter);
        Assert.Equal([1, 2, 3], shorter);

        var backing = new byte[3];
        var memory = new Memory<byte>(backing);
        V4.Deserialize(payload, ref memory);
        Assert.Equal([1, 2, 3], backing); // the caller's backing buffer saw the bytes
        var segmentBacking = new byte[5];
        var segment = new ArraySegment<byte>(segmentBacking, 1, 3);
        V4.Deserialize(payload, ref segment);
        Assert.Equal([0, 1, 2, 3, 0], segmentBacking);
        Assert.Same(segmentBacking, segment.Array);

        var mismatched = new Memory<byte>(new byte[2]);
        V4.Deserialize(payload, ref mismatched);
        Assert.Equal([1, 2, 3], mismatched.ToArray());

        // the array-of-integers compat form (producers that wrote byte arrays as arrays) populates the same way
        var fromArrayForm = new byte[3];
        var sameFromArrayForm = fromArrayForm;
        V4.Deserialize([0x93, 0x07, 0x08, 0x09], ref fromArrayForm);
        Assert.Same(sameFromArrayForm, fromArrayForm);
        Assert.Equal([7, 8, 9], fromArrayForm);
    }
}

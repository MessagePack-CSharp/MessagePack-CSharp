using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;
using AliasedSurrogate = MessagePack.IMessagePackSurrogate<MessagePack.Tests.AliasTarget, MessagePack.Tests.AliasTargetSurrogate>;

namespace MessagePack.Tests;

// an unknown-members packet is settable, so it can be moved to a type that declares one of the keys it holds: the
// replay must refuse rather than write the key twice into a map the type's own reader rejects
[MessagePackObject(true)]
public class PacketCarrierA
{
    public int A { get; set; }

    public MessagePackUnknownMembers? Packet { get; set; }
}

[MessagePackObject(true)]
public class PacketCarrierB
{
    public int A { get; set; }

    public int C { get; set; }

    public MessagePackUnknownMembers? Packet { get; set; }
}

// two cases whose sanitized type names coincide: the generated fields must still be distinct (compiling is the assertion)
[MessagePackObject]
[UnionTag(typeof(int[][]), 0)]
[UnionTag(typeof(int[,,]), 1)]
public union JaggedOrCube(int[][], int[,,]);

// the surrogate interface spelled through a using alias: the generator must still register the association
public class AliasTarget
{
    public int X { get; set; }
}

[MessagePackObject]
public record struct AliasTargetSurrogate([property: Key(0)] int X) : AliasedSurrogate
{
    public AliasTargetSurrogate ToSurrogate(AliasTarget value) => new(value.X);

    public AliasTarget ToTarget() => new() { X = X };
}

public class ReviewRound6RegressionTests
{
    // a broken-out (or finished) enumeration of a compressed message stream must leave the stream positioned after
    // the messages it consumed, so the caller can read the rest, exactly as with no MessageProcessor
    [Fact]
    public async Task MessageStream_WithProcessor_HandsTheReadAheadBack()
    {
        foreach (var options in new[] { V4Options.Default.WithFraming(), V4Options.Default.WithZstandardFrame() })
        {
            var stream = new MemoryStream();
            foreach (var i in new[] { 1, 2, 3 })
            {
                V4.Serialize(stream, i, options);
            }
            stream.Position = 0;

            // three enumerations, each broken out after one message: every one must start where the previous one
            // stopped consuming (the pipe's read-ahead went back to the stream), so the messages come out in order
            var seen = new List<int>();
            var positions = new List<long>();
            for (var round = 0; round < 3; round++)
            {
                await foreach (var value in V4.DeserializeMessagesAsync<int>(stream, options))
                {
                    seen.Add(value);
                    break;
                }
                positions.Add(stream.Position);
            }
            Assert.Equal([1, 2, 3], seen);
            Assert.True(positions[0] > 0 && positions[0] < positions[1] && positions[1] < positions[2]);
            Assert.Equal(stream.Length, positions[2]);
        }
    }

    [Fact]
    public void TransplantedPacket_WithADeclaredKey_IsRefused()
    {
        var a = V4.Deserialize<PacketCarrierA>(V4.Serialize(new PacketCarrierB { A = 1, C = 2 }))!;
        Assert.Equal(1, a.Packet!.Count); // "C" captured
        Assert.Equal(V4.Serialize(new PacketCarrierB { A = 1, C = 2 }), V4.Serialize(a)); // replayed by its own type

        var b = new PacketCarrierB { A = 1, C = 3, Packet = a.Packet };
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(b));
        Assert.Contains("\"C\"", ex.Message);
    }

    [Fact]
    public void UnionCasesWithCollidingSanitizedNames_Roundtrip()
    {
        JaggedOrCube jagged = new int[][] { [1, 2], [3] };
        var backJagged = V4.Deserialize<JaggedOrCube>(V4.Serialize(jagged));
        Assert.Equal(3, Assert.IsType<int[][]>(backJagged.Value)[1][0]);

        JaggedOrCube cube = new int[1, 1, 2] { { { 4, 5 } } };
        var backCube = V4.Deserialize<JaggedOrCube>(V4.Serialize(cube));
        Assert.Equal(5, Assert.IsType<int[,,]>(backCube.Value)[0, 0, 1]);
    }

    [Fact]
    public void AliasedSurrogateInterface_IsRegistered()
    {
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot);
        var formatter = resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, AliasTarget>();
        Assert.DoesNotContain("Missing", formatter.GetType().Name);

        var aot = V4Options.DefaultAot;
        Assert.Equal(7, V4.Deserialize<AliasTarget>(V4.Serialize(new AliasTarget { X = 7 }, aot), aot)!.X);
    }
}

extern alias V3;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MaxDepth guards both directions: deserialize stops adversarial deeply-nested payloads
// before formatter recursion overflows the stack; serialize turns cyclic object graphs
// into a clean exception instead of a process-killing StackOverflowException. DepthNode's
// formatter is source-generated, so these tests also pin the Emitter's Enter/
// Exit emission; the jagged-array cases pin the built-in collection formatters.
public class DepthLimitTest
{
    static byte[] NestedPayload(int levels)
    {
        var bytes = new byte[levels + 1];
        bytes.AsSpan(0, levels).Fill(0x91); // fixarray(1) per level
        bytes[levels] = 0xC0;               // terminal nil child
        return bytes;
    }

    static DepthNode Chain(int levels)
    {
        DepthNode? head = null;
        for (int i = 0; i < levels; i++)
        {
            head = new DepthNode { Child = head };
        }
        return head!;
    }

    [Fact]
    public void Deserialize_BeyondDefaultMaxDepth_Throws()
    {
        var options = MessagePackSerializerOptions.Default;
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<DepthNode>(NestedPayload(600), options));
        // the message names the option, not the configured number: the state deliberately
        // does not carry maxDepth (kept to a single int, see SerializeState)
        Assert.Contains("MaxDepth", ex.Message);

        // the same shape within the limit roundtrips completely
        var back = V4.Deserialize<DepthNode>(NestedPayload(100), options);
        int depth = 0;
        for (var n = back; n != null; n = n.Child)
        {
            depth++;
        }
        Assert.Equal(100, depth);
    }

    [Fact]
    public void Serialize_CyclicGraph_ThrowsInsteadOfStackOverflow()
    {
        var cyclic = new DepthNode();
        cyclic.Child = cyclic;
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => V4.Serialize(cyclic, MessagePackSerializerOptions.Default));
        Assert.Contains("cyclic", ex.Message);
    }

    [Fact]
    public void Serialize_DeepChain_RespectsMaxDepth()
    {
        var options = MessagePackSerializerOptions.Default;
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(Chain(600), options));
        Assert.Equal(NestedPayload(100), V4.Serialize(Chain(100), options));
    }

    [Fact]
    public void MaxDepth_IsConfigurable_AndBoundaryIsExact()
    {
        var tight = MessagePackSerializerOptions.Default with { MaxDepth = 3 };
        Assert.NotNull(V4.Deserialize<DepthNode>(NestedPayload(3), tight));
        Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<DepthNode>(NestedPayload(4), tight));
    }

    [Fact]
    public void EmptyArray_ReturnsSharedSingleton_WithoutLeakingDepth()
    {
        var options = MessagePackSerializerOptions.Default;
        Assert.Same(Array.Empty<double>(), V4.Deserialize<double[]>(new byte[] { 0x90 }, options));

        // regression guard for the early-return pairing: ten empty inner arrays under a
        // tight MaxDepth — a leaked Enter per sibling would blow the limit
        var manyEmpty = new double[10][];
        for (int i = 0; i < manyEmpty.Length; i++)
        {
            manyEmpty[i] = [];
        }
        var tight = MessagePackSerializerOptions.Default with { MaxDepth = 2 };
        var back = V4.Deserialize<double[][]>(V4.Serialize(manyEmpty, tight), tight);
        Assert.Equal(manyEmpty, back);
    }

    [Fact]
    public void BuiltinCollectionFormatters_CountTowardDepth()
    {
        // double[][] nests two ArrayFormatter levels
        var jagged = new double[][] { [1.5], [2.5] };
        var two = MessagePackSerializerOptions.Default with { MaxDepth = 2 };
        var bytes = V4.Serialize(jagged, two);
        Assert.Equal(jagged, V4.Deserialize<double[][]>(bytes, two));

        var one = MessagePackSerializerOptions.Default with { MaxDepth = 1 };
        Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(jagged, one));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<double[][]>(bytes, one));
    }
}

[V3::MessagePack.MessagePackObject]
public class DepthNode
{
    [V3::MessagePack.Key(0)] public DepthNode? Child { get; set; }
}

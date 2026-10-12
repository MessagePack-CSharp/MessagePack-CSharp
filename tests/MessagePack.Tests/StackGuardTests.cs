using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MaxDepth counts levels, but the stack a level costs differs by shape and runtime (about 2.4 KB for object[] on
// .NET Framework x64), so a payload within MaxDepth could overflow a small thread stack: an uncatchable crash.
// SerializeState/DeserializeState.Enter now probe the stack as it grows. These run on a 256 KB thread with MaxDepth
// off, where every shape overflows long before any depth limit: the guard must turn that into the data error.
// A regression here crashes the test host instead of failing a test.
public class StackGuardTests
{
    const int SmallStack = 256 * 1024;
    const int Levels = 100_000;

    static readonly MessagePackSerializerOptions Unlimited = MessagePackSerializerOptions.Default with { MaxDepth = 0 };

    static Exception? RunOnSmallStack(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, SmallStack);
        thread.Start();
        thread.Join();
        return caught;
    }

    static byte[] NestedArrays(int levels)
    {
        var bytes = new byte[levels + 1];
        bytes.AsSpan(0, levels).Fill(0x91); // fixarray(1) per level
        bytes[levels] = 0xC0;
        return bytes;
    }

    static byte[] NestedMaps(int levels)
    {
        // {"a": {"a": ... nil}}
        var bytes = new byte[levels * 3 + 1];
        for (int i = 0; i < levels; i++)
        {
            bytes[i * 3] = 0x81;
            bytes[i * 3 + 1] = 0xA1;
            bytes[i * 3 + 2] = (byte)'a';
        }
        bytes[^1] = 0xC0;
        return bytes;
    }

    [Fact]
    public void Deserialize_ObjectArrays_ThrowsInsteadOfOverflowing()
    {
        var ex = RunOnSmallStack(() => V4.Deserialize<object>(NestedArrays(Levels), Unlimited));
        Assert.IsType<MessagePackSerializationException>(ex);
        Assert.Contains("stack", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_ObjectMaps_ThrowsInsteadOfOverflowing()
    {
        var ex = RunOnSmallStack(() => V4.Deserialize<object>(NestedMaps(Levels), Unlimited));
        Assert.IsType<MessagePackSerializationException>(ex);
    }

    [Fact]
    public void Deserialize_GeneratedType_ThrowsInsteadOfOverflowing()
    {
        var ex = RunOnSmallStack(() => V4.Deserialize<DepthNode>(NestedArrays(Levels), Unlimited));
        Assert.IsType<MessagePackSerializationException>(ex);
    }

    [Fact]
    public void Serialize_DeepGraph_ThrowsInsteadOfOverflowing()
    {
        object?[] root = [null];
        var current = root;
        for (int i = 0; i < Levels; i++)
        {
            object?[] next = [null];
            current[0] = next;
            current = next;
        }
        var ex = RunOnSmallStack(() => V4.Serialize<object>(root, Unlimited));
        Assert.IsType<MessagePackSerializationException>(ex);
    }

    [Fact]
    public void ConvertToJson_HasItsOwnDepthLimit()
    {
        // previously only Utf8JsonWriter's 1000 applied, surfacing as InvalidOperationException
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.ConvertToJson(NestedArrays(1500)));
        Assert.Contains("500", ex.Message, StringComparison.Ordinal);
        Assert.Equal(new string('[', 400) + "null" + new string(']', 400), V4.ConvertToJson(NestedArrays(400)));
    }

    [Fact]
    public void ConvertToJson_SmallStack_ThrowsInsteadOfOverflowing()
    {
        var ex = RunOnSmallStack(() => V4.ConvertToJson(NestedArrays(499)));
        Assert.True(ex is null or MessagePackSerializationException, ex?.ToString());
    }

    [Fact]
    public void WithinTheStack_DeepPayloadStillReads()
    {
        // the probe must not refuse what fits: 500 levels of object[] on a normal thread
        var back = V4.Deserialize<object>(NestedArrays(499));
        Assert.IsType<object?[]>(back);
    }
}

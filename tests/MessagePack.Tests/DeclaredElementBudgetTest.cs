using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using SerializerFoundation;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The declared-element budget in DeserializeState: every array element, map key and map value is a distinct
// msgpack value of at least one byte, so the counts declared by all the array/map headers of a message together
// can never exceed the message length. ReadArrayHeader/ReadMapHeader(ref state) charge each count against the
// budget and reject the first header that breaks the bound, before any formatter preallocates from it.
//
// The per-header guard (count <= BytesRemaining) alone is defeated by nesting: every nested header measures the
// same tail of the message, so 500 headers can each claim the whole tail and each level preallocates for it
// (the "allocate before the depth check" amplification: a ~32 KB payload reaching ~120 MB through object?[]).
public class DeclaredElementBudgetTest
{
    static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Default;

    // The advisory's shape: `layers` nested array32 headers, each claiming exactly the bytes that remain after it
    // (so each one passes the per-header guard on its own), then nil filler. Every level allocates
    // object?[~30000] (240 KB on x64) before descending, 500 levels deep.
    static byte[] NestedArrayBomb(int totalLength = 32_495, int layers = 500)
    {
        var bytes = new byte[totalLength];
        for (int i = 0; i < layers; i++)
        {
            var offset = i * 5;
            bytes[offset] = MessagePackCode.Array32;
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset + 1), (uint)(totalLength - offset - 5));
        }
        bytes.AsSpan(layers * 5).Fill(MessagePackCode.Nil);
        return bytes;
    }

    // map32 variant: each level claims (remaining / 2) entries, the per-header bound for maps
    static byte[] NestedMapBomb(int totalLength = 32_495, int layers = 500)
    {
        var bytes = new byte[totalLength];
        for (int i = 0; i < layers; i++)
        {
            var offset = i * 5;
            bytes[offset] = MessagePackCode.Map32;
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset + 1), (uint)((totalLength - offset - 5) / 2));
        }
        bytes.AsSpan(layers * 5).Fill(MessagePackCode.Nil);
        return bytes;
    }

    // source-generated recursive type: every level is [ children:array32(N) ] and the children are the next
    // level, so the object header (fixarray 1) and the collection header both charge the budget
    static byte[] RecursiveNodeBomb(int totalLength = 32_495, int layers = 200)
    {
        var bytes = new byte[totalLength];
        for (int i = 0; i < layers; i++)
        {
            var offset = i * 6;
            bytes[offset] = MessagePackCode.MinFixArray | 1;
            bytes[offset + 1] = MessagePackCode.Array32;
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset + 2), (uint)(totalLength - offset - 6));
        }
        bytes.AsSpan(layers * 6).Fill(MessagePackCode.Nil);
        return bytes;
    }

    static void AssertRejectedBeforeAllocating(byte[] payload, Func<byte[], object?> deserialize)
    {
        // a nil warms the formatter chain up (resolver caches, JIT), so the measurement below is the payload alone
        Assert.Null(deserialize([MessagePackCode.Nil]));
        AssertRejectedBeforeAllocating(payload.Length, () => deserialize(payload));
    }

    static void AssertRejectedBeforeAllocating(byte[] payload, int chunkSize, Func<ReadOnlySequence<byte>, object?> deserialize)
    {
        Assert.Null(deserialize(new ReadOnlySequence<byte>([MessagePackCode.Nil])));
        var sequence = Split(payload, chunkSize); // the segment chain is built outside the measured window
        AssertRejectedBeforeAllocating(payload.Length, () => deserialize(sequence));
    }

    static void AssertRejectedBeforeAllocating(int payloadLength, Action deserialize)
    {
        // GetAllocatedBytesForCurrentThread is per-thread, so the deserialize runs right here
        var before = GC.GetAllocatedBytesForCurrentThread();
        var ex = Record.Exception(deserialize);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        var mpex = Assert.IsType<MessagePackSerializationException>(ex);
        Assert.Contains("declared", mpex.Message);
        // the first level legitimately allocates for its own claim (object?[] costs 8 bytes per nil element);
        // the amplified path allocates ~3,800x the payload
        Assert.True(allocated < 16L * payloadLength, $"rejected input allocated {allocated:N0} bytes for a {payloadLength:N0} byte payload (allocation-bomb guard breached)");
    }

    [Fact]
    public void AdvisoryPayload_Object_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(NestedArrayBomb(), bytes => V4.Deserialize<object>(bytes, Options));
    }

    [Fact]
    public void AdvisoryPayload_ObjectArray_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(NestedArrayBomb(), bytes => V4.Deserialize<object[]>(bytes, Options));
    }

    [Fact]
    public void AdvisoryPayload_ListOfObject_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(NestedArrayBomb(), bytes => V4.Deserialize<List<object>>(bytes, Options));
    }

    [Fact]
    public void NestedMapBomb_DictionaryOfObject_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(NestedMapBomb(), bytes => V4.Deserialize<Dictionary<object, object>>(bytes, Options));
        AssertRejectedBeforeAllocating(NestedMapBomb(), bytes => V4.Deserialize<object>(bytes, Options));
    }

    [Fact]
    public void RecursiveNodeBomb_SourceGenerated_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(RecursiveNodeBomb(), bytes => V4.Deserialize<BudgetTreeNode>(bytes, Options));
    }

    [Fact]
    public void AdvisoryPayload_MultiSegmentSequence_RejectedBeforeAmplifying()
    {
        AssertRejectedBeforeAllocating(NestedArrayBomb(), 7, sequence => V4.Deserialize<object>(sequence, Options));
        AssertRejectedBeforeAllocating(NestedArrayBomb(), 1, sequence => V4.Deserialize<object>(sequence, Options));
    }

    [Fact]
    public async Task AdvisoryPayload_Async_RejectedBeforeAmplifying()
    {
        var payload = NestedArrayBomb();
        // the async entry buffers the message and hands the exact slice to the sync path, so the budget is exact
        var ex = await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
            await V4.DeserializeAsync<object>(PipeReader.Create(Split(payload, 1000)), Options));
        Assert.Contains("declared", ex.Message);
    }

    [Fact]
    public void Rejection_NamesTheLyingHeader()
    {
        // the first header spends all but 5 bytes of the budget, the second claims the whole tail again
        var payload = NestedArrayBomb();
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<object>(payload, Options));
        Assert.Contains("array header claims 32485 elements", ex.Message);
        Assert.Contains("all but 5", ex.Message);
    }

    // ---------------------------------------------------------------- no false positives: maximal legal fill

    [Fact]
    public void DeepChainOfSingletonArrays_FillsTheBudget_Succeeds()
    {
        // fixarray(1) x 400 then nil: 401 bytes declare 400 elements
        var bytes = new byte[401];
        bytes.AsSpan(0, 400).Fill(MessagePackCode.MinFixArray | 1);
        bytes[400] = MessagePackCode.Nil;

        var value = V4.Deserialize<object>(bytes, Options);
        var depth = 0;
        while (value is object?[] array)
        {
            depth++;
            Assert.Single(array);
            value = array[0];
        }
        Assert.Equal(400, depth);
        Assert.Null(value);
    }

    [Fact]
    public void WideArrayOfNils_FillsTheBudget_Succeeds()
    {
        const int count = 70_000;
        var bytes = new byte[5 + count];
        bytes[0] = MessagePackCode.Array32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), count);
        bytes.AsSpan(5).Fill(MessagePackCode.Nil);

        Assert.Equal(count, V4.Deserialize<object[]>(bytes, Options).Length);
        Assert.Equal(count, V4.Deserialize<List<object>>(bytes, Options).Count);
        Assert.Equal(count, V4.Deserialize<string[]>(bytes, Options).Length);
    }

    [Fact]
    public void NestedArraysOfNils_EveryByteAnElement_Succeeds()
    {
        // [[nil, nil], [nil, nil], [nil, nil]]: 10 bytes declare 9 elements, the tightest legal ratio
        byte[] bytes = [0x93, 0x92, 0xc0, 0xc0, 0x92, 0xc0, 0xc0, 0x92, 0xc0, 0xc0];
        var value = Assert.IsType<object?[]>(V4.Deserialize<object>(bytes, Options));
        Assert.Equal(3, value.Length);
        Assert.All(value, inner => Assert.Equal(2, Assert.IsType<object?[]>(inner).Length));

        var jagged = V4.Deserialize<int?[][]>(bytes, Options);
        Assert.Equal(3, jagged.Length);
        Assert.All(jagged, inner => Assert.Equal([null, null], inner));

        foreach (var chunk in new[] { 1, 2, 3 })
        {
            Assert.Equal(3, Assert.IsType<object?[]>(V4.Deserialize<object>(Split(bytes, chunk), Options)).Length);
        }
    }

    [Fact]
    public void MapOfEmptyArrays_EveryByteAnElement_Succeeds()
    {
        // fixmap(15) of fixint key -> fixarray(0): 31 bytes declare 30 elements
        var bytes = new byte[31];
        bytes[0] = MessagePackCode.MinFixMap | 15;
        for (int i = 0; i < 15; i++)
        {
            bytes[1 + i * 2] = (byte)i;
            bytes[2 + i * 2] = MessagePackCode.MinFixArray;
        }

        var dictionary = V4.Deserialize<Dictionary<int, int[]>>(bytes, Options);
        Assert.Equal(15, dictionary.Count);
        Assert.All(dictionary.Values, v => Assert.Empty(v));

        var objects = V4.Deserialize<Dictionary<object, object>>(bytes, Options);
        Assert.Equal(15, objects.Count);
    }

    [Fact]
    public void RecursiveNode_LegalTree_Roundtrips()
    {
        var root = new BudgetTreeNode
        {
            Children =
            [
                new BudgetTreeNode { Children = [new BudgetTreeNode(), new BudgetTreeNode { Children = [] }] },
                new BudgetTreeNode(),
                null,
            ],
        };
        var bytes = V4.Serialize(root, Options);
        var back = V4.Deserialize<BudgetTreeNode>(bytes, Options);
        Assert.Equal(3, back.Children!.Length);
        Assert.Equal(2, back.Children[0]!.Children!.Length);
        Assert.Null(back.Children[2]);
    }

    // ---------------------------------------------------------------- the reader surface itself

    [Fact]
    public void ReadHeaderWithState_ChargesAndRejectsAtTheBound()
    {
        // budget 3: array(2) charges 2, map(1) charges 2 -> over
        // (each header passes its own per-header guard: the map has two bytes behind it)
        var ex = Assert.Throws<MessagePackSerializationException>(() => ReadBoth([0x92, 0x81, 0xc0, 0xc0]));
        Assert.Contains("map header claims 1 elements", ex.Message);
        Assert.Contains("all but 1", ex.Message);

        // the ref-struct buffer cannot cross into the lambda, so the whole read runs inside the helper
        static void ReadBoth(byte[] bytes)
        {
            var buffer = new ReadOnlySpanReadBuffer(bytes);
            var state = new DeserializeState(maxDepth: 0, messageLength: 3);
            Assert.Equal(2, buffer.ReadArrayHeader(ref state));
            buffer.ReadMapHeader(ref state);
        }
    }

    [Fact]
    public void ReadHeaderWithState_ExactFill_Succeeds()
    {
        // budget 3: array(1) + map(1) + array(0) = 3
        byte[] bytes = [0x91, 0x81, 0x90, 0xc0];
        var buffer = new ReadOnlySpanReadBuffer(bytes);
        var state = new DeserializeState(maxDepth: 0, messageLength: 3);
        Assert.Equal(1, buffer.ReadArrayHeader(ref state));
        Assert.Equal(1, buffer.ReadMapHeader(ref state));
        Assert.Equal(0, buffer.ReadArrayHeader(ref state));
    }

    [Fact]
    public void StateWithoutMessageLength_IsUnlimited()
    {
        byte[] bytes = [0x92, 0x81, 0x90, 0xc0];
        var buffer = new ReadOnlySpanReadBuffer(bytes);
        var state = new DeserializeState(maxDepth: 0);
        Assert.Equal(2, buffer.ReadArrayHeader(ref state));
        Assert.Equal(1, buffer.ReadMapHeader(ref state));
        Assert.Equal(0, buffer.ReadArrayHeader(ref state));
    }

    [Fact]
    public void StatelessReader_StillOnlyChecksTheLocalBound()
    {
        // Skip and scanners read headers without a state: the per-header guard alone, no budget
        byte[] bytes = [0x92, 0x92, 0xc0, 0xc0];
        var buffer = new ReadOnlySpanReadBuffer(bytes);
        Assert.Equal(2, buffer.ReadArrayHeader());
        Assert.Equal(2, buffer.ReadArrayHeader());
    }

    // ---------------------------------------------------------------- helpers

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<byte> Split(byte[] data, int chunkSize)
    {
        if (data.Length <= chunkSize)
        {
            return new ReadOnlySequence<byte>(data);
        }
        var first = new Segment(data.AsMemory(0, chunkSize));
        var last = first;
        for (int i = chunkSize; i < data.Length; i += chunkSize)
        {
            last = last.Append(data.AsMemory(i, Math.Min(chunkSize, data.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}

[MessagePackObject]
public class BudgetTreeNode
{
    [Key(0)] public BudgetTreeNode?[]? Children { get; set; }
}

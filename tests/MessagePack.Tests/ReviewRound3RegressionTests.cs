using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// a [MessagePackObject] nested in a generic type is refused by the parser (MsgPack005, the reflection tier serves it);
// a DTO holding its closed form must not take the harvest down with it (`new string(',', -1)` at every DTO's expense)
public class HarvestOuter<T>
{
#pragma warning disable MsgPack005
    [MessagePackObject]
    public class Inner
    {
        [Key(0)] public int X { get; set; }

        [Key(1)] public T? Value { get; set; }
    }
#pragma warning restore MsgPack005
}

[MessagePackObject]
public class HarvestNestedHolder
{
    [Key(0)] public HarvestOuter<int>.Inner? Inner { get; set; }

    [Key(1)] public int Other { get; set; }
}

// `required` members the formatter never reads, whatever excluded them from the model
[MessagePackObject]
public class NonSerializedRequiredMembers
{
    [Key(0)] public int Id;

    [NonSerialized] public required int Cache;

    public required int Hidden { private get; set; }

    public int RevealHidden() => Hidden;
}

// a constructor with a by-reference parameter can never be called with the generated locals
[MessagePackObject]
public class RefParameterConstructor
{
    public RefParameterConstructor()
    {
    }

    public RefParameterConstructor(ref int id)
    {
        Id = id;
    }

    [Key(0)] public int Id { get; set; }
}

// a shadowed member's generated identifier (X_<declarer>) spelled by another member's name
public class CollideBase
{
    [Key(0)] public int X { get; set; }
}

[MessagePackObject]
public class CollideDerived : CollideBase
{
    [Key(1)] public new int X { get; set; }

    [Key(2)] public int X_MessagePack_Tests_CollideBase { get; set; }
}

public class ReviewRound3RegressionTests
{
    [Fact]
    public void NestedInGenericMember_DoesNotBreakTheHarvest()
    {
        // the holder's generated formatter exists (the compile proves the harvest survived); the nested type itself is
        // served by the reflection tier on the Default chain
        var back = V4.Deserialize<HarvestNestedHolder>(V4.Serialize(new HarvestNestedHolder { Inner = new() { X = 1, Value = 2 }, Other = 3 }))!;
        Assert.Equal(1, back.Inner!.X);
        Assert.Equal(2, back.Inner.Value);
        Assert.Equal(3, back.Other);
    }

    [Fact]
    public void RequiredMembersOutsideTheModel_AreDefaulted()
    {
        var back = V4.Deserialize<NonSerializedRequiredMembers>(V4.Serialize(new NonSerializedRequiredMembers { Id = 4, Cache = 9, Hidden = 8 }))!;
        Assert.Equal(4, back.Id);
        Assert.Equal(0, back.Cache);
        Assert.Equal(0, back.RevealHidden());
    }

    [Fact]
    public void RefParameterConstructor_IsNotTheSerializationConstructor()
    {
        var back = V4.Deserialize<RefParameterConstructor>(V4.Serialize(new RefParameterConstructor { Id = 5 }))!;
        Assert.Equal(5, back.Id);
    }

    [Fact]
    public void ShadowedMemberIdentifier_DoesNotCollideWithAnotherMember()
    {
        var value = new CollideDerived { X = 1, X_MessagePack_Tests_CollideBase = 2 };
        ((CollideBase)value).X = 3;
        var back = V4.Deserialize<CollideDerived>(V4.Serialize(value))!;
        Assert.Equal(1, back.X);
        Assert.Equal(2, back.X_MessagePack_Tests_CollideBase);
        Assert.Equal(3, ((CollideBase)back).X);
    }

    // [2147483591, 0, []]: reads instantly, and serializing it back must not walk 2^31 empty rows
    [Fact]
    public void EmptyMultiDimensionalArray_WithHugeDimension_SerializesInstantly()
    {
        byte[] payload = [0x93, 0xCE, 0x7F, 0xFF, 0xFF, 0xC7, 0x00, 0x90];
        var array = V4.Deserialize<int[,]>(payload)!;
        Assert.Equal(2147483591, array.GetLength(0));
        Assert.Equal(0, array.Length);
        var started = Environment.TickCount64;
        Assert.Equal(payload, V4.Serialize(array));
        Assert.True(Environment.TickCount64 - started < 2000, "serializing an empty array walked its dimensions");

        var cube = new int[3, 0, 5];
        Assert.Equal(0, V4.Deserialize<int[,,]>(V4.Serialize(cube))!.Length);
        var hyper = new int[0, 7, 1, 2];
        Assert.Equal(7, V4.Deserialize<int[,,,]>(V4.Serialize(hyper))!.GetLength(1));
    }

    // int[50000, 50000, 0] exists (the CLR's limit is the running product fitting uint32, see DimensionProduct): a
    // zero dimension must not be rejected because an intermediate product passed int.MaxValue
    [Fact]
    public void EmptyMultiDimensionalArray_WithOverflowingIntermediateProduct_IsAccepted()
    {
        byte[] payload = [0x94, 0xCD, 0xC3, 0x50, 0xCD, 0xC3, 0x50, 0x00, 0x90]; // [50000, 50000, 0, []]
        var array = V4.Deserialize<int[,,]>(payload)!;
        Assert.Equal(50000, array.GetLength(0));
        Assert.Equal(0, array.GetLength(2));
        Assert.Equal(payload, V4.Serialize(array));
        var reused = array;
        V4.Deserialize(payload, ref reused);
        Assert.Same(array, reused); // populate reuses the matching (empty) dimensions
        Assert.Equal(0, V4.Deserialize<int[,,]>([0x94, 0x00, 0xCE, 0x00, 0x01, 0x11, 0x70, 0xCE, 0x00, 0x01, 0x11, 0x70, 0x90])!.Length); // [0, 70000, 70000]: the zero comes first

        // past the CLR's own range: [70000, 70000, 0] and [2147483647, 0] cannot be constructed, so they are format errors
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[,,]>([0x94, 0xCE, 0x00, 0x01, 0x11, 0x70, 0xCE, 0x00, 0x01, 0x11, 0x70, 0x00, 0x90]));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[,]>([0x93, 0xCE, 0x7F, 0xFF, 0xFF, 0xFF, 0x00, 0x90]));

        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[,,]>([0x94, 0x02, 0x02, 0x00, 0x91, 0x01])); // 2x2x0 with one element
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[,]>([0x93, 0xCD, 0xC3, 0x50, 0xCD, 0xC3, 0x50, 0x90])); // 50000x50000 with no elements
    }

    // the reader goes away while the source is suspended on something only its consumer would have unblocked: the
    // flush issued around that suspension sees the completed reader, and the serializer must then stop the source
    // (through its enumeration token) instead of waiting on it forever
    [Fact]
    public async Task SerializeMessagesAsync_CompletedReader_StopsASuspendedSource()
    {
        var gate = new TaskCompletionSource();
        var sawCancellation = false;

        async IAsyncEnumerable<int> Source(PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return 1;
            reader.Complete(); // the consumer leaves exactly as the source is about to suspend
            try
            {
                await gate.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                sawCancellation = true;
                throw;
            }
            yield return 2;
        }

        var messages = new Pipe();
        await V4.SerializeMessagesAsync(messages.Writer, Source(messages.Reader)).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(sawCancellation);

        var elements = new Pipe();
        sawCancellation = false;
        await V4.SerializeElementsAsync(elements.Writer, Source(elements.Reader), 2).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(sawCancellation);

        // a reader that completes with an exception makes the flush throw instead: the source is stopped the same way,
        // and the flush's exception is the one reported
        async IAsyncEnumerable<int> FaultingSource(PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return 1;
            reader.Complete(new IOException("consumer failed"));
            try
            {
                await gate.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                sawCancellation = true;
                throw;
            }
            yield return 2;
        }

        var faulted = new Pipe();
        sawCancellation = false;
        await Assert.ThrowsAsync<IOException>(() => V4.SerializeMessagesAsync(faulted.Writer, FaultingSource(faulted.Reader)).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(sawCancellation);
    }
}

using System.Buffers.Binary;
using System.Collections.Immutable;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The header guards bound a collection's declared COUNT by the bytes of the message (one byte per element, plus the
// message-wide declared-element budget), but a formatter allocates count * sizeof(T) up front, so a header could still
// claim sizeof(T) times the payload: 1 MB declaring a million 64-byte structs allocated 61 MB before the first element
// was read, and the 64 MB MaxBufferedMessageSize default let a peer force ~4 GB per message. PresizeCapacity caps the
// up-front allocation at 8x the unread bytes; past that the collection grows while reading, so the allocation follows
// the bytes actually decoded. Data whose in-memory size is at most 8x its wire size never leaves the one-allocation
// path; near-all-nil arrays of large nullable structs do, and must still read back exactly.
public class PresizeCapacityTests
{
    static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Default;

    // 64 bytes in memory, at least 9 bytes on the wire (fixarray 8 + eight fixints): a header can lie by 64x
    [MessagePackObject]
    public struct Wide64
    {
        [Key(0)] public long A;
        [Key(1)] public long B;
        [Key(2)] public long C;
        [Key(3)] public long D;
        [Key(4)] public long E;
        [Key(5)] public long F;
        [Key(6)] public long G;
        [Key(7)] public long H;
    }

    // array32 claiming one element per remaining byte, then zero filler: every claim passes the byte guards,
    // and the first element (a fixint where a fixarray is expected) is rejected
    static byte[] ArrayBomb(int payloadLength = 1_000_005)
    {
        var bytes = new byte[payloadLength];
        bytes[0] = MessagePackCode.Array32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), (uint)(payloadLength - 5));
        return bytes;
    }

    // map32 claiming one entry per two remaining bytes
    static byte[] MapBomb(int payloadLength = 2_000_005)
    {
        var bytes = new byte[payloadLength];
        bytes[0] = MessagePackCode.Map32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), (uint)((payloadLength - 5) / 2));
        return bytes;
    }

    static void AssertAllocationBounded(byte[] payload, Func<byte[], object?> deserialize)
    {
        // a nil warms the formatter chain up (resolver caches, JIT), so the measurement below is the payload alone
        Assert.Null(deserialize([MessagePackCode.Nil]));

        var before = GC.GetAllocatedBytesForCurrentThread();
        var ex = Record.Exception(() => deserialize(payload));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsType<MessagePackSerializationException>(ex);
        // 8x the bytes is the cap; a little slack for the exception and the growing array's first step
        Assert.True(allocated < 10L * payload.Length, $"rejected input allocated {allocated:N0} bytes for a {payload.Length:N0} byte payload (sizeof amplification not capped)");
    }

    [Fact]
    public void WideStructArray_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(ArrayBomb(), bytes => V4.Deserialize<Wide64[]>(bytes, Options));
    }

    // served by GenericEnumerableFormatter (no default constructor, a constructor accepting IEnumerable<T>)
    public sealed class Wide64Bag : IEnumerable<Wide64>
    {
        readonly List<Wide64> items;

        public Wide64Bag(IEnumerable<Wide64> items) => this.items = [.. items];

        public IEnumerator<Wide64> GetEnumerator() => items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void WideStructEnumerableConstructedCollection_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(ArrayBomb(), bytes => V4.Deserialize<Wide64Bag>(bytes, Options));

        var back = V4.Deserialize<Wide64Bag>(V4.Serialize(new Wide64Bag([new Wide64 { A = 1, H = 8 }, new Wide64 { B = 2 }]), Options), Options)!;
        Assert.Equal([1L, 0L], back.Select(static w => w.A));
        Assert.Equal([8L, 0L], back.Select(static w => w.H));
        Assert.Equal([0L, 2L], back.Select(static w => w.B));
    }

    // [[]]: one element (an empty array) with one byte left after the header, so the 8x cap (8 bytes) is below the
    // 64-byte element and the List takes the growing path. Populate semantics must not depend on the path: the
    // existing element is read into in place (the empty array touches no field), the excess is truncated, exactly as
    // the one-allocation SetCount/AsSpan path does with the same payload padded past the cap
    [Fact]
    public void WideStructList_GrowingPath_KeepsPopulateSemantics()
    {
        byte[] payload = [0x91, 0x90];

        var list = new List<Wide64> { new() { A = 42, H = 7 }, new() { A = 99 } };
        V4.Deserialize(payload, ref list, Options);
        Assert.Single(list);
        Assert.Equal(42, list[0].A);
        Assert.Equal(7, list[0].H);

        List<Wide64>? fresh = null;
        V4.Deserialize(payload, ref fresh, Options);
        Assert.Equal(0, Assert.Single(fresh!).A);

        // the 2D array: a reused array of matching dimensions is never on the growing path (nothing is allocated)
        byte[] payload2d = [0x93, 0x01, 0x01, 0x91, 0x90];
        var grid = new Wide64[1, 1];
        grid[0, 0] = new Wide64 { A = 42 };
        V4.Deserialize(payload2d, ref grid, Options);
        Assert.Equal(42, grid[0, 0].A);
        Wide64[,]? freshGrid = null;
        V4.Deserialize(payload2d, ref freshGrid, Options);
        Assert.Equal(0, freshGrid![0, 0].A);
    }

    [MessagePackObject]
    public struct Pair
    {
        [Key(0)] public int A;
        [Key(1)] public int B;
    }

    // a reused List<struct> that was cleared: SetCount would re-expose the removed elements' values, and a shorter
    // payload ([5] without B) would then keep the removed element's B instead of the default
    [Fact]
    public void StructList_ReusedAfterClear_DoesNotResurrectRemovedElements()
    {
        var list = new List<Pair> { new() { A = 1, B = 9 }, new() { A = 2, B = 8 } };
        list.Clear();
        V4.Deserialize([0x92, 0x91, 0x05, 0x91, 0x06], ref list, Options); // [[5], [6]]: the one-allocation path (16 bytes of elements for 4 of payload)
        Assert.Equal(2, list.Count);
        Assert.Equal((5, 0), (list[0].A, list[0].B));
        Assert.Equal((6, 0), (list[1].A, list[1].B));

        // while elements still present are populated in place
        var kept = new List<Pair> { new() { A = 1, B = 9 } };
        V4.Deserialize([0x91, 0x91, 0x05], ref kept, Options);
        Assert.Equal((5, 9), (kept[0].A, kept[0].B));
    }

    [Fact]
    public void WideStructList_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(ArrayBomb(), bytes => V4.Deserialize<List<Wide64>>(bytes, Options));
    }

    [Fact]
    public void WideStructImmutableArray_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(ArrayBomb(), bytes => V4.Deserialize<ImmutableArray<Wide64>?>(bytes, Options));
    }

    [Fact]
    public void WideStructStack_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(ArrayBomb(), bytes => V4.Deserialize<Stack<Wide64>>(bytes, Options));
    }

    [Fact]
    public void WideStructDictionary_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        AssertAllocationBounded(MapBomb(), bytes => V4.Deserialize<Dictionary<long, Wide64>>(bytes, Options));
    }

    [Fact]
    public void WideStruct2DArray_LyingHeader_AllocatesAtMostEightTimesThePayload()
    {
        // [len0, len1, array32(count)] with len0 * len1 == count, the T[,] wire shape
        const int count = 1_000_000;
        var bytes = new byte[1 + 5 + 5 + 5 + count];
        bytes[0] = MessagePackCode.MinFixArray | 3;
        bytes[1] = MessagePackCode.UInt32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(2), 1000);
        bytes[6] = MessagePackCode.UInt32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(7), 1000);
        bytes[11] = MessagePackCode.Array32;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12), count);
        AssertAllocationBounded(bytes, b => V4.Deserialize<Wide64[,]>(b, Options));
    }

    // ---- the growing path on honest data: all-nil long? costs 1 byte on the wire and 16 in memory, past the 8x cap ----

    static long?[] AllNil(int count) => new long?[count];

    static void AssertRoundtrip<T>(T value, Func<T, long?[]> flatten)
    {
        var bytes = V4.Serialize(value, Options);
        var back = V4.Deserialize<T>(bytes, Options);
        Assert.Equal(flatten(value), flatten(back!));
    }

    [Fact]
    public void AllNilNullableArray_ReadsBackExactly()
    {
        AssertRoundtrip(AllNil(100_000), v => v);
        // a few non-nil values at the end: the growing array must end at exactly count
        var mixed = AllNil(70_001);
        mixed[^1] = 5;
        mixed[12345] = -1;
        AssertRoundtrip(mixed, v => v);
    }

    [Fact]
    public void AllNilNullableCollections_ReadBackExactly()
    {
        var source = AllNil(50_000);
        source[7] = 7;
        AssertRoundtrip(new List<long?>(source), v => v.ToArray());
        AssertRoundtrip(ImmutableArray.Create(source), v => v.ToArray());
        AssertRoundtrip(new Stack<long?>(source), v => v.ToArray()); // Stack enumerates top-first, both sides alike
        AssertRoundtrip(new ReadOnlyMemory<long?>(source), v => v.ToArray());
        AssertRoundtrip(new ArraySegment<long?>(source), v => v.ToArray());
        AssertRoundtrip(new Memory<long?>(source), v => v.ToArray());
        AssertRoundtrip(new ReadOnlyMemory<long?>(source), v => v.ToArray());
    }

    [Fact]
    public void AllNilNullable2DArray_ReadsBackExactly()
    {
        var source = new long?[300, 200];
        source[299, 199] = 42;
        var back = V4.Deserialize<long?[,]>(V4.Serialize(source, Options), Options)!;
        Assert.Equal(300, back.GetLength(0));
        Assert.Equal(200, back.GetLength(1));
        Assert.Equal(42, back[299, 199]);
        Assert.Null(back[0, 0]);
    }

    [Fact]
    public void PresizeCapacity_KeepsHonestCountsAndCapsLies()
    {
        // 8 bytes per unread byte: a long behind a fixint, or a reference behind a nil, stays at count
        Assert.Equal(1000, ReadBufferExtensions.PresizeCapacity<long>(1000, 1000));
        Assert.Equal(1000, ReadBufferExtensions.PresizeCapacity<object>(1000, 1000));
        // 16 bytes per unread byte is past the cap: half the count, grown from there
        Assert.Equal(500, ReadBufferExtensions.PresizeCapacity<long?>(1000, 1000));
        // honest 16-byte structs carry at least 2 wire bytes each (bin8 Guid: 18), so they stay at count
        Assert.Equal(1000, ReadBufferExtensions.PresizeCapacity<Guid>(1000, 18_000));
        Assert.Equal(125, ReadBufferExtensions.PresizeCapacity<Wide64>(1000, 1000));
        Assert.Equal(0, ReadBufferExtensions.PresizeCapacity<Wide64>(0, 0));
    }
}

using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.IO.Pipelines;
using MessagePack;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// Regression tests for the line-by-line audit before 4.0.0-rc.1 (2026-11-10). Every case was reproduced against the
// library before its fix; the numbers quoted are the ones observed then. The frame walkers' part is in Lz4FrameTests /
// ZstandardFrameTests (BoundaryWalk_ResumesInsideTheSkippablePrefix), the frozen collections' in
// FrozenCollectionFormatterTests.
public class Rc1AuditRegressionTests
{
    // ---- A3: SortedList outside the exact SortedList<K,V> formatter ----
    // SortedList inserts by binary search plus a shift of everything behind the slot. The exact-type formatter already
    // read, sorted and appended; a non-generic SortedList, a class derived from SortedList<K,V> and an IDictionary
    // member holding one added in payload order: 200,000 descending keys (1.2 MB) took 5.1 s, 3.1 s and 3.4 s.

    static byte[] DescendingIntMap(int count)
    {
        var bytes = new byte[5 + count * 6];
        bytes[0] = 0xdf;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), (uint)count);
        for (int i = 0, key = count; i < count; i++, key--)
        {
            var offset = 5 + i * 6;
            bytes[offset] = 0xd2;
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(offset + 1), key);
            bytes[offset + 5] = 0xc0;
        }
        return bytes;
    }

    [Fact]
    public void SortedListFamily_DescendingKeys_AreNotQuadratic()
    {
        var bytes = DescendingIntMap(200_000);
        var watch = Stopwatch.StartNew();

        var nonGeneric = V4.Deserialize<SortedList>(bytes)!;
        var derived = V4.Deserialize<AuditSortedList>(bytes)!;
        IDictionary<int, object?> populated = new SortedList<int, object?>();
        V4.Deserialize(bytes, ref populated!);
        IDictionary populatedNonGeneric = new SortedList();
        V4.Deserialize(bytes, ref populatedNonGeneric!);

        // generous: the four reads took about 0.2 s after the fix and more than 14 s before
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"four SortedList reads of 200,000 descending keys took {watch.Elapsed}");
        Assert.Equal(200_000, nonGeneric.Count);
        Assert.Equal(1, nonGeneric.GetKey(0));
        Assert.Equal(200_000, nonGeneric.GetKey(199_999));
        Assert.Equal(200_000, derived.Count);
        Assert.Equal(1, derived.Keys[0]);
        Assert.IsType<SortedList<int, object?>>(populated);
        Assert.Equal(200_000, populated.Count);
        Assert.IsType<SortedList>(populatedNonGeneric);
        Assert.Equal(200_000, populatedNonGeneric.Count);
    }

    [Fact]
    public void SortedListFamily_KeepsItsRulesOnTheSortedPath()
    {
        // {3: nil, 1: nil, 3: nil}: a duplicate key is still refused by every one of them
        byte[] duplicate = [0x83, 0x03, 0xc0, 0x01, 0xc0, 0x03, 0xc0];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<SortedList>(duplicate));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<AuditSortedList>(duplicate));
        IDictionary<int, object?> populated = new SortedList<int, object?>();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize(duplicate, ref populated!));

        // an instance on a comparer of its own keeps it, generic and non-generic, ascending and descending input
        byte[] ascending = [0x84, 0x01, 0xc0, 0x02, 0xc0, 0x03, 0xc0, 0x04, 0xc0];
        byte[] descending = [0x84, 0x04, 0xc0, 0x03, 0xc0, 0x02, 0xc0, 0x01, 0xc0];
        foreach (var payload in new[] { ascending, descending })
        {
            IDictionary<int, object?> reversed = new SortedList<int, object?>(Comparer<int>.Create(static (a, b) => b.CompareTo(a)));
            V4.Deserialize(payload, ref reversed!);
            Assert.Equal([4, 3, 2, 1], reversed.Keys);

            IDictionary reversedNonGeneric = new SortedList(new ReverseBoxedComparer());
            V4.Deserialize(payload, ref reversedNonGeneric!);
            Assert.Equal(new object[] { 4, 3, 2, 1 }, ((SortedList)reversedNonGeneric).GetKeyList().Cast<object>().Select(static k => (object)Convert.ToInt32(k)));
        }

        // small maps (the sort is skipped below three entries) and the empty one
        Assert.Empty(V4.Deserialize<SortedList>([0x80])!);
        Assert.Single(V4.Deserialize<SortedList>([0x81, 0x01, 0xc0])!);
        Assert.Equal(2, V4.Deserialize<SortedList>([0x82, 0x02, 0xc0, 0x01, 0xc0])!.Count);
    }

    sealed class ReverseBoxedComparer : IComparer
    {
        public int Compare(object? x, object? y) => Comparer.Default.Compare(y, x);
    }

    // ---- A4: the message stream's scan path sliced every message from the buffer start ----
    // ReadOnlySequence.Slice(long, long) walks the segment list from the first segment: messages x segments. 4 MiB of
    // one-byte messages in 4 KB segments took 2.4 s (1 MiB: 0.27 s, 2 MiB: 0.63 s); now each is sliced off what is left.

    [Fact]
    public async Task MessageStream_ManyMessagesInManySegments_IsNotQuadratic()
    {
        var bytes = new byte[8 << 20];
        bytes.AsSpan().Fill(0xc0);
        // a cap below the buffer length keeps the completed-reader fast path out, as a processor would
        var options = V4Options.Default with { MaxBufferedMessageSize = 1024 };

        var watch = Stopwatch.StartNew();
        long count = 0;
        await foreach (var _ in V4.DeserializeMessagesAsync<object?>(PipeReader.Create(Segmented(bytes, 4096)), options, TestContext.Current.CancellationToken))
        {
            count++;
        }
        // about 0.4 s after the fix, about 10 s before
        Assert.Equal(bytes.Length, count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"{count:N0} messages took {watch.Elapsed}");
    }

    [Fact]
    public async Task ElementStream_ManyElementsInManySegments_ReadsEveryElementAndLeavesTheRest()
    {
        // array32 of 100,000 small integers, then two trailing bytes that are not part of the array
        var bytes = new byte[5 + 100_000 + 2];
        bytes[0] = 0xdd;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), 100_000);
        for (int i = 0; i < 100_000; i++)
        {
            bytes[5 + i] = (byte)(i % 100);
        }
        bytes[^2] = 0x07;
        bytes[^1] = 0x08;
        var options = V4Options.Default with { MaxBufferedMessageSize = 1024 };
        var reader = PipeReader.Create(Segmented(bytes, 100));

        long sum = 0;
        var n = 0;
        await foreach (var element in V4.DeserializeElementsAsync<int>(reader, options, TestContext.Current.CancellationToken))
        {
            sum += element;
            n++;
        }
        Assert.Equal(100_000, n);
        Assert.Equal(1000L * (99 * 100 / 2), sum);
        Assert.Equal(7, await V4.DeserializeAsync<int>(reader, options, TestContext.Current.CancellationToken));
    }

    static ReadOnlySequence<byte> Segmented(byte[] bytes, int size)
    {
        var first = new Segment(bytes.AsMemory(0, Math.Min(size, bytes.Length)), 0);
        var last = first;
        for (var offset = size; offset < bytes.Length; offset += size)
        {
            last = last.Append(bytes.AsMemory(offset, Math.Min(size, bytes.Length - offset)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }

    // ---- A5: LZ4 block array (ext 98) with tiny blocks ----
    // every block cost a rented array (16 bytes at least), a segment object and a slot in the owner array: an envelope
    // of one-byte blocks held 19 times its own length (968 KB for 50 KB). Blocks whose declared sizes average under
    // 1 KB are now decoded side by side into one buffer.

    static byte[] OneByteBlocks(int blockCount)
    {
        // [array n+1][ext 98: n sizes of 1][n x bin8: an LZ4 block of one literal 'A']
        var bytes = new byte[5 + 6 + blockCount + blockCount * 4];
        var offset = 0;
        bytes[offset++] = 0xdd;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), (uint)(blockCount + 1));
        offset += 4;
        bytes[offset++] = 0xc9;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), (uint)blockCount);
        offset += 4;
        bytes[offset++] = 98;
        bytes.AsSpan(offset, blockCount).Fill(0x01);
        offset += blockCount;
        for (int i = 0; i < blockCount; i++)
        {
            bytes[offset++] = 0xc4;
            bytes[offset++] = 0x02;
            bytes[offset++] = 0x10;
            bytes[offset++] = 0x41;
        }
        return bytes;
    }

    [Fact]
    public void Lz4BlockArray_TinyBlocks_DoNotAmplify()
    {
        var options = V4Options.Default.WithLz4BlockArray();
        var bytes = OneByteBlocks(10_000);
        Assert.Equal(0x41, V4.Deserialize<int>(OneByteBlocks(3), options)); // warm; the message is 'A' (fixint 65), the rest trails

        var before = GC.GetAllocatedBytesForCurrentThread();
        var value = V4.Deserialize<int>(bytes, options);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0x41, value);
        Assert.True(allocated < 2L * bytes.Length, $"a {bytes.Length:N0}-byte envelope allocated {allocated:N0} bytes");

        // the decoded bytes are the blocks in order: 300 one-byte blocks spelling str8 + its 298 characters
        var text = new string('x', 298);
        var message = V4.Serialize(text); // d9 + length byte + 298 chars... as one-byte blocks
        var envelope = new List<byte> { 0xdd };
        envelope.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)(message.Length + 1))));
        envelope.Add(0xc9);
        envelope.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)message.Length)));
        envelope.Add(98);
        envelope.AddRange(Enumerable.Repeat((byte)0x01, message.Length));
        foreach (var b in message)
        {
            envelope.AddRange([0xc4, 0x02, 0x10, b]);
        }
        Assert.Equal(text, V4.Deserialize<string>(envelope.ToArray(), options));

        // a corrupt block inside the one-buffer path is the same domain exception as before
        var corrupt = OneByteBlocks(10);
        corrupt[^2] = 0xF0; // a literal run that claims more than the block holds
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int>(corrupt, options));
    }

    // ---- A7: a processor's lower bound read off the wire ----
    // start + length wrapped negative for a length near long.MaxValue on the second or later message of a batch, so
    // the size cap compared a negative number and let the message buffer without limit

    [Fact]
    public void BoundaryFinder_HugeLowerBound_SaturatesInsteadOfWrapping()
    {
        var finder = new MessageBoundaryFinder(new HugeLengthProcessor());
        var buffer = new ReadOnlySequence<byte>(new byte[16]);
        Assert.True(finder.TryFindEnd(buffer)); // first message: 8 bytes
        Assert.Equal(8, finder.Consumed);
        finder.StartNextValue();
        Assert.False(finder.TryFindEnd(buffer)); // second: "needs long.MaxValue - 1 bytes"
        Assert.True(finder.MinimumMessageSize - 8 > 64L << 20, $"lower bound {finder.MinimumMessageSize}");
    }

    sealed class HugeLengthProcessor : MessagePackMessageProcessor
    {
        int calls;

        public override bool DefinesMessageBoundaries => true;

        public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output) => false;

        public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output) => false;

        public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
        {
            message = default;
            return false;
        }

        public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
        {
            message = default;
            return false;
        }

        public override bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, out long length)
        {
            if (calls++ == 0)
            {
                length = 8;
                return true;
            }
            length = long.MaxValue - 1;
            return false;
        }
    }
}

public class AuditSortedList : SortedList<int, object?>
{
}

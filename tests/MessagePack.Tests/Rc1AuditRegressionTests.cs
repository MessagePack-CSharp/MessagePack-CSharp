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
    // ---- B: only the serializer's own exception for data a target refuses ----
    // the stated contract: a payload fails as MessagePackSerializationException (or InvalidOperationException /
    // EndOfStreamException), and a user constructor's exception surfaces as itself, not wrapped by reflection

    static readonly V4Options Contractless = new(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

    [Fact]
    public void CollectionsThatRefuseAValue_ThrowTheSerializationException()
    {
        // a KeyedCollection-derived type with a duplicate key (ArgumentException from Add)
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<AuditItemsById>([0x92, 0x91, 0x01, 0x91, 0x01]));
        Assert.Equal(2, V4.Deserialize<AuditItemsById>([0x92, 0x91, 0x01, 0x91, 0x02])!.Count);

        // StringCollection handed a boxed byte (InvalidCastException)
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<System.Collections.Specialized.StringCollection>([0x91, 0x01]));
        Assert.Equal("a", V4.Deserialize<System.Collections.Specialized.StringCollection>([0x91, 0xa1, 0x61])![0]);

        // a caller's sorted set that cannot order what the payload holds (ArgumentException from the comparer)
        ISet<object> sortedSet = new SortedSet<object>();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x92, 0x01, 0xa1, 0x61], ref sortedSet!));

        // a caller's Dictionary behind ICollection<KeyValuePair>, with a duplicate key
        ICollection<KeyValuePair<string, int>> pairs = new Dictionary<string, int>();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x92, 0x92, 0xa1, 0x61, 0x01, 0x92, 0xa1, 0x61, 0x02], ref pairs!));

        // a caller's sorted dictionary behind IDictionary<object, object>
        IDictionary<object, object?> sortedDictionary = new SortedDictionary<object, object?>();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x82, 0x01, 0xc0, 0xa1, 0x61, 0xc0], ref sortedDictionary!));

        // a typed list or dictionary behind the non-generic view takes only its own element type: the object formatter
        // hands a small integer over as a byte
        IList typedList = new List<int>();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x91, 0x01], ref typedList!));
        IDictionary typedDictionary = new Dictionary<string, int>();
        var refused = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x81, 0xa1, 0x6b, 0x02], ref typedDictionary!));
        Assert.DoesNotContain("nil or duplicate", refused.Message); // it is neither
        IDictionary duplicated = new Hashtable();
        Assert.Contains("nil or duplicate", Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize([0x82, 0x01, 0xc0, 0x01, 0xc0], ref duplicated!)).Message);
    }

    [Fact]
    public void ConstructorsThatThrow_SurfaceTheirOwnException()
    {
        Assert.Equal("list says no", Assert.Throws<InvalidOperationException>(() => V4.Deserialize<AuditThrowingList>([0x91, 0x01])).Message);
        Assert.Equal("type says no", Assert.Throws<InvalidOperationException>(() => V4.Deserialize<AuditThrowingCtor>([0x81, 0xa1, 0x41, 0x01], Contractless)).Message);
    }

    [Fact]
    public void Tuple8_RestThatIsNotATuple_ThrowsTheSerializationException()
    {
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Tuple<int, int, int, int, int, int, int, int>>([0x98, 1, 2, 3, 4, 5, 6, 7, 8]));
        Assert.Contains("Rest", exception.Message);
    }

    [Fact]
    public void MultiDimensionalArray_CovariantInitializer_IsReplaced()
    {
        // object[,] = new string[1, 2]: a ref into it for T = object is ArrayTypeMismatchException, and only when the
        // payload's dimensions equal the initializer's (another shape already took the fresh-array path)
        var board = V4.Deserialize<AuditBoard>([0x91, 0x93, 0x01, 0x02, 0x92, 0x01, 0x02])!;
        Assert.IsType<object[,]>(board.Cells);
        Assert.Equal(2, Convert.ToInt32(board.Cells[0, 1]));

        object[,,]? three = new string[1, 1, 1];
        V4.Deserialize([0x94, 0x01, 0x01, 0x01, 0x91, 0x07], ref three);
        Assert.IsType<object[,,]>(three);

        object[,,,]? four = new string[1, 1, 1, 1];
        V4.Deserialize([0x95, 0x01, 0x01, 0x01, 0x01, 0x91, 0x07], ref four);
        Assert.IsType<object[,,,]>(four);

        // an array of the exact type is still refilled in place
        var same = new object[1, 2];
        object[,]? reused = same;
        V4.Deserialize([0x93, 0x01, 0x02, 0x92, 0x01, 0x02], ref reused);
        Assert.Same(same, reused);
    }

    [Theory]
    [InlineData("System.Void")]
    [InlineData("System.Int32*")]
    [InlineData("System.Int32&")]
    [InlineData("System.Span`1[[System.Byte]]")]
    [InlineData("System.Collections.Generic.List`1")]
    public void Typeless_NamesThatAreNotTheTypeOfAValue_ThrowTheSerializationException(string typeName)
    {
#pragma warning disable CS0618 // LoadAnyType is the opt-in, [Obsolete]-gated loader this test is about
        var options = new V4Options(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())]));
#pragma warning restore CS0618
        var name = System.Text.Encoding.UTF8.GetBytes(typeName);
        byte[] payload = name.Length <= 31
            ? [0xc7, (byte)(name.Length + 1), 0x64, (byte)(0xa0 | name.Length), .. name]
            : [0xc7, (byte)(name.Length + 2), 0x64, 0xd9, (byte)name.Length, .. name];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<object>(payload, options));
    }

    [Fact]
    public void TypeFormatter_RefusesAnImplausiblyLongName()
    {
#pragma warning disable CS0618 // TypeFormatterFactory is opt-in and [Obsolete]-gated
        var options = new V4Options(new MessagePackFormatterResolver([new TypeFormatterFactory(), MessagePackFormatterFactory.Default]));
#pragma warning restore CS0618
        Assert.Equal(typeof(int), V4.Deserialize<Type>(V4.Serialize(typeof(int), options), options));
        var nested = string.Concat(Enumerable.Repeat("A`1[[", 400)); // 2,000 characters of generic nesting
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Type>(V4.Serialize(nested), options));
    }

    [Fact]
    public void MemoryStream_PositionedPastItsLength_ReadsAsEmptyInput()
    {
        var stream = new MemoryStream();
        stream.WriteByte(1);
        stream.Position = 5;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int>(stream));

        // at its length it is an empty stream too, and one byte before it the value is read
        stream.Position = 1;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int>(stream));
        stream.Position = 0;
        Assert.Equal(1, V4.Deserialize<int>(stream));
    }

    [Fact]
    public async Task SerializeElementsAsync_NullOptions_IsArgumentNull()
    {
        var pipe = new Pipe();
        await Assert.ThrowsAsync<ArgumentNullException>(() => V4.SerializeElementsAsync(pipe.Writer, (IEnumerable<int>)[1], 1, null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => V4.SerializeElementsAsync(pipe.Writer, AsyncOne(), 1, null!, TestContext.Current.CancellationToken));

        static async IAsyncEnumerable<int> AsyncOne()
        {
            await Task.Yield();
            yield return 1;
        }
    }

    // a canceled read used to be advanced as "everything examined": a Pipe then waits for more data, so the next
    // DeserializeAsync did not return although a complete message was buffered
    [Fact]
    public async Task DeserializeAsync_AfterACanceledRead_ReturnsTheBufferedMessage()
    {
        var pipe = new Pipe();
        pipe.Writer.Write(new byte[] { 0x2a });
        await pipe.Writer.FlushAsync(TestContext.Current.CancellationToken);
        pipe.Reader.CancelPendingRead();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await V4.DeserializeAsync<int>(pipe.Reader, TestContext.Current.CancellationToken));

        var second = V4.DeserializeAsync<int>(pipe.Reader, TestContext.Current.CancellationToken).AsTask();
        Assert.Same(second, await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)));
        Assert.Equal(42, await second);
    }
    // ---- C: the two tiers agree, and nothing is dropped without a word ----

    // An overridden property sorts at the level of the declaration it overrides in both tiers. The reflection tier
    // sorted it at its own level and wrote {"Y","X"} where the generated formatter of the same shape writes {"X","Y"}.
    [Fact]
    public void OverriddenProperty_MapKeyOrder_IsTheSameInBothTiers()
    {
        var generated = V4.Serialize(new AuditOverrideGenerated { X = 1, Y = 2, Z = 3 });
        var reflected = V4.Serialize(new AuditOverrideReflected { X = 1, Y = 2, Z = 3 });
        Assert.Equal("""{"X":1,"Y":2,"Z":3}""", V4.ConvertToJson(generated));
        Assert.Equal(generated, reflected);

        var generatedKeyed = V4.Serialize(new AuditOverrideKeyedGenerated { X = 1, Y = 2 });
        var reflectedKeyed = V4.Serialize(new AuditOverrideKeyedReflected { X = 1, Y = 2 });
        Assert.Equal("""{"x":1,"y":2}""", V4.ConvertToJson(generatedKeyed));
        Assert.Equal(generatedKeyed, reflectedKeyed);

        // either order reads in either tier
        byte[] swapped = [0x83, 0xa1, (byte)'Z', 0x03, 0xa1, (byte)'Y', 0x02, 0xa1, (byte)'X', 0x01];
        Assert.Equal(1, V4.Deserialize<AuditOverrideGenerated>(swapped)!.X);
        Assert.Equal(1, V4.Deserialize<AuditOverrideReflected>(swapped)!.X);
    }

    // an override that declares only `get` inherits its setter (`derived.X = 5` compiles); both tiers wrote the member
    // and skipped it on the way back (X = 5 read as 0)
    [Fact]
    public void OverrideDeclaringOnlyGet_IsReadBackThroughTheInheritedSetter()
    {
        Assert.Equal(5, V4.Deserialize<AuditGetOnlyOverrideGenerated>(V4.Serialize(new AuditGetOnlyOverrideGenerated { X = 5 }))!.X);
        Assert.Equal(5, V4.Deserialize<AuditGetOnlyOverrideReflected>(V4.Serialize(new AuditGetOnlyOverrideReflected { X = 5 }))!.X);
        Assert.Equal(V4.Serialize(new AuditGetOnlyOverrideGenerated { X = 5 }), V4.Serialize(new AuditGetOnlyOverrideReflected { X = 5 }));
    }

    // a `new` property on the key of a virtual base property is separate storage: the generator reports the duplicate
    // key (MsgPack003), and the reflection tier dropped the derived member without a word
    [Fact]
    public void NewPropertyOnTheKeyOfAVirtualBaseProperty_IsADuplicateKeyInTheReflectionTier()
    {
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new AuditNewKeyReflected()));
        Assert.Contains("more than once", exception.Message);
    }

    // a ref struct (or pointer, or by-ref) cannot be a type argument, so no formatter exists for a member of such a
    // type: a `ReadOnlySpan<byte> Payload => Data` view made the reflection tier fail the whole type with
    // ArgumentException, and the generated tier emit code that does not compile when the member was keyed
    [Fact]
    public void MemberOfARefStructType_IsNoMemberOfTheContract()
    {
        Assert.Equal("""{"Data":"AQI="}""", V4.ConvertToJson(V4.Serialize(new AuditSpanViewContractless { Data = [1, 2] }, Contractless)));
        Assert.Equal("""{"Data":"AQI="}""", V4.ConvertToJson(V4.Serialize(new AuditSpanViewReflected { Data = [1, 2] })));
        Assert.Equal("""{"Data":"AQI="}""", V4.ConvertToJson(V4.Serialize(new AuditSpanViewGenerated { Data = [1, 2] })));
        Assert.Equal([1, 2], V4.Deserialize<AuditSpanViewGenerated>(V4.Serialize(new AuditSpanViewReflected { Data = [1, 2] }))!.Data);
    }

    // a type-level [MessagePackFormatter] on an enum in an assembly the generator did not process was ignored: the
    // enum was written as an integer, where a class in the same position fails with the message naming the attribute
    [Fact]
    public void EnumWithATypeLevelFormatter_NotCompiledByTheGenerator_FailsLikeAClass()
    {
        var assembly = AnalyzerTestHost.CompileAndLoad("""
            using MessagePack;
            using MessagePack.Formatters;
            [MessagePackFormatter(typeof(EnumAsStringFormatterFactory<Fruit>))] public enum Fruit { Apple = 1, Banana = 7 }
            public enum Plain { A = 1 }
            """, withGenerator: false);
        var fruit = assembly.GetType("Fruit")!;
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(fruit, Enum.ToObject(fruit, 7)));
        Assert.Contains("[MessagePackFormatter]", exception.Message);
        // an enum without the attribute is the integer it always was
        var plain = assembly.GetType("Plain")!;
        Assert.Equal([0x01], V4.Serialize(plain, Enum.ToObject(plain, 1)));
    }

    // 0 (or a negative value) made every Stream and PipeReader read fail; MaxDepth = 0 means unlimited, this does not
    [Fact]
    public void MaxBufferedMessageSize_MustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => V4Options.Default with { MaxBufferedMessageSize = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => V4Options.Default with { MaxBufferedMessageSize = -1 });
        var unlimited = V4Options.Default with { MaxBufferedMessageSize = long.MaxValue };
        Assert.Equal(1, V4.Deserialize<int>(new MemoryStream([0x01]), unlimited));
    }
}

public class AuditSortedList : SortedList<int, object?>
{
}

[MessagePackObject(true)]
public class AuditOverrideBaseGenerated
{
    public virtual int X { get; set; }

    public int Y { get; set; }
}

[MessagePackObject(true)]
public class AuditOverrideGenerated : AuditOverrideBaseGenerated
{
    public override int X { get; set; }

    public int Z { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class AuditOverrideBaseReflected
{
    public virtual int X { get; set; }

    public int Y { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class AuditOverrideReflected : AuditOverrideBaseReflected
{
    public override int X { get; set; }

    public int Z { get; set; }
}

[MessagePackObject]
public class AuditOverrideKeyedBaseGenerated
{
    [Key("x")]
    public virtual int X { get; set; }

    [Key("y")]
    public int Y { get; set; }
}

[MessagePackObject]
public class AuditOverrideKeyedGenerated : AuditOverrideKeyedBaseGenerated
{
    public override int X { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditOverrideKeyedBaseReflected
{
    [Key("x")]
    public virtual int X { get; set; }

    [Key("y")]
    public int Y { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditOverrideKeyedReflected : AuditOverrideKeyedBaseReflected
{
    public override int X { get; set; }
}

[MessagePackObject]
public class AuditGetOnlyOverrideBaseGenerated
{
    [Key(0)]
    public virtual int X { get; set; }
}

[MessagePackObject]
public class AuditGetOnlyOverrideGenerated : AuditGetOnlyOverrideBaseGenerated
{
    public override int X { get => base.X; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditGetOnlyOverrideBaseReflected
{
    [Key(0)]
    public virtual int X { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditGetOnlyOverrideReflected : AuditGetOnlyOverrideBaseReflected
{
    public override int X { get => base.X; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditNewKeyBaseReflected
{
    [Key(0)]
    public virtual int X { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class AuditNewKeyReflected : AuditNewKeyBaseReflected
{
    [Key(0)]
    public new int X { get; set; }
}

public class AuditSpanViewContractless
{
    public byte[] Data { get; set; } = [];

    public ReadOnlySpan<byte> Payload => Data;
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class AuditSpanViewReflected
{
    public byte[] Data { get; set; } = [];

    public ReadOnlySpan<byte> Payload => Data;
}

[MessagePackObject(true)]
public class AuditSpanViewGenerated
{
    public byte[] Data { get; set; } = [];

    public ReadOnlySpan<byte> Payload => Data;
}

[MessagePackObject]
public class AuditItem
{
    [Key(0)]
    public int Id { get; set; }
}

public class AuditItemsById : System.Collections.ObjectModel.KeyedCollection<int, AuditItem>
{
    protected override int GetKeyForItem(AuditItem item) => item.Id;
}

public class AuditThrowingList : List<int>
{
    public AuditThrowingList() => throw new InvalidOperationException("list says no");
}

public class AuditThrowingCtor
{
    public AuditThrowingCtor() => throw new InvalidOperationException("type says no");

    public int A { get; set; }
}

[MessagePackObject]
public class AuditBoard
{
    [Key(0)]
    public object[,] Cells { get; set; } = new string[1, 2];
}

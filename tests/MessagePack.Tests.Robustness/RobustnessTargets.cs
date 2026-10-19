using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using MessagePack;
using NativeCompressions;


namespace MessagePack.Tests.Robustness;

// The shared attack surface: every (target shape x resolver tier) deserialize thunk, the entry-point thunks (the
// async boundary scanner over a PipeReader, the message and element streams, the non-seekable Stream, JSON
// conversion), plus the valid-payload seed corpus. Extracted from RobustnessTests so the coverage-guided
// SharpFuzz harness (MessagePack.Tests.Fuzz) drives the exact same surface: the randomized
// property tests and the fuzzer stay in lockstep by construction, not by copy-paste.
// Public because this file is compile-linked into MessagePack.Tests.Fuzz.Target, whose
// consumers live in a different assembly.
public static class RobustnessTargets
{
    // ---- resolver tiers under test ----
    // Default = source-generated formatters (in-box tier)
    static readonly MessagePackSerializerOptions Generated = MessagePackSerializerOptions.Default;

    // reflection tier: SuppressSourceGeneration is per-type, so instead force the whole
    // graph onto reflection by resolving through a contractless-augmented chain whose
    // types are the sample models - the generated formatters still win by registration, so
    // to genuinely hit reflection we use the models declared without generation below.
    // Practically, Contractless exercises the map-by-name reflection path for ANY type.
    static readonly MessagePackSerializerOptions Contractless = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

    static readonly MessagePackSerializerOptions Lenient = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default]) { ValidateRequiredMembers = false });

    static readonly MessagePackSerializerOptions Strict = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default]) { ValidateNullableAnnotations = true });

    // the .NET-native formats (DateTime as ticks+kind, ...) are a second set of built-in formatters with their own
    // readers
    static readonly MessagePackSerializerOptions DotNet = MessagePackSerializerOptions.DotNetOptimized;

    // Typeless: object slots carry a type name the reader resolves through the loader; AllowedTypes is the
    // production shape, so a forged name must be refused there (and the name decoding itself is attacker input)
    static readonly MessagePackSerializerOptions Typeless = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.AllowedTypes(
            typeof(SampleArrayPoco), typeof(SampleMapPoco), typeof(SampleCtorPoco), typeof(SampleCircle), typeof(SampleRectangle), typeof(SampleTriangle),
            typeof(int[]), typeof(string), typeof(List<SampleArrayPoco>), typeof(Dictionary<string, SampleMapPoco>)))]));

    // LZ4 decode is a distinct attack surface: the MessageProcessor.TryDecode path runs
    // before any formatter, so malformed block headers / decompression bombs must be
    // rejected by the ext-envelope parse and the MaxDecompressedSize cap, again only ever
    // as MessagePackSerializationException. A small cap makes the bomb guard reachable
    // within the generated payload sizes.
    static readonly MessagePackSerializerOptions Lz4Block = MessagePackSerializerOptions.Default.WithLz4Block(maxDecompressedSize: 1 << 20);
    static readonly MessagePackSerializerOptions Lz4Array = MessagePackSerializerOptions.Default.WithLz4BlockArray(maxDecompressedSize: 1 << 20);

    // the container processors (one standard compression frame per message, header walked by the processor itself,
    // declared content sizes checked against what the compressed bytes could produce) and the envelope processor
    static readonly MessagePackSerializerOptions Lz4Frame = MessagePackSerializerOptions.Default.WithLz4Frame(maxDecompressedSize: 1 << 20);
    static readonly MessagePackSerializerOptions Zstd = MessagePackSerializerOptions.Default.WithZstandardFrame(3, maxDecompressedSize: 1 << 20);
    static readonly MessagePackSerializerOptions Framing = MessagePackSerializerOptions.Default.WithFraming();

    // deserialize thunks: one per (target type, tier). Each takes raw bytes and attempts a
    // full deserialize, discarding the result. The caller classifies whatever happens.
    public static readonly (string Name, Action<byte[]> Deserialize)[] Targets = BuildTargets();

    static (string, Action<byte[]>)[] BuildTargets()
    {
        var list = new List<(string, Action<byte[]>)>();
        void Add<T>(string tier, MessagePackSerializerOptions opt)
            => list.Add(($"{typeof(T).Name}/{tier}", bytes => MessagePackSerializer.Deserialize<T>(bytes, opt)));

        foreach (var (tier, opt) in new[]
        {
            ("gen", Generated), ("contractless", Contractless), ("lenient", Lenient), ("strict", Strict), ("dotnet", DotNet), ("typeless", Typeless),
            ("lz4block", Lz4Block), ("lz4array", Lz4Array), ("lz4frame", Lz4Frame), ("zstd", Zstd), ("framing", Framing),
        })
        {
            Add<SampleArrayPoco>(tier, opt);
            Add<SampleMapPoco>(tier, opt);
            Add<SampleCtorPoco>(tier, opt);
            Add<SampleUnknownPoco>(tier, opt);
            Add<ISampleShape>(tier, opt);
            Add<SampleTriangle>(tier, opt);
            Add<int[]>(tier, opt);
            Add<int[,]>(tier, opt); // multi-dim: dimension-length x element-count product is attacker-controlled
            Add<int[,,]>(tier, opt);
            Add<List<SampleArrayPoco>>(tier, opt);
            Add<Dictionary<string, SampleMapPoco>>(tier, opt);
            Add<SampleTreePoco>(tier, opt); // recursive collections: the nested-header amplification shape
            Add<string>(tier, opt);
            Add<object[]>(tier, opt);
        }

        // ---- entry points other than the synchronous byte[] one ----
        // the async entries find the message boundary first (MessagePackBoundaryScanner over the PipeReader's buffered
        // bytes, resumed across segments; the processor's own header walk under a frame processor), the message and
        // element streams repeat that per message, the Stream entry buffers a non-seekable stream to its end. The
        // bytes arrive in small segments so every resume path runs.
        void AddEntries<T>(string tier, MessagePackSerializerOptions opt, bool container)
        {
            list.Add(($"{typeof(T).Name}/{tier}/pipe", bytes => MessagePackSerializer.DeserializeAsync<T>(Segmented(bytes), opt).AsTask().GetAwaiter().GetResult()));
            list.Add(($"{typeof(T).Name}/{tier}/messages", bytes => Drain(MessagePackSerializer.DeserializeMessagesAsync<T>(Segmented(bytes), opt))));
            if (!container)
            {
                // a container processor owns the message boundaries, so the element stream is NotSupportedException by
                // contract there, not an input-dependent outcome
                list.Add(($"{typeof(T).Name}/{tier}/elements", bytes => Drain(MessagePackSerializer.DeserializeElementsAsync<T>(Segmented(bytes), opt))));
            }
            list.Add(($"{typeof(T).Name}/{tier}/stream", bytes => MessagePackSerializer.Deserialize<T>(new NonSeekableStream(bytes), opt)));
        }

        foreach (var (tier, opt, container) in new[] { ("gen", Generated, false), ("framing", Framing, false), ("lz4frame", Lz4Frame, true), ("zstd", Zstd, true) })
        {
            AddEntries<SampleArrayPoco>(tier, opt, container);
            AddEntries<ISampleShape>(tier, opt, container);
            AddEntries<int[]>(tier, opt, container);
            AddEntries<object[]>(tier, opt, container);
            AddEntries<string>(tier, opt, container);
            AddEntries<SampleTreePoco>(tier, opt, container);
        }

        // JSON conversion walks the raw msgpack without a target type (its own depth limit and token reader)
        list.Add(("json", bytes => MessagePackSerializer.ConvertToJson(bytes)));

        return list.ToArray();
    }

    // the payload as a PipeReader over a multi-segment sequence (5-byte segments), so headers and payloads straddle
    // segment boundaries and the scanner's resume positions are exercised on every input
    static PipeReader Segmented(byte[] bytes) => PipeReader.Create(SegmentedSequence(bytes, 5));

    public static ReadOnlySequence<byte> SegmentedSequence(byte[] bytes, int segmentSize)
    {
        if (bytes.Length <= segmentSize)
        {
            return new ReadOnlySequence<byte>(bytes);
        }
        var first = new Segment(bytes.AsMemory(0, segmentSize), 0);
        var last = first;
        for (var offset = segmentSize; offset < bytes.Length; offset += segmentSize)
        {
            last = last.Append(bytes.AsMemory(offset, Math.Min(segmentSize, bytes.Length - offset)));
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

    static void Drain<T>(IAsyncEnumerable<T> source)
    {
        var enumerator = source.GetAsyncEnumerator();
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // a network-like stream: no length, no seeking, data in small reads
    sealed class NonSeekableStream : Stream
    {
        readonly byte[] bytes;
        int position;

        public NonSeekableStream(byte[] bytes) => this.bytes = bytes;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var n = Math.Min(Math.Min(buffer.Length, 7), bytes.Length - position);
            bytes.AsSpan(position, n).CopyTo(buffer);
            position += n;
            return n;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // A representative valid payload per target, used as the mutation/truncation seed.
    public static byte[][] SeedCorpus()
    {
        var poco = new SampleArrayPoco
        {
            Id = 42,
            Name = "hello",
            Score = 3.14,
            Flag = true,
            Color = SampleColor.Blue,
            When = new DateTime(2026, 8, 30, 1, 2, 3, DateTimeKind.Utc),
            Numbers = [1, 2, 3, -1, int.MaxValue, int.MinValue],
            Tags = ["a", "bb", "ccc"],
            Child = new SampleArrayPoco { Id = 7, Name = "child", Numbers = [] },
            Map = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 },
        };
        var map = new SampleMapPoco { Id = 1, Name = "m", Score = 2.5, Color = SampleColor.Green, Numbers = [4, 5, 6], Child = new SampleMapPoco { Id = 2 } };
        var ctor = new SampleCtorPoco(9, "ctor", SampleColor.Red) { Extra = 100 };
        ISampleShape shape = new SampleTriangle { Base = 1, Height = 2, Inscribed = new SampleCircle { Radius = 0.5 } };
        var pocos = new List<SampleArrayPoco> { poco, poco, poco, poco };

        return
        [
            MessagePackSerializer.Serialize(poco),
            MessagePackSerializer.Serialize(map),
            MessagePackSerializer.Serialize(ctor),
            MessagePackSerializer.Serialize(shape),
            MessagePackSerializer.Serialize<int[]>([1, 2, 3, 4, 5]),
            MessagePackSerializer.Serialize(new List<SampleArrayPoco> { poco, poco }),
            MessagePackSerializer.Serialize<object[]>([1, "two", 3.0, true, null!]),
            // valid LZ4 envelopes, so mutation/truncation exercises the decode path with
            // a real block header to corrupt (not just random bytes that fail the ext parse)
            MessagePackSerializer.Serialize(pocos, Lz4Block),
            MessagePackSerializer.Serialize(pocos, Lz4Array),
            // the frames and the envelope, the same way: real headers, content sizes and checksums to corrupt
            MessagePackSerializer.Serialize(pocos, Lz4Frame),
            MessagePackSerializer.Serialize(pocos, Zstd),
            MessagePackSerializer.Serialize(poco, Framing),
            // the .NET-native DateTime form, and typeless payloads carrying type names in the object slots
            MessagePackSerializer.Serialize(poco, DotNet),
            MessagePackSerializer.Serialize<object[]>([poco, "two", 3, shape], Typeless),
            MessagePackSerializer.Serialize<object>(map, Typeless),
            // a message stream (three messages back to back) and an element stream (header, then one message per
            // element): the shapes the async entries read
            [.. MessagePackSerializer.Serialize(poco), .. MessagePackSerializer.Serialize(poco), .. MessagePackSerializer.Serialize(poco)],
            Elements(pocos, Generated),
            Elements(pocos, Framing),
            // the nested-header allocation amplification (Nerdbank.MessagePack GHSA-grf3-jgmf-2g5j): a chain of array
            // or map headers each claiming the bytes that remain, so each level passes the per-header bound and would
            // preallocate for the whole tail before the depth check ends the descent. DeserializeState's declared-element
            // budget rejects the chain (DeclaredElementBudgetTest); these seeds put the mutator right at that boundary,
            // for every tier and entry, and inside the frames (the budget is the decoded length there)
            NestedArrayBomb(4096, 200),
            NestedMapBomb(4096, 200),
            RecursiveTreeBomb(4096, 200),
            Framed(NestedArrayBomb(4096, 200)),
            LZ4.Compress(NestedArrayBomb(4096, 200), new LZ4CompressionOptions { ContentSize = 4096 }),
            Zstandard.Compress(NestedArrayBomb(4096, 200), new ZstandardCompressionOptions { CompressionLevel = 3 }),
            // and the legitimate deep shapes next to them: a 100-level tree, and 100 nested object arrays with typeless type names
            MessagePackSerializer.Serialize(DeepTree(100)),
            MessagePackSerializer.Serialize<object>(DeepObjectArrays(100), Typeless),
        ];
    }

    /// <summary>`layers` nested array32 headers, each claiming exactly the bytes that remain after it, then nil filler.</summary>
    public static byte[] NestedArrayBomb(int totalLength, int layers)
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

    /// <summary>The map32 variant: each level claims (remaining / 2) entries, the per-header bound for maps.</summary>
    public static byte[] NestedMapBomb(int totalLength, int layers)
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

    /// <summary>
    /// The typed recursive shape (SampleTreePoco): every level is [ Children: array32(remaining) ] whose first child is
    /// the next level, so the object header and the collection header both charge the budget.
    /// </summary>
    public static byte[] RecursiveTreeBomb(int totalLength, int layers)
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

    /// <summary>The WithFraming envelope around a raw message: ext32 header, length, the framing type code.</summary>
    public static byte[] Framed(byte[] message)
    {
        var framed = new byte[message.Length + 6];
        framed[0] = MessagePackCode.Ext32;
        BinaryPrimitives.WriteUInt32BigEndian(framed.AsSpan(1), (uint)message.Length);
        framed[5] = unchecked((byte)ThisLibraryExtensionTypeCodes.Framing);
        message.CopyTo(framed, 6);
        return framed;
    }

    static SampleTreePoco DeepTree(int levels)
    {
        var root = new SampleTreePoco { Value = 0 };
        var current = root;
        for (int i = 1; i < levels; i++)
        {
            var next = new SampleTreePoco { Value = i };
            current.Children = [next];
            current.Kids = [null]; // not `next` again: a node reachable twice per level doubles the output per level
            current = next;
        }
        return root;
    }

    static object DeepObjectArrays(int levels)
    {
        object current = new object[] { 1, "leaf" };
        for (int i = 1; i < levels; i++)
        {
            current = new object[] { current, i };
        }
        return current;
    }

    static byte[] Elements<T>(IReadOnlyList<T> values, MessagePackSerializerOptions options)
    {
        var pipe = new Pipe();
        MessagePackSerializer.SerializeElementsAsync(pipe.Writer, values, values.Count, options).GetAwaiter().GetResult();
        pipe.Writer.Complete();
        var result = pipe.Reader.ReadAtLeastAsync(int.MaxValue).AsTask().GetAwaiter().GetResult();
        var bytes = result.Buffer.ToArray();
        pipe.Reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }
}

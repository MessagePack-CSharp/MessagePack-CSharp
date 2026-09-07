using System.Buffers;
using SerializerFoundation;

namespace MessagePack.Tests;

// Chunk-boundary sweeps for every buffer tier. A gate that only ever writes into a generous
// ArrayBufferWriter (or reads a single contiguous span) never drives the refill/reposition
// arms of the buffers, so a broken fast/slow transition can pass the whole suite: the
// differential of another serializer shipped a fully broken span-direct writer that way.
//
// Write side: an IBufferWriter that grants EXACTLY max(sizeHint, lease) bytes and never more,
// swept over lease sizes, so the flush boundary walks through every emitted op of a corpus
// that covers every token family (all integer widths, str/bin/array/map 8/16/32 headers, the
// SIMD primitive-array region codecs across their region size, string keys, unions, ext).
// Read side: the same corpus as a two-segment ReadOnlySequence split at EVERY byte offset, plus
// the all-single-byte-segments sequence. The oracle is the byte[] entry's canonical output
// (roundtrips are proven by re-serializing what was read and comparing bytes).
public class ChunkBoundarySweepTests
{
    // dense below 64 (every token width and every header width lands mid-lease), then the
    // power-of-two edges up to the largest single reservation the region codecs make
    static readonly int[] LeaseSizes =
    [
        .. Enumerable.Range(1, 64),
        100, 127, 128, 129, 255, 256, 257, 511, 512, 513, 1000, 1023, 1024, 1025,
        4095, 4096, 4097, 8191, 8192, 8193, 16383, 16384, 16385, 65535, 65536, 65537,
    ];

    // ---------------------------------------------------------------- write sweeps

    [Fact]
    public void Write_StrictLeaseSweep_Medium()
    {
        var value = SweepCorpus.Medium();
        var canonical = MessagePackSerializer.Serialize(value);
        SweepWrites(value, canonical);
    }

    [Fact]
    public void Write_StrictLeaseSweep_Large()
    {
        var value = SweepCorpus.Large();
        var canonical = MessagePackSerializer.Serialize(value);
        Assert.True(canonical.Length > 65536, "the large corpus must carry 32-bit headers and multiple SIMD regions");
        SweepWrites(value, canonical);
    }

    static void SweepWrites<T>(T value, byte[] canonical)
    {
        foreach (var lease in LeaseSizes)
        {
            // primary tier through the public IBufferWriter entry (BufferWriterWriteBuffer)
            var output = new ExactBufferWriter(lease);
            MessagePackSerializer.Serialize(output, value);
            AssertBytes(canonical, output.ToArray(), $"IBufferWriter entry, lease={lease}");
            if (lease <= 64)
            {
                // the sweep must actually cross the boundary, or it measures nothing
                Assert.True(output.LeaseCount > 1, $"lease={lease} never refilled; the sweep is vacuous");
            }

            // fallback tier (Memory<byte> window, GetMemory-based), driven directly
            var compatOutput = new ExactBufferWriter(lease);
            var compat = new CompatibleBufferWriterWriteBuffer(compatOutput);
            try
            {
                MessagePackSerializer.Serialize(ref compat, value);
                compat.Flush();
                Assert.Equal(canonical.Length, compat.BytesWritten);
            }
            finally
            {
                compat.Dispose();
            }
            AssertBytes(canonical, compatOutput.ToArray(), $"CompatibleBufferWriterWriteBuffer, lease={lease}");

            // pooled-segment tier: the scratch -> pooled seam and segment growth, swept by scratch size
            var pooled = new ArrayPoolListWriteBuffer(new byte[lease]);
            try
            {
                MessagePackSerializer.Serialize(ref pooled, value);
                Assert.Equal(canonical.Length, pooled.BytesWritten);
                AssertBytes(canonical, pooled.ToArray(), $"ArrayPoolListWriteBuffer, scratch={lease}");
            }
            finally
            {
                pooled.Dispose();
            }
        }
    }

    // ---------------------------------------------------------------- read sweeps

    [Fact]
    public void Read_TwoSegmentSplitAtEveryOffset_Medium()
    {
        var canonical = MessagePackSerializer.Serialize(SweepCorpus.Medium());
        for (int split = 0; split <= canonical.Length; split++)
        {
            ReadAllTiers<SweepRoot>(canonical, TwoSegments(canonical, split), $"split={split}");
        }
    }

    [Fact]
    public void Read_TwoSegmentSplitStrided_Large()
    {
        var canonical = MessagePackSerializer.Serialize(SweepCorpus.Large());

        // a prime stride so the seam lands at every residue of every token width (and several
        // times inside every 32-bit-headed member), plus both edges; every-offset coverage of
        // the token straddles themselves is the medium sweep's job, this one is about the
        // region codecs and the 32-bit headers at a cost that stays in seconds
        var splits = new SortedSet<int>();
        for (int split = 0; split <= canonical.Length; split += 8191) splits.Add(split);
        for (int i = 0; i < 8; i++)
        {
            splits.Add(i);
            splits.Add(canonical.Length - i);
        }
        foreach (var split in splits)
        {
            ReadAllTiers<SweepLarge>(canonical, TwoSegments(canonical, split), $"split={split}");
        }
    }

    [Fact]
    public void Read_AllSingleByteSegments_Medium()
    {
        var canonical = MessagePackSerializer.Serialize(SweepCorpus.Medium());
        ReadAllTiers<SweepRoot>(canonical, SingleByteSegments(canonical), "single-byte segments");
    }

    static void ReadAllTiers<T>(byte[] canonical, ReadOnlySequence<byte> sequence, string label)
    {
        // public sequence entry (primary tier with the entry's own scratch policy)
        var viaEntry = MessagePackSerializer.Deserialize<T>(sequence);
        AssertBytes(canonical, MessagePackSerializer.Serialize(viaEntry), $"sequence entry, {label}");

        // primary tier with NO scratch: every straddling window takes the pool-rent arm
        var noScratch = new ReadOnlySequenceReadBuffer(sequence, default);
        try
        {
            var value = MessagePackSerializer.Deserialize<ReadOnlySequenceReadBuffer, T>(ref noScratch);
            Assert.Equal(canonical.Length, noScratch.BytesConsumed);
            AssertBytes(canonical, MessagePackSerializer.Serialize(value), $"ReadOnlySequenceReadBuffer/no scratch, {label}");
        }
        finally
        {
            noScratch.Dispose();
        }

        // fallback tier
        var compat = new CompatibleReadOnlySequenceReadBuffer(sequence);
        try
        {
            var value = MessagePackSerializer.Deserialize<CompatibleReadOnlySequenceReadBuffer, T>(ref compat);
            Assert.Equal(canonical.Length, compat.BytesConsumed);
            AssertBytes(canonical, MessagePackSerializer.Serialize(value), $"CompatibleReadOnlySequenceReadBuffer, {label}");
        }
        finally
        {
            compat.Dispose();
        }
    }

    // ---------------------------------------------------------------- the strict writer itself

    [Fact]
    public void ExactBufferWriter_GrantsExactlyWhatIsAsked_AndRejectsOverrun()
    {
        // the fixture must be able to fail, or the sweep above proves nothing
        var writer = new ExactBufferWriter(leaseSize: 3);
        Assert.Equal(3, writer.GetSpan(0).Length);
        Assert.Equal(3, writer.GetSpan(2).Length);
        Assert.Equal(7, writer.GetSpan(7).Length);
        writer.Advance(7);
        Assert.Equal(5, writer.GetMemory(5).Length);
        Assert.Throws<InvalidOperationException>(() => writer.Advance(6));
        writer.Advance(5);
        Assert.Throws<InvalidOperationException>(() => writer.Advance(1)); // nothing granted
        Assert.Equal(12, writer.ToArray().Length);
        Assert.Equal(300, writer.GetSpan(300).Length); // growth past the initial 256 bytes
    }

    /// <summary>
    /// An IBufferWriter that honours the contract and nothing beyond it: each GetSpan/GetMemory
    /// hands out exactly max(sizeHint, lease) bytes, and Advance past the grant throws.
    /// </summary>
    sealed class ExactBufferWriter(int leaseSize) : IBufferWriter<byte>
    {
        byte[] storage = new byte[256];
        int written;
        int granted = -1;

        public int LeaseCount { get; private set; }

        public void Advance(int count)
        {
            if (granted < 0 || (uint)count > (uint)granted)
            {
                throw new InvalidOperationException($"Advance({count}) exceeds the {granted} bytes granted");
            }
            written += count;
            granted = -1;
        }

        // Grant may replace `storage`, so it has to run before the array is read
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var size = Grant(sizeHint);
            return storage.AsMemory(written, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var size = Grant(sizeHint);
            return storage.AsSpan(written, size);
        }

        int Grant(int sizeHint)
        {
            if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
            var size = Math.Max(sizeHint, leaseSize);
            if (storage.Length - written < size)
            {
                Array.Resize(ref storage, Math.Max(storage.Length * 2, written + size));
            }
            granted = size;
            LeaseCount++;
            return size;
        }

        public byte[] ToArray() => storage.AsSpan(0, written).ToArray();
    }

    // ---------------------------------------------------------------- helpers

    static ReadOnlySequence<byte> TwoSegments(byte[] data, int splitAt)
    {
        var second = new Segment(data.AsMemory(splitAt), null, splitAt);
        var first = new Segment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }

    static ReadOnlySequence<byte> SingleByteSegments(byte[] data)
    {
        var last = new Segment(data.AsMemory(data.Length - 1, 1), null, data.Length - 1);
        var next = last;
        for (int i = data.Length - 2; i >= 0; i--)
        {
            next = new Segment(data.AsMemory(i, 1), next, i);
        }
        return new ReadOnlySequence<byte>(next, 0, last, 1);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? next, long runningIndex)
        {
            Memory = memory;
            Next = next;
            RunningIndex = runningIndex;
        }
    }

    static void AssertBytes(byte[] expected, byte[] actual, string label)
    {
        if (expected.AsSpan().SequenceEqual(actual))
        {
            return;
        }
        var common = expected.AsSpan().CommonPrefixLength(actual);
        Assert.Fail($"{label}: bytes differ at offset {common} (expected length {expected.Length}, actual {actual.Length}; " +
            $"expected byte {(common < expected.Length ? $"0x{expected[common]:x2}" : "<end>")}, " +
            $"actual byte {(common < actual.Length ? $"0x{actual[common]:x2}" : "<end>")})");
    }
}

// ---------------------------------------------------------------- corpus

static class SweepCorpus
{
    static readonly int[] IntPool = [0, 1, -1, -32, -33, 127, 128, 255, 256, 65535, 65536, -32768, -32769, int.MaxValue, int.MinValue];
    static readonly short[] ShortPool = [0, 1, -1, -32, -33, 127, 128, 255, 256, -128, -129, short.MinValue, short.MaxValue];
    static readonly long[] LongPool = [0, 1, -1, -32, 127, int.MaxValue, int.MinValue, (long)int.MaxValue + 1, long.MaxValue, long.MinValue];
    static readonly float[] FloatPool = [0f, -0f, 1.5f, float.MaxValue, float.Epsilon, float.PositiveInfinity, float.NaN, 12345.678f];
    static readonly double[] DoublePool = [0d, 1.5, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.NaN, 12345.6789];

    static T[] Pick<T>(Random rand, int count, T[] pool)
    {
        var values = new T[count];
        for (int i = 0; i < count; i++) values[i] = pool[rand.Next(pool.Length)];
        return values;
    }

    // ASCII, 3-byte (Japanese) and 4-byte (emoji) so GetMaxStringByteCount over-reserves for real
    static string Text(Random rand, int chars)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789 あいうえお漢字😀🚀";
        var text = new System.Text.StringBuilder(chars);
        while (text.Length < chars)
        {
            var c = alphabet[rand.Next(alphabet.Length)];
            if (char.IsHighSurrogate(c))
            {
                text.Append(c).Append(alphabet[alphabet.IndexOf(c) + 1]);
            }
            else if (!char.IsLowSurrogate(c))
            {
                text.Append(c);
            }
        }
        return text.ToString(0, chars);
    }

    static byte[] Bytes(Random rand, int count)
    {
        var bytes = new byte[count];
        rand.NextBytes(bytes);
        return bytes;
    }

    static SweepInner Inner(Random rand, int id) => new()
    {
        Id = id,
        Name = rand.Next(4) == 0 ? null : Text(rand, rand.Next(0, 40)),
        Ratio = rand.NextDouble(),
        Color = (SweepColor)(rand.Next(3) switch { 0 => 0, 1 => 1, _ => 200 }),
    };

    public static SweepRoot Medium()
    {
        var rand = new Random(1234);
        return new SweepRoot
        {
            SByte = -100,
            Byte = 200,
            Int16 = -12345,
            UInt16 = 54321,
            Int32 = -70000,
            UInt32 = 3_000_000_000,
            Int64 = long.MinValue + 5,
            UInt64 = ulong.MaxValue - 5,
            Single = 3.5f,
            Double = -2.25,
            Boolean = true,
            Char = 'あ',
            Decimal = 123456.789m,
            Guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
            DateTime = new DateTime(2026, 9, 2, 12, 34, 56, DateTimeKind.Utc),
            TimeSpan = TimeSpan.FromMilliseconds(123456789),
            EmptyString = "",
            // sizes sit just past each header-width edge and past one SIMD chunk of each
            // codec, keeping the every-offset read sweep (payload^2 work) around 3 KB
            FixStr = Text(rand, 31),
            Str8 = Text(rand, 40),
            Str16 = Text(rand, 270),
            NullString = null,
            Bin8 = Bytes(rand, 40),
            Bin16 = Bytes(rand, 270),
            NullBytes = null,
            Ints = Pick(rand, 200, IntPool),
            Bools = Pick(rand, 70, [true, false]),
            Floats = Pick(rand, 33, FloatPool),
            Doubles = Pick(rand, 33, DoublePool),
            Shorts = Pick(rand, 70, ShortPool),
            Longs = Pick(rand, 33, LongPool),
            EmptyInts = [],
            Colors = Pick(rand, 20, [SweepColor.Red, SweepColor.Green, SweepColor.Blue]),
            Level = SweepLevel.High,
            Inner = Inner(rand, 1),
            NullInner = null,
            Inners = [.. Enumerable.Range(0, 8).Select(i => Inner(rand, i))],
            InnerList = [.. Enumerable.Range(100, 5).Select(i => Inner(rand, i))],
            StringToInt = Enumerable.Range(0, 16).ToDictionary(i => Text(rand, 1 + i), i => IntPool[i % IntPool.Length]),
            // distinct keys across every integer width class: ±1, ∓2, ±4, ... ∓2^15
            IntToString = Enumerable.Range(0, 16).ToDictionary(i => (i % 2 == 0 ? 1 : -1) * (1 << i), i => Text(rand, i * 2)),
            NullableSet = 5,
            NullableNull = null,
            StringKeyed = new SweepStringKeyed
            {
                Alpha = 42,
                BetaGamma = Text(rand, 50),
                DeltaEpsilonZeta = Pick(rand, 33, LongPool),
                Eta = Inner(rand, 7),
            },
            Shapes = [new SweepCircle { Radius = 1.5 }, new SweepSquare { Side = 3, Label = Text(rand, 20) }, new SweepCircle { Radius = -2 }, null, new SweepSquare { Side = -1, Label = null }],
            Strings = [.. Enumerable.Range(0, 20).Select(i => Text(rand, i * 3))],
            Levels = Pick(rand, 40, [SweepLevel.Low, SweepLevel.Mid, SweepLevel.High, (SweepLevel)200]),
            LevelList = [.. Pick(rand, 21, [SweepLevel.Low, SweepLevel.Mid, SweepLevel.High])],
        };
    }

    public static SweepLarge Large()
    {
        var rand = new Random(5678);
        return new SweepLarge
        {
            Str32 = Text(rand, 65_600),               // str32 header (> 65535 bytes)
            Bin32 = Bytes(rand, 65_600),              // bin32 header
            Ints = Pick(rand, 65_600, IntPool),       // array32 header, 17 SIMD regions of 4096 elements
            Doubles = Pick(rand, 4_200, DoublePool),  // crosses the 4096-element region once
            Bools = Pick(rand, 8_300, [true, false]), // crosses the 8192-element region once
            Shorts = Pick(rand, 8_300, ShortPool),    // same, for the 2-byte codec
            IntList = [.. Pick(rand, 300, IntPool)],  // the List<T> shape of the same codec
            Tail = Inner(rand, -1),
        };
    }
}

public enum SweepColor : byte { Red = 0, Green = 1, Blue = 200 }

public enum SweepLevel { Low = -1, Mid = 0, High = 70000 }

[MessagePackObject]
public class SweepInner
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Ratio { get; set; }
    [Key(3)] public SweepColor Color { get; set; }
}

[MessagePackObject(true)]
public class SweepStringKeyed
{
    public int Alpha { get; set; }
    public string? BetaGamma { get; set; }
    public long[]? DeltaEpsilonZeta { get; set; }
    public SweepInner? Eta { get; set; }
}

[MessagePackObject]
[UnionTag(typeof(SweepCircle), 0)]
[UnionTag(typeof(SweepSquare), 1)]
public interface ISweepShape
{
}

[MessagePackObject]
public class SweepCircle : ISweepShape
{
    [Key(0)] public double Radius { get; set; }
}

[MessagePackObject]
public class SweepSquare : ISweepShape
{
    [Key(0)] public int Side { get; set; }
    [Key(1)] public string? Label { get; set; }
}

[MessagePackObject]
public class SweepRoot
{
    [Key(0)] public sbyte SByte { get; set; }
    [Key(1)] public byte Byte { get; set; }
    [Key(2)] public short Int16 { get; set; }
    [Key(3)] public ushort UInt16 { get; set; }
    [Key(4)] public int Int32 { get; set; }
    [Key(5)] public uint UInt32 { get; set; }
    [Key(6)] public long Int64 { get; set; }
    [Key(7)] public ulong UInt64 { get; set; }
    [Key(8)] public float Single { get; set; }
    [Key(9)] public double Double { get; set; }
    [Key(10)] public bool Boolean { get; set; }
    [Key(11)] public char Char { get; set; }
    [Key(12)] public decimal Decimal { get; set; }
    [Key(13)] public Guid Guid { get; set; }
    [Key(14)] public DateTime DateTime { get; set; }
    [Key(15)] public TimeSpan TimeSpan { get; set; }
    [Key(16)] public string? EmptyString { get; set; }
    [Key(17)] public string? FixStr { get; set; }
    [Key(18)] public string? Str8 { get; set; }
    [Key(19)] public string? Str16 { get; set; }
    [Key(20)] public string? NullString { get; set; }
    [Key(21)] public byte[]? Bin8 { get; set; }
    [Key(22)] public byte[]? Bin16 { get; set; }
    [Key(23)] public byte[]? NullBytes { get; set; }
    [Key(24)] public int[]? Ints { get; set; }
    [Key(25)] public bool[]? Bools { get; set; }
    [Key(26)] public float[]? Floats { get; set; }
    [Key(27)] public double[]? Doubles { get; set; }
    [Key(28)] public short[]? Shorts { get; set; }
    [Key(29)] public long[]? Longs { get; set; }
    [Key(30)] public int[]? EmptyInts { get; set; }
    [Key(31)] public SweepColor[]? Colors { get; set; }
    [Key(32)] public SweepLevel Level { get; set; }
    [Key(33)] public SweepInner? Inner { get; set; }
    [Key(34)] public SweepInner? NullInner { get; set; }
    [Key(35)] public SweepInner[]? Inners { get; set; }
    [Key(36)] public List<SweepInner>? InnerList { get; set; }
    [Key(37)] public Dictionary<string, int>? StringToInt { get; set; }
    [Key(38)] public Dictionary<int, string>? IntToString { get; set; }
    [Key(39)] public int? NullableSet { get; set; }
    [Key(40)] public int? NullableNull { get; set; }
    [Key(41)] public SweepStringKeyed? StringKeyed { get; set; }
    [Key(42)] public ISweepShape?[]? Shapes { get; set; }
    [Key(43)] public string[]? Strings { get; set; }
    [Key(44)] public SweepLevel[]? Levels { get; set; }
    [Key(45)] public List<SweepLevel>? LevelList { get; set; }
}

[MessagePackObject]
public class SweepLarge
{
    [Key(0)] public string? Str32 { get; set; }
    [Key(1)] public byte[]? Bin32 { get; set; }
    [Key(2)] public int[]? Ints { get; set; }
    [Key(3)] public double[]? Doubles { get; set; }
    [Key(4)] public bool[]? Bools { get; set; }
    [Key(5)] public short[]? Shorts { get; set; }
    [Key(6)] public List<int>? IntList { get; set; }
    [Key(7)] public SweepInner? Tail { get; set; }
}

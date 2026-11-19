using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;

// The ReadNullableX readers against the expression they replaced, for the kinds NullableReadBenchmark (Int32) and
// AnswerNullableReadEndToEndBenchmark (Int32, Boolean) do not cover. One kind per wire tier:
//   Int64  - Int64ReadTable under the 9-byte gate      (ReadInt64OrNil; UInt64 is the same shape)
//   UInt16 - Narrow16ReadTable under the 3-byte gate   (ReadNarrow16OrNil; Int16 and Char are the same helper)
//   Byte   - fixint + uint8 under the 2-byte gate      (ReadByteOrNil; SByte is the same shape)
//   Double - the 0xcb bet under the 9-byte gate        (ReadDoubleOrNil; Single is the same shape)
// Same blob loop as NullableReadBenchmark: 50k objects of three nullable members in the generated for / switch
// shape, one member in five nil, values drawn uniformly from the type's encoding classes.
//   Pair   - `buffer.TryReadNil() ? default(T?) : buffer.ReadX()` at the call site (what the generator emitted)
//   Reader - `buffer.ReadNullableX()` (what it emits now)
//
// MEASURED, first round, when these readers were still the pair written out behind one return expression
// (i7-13700KF, .NET 11, ShortRun x3 not elevated; instructions from one elevated run), Reader / Pair:
//   Double  0.94 / 1.02 / 0.94   instructions -3.6%
//   Int64   1.04 / 1.04 / 1.03   instructions -2.8%   (63k mispredicts per 150k members in both: mixed widths)
// The long? reader was reproducibly 3-4% slower although it ran fewer instructions. Not the Nullable<T> stall: both
// shapes stored HasValue and the value to the fields from two registers; what differed was the block layout the JIT
// picked. That reader no longer exists: every kind now decodes through its own one-window helper.
//
// MEASURED, second round, every kind through its ReadXOrNil helper (ShortRun x2 not elevated; instructions and
// mispredicts from one elevated run, per 150k members), Reader / Pair:
//   Int64   0.92 / 0.93   instructions -15.7% (-13.5 per member)
//   UInt16  0.93 / 0.93   instructions -22.0% (-18.7)
//   Byte    1.01 / 0.93   instructions -22.9% (-15.9)   mispredicts 92k -> 99k: fixint or uint8 at random, the one
//                                                        distribution where the saving is not visible in time
//   Double  0.89 / 0.85   instructions -21.5% (-14.3)
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class NullableReaderPairBenchmark
{
    const int ObjectCount = 50_000;

    byte[] longBlob = null!;
    byte[] ushortBlob = null!;
    byte[] byteBlob = null!;
    byte[] doubleBlob = null!;
    readonly NullableLong3 longs = new();
    readonly NullableUInt16x3 ushorts = new();
    readonly NullableByte3 bytes = new();
    readonly NullableDouble3 doubles = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        longBlob = Build(rand, static (ref BufferWriterWriteBuffer b, Random r) => b.WriteInt64(r.Next(4) switch
        {
            0 => r.Next(0, 128),
            1 => r.Next(256, 65536),
            2 => r.Next(100_000, int.MaxValue),
            _ => (long)r.Next(1, int.MaxValue) << 31,
        }));
        ushortBlob = Build(rand, static (ref BufferWriterWriteBuffer b, Random r) => b.WriteUInt16((ushort)(r.Next(3) switch
        {
            0 => r.Next(0, 128),
            1 => r.Next(128, 256),
            _ => r.Next(256, 65536),
        })));
        byteBlob = Build(rand, static (ref BufferWriterWriteBuffer b, Random r) => b.WriteByte((byte)r.Next(0, 256)));
        doubleBlob = Build(rand, static (ref BufferWriterWriteBuffer b, Random r) => b.WriteDouble(r.NextDouble() * 2000.0 - 1000.0));

        if (Int64_Reader() != Int64_Pair() || UInt16_Reader() != UInt16_Pair() || Byte_Reader() != Byte_Pair() || Double_Reader() != Double_Pair())
        {
            throw new InvalidOperationException("checksum mismatch");
        }
    }

    delegate void WriteValue(ref BufferWriterWriteBuffer buffer, Random rand);

    // three members per object, one in five nil
    static byte[] Build(Random rand, WriteValue write)
    {
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            for (int o = 0; o < ObjectCount; o++)
            {
                buffer.WriteArrayHeader(3);
                for (int i = 0; i < 3; i++)
                {
                    if (rand.Next(5) == 0)
                    {
                        buffer.WriteNil();
                    }
                    else
                    {
                        write(ref buffer, rand);
                    }
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    [BenchmarkCategory("Int64"), Benchmark(Baseline = true)]
    public long Int64_Pair()
    {
        var buffer = new ReadOnlySpanReadBuffer(longBlob);
        var value = longs;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.TryReadNil() ? default(long?) : buffer.ReadInt64(); break;
                    case 1: value.B = buffer.TryReadNil() ? default(long?) : buffer.ReadInt64(); break;
                    case 2: value.C = buffer.TryReadNil() ? default(long?) : buffer.ReadInt64(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("Int64"), Benchmark]
    public long Int64_Reader()
    {
        var buffer = new ReadOnlySpanReadBuffer(longBlob);
        var value = longs;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableInt64(); break;
                    case 1: value.B = buffer.ReadNullableInt64(); break;
                    case 2: value.C = buffer.ReadNullableInt64(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("UInt16"), Benchmark(Baseline = true)]
    public long UInt16_Pair()
    {
        var buffer = new ReadOnlySpanReadBuffer(ushortBlob);
        var value = ushorts;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.TryReadNil() ? default(ushort?) : buffer.ReadUInt16(); break;
                    case 1: value.B = buffer.TryReadNil() ? default(ushort?) : buffer.ReadUInt16(); break;
                    case 2: value.C = buffer.TryReadNil() ? default(ushort?) : buffer.ReadUInt16(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("UInt16"), Benchmark]
    public long UInt16_Reader()
    {
        var buffer = new ReadOnlySpanReadBuffer(ushortBlob);
        var value = ushorts;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableUInt16(); break;
                    case 1: value.B = buffer.ReadNullableUInt16(); break;
                    case 2: value.C = buffer.ReadNullableUInt16(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("Byte"), Benchmark(Baseline = true)]
    public long Byte_Pair()
    {
        var buffer = new ReadOnlySpanReadBuffer(byteBlob);
        var value = bytes;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.TryReadNil() ? default(byte?) : buffer.ReadByte(); break;
                    case 1: value.B = buffer.TryReadNil() ? default(byte?) : buffer.ReadByte(); break;
                    case 2: value.C = buffer.TryReadNil() ? default(byte?) : buffer.ReadByte(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("Byte"), Benchmark]
    public long Byte_Reader()
    {
        var buffer = new ReadOnlySpanReadBuffer(byteBlob);
        var value = bytes;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableByte(); break;
                    case 1: value.B = buffer.ReadNullableByte(); break;
                    case 2: value.C = buffer.ReadNullableByte(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("Double"), Benchmark(Baseline = true)]
    public long Double_Pair()
    {
        var buffer = new ReadOnlySpanReadBuffer(doubleBlob);
        var value = doubles;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.TryReadNil() ? default(double?) : buffer.ReadDouble(); break;
                    case 1: value.B = buffer.TryReadNil() ? default(double?) : buffer.ReadDouble(); break;
                    case 2: value.C = buffer.TryReadNil() ? default(double?) : buffer.ReadDouble(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [BenchmarkCategory("Double"), Benchmark]
    public long Double_Reader()
    {
        var buffer = new ReadOnlySpanReadBuffer(doubleBlob);
        var value = doubles;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableDouble(); break;
                    case 1: value.B = buffer.ReadNullableDouble(); break;
                    case 2: value.C = buffer.ReadNullableDouble(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }
}

public sealed class NullableLong3
{
    public long? A;
    public long? B;
    public long? C;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => A.GetValueOrDefault() + B.GetValueOrDefault() + C.GetValueOrDefault()
        + (A.HasValue ? 1 : 0) + (B.HasValue ? 1 : 0) + (C.HasValue ? 1 : 0);
}

public sealed class NullableUInt16x3
{
    public ushort? A;
    public ushort? B;
    public ushort? C;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => (long)A.GetValueOrDefault() + B.GetValueOrDefault() + C.GetValueOrDefault()
        + (A.HasValue ? 1 : 0) + (B.HasValue ? 1 : 0) + (C.HasValue ? 1 : 0);
}

public sealed class NullableByte3
{
    public byte? A;
    public byte? B;
    public byte? C;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => (long)A.GetValueOrDefault() + B.GetValueOrDefault() + C.GetValueOrDefault()
        + (A.HasValue ? 1 : 0) + (B.HasValue ? 1 : 0) + (C.HasValue ? 1 : 0);
}

public sealed class NullableDouble3
{
    public double? A;
    public double? B;
    public double? C;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => BitConverter.DoubleToInt64Bits(A.GetValueOrDefault()) + BitConverter.DoubleToInt64Bits(B.GetValueOrDefault()) + BitConverter.DoubleToInt64Bits(C.GetValueOrDefault())
        + (A.HasValue ? 1 : 0) + (B.HasValue ? 1 : 0) + (C.HasValue ? 1 : 0);
}

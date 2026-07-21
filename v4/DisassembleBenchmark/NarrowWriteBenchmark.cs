using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using UltraMessagePack;

// Narrow integer writers: byte routed through UnsafeWriteUInt32 pays the branchless core
// (lzcnt + format-table load + 4-byte wide store) to compute a statically known answer
// (always uint8/len=2 in the wide half) and inherits the 5-byte destination contract.
// Dedicated candidates shrink the contract to the true output max (2 bytes for byte,
// 3 for ushort) and drop the table walk. The Ternary candidates were an attempt at a
// branchless form, but RyuJIT does not if-convert ternaries inside loops (no cmov),
// so the header pick stays a real branch — they lost everywhere and are kept as a record.
public class NarrowWriteByteBenchmark
{
    // large enough that the branch predictor cannot memorize the repeating sequence
    const int Count = 100_000;

    byte[] values = default!;
    byte[] buffer = default!;

    // Small: all fixint (predictable) / Large: all uint8 form (predictable)
    // Mixed: uniform 0..255, 50/50 class choice (worst case for the fixint branch)
    [Params("Small", "Large", "Mixed")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new byte[Count];
        buffer = new byte[Count * 5];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Small" => (byte)rand.Next(0, 128),
                "Large" => (byte)rand.Next(128, 256),
                _ => (byte)rand.Next(0, 256),
            };
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int ViaUInt32()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += UltraMessagePack.MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DedicatedHybrid()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteByteHybrid(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DedicatedTernary()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteByteTernary(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    // 2-byte contract, same branch profile as the current fixint fast path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteByteHybrid(ref byte destination, byte value)
    {
        if (value <= 0x7f)
        {
            destination = value;
            return 1;
        }
        destination = MessagePackCode.UInt8;
        Unsafe.Add(ref destination, 1) = value;
        return 2;
    }

    // 2-byte contract, single-exit form: intended as branchless (cmov header +
    // unconditional payload store), but the JIT compiles the ternary to a branch
    // inside loops, so it keeps the Hybrid's branch profile with more instructions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteByteTernary(ref byte destination, byte value)
    {
        int wide = value >> 7; // 0 or 1
        destination = wide != 0 ? MessagePackCode.UInt8 : value;
        Unsafe.Add(ref destination, 1) = value;
        return 1 + wide;
    }
}

public class NarrowWriteUInt16Benchmark
{
    const int Count = 100_000;

    ushort[] values = default!;
    byte[] buffer = default!;

    // Small: all fixint / Large: all uint16 form (both predictable)
    // Mixed: format class chosen at random per element (fixint / uint8 / uint16)
    [Params("Small", "Large", "Mixed")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new ushort[Count];
        buffer = new byte[Count * 5];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Small" => (ushort)rand.Next(0, 128),
                "Large" => (ushort)rand.Next(256, 65536),
                _ => rand.Next(3) switch
                {
                    0 => (ushort)rand.Next(0, 128),
                    1 => (ushort)rand.Next(128, 256),
                    _ => (ushort)rand.Next(256, 65536),
                },
            };
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int ViaUInt32()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += UltraMessagePack.MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DedicatedHybrid()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteUInt16Hybrid(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DedicatedTernary()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteUInt16Ternary(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    // 3-byte contract: hybrid fixint front, then a setcc-selected uint8/uint16 form with
    // one unconditional 2-byte big-endian store (mini WriteHeaderCore without the table)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteUInt16Hybrid(ref byte destination, ushort value)
    {
        if (value <= 0x7f)
        {
            destination = (byte)value;
            return 1;
        }
        int wide = value > 0xff ? 1 : 0;
        destination = (byte)(MessagePackCode.UInt8 + wide); // 0xcc or 0xcd
        // wide=0: value << 8 -> BE bytes [value, scratch]; wide=1: [hi, lo]
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(value << ((1 - wide) * 8))));
        return 2 + wide;
    }

    // 3-byte contract, single-exit form: intended as branchless (setcc len + cmov header,
    // 0xca + len gives 0xcc/0xcd; len=1 shifts the store to 0 = scratch), but the JIT
    // branches on the ternaries inside loops and the longer dependency chain loses badly
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteUInt16Ternary(ref byte destination, ushort value)
    {
        int len = 1 + (value > 0x7f ? 1 : 0) + (value > 0xff ? 1 : 0);
        destination = (byte)(len == 1 ? value : (uint)(0xca + len));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(value << ((3 - len) * 8))));
        return len;
    }
}

public class NarrowWriteSByteBenchmark
{
    const int Count = 100_000;

    sbyte[] values = default!;
    byte[] buffer = default!;

    // Small: all fixint (-32..127) / Large: all int8 form (-128..-33)
    // Mixed: uniform full range (fixint with p=160/256)
    [Params("Small", "Large", "Mixed")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new sbyte[Count];
        buffer = new byte[Count * 5];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Small" => (sbyte)rand.Next(-32, 128),
                "Large" => (sbyte)rand.Next(-128, -32),
                _ => (sbyte)rand.Next(-128, 128),
            };
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int ViaInt32()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DedicatedHybrid()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteSByteHybrid(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    // 2-byte contract: fixint covers -32..127, everything else (-128..-33) is int8
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteSByteHybrid(ref byte destination, sbyte value)
    {
        if ((uint)(value + 32) <= 159)
        {
            destination = unchecked((byte)value);
            return 1;
        }
        destination = MessagePackCode.Int8;
        Unsafe.Add(ref destination, 1) = unchecked((byte)value);
        return 2;
    }
}

public class NarrowWriteInt16Benchmark
{
    const int Count = 100_000;

    short[] values = default!;
    byte[] buffer = default!;

    // Small: all fixint / Large: all 3-byte forms, both signs (stresses the sign branch)
    // Mixed: one of the six format classes at random per element
    [Params("Small", "Large", "Mixed")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new short[Count];
        buffer = new byte[Count * 5];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Small" => (short)rand.Next(-32, 128),
                "Large" => rand.Next(2) == 0 ? (short)rand.Next(256, 32768) : (short)rand.Next(-32768, -128),
                _ => rand.Next(6) switch
                {
                    0 => (short)rand.Next(0, 128),
                    1 => (short)rand.Next(-32, 0),
                    2 => (short)rand.Next(128, 256),
                    3 => (short)rand.Next(-128, -32),
                    4 => (short)rand.Next(256, 32768),
                    _ => (short)rand.Next(-32768, -128),
                },
            };
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int ViaInt32()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int TableMini()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteInt16TableMini(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int HybridSignSplit()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteInt16HybridSignSplit(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    const uint WU8 = 2 | ((uint)MessagePackCode.UInt8 << 8);
    const uint WU16 = 3 | ((uint)MessagePackCode.UInt16 << 8);
    const uint WI8 = 2 | ((uint)MessagePackCode.Int8 << 8);
    const uint WI16 = 3 | ((uint)MessagePackCode.Int16 << 8);

    // Int32Formats truncated to the short-reachable rows: positive bits <= 15 (uint8/uint16),
    // negative bits(~value) <= 15 (int8/int16); the len-5 rows can never be indexed, but the
    // index layout ((value >>> 26) & 32 | bits) is kept identical to the proven int32 core
    static ReadOnlySpan<uint> Int16Formats =>
    [
        WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8,
        WU8,
        WU16, WU16, WU16, WU16, WU16, WU16, WU16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        WI8, WI8, WI8, WI8, WI8, WI8, WI8, WI8,
        WI16, WI16, WI16, WI16, WI16, WI16, WI16, WI16,
    ];

    // 3-byte contract, same shape as the proven int32 hybrid (fixint front + branchless
    // table core), but the unconditional payload store narrows to 2 bytes
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteInt16TableMini(ref byte destination, short value)
    {
        int v = value;
        if ((uint)(v + 32) <= 159)
        {
            destination = unchecked((byte)v);
            return 1;
        }
        int x = v ^ (v >> 31);
        int bits = 32 - System.Numerics.BitOperations.LeadingZeroCount((uint)x); // 1..15
        int idx = ((v >>> 26) & 32) | bits;
        uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int16Formats), idx);
        int len = (int)(e & 0xff);
        destination = (byte)(e >> 8);
        // len in {2,3}: shift in {8,0}, big-endian payload right after the header
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(v << ((3 - len) * 8))));
        return len;
    }

    // 3-byte contract: fixint front, then a sign branch picking uint8/uint16 vs int8/int16,
    // each side a setcc wide-pick + one unconditional 2-byte store
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteInt16HybridSignSplit(ref byte destination, short value)
    {
        int v = value;
        if ((uint)(v + 32) <= 159)
        {
            destination = unchecked((byte)v);
            return 1;
        }
        int wide;
        if (v >= 0)
        {
            wide = v > 0xff ? 1 : 0;
            destination = (byte)(MessagePackCode.UInt8 + wide); // 0xcc or 0xcd
        }
        else
        {
            wide = v < -128 ? 1 : 0;
            destination = (byte)(MessagePackCode.Int8 + wide); // 0xd0 or 0xd1
        }
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(v << ((1 - wide) * 8))));
        return 2 + wide;
    }
}

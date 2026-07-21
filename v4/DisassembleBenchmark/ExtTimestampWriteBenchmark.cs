using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using UltraMessagePack;

// Ext header / timestamp writers: the current library implementations are compare cascades
// (fixext pow2 test -> ext8 -> ext16 -> ext32, and ts32 -> ts64 -> ts96), so the format
// class of each value is a data-dependent branch chain. Candidates apply the proven
// integer-writer recipe: derive a format index without compare-branches, load {len, header}
// from a table, then unconditional stores with scratch confined to the documented
// worst-case window (6 bytes for ext headers, 15 for timestamps).
public class ExtHeaderWriteBenchmark
{
    // large enough that the branch predictor cannot memorize the repeating sequence
    const int Count = 100_000;

    int[] lengths = default!;
    byte[] buffer = default!;

    // Fix: dataLength in {1,2,4,8,16} (fixext, predictable class) / Ext8: 17..255
    // (predictable class) / Mixed: one of the four format classes at random per element
    [Params("Fix", "Ext8", "Mixed")]
    public string Distribution = "Mixed";

    static readonly int[] FixLengths = [1, 2, 4, 8, 16];

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        lengths = new int[Count];
        buffer = new byte[Count * 6];
        for (int i = 0; i < Count; i++)
        {
            lengths[i] = Distribution switch
            {
                "Fix" => FixLengths[rand.Next(5)],
                "Ext8" => rand.Next(17, 256),
                _ => rand.Next(4) switch
                {
                    0 => FixLengths[rand.Next(5)],
                    1 => rand.Next(17, 256),
                    2 => rand.Next(256, 65536),
                    _ => rand.Next(65536, int.MaxValue),
                },
            };
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Cascade()
    {
        var lens = lengths;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < lens.Length; i++)
        {
            pos += WriteExtHeaderCascade(ref Unsafe.Add(ref d, pos), 42, lens[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Hybrid()
    {
        var lens = lengths;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < lens.Length; i++)
        {
            pos += WriteExtHeaderHybrid(ref Unsafe.Add(ref d, pos), 42, lens[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless()
    {
        var lens = lengths;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < lens.Length; i++)
        {
            pos += WriteExtHeaderBranchless(ref Unsafe.Add(ref d, pos), 42, lens[i]);
        }
        return pos;
    }

    // current library implementation: three-deep compare cascade after the fixext test
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteExtHeaderCascade(ref byte destination, sbyte typeCode, int dataLength)
    {
        if (BitOperations.IsPow2(dataLength) && (uint)dataLength <= 16)
        {
            destination = (byte)(MessagePackCode.FixExt1 + BitOperations.Log2((uint)dataLength));
            Unsafe.Add(ref destination, 1) = unchecked((byte)typeCode);
            return 2;
        }
        if ((uint)dataLength <= byte.MaxValue)
        {
            destination = MessagePackCode.Ext8;
            Unsafe.Add(ref destination, 1) = (byte)dataLength;
            Unsafe.Add(ref destination, 2) = unchecked((byte)typeCode);
            return 3;
        }
        if ((uint)dataLength <= ushort.MaxValue)
        {
            destination = MessagePackCode.Ext16;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)dataLength));
            Unsafe.Add(ref destination, 3) = unchecked((byte)typeCode);
            return 4;
        }
        destination = MessagePackCode.Ext32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)dataLength));
        Unsafe.Add(ref destination, 5) = unchecked((byte)typeCode);
        return 6;
    }

    const uint E8 = 3 | ((uint)MessagePackCode.Ext8 << 8);
    const uint E16 = 4 | ((uint)MessagePackCode.Ext16 << 8);
    const uint E32 = 6 | ((uint)MessagePackCode.Ext32 << 8);

    // idx 0..31 = significant bits of dataLength (non-fix forms), idx 32..37 = fix flag set:
    // 32 is dataLength == 0 (passes the pow2-or-zero test, must stay ext8), 33..37 are
    // fixext1/2/4/8/16 (bits = log2 + 1, and FixExt codes are consecutive)
    static ReadOnlySpan<uint> ExtHeaderFormats =>
    [
        // bits 0..8 -> ext8
        E8, E8, E8, E8, E8, E8, E8, E8, E8,
        // bits 9..16 -> ext16
        E16, E16, E16, E16, E16, E16, E16, E16,
        // bits 17..31 -> ext32
        E32, E32, E32, E32, E32, E32, E32, E32, E32, E32, E32, E32, E32, E32, E32,
        // idx 32: dataLength == 0 with the fix flag -> ext8
        E8,
        // idx 33..37: fixext1/2/4/8/16
        2 | ((uint)MessagePackCode.FixExt1 << 8), 2 | ((uint)MessagePackCode.FixExt2 << 8),
        2 | ((uint)MessagePackCode.FixExt4 << 8), 2 | ((uint)MessagePackCode.FixExt8 << 8),
        2 | ((uint)MessagePackCode.FixExt16 << 8),
    ];

    // fixext branch kept (exact-size ext payloads are the common case: timestamps, GUIDs),
    // ext8/16/32 tail flattened to the branchless table core
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteExtHeaderHybrid(ref byte destination, sbyte typeCode, int dataLength)
    {
        if (BitOperations.IsPow2(dataLength) && (uint)dataLength <= 16)
        {
            destination = (byte)(MessagePackCode.FixExt1 + BitOperations.Log2((uint)dataLength));
            Unsafe.Add(ref destination, 1) = unchecked((byte)typeCode);
            return 2;
        }
        int bits = 32 - BitOperations.LeadingZeroCount((uint)dataLength); // 0..31 (dataLength >= 0)
        uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ExtHeaderFormats), bits);
        int len = (int)(e & 0xff);
        destination = (byte)(e >> 8);
        // len in {3,4,6}: shift in {24,16,0} puts the big-endian length right after the header
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)dataLength << ((6 - len) * 8)));
        Unsafe.Add(ref destination, len - 1) = unchecked((byte)typeCode);
        return len;
    }

    // fully flat: the fixext class test (pow2 and < 32) is computed as an arithmetic flag
    // and folded into the table index, so every format class runs the same instruction path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteExtHeaderBranchless(ref byte destination, sbyte typeCode, int dataLength)
    {
        // fixMask == 0 iff dataLength in {0, 1, 2, 4, 8, 16}: pow2-or-zero AND below 32
        int fixMask = (dataLength & (dataLength - 1)) | (dataLength & ~31);
        int isFix = (int)((uint)(fixMask - 1) >> 31); // 1 when fixMask == 0
        int bits = 32 - BitOperations.LeadingZeroCount((uint)dataLength); // 0..31
        int idx = (isFix << 5) | bits;
        uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ExtHeaderFormats), idx);
        int len = (int)(e & 0xff);
        destination = (byte)(e >> 8);
        // len in {3,4,6}: shift in {24,16,0} puts the big-endian length right after the
        // header; len == 2 (fixext) computes shift 32 -> masked to 0 by C#, but all four
        // stored bytes are scratch there (typeCode overwrites offset 1 just below)
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)dataLength << ((6 - len) * 8)));
        Unsafe.Add(ref destination, len - 1) = unchecked((byte)typeCode);
        return len;
    }
}

public class TimestampWriteBenchmark
{
    const int Count = 100_000;

    DateTime[] values = default!;
    byte[] buffer = default!;

    // Ts64: UtcNow-like, sub-second precision (predictable class, the real-world common case)
    // Mixed3264: whole-second u32 timestamps and ts64 at random (the cascade's inner branch)
    // MixedAll: all three classes at random, including pre-1970 / post-2514 (ts96)
    [Params("Ts64", "Mixed3264", "MixedAll")]
    public string Distribution = "MixedAll";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new DateTime[Count];
        buffer = new byte[Count * 15];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Ts64" => FromUnix(rand.NextInt64(0, 1L << 34), rand.Next(1, 10_000_000)),
                "Mixed3264" => rand.Next(2) == 0
                    ? FromUnix(rand.NextInt64(0, 1L << 32), 0)
                    : FromUnix(rand.NextInt64(0, 1L << 34), rand.Next(1, 10_000_000)),
                _ => rand.Next(3) switch
                {
                    0 => FromUnix(rand.NextInt64(0, 1L << 32), 0),
                    1 => FromUnix(rand.NextInt64(0, 1L << 34), rand.Next(1, 10_000_000)),
                    _ => rand.Next(2) == 0
                        ? FromUnix(rand.NextInt64(-62135596800, 0), rand.Next(0, 10_000_000))
                        : FromUnix(rand.NextInt64(1L << 34, 250_000_000_000), rand.Next(0, 10_000_000)),
                },
            };
        }
    }

    // seconds relative to the unix epoch + sub-second tick fraction, Kind.Utc
    static DateTime FromUnix(long seconds, int tickFraction) =>
        new((62135596800 + seconds) * TimeSpan.TicksPerSecond + tickFraction, DateTimeKind.Utc);

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Cascade()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteTimestampCascade(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Hybrid96()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteTimestampHybrid96(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        int pos = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            pos += WriteTimestampBranchless(ref Unsafe.Add(ref d, pos), vals[i]);
        }
        return pos;
    }

    const long BclSecondsAtUnixEpoch = 62135596800; // DateTime.UnixEpoch.Ticks / TicksPerSecond
    const int NanosecondsPerTick = 100;

    // current library implementation: ts32 -> ts64 -> ts96 compare cascade
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteTimestampCascade(ref byte destination, DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        long seconds = value.Ticks / TimeSpan.TicksPerSecond - BclSecondsAtUnixEpoch;
        long nanoseconds = value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick;

        if (seconds >> 34 == 0)
        {
            // data64: [nanoseconds in 30-bit | seconds in 34-bit]
            ulong data64 = unchecked((ulong)(nanoseconds << 34 | seconds));
            if ((data64 & 0xffffffff00000000UL) == 0)
            {
                destination = MessagePackCode.FixExt4;
                Unsafe.Add(ref destination, 1) = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), BinaryPrimitives.ReverseEndianness((uint)data64));
                return 6;
            }
            destination = MessagePackCode.FixExt8;
            Unsafe.Add(ref destination, 1) = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), BinaryPrimitives.ReverseEndianness(data64));
            return 10;
        }

        // data96: [nanoseconds in 32-bit unsigned | seconds in 64-bit signed]
        destination = MessagePackCode.Ext8;
        Unsafe.Add(ref destination, 1) = 12;
        Unsafe.Add(ref destination, 2) = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 3), BinaryPrimitives.ReverseEndianness((uint)nanoseconds));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 7), BinaryPrimitives.ReverseEndianness(unchecked((ulong)seconds)));
        return 15;
    }

    // ts32/ts64 collapse to one flat path (both are fixext with the same layout; d6/d7
    // differ by 1 and the ts32 payload is data64's low u32 lifted to the high half for
    // one unconditional 8-byte store). Only ts96 — genuinely rare in real data — branches.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteTimestampHybrid96(ref byte destination, DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        long seconds = value.Ticks / TimeSpan.TicksPerSecond - BclSecondsAtUnixEpoch;
        long nanoseconds = value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick;

        if ((ulong)seconds >> 34 == 0)
        {
            ulong data64 = (ulong)(nanoseconds << 34) | (ulong)seconds;
            ulong hi = data64 >> 32;
            int wide = (int)((hi | (0UL - hi)) >> 63); // 1 -> ts64 (data64 needs more than 4 bytes)
            // one 2-byte store: [0xd6 + wide, 0xff] (no carry into the type byte)
            Unsafe.WriteUnaligned(ref destination, (ushort)(0xffd6 + (uint)wide));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), BinaryPrimitives.ReverseEndianness(data64 << ((1 - wide) << 5)));
            return 6 + (wide << 2);
        }

        destination = MessagePackCode.Ext8;
        Unsafe.Add(ref destination, 1) = 12;
        Unsafe.Add(ref destination, 2) = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 3), BinaryPrimitives.ReverseEndianness((uint)nanoseconds));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 7), BinaryPrimitives.ReverseEndianness(unchecked((ulong)seconds)));
        return 15;
    }

    // first 4 bytes of each format, little-endian packed: ts32 [d6 ff . .], ts64 [d7 ff . .],
    // ts96 [c7 0c ff .] (trailing bytes are scratch, overwritten by the payload stores)
    static ReadOnlySpan<uint> TimestampHeaderWords => [0x0000ffd6u, 0x0000ffd7u, 0x00ff0cc7u];

    // fully flat (except the unavoidable Kind == Local conversion): format index f in
    // {0 = ts32, 1 = ts64, 2 = ts96} from two zero-tests, then header word + nanoseconds +
    // payload stores at f-derived offsets. Scratch stays within the 15-byte window.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteTimestampBranchless(ref byte destination, DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        long seconds = value.Ticks / TimeSpan.TicksPerSecond - BclSecondsAtUnixEpoch;
        long nanoseconds = value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick;

        // data64: [nanoseconds in 30-bit | seconds in 34-bit] (garbage when ts96, masked out below)
        ulong data64 = unchecked((ulong)(nanoseconds << 34 | seconds));
        ulong big = (ulong)seconds >> 34; // nonzero iff seconds is negative or >= 2^34 (ts96)
        ulong hi = data64 >> 32;          // nonzero iff data64 needs more than 4 bytes (ts64)
        int f2 = (int)((big | (0UL - big)) >> 63);
        int f1 = (int)((hi | (0UL - hi)) >> 63);
        int f = (f2 << 1) | (f1 & (f2 ^ 1)); // 0, 1, 2

        int ts96 = f >> 1;                     // 1 only for ts96
        ulong mask96 = (ulong)-(long)ts96;     // all ones for ts96
        // ts96 stores raw seconds at offset 7; ts32/64 store data64 at offset 2 (ts32's
        // u32 payload lifted to the high half by shift 32)
        ulong payload = (data64 ^ ((data64 ^ (ulong)seconds) & mask96)) << (((2 - f) >> 1) << 5);

        uint headerWord = Unsafe.Add(ref MemoryMarshal.GetReference(TimestampHeaderWords), f);
        Unsafe.WriteUnaligned(ref destination, headerWord); // bytes 0..3 (2..3 scratch except ts96's 0xff)
        // ts96: real nanoseconds at 3..6; ts32/64: scratch at 2..5, fully overwritten below
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2 + ts96), BinaryPrimitives.ReverseEndianness((uint)nanoseconds));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2 + 5 * ts96), BinaryPrimitives.ReverseEndianness(payload));
        return (0x0F0A06 >> (f << 3)) & 0xFF; // 6 / 10 / 15
    }
}

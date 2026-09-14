using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using MessagePack;

// TokenScan round 5: vector bulk-skip for the async pass-1 boundary scanner.
// Rounds 1-4 settled the per-token decode shape (FullyInlined TryReadToken); this round
// asks whether runs of STANDALONE tokens can be skipped without decoding at all.
// A positive/negative fixint is a complete value in one byte (no payload, no children),
// and the whole fixint set is exactly `(sbyte)b >= -32`: one signed SIMD compare
// classifies 32 token positions at once. Induction makes the bulk skip sound: position
// 0 is a token boundary, a fixint there consumes exactly one byte, so position 1 is a
// token boundary too - an all-fixint chunk is 32 whole values.
// Gate: remaining >= 32, so the skip can never run past the end of the message
// (small-object streams keep remaining at 1-3 and never even attempt the load).
// Candidates:
//   Scalar     - adopted round-4 shape (MessagePack.MessagePackPrimitives.TryReadToken), baseline
//   VecAll     - all-32-fixint test each token position while the gate is open;
//                measures the naive tax on gate-open-but-never-fixint payloads
//   VecAllHyst - same, but a failed test defers the next attempt until the scalar
//                loop has consumed past the failing 32-byte window
//   VecRunHyst - ExtractMostSignificantBits + tzcnt skips the LEADING fixint run of
//                the chunk (handles interleaved data, e.g. fixint-heavy maps); short
//                runs (< 8) also defer, so pure non-fixint payloads pay ~1 vector
//                test per 32 bytes
// Payloads (100k+ tokens, random content per the predictor pitfall):
//   IntDense - array32 of 100k fixints: the win case
//   U16Dense - array32 of 100k uint16: gate open, every test fails (worst-case tax)
//   StrDense - array32 of 100k fixstr(0-31): gate open, fails at every element boundary
//   Poco     - fixarray(2)+fixint+fixstr stream: gate closed, must tie Scalar exactly
//   Mixed    - the TokenScanBenchmark token soup: top-level values, gate mostly closed
//
// Round 1 verdict: VecAll/VecAllHyst 0.03x IntDense, but hysteresis's index-vs-deadline
// compare is an UNPREDICTABLE branch on random-length payloads (StrDense 1.71x vs
// VecAll's 1.14x tax) and VecRunHyst's movemask+tzcnt chain is 6x VecAll on the pure
// win case. Round 2 (VecAfterFixint): entering only on scalar evidence fixes the dense
// non-fixint payloads (U16 1.03) but the code-byte re-load sits outside the
// remaining>=32 gate and taxes object streams (Poco 1.19). Round 3 verdict — VecGated
// ADOPTED into MessagePackBoundaryScanner: remaining >= 32 leads the entry condition,
// standalone-ness derives from TryReadToken's register outputs, no re-load.
// 0.02 IntDense (52x) / 1.04 U16 / 0.98 Str / 0.96 Poco; the ShortRun Mixed 1.46 was
// noise — MediumRun says 0.96 +-0.08 (the gate structurally never opens there).
public class TokenScanVectorBenchmark
{
    [Params("IntDense", "U16Dense", "StrDense", "Poco", "Mixed")]
    public string Payload = "IntDense";

    byte[] data = null!;
    long expectedCount;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var bytes = new List<byte>(4 * 1024 * 1024);
        switch (Payload)
        {
            case "IntDense":
                expectedCount = 1;
                bytes.Add(0xdd); bytes.Add(0); bytes.Add(1); bytes.Add(0x86); bytes.Add(0xa0); // array32(100_000)
                for (var i = 0; i < 100_000; i++)
                {
                    bytes.Add(unchecked((byte)(sbyte)random.Next(-32, 128)));
                }
                break;
            case "U16Dense":
                expectedCount = 1;
                bytes.Add(0xdd); bytes.Add(0); bytes.Add(1); bytes.Add(0x86); bytes.Add(0xa0);
                for (var i = 0; i < 100_000; i++)
                {
                    bytes.Add(0xcd); bytes.Add((byte)random.Next(256)); bytes.Add((byte)random.Next(256));
                }
                break;
            case "StrDense":
                expectedCount = 1;
                bytes.Add(0xdd); bytes.Add(0); bytes.Add(1); bytes.Add(0x86); bytes.Add(0xa0);
                for (var i = 0; i < 100_000; i++)
                {
                    var length = random.Next(0, 32);
                    bytes.Add((byte)(0xa0 | length));
                    for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                }
                break;
            case "Poco":
                expectedCount = 100_000;
                for (var i = 0; i < expectedCount; i++)
                {
                    bytes.Add(0x92);
                    bytes.Add(unchecked((byte)(sbyte)random.Next(-32, 128)));
                    var length = random.Next(0, 32);
                    bytes.Add((byte)(0xa0 | length));
                    for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                }
                break;
            case "Mixed":
                expectedCount = 200_000;
                for (var i = 0; i < expectedCount; i++)
                {
                    switch (random.Next(14))
                    {
                        case 0:
                        case 1:
                            bytes.Add(unchecked((byte)(sbyte)random.Next(-32, 128)));
                            break;
                        case 2:
                            bytes.Add(0xc0);
                            break;
                        case 3:
                            bytes.Add(random.Next(2) == 0 ? (byte)0xc2 : (byte)0xc3);
                            break;
                        case 4:
                            bytes.Add(0xcc); bytes.Add((byte)random.Next(256));
                            break;
                        case 5:
                            bytes.Add(0xcd); bytes.Add(0); bytes.Add(1);
                            break;
                        case 6:
                            bytes.Add(0xce); for (var j = 0; j < 4; j++) bytes.Add(1);
                            break;
                        case 7:
                            bytes.Add(0xcb); for (var j = 0; j < 8; j++) bytes.Add(1);
                            break;
                        case 8:
                            {
                                var length = random.Next(0, 32);
                                bytes.Add((byte)(0xa0 | length));
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 9:
                            {
                                var length = random.Next(32, 200);
                                bytes.Add(0xd9); bytes.Add((byte)length);
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 10:
                            {
                                var length = random.Next(256, 1000);
                                bytes.Add(0xda); bytes.Add((byte)(length >> 8)); bytes.Add((byte)length);
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 11:
                            {
                                var count = random.Next(0, 4);
                                bytes.Add((byte)(0x80 | count));
                                for (var j = 0; j < 2 * count; j++) bytes.Add((byte)random.Next(0, 128));
                                break;
                            }
                        case 12:
                            {
                                var count = random.Next(0, 8);
                                bytes.Add((byte)(0x90 | count));
                                for (var j = 0; j < count; j++) bytes.Add((byte)random.Next(0, 128));
                                break;
                            }
                        default:
                            bytes.Add(0xd6); bytes.Add(0xff); for (var j = 0; j < 4; j++) bytes.Add(1);
                            break;
                    }
                }
                break;
        }
        data = bytes.ToArray();

        Check(nameof(Scalar), Scalar());
        Check(nameof(VecAll), VecAll());
        Check(nameof(VecAllHyst), VecAllHyst());
        Check(nameof(VecRunHyst), VecRunHyst());
        Check(nameof(VecAfterFixint), VecAfterFixint());
        Check(nameof(VecGated), VecGated());

        void Check(string name, long count)
        {
            if (count != expectedCount)
            {
                throw new InvalidOperationException($"{name} counted {count} values, expected {expectedCount}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowUnexpected() => throw new InvalidOperationException("unexpected token result");

    [Benchmark(Baseline = true)]
    public long Scalar()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    [Benchmark]
    public long VecAll()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                if (remaining >= 32 && span.Length - index >= 32)
                {
                    var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                    if (Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                    {
                        index += 32;
                        remaining -= 32;
                        continue;
                    }
                }
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    [Benchmark]
    public long VecAllHyst()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        var nextAttempt = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                if (remaining >= 32 && span.Length - index >= 32 && index >= nextAttempt)
                {
                    var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                    if (Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                    {
                        index += 32;
                        remaining -= 32;
                        continue;
                    }
                    nextAttempt = index + 32;
                }
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    [Benchmark]
    public long VecRunHyst()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        var nextAttempt = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                if (remaining >= 32 && span.Length - index >= 32 && index >= nextAttempt)
                {
                    var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                    var mask = Vector256.GreaterThan(v, Vector256.Create((sbyte)-33)).ExtractMostSignificantBits();
                    var run = BitOperations.TrailingZeroCount(~mask); // 32 when the whole chunk is fixints
                    if (run > 0)
                    {
                        index += run;
                        remaining -= run;
                        if (run >= 8) continue;
                    }
                    nextAttempt = index + 32;
                }
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    // Round 2: enter the bulk-skip lane only on scalar evidence - the token just decoded
    // was itself a fixint. Homogeneous non-fixint payloads (StrDense/U16Dense) then never
    // attempt a vector test and their entry branch is perfectly predicted, while IntDense
    // pays one scalar element before living in the 32-wide loop. This replaces the
    // round-1 hysteresis counter, whose index-vs-deadline compare was an unpredictable
    // branch on random-length payloads (StrDense 1.71x).
    [Benchmark]
    public long VecAfterFixint()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                var wasFixint = (sbyte)span[index] >= -32;
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
                if (wasFixint)
                {
                    while (remaining >= 32 && span.Length - index >= 32)
                    {
                        var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                        if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                        {
                            break;
                        }
                        index += 32;
                        remaining -= 32;
                    }
                }
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    // Round 3: both round-2 lessons combined. remaining >= 32 leads the entry condition
    // (one predictable register compare shuts the whole thing off for object streams -
    // round 2's VecAfterFixint put the standalone check outside it and paid 1.19x on
    // Poco), and standalone-ness derives from TryReadToken's outputs already in
    // registers (tokenSize == 1 with no payload and no children) instead of re-loading
    // the code byte. The vector loop itself re-verifies everything, so the entry test
    // is only a heuristic and nil/bool matching it is harmless.
    [Benchmark]
    public long VecGated()
    {
        var span = data.AsSpan();
        long count = 0;
        long remaining = 1;
        long pending = 0;
        var index = 0;
        while (index < span.Length)
        {
            if (pending > 0)
            {
                var take = (int)Math.Min(pending, span.Length - index);
                index += take;
                pending -= take;
            }
            else if (remaining == 0)
            {
                count++;
                remaining = 1;
            }
            else
            {
                var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
                if (remaining >= 32 && (payloadLength | childValueCount) == 0 && tokenSize == 1)
                {
                    while (remaining >= 32 && span.Length - index >= 32)
                    {
                        var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                        if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                        {
                            break;
                        }
                        index += 32;
                        remaining -= 32;
                    }
                }
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }
}

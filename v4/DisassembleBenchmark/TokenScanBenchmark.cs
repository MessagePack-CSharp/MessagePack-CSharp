using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using UltraMessagePack;

// TryReadToken candidates for the async pass-1 boundary scanner. The current shape
// inlines only the fixint fast path; every other token — including fixstr, fixmap,
// fixarray and the numeric family, which dominate real POCO streams — pays a
// NoInlining call into an if-chain + switch. Candidates:
//   Current         - copy of MessagePackPrimitives.TryReadToken (fixint inline, rest slow)
//   SingleByteTable - ushort[256] lookup covering every tokenSize==1 code (fixint,
//                     fixmap, fixarray, fixstr, nil, bool, all numerics); one load,
//                     one predictable branch, immediates unpacked by shifts
//   FullTable       - uint[256] lookup covering ALL codes: size + immediates + length
//                     field width; no per-token-type branches at all, only a
//                     width!=0 branch for the multi-byte headers
//   FixFamilyBranch - no table: arithmetic fast path widened to the whole fix family
//                     (fixint/fixmap/fixarray/fixstr), rest slow
//   BranchThenTable - round 2 hybrid: fixint keeps the branch-only constant path (no
//                     load on the dominant class), every OTHER single-byte token
//                     resolves through the table, multi-byte headers go slow
//   FixFamilyThenTable - round 2: full fix-family branches first, table only for the
//                     0xc0-0xdf single-byte codes (nil/bool/numerics)
//
// Round 1 measured: Current=1.0 | SingleByteTable 1.23/0.79/0.87 | FullTable
// 3.77/1.57/0.86 | FixFamilyBranch 1.10/0.67/1.01 (IntDense/Poco/Mixed). FullTable's
// IntDense 3.77x is the lesson: deriving tokenSize from a table load puts the L1
// latency into the loop-carried index chain, serializing every iteration. Branch-based
// constant-tokenSize paths win whenever the data has schema repetition (Poco 0.67);
// the table wins when token types are adversarially random (Mixed 0.86).
//
// SchemaSkip probe (type-knowledge ceiling): hard-coding the Poco shape as a
// straight-line skipper measures 0.34x vs Current on Poco — ~1.9x beyond the adopted
// generic fast path (0.63x same run). The remaining 2ns/message is the floor: every
// token header must still be read because msgpack lengths are data-dependent (schema
// cannot predict byte counts). Not pursued: a generated per-type skipper's acceptance
// set must exactly mirror the formatter's (nil, every int/str width, compat forms) or
// the stream frame desyncs, and the scan is the minor pass of the 2-pass design —
// see the DeserializeAsync completed-reader path, which already skips scanning
// entirely by letting the parse itself find the boundary.
//
// Round 4 verdict — FullyInlined ADOPTED (TryReadTokenSlow NoInlining ->
// AggressiveInlining): 0.69/0.80/0.55 (IntDense/Mixed/Poco), dominating every
// payload. The cold call was taxing the FAST path: out params passed by address to
// the residual call force payload/child/size into stack slots for the whole method
// (enregistration is all-or-nothing per local) — IntDense 0.69 vs Current 1.00 with
// an identical fixint code path is that stack round-trip alone. Asm-verified: no
// TryReadTokenSlow call remains in ScanTokens, outputs live in registers, movbe
// decodes the BE length fields in-loop.
//
// Round 3 verdict — FixIntStrThenTable adopted interim, superseded by round 4:
// 1.09/0.65/0.92 vs BranchThenTable's 0.99/0.73/0.94. Best on the realistic object
// stream, ties best on Mixed; the IntDense +9% costs 0.07ns/token where pass-2 parsing
// dominates end-to-end. A never-taken second compare-branch consistently costs ~10% on
// IntDense (FixFamily*/FixIntStr* all show it; layout, not prediction) — accepted.
// The driver loop mirrors MessagePackBoundaryScanner.ScanTokens' single-span core and
// counts top-level values. Payloads are 100k+ tokens with random content per
// CLAUDE.md's predictor pitfall (small repeated buffers get memorized):
//   Poco     - fixarray(2) + fixint + fixstr(random len): the AsyncPipeBenchmark shape
//   IntDense - one array32 of 100k fixints: the current fast path's best case
//   Mixed    - random token soup incl. str8/str16, ext, nested fixmap/fixarray,
//              numerics: unpredictable type branches, slow-path heavy
public class TokenScanBenchmark
{
    [Params("Poco", "IntDense", "Mixed")]
    public string Payload = "Poco";

    byte[] data = null!;
    long expectedCount;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var bytes = new List<byte>(4 * 1024 * 1024);
        switch (Payload)
        {
            case "Poco":
                expectedCount = 100_000;
                for (var i = 0; i < expectedCount; i++)
                {
                    bytes.Add(0x92); // fixarray(2)
                    bytes.Add(unchecked((byte)(sbyte)random.Next(-32, 128)));
                    var length = random.Next(0, 32);
                    bytes.Add((byte)(0xa0 | length));
                    for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                }
                break;
            case "IntDense":
                expectedCount = 1;
                bytes.Add(0xdd); // array32
                bytes.Add(0); bytes.Add(1); bytes.Add(0x86); bytes.Add(0xa0); // 100_000 big-endian
                for (var i = 0; i < 100_000; i++)
                {
                    bytes.Add(unchecked((byte)(sbyte)random.Next(-32, 128)));
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
                            bytes.Add(0xc0); // nil
                            break;
                        case 3:
                            bytes.Add(random.Next(2) == 0 ? (byte)0xc2 : (byte)0xc3);
                            break;
                        case 4:
                            bytes.Add(0xcc); bytes.Add((byte)random.Next(256)); // uint8
                            break;
                        case 5:
                            bytes.Add(0xcd); bytes.Add(0); bytes.Add(1); // uint16
                            break;
                        case 6:
                            bytes.Add(0xce); for (var j = 0; j < 4; j++) bytes.Add(1); // uint32
                            break;
                        case 7:
                            bytes.Add(0xcb); for (var j = 0; j < 8; j++) bytes.Add(1); // float64
                            break;
                        case 8:
                            {
                                var length = random.Next(0, 32);
                                bytes.Add((byte)(0xa0 | length)); // fixstr
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 9:
                            {
                                var length = random.Next(32, 200);
                                bytes.Add(0xd9); bytes.Add((byte)length); // str8
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 10:
                            {
                                var length = random.Next(256, 1000);
                                bytes.Add(0xda); bytes.Add((byte)(length >> 8)); bytes.Add((byte)length); // str16
                                for (var j = 0; j < length; j++) bytes.Add((byte)'a');
                                break;
                            }
                        case 11:
                            {
                                var count = random.Next(0, 4);
                                bytes.Add((byte)(0x80 | count)); // fixmap
                                for (var j = 0; j < 2 * count; j++) bytes.Add((byte)random.Next(0, 128));
                                break;
                            }
                        case 12:
                            {
                                var count = random.Next(0, 8);
                                bytes.Add((byte)(0x90 | count)); // fixarray
                                for (var j = 0; j < count; j++) bytes.Add((byte)random.Next(0, 128));
                                break;
                            }
                        default:
                            bytes.Add(0xd6); bytes.Add(0xff); for (var j = 0; j < 4; j++) bytes.Add(1); // fixext4
                            break;
                    }
                }
                break;
        }
        data = bytes.ToArray();

        // candidates must agree with each other and with the payload's construction
        Check(nameof(Current), Current());
        Check(nameof(SingleByteTable), SingleByteTable());
        Check(nameof(FullTable), FullTable());
        Check(nameof(FixFamilyBranch), FixFamilyBranch());
        Check(nameof(BranchThenTable), BranchThenTable());
        Check(nameof(FixFamilyThenTable), FixFamilyThenTable());
        Check(nameof(FixIntStrThenTable), FixIntStrThenTable());
        Check(nameof(SchemaSkip), SchemaSkip());
        Check(nameof(FullyInlined), FullyInlined());

        void Check(string name, long count)
        {
            if (count != expectedCount)
            {
                throw new InvalidOperationException($"{name} counted {count} values, expected {expectedCount}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public long Current()
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
                var result = TryReadTokenCurrent(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long SingleByteTable()
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
                var result = TryReadTokenSingleByteTable(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long FullTable()
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
                var result = TryReadTokenFullTable(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long FixFamilyBranch()
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
                var result = TryReadTokenFixFamilyBranch(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long BranchThenTable()
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
                var result = TryReadTokenBranchThenTable(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long FixFamilyThenTable()
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
                var result = TryReadTokenFixFamilyThenTable(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
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
    public long FixIntStrThenTable()
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
                var result = TryReadTokenFixIntStrThenTable(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result != DecodeResult.Success) ThrowUnexpected();
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    // Ceiling probe for schema-aware skipping: the Poco message shape
    // (fixarray(2)[int, str]) hard-coded as straight-line code — what a
    // source-generated per-type skipper could do at best. Each position checks its
    // expected token first (own branch-history slot), no remaining/pending
    // bookkeeping, payload skip folded into the same iteration. Accepts the full
    // int/str wire families so it is not cheating on acceptance, but nil/other
    // schemas would need the generic fallback a real generated skipper must carry.
    // Only the Poco row is meaningful; other payloads fall back to Current.
    [Benchmark]
    public long SchemaSkip()
    {
        if (Payload != "Poco") return Current();
        var span = data.AsSpan();
        long count = 0;
        var index = 0;
        while (index < span.Length)
        {
            // array header: expected fixarray(2)
            if (span[index] != 0x92) ThrowUnexpected();
            index++;

            // field 0: int family, expected fixint
            var code = span[index];
            if ((byte)(code + 32) <= 159)
            {
                index++;
            }
            else if ((byte)(code - 0xcc) < 8) // uint8..int64: body size 1/2/4/8
            {
                index += 1 + (1 << ((code - 0xcc) & 3));
            }
            else
            {
                ThrowUnexpected();
            }

            // field 1: str family, expected fixstr
            code = span[index];
            if ((byte)(code - 0xa0) < 0x20)
            {
                index += 1 + (code & 31);
            }
            else if (code == 0xd9)
            {
                index += 2 + span[index + 1];
            }
            else if (code == 0xda)
            {
                index += 3 + ReadUInt16BigEndian(span.Slice(index));
            }
            else
            {
                ThrowUnexpected();
            }
            count++;
        }
        return count;
    }

    // The adopted tiered decode with the slow path fully inlined into the loop body:
    // no call anywhere, so payload/child/size are never address-taken and can live in
    // registers even on the fast path (the cold-call shape forces them into stack
    // slots — see the captured ScanTokens asm). Measures whether
    // TryReadTokenSlow's NoInlining is costing the hot path.
    [Benchmark]
    public long FullyInlined()
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
                long payloadLength = 0;
                long childValueCount = 0;
                var tokenSize = 1;
                var code = span[index];
                if ((byte)(code + 32) <= 159) // fixint
                {
                }
                else if ((byte)(code - 0xa0) < 0x20) // fixstr
                {
                    payloadLength = code & 31;
                }
                else
                {
                    uint entry = SingleByteTokenTable[code];
                    if ((entry & 1) != 0)
                    {
                        payloadLength = (entry >> 1) & 31;
                        childValueCount = entry >> 6;
                    }
                    else
                    {
                        var tail = span.Slice(index);
                        switch (code)
                        {
                            case MessagePackCode.FixExt1:
                            case MessagePackCode.FixExt2:
                            case MessagePackCode.FixExt4:
                            case MessagePackCode.FixExt8:
                            case MessagePackCode.FixExt16:
                                tokenSize = 2;
                                if (tail.Length < 2) ThrowUnexpected();
                                payloadLength = 1 << (code - MessagePackCode.FixExt1);
                                break;
                            case MessagePackCode.Str8:
                            case MessagePackCode.Bin8:
                                tokenSize = 2;
                                if (tail.Length < 2) ThrowUnexpected();
                                payloadLength = tail[1];
                                break;
                            case MessagePackCode.Str16:
                            case MessagePackCode.Bin16:
                                tokenSize = 3;
                                if (tail.Length < 3) ThrowUnexpected();
                                payloadLength = ReadUInt16BigEndian(tail);
                                break;
                            case MessagePackCode.Str32:
                            case MessagePackCode.Bin32:
                                tokenSize = 5;
                                if (tail.Length < 5) ThrowUnexpected();
                                payloadLength = ReadUInt32BigEndian(tail);
                                break;
                            case MessagePackCode.Ext8:
                                tokenSize = 3;
                                if (tail.Length < 3) ThrowUnexpected();
                                payloadLength = tail[1];
                                break;
                            case MessagePackCode.Ext16:
                                tokenSize = 4;
                                if (tail.Length < 4) ThrowUnexpected();
                                payloadLength = ReadUInt16BigEndian(tail);
                                break;
                            case MessagePackCode.Ext32:
                                tokenSize = 6;
                                if (tail.Length < 6) ThrowUnexpected();
                                payloadLength = ReadUInt32BigEndian(tail);
                                break;
                            case MessagePackCode.Array16:
                                tokenSize = 3;
                                if (tail.Length < 3) ThrowUnexpected();
                                childValueCount = ReadUInt16BigEndian(tail);
                                break;
                            case MessagePackCode.Array32:
                                tokenSize = 5;
                                if (tail.Length < 5) ThrowUnexpected();
                                childValueCount = ReadUInt32BigEndian(tail);
                                break;
                            case MessagePackCode.Map16:
                                tokenSize = 3;
                                if (tail.Length < 3) ThrowUnexpected();
                                childValueCount = 2L * ReadUInt16BigEndian(tail);
                                break;
                            case MessagePackCode.Map32:
                                tokenSize = 5;
                                if (tail.Length < 5) ThrowUnexpected();
                                childValueCount = 2L * ReadUInt32BigEndian(tail);
                                break;
                            default: // 0xc1
                                ThrowUnexpected();
                                break;
                        }
                    }
                }
                remaining += childValueCount - 1;
                pending = payloadLength;
                index += tokenSize;
            }
        }
        if (pending == 0 && remaining == 0) count++;
        return count;
    }

    static void ThrowUnexpected() => throw new InvalidOperationException("scan failed on a complete well-formed payload");

    #region candidates

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenCurrent(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // positive/negative fixint, folded form
            {
                payloadLength = 0;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    // bit0 = single-byte token, bits1-5 = payloadLength, bits6-10 = childValueCount;
    // 0 = multi-byte header or 0xc1, resolved by the shared slow path
    static ReadOnlySpan<ushort> SingleByteTokenTable =>
    [
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0081, 0x0101, 0x0181, 0x0201, 0x0281, 0x0301, 0x0381, 0x0401, 0x0481, 0x0501, 0x0581, 0x0601, 0x0681, 0x0701, 0x0781,
        0x0001, 0x0041, 0x0081, 0x00c1, 0x0101, 0x0141, 0x0181, 0x01c1, 0x0201, 0x0241, 0x0281, 0x02c1, 0x0301, 0x0341, 0x0381, 0x03c1,
        0x0001, 0x0003, 0x0005, 0x0007, 0x0009, 0x000b, 0x000d, 0x000f, 0x0011, 0x0013, 0x0015, 0x0017, 0x0019, 0x001b, 0x001d, 0x001f,
        0x0021, 0x0023, 0x0025, 0x0027, 0x0029, 0x002b, 0x002d, 0x002f, 0x0031, 0x0033, 0x0035, 0x0037, 0x0039, 0x003b, 0x003d, 0x003f,
        0x0001, 0x0000, 0x0001, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0009, 0x0011, 0x0003, 0x0005, 0x0009, 0x0011,
        0x0003, 0x0005, 0x0009, 0x0011, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
        0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenSingleByteTable(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            uint entry = SingleByteTokenTable[source[0]];
            if ((entry & 1) != 0)
            {
                payloadLength = (entry >> 1) & 31;
                childValueCount = entry >> 6;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    // bits0-2 = tokenSize (0 = the never-used 0xc1), bits3-7 = immediate payloadLength,
    // bits8-12 = immediate childValueCount, bits13-14 = length field width
    // (0 none, 1 u8, 2 u16, 3 u32), bit15 = length is a child count (array/map),
    // bit16 = double it (map)
    static ReadOnlySpan<uint> FullTokenTable =>
    [
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00201, 0x00401, 0x00601, 0x00801, 0x00a01, 0x00c01, 0x00e01, 0x01001, 0x01201, 0x01401, 0x01601, 0x01801, 0x01a01, 0x01c01, 0x01e01,
        0x00001, 0x00101, 0x00201, 0x00301, 0x00401, 0x00501, 0x00601, 0x00701, 0x00801, 0x00901, 0x00a01, 0x00b01, 0x00c01, 0x00d01, 0x00e01, 0x00f01,
        0x00001, 0x00009, 0x00011, 0x00019, 0x00021, 0x00029, 0x00031, 0x00039, 0x00041, 0x00049, 0x00051, 0x00059, 0x00061, 0x00069, 0x00071, 0x00079,
        0x00081, 0x00089, 0x00091, 0x00099, 0x000a1, 0x000a9, 0x000b1, 0x000b9, 0x000c1, 0x000c9, 0x000d1, 0x000d9, 0x000e1, 0x000e9, 0x000f1, 0x000f9,
        0x00001, 0x00000, 0x00001, 0x00001, 0x02002, 0x04003, 0x06005, 0x02003, 0x04004, 0x06006, 0x00021, 0x00041, 0x00009, 0x00011, 0x00021, 0x00041,
        0x00009, 0x00011, 0x00021, 0x00041, 0x0000a, 0x00012, 0x00022, 0x00042, 0x00082, 0x02002, 0x04003, 0x06005, 0x0c003, 0x0e005, 0x1c003, 0x1e005,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
        0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001, 0x00001,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenFullTable(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (source.IsEmpty)
        {
            payloadLength = 0;
            childValueCount = 0;
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        uint entry = FullTokenTable[source[0]];
        var size = (int)(entry & 7);
        payloadLength = (entry >> 3) & 31;
        childValueCount = (entry >> 8) & 31;
        tokenSize = size;
        if ((uint)source.Length < (uint)size)
        {
            return DecodeResult.InsufficientBuffer;
        }
        if (size == 0) // 0xc1
        {
            return DecodeResult.TokenMismatch;
        }
        uint width = (entry >> 13) & 3;
        if (width != 0)
        {
            long length = width == 1 ? source[1]
                : width == 2 ? ReadUInt16BigEndian(source)
                : ReadUInt32BigEndian(source);
            if ((entry & (1u << 15)) != 0)
            {
                childValueCount = length << (int)((entry >> 16) & 1);
            }
            else
            {
                payloadLength = length;
            }
        }
        return DecodeResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenFixFamilyBranch(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // positive/negative fixint, folded form
            {
                payloadLength = 0;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            if ((byte)(code0 - 0x80) < 0x40) // fixmap/fixarray/fixstr
            {
                if (code0 < MessagePackCode.MinFixStr)
                {
                    // fixmap (bit 0x10 clear) doubles the count, fixarray keeps it
                    childValueCount = (long)(code0 & 15) << (((code0 >> 4) & 1) ^ 1);
                    payloadLength = 0;
                }
                else
                {
                    payloadLength = code0 & 31;
                    childValueCount = 0;
                }
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenBranchThenTable(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // positive/negative fixint: no load, constant outs
            {
                payloadLength = 0;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            uint entry = SingleByteTokenTable[code0];
            if ((entry & 1) != 0)
            {
                payloadLength = (entry >> 1) & 31;
                childValueCount = entry >> 6;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenFixFamilyThenTable(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // positive/negative fixint, folded form
            {
                payloadLength = 0;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            if ((byte)(code0 - 0x80) < 0x40) // fixmap/fixarray/fixstr
            {
                if (code0 < MessagePackCode.MinFixStr)
                {
                    childValueCount = (long)(code0 & 15) << (((code0 >> 4) & 1) ^ 1);
                    payloadLength = 0;
                }
                else
                {
                    payloadLength = code0 & 31;
                    childValueCount = 0;
                }
                tokenSize = 1;
                return DecodeResult.Success;
            }
            uint entry = SingleByteTokenTable[code0];
            if ((entry & 1) != 0) // nil/bool/numerics
            {
                payloadLength = (entry >> 1) & 31;
                childValueCount = entry >> 6;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    // round 3: fixint branch (no load) -> fixstr arithmetic (payload = code & 31, the
    // most common non-fixint token in real POCO streams) -> table for the remaining
    // single-byte codes -> slow for multi-byte headers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadTokenFixIntStrThenTable(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // positive/negative fixint, folded form
            {
                payloadLength = 0;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            if ((byte)(code0 - 0xa0) < 0x20) // fixstr
            {
                payloadLength = code0 & 31;
                childValueCount = 0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            uint entry = SingleByteTokenTable[code0];
            if ((entry & 1) != 0)
            {
                payloadLength = (entry >> 1) & 31;
                childValueCount = entry >> 6;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadTokenSlowShared(source, out payloadLength, out childValueCount, out tokenSize);
    }

    // copy of the library's TryReadTokenSlow, shared by every non-table candidate
    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult TryReadTokenSlowShared(ReadOnlySpan<byte> source, out long payloadLength, out long childValueCount, out int tokenSize)
    {
        payloadLength = 0;
        childValueCount = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        byte code = source[0];
        if ((byte)(code + 32) <= 159)
        {
            tokenSize = 1;
            return DecodeResult.Success;
        }
        if (code < MessagePackCode.MinFixArray)
        {
            childValueCount = 2L * (code & 0b0000_1111);
            tokenSize = 1;
            return DecodeResult.Success;
        }
        if (code < MessagePackCode.MinFixStr)
        {
            childValueCount = code & 0b0000_1111;
            tokenSize = 1;
            return DecodeResult.Success;
        }
        if (code < MessagePackCode.Nil)
        {
            payloadLength = code & 0b0001_1111;
            tokenSize = 1;
            return DecodeResult.Success;
        }
        switch (code)
        {
            case MessagePackCode.Nil:
            case MessagePackCode.False:
            case MessagePackCode.True:
                tokenSize = 1;
                return DecodeResult.Success;
            case MessagePackCode.UInt8:
            case MessagePackCode.Int8:
                payloadLength = 1;
                tokenSize = 1;
                return DecodeResult.Success;
            case MessagePackCode.UInt16:
            case MessagePackCode.Int16:
                payloadLength = 2;
                tokenSize = 1;
                return DecodeResult.Success;
            case MessagePackCode.UInt32:
            case MessagePackCode.Int32:
            case MessagePackCode.Float32:
                payloadLength = 4;
                tokenSize = 1;
                return DecodeResult.Success;
            case MessagePackCode.UInt64:
            case MessagePackCode.Int64:
            case MessagePackCode.Float64:
                payloadLength = 8;
                tokenSize = 1;
                return DecodeResult.Success;
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                payloadLength = 1 << (code - MessagePackCode.FixExt1);
                return DecodeResult.Success;
            case MessagePackCode.Str8:
            case MessagePackCode.Bin8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                payloadLength = source[1];
                return DecodeResult.Success;
            case MessagePackCode.Str16:
            case MessagePackCode.Bin16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                payloadLength = ReadUInt16BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Str32:
            case MessagePackCode.Bin32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                payloadLength = ReadUInt32BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Ext8:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                payloadLength = source[1];
                return DecodeResult.Success;
            case MessagePackCode.Ext16:
                tokenSize = 4;
                if (source.Length < 4) return DecodeResult.InsufficientBuffer;
                payloadLength = ReadUInt16BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Ext32:
                tokenSize = 6;
                if (source.Length < 6) return DecodeResult.InsufficientBuffer;
                payloadLength = ReadUInt32BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Array16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                childValueCount = ReadUInt16BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Array32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                childValueCount = ReadUInt32BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Map16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                childValueCount = 2L * ReadUInt16BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Map32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                childValueCount = 2L * ReadUInt32BigEndian(source);
                return DecodeResult.Success;
            default: // 0xc1 (never used)
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
        => MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
        => MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1)));

    #endregion
}

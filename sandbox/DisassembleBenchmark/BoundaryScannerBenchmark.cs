using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// Pass 1 of the async deserializer in isolation: how fast can the boundary scanner find the
// end of one Answer message (1658 B, ~100 tokens, string-heavy)? AnswerPipeBenchmark put
// the scan at ~300 ns (DeserializeV4Async 1288 vs DeserializeV4Manual 989), about 3 ns
// per token, which is heavy for a predicted-branch loop over a table-driven tokenizer.
//
// Candidates, all keeping the resumable contract of MessagePackBoundaryScanner
// (remaining values + pending payload bytes + consumed offset, suspendable at any byte):
//   Baseline   = the shipped scanner (TryReadToken per token, payload skipped by bouncing
//                through the loop head as `pending`)
//   Inline     = the hot single-byte token classes classified in the scan loop itself
//                (fixint / fixstr / fixarray / fixmap / nil / bool), the payload of a fixstr
//                or any other token skipped in place when it fits the span, the end-of-value
//                check moved to right after the decrement; TryReadToken only for the rest
//   InlineSpan = Inline plus a single-segment entry that scans buffer.FirstSpan directly,
//                bypassing the segment enumerator and the stitch path entirely
//
// Setup is self-verifying: every candidate must report the same Consumed as the shipped
// scanner on the Answer wire and on a synthetic message carrying every multi-byte header
// (str8/16/32, bin8/16/32, ext8/16/32 + fixext, array16/32, map16/32, uint8..64, float, int
// bodies), fed whole, split into two segments at every offset, and grown one byte at a time.
// (--scanner-check runs this setup alone, without BenchmarkDotNet.)
//
// MEASURED (i7-13700KF, ShortRun, Answer wire 1658 B, ns/op):
//   round 1: Baseline 264 | Inline 241 (0.91x) | InlineSpan 199 (0.75x)
//   round 2: Baseline 267 | Inline 241 | InlineSpan 199 | InlineSpan2 208 | SwitchSpan 186 (0.70x)
// The per-token classification chain, the pending round trip and the segment enumerator
// were worth ~25%; the jump table another ~5%; InlineSpan2's per-branch end test and unsafe
// indexing bought nothing over InlineSpan (tie band). At 186 ns for ~100 tokens the loop sits
// at ~9 cycles per token against a floor of ~7 set by the load-to-address dependency of every
// string length, so this line of work is spent. SwitchSpan was ported into
// MessagePackBoundaryScanner (single-segment span entry + the same span loop per segment when
// stitching), after which the Baseline row measures the ported scanner: 184 ns, and
// AnswerPipeBenchmark's DeserializeV4Async moved 1288 -> 1235 ns (the scan is now ~250 of the
// 1235; the rest of the gap to DeserializeV4Manual's 980 is inherent to scanning at all,
// which is the case for the optimistic single-pass idea or length-prefixed framing).
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BoundaryScannerBenchmark
{
    byte[] answer = default!;
    byte[] synthetic = default!;
    ReadOnlySequence<byte> answerSequence;

    [GlobalSetup]
    public void Setup()
    {
        answer = MessagePackSerializer.Serialize(AnswerBenchmark.CreateAnswer());
        answerSequence = new ReadOnlySequence<byte>(answer);
        synthetic = BuildSynthetic();

        foreach (var payload in new[] { answer, synthetic })
        {
            Verify(payload);
        }
    }

    static void Verify(byte[] payload)
    {
        // the shipped scanner consumes the whole message; every candidate must agree with it
        var expected = ScanWhole<BaselineScanner>(payload);
        if (expected != payload.Length) throw new InvalidOperationException($"verify failed: baseline consumed {expected} of {payload.Length}");
        Check<BaselineScanner>(payload, expected);
        Check<InlineScanner>(payload, expected);
        Check<InlineSpanScanner>(payload, expected);
        Check<InlineSpan2Scanner>(payload, expected);
        Check<SwitchSpanScanner>(payload, expected);
    }

    static void Check<TScanner>(byte[] payload, long expected) where TScanner : struct, IScanner
    {
        var name = typeof(TScanner).Name;
        if (ScanWhole<TScanner>(payload) != expected) throw new InvalidOperationException($"verify failed: {name} whole");

        // two segments split at every offset (the straddling-token stitch path)
        for (var split = 1; split < payload.Length; split++)
        {
            if (ScanSequence<TScanner>(Split(payload, split)) != expected) throw new InvalidOperationException($"verify failed: {name} split {split}");
        }

        // grown one byte at a time (the resumable contract): every intermediate call must
        // report "more needed" and the final one the same Consumed
        if (ScanGrowing<TScanner>(payload) != expected) throw new InvalidOperationException($"verify failed: {name} growing");

        // a trailing byte must not be consumed
        var padded = payload.Concat(new byte[] { 0xc0 }).ToArray();
        if (ScanWhole<TScanner>(padded) != expected) throw new InvalidOperationException($"verify failed: {name} trailing");
    }

    static long ScanWhole<TScanner>(byte[] payload) where TScanner : struct, IScanner
        => ScanSequence<TScanner>(new ReadOnlySequence<byte>(payload));

    static long ScanSequence<TScanner>(ReadOnlySequence<byte> sequence) where TScanner : struct, IScanner
    {
        var scanner = new TScanner();
        scanner.Init();
        if (!scanner.TryFindEnd(in sequence)) throw new InvalidOperationException($"{typeof(TScanner).Name} did not find the end of a complete {sequence.Length}-byte message (stopped at {scanner.Consumed}, single segment: {sequence.IsSingleSegment})");
        return scanner.Consumed;
    }

    static long ScanGrowing<TScanner>(byte[] payload) where TScanner : struct, IScanner
    {
        var scanner = new TScanner();
        scanner.Init();
        for (var length = 1; length < payload.Length; length++)
        {
            var sequence = new ReadOnlySequence<byte>(payload, 0, length);
            if (scanner.TryFindEnd(in sequence)) throw new InvalidOperationException($"scanner claimed the end at {length} of {payload.Length}");
        }
        var whole = new ReadOnlySequence<byte>(payload);
        if (!scanner.TryFindEnd(in whole)) throw new InvalidOperationException("scanner did not find the end after growing to the full message");
        return scanner.Consumed;
    }

    static ReadOnlySequence<byte> Split(byte[] payload, int split)
    {
        var first = new Segment(payload.AsMemory(0, split));
        var second = first.Append(payload.AsMemory(split));
        return new ReadOnlySequence<byte>(first, 0, second, second.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    // one array holding every header shape the slow tail handles, with bodies large enough
    // to cross a split, plus nested containers so child counting is exercised
    static byte[] BuildSynthetic()
    {
        var buffer = new ArrayBufferWriter<byte>();
        void Write(params byte[] bytes) => buffer.Write(bytes);
        void Fill(int count, byte value) { for (var i = 0; i < count; i++) Write(value); }

        Write(0xdc, 0x00, 0x1a); // array16(26)
        Write(0xd9, 40); Fill(40, (byte)'a');                       // str8
        Write(0xda, 0x01, 0x00); Fill(256, (byte)'b');              // str16
        Write(0xdb, 0x00, 0x00, 0x00, 0x05); Fill(5, (byte)'c');    // str32
        Write(0xc4, 3); Fill(3, 0xff);                              // bin8
        Write(0xc5, 0x00, 0x03); Fill(3, 0xfe);                     // bin16
        Write(0xc6, 0x00, 0x00, 0x00, 0x03); Fill(3, 0xfd);         // bin32
        Write(0xd4, 0x01, 0x11);                                    // fixext1
        Write(0xd5, 0x01, 0x11, 0x22);                              // fixext2
        Write(0xd6, 0xff, 1, 2, 3, 4);                              // fixext4 (timestamp32)
        Write(0xd7, 0xff, 1, 2, 3, 4, 5, 6, 7, 8);                  // fixext8
        Write(0xd8, 0x01); Fill(16, 0x33);                          // fixext16
        Write(0xc7, 5, 0x02); Fill(5, 0x44);                        // ext8
        Write(0xc8, 0x00, 0x05, 0x02); Fill(5, 0x55);               // ext16
        Write(0xc9, 0x00, 0x00, 0x00, 0x05, 0x02); Fill(5, 0x66);   // ext32
        Write(0xcc, 200);                                           // uint8
        Write(0xcd, 0x12, 0x34);                                    // uint16
        Write(0xce, 1, 2, 3, 4);                                    // uint32
        Write(0xcf, 1, 2, 3, 4, 5, 6, 7, 8);                        // uint64
        Write(0xd0, 0xff); Write(0xd1, 0xff, 0xff); Write(0xd2, 1, 2, 3, 4); Write(0xd3, 1, 2, 3, 4, 5, 6, 7, 8); // int8..64 (4 values)
        Write(0xca, 1, 2, 3, 4); Write(0xcb, 1, 2, 3, 4, 5, 6, 7, 8); // float32/64 (2 values)
        Write(0xdd, 0x00, 0x00, 0x00, 0x02, 0x01, 0xa1, (byte)'x'); // array32(2): fixint, fixstr
        Write(0xde, 0x00, 0x01, 0xa1, (byte)'k', 0xc0);             // map16(1): key, nil
        Write(0xdf, 0x00, 0x00, 0x00, 0x01, 0xa1, (byte)'k', 0x81, 0xa1, (byte)'k', 0x92, 0xc2, 0xc3); // map32(1): k -> fixmap(1){k: fixarray[false,true]}
        Write(0x93, 0x01, 0x02, 0x03);                              // fixarray[1,2,3]
        Fill(40, 0x7f);                                             // 40 positive fixints (exercises the vector bulk skip)
        // element count: 3 str + 3 bin + 5 fixext + 3 ext + 4 uint + 4 int + 2 float
        //                + array32 + map16 + map32 + fixarray + 40 fixints = 68
        var body = buffer.WrittenSpan.ToArray();
        body[1] = 0x00;
        body[2] = 68;
        return body;
    }

    #region benchmarks

    [BenchmarkCategory("Answer"), Benchmark(Baseline = true)]
    public long Baseline()
    {
        var scanner = new BaselineScanner();
        scanner.Init();
        scanner.TryFindEnd(in answerSequence);
        return scanner.Consumed;
    }

    [BenchmarkCategory("Answer"), Benchmark]
    public long Inline()
    {
        var scanner = new InlineScanner();
        scanner.Init();
        scanner.TryFindEnd(in answerSequence);
        return scanner.Consumed;
    }

    [BenchmarkCategory("Answer"), Benchmark]
    public long InlineSpan()
    {
        var scanner = new InlineSpanScanner();
        scanner.Init();
        scanner.TryFindEnd(in answerSequence);
        return scanner.Consumed;
    }

    [BenchmarkCategory("Answer"), Benchmark]
    public long InlineSpan2()
    {
        var scanner = new InlineSpan2Scanner();
        scanner.Init();
        scanner.TryFindEnd(in answerSequence);
        return scanner.Consumed;
    }

    [BenchmarkCategory("Answer"), Benchmark]
    public long SwitchSpan()
    {
        var scanner = new SwitchSpanScanner();
        scanner.Init();
        scanner.TryFindEnd(in answerSequence);
        return scanner.Consumed;
    }

    #endregion

    #region candidates

    interface IScanner
    {
        void Init();
        long Consumed { get; }
        bool TryFindEnd(in ReadOnlySequence<byte> buffer);
    }

    struct BaselineScanner : IScanner
    {
        MessagePackBoundaryScanner inner;
        public void Init() => inner = new MessagePackBoundaryScanner();
        public long Consumed => inner.Consumed;
        public bool TryFindEnd(in ReadOnlySequence<byte> buffer) => inner.TryFindEnd(in buffer);
    }

    // shared hot loop for Inline and InlineSpan: scans one span from `index`, updating the
    // resumable state. Returns:
    //   Done       - the value ended inside this span (index = its end)
    //   Exhausted  - every byte of the span was scanned, more data needed (pending may be > 0)
    //   Straddle   - a multi-byte header starts at index but its length bytes are not all in
    //                this span; `need` is the token's size, the caller stitches from the sequence
    enum ScanStatus { Done, Exhausted, Straddle }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ScanStatus ScanSpan(ReadOnlySpan<byte> span, ref long remaining, ref long pending, ref int index, out int need)
    {
        need = 0;
        var length = span.Length;
        var i = index;
        var rem = remaining;
        var pend = pending;

        // a payload left over from the previous span
        if (pend > 0)
        {
            var take = (int)Math.Min(pend, length - i);
            i += take;
            pend -= take;
            if (pend > 0)
            {
                index = i;
                remaining = rem;
                pending = pend;
                return ScanStatus.Exhausted;
            }
        }

        while (rem > 0)
        {
            if (i >= length)
            {
                break;
            }
            var code = span[i];
            if ((byte)(code + 32) <= 159) // positive/negative fixint
            {
                i++;
                rem--;
                // bulk fixint skip: 32 code bytes at once while they are all fixints
                if (Vector256.IsHardwareAccelerated && rem >= 32)
                {
                    while (rem >= 32 && length - i >= 32)
                    {
                        var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)i).AsSByte();
                        if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                        {
                            break;
                        }
                        i += 32;
                        rem -= 32;
                    }
                }
                continue;
            }
            if ((byte)(code - 0xa0) < 0x20) // fixstr: payload in place when buffered
            {
                var payload = code & 0x1f;
                i++;
                rem--;
                if (length - i >= payload)
                {
                    i += payload;
                    continue;
                }
                pend = payload - (length - i);
                i = length;
                break;
            }
            if ((byte)(code - 0x90) < 0x10) // fixarray
            {
                i++;
                rem += (code & 0x0f) - 1;
                continue;
            }
            if ((byte)(code - 0x80) < 0x10) // fixmap: entries count twice
            {
                i++;
                rem += ((code & 0x0f) << 1) - 1;
                continue;
            }
            if (code == 0xc0 || (byte)(code - 0xc2) < 2) // nil, false, true
            {
                i++;
                rem--;
                continue;
            }

            // everything else: numeric bodies, str8+/bin/ext headers, array16+/map16+
            var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(i), out var payloadLength, out var childValueCount, out var tokenSize);
            if (result == DecodeResult.Success)
            {
                i += tokenSize;
                rem += childValueCount - 1;
                if (payloadLength > 0)
                {
                    if (length - i >= payloadLength)
                    {
                        i += (int)payloadLength;
                        continue;
                    }
                    pend = payloadLength - (length - i);
                    i = length;
                    break;
                }
                continue;
            }
            if (result == DecodeResult.TokenMismatch)
            {
                throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
            }
            // InsufficientBuffer with the code byte present: the header straddles the span end
            index = i;
            remaining = rem;
            pending = pend;
            need = tokenSize;
            return ScanStatus.Straddle;
        }

        index = i;
        remaining = rem;
        pending = pend;
        return rem == 0 && pend == 0 ? ScanStatus.Done : ScanStatus.Exhausted;
    }

    struct InlineScanner : IScanner
    {
        const int MaxTokenSize = 6;
        long remainingValues;
        long pendingPayloadBytes;
        long consumed;

        public void Init() => remainingValues = 1;
        public long Consumed => consumed;
        public long Remaining => remainingValues;
        public long Pending => pendingPayloadBytes;

        // lets the single-span scanner hand its state over when the sequence grows a second segment
        public void Restore(long remaining, long pending, long offset)
        {
            remainingValues = remaining;
            pendingPayloadBytes = pending;
            consumed = offset;
        }

        public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
        {
            var remaining = remainingValues;
            var pending = pendingPayloadBytes;
            var offset = consumed;
            var found = Scan(in buffer, ref remaining, ref pending, ref offset);
            remainingValues = remaining;
            pendingPayloadBytes = pending;
            consumed = offset;
            return found;
        }

        // the original segment walk with ScanSpan as the per-segment loop
        static bool Scan(in ReadOnlySequence<byte> buffer, ref long remaining, ref long pending, ref long offset)
        {
            var totalLength = buffer.Length;
            Span<byte> stitch = stackalloc byte[MaxTokenSize];
            while (remaining > 0 || pending > 0)
            {
                if (offset == totalLength)
                {
                    return false;
                }
                var straddled = false;
                foreach (var segment in buffer.Slice(offset))
                {
                    var index = 0;
                    var status = ScanSpan(segment.Span, ref remaining, ref pending, ref index, out var need);
                    offset += index;
                    if (status == ScanStatus.Done)
                    {
                        return true;
                    }
                    if (status == ScanStatus.Straddle)
                    {
                        if (totalLength - offset < need)
                        {
                            return false; // the header's length bytes are not buffered yet
                        }
                        buffer.Slice(offset, need).CopyTo(stitch);
                        MessagePack.MessagePackPrimitives.TryReadToken(stitch.Slice(0, need), out var payloadLength, out var childValueCount, out var tokenSize);
                        remaining += childValueCount - 1;
                        pending = payloadLength;
                        offset += tokenSize;
                        straddled = true;
                        break; // re-slice from the new offset
                    }
                }
                if (!straddled)
                {
                    return remaining == 0 && pending == 0;
                }
            }
            return true;
        }
    }

    // Round 2 of the span loop. Same contract as ScanSpan; the differences are shape only:
    //   - fixstr is tested first (the dominant token on string-heavy graphs)
    //   - unchecked indexing through a ref, so the JIT has no bounds check to keep next to the
    //     loop's own `i < length` test
    //   - the end-of-value test lives in each branch right after the decrement instead of at
    //     the loop head, so a token that cannot end the value (a non-empty container) never
    //     pays for it and the loop head is one compare
    //   - fixmap and fixarray share one range test (0x80-0x9f) and pick the child count by bit 4
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ScanStatus ScanSpan2(ReadOnlySpan<byte> span, ref long remaining, ref long pending, ref int index, out int need)
    {
        need = 0;
        var length = span.Length;
        var i = index;
        var rem = remaining;
        var pend = pending;
        ref var start = ref MemoryMarshal.GetReference(span);

        if (pend > 0)
        {
            var take = (int)Math.Min(pend, length - i);
            i += take;
            pend -= take;
            if (pend > 0)
            {
                goto exhausted;
            }
            if (rem == 0)
            {
                goto done;
            }
        }

        while (i < length)
        {
            var code = Unsafe.Add(ref start, i);
            if ((byte)(code - 0xa0) < 0x20) // fixstr
            {
                var payload = code & 0x1f;
                i++;
                rem--;
                var available = length - i;
                if (available >= payload)
                {
                    i += payload;
                    if (rem == 0) goto done;
                    continue;
                }
                pend = payload - available;
                i = length;
                goto exhausted;
            }
            if ((byte)(code + 32) <= 159) // fixint
            {
                i++;
                rem--;
                if (Vector256.IsHardwareAccelerated && rem >= 32)
                {
                    while (rem >= 32 && length - i >= 32)
                    {
                        var v = Vector256.LoadUnsafe(ref start, (nuint)i).AsSByte();
                        if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                        {
                            break;
                        }
                        i += 32;
                        rem -= 32;
                    }
                }
                if (rem == 0) goto done;
                continue;
            }
            if ((byte)(code - 0x80) < 0x20) // fixmap 0x80-0x8f, fixarray 0x90-0x9f
            {
                i++;
                var count = code & 0x0f;
                rem += ((code & 0x10) != 0 ? count : count << 1) - 1;
                if (rem == 0) goto done;
                continue;
            }
            if (code == 0xc0 || (byte)(code - 0xc2) < 2) // nil, false, true
            {
                i++;
                rem--;
                if (rem == 0) goto done;
                continue;
            }

            var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(i), out var payloadLength, out var childValueCount, out var tokenSize);
            if (result == DecodeResult.Success)
            {
                i += tokenSize;
                rem += childValueCount - 1;
                if (payloadLength > 0)
                {
                    var available = length - i;
                    if (available >= payloadLength)
                    {
                        i += (int)payloadLength;
                    }
                    else
                    {
                        pend = payloadLength - available;
                        i = length;
                        goto exhausted;
                    }
                }
                if (rem == 0) goto done;
                continue;
            }
            if (result == DecodeResult.TokenMismatch)
            {
                throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
            }
            index = i;
            remaining = rem;
            pending = pend;
            need = tokenSize;
            return ScanStatus.Straddle;
        }

    exhausted:
        index = i;
        remaining = rem;
        pending = pend;
        return ScanStatus.Exhausted;

    done:
        index = i;
        remaining = 0;
        pending = 0;
        return ScanStatus.Done;
    }

    // ScanSpan2 with the classification chain replaced by one jump table on the top three
    // bits of the code byte (8 classes): positive fixint x4, fixmap/fixarray, fixstr,
    // 0xc0-0xdf (nil/bool inline, the rest through TryReadToken), negative fixint
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ScanStatus ScanSpanSwitch(ReadOnlySpan<byte> span, ref long remaining, ref long pending, ref int index, out int need)
    {
        need = 0;
        var length = span.Length;
        var i = index;
        var rem = remaining;
        var pend = pending;
        ref var start = ref MemoryMarshal.GetReference(span);

        if (pend > 0)
        {
            var take = (int)Math.Min(pend, length - i);
            i += take;
            pend -= take;
            if (pend > 0)
            {
                goto exhausted;
            }
            if (rem == 0)
            {
                goto done;
            }
        }

        while (i < length)
        {
            var code = Unsafe.Add(ref start, i);
            switch (code >> 5)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 7: // fixint
                    i++;
                    rem--;
                    if (Vector256.IsHardwareAccelerated && rem >= 32)
                    {
                        while (rem >= 32 && length - i >= 32)
                        {
                            var v = Vector256.LoadUnsafe(ref start, (nuint)i).AsSByte();
                            if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                            {
                                break;
                            }
                            i += 32;
                            rem -= 32;
                        }
                    }
                    if (rem == 0) goto done;
                    continue;
                case 4: // fixmap / fixarray
                {
                    i++;
                    var count = code & 0x0f;
                    rem += ((code & 0x10) != 0 ? count : count << 1) - 1;
                    if (rem == 0) goto done;
                    continue;
                }
                case 5: // fixstr
                {
                    var payload = code & 0x1f;
                    i++;
                    rem--;
                    var available = length - i;
                    if (available >= payload)
                    {
                        i += payload;
                        if (rem == 0) goto done;
                        continue;
                    }
                    pend = payload - available;
                    i = length;
                    goto exhausted;
                }
                default: // 0xc0-0xdf
                {
                    if (code == 0xc0 || (byte)(code - 0xc2) < 2)
                    {
                        i++;
                        rem--;
                        if (rem == 0) goto done;
                        continue;
                    }
                    var result = MessagePack.MessagePackPrimitives.TryReadToken(span.Slice(i), out var payloadLength, out var childValueCount, out var tokenSize);
                    if (result == DecodeResult.Success)
                    {
                        i += tokenSize;
                        rem += childValueCount - 1;
                        if (payloadLength > 0)
                        {
                            var available = length - i;
                            if (available >= payloadLength)
                            {
                                i += (int)payloadLength;
                            }
                            else
                            {
                                pend = payloadLength - available;
                                i = length;
                                goto exhausted;
                            }
                        }
                        if (rem == 0) goto done;
                        continue;
                    }
                    if (result == DecodeResult.TokenMismatch)
                    {
                        throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
                    }
                    index = i;
                    remaining = rem;
                    pending = pend;
                    need = tokenSize;
                    return ScanStatus.Straddle;
                }
            }
        }

    exhausted:
        index = i;
        remaining = rem;
        pending = pend;
        return ScanStatus.Exhausted;

    done:
        index = i;
        remaining = 0;
        pending = 0;
        return ScanStatus.Done;
    }

    struct InlineSpan2Scanner : IScanner
    {
        long remainingValues;
        long pendingPayloadBytes;
        long consumed;

        public void Init() => remainingValues = 1;
        public long Consumed => consumed;

        public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                var remaining = remainingValues;
                var pending = pendingPayloadBytes;
                var index = (int)consumed;
                var status = ScanSpan2(buffer.FirstSpan, ref remaining, ref pending, ref index, out _);
                remainingValues = remaining;
                pendingPayloadBytes = pending;
                consumed = index;
                return status == ScanStatus.Done;
            }
            var multi = new InlineScanner();
            multi.Restore(remainingValues, pendingPayloadBytes, consumed);
            var found = multi.TryFindEnd(in buffer);
            remainingValues = multi.Remaining;
            pendingPayloadBytes = multi.Pending;
            consumed = multi.Consumed;
            return found;
        }
    }

    struct SwitchSpanScanner : IScanner
    {
        long remainingValues;
        long pendingPayloadBytes;
        long consumed;

        public void Init() => remainingValues = 1;
        public long Consumed => consumed;

        public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                var remaining = remainingValues;
                var pending = pendingPayloadBytes;
                var index = (int)consumed;
                var status = ScanSpanSwitch(buffer.FirstSpan, ref remaining, ref pending, ref index, out _);
                remainingValues = remaining;
                pendingPayloadBytes = pending;
                consumed = index;
                return status == ScanStatus.Done;
            }
            var multi = new InlineScanner();
            multi.Restore(remainingValues, pendingPayloadBytes, consumed);
            var found = multi.TryFindEnd(in buffer);
            remainingValues = multi.Remaining;
            pendingPayloadBytes = multi.Pending;
            consumed = multi.Consumed;
            return found;
        }
    }

    struct InlineSpanScanner : IScanner
    {
        long remainingValues;
        long pendingPayloadBytes;
        long consumed;

        public void Init() => remainingValues = 1;
        public long Consumed => consumed;

        public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                // the common case: one span, no enumerator, no stitching possible (a header that
                // runs past the end is simply "more needed")
                var remaining = remainingValues;
                var pending = pendingPayloadBytes;
                var index = (int)consumed;
                var status = ScanSpan(buffer.FirstSpan, ref remaining, ref pending, ref index, out _);
                remainingValues = remaining;
                pendingPayloadBytes = pending;
                consumed = index;
                return status == ScanStatus.Done;
            }
            var multi = new InlineScanner();
            multi.Restore(remainingValues, pendingPayloadBytes, consumed);
            var found = multi.TryFindEnd(in buffer);
            remainingValues = multi.Remaining;
            pendingPayloadBytes = multi.Pending;
            consumed = multi.Consumed;
            return found;
        }
    }

    #endregion
}

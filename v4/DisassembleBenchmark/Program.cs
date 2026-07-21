using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

if (args.Contains("--verify"))
{
    var ok = VerifySum();
    ok &= VerifyTryWriteInt32();
    ok &= VerifyTryReadInt32();
    ok &= VerifyUtf8Transcode();
    ok &= VerifyNarrowWrite();
    ok &= VerifyExtTimestampWrite();
    ok &= VerifyPocoSerializers();
    ok &= VerifyNbOfficial();
    ok &= VerifyAnswer();
    Console.WriteLine(ok ? "all OK" : "FAILED");
    return ok ? 0 : 1;
}

// opt-in: allocates multi-GB strings/buffers to reach the > 715,827,880-char huge-string
// paths of GetMaxStringByteCount / WriteString, which --verify cannot exercise
if (args.Contains("--verify-huge"))
{
    var ok = VerifyHugeString();
    Console.WriteLine(ok ? "all OK" : "FAILED");
    return ok ? 0 : 1;
}

// hot-loops the Ultra candidates of NerdbankOfficialBenchmark so DOTNET_JitDisasm can capture
// their Tier1 code — BDN's DisassemblyDiagnoser silently produces no output for these methods
// (only the Nerdbank ones get disassembled). Usage:
//   $env:DOTNET_JitDisasm = "*_Ultra *NbPoco*Formatter*"; dotnet run -c Release -- --jit-nb
if (args.Contains("--jit-nb"))
{
    var b = new NerdbankOfficialBenchmark();
    b.Setup();
    // ~30M calls x ~100ns keeps the process alive long enough for tier1 promotion + rejit
    for (int i = 0; i < 30_000_000; i++)
    {
        b.SerializeAsArray_Ultra();
        b.DeserializeAsArray_Ultra();
        b.SerializeMap_Ultra();
        b.DeserializeMap_Ultra();
    }
    return 0;
}

// hot-loops the DisasmProbe6 entry-shape variants for DOTNET_JitDisasm capture — the
// try/finally ones are silently dropped by DisassemblyDiagnoser (round 2 finding). Usage:
//   $env:DOTNET_JitDisasm = "*EntryShapeProbe*"; dotnet run -c Release -- --jit-probe6
if (args.Contains("--jit-probe6"))
{
    var b = new DisasmProbe6Benchmark();
    b.Setup();
    for (int i = 0; i < 3_000_000; i++)
    {
        b.PocoTryFinally();
        b.PocoNoTryFinally();
        b.PocoNoDispose();
    }
    return 0;
}

// same JitDisasm hot-loop for the DisasmProbe7 entry variants
if (args.Contains("--jit-probe7"))
{
    var b = new DisasmProbe7Benchmark();
    b.Setup();
    for (int i = 0; i < 3_000_000; i++)
    {
        b.DefaultSerialize();
        b.CustomSerialize();
        b.NewSerializerEachCall();
    }
    return 0;
}

// same JitDisasm hot-loop for the DisasmProbe8 buffer/formatter variants
if (args.Contains("--jit-probe8"))
{
    var b = new DisasmProbe8Benchmark();
    b.Setup();
    for (int i = 0; i < 3_000_000; i++)
    {
        b.Current();
        b.BatchFormatter();
    }
    return 0;
}

// same JitDisasm hot-loop for the DisasmProbe9 per-value/batch formatter shapes
if (args.Contains("--jit-probe9"))
{
    var b = new DisasmProbe9Benchmark();
    b.Setup();
    for (int i = 0; i < 3_000_000; i++)
    {
        b.SerializePerValue();
        b.SerializeBatch();
        b.DeserializePerValue();
        b.DeserializeBatch();
    }
    return 0;
}

// same JitDisasm hot-loop for the DisasmProbe11 nested-dispatch variants
if (args.Contains("--jit-probe11"))
{
    var b = new DisasmProbe11Benchmark();
    b.Setup();
    for (int i = 0; i < 3_000_000; i++)
    {
        b.SerializeIfaceField();
        b.SerializePerCall();
        b.SerializeDirect();
        b.SerializeFlat();
        b.DeserializeIfaceField();
        b.DeserializePerCall();
        b.DeserializeDirect();
        b.DeserializeFlat();
    }
    return 0;
}

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.ShortRun) // 3 warmup + 3 iterations, fast feedback loop for AI iteration
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
        maxDepth: 3,
        printSource: true,
        exportGithubMarkdown: true,
        exportCombinedDisassemblyReport: true)))
    .AddExporter(JsonExporter.Full);

// ETW kernel session requires elevation; enable counters only when elevated so
// non-admin runs still work (without the counter columns)
var isElevated = OperatingSystem.IsWindows()
    && new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
if (isElevated)
{
    config.AddHardwareCounters(
        HardwareCounter.BranchMispredictions,
        HardwareCounter.BranchInstructions,
        HardwareCounter.InstructionRetired, // with TotalCycles gives IPC: low IPC = latency-bound (dependency chain)
        HardwareCounter.TotalCycles);
}
else
{
    Console.WriteLine("// not elevated: HardwareCounters disabled (run elevated to collect counters)");
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
return 0;

// the three serializers in SerializerPocoBenchmark must produce byte-identical payloads
// (all use the array-of-3 format) and deserialize each other's output before comparing speed
static bool VerifyPocoSerializers()
{
    var ok = true;
    var b = new SerializerPocoBenchmark();
    b.Setup();

    var csharp = b.SerializeMessagePackCSharp();
    var payloads = new Dictionary<string, byte[]>
    {
        [nameof(b.SerializeUltra)] = b.SerializeUltra(),
        [nameof(b.SerializeNerdbank)] = b.SerializeNerdbank(),
    };
    foreach (var (name, actual) in payloads)
    {
        if (!actual.AsSpan().SequenceEqual(csharp))
        {
            ok = false;
            Console.WriteLine($"NG Poco {name}: [{Convert.ToHexString(actual)}] expected [{Convert.ToHexString(csharp)}]");
        }
    }

    var expected = b.DeserializeMessagePackCSharp();
    var people = new Dictionary<string, BenchPerson>
    {
        [nameof(b.DeserializeUltra)] = b.DeserializeUltra(),
        [nameof(b.DeserializeNerdbank)] = b.DeserializeNerdbank(),
    };
    foreach (var (name, actual) in people)
    {
        if (actual.Id != expected.Id || actual.Name != expected.Name || actual.Score != expected.Score)
        {
            ok = false;
            Console.WriteLine($"NG Poco {name}: ({actual.Id}, {actual.Name}, {actual.Score}) expected ({expected.Id}, {expected.Name}, {expected.Score})");
        }
    }
    return ok;
}

// UltraMessagePack in NerdbankOfficialBenchmark must produce byte-identical payloads to the
// MessagePack-CSharp baseline for both POCO shapes (map with string keys / array), on both
// serialize entry points the benchmark exercises (byte[] and reused IBufferWriter). The
// deserialize side reads a ReadOnlySequence: verified contiguous AND split at every byte
// position into two segments, which exercises ReadOnlySequenceReadBuffer's stitched-window
// slow paths (a key or value straddling a segment boundary).
// AnswerBenchmark.Setup() is self-verifying: it demands Ultra bytes == MessagePack-CSharp
// oracle bytes on the nested Stack Overflow Answer graph and re-serializes every library's
// roundtripped object through the oracle for byte-identity. It throws on any mismatch.
static bool VerifyAnswer()
{
    try
    {
        new AnswerBenchmark().Setup();
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"NG Answer: {ex.Message}");
        return false;
    }
}

static bool VerifyNbOfficial()
{
    var ok = true;
    var b = new NerdbankOfficialBenchmark();
    b.Setup(); // registers the Ultra formatters

    var cases = new (string Name, byte[] Expected, Func<byte[]> SerializeUltra, Action<ArrayBufferWriter<byte>> SerializeUltraWriter, Func<ReadOnlySequence<byte>, (int Int, string? Str)> DeserializeUltra)[]
    {
        ("map",
            MessagePack.MessagePackSerializer.Serialize(NbData.PocoMapSingle, MessagePack.MessagePackSerializerOptions.Standard),
            () => UltraMessagePack.MessagePackSerializer.Default.Serialize(NbData.PocoMapSingle),
            w => UltraMessagePack.MessagePackSerializer.Default.Serialize(w, NbData.PocoMapSingle),
            seq => { var v = UltraMessagePack.MessagePackSerializer.Default.Deserialize<NbPocoMap>(seq); return (v.SomeInt, v.SomeString); }),
        ("array",
            MessagePack.MessagePackSerializer.Serialize(NbData.PocoAsArraySingle, MessagePack.MessagePackSerializerOptions.Standard),
            () => UltraMessagePack.MessagePackSerializer.Default.Serialize(NbData.PocoAsArraySingle),
            w => UltraMessagePack.MessagePackSerializer.Default.Serialize(w, NbData.PocoAsArraySingle),
            seq => { var v = UltraMessagePack.MessagePackSerializer.Default.Deserialize<NbPocoAsArray>(seq); return (v.SomeInt, v.SomeString); }),
    };

    foreach (var (name, expected, serialize, serializeWriter, deserialize) in cases)
    {
        var actual = serialize();
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            ok = false;
            Console.WriteLine($"NG NbOfficial {name} serialize: [{Convert.ToHexString(actual)}] expected [{Convert.ToHexString(expected)}]");
        }

        var writer = new ArrayBufferWriter<byte>();
        serializeWriter(writer);
        if (!writer.WrittenSpan.SequenceEqual(expected))
        {
            ok = false;
            Console.WriteLine($"NG NbOfficial {name} serialize(IBufferWriter): [{Convert.ToHexString(writer.WrittenSpan)}] expected [{Convert.ToHexString(expected)}]");
        }

        for (int splitAt = 0; splitAt <= expected.Length; splitAt++)
        {
            var sequence = splitAt == expected.Length
                ? new ReadOnlySequence<byte>(expected) // contiguous single segment
                : NbSequenceSegment.CreateSplit(expected, splitAt);
            var (someInt, someString) = deserialize(sequence);
            if (someInt != 42 || someString != "Hello, World!")
            {
                ok = false;
                Console.WriteLine($"NG NbOfficial {name} deserialize splitAt={splitAt}: ({someInt}, {someString})");
            }
        }
    }
    return ok;
}

static bool VerifySum()
{
    var ok = true;
    foreach (var n in new[] { 0, 1, 3, 7, 8, 15, 16, 17, 63, 64, 65, 100, 127, 128, 129, 1000 })
    {
        var b = new SumBenchmark { N = n };
        b.Setup();
        var expected = b.ForLoop();
        var results = new Dictionary<string, int>
        {
            [nameof(b.ForEachLoop)] = b.ForEachLoop(),
            [nameof(b.LinqSum)] = b.LinqSum(),
            [nameof(b.VectorSimd)] = b.VectorSimd(),
            [nameof(b.TensorPrimitivesSum)] = b.TensorPrimitivesSum(),
            [nameof(b.VectorRef)] = b.VectorRef(),
            [nameof(b.Vector512Unrolled)] = b.Vector512Unrolled(),
            [nameof(b.Vector512Final)] = b.Vector512Final(),
        };
        foreach (var (name, actual) in results)
        {
            if (actual != expected)
            {
                ok = false;
                Console.WriteLine($"NG Sum N={n} {name}: {actual} (expected {expected})");
            }
        }
    }
    return ok;
}

static bool VerifyTryWriteInt32()
{
    var ok = true;

    // known-answer vectors pin down the reference implementation itself
    var known = new (int Value, byte[] Expected)[]
    {
        (0, [0x00]), (1, [0x01]), (127, [0x7f]),
        (128, [0xcc, 0x80]), (255, [0xcc, 0xff]),
        (256, [0xcd, 0x01, 0x00]), (65535, [0xcd, 0xff, 0xff]),
        (65536, [0xce, 0x00, 0x01, 0x00, 0x00]), (int.MaxValue, [0xce, 0x7f, 0xff, 0xff, 0xff]),
        (-1, [0xff]), (-32, [0xe0]),
        (-33, [0xd0, 0xdf]), (-128, [0xd0, 0x80]),
        (-129, [0xd1, 0xff, 0x7f]), (-32768, [0xd1, 0x80, 0x00]),
        (-32769, [0xd2, 0xff, 0xff, 0x7f, 0xff]), (int.MinValue, [0xd2, 0x80, 0x00, 0x00, 0x00]),
    };
    Span<byte> reference = stackalloc byte[5];
    foreach (var (v, expected) in known)
    {
        reference.Clear();
        if (!MessagePackPrimitives.TryWriteInt32Cascade(reference, v, out var w)
            || w != expected.Length
            || !reference[..w].SequenceEqual(expected))
        {
            ok = false;
            Console.WriteLine($"NG TryWriteInt32 known-answer value={v}: got [{string.Join(",", reference[..w].ToArray().Select(x => $"{x:x2}"))}] expected [{string.Join(",", expected.Select(x => $"{x:x2}"))}]");
        }
    }

    var candidates = new (string Name, TryWriteInt32Delegate Fn)[]
    {
        ("CascadeUnsafe", MessagePackPrimitives.TryWriteInt32CascadeUnsafe),
        ("FixintFirst", MessagePackPrimitives.TryWriteInt32FixintFirst),
        ("BranchlessTable", MessagePackPrimitives.TryWriteInt32BranchlessTable),
        ("Branchless2", MessagePackPrimitives.TryWriteInt32Branchless2),
        ("Hybrid", MessagePackPrimitives.TryWriteInt32Hybrid),
    };

    // boundary neighborhoods + random values, cross-checked against the reference.
    // candidates may write scratch bytes beyond bytesWritten (within destination),
    // so only the first bytesWritten bytes are compared.
    var testValues = new List<int>();
    int[] boundaries = [0, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue];
    foreach (var b in boundaries)
    {
        for (int d = -2; d <= 2; d++)
        {
            testValues.Add(unchecked(b + d));
        }
    }
    var rand = new Random(42);
    for (int i = 0; i < 100_000; i++)
    {
        testValues.Add(rand.Next(int.MinValue, int.MaxValue));
    }

    Span<byte> actual = stackalloc byte[5];
    foreach (var v in testValues)
    {
        MessagePackPrimitives.TryWriteInt32Cascade(reference, v, out var expectedLen);
        foreach (var (name, fn) in candidates)
        {
            // full-size destination
            actual.Clear();
            if (!fn(actual, v, out var len) || len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG TryWriteInt32 {name} value={v}: len={len} (expected {expectedLen})");
            }
            // tight destination (exactly the required bytes)
            actual.Clear();
            if (!fn(actual[..expectedLen], v, out len) || len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG TryWriteInt32 {name} value={v} tight destination failed");
            }
            // too-short destinations must return false
            for (int shortLen = 0; shortLen < expectedLen; shortLen++)
            {
                if (fn(actual[..shortLen], v, out _))
                {
                    ok = false;
                    Console.WriteLine($"NG TryWriteInt32 {name} value={v} destLen={shortLen} returned true");
                }
            }
        }
    }
    return ok;
}

static bool VerifyTryReadInt32()
{
    var ok = true;
    var readers = new (string Name, TryReadInt32Delegate Fn)[]
    {
        ("Cascade", MessagePackPrimitives.TryReadInt32Cascade),
        ("Branchless", MessagePackPrimitives.TryReadInt32Branchless),
        ("Branchless2", MessagePackPrimitives.TryReadInt32Branchless2),
        ("Branchless3", MessagePackPrimitives.TryReadInt32Branchless3),
        ("Hybrid", MessagePackPrimitives.TryReadInt32Hybrid),
    };

    // roundtrip: whatever the (already verified) writer emits must read back identically
    var testValues = new List<int>();
    int[] boundaries = [0, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue];
    foreach (var b in boundaries)
    {
        for (int d = -2; d <= 2; d++)
        {
            testValues.Add(unchecked(b + d));
        }
    }
    var rand = new Random(42);
    for (int i = 0; i < 100_000; i++)
    {
        testValues.Add(rand.Next(int.MinValue, int.MaxValue));
    }

    Span<byte> buf = stackalloc byte[5];
    foreach (var v in testValues)
    {
        buf.Clear();
        MessagePackPrimitives.TryWriteInt32Cascade(buf, v, out var written);
        foreach (var (name, fn) in readers)
        {
            // full 5-byte source (fast path)
            if (!fn(buf, out var value, out var consumed) || value != v || consumed != written)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32 {name} roundtrip value={v}: got {value} consumed={consumed} (expected consumed {written})");
            }
            // tight source (exactly the message; branchless falls back for len < 5)
            if (!fn(buf[..written], out value, out consumed) || value != v || consumed != written)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32 {name} tight source value={v}");
            }
            // truncated sources must return false
            for (int shortLen = 0; shortLen < written; shortLen++)
            {
                if (fn(buf[..shortLen], out _, out _))
                {
                    ok = false;
                    Console.WriteLine($"NG TryReadInt32 {name} value={v} srcLen={shortLen} returned true");
                }
            }
        }
    }

    // non-integer format codes must return false (cf/d3 are now valid wide formats, tested below)
    byte[] invalidCodes = [0x80, 0x8f, 0x90, 0x9f, 0xa0, 0xbf, 0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xca, 0xcb, 0xd4, 0xd9, 0xdc, 0xde, 0xdf];
    foreach (var code in invalidCodes)
    {
        buf.Clear();
        buf[0] = code;
        foreach (var (name, fn) in readers)
        {
            if (fn(buf, out _, out _))
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32 {name} code=0x{code:x2} returned true");
            }
        }
    }

    // uint32 above int.MaxValue must return false; exactly int.MaxValue must succeed
    byte[] overflow = [0xce, 0x80, 0x00, 0x00, 0x00];
    byte[] maxOk = [0xce, 0x7f, 0xff, 0xff, 0xff];
    foreach (var (name, fn) in readers)
    {
        if (fn(overflow, out _, out _))
        {
            ok = false;
            Console.WriteLine($"NG TryReadInt32 {name} uint32 overflow returned true");
        }
        if (!fn(maxOk, out var value, out var consumed) || value != int.MaxValue || consumed != 5)
        {
            ok = false;
            Console.WriteLine($"NG TryReadInt32 {name} uint32 int.MaxValue failed");
        }
    }

    // ---- DecodeResult (enum) candidate: same values through the 4-category contract ----
    {
        Span<byte> b = stackalloc byte[5];
        foreach (var v in testValues)
        {
            b.Clear();
            MessagePackPrimitives.TryWriteInt32Cascade(b, v, out var written);

            // full window: Success with exact value/length
            if (MessagePackPrimitives.TryReadInt32Dr(b, out var dv, out var dc) != MessagePackPrimitives.DecodeResult.Success
                || dv != v || dc != written)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32Dr roundtrip value={v}");
            }
            // tight window: still Success
            if (MessagePackPrimitives.TryReadInt32Dr(b[..written], out dv, out dc) != MessagePackPrimitives.DecodeResult.Success
                || dv != v || dc != written)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32Dr tight value={v}");
            }
            // truncated: InsufficientBuffer with consumed 0
            for (int shortLen = 0; shortLen < written; shortLen++)
            {
                if (MessagePackPrimitives.TryReadInt32Dr(b[..shortLen], out _, out dc) != MessagePackPrimitives.DecodeResult.InsufficientBuffer
                    || dc != 0)
                {
                    ok = false;
                    Console.WriteLine($"NG TryReadInt32Dr truncated value={v} srcLen={shortLen}");
                }
            }
        }

        // invalid codes: TokenMismatch with consumed 0 (regardless of window size)
        byte[] drInvalid = [0x80, 0x8f, 0x90, 0x9f, 0xa0, 0xbf, 0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xca, 0xcb, 0xd4, 0xd9, 0xdc, 0xde, 0xdf];
        foreach (var code in drInvalid)
        {
            b.Clear();
            b[0] = code;
            if (MessagePackPrimitives.TryReadInt32Dr(b, out _, out var dc) != MessagePackPrimitives.DecodeResult.TokenMismatch || dc != 0)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32Dr invalid code=0x{code:x2} (full window)");
            }
            if (MessagePackPrimitives.TryReadInt32Dr(b[..1], out _, out dc) != MessagePackPrimitives.DecodeResult.TokenMismatch || dc != 0)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32Dr invalid code=0x{code:x2} (careful window)");
            }
        }

        // uint32 above int.MaxValue: ValueOutOfRange with the true length (skippable)
        byte[] drOverflow = [0xce, 0x80, 0x00, 0x00, 0x00];
        if (MessagePackPrimitives.TryReadInt32Dr(drOverflow, out _, out var doc) != MessagePackPrimitives.DecodeResult.ValueOutOfRange || doc != 5)
        {
            ok = false;
            Console.WriteLine("NG TryReadInt32Dr uint32 overflow category");
        }

        // wide formats: in-range Success(9), out-of-range ValueOutOfRange(9), truncated Insufficient
        Span<byte> w9 = stackalloc byte[9];
        w9[0] = 0xd3;
        BinaryPrimitives.WriteInt64BigEndian(w9[1..], int.MinValue);
        if (MessagePackPrimitives.TryReadInt32Dr(w9, out var wv, out doc) != MessagePackPrimitives.DecodeResult.Success || wv != int.MinValue || doc != 9)
        {
            ok = false;
            Console.WriteLine("NG TryReadInt32Dr wide in-range");
        }
        BinaryPrimitives.WriteInt64BigEndian(w9[1..], long.MaxValue);
        if (MessagePackPrimitives.TryReadInt32Dr(w9, out _, out doc) != MessagePackPrimitives.DecodeResult.ValueOutOfRange || doc != 9)
        {
            ok = false;
            Console.WriteLine("NG TryReadInt32Dr wide out-of-range");
        }
        w9[0] = 0xcf;
        BinaryPrimitives.WriteUInt64BigEndian(w9[1..], ulong.MaxValue); // -1 as long: the sign trap
        if (MessagePackPrimitives.TryReadInt32Dr(w9, out _, out doc) != MessagePackPrimitives.DecodeResult.ValueOutOfRange || doc != 9)
        {
            ok = false;
            Console.WriteLine("NG TryReadInt32Dr uint64 sign-trap category");
        }
        for (int shortLen = 1; shortLen < 9; shortLen++)
        {
            if (MessagePackPrimitives.TryReadInt32Dr(w9[..shortLen], out _, out doc) != MessagePackPrimitives.DecodeResult.InsufficientBuffer || doc != 0)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32Dr wide truncated srcLen={shortLen}");
            }
        }
    }

    // wide formats int64(d3)/uint64(cf): non-minimal but legal encodings, accepted when in range
    Span<byte> wide = stackalloc byte[9];
    long[] wideInRange = [0, 1, -1, 42, -42, 127, 128, -32, -33, 65536, int.MaxValue, int.MinValue];
    foreach (var v in wideInRange)
    {
        wide[0] = 0xd3;
        BinaryPrimitives.WriteInt64BigEndian(wide[1..], v);
        foreach (var (name, fn) in readers)
        {
            if (!fn(wide, out var value, out var consumed) || value != (int)v || consumed != 9)
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32 {name} int64 wide value={v}");
            }
            // truncated wide messages must return false
            for (int shortLen = 1; shortLen < 9; shortLen++)
            {
                if (fn(wide[..shortLen], out _, out _))
                {
                    ok = false;
                    Console.WriteLine($"NG TryReadInt32 {name} int64 wide value={v} srcLen={shortLen} returned true");
                }
            }
        }
        if (v >= 0)
        {
            wide[0] = 0xcf;
            BinaryPrimitives.WriteUInt64BigEndian(wide[1..], (ulong)v);
            foreach (var (name, fn) in readers)
            {
                if (!fn(wide, out var value, out var consumed) || value != (int)v || consumed != 9)
                {
                    ok = false;
                    Console.WriteLine($"NG TryReadInt32 {name} uint64 wide value={v}");
                }
            }
        }
    }
    // out of int32 range must return false. ulong.MaxValue is the trap: reinterpreted
    // as long it is -1, which fits int32 — the sign check must catch it
    (byte Code, ulong Raw)[] wideOutOfRange =
    [
        (0xd3, 2147483648UL), (0xd3, unchecked((ulong)-2147483649L)),
        (0xd3, long.MaxValue), (0xd3, unchecked((ulong)long.MinValue)),
        (0xcf, 2147483648UL), (0xcf, 4294967296UL), (0xcf, long.MaxValue),
        (0xcf, 0x8000000000000000UL), (0xcf, ulong.MaxValue),
    ];
    foreach (var (codeByte, raw) in wideOutOfRange)
    {
        wide[0] = codeByte;
        BinaryPrimitives.WriteUInt64BigEndian(wide[1..], raw);
        foreach (var (name, fn) in readers)
        {
            if (fn(wide, out _, out _))
            {
                ok = false;
                Console.WriteLine($"NG TryReadInt32 {name} wide out-of-range code=0x{codeByte:x2} raw=0x{raw:x16} returned true");
            }
        }
    }
    return ok;
}

static bool VerifyUtf8Transcode()
{
    var ok = true;
    var candidates = new (string Name, WriteStringDelegate Fn)[]
    {
        ("GetBytes", Utf8TranscodeBenchmark.WriteStringGetBytes),
        ("TryGetBytes", Utf8TranscodeBenchmark.WriteStringTryGetBytes),
        ("FromUtf16", Utf8TranscodeBenchmark.WriteStringFromUtf16),
    };

    // char lengths around every header-class boundary, both the byteCount == length side
    // (ascii) and the straddle zones where 3*length crosses a class (japanese)
    int[] lengths = [0, 1, 10, 11, 12, 31, 32, 63, 85, 86, 127, 128, 255, 256, 21845, 21846, 65535, 65536];
    var contents = new (string Name, Func<int, string> Make)[]
    {
        ("Ascii", n => new string('x', n)),
        ("Japanese", n => new string('あ', n)),
        ("Mixed", n => string.Create(n, 0, (span, _) => { for (int i = 0; i < span.Length; i++) span[i] = (i % 3 == 0) ? 'あ' : 'x'; })),
        // lone surrogates: both APIs must agree on U+FFFD replacement
        ("LoneSurrogate", n => string.Create(n, 0, (span, _) => { for (int i = 0; i < span.Length; i++) span[i] = (i % 5 == 2) ? '\ud800' : 'x'; })),
    };

    foreach (var length in lengths)
    {
        foreach (var (contentName, make) in contents)
        {
            var value = make(length);
            var expected = new byte[3 * length + 5];
            var expectedLen = UltraMessagePack.MessagePackPrimitives.UnsafeWriteString(ref MemoryMarshal.GetArrayDataReference(expected), value);
            foreach (var (name, fn) in candidates)
            {
                var actual = new byte[3 * length + 5];
                var actualLen = fn(ref MemoryMarshal.GetArrayDataReference(actual), value);
                if (actualLen != expectedLen || !actual.AsSpan(0, actualLen).SequenceEqual(expected.AsSpan(0, expectedLen)))
                {
                    ok = false;
                    Console.WriteLine($"NG Utf8Transcode {name} length={length} content={contentName}: len={actualLen} (expected {expectedLen})");
                }
            }
        }
    }
    return ok;
}

static bool VerifyNarrowWrite()
{
    var ok = true;

    // exhaustive: every byte value against the current UnsafeWriteUInt32 routing.
    // sentinel bytes past each candidate's contract window (2 bytes) must stay untouched.
    var byteCandidates = new (string Name, WriteByteDelegate Fn, int Window)[]
    {
        ("ByteHybrid", NarrowWriteByteBenchmark.WriteByteHybrid, 2),
        ("ByteTernary", NarrowWriteByteBenchmark.WriteByteTernary, 2),
        ("UnsafeWriteByte", (ref byte d, byte v) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteByte(ref d, v), 2),
    };
    Span<byte> reference = stackalloc byte[8];
    Span<byte> actual = stackalloc byte[8];
    for (int v = 0; v <= byte.MaxValue; v++)
    {
        reference.Fill(0xAA);
        var expectedLen = UltraMessagePack.MessagePackPrimitives.UnsafeWriteUInt32(ref reference[0], (byte)v);
        foreach (var (name, fn, window) in byteCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], (byte)v);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: len={len} (expected {expectedLen})");
            }
            if (actual[window..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: wrote beyond {window}-byte window");
            }
        }
    }

    // exhaustive: every sbyte value against the current UnsafeWriteInt32 routing, 2-byte window
    var sbyteCandidates = new (string Name, WriteSByteDelegate Fn, int Window)[]
    {
        ("SByteHybrid", NarrowWriteSByteBenchmark.WriteSByteHybrid, 2),
        ("UnsafeWriteSByte", (ref byte d, sbyte v) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteSByte(ref d, v), 2),
    };
    for (int v = sbyte.MinValue; v <= sbyte.MaxValue; v++)
    {
        reference.Fill(0xAA);
        var expectedLen = UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref reference[0], (sbyte)v);
        foreach (var (name, fn, window) in sbyteCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], (sbyte)v);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: len={len} (expected {expectedLen})");
            }
            if (actual[window..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: wrote beyond {window}-byte window");
            }
        }
    }

    // exhaustive: every short value, 3-byte window
    var shortCandidates = new (string Name, WriteInt16Delegate Fn, int Window)[]
    {
        ("Int16TableMini", NarrowWriteInt16Benchmark.WriteInt16TableMini, 3),
        ("Int16HybridSignSplit", NarrowWriteInt16Benchmark.WriteInt16HybridSignSplit, 3),
        ("UnsafeWriteInt16", (ref byte d, short v) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref d, v), 3),
    };
    for (int v = short.MinValue; v <= short.MaxValue; v++)
    {
        reference.Fill(0xAA);
        var expectedLen = UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref reference[0], (short)v);
        foreach (var (name, fn, window) in shortCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], (short)v);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: len={len} (expected {expectedLen})");
            }
            if (actual[window..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: wrote beyond {window}-byte window");
            }
        }
    }

    // exhaustive: every ushort value, 3-byte window
    var ushortCandidates = new (string Name, WriteUInt16Delegate Fn, int Window)[]
    {
        ("UInt16Hybrid", NarrowWriteUInt16Benchmark.WriteUInt16Hybrid, 3),
        ("UInt16Ternary", NarrowWriteUInt16Benchmark.WriteUInt16Ternary, 3),
        ("UnsafeWriteUInt16", (ref byte d, ushort v) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteUInt16(ref d, v), 3),
    };
    for (int v = 0; v <= ushort.MaxValue; v++)
    {
        reference.Fill(0xAA);
        var expectedLen = UltraMessagePack.MessagePackPrimitives.UnsafeWriteUInt32(ref reference[0], (ushort)v);
        foreach (var (name, fn, window) in ushortCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], (ushort)v);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: len={len} (expected {expectedLen})");
            }
            if (actual[window..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG NarrowWrite {name} value={v}: wrote beyond {window}-byte window");
            }
        }
    }
    return ok;
}

static bool VerifyExtTimestampWrite()
{
    var ok = true;
    Span<byte> reference = stackalloc byte[24];
    Span<byte> actual = stackalloc byte[24];

    // ---- ext header ----

    // known-answer vectors pin down the cascade reference itself
    var extKnown = new (int DataLength, sbyte TypeCode, byte[] Expected)[]
    {
        (1, 5, [0xd4, 0x05]),
        (2, -1, [0xd5, 0xff]),
        (4, 127, [0xd6, 0x7f]),
        (8, -128, [0xd7, 0x80]),
        (16, 1, [0xd8, 0x01]),
        (0, 3, [0xc7, 0x00, 0x03]),
        (3, 7, [0xc7, 0x03, 0x07]),
        (17, -1, [0xc7, 0x11, 0xff]),
        (255, 2, [0xc7, 0xff, 0x02]),
        (256, 2, [0xc8, 0x01, 0x00, 0x02]),
        (65535, -5, [0xc8, 0xff, 0xff, 0xfb]),
        (65536, 9, [0xc9, 0x00, 0x01, 0x00, 0x00, 0x09]),
        (int.MaxValue, -1, [0xc9, 0x7f, 0xff, 0xff, 0xff, 0xff]),
    };
    foreach (var (dataLength, typeCode, expected) in extKnown)
    {
        reference.Clear();
        var w = ExtHeaderWriteBenchmark.WriteExtHeaderCascade(ref reference[0], typeCode, dataLength);
        if (w != expected.Length || !reference[..w].SequenceEqual(expected))
        {
            ok = false;
            Console.WriteLine($"NG ExtHeader known-answer dataLength={dataLength}: got [{string.Join(",", reference[..w].ToArray().Select(x => $"{x:x2}"))}] expected [{string.Join(",", expected.Select(x => $"{x:x2}"))}]");
        }
    }

    var extCandidates = new (string Name, WriteExtHeaderDelegate Fn)[]
    {
        ("Hybrid", ExtHeaderWriteBenchmark.WriteExtHeaderHybrid),
        ("Branchless", ExtHeaderWriteBenchmark.WriteExtHeaderBranchless),
        ("Library", (ref byte d, sbyte t, int l) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteExtHeader(ref d, t, l)),
    };

    // every length 0..40 (all fixext members and their neighbors), format boundaries, random
    var extLengths = new List<int>();
    for (int i = 0; i <= 40; i++) extLengths.Add(i);
    extLengths.AddRange([63, 64, 65, 127, 128, 129, 254, 255, 256, 257, 1000, 4095, 4096,
        65534, 65535, 65536, 65537, 1 << 20, (1 << 20) + 3, int.MaxValue - 1, int.MaxValue]);
    var rand = new Random(42);
    for (int i = 0; i < 100_000; i++)
    {
        extLengths.Add(rand.Next(int.MaxValue));
    }
    sbyte[] typeCodes = [-128, -1, 0, 1, 42, 127];

    foreach (var dataLength in extLengths)
    {
        var typeCode = typeCodes[rand.Next(typeCodes.Length)];
        reference.Fill(0xAA);
        var expectedLen = ExtHeaderWriteBenchmark.WriteExtHeaderCascade(ref reference[0], typeCode, dataLength);
        foreach (var (name, fn) in extCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], typeCode, dataLength);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG ExtHeader {name} dataLength={dataLength} typeCode={typeCode}: len={len} (expected {expectedLen})");
            }
            // scratch must stay within the 6-byte contract window
            if (actual[6..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG ExtHeader {name} dataLength={dataLength}: wrote beyond 6-byte window");
            }
        }
    }

    // ---- timestamp ----

    // known-answer vectors pin down the cascade reference (MessagePack-CSharp compatible)
    var tsKnown = new (DateTime Value, byte[] Expected)[]
    {
        (DateTime.UnixEpoch, [0xd6, 0xff, 0x00, 0x00, 0x00, 0x00]),
        (DateTime.UnixEpoch.AddSeconds(1), [0xd6, 0xff, 0x00, 0x00, 0x00, 0x01]),
        // 1 tick = 100ns: data64 = 100 << 34 = 0x00000190_00000000
        (DateTime.UnixEpoch.AddTicks(1), [0xd7, 0xff, 0x00, 0x00, 0x01, 0x90, 0x00, 0x00, 0x00, 0x00]),
        (DateTime.UnixEpoch.AddSeconds(-1), [0xc7, 0x0c, 0xff, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]),
        (DateTime.UnixEpoch.AddSeconds(4294967295), [0xd6, 0xff, 0xff, 0xff, 0xff, 0xff]),
        (DateTime.UnixEpoch.AddSeconds(4294967296), [0xd7, 0xff, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00]),
        (DateTime.UnixEpoch.AddSeconds((1L << 34) - 1), [0xd7, 0xff, 0x00, 0x00, 0x00, 0x03, 0xff, 0xff, 0xff, 0xff]),
        (DateTime.UnixEpoch.AddSeconds(1L << 34), [0xc7, 0x0c, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00]),
    };
    foreach (var (value, expected) in tsKnown)
    {
        reference.Clear();
        var w = TimestampWriteBenchmark.WriteTimestampCascade(ref reference[0], value);
        if (w != expected.Length || !reference[..w].SequenceEqual(expected))
        {
            ok = false;
            Console.WriteLine($"NG Timestamp known-answer value={value:O}: got [{string.Join(",", reference[..w].ToArray().Select(x => $"{x:x2}"))}] expected [{string.Join(",", expected.Select(x => $"{x:x2}"))}]");
        }
    }

    var tsCandidates = new (string Name, WriteTimestampDelegate Fn)[]
    {
        ("Hybrid96", TimestampWriteBenchmark.WriteTimestampHybrid96),
        ("Branchless", TimestampWriteBenchmark.WriteTimestampBranchless),
        ("Library", (ref byte d, DateTime v) => UltraMessagePack.MessagePackPrimitives.UnsafeWriteTimestamp(ref d, v)),
    };

    // format-boundary seconds (each ± neighbors) x tick fractions, plus range extremes and random
    var testTimes = new List<DateTime>();
    long[] boundarySeconds = [0, 1, 4294967295L, 4294967296L, (1L << 34) - 1, 1L << 34, -1, -62135596800L];
    int[] fractions = [0, 1, 9_999_999];
    foreach (var s in boundarySeconds)
    {
        foreach (var d in (long[])[-1, 0, 1])
        {
            foreach (var frac in fractions)
            {
                long ticks = (62135596800L + s + d) * TimeSpan.TicksPerSecond + frac;
                if (ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks)
                {
                    testTimes.Add(new DateTime(ticks, DateTimeKind.Utc));
                    testTimes.Add(new DateTime(ticks, DateTimeKind.Unspecified));
                }
            }
        }
    }
    testTimes.Add(DateTime.MinValue);
    testTimes.Add(DateTime.MaxValue);
    for (int i = 0; i < 100_000; i++)
    {
        testTimes.Add(new DateTime(rand.NextInt64(0, DateTime.MaxValue.Ticks + 1), rand.Next(2) == 0 ? DateTimeKind.Utc : DateTimeKind.Unspecified));
    }
    // Kind.Local goes through the same ToUniversalTime in every candidate; a few mid-range
    // values confirm the conversion path agrees (extremes skipped: clamping is tz-dependent)
    for (int i = 0; i < 100; i++)
    {
        testTimes.Add(new DateTime(DateTime.UnixEpoch.Ticks + rand.NextInt64(0, 2_000_000_000_000_000_000L), DateTimeKind.Local));
    }

    foreach (var value in testTimes)
    {
        reference.Fill(0xAA);
        var expectedLen = TimestampWriteBenchmark.WriteTimestampCascade(ref reference[0], value);
        foreach (var (name, fn) in tsCandidates)
        {
            actual.Fill(0xAA);
            var len = fn(ref actual[0], value);
            if (len != expectedLen || !actual[..len].SequenceEqual(reference[..expectedLen]))
            {
                ok = false;
                Console.WriteLine($"NG Timestamp {name} value={value:O} kind={value.Kind}: len={len} (expected {expectedLen})");
            }
            // scratch must stay within the 15-byte contract window
            if (actual[15..].IndexOfAnyExcept((byte)0xAA) >= 0)
            {
                ok = false;
                Console.WriteLine($"NG Timestamp {name} value={value:O}: wrote beyond 15-byte window");
            }
        }
    }
    return ok;
}

static bool VerifyHugeString()
{
    var ok = true;
    const int threshold = (int.MaxValue - 5) / 3; // MessagePackPrimitives.MaxWorstCaseStringLength

    try
    {
        // at the threshold the O(1) bound is still used and fits int exactly (3n+5 = int.MaxValue - 2)
        {
            var s = new string('x', threshold);
            var max = UltraMessagePack.MessagePackPrimitives.GetMaxStringByteCount(s);
            if (max != threshold * 3 + 5)
            {
                ok = false;
                Console.WriteLine($"NG HugeString sizing at threshold: {max} (expected {threshold * 3 + 5})");
            }
        }

        // one past the threshold, ascii: exact sizing (n+5) + WriteHugeString path
        {
            int n = threshold + 1;
            var s = new string('x', n);
            var max = UltraMessagePack.MessagePackPrimitives.GetMaxStringByteCount(s);
            if (max != n + 5)
            {
                ok = false;
                Console.WriteLine($"NG HugeString ascii sizing: {max} (expected {n + 5})");
            }
            var buffer = new byte[max];
            var written = UltraMessagePack.MessagePackPrimitives.UnsafeWriteString(ref MemoryMarshal.GetArrayDataReference(buffer), s);
            if (written != max || buffer[0] != 0xdb
                || BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(1)) != (uint)n
                || buffer[5] != (byte)'x' || buffer[written - 1] != (byte)'x')
            {
                ok = false;
                Console.WriteLine($"NG HugeString ascii write: written={written} header=0x{buffer[0]:x2}");
            }
        }

        // one past the threshold, non-ascii at both ends: byteCount = (n-2) + 2*3 = n+4
        {
            int n = threshold + 1;
            var s = string.Create(n, 0, (span, _) => { span.Fill('x'); span[0] = 'あ'; span[^1] = 'あ'; });
            int byteCount = n + 4;
            var max = UltraMessagePack.MessagePackPrimitives.GetMaxStringByteCount(s);
            if (max != byteCount + 5)
            {
                ok = false;
                Console.WriteLine($"NG HugeString mixed sizing: {max} (expected {byteCount + 5})");
            }
            var buffer = new byte[max];
            var written = UltraMessagePack.MessagePackPrimitives.UnsafeWriteString(ref MemoryMarshal.GetArrayDataReference(buffer), s);
            ReadOnlySpan<byte> hiragana = [0xe3, 0x81, 0x82]; // 'あ'
            if (written != max || buffer[0] != 0xdb
                || BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(1)) != (uint)byteCount
                || !buffer.AsSpan(5, 3).SequenceEqual(hiragana) || buffer[8] != (byte)'x'
                || buffer[written - 4] != (byte)'x' || !buffer.AsSpan(written - 3, 3).SequenceEqual(hiragana))
            {
                ok = false;
                Console.WriteLine($"NG HugeString mixed write: written={written} header=0x{buffer[0]:x2}");
            }
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("SKIP HugeString: not enough memory (needs roughly 4GB free)");
    }
    return ok;
}

delegate bool TryWriteInt32Delegate(Span<byte> destination, int value, out int bytesWritten);
delegate bool TryReadInt32Delegate(ReadOnlySpan<byte> source, out int value, out int bytesConsumed);
delegate int WriteStringDelegate(ref byte destination, string value);
delegate int WriteByteDelegate(ref byte destination, byte value);
delegate int WriteUInt16Delegate(ref byte destination, ushort value);
delegate int WriteSByteDelegate(ref byte destination, sbyte value);
delegate int WriteInt16Delegate(ref byte destination, short value);
delegate int WriteExtHeaderDelegate(ref byte destination, sbyte typeCode, int dataLength);
delegate int WriteTimestampDelegate(ref byte destination, DateTime value);

// two-segment ReadOnlySequence builder for VerifyNbOfficial's segment-straddling checks
sealed class NbSequenceSegment : System.Buffers.ReadOnlySequenceSegment<byte>
{
    NbSequenceSegment(ReadOnlyMemory<byte> memory, NbSequenceSegment? next, long runningIndex)
    {
        Memory = memory;
        Next = next;
        RunningIndex = runningIndex;
    }

    public static ReadOnlySequence<byte> CreateSplit(byte[] data, int splitAt)
    {
        var second = new NbSequenceSegment(data.AsMemory(splitAt), null, splitAt);
        var first = new NbSequenceSegment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }
}

public partial class Program { }

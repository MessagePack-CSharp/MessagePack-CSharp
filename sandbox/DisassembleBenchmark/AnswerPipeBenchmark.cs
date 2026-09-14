extern alias V3;
using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

// AnswerBenchmark with the byte[] endpoints swapped for System.IO.Pipelines: every row
// serializes one Answer into a PipeWriter, or deserializes one out of a PipeReader. Same
// graph, same data, same oracle checks; only the target changes.
//
// One in-memory Pipe (default options) is reused for every operation. A single Answer
// message is 1.7-3.7KB, far under the 64KB pause threshold and inside one 4KB segment, so
// every FlushAsync/ReadAsync here completes synchronously and the reader always sees one
// segment. The rows therefore measure what each library's pipe endpoint costs on top of its
// byte[] path (flush bookkeeping, boundary scanning, async state machines), not suspension
// cost; AsyncPipeBenchmark is the throttled counterpart where genuine awaits dominate.
//
// The writer is never completed between messages, the network-socket shape: a library
// must stop at the end of the value on its own. Setup runs every row once under a timeout
// and rejects any row that instead waits for end-of-stream, and any row that leaves bytes
// unconsumed in the pipe (which would accumulate across iterations). Two libraries fail
// that bar: Nerdbank.MessagePack's DeserializeAsync(PipeReader) and System.Text.Json's
// DeserializeAsync(PipeReader) read until the writer completes, so they run in a second
// "Completed" shape (one message per pipe lifetime, writer completed before the read,
// pipe Reset afterwards) with a V4 row and a floor row in the same shape for the pair.
// (--answer-pipe-check runs this setup alone, without BenchmarkDotNet.)
//
// Serialize rows write and then drain the pipe (TryRead + AdvanceTo); deserialize rows fill
// the pipe (Write + FlushAsync) and then read. Both costs are part of every row, and the
// Floor rows carry only that overhead so the per-library part is the difference.
//
// Row suffixes: Async = the library's own PipeWriter/PipeReader API; SyncFlush = V4's
// synchronous IBufferWriter entry plus an awaited flush (the async wrapper's cost is the
// gap to Async); Manual = IBufferWriter write + FlushAsync, or TryRead + synchronous
// ReadOnlySequence deserialize + AdvanceTo, for libraries with no pipe API (the manual
// deserialize rows trust the frame and skip boundary detection); StreamReader = v3's
// MessagePackStreamReader framing over PipeReader.AsStream, its only async surface that stops
// at a value boundary (v3's DeserializeAsync(Stream) reads to end-of-stream, so it cannot
// run here). Newtonsoft.Json has no buffer API and ShapeShift.Protobuf ships no pipe
// surface, so both sit this one out.
//
// MEASURED (i7-13700KF, ShortRun, same machine and data as AnswerBenchmark round 9, whose
// byte[] numbers are quoted in brackets; ns/op, floors included in every row):
//   Serialize:   floor 84 | V4 Async 407 / SyncFlush 397 [398] | Google.Protobuf 797 [978]
//                | mpcs 1198 [1043] | mpcs source-gen 1342 [1333] | Orleans 1588 [1339]
//                | Nerdbank Async 2189 [942] | protobuf-net 2242 [1781] | STJ 3128 [3142]
//                | ShapeShift.Json 3186 [3305] | ShapeShift.MsgPack Async 3332 [1560]
//                | STJ source-gen 3847 [3420]
//   Deserialize (socket shape, writer never completed):
//                floor 81 | V4 Manual 989 [880] | Google.Protobuf 1108 [981]
//                | V4 Async 1288 | mpcs 1744 [1643] | Nerdbank Manual 1839 [1758]
//                | Orleans 1884 [1694] | protobuf-net 1936 [2002] | mpcs source-gen 2122 [2050]
//                | mpcs StreamReader 2373 | ShapeShift.MsgPack Async 5575 [3424]
//                | ShapeShift.Json Async 9819 [7790]
//   Deserialize (completed shape, one message per pipe lifetime):
//                floor 129 | V4 Async 1052 | Nerdbank Async 2018 | STJ 6062 [5392]
//                | STJ source-gen 6167 [5285]
// Findings:
//   1. V4's pipe serialize is free: 407 total with an 84 floor means the write itself is
//      ~320, under the byte[] path's 398, because the pipe target skips the final
//      scratch-to-array copy. SyncFlush vs Async is noise, so the async wrapper costs nothing.
//      Google.Protobuf gains the same way (WriteTo(IBufferWriter) skips ToByteArray).
//   2. The two-pass deserialize costs ~300 ns on this 1.65KB graph: Async 1288 vs Manual 989,
//      while the Completed shape (no boundary scan, single-pass fast path) sits at 1052, so
//      the async state machine itself is ~15 ns and the rest is the boundary scan, roughly
//      0.18 ns/byte or ~3 ns per token. That is the price of framing without a length
//      prefix; v3's equivalent (MessagePackStreamReader, TrySkip over AsStream) pays 630.
//      Follow-up (BoundaryScannerBenchmark): the scanner loop was reshaped (jump-table
//      classification, in-place payload skip, single-segment entry), scan alone 267 -> 184 ns,
//      DeserializeV4Async 1288 -> 1235 (Manual 980, Completed 1052 in the same run). The
//      remaining ~250 ns is the load-to-address dependency of every string length, the floor
//      of any byte-serial scan; only skipping the scan (optimistic single pass, or a length
//      prefix) can close the rest.
//   3. True-async serializers pay heavily for the pipe target: Nerdbank 942 -> 2189 (2.3x,
//      plus 144 B of state-machine allocation) and ShapeShift.MsgPack 1560 -> 3332, against
//      V4's sync-write-plus-flush design staying flat. V4's serialize lead over Nerdbank
//      widens from 2.4x (byte[]) to 5.4x (pipe); on deserialize Nerdbank's async path is
//      1.1x its own sync path and 1.9x V4's Completed row.
//   4. Nerdbank's and System.Text.Json's DeserializeAsync(PipeReader) cannot frame messages
//      on a live connection at all (they wait for the writer to complete), which is why they
//      only appear in the Completed shape. ShapeShift's async reader does stop at the value
//      boundary but pays ~2150 ns for it (5575 vs its 3424 byte[] path).
//   5. IBufferWriter+FlushAsync rows (mpcs, protobuf-net, Orleans) pay 150-460 ns over their
//      byte[] paths for pipe segment bookkeeping; the drain floor is 84 of that.
//   6. Framing (ns, same run as the scanner follow-up, sandbox processor): Deserialize Async 1235
//      | Framed 1041 (alias) / 1060 (copy) | Completed 1037 | Manual 964; Serialize Async 418 |
//      Framed 471. The envelope turns the boundary scan into one header read, landing the socket
//      shape on the Completed shape's number; the remaining ~75 over Manual is the async
//      machinery plus TryDecode. Serialize pays ~55 ns for the whole-message staging the seam
//      implies. MOVED TO CORE 2026-09-08 as FramingProcessor (ext 95, WithFraming()); its
//      TryDecode returns DecodedMessage.SourceSlice, the alias shape, now sanctioned by the seam.
//      Core processor, MediumRun: Deserialize Async 1353 | Framed 1070 | Completed 1081 ns.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerPipeBenchmark
{
    readonly Pipe pipe = new(new PipeOptions(useSynchronizationContext: false));
    V3::MessagePack.MessagePackStreamReader mpcsStreamReader = default!;

    Answer answer = default!;
    V3GeneratedModels.Answer answerV3SourceGen = default!;
    AnswerProto.Answer protoAnswer = default!;
    byte[] mpcsPayload = default!;
    byte[] v4Payload = default!;
    byte[] nbPayload = default!;
    byte[] pbPayload = default!;
    byte[] stjPayload = default!;
    byte[] stjSgPayload = default!;
    byte[] orleansPayload = default!;
    byte[] gpbPayload = default!;
    byte[] ssMsgPackPayload = default!;
    byte[] ssJsonPayload = default!;

    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();
    readonly ShapeShift.MsgPack.MsgPackSerializer ssMsgPack = new();
    readonly ShapeShift.Json.JsonSerializer ssJson = new();
    readonly Orleans.Serialization.Serializer orleans =
        new ServiceCollection().AddSerializer().BuildServiceProvider().GetRequiredService<Orleans.Serialization.Serializer>();

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();
        mpcsPayload = V3::MessagePack.MessagePackSerializer.Serialize(answer);
        answerV3SourceGen = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(mpcsPayload, V3GeneratedModels.V3SourceGen.Options);
        protoAnswer = AnswerProtoMap.ToProto(answer);
        mpcsStreamReader = new V3::MessagePack.MessagePackStreamReader(pipe.Reader.AsStream(leaveOpen: true), leaveOpen: true);

        // every serialize row once: the bytes that came through the pipe are the payloads
        // the deserialize rows read back, and the msgpack rows must match the v3 oracle
        v4Payload = Capture(nameof(SerializeV4Async), WriteV4Async);
        Expect(v4Payload.AsSpan().SequenceEqual(mpcsPayload), "V4 pipe bytes != MessagePack-CSharp oracle");
        Expect(Capture(nameof(SerializeV4SyncFlush), WriteV4SyncFlush).AsSpan().SequenceEqual(mpcsPayload), "V4 sync+flush pipe bytes != oracle");
        Expect(Capture(nameof(SerializeMpcsManual), WriteMpcs).AsSpan().SequenceEqual(mpcsPayload), "MessagePack-CSharp pipe bytes != byte[] bytes");
        Expect(Capture(nameof(SerializeMpcsSourceGenManual), WriteMpcsSourceGen).AsSpan().SequenceEqual(mpcsPayload), "MessagePack-CSharp source-gen pipe bytes != oracle");
        nbPayload = Capture(nameof(SerializeNerdbankAsync), WriteNerdbank);
        pbPayload = Capture(nameof(SerializeProtobufNetManual), WriteProtobufNet);
        stjPayload = Capture(nameof(SerializeSystemTextJsonAsync), WriteSystemTextJson);
        stjSgPayload = Capture(nameof(SerializeSystemTextJsonSourceGenAsync), WriteSystemTextJsonSourceGen);
        Expect(stjSgPayload.AsSpan().SequenceEqual(stjPayload), "System.Text.Json source-gen pipe bytes != reflection bytes");
        orleansPayload = Capture(nameof(SerializeOrleansManual), WriteOrleans);
        gpbPayload = Capture(nameof(SerializeGoogleProtobufManual), WriteGoogleProtobuf);
        ssMsgPackPayload = Capture(nameof(SerializeShapeShiftMsgPackAsync), WriteShapeShiftMsgPack);
        Expect(ssMsgPackPayload is [0xdc, 0x00, 0x18, ..], "ShapeShift positional contract did not produce array16(24)");
        ssJsonPayload = Capture(nameof(SerializeShapeShiftJsonAsync), WriteShapeShiftJson);
        v4FramedPayload = Capture(nameof(SerializeV4FramedAsync), WriteV4Framed);
        Expect(v4FramedPayload.Length == v4Payload.Length + 6 && v4FramedPayload[0] == 0xc9 && v4FramedPayload.AsSpan(6).SequenceEqual(v4Payload), "framed envelope is not ext32 header + the plain V4 bytes");

        // every deserialize row once: must stop at the value boundary, consume exactly the
        // message, and roundtrip through the oracle
        VerifyRow(nameof(DeserializeV4Async), DeserializeV4Async);
        VerifyRow(nameof(DeserializeV4Manual), DeserializeV4Manual);
        VerifyRow(nameof(DeserializeMpcsManual), DeserializeMpcsManual);
        VerifyRow(nameof(DeserializeMpcsStreamReader), DeserializeMpcsStreamReader);
        VerifyRow(nameof(DeserializeMpcsSourceGenManual), async () => V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(V3::MessagePack.MessagePackSerializer.Serialize(await DeserializeMpcsSourceGenManual(), V3GeneratedModels.V3SourceGen.Options)));
        VerifyRow(nameof(DeserializeNerdbankManual), DeserializeNerdbankManual);
        VerifyRow(nameof(DeserializeProtobufNetManual), DeserializeProtobufNetManual);
        VerifyRow(nameof(DeserializeOrleansManual), DeserializeOrleansManual);
        VerifyRow(nameof(DeserializeGoogleProtobufManual), async () => AnswerProtoMap.ToPoco(await DeserializeGoogleProtobufManual()));
        VerifyRow(nameof(DeserializeShapeShiftMsgPackAsync), DeserializeShapeShiftMsgPackAsync);
        VerifyRow(nameof(DeserializeShapeShiftJsonAsync), DeserializeShapeShiftJsonAsync);
        VerifyRow(nameof(DeserializeV4FramedAsync), DeserializeV4FramedAsync);
        // the completed-writer shape (one message per pipe lifetime)
        VerifyRow(nameof(DeserializeV4AsyncCompleted), DeserializeV4AsyncCompleted);
        VerifyRow(nameof(DeserializeNerdbankAsyncCompleted), DeserializeNerdbankAsyncCompleted);
        VerifyRow(nameof(DeserializeSystemTextJsonAsyncCompleted), DeserializeSystemTextJsonAsyncCompleted);
        VerifyRow(nameof(DeserializeSystemTextJsonSourceGenAsyncCompleted), DeserializeSystemTextJsonSourceGenAsyncCompleted);
        if (Run(nameof(FloorReadAdvance), FloorReadAdvance)) EnsurePipeEmpty(nameof(FloorReadAdvance));
        if (Run(nameof(FloorReadAdvanceCompleted), FloorReadAdvanceCompleted)) EnsurePipeEmpty(nameof(FloorReadAdvanceCompleted));
        if (Run(nameof(FloorRawCopy), FloorRawCopy)) EnsurePipeEmpty(nameof(FloorRawCopy));

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"AnswerPipeBenchmark setup: {failures.Count} row(s) failed\n  " + string.Join("\n  ", failures));
        }
    }

    #region setup helpers

    // every row is checked and every failure is reported together, so one run shows the
    // whole picture instead of stopping at the first library that misbehaves
    readonly List<string> failures = [];
    static readonly TimeSpan RowTimeout = TimeSpan.FromSeconds(5);

    void Expect(bool condition, string message)
    {
        if (!condition) failures.Add("verify failed: " + message);
    }

    bool Run(string name, Func<Task> operation)
    {
        Task task;
        try
        {
            task = operation();
            task.WaitAsync(RowTimeout).GetAwaiter().GetResult();
            return true;
        }
        catch (TimeoutException)
        {
            failures.Add($"{name} did not complete: the library waits for the writer to complete (reads to end-of-stream) instead of stopping at the end of the value");
            ResetPipe();
            return false;
        }
        catch (Exception exception)
        {
            failures.Add($"{name} failed: {exception.Message}");
            ResetPipe();
            return false;
        }
    }

    // a timed-out row still holds a pending ReadAsync on the reader; completing the writer
    // releases it (the library then finishes or throws), after which the pipe can be reset
    // for the remaining rows
    void ResetPipe()
    {
        pipe.Writer.Complete();
        try
        {
            // let the released operation settle before completing the reader under it
            Task.Delay(100).GetAwaiter().GetResult();
            pipe.Reader.Complete();
        }
        catch
        {
        }
        pipe.Reset();
    }

    // runs a write, then takes the flushed bytes out of the pipe; a library that never
    // flushes leaves the pipe empty and is reported here
    byte[] Capture(string name, Func<Task> write)
    {
        if (!Run(name, write)) return [];
        if (!pipe.Reader.TryRead(out var result) || result.Buffer.IsEmpty)
        {
            failures.Add($"{name} left nothing readable in the pipe: the library did not flush");
            return [];
        }
        var bytes = result.Buffer.ToArray();
        pipe.Reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }

    void VerifyRow(string name, Func<Task<Answer>> read)
    {
        Answer back = default!;
        if (!Run(name, async () => back = await read())) return;
        EnsurePipeEmpty(name);
        // field-by-field equality via the oracle: re-serialize with MessagePack-CSharp and
        // demand byte identity with the original payload
        var bytes = V3::MessagePack.MessagePackSerializer.Serialize(back);
        Expect(bytes.AsSpan().SequenceEqual(mpcsPayload), $"{name} roundtrip");
    }

    void EnsurePipeEmpty(string name)
    {
        if (pipe.Reader.TryRead(out var result))
        {
            var leftover = result.Buffer.Length;
            pipe.Reader.AdvanceTo(result.Buffer.End);
            if (leftover != 0) failures.Add($"{name} left {leftover} bytes unconsumed in the pipe");
        }
    }

    #endregion

    #region pipe plumbing shared by the rows

    // after a serialize row: take the message out so the pipe stays empty across iterations
    void Drain()
    {
        if (!pipe.Reader.TryRead(out var result)) throw new InvalidOperationException("nothing to drain");
        pipe.Reader.AdvanceTo(result.Buffer.End);
    }

    // before a deserialize row: one message into the pipe; the flush completes synchronously
    // because nothing here approaches the pause threshold
    void Fill(byte[] payload)
    {
        pipe.Writer.Write(payload);
        var flush = pipe.Writer.FlushAsync();
        if (!flush.IsCompleted) throw new InvalidOperationException("backpressure engaged on a single message");
        flush.GetAwaiter().GetResult();
    }

    // a fully buffered single-segment read: the shape every manual deserialize row sees
    ReadOnlySequence<byte> Take()
    {
        if (!pipe.Reader.TryRead(out var result)) throw new InvalidOperationException("nothing to read");
        return result.Buffer;
    }

    #endregion

    #region serialize

    // the pipe overhead alone: memcpy of the finished bytes + flush + drain
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task FloorRawCopy()
    {
        pipe.Writer.Write(v4Payload);
        await pipe.Writer.FlushAsync();
        Drain();
    }

    Task WriteV4Async() => MessagePack.MessagePackSerializer.SerializeAsync(pipe.Writer, answer);
    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public async Task SerializeV4Async() { await WriteV4Async(); Drain(); }

    async Task WriteV4SyncFlush()
    {
        MessagePack.MessagePackSerializer.Serialize(pipe.Writer, answer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeV4SyncFlush() { await WriteV4SyncFlush(); Drain(); }

    async Task WriteMpcs()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(pipe.Writer, answer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeMpcsManual() { await WriteMpcs(); Drain(); }

    async Task WriteMpcsSourceGen()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(pipe.Writer, answerV3SourceGen, V3GeneratedModels.V3SourceGen.Options);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeMpcsSourceGenManual() { await WriteMpcsSourceGen(); Drain(); }

    // Nerdbank.MessagePack writes into the PipeWriter but leaves the flush to the caller
    // (Setup caught the pipe empty without it), so the row carries an explicit FlushAsync
    async Task WriteNerdbank()
    {
        await nb.SerializeAsync(pipe.Writer, answer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeNerdbankAsync() { await WriteNerdbank(); Drain(); }

    async Task WriteProtobufNet()
    {
        ProtoBuf.Serializer.Serialize(pipe.Writer, answer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeProtobufNetManual() { await WriteProtobufNet(); Drain(); }

    Task WriteSystemTextJson() => System.Text.Json.JsonSerializer.SerializeAsync(pipe.Writer, answer);
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeSystemTextJsonAsync() { await WriteSystemTextJson(); Drain(); }

    Task WriteSystemTextJsonSourceGen() => System.Text.Json.JsonSerializer.SerializeAsync(pipe.Writer, answer, AnswerJsonContext.Default.Answer);
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeSystemTextJsonSourceGenAsync() { await WriteSystemTextJsonSourceGen(); Drain(); }

    async Task WriteOrleans()
    {
        orleans.Serialize(answer, pipe.Writer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeOrleansManual() { await WriteOrleans(); Drain(); }

    async Task WriteGoogleProtobuf()
    {
        Google.Protobuf.MessageExtensions.WriteTo(protoAnswer, pipe.Writer);
        await pipe.Writer.FlushAsync();
    }
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeGoogleProtobufManual() { await WriteGoogleProtobuf(); Drain(); }

    Task WriteShapeShiftMsgPack() => ssMsgPack.SerializeAsync(pipe.Writer, answer).AsTask();
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeShapeShiftMsgPackAsync() { await ssMsgPack.SerializeAsync(pipe.Writer, answer); Drain(); }

    Task WriteShapeShiftJson() => ssJson.SerializeAsync(pipe.Writer, answer).AsTask();
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeShapeShiftJsonAsync() { await ssJson.SerializeAsync(pipe.Writer, answer); Drain(); }

    #endregion

    #region deserialize

    // the pipe overhead alone: fill + TryRead + AdvanceTo, no parsing
    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task FloorReadAdvance()
    {
        Fill(v4Payload);
        var buffer = Take();
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.CompletedTask;
    }

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public async Task<Answer> DeserializeV4Async()
    {
        Fill(v4Payload);
        return await MessagePack.MessagePackSerializer.DeserializeAsync<Answer>(pipe.Reader);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<Answer> DeserializeV4Manual()
    {
        Fill(v4Payload);
        var buffer = Take();
        var value = MessagePack.MessagePackSerializer.Deserialize<Answer>(in buffer)!;
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<Answer> DeserializeMpcsManual()
    {
        Fill(mpcsPayload);
        var buffer = Take();
        var value = V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(buffer);
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeMpcsStreamReader()
    {
        Fill(mpcsPayload);
        var message = await mpcsStreamReader.ReadAsync(CancellationToken.None) ?? throw new InvalidOperationException("stream reader saw no message");
        return V3::MessagePack.MessagePackSerializer.Deserialize<Answer>(message);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<V3GeneratedModels.Answer> DeserializeMpcsSourceGenManual()
    {
        Fill(mpcsPayload);
        var buffer = Take();
        var value = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.Answer>(buffer, V3GeneratedModels.V3SourceGen.Options);
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<Answer> DeserializeNerdbankManual()
    {
        Fill(nbPayload);
        var buffer = Take();
        var value = nb.Deserialize<Answer>(buffer)!;
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<Answer> DeserializeProtobufNetManual()
    {
        Fill(pbPayload);
        var buffer = Take();
        var value = ProtoBuf.Serializer.Deserialize<Answer>(buffer);
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<Answer> DeserializeOrleansManual()
    {
        Fill(orleansPayload);
        var buffer = Take();
        var value = orleans.Deserialize<Answer>(buffer);
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task<AnswerProto.Answer> DeserializeGoogleProtobufManual()
    {
        Fill(gpbPayload);
        var buffer = Take();
        var value = AnswerProto.Answer.Parser.ParseFrom(buffer);
        pipe.Reader.AdvanceTo(buffer.End);
        return Task.FromResult(value);
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeShapeShiftMsgPackAsync()
    {
        Fill(ssMsgPackPayload);
        return (await ssMsgPack.DeserializeAsync<Answer>(pipe.Reader, maxBufferedSize: 1 << 20))!;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeShapeShiftJsonAsync()
    {
        Fill(ssJsonPayload);
        return (await ssJson.DeserializeAsync<Answer>(pipe.Reader, maxBufferedSize: 1 << 20))!;
    }

    #endregion

    #region framed (length-prefixed) through the core FramingProcessor

    // The core FramingProcessor (ext 95 envelope): the boundary scanner reads one header and skips the payload
    // arithmetically instead of walking ~100 tokens, and TryDecode hands the serializer a slice of the source
    // (DecodedMessage.SourceSlice, no copy). Serialize still stages the message and copies it once into the pipe,
    // since the seam is whole-message.
    static readonly MessagePack.MessagePackSerializerOptions framed = MessagePack.FramingMessagePackOptionsExtensions.WithFraming(MessagePack.MessagePackSerializerOptions.Default);
    byte[] v4FramedPayload = default!;

    Task WriteV4Framed() => MessagePack.MessagePackSerializer.SerializeAsync(pipe.Writer, answer, framed);
    [BenchmarkCategory("Serialize"), Benchmark]
    public async Task SerializeV4FramedAsync() { await WriteV4Framed(); Drain(); }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeV4FramedAsync()
    {
        Fill(v4FramedPayload);
        return await MessagePack.MessagePackSerializer.DeserializeAsync<Answer>(pipe.Reader, framed);
    }

    #endregion

    #region deserialize, completed-writer shape

    // Nerdbank.MessagePack's DeserializeAsync(PipeReader) and System.Text.Json's
    // DeserializeAsync(PipeReader) both keep reading until the writer completes (Setup timed
    // them out on the socket shape above), so their rows run one message per pipe lifetime:
    // write, complete the writer, read, complete the reader, Reset. V4 gets the same shape
    // as the pair; a completed pipe lets it skip the boundary scan (the buffered bytes are
    // the whole message by definition), so this is also V4's best case.

    void FillCompleted(byte[] payload)
    {
        pipe.Writer.Write(payload);
        pipe.Writer.Complete();
    }

    void FinishCompleted()
    {
        pipe.Reader.Complete();
        pipe.Reset();
    }

    // the completed-shape overhead alone: fill + complete + TryRead + AdvanceTo + complete + Reset
    [BenchmarkCategory("Deserialize"), Benchmark]
    public Task FloorReadAdvanceCompleted()
    {
        FillCompleted(v4Payload);
        var buffer = Take();
        pipe.Reader.AdvanceTo(buffer.End);
        FinishCompleted();
        return Task.CompletedTask;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeV4AsyncCompleted()
    {
        FillCompleted(v4Payload);
        var value = await MessagePack.MessagePackSerializer.DeserializeAsync<Answer>(pipe.Reader);
        FinishCompleted();
        return value;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeNerdbankAsyncCompleted()
    {
        FillCompleted(nbPayload);
        var value = (await nb.DeserializeAsync<Answer>(pipe.Reader))!;
        FinishCompleted();
        return value;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeSystemTextJsonAsyncCompleted()
    {
        FillCompleted(stjPayload);
        var value = (await System.Text.Json.JsonSerializer.DeserializeAsync<Answer>(pipe.Reader))!;
        FinishCompleted();
        return value;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public async Task<Answer> DeserializeSystemTextJsonSourceGenAsyncCompleted()
    {
        FillCompleted(stjSgPayload);
        var value = (await System.Text.Json.JsonSerializer.DeserializeAsync(pipe.Reader, AnswerJsonContext.Default.Answer))!;
        FinishCompleted();
        return value;
    }

    #endregion
}

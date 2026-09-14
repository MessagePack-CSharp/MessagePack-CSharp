extern alias V3;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using static MessagePack.MessagePackPrimitives;

// Round 8 (concluded): three ArrayPoolListWriteBuffer / IWriteBuffer hypotheses from the
// entry-asm analysis, all byte-verified and all measured EQUAL (0.90-1.11 across 3 runs,
// no consistent direction; ShortRun 10% rule):
//   BatchFormatter (kept below)   - one reservation + register-resident offset + one
//                                   Advance instead of per-value GetReference/Advance
//   SlimBuffer / SlimStrict (removed) - spill state evicted from the ref struct (prolog
//                                   GC-ref zeroing 5x zmm -> 1x, verified in asm) and a
//                                   "sizeHint >= 1" contract (one capacity branch/write).
//                                   No measured effect: ~340B of wide-store zeroing is
//                                   ~3 cycles, predicted-not-taken branches and stack
//                                   ping-pong hide behind the OoO core. Also rejected on
//                                   semantics: SpillState added an alloc on spill, and
//                                   sizeHint >= 1 breaks "give me everything" callers.
// Entry bookkeeping is at its measured ceiling; the remaining real axis is per-byte work
// (nested-formatter dispatch removal / SIMD leaf formatters).
// Both entries resolve through options.Resolver.GetFormatter (the table path) so
// formatter resolution cost is identical; the serializers differ only in which BenchPerson
// formatter their resolver hands out.
public class WriteBufferBatchBenchmark
{
    BenchPerson person = default!;
    BenchPerson bigPerson = default!;
    MessagePack.MessagePackSerializerOptions normal = default!;
    MessagePack.MessagePackSerializerOptions batch = default!;

    [GlobalSetup]
    public void Setup()
    {
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        // 5000 chars > 1KB scratch: exercises the slim buffers' spill path
        bigPerson = new BenchPerson { Id = -98765, Name = new string('あ', 5000), Score = -1.5 };
        normal = new MessagePack.MessagePackSerializerOptions(new MessagePack.MessagePackFormatterResolver(new BenchPersonFormatterFactory()));
        batch = new MessagePack.MessagePackSerializerOptions(new MessagePack.MessagePackFormatterResolver(new BenchPersonBatchFormatterFactory()));

        foreach (var p in new[] { person, bigPerson })
        {
            var expected = V3::MessagePack.MessagePackSerializer.Serialize(p);
            foreach (var (name, actual) in new (string, byte[])[]
            {
                ("Current", EntryShapeProbe.SerializeUniformTable(normal, p)),
                ("BatchFormatter", EntryShapeProbe.SerializeUniformTable(batch, p)),
            })
            {
                if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name} (Name.Length={p.Name!.Length})");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public byte[] Current() => EntryShapeProbe.SerializeUniformTable(normal, person);

    [Benchmark]
    public byte[] BatchFormatter() => EntryShapeProbe.SerializeUniformTable(batch, person);
}

// hands out exactly one formatter (BenchPerson); keeps the two probe serializers'
// resolution chains independent of the shared Dynamic singleton

// one reservation for the summed worst case, register-resident offset, one Advance
public sealed class BenchPersonBatchFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, BenchPerson>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref MessagePack.SerializeState state, BenchPerson value)
    {
        var name = value.Name;
        var max = MaxArrayHeaderLength + MaxInt32Length + GetMaxStringByteCount(name) + MaxFloat64Length;
        ref byte d = ref buffer.GetReference(max);
        var offset = UnsafeWriteArrayHeader(ref d, 3);
        offset += UnsafeWriteInt32(ref Unsafe.Add(ref d, offset), value.Id);
        offset += UnsafeWriteString(ref Unsafe.Add(ref d, offset), name);
        offset += UnsafeWriteDouble(ref Unsafe.Add(ref d, offset), value.Score);
        buffer.Advance(offset);
    }

    public void Deserialize(ref TReadBuffer buffer, ref MessagePack.DeserializeState state, ref BenchPerson value)
    {
        throw new NotImplementedException();
    }
}

public sealed partial class BenchPersonBatchFormatterFactory : MessagePack.MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new BenchPersonBatchFormatter<TWriteBuffer, TReadBuffer>();
    }
}

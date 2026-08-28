// MsgPack104: hand-written baseline formatter; the direct primitive calls are the benchmarked subject.
#pragma warning disable MsgPack104

extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using MessagePack;

// The contractless member-table formatter (construction-time delegate distillation, no
// Reflection.Emit) vs a hand-written map-format formatter (the ceiling: what a generated
// map formatter would cost) vs MessagePack-CSharp's ContractlessStandardResolver
// (Reflection.Emit, the incumbent). All three write the same wire: a 3-entry map of
// member-name strings. The open question this benchmark answers: how much does
// "delegate + virtual call per member" pay over inlined member access, and does
// rejecting Emit keep us competitive with v3's emitted formatters?
//
// MEASURED (i7-13700KF, ShortRun, single ContractlessPerson{int,string,double}, ns/op,
// round-1 formatter, clean machine):
//   Serialize:   Contractless 32.2 | HandMap 23.5 | v3 emit 41.9  (allocs identical)
//   Deserialize: Contractless 43.8 | HandMap 35.4 | v3 emit 64.0
// VERDICT: HandMap is the honest proxy for "UMP with Emit" (emit would generate roughly
// the hand-written shape), so the true cost of rejecting whole-formatter Emit is ~8.5ns
// per object = ~2.8ns per member (virtual + delegate + non-inlined accessor). The v3
// numbers do NOT measure emit-vs-delegate — UMP's faster base (buffer/entry layer) is
// mixed in; they only show v3 contractless users migrating here do not get slower. For
// context, the source-generated array-format BenchPerson is 18.3/25.5 — the rest of the
// gap is the map format itself (keys on the wire).
//
// HISTORY: the round-2 ctor-matching refactor regressed property deserialize 2.28x over
// HandMap (the boxed argument-state machinery inlined into the populate hot path); fixed
// with NoInlining on DeserializeWithArguments + AggressiveInlining on the key matcher +
// a cold throw helper — ratio restored to 1.20x. Field POCOs (SerializeFields /
// DeserializeFields, the emitted single-ldfld/stfld FieldSlot accessors) measure at
// parity with the property slots, replacing the earlier boxed-FieldInfo path. Later runs
// were taken on a degraded machine (~2x on code-identical baselines) — RATIOS above are
// trustworthy, re-measure absolutes on a clean machine before publishing numbers.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ContractlessBenchmark
{
    ContractlessPerson person = default!;
    byte[] payload = default!;
    MessagePack.MessagePackSerializerOptions contractless = default!;
    MessagePack.MessagePackSerializerOptions handMap = default!;
    V3::MessagePack.MessagePackSerializerOptions v3Contractless = default!;

    [GlobalSetup]
    public void Setup()
    {
        person = new ContractlessPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };

        contractless = new MessagePack.MessagePackSerializerOptions(
            new MessagePackFormatterResolver([MessagePack.MessagePackFormatterFactory.Default.WithContractless()]));
        handMap = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new HandMapPersonFormatterFactory(),
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
        ]));
        v3Contractless = V3::MessagePack.Resolvers.ContractlessStandardResolver.Options;

        payload = MessagePack.MessagePackSerializer.Serialize(person, contractless);

        // verify: hand-written bytes must be identical (same member order, same wire);
        // v3 must cross-read ours and vice versa (byte order is v3's business)
        if (!MessagePack.MessagePackSerializer.Serialize(person, handMap).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: handmap bytes vs contractless");
        var fromV3 = MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(
            V3::MessagePack.MessagePackSerializer.Serialize(person, v3Contractless), contractless)!;
        if (fromV3.Id != person.Id || fromV3.Name != person.Name || fromV3.Score != person.Score) throw new InvalidOperationException("verify failed: v3 -> ump");
        var fromUmp = V3::MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(payload, v3Contractless);
        if (fromUmp.Id != person.Id || fromUmp.Name != person.Name || fromUmp.Score != person.Score) throw new InvalidOperationException("verify failed: ump -> v3");
        var roundTrip = MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(payload, handMap)!;
        if (roundTrip.Id != person.Id || roundTrip.Name != person.Name || roundTrip.Score != person.Score) throw new InvalidOperationException("verify failed: handmap roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeContractless() => MessagePack.MessagePackSerializer.Serialize(person, contractless);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeHandMap() => MessagePack.MessagePackSerializer.Serialize(person, handMap);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3Contractless() => V3::MessagePack.MessagePackSerializer.Serialize(person, v3Contractless);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public ContractlessPerson DeserializeContractless() => MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(payload, contractless)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public ContractlessPerson DeserializeHandMap() => MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(payload, handMap)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public ContractlessPerson DeserializeV3Contractless() => V3::MessagePack.MessagePackSerializer.Deserialize<ContractlessPerson>(payload, v3Contractless);

    // field members ride FieldSlot (the emitted single-ldfld/stfld accessors); the
    // property POCO above rides the CreateDelegate slots — same member shapes otherwise
    FieldPerson fieldPerson;
    byte[] fieldPayload = default!;

    [GlobalSetup(Targets = [nameof(SerializeContractlessFields), nameof(DeserializeContractlessFields), nameof(SerializeV3ContractlessFields), nameof(DeserializeV3ContractlessFields)])]
    public void SetupFields()
    {
        Setup();
        fieldPerson = new FieldPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        fieldPayload = MessagePack.MessagePackSerializer.Serialize(fieldPerson, contractless);

        var back = MessagePack.MessagePackSerializer.Deserialize<FieldPerson>(fieldPayload, contractless);
        if (back.Id != fieldPerson.Id || back.Name != fieldPerson.Name || back.Score != fieldPerson.Score) throw new InvalidOperationException("verify failed: field poco roundtrip");
        var v3Back = V3::MessagePack.MessagePackSerializer.Deserialize<FieldPerson>(fieldPayload, v3Contractless);
        if (v3Back.Id != fieldPerson.Id || v3Back.Name != fieldPerson.Name || v3Back.Score != fieldPerson.Score) throw new InvalidOperationException("verify failed: ump -> v3 field poco");
    }

    [BenchmarkCategory("SerializeFields"), Benchmark(Baseline = true)]
    public byte[] SerializeContractlessFields() => MessagePack.MessagePackSerializer.Serialize(fieldPerson, contractless);

    [BenchmarkCategory("SerializeFields"), Benchmark]
    public byte[] SerializeV3ContractlessFields() => V3::MessagePack.MessagePackSerializer.Serialize(fieldPerson, v3Contractless);

    [BenchmarkCategory("DeserializeFields"), Benchmark(Baseline = true)]
    public FieldPerson DeserializeContractlessFields() => MessagePack.MessagePackSerializer.Deserialize<FieldPerson>(fieldPayload, contractless);

    [BenchmarkCategory("DeserializeFields"), Benchmark]
    public FieldPerson DeserializeV3ContractlessFields() => V3::MessagePack.MessagePackSerializer.Deserialize<FieldPerson>(fieldPayload, v3Contractless);
}

public struct FieldPerson
{
    public int Id;
    public string? Name;
    public double Score;
}

// deliberately attribute-free: v3's ContractlessStandardResolver must fall through to its
// contractless (map) tier, not the attribute-driven array tier
public class ContractlessPerson
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Score { get; set; }
}

// The ceiling: identical wire and identical read strategy to the contractless formatter,
// but member access is inlined instead of delegate-dispatched.
public sealed class HandMapPersonFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, ContractlessPerson?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref MessagePack.SerializeState state, ContractlessPerson? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteMapHeader(3);
        buffer.WriteString("Id"u8);
        buffer.WriteInt32(value.Id);
        buffer.WriteString("Name"u8);
        buffer.WriteString(value.Name);
        buffer.WriteString("Score"u8);
        buffer.WriteDouble(value.Score);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref MessagePack.DeserializeState state, ref ContractlessPerson? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadMapHeader();
        value ??= new ContractlessPerson();
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            var keyLength = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(keyLength, out var window))
            {
                throw new MessagePackSerializationException("truncated key");
            }
            var key = window.Slice(0, keyLength);
            buffer.Advance(keyLength);
            if (key.SequenceEqual("Id"u8))
            {
                value.Id = buffer.ReadInt32();
            }
            else if (key.SequenceEqual("Name"u8))
            {
                value.Name = buffer.ReadString();
            }
            else if (key.SequenceEqual("Score"u8))
            {
                value.Score = buffer.ReadDouble();
            }
            else
            {
                buffer.Skip();
            }
        }
        state.Exit();
    }
}

public sealed partial class HandMapPersonFormatterFactory : MessagePack.MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(ContractlessPerson))
        {
            return new HandMapPersonFormatter<TWriteBuffer, TReadBuffer>();
        }
        return null;
    }
}

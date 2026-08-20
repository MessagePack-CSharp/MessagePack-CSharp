using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using UltraMessagePack;
using UltraMessagePack.Formatters;

// DictionaryFormatter.Deserialize loop A/B/C: the shipping loop (SlotClear) deserializes
// the value straight into the dictionary slot via CollectionsMarshal.GetValueRefOrAddDefault,
// paying an unconditional `slot = default!` so a duplicate key in the payload can never
// leak populate semantics into what should be a fresh value. Candidates:
//   Indexer       — pre-slot shape: deserialize into a local, `result[k] = v` (one extra
//                   TValue copy per entry; the copy is the whole point of the A/B)
//   SlotClear     — shipping: slot ref + unconditional clear (extra TValue-sized zeroing
//                   per entry, paid even though duplicate keys are the rare case)
//   SlotCondClear — slot ref + clear only when GetValueRefOrAddDefault reports the key
//                   already existed (`out bool exists`); the zeroing becomes a predicted-
//                   not-taken branch on duplicate-free payloads
// The value-size axis decides the stakes: int (4B, copy is free), a 32-byte struct
// (copy and zeroing both measurable), string (reference — all variants move one pointer).
public sealed class IndexerDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Dictionary<TKey, TValue>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Dictionary<TKey, TValue>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();
        buffer.WriteMapHeader(value.Count);
        foreach (var kv in value)
        {
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Dictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        Dictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value;
            result.Clear();
            result.EnsureCapacity(count);
        }
        else
        {
            result = new Dictionary<TKey, TValue>(count);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }

        value = result;
        state.Exit();
    }
}

public sealed class SlotCondClearDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Dictionary<TKey, TValue>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Dictionary<TKey, TValue>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();
        buffer.WriteMapHeader(value.Count);
        foreach (var kv in value)
        {
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Dictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        Dictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value;
            result.Clear();
            result.EnsureCapacity(count);
        }
        else
        {
            result = new Dictionary<TKey, TValue>(count);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(result, k, out var exists)!;
            if (exists)
            {
                slot = default!; // duplicate key: never hand a stale value to a populate-style Deserialize
            }
            vf.Deserialize(ref buffer, ref state, ref slot);
        }

        value = result;
        state.Exit();
    }
}

public enum DictLoopVariant
{
    Indexer,
    SlotCondClear,
}

public sealed partial class VariantDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly DictLoopVariant variant;

    public VariantDictionaryFormatterFactory(DictLoopVariant variant)
    {
        this.variant = variant;
    }

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(Dictionary<TKey, TValue>))
        {
            return variant switch
            {
                DictLoopVariant.Indexer => new IndexerDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(),
                _ => new SlotCondClearDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(),
            };
        }
        return null;
    }
}

// 32-byte struct value: big enough that the local→entry copy (Indexer) and the
// unconditional zeroing (SlotClear) both show up as real work per entry
public struct BigVal
{
    public long A, B, C, D;
}

public sealed class BigValFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BigVal>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BigVal value)
    {
        buffer.WriteArrayHeader(4);
        buffer.WriteInt64(value.A);
        buffer.WriteInt64(value.B);
        buffer.WriteInt64(value.C);
        buffer.WriteInt64(value.D);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BigVal value)
    {
        _ = buffer.ReadArrayHeader();
        // fields written straight through the incoming ref — for the slot variants this
        // lands in the dictionary entry itself, which is the whole effect being measured
        value.A = buffer.ReadInt64();
        value.B = buffer.ReadInt64();
        value.C = buffer.ReadInt64();
        value.D = buffer.ReadInt64();
    }
}

public sealed partial class BigValFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(BigVal))
        {
            return new BigValFormatter<TWriteBuffer, TReadBuffer>();
        }
        return null;
    }
}

public class DictSlotWriteIntBenchmark
{
    byte[] payload = default!;
    MessagePackSerializerOptions indexerOptions = default!;
    MessagePackSerializerOptions slotClearOptions = default!; // shipping DictionaryFormatter
    MessagePackSerializerOptions slotCondClearOptions = default!;
    Dictionary<int, int>? populateTarget;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        indexerOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<int, int>(DictLoopVariant.Indexer), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotCondClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<int, int>(DictLoopVariant.SlotCondClear), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));

        var rand = new Random(42);
        var dict = new Dictionary<int, int>(N);
        for (int i = 0; i < N; i++) dict[i * 31 - 7] = rand.Next();
        payload = MessagePackSerializer.Serialize(dict, slotClearOptions);
        populateTarget = MessagePackSerializer.Deserialize<Dictionary<int, int>?>(payload, slotClearOptions);
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int>? Fresh_Indexer() => MessagePackSerializer.Deserialize<Dictionary<int, int>?>(payload, indexerOptions);

    [Benchmark]
    public Dictionary<int, int>? Fresh_SlotClear() => MessagePackSerializer.Deserialize<Dictionary<int, int>?>(payload, slotClearOptions);

    [Benchmark]
    public Dictionary<int, int>? Fresh_SlotCondClear() => MessagePackSerializer.Deserialize<Dictionary<int, int>?>(payload, slotCondClearOptions);

    [Benchmark]
    public Dictionary<int, int>? Populate_Indexer()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, indexerOptions);
        return target;
    }

    [Benchmark]
    public Dictionary<int, int>? Populate_SlotClear()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, slotClearOptions);
        return target;
    }

    [Benchmark]
    public Dictionary<int, int>? Populate_SlotCondClear()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, slotCondClearOptions);
        return target;
    }
}

public class DictSlotWriteBigStructBenchmark
{
    byte[] payload = default!;
    MessagePackSerializerOptions indexerOptions = default!;
    MessagePackSerializerOptions slotClearOptions = default!; // shipping DictionaryFormatter
    MessagePackSerializerOptions slotCondClearOptions = default!;
    Dictionary<int, BigVal>? populateTarget;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        indexerOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<int, BigVal>(DictLoopVariant.Indexer), new BigValFormatterFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new BigValFormatterFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotCondClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<int, BigVal>(DictLoopVariant.SlotCondClear), new BigValFormatterFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));

        var rand = new Random(42);
        var dict = new Dictionary<int, BigVal>(N);
        for (int i = 0; i < N; i++) dict[i * 31 - 7] = new BigVal { A = rand.NextInt64(), B = rand.NextInt64(), C = rand.NextInt64(), D = rand.NextInt64() };
        payload = MessagePackSerializer.Serialize(dict, slotClearOptions);
        populateTarget = MessagePackSerializer.Deserialize<Dictionary<int, BigVal>?>(payload, slotClearOptions);
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, BigVal>? Fresh_Indexer() => MessagePackSerializer.Deserialize<Dictionary<int, BigVal>?>(payload, indexerOptions);

    [Benchmark]
    public Dictionary<int, BigVal>? Fresh_SlotClear() => MessagePackSerializer.Deserialize<Dictionary<int, BigVal>?>(payload, slotClearOptions);

    [Benchmark]
    public Dictionary<int, BigVal>? Fresh_SlotCondClear() => MessagePackSerializer.Deserialize<Dictionary<int, BigVal>?>(payload, slotCondClearOptions);

    [Benchmark]
    public Dictionary<int, BigVal>? Populate_Indexer()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, indexerOptions);
        return target;
    }

    [Benchmark]
    public Dictionary<int, BigVal>? Populate_SlotClear()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, slotClearOptions);
        return target;
    }

    [Benchmark]
    public Dictionary<int, BigVal>? Populate_SlotCondClear()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, slotCondClearOptions);
        return target;
    }
}

// reference-type values: every variant moves one pointer into the entry, and per-element
// cost is dominated by string decode + alloc — expected parity; this class is the guard
// against the slot path somehow costing on the common shape
public class DictSlotWriteStringBenchmark
{
    byte[] payload = default!;
    MessagePackSerializerOptions indexerOptions = default!;
    MessagePackSerializerOptions slotClearOptions = default!; // shipping DictionaryFormatter
    MessagePackSerializerOptions slotCondClearOptions = default!;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        indexerOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<string, string>(DictLoopVariant.Indexer), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        slotCondClearOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new VariantDictionaryFormatterFactory<string, string>(DictLoopVariant.SlotCondClear), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));

        var dict = new Dictionary<string, string>(N);
        for (int i = 0; i < N; i++) dict[$"key{i}"] = $"value{i}";
        payload = MessagePackSerializer.Serialize(dict, slotClearOptions);
    }

    [Benchmark(Baseline = true)]
    public Dictionary<string, string>? Fresh_Indexer() => MessagePackSerializer.Deserialize<Dictionary<string, string>?>(payload, indexerOptions);

    [Benchmark]
    public Dictionary<string, string>? Fresh_SlotClear() => MessagePackSerializer.Deserialize<Dictionary<string, string>?>(payload, slotClearOptions);

    [Benchmark]
    public Dictionary<string, string>? Fresh_SlotCondClear() => MessagePackSerializer.Deserialize<Dictionary<string, string>?>(payload, slotCondClearOptions);
}

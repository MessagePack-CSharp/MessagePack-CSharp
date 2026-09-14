// MsgPack104: hand-written baseline formatter; the direct primitive calls are the benchmarked subject.
#pragma warning disable MsgPack104

extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using MessagePack;
using static MessagePack.MessagePackPrimitives;

// MessagePack vs MessagePack-CSharp. Types are fully qualified: this project's own
// experimental MessagePackPrimitives and both libraries' writer types would collide.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SerializerBenchmark
{
    const int Count = 10_000;

    int[] values = default!;
    byte[] serialized = default!;

    [Params("Small", "Mixed")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new int[Count];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
            {
                "Small" => rand.Next(-32, 128),
                _ => rand.Next(8) switch
                {
                    0 => rand.Next(0, 128),
                    1 => rand.Next(-32, 0),
                    2 => rand.Next(128, 256),
                    3 => rand.Next(-128, -32),
                    4 => rand.Next(256, 65536),
                    5 => rand.Next(-32768, -128),
                    6 => rand.Next(65536, int.MaxValue),
                    _ => rand.Next(int.MinValue, -32768),
                },
            };
        }
        serialized = V3::MessagePack.MessagePackSerializer.Serialize(values);
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeMessagePackCSharp() => V3::MessagePack.MessagePackSerializer.Serialize(values);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(values);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public int[] DeserializeMessagePackCSharp() => V3::MessagePack.MessagePackSerializer.Deserialize<int[]>(serialized)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int[] DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<int[]>(serialized)!;
}

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SerializerPocoBenchmark
{
    // Nerdbank's serializer is instance-based (immutable record, thread-safe); one shared
    // default-configured instance, matching how the other two use their static defaults
    static readonly Nerdbank.MessagePack.MessagePackSerializer NerdbankSerializer = new();

    BenchPerson person = default!;
    // v3 source-gen twin (see AnswerBenchmark for the satellite rationale)
    V3GeneratedModels.BenchPerson personV3SourceGen = default!;
    byte[] serialized = default!;

    [GlobalSetup]
    public void Setup()
    {
        // MessagePack.MessagePackSerializer.Register(new BenchPersonFormatter());
        MessagePack.SourceGeneratedFormatterFactory.Instance.Register(typeof(BenchPerson), new BenchPersonFormatterFactory());

        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        serialized = V3::MessagePack.MessagePackSerializer.Serialize(person);

        personV3SourceGen = V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.BenchPerson>(serialized, V3GeneratedModels.V3SourceGen.Options);
        if (!V3::MessagePack.MessagePackSerializer.Serialize(personV3SourceGen, V3GeneratedModels.V3SourceGen.Options).AsSpan().SequenceEqual(serialized)) throw new InvalidOperationException("verify failed: MessagePack-CSharp source-gen twin roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeMessagePackCSharp() => V3::MessagePack.MessagePackSerializer.Serialize(person);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMessagePackCSharpSourceGen() => V3::MessagePack.MessagePackSerializer.Serialize(personV3SourceGen, V3GeneratedModels.V3SourceGen.Options);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(person);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => NerdbankSerializer.Serialize(person);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public BenchPerson DeserializeMessagePackCSharp() => V3::MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(serialized)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public V3GeneratedModels.BenchPerson DeserializeMessagePackCSharpSourceGen() => V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.BenchPerson>(serialized, V3GeneratedModels.V3SourceGen.Options)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public BenchPerson DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(serialized)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public BenchPerson DeserializeNerdbank() => NerdbankSerializer.Deserialize<BenchPerson>(serialized)!;
}

// partial + [GenerateShape]: PolyType source-generates the type shape Nerdbank.MessagePack
// needs. Both libraries define a KeyAttribute, so they are attached fully qualified;
// Nerdbank's [Key] opts it into the same array-of-3 format as MessagePack-CSharp, keeping
// the payloads byte-identical across all serializers.
[V3::MessagePack.MessagePackObject]
[PolyType.GenerateShape]
public partial class BenchPerson
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)] public string? Name { get; set; }
    [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)] public double Score { get; set; }
}


public sealed class BenchPersonFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, BenchPerson>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref MessagePack.SerializeState state, BenchPerson value)
    {

        buffer.Advance(UnsafeWriteArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), 3));
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.Id));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference(GetMaxStringByteCount(value.Name)), value.Name));
        buffer.Advance(UnsafeWriteDouble(ref buffer.GetReference(MaxFloat64Length), value.Score));
    }

    public void Deserialize(ref TReadBuffer buffer, ref MessagePack.DeserializeState state, ref BenchPerson value)
    {
   var count = buffer.ReadArrayHeader();

        if (value == null)
        {
            value = new BenchPerson();
        }

        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0:
                    value.Id = buffer.ReadInt32();
                    break;
                case 1:
                    value.Name = buffer.ReadString();
                    break;
                case 2:
                    value.Score = buffer.ReadDouble();
                    break;
                default:
                    break;
            }
        }
    }
}

public sealed partial class BenchPersonFormatterFactory : MessagePack.MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new BenchPersonFormatter<TWriteBuffer, TReadBuffer>();
    }
}
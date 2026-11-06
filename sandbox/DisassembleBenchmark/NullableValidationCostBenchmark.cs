using BenchmarkDotNet.Attributes;
using MessagePack;
using MessagePack.Formatters;

// Cost of ValidateNullableAnnotations (on by default since it was flipped). In the generated tier the flag is read
// only on the null path, so on/off resolvers run the SAME machine code: the generated A/B is instead a type whose
// members get the check emitted (non-nullable) against its twin with `?` members (no check emitted), same payload.
// The reflection slots read the flag on every member, so there the on/off rows are the A/B.
//
//   dotnet run -c Release --project sandbox/DisassembleBenchmark -- --filter *NullableValidationCost* --no-disasm --iterations=20
[MessagePackObject]
public class NullableValidationOrder
{
    [Key(0)] public string OrderId { get; set; } = "";
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public string CustomerName { get; set; } = "";
    [Key(3)] public string Category { get; set; } = "";
    [Key(4)] public int[] Lines { get; set; } = [];
    [Key(5)] public string Note { get; set; } = "";
}

// the same wire shape with every reference member nullable: no null check is emitted at all
[MessagePackObject]
public class NullableValidationOrderUnchecked
{
    [Key(0)] public string? OrderId { get; set; }
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public string? CustomerName { get; set; }
    [Key(3)] public string? Category { get; set; }
    [Key(4)] public int[]? Lines { get; set; }
    [Key(5)] public string? Note { get; set; }
}

public class NullableValidationCostBenchmark
{
    MessagePackSerializerOptions on = null!;
    MessagePackSerializerOptions reflectionOn = null!;
    MessagePackSerializerOptions reflectionOff = null!;
    NullableValidationOrder value = null!;
    NullableValidationOrderUnchecked unchecked_ = null!;
    byte[] payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        on = new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { ValidateNullableAnnotations = true });
        MessagePackFormatterFactory[] reflection = [BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: true)];
        reflectionOn = new(new MessagePackFormatterResolver(reflection) { ValidateNullableAnnotations = true });
        reflectionOff = new(new MessagePackFormatterResolver(reflection) { ValidateNullableAnnotations = false });
        value = new NullableValidationOrder { OrderId = "ORD-2026-11-06", Quantity = 3, CustomerName = "Yamada Taro", Category = "books", Lines = [1, 2, 3], Note = "leave at the door" };
        unchecked_ = new NullableValidationOrderUnchecked { OrderId = value.OrderId, Quantity = value.Quantity, CustomerName = value.CustomerName, Category = value.Category, Lines = value.Lines, Note = value.Note };
        payload = MessagePackSerializer.Serialize(value, on);
    }

    [Benchmark(Baseline = true)] public byte[] SerializeUnchecked() => MessagePackSerializer.Serialize(unchecked_, on);
    [Benchmark] public byte[] SerializeChecked() => MessagePackSerializer.Serialize(value, on);
    [Benchmark] public NullableValidationOrderUnchecked? DeserializeUnchecked() => MessagePackSerializer.Deserialize<NullableValidationOrderUnchecked>(payload, on);
    [Benchmark] public NullableValidationOrder? DeserializeChecked() => MessagePackSerializer.Deserialize<NullableValidationOrder>(payload, on);
    [Benchmark] public byte[] ReflectionSerializeOff() => MessagePackSerializer.Serialize(value, reflectionOff);
    [Benchmark] public byte[] ReflectionSerializeOn() => MessagePackSerializer.Serialize(value, reflectionOn);
}

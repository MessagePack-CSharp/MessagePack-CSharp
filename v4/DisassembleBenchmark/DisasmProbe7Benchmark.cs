using BenchmarkDotNet.Attributes;

// Round 7 (concluded): measured the formatter-resolution strategies on an ~18ns Poco entry:
//   static entry + frozen Cache<> read   1.00 (baseline, the old UltraMessagePackSerializer)
//   instance entry via virtual GetFormatter (GVM, never devirtualized)   1.15
//   instance entry via resolver dictionary per call                      1.7
//   ReferenceEquals(this, Default) -> frozen cache                       ~1.00
//   typeid table                                                         ~1.05
// The typeid table won and then became the WHOLE design: the resolver's formatterTable is
// now the single cache — the construction ConcurrentDictionary, the serializer-side
// DefaultCache and its ReferenceEquals branch were all deleted once "UniformTable"
// measured within noise (1.04) of the frozen-cache path. This benchmark guards the
// surviving structure:
//   DefaultSerialize      - Default instance through the resolver table
//   CustomSerialize       - non-Default instance, same path (must be ~equal)
//   NewSerializerEachCall - the misuse pattern (new serializer per call, shared resolver);
//                           stateless serializer must make this ~= CustomSerialize + alloc
public class DisasmProbe7Benchmark
{
    BenchPerson person = default!;
    UltraMessagePack.MessagePackFormatterResolver sharedResolver = default!;
    UltraMessagePack.MessagePackSerializer custom = default!;

    [GlobalSetup]
    public void Setup()
    {
        UltraMessagePack.DynamicFormatterFactory.Instance.RegisterFactory<BenchPerson>(new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        sharedResolver = new UltraMessagePack.MessagePackFormatterResolver(UltraMessagePack.DefaultFormatterFactory.Instance);
        custom = new UltraMessagePack.MessagePackSerializer(sharedResolver);

        var expected = MessagePack.MessagePackSerializer.Serialize(person);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(DefaultSerialize), DefaultSerialize()),
            (nameof(CustomSerialize), CustomSerialize()),
            (nameof(NewSerializerEachCall), NewSerializerEachCall()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name}");
        }
    }

    [Benchmark(Baseline = true)]
    public byte[] DefaultSerialize() => UltraMessagePack.MessagePackSerializer.Default.Serialize(person);

    [Benchmark]
    public byte[] CustomSerialize() => custom.Serialize(person);

    [Benchmark]
    public byte[] NewSerializerEachCall() => new UltraMessagePack.MessagePackSerializer(sharedResolver).Serialize(person);
}

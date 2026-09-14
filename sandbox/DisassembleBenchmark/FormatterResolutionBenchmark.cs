extern alias V3;
using BenchmarkDotNet.Attributes;

// Round 7 (concluded): measured the formatter-resolution strategies on an ~18ns Poco entry:
//   static entry + frozen Cache<> read   1.00 (baseline, the old MessagePackSerializer)
//   instance entry via virtual GetFormatter (GVM, never devirtualized)   1.15
//   instance entry via resolver dictionary per call                      1.7
//   ReferenceEquals(this, Default) -> frozen cache                       ~1.00
//   typeid table                                                         ~1.05
// The typeid table won and then became the WHOLE design: the resolver's formatterTable is
// now the single cache — the construction ConcurrentDictionary, the serializer-side
// DefaultCache and its ReferenceEquals branch were all deleted once "UniformTable"
// measured within noise (1.04) of the frozen-cache path. This benchmark guards the
// surviving structure:
//   DefaultSerialize      - options=null path through the resolver table
//   CustomSerialize       - explicit options, same path (must be ~equal)
//   NewSerializerEachCall - the misuse pattern (new options per call, shared resolver);
//                           stateless options must make this ~= CustomSerialize + alloc
public class FormatterResolutionBenchmark
{
    BenchPerson person = default!;
    MessagePack.MessagePackFormatterResolver sharedResolver = default!;
    MessagePack.MessagePackSerializerOptions custom = default!;

    [GlobalSetup]
    public void Setup()
    {
        MessagePack.SourceGeneratedFormatterFactory.Instance.Register(typeof(BenchPerson), new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        sharedResolver = new MessagePack.MessagePackFormatterResolver(MessagePack.MessagePackFormatterFactory.Default);
        custom = new MessagePack.MessagePackSerializerOptions(sharedResolver);

        var expected = V3::MessagePack.MessagePackSerializer.Serialize(person);
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
    public byte[] DefaultSerialize() => MessagePack.MessagePackSerializer.Serialize(person);

    [Benchmark]
    public byte[] CustomSerialize() => MessagePack.MessagePackSerializer.Serialize(person, custom);

    [Benchmark]
    public byte[] NewSerializerEachCall() => MessagePack.MessagePackSerializer.Serialize(person, new MessagePack.MessagePackSerializerOptions(sharedResolver));
}

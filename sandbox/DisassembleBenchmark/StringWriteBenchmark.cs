extern alias V3;
using BenchmarkDotNet.Attributes;
// string serialize: MessagePack-CSharp (speculative encode + post-move, always) vs
// MessagePack (class-stability single pass; move only for non-ASCII in straddle zones).
// Lengths chosen to hit stable zones (8, 60, 1000) and straddle zones (20, 150, 40000).
public class StringWriteBenchmark
{
    string value = default!;

    [Params(8, 20, 60, 150, 1000, 40000)]
    public int Length;

    [Params("Ascii", "Japanese")]
    public string Content = "Ascii";

    [GlobalSetup]
    public void Setup()
    {
        value = Content == "Ascii" ? new string('x', Length) : new string('あ', Length);
    }

    [Benchmark(Baseline = true)]
    public byte[] MessagePackCSharp() => V3::MessagePack.MessagePackSerializer.Serialize(value);

    [Benchmark]
    public byte[] V4() => MessagePack.MessagePackSerializer.Serialize(value);
}

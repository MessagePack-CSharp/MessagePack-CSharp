using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using UltraMessagePack;

// Does routing a string FIELD through an IMessagePackFormatter interface field
// (GDV-dependent dispatch) cost anything vs calling buffer.WriteString directly in the
// POCO formatter? Prediction from probe11 + call-shape analysis: no — the string cores
// (UnsafeWriteString / TryReadString) are real calls even in the direct shape, so a GDV
// hit collapses Via to the identical code and even a GDV miss only adds one callvirt to
// 10ns+ of encode work. probe11 already showed IfaceField ≒ Direct for int fields (the
// worst work-to-dispatch ratio); string should be strictly safer.
//   Short - 1..8 ascii chars: highest possible dispatch-to-work ratio (adversarial)
//   Long  - 32..64 chars with multibyte mixed in: realistic payloads
//
// MEASURED (Zen 5, ShortRun; pairwise Direct vs Via, both include the e2e entry cost):
//   Short: Serialize 25.9 vs 26.0 ns (1.01x)   Deserialize 27.2 vs 27.0 ns (1.00x)
//   Long:  Serialize 72.4 vs 75.3 ns (1.04x)   Deserialize 112.1 vs 120.6 ns (1.08x*)
//   (* the Long/DeserializeVia iteration was noisy: StdDev 7.0ns, Error 128ns)
// Every delta is inside the 10% ShortRun equivalence band, and the ADVERSARIAL case
// (Short, where dispatch is proportionally largest) is a flat 1.00-1.01x. VERDICT:
// routing string members through an IMessagePackFormatter field is free — GDV collapses
// the callsite, and even the theoretical miss adds one callvirt to work that is 25ns+.
//
// RE-MEASURED at MediumRun 15x2 (the block above was ShortRun, whose +-128ns Error made it
// weak evidence) when String actually moved off the direct path — the generator now routes
// it through the formatter field so a chain-supplied interning/custom-encoding formatter
// reaches [MessagePackObject] members at all (see DirectKind):
//   Short: Serialize 30.92 vs 32.74 ns (1.06x)   Deserialize 28.50 vs 28.21 ns (0.99x)
//   Long:  Serialize 64.24 vs 62.12 ns (0.97x)   Deserialize 99.91 vs 99.85 ns (1.00x)
// Three of the four cells are dead flat or favour Via; the one 1.06x is Short/Serialize,
// where Direct owns the block's widest StdDev (3.23ns) — wider than the 1.8ns delta itself.
// The original verdict holds with tighter bars: string dispatch is free. Contrast DateTime
// (DateTimeFormatterDispatchBenchmark), which DID cost ~1 ns/member — because DateTime was
// FUSED into the shared reservation and strings never were.
public class StringFormatterDispatchBenchmark
{
    const int Count = 10_000;

    [Params("Short", "Long")]
    public string Names = "Short";

    SPersonDirect[] directs = default!;
    SPersonVia[] vias = default!;
    byte[][] payloads = default!;

    [GlobalSetup]
    public void Setup()
    {
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<SPersonDirect>(new SPersonDirectFormatterFactory());
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<SPersonVia>(new SPersonViaFormatterFactory());

        var rand = new Random(42);
        string NextName()
        {
            int len = Names == "Short" ? rand.Next(1, 9) : rand.Next(32, 65);
            var chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = Names == "Long" && (i & 3) == 3
                    ? (char)rand.Next(0x3042, 0x3094)          // hiragana: 3-byte utf8
                    : (char)rand.Next('a', 'z' + 1);
            }
            return new string(chars);
        }

        directs = new SPersonDirect[Count];
        vias = new SPersonVia[Count];
        payloads = new byte[Count][];
        for (int i = 0; i < Count; i++)
        {
            var name = NextName();
            directs[i] = new SPersonDirect { Id = rand.Next(), Name = name, Score = rand.NextDouble() };
            vias[i] = new SPersonVia { Id = directs[i].Id, Name = name, Score = directs[i].Score };

            // both shapes must produce identical bytes and roundtrip identically
            var d = MessagePackSerializer.Serialize(directs[i]);
            var v = MessagePackSerializer.Serialize(vias[i]);
            if (!d.AsSpan().SequenceEqual(v)) throw new InvalidOperationException($"bytes mismatch at {i}");
            var db = MessagePackSerializer.Deserialize<SPersonDirect>(d);
            var vb = MessagePackSerializer.Deserialize<SPersonVia>(v);
            if (db.Id != vias[i].Id || db.Name != name || vb.Name != name || vb.Score != directs[i].Score)
            {
                throw new InvalidOperationException($"roundtrip mismatch at {i}");
            }
            payloads[i] = d;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int SerializeDirect()
    {
        int total = 0;
        var items = directs;
        for (int i = 0; i < items.Length; i++)
        {
            total += MessagePackSerializer.Serialize(items[i]).Length;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SerializeVia()
    {
        int total = 0;
        var items = vias;
        for (int i = 0; i < items.Length; i++)
        {
            total += MessagePackSerializer.Serialize(items[i]).Length;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DeserializeDirect()
    {
        int total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<SPersonDirect>(data[i]).Name!.Length;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DeserializeVia()
    {
        int total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<SPersonVia>(data[i]).Name!.Length;
        }
        return total;
    }
}

public class SPersonDirect
{
    public int Id;
    public string? Name;
    public double Score;
}

public class SPersonVia
{
    public int Id;
    public string? Name;
    public double Score;
}

// Name written/read via the buffer extensions directly (the PersonFormatter shape)
public sealed class SPersonDirectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SPersonDirect>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SPersonDirect value)
    {
        buffer.WriteFixArrayHeader(3);
        buffer.WriteInt32(value.Id);
        buffer.WriteString(value.Name);
        buffer.WriteDouble(value.Score);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SPersonDirect value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new SPersonDirect();
        value.Id = buffer.ReadInt32();
        value.Name = buffer.ReadString();
        value.Score = buffer.ReadDouble();
    }
}

public sealed partial class SPersonDirectFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new SPersonDirectFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// Name routed through the resolver's StringFormatter via an interface-typed field —
// the shape a source generator would emit for non-primitive members
public sealed class SPersonViaFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SPersonVia>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> stringFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        stringFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SPersonVia value)
    {
        buffer.WriteFixArrayHeader(3);
        buffer.WriteInt32(value.Id);
        stringFormatter.Serialize(ref buffer, ref state, value.Name);
        buffer.WriteDouble(value.Score);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SPersonVia value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new SPersonVia();
        value.Id = buffer.ReadInt32();
        stringFormatter.Deserialize(ref buffer, ref state, ref value.Name);
        value.Score = buffer.ReadDouble();
    }
}

public sealed partial class SPersonViaFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new SPersonViaFormatter<TWriteBuffer, TReadBuffer>();
    }
}

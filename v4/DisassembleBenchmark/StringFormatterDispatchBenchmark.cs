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
// The "primitives direct, everything else via formatter field" split stands.
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
        DynamicFormatterFactory.Instance.RegisterFactory<SPersonDirect>(new SPersonDirectFormatterFactory());
        DynamicFormatterFactory.Instance.RegisterFactory<SPersonVia>(new SPersonViaFormatterFactory());

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
            var d = MessagePackSerializer.Default.Serialize(directs[i]);
            var v = MessagePackSerializer.Default.Serialize(vias[i]);
            if (!d.AsSpan().SequenceEqual(v)) throw new InvalidOperationException($"bytes mismatch at {i}");
            var db = MessagePackSerializer.Default.Deserialize<SPersonDirect>(d);
            var vb = MessagePackSerializer.Default.Deserialize<SPersonVia>(v);
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
            total += MessagePackSerializer.Default.Serialize(items[i]).Length;
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
            total += MessagePackSerializer.Default.Serialize(items[i]).Length;
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
            total += MessagePackSerializer.Default.Deserialize<SPersonDirect>(data[i]).Name!.Length;
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
            total += MessagePackSerializer.Default.Deserialize<SPersonVia>(data[i]).Name!.Length;
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref SPersonDirect value)
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

public sealed class SPersonDirectFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref SPersonVia value)
    {
        buffer.WriteFixArrayHeader(3);
        buffer.WriteInt32(value.Id);
        stringFormatter.Serialize(ref buffer, ref state, ref value.Name);
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

public sealed class SPersonViaFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new SPersonViaFormatter<TWriteBuffer, TReadBuffer>();
    }
}

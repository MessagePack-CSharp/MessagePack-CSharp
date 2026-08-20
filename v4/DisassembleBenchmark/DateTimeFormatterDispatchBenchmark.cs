using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using UltraMessagePack;
using static UltraMessagePack.MessagePackPrimitives;

// What does it cost to take DateTime OFF the source generator's direct-write path?
// DateTime had to move: its wire form is factory-chain configurable (DotNetOptimized
// rewrites timestamp ext to Kind-preserving ToBinary), and an inlined UnsafeWriteTimestamp
// in the generated formatter pinned the default encoding, making that tier a silent no-op
// for every DateTime member of a [MessagePackObject] type. This measures the price.
//
// Unlike StringFormatterDispatchBenchmark (where the direct shape ALSO flushed the batch,
// so only dispatch was at stake), DateTime was a fused member: it sat inside the shared
// GetReference/Advance run with the ints. So Via loses TWO things at once —
//   1. the fused reservation: 5 separate GetReference(15)/Advance pairs instead of one
//      15-byte slice of a single 46-byte reservation
//   2. the callvirt through the IMessagePackFormatter field (GDV-dependent)
// The poco is adversarial on purpose: 5 DateTime members to 2 ints, so the DateTime work
// dominates and any per-member cost shows up 5x. Answer's own worst class (Answer: 5
// DateTime, 24 members) is diluted by comparison.
//   Sec    - whole-second Utc: timestamp32, the 6-byte fixext4 form (Answer's own data)
//   SubSec - sub-second ticks: timestamp64, the 10-byte fixext8 form
//
// Two more arms answer "can an Initialize-time `bool isDefaultDateTimeFormatter` buy the
// cost back, and at what branch granularity":
//   ViaFlag   - the flag tested AT EACH MEMBER. Kills the callvirt; CANNOT restore the
//               fused reservation, because a shared GetReference cannot span a conditional
//   FlagFused - the flag hoisted around the WHOLE run, so the fused form comes back at the
//               price of a duplicated Serialize body. The ceiling of any flag design
//
// MEASURED (Zen 5, MediumRun 15x2 — ShortRun was useless here, Error ran to +-50ns on a
// 30ns mean; --job medium, NOT --job Default, which BDN drops when the config already
// defines a job). Per-op figures cover 5 DateTime members:
//                        Fused | Via   | ViaFlag | FlagFused
//   Serialize   Sec       32.1 |  36.4 |    37.5 |      32.1
//   Serialize   SubSec    34.3 |  35.5 |    36.3 |      34.5
//   Deserialize Sec       25.3 |  30.5 |    24.1 |      24.3
//   Deserialize SubSec    28.8 |  29.5 |    25.9 |      24.7
//
// VERDICT, and the two directions do NOT agree:
//   WRITE - the entire penalty is the LOST FUSION, not the callvirt. ViaFlag (37.5) is no
//           better than plain Via (36.4) and trends slightly worse: you pay five branches
//           and still get five separate GetReference/Advance pairs. Only FlagFused claws
//           it back, landing exactly on Fused (32.1 vs 32.1, non-overlapping error bars
//           against the Via pair). A per-member flag is the wrong shape here.
//   READ  - reads never fused, so the penalty IS the callvirt, and either granularity
//           removes it: Via 30.5 -> ViaFlag 24.1 / FlagFused 24.3, both at or under Fused.
//           (Both flag arms edging BELOW Fused is layout noise — Fused owns the widest
//           error bars in the block, +-4.1ns — but "flag beats Via" holds across both tick
//           classes and both runs.)
// So a read-side per-member flag is a free ~0.9-1.2 ns/member, needing no duplicated body;
// a write-side flag only pays if the whole run is hoisted, which doubles Serialize's IL for
// every type with a DateTime member. Dilution check before spending that: Answer carries 8
// DateTime members in a 1.65KB graph, and AnswerBenchmark round 5 measured NO regression
// from the move (1-2% predicted, under the noise floor). The adversarial 5:2 ratio here is
// what makes the effect visible at all.
//
// DECIDED: NEITHER flag ships. The v2-era experiments that made direct-vs-dispatch look
// dramatic predate PGO guarded devirtualization, and the write side confirms GDV now
// collapses the callsite (removing the callvirt by hand buys nothing). The read side's
// surviving ~1 ns/member is real and reproducible, but it is only visible at this poco's
// synthetic timestamp density, and the price is generated code that has to test formatter
// IDENTITY against the built-in types. If a DateTime-dense workload ever justifies
// revisiting: take the READ-side per-member flag only (no body duplication), gate it on a
// marker interface such as IDirectWireFormatter rather than a concrete type test, and
// leave the write side on the formatter path.
public class DateTimeFormatterDispatchBenchmark
{
    const int Count = 10_000;

    [Params("Sec", "SubSec")]
    public string Ticks = "Sec";

    DtFused[] fused = default!;
    DtVia[] vias = default!;
    DtViaFlag[] viaFlags = default!;
    DtFlagFused[] flagFuseds = default!;
    byte[][] payloads = default!;

    [GlobalSetup]
    public void Setup()
    {
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<DtFused>(new DtFusedFormatterFactory());
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<DtVia>(new DtViaFormatterFactory());
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<DtViaFlag>(new DtViaFlagFormatterFactory());
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<DtFlagFused>(new DtFlagFusedFormatterFactory());

        var rand = new Random(42);
        var epoch = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime Next()
        {
            var value = epoch.AddSeconds(rand.Next(0, 500_000_000));
            return Ticks == "SubSec" ? value.AddTicks(rand.Next(1, 9_999_999)) : value;
        }

        fused = new DtFused[Count];
        vias = new DtVia[Count];
        viaFlags = new DtViaFlag[Count];
        flagFuseds = new DtFlagFused[Count];
        payloads = new byte[Count][];
        for (int i = 0; i < Count; i++)
        {
            var f = new DtFused
            {
                Id = rand.Next(),
                A = Next(),
                B = Next(),
                C = Next(),
                D = Next(),
                E = Next(),
                Score = rand.Next(),
            };
            fused[i] = f;
            vias[i] = new DtVia { Id = f.Id, A = f.A, B = f.B, C = f.C, D = f.D, E = f.E, Score = f.Score };
            viaFlags[i] = new DtViaFlag { Id = f.Id, A = f.A, B = f.B, C = f.C, D = f.D, E = f.E, Score = f.Score };
            flagFuseds[i] = new DtFlagFused { Id = f.Id, A = f.A, B = f.B, C = f.C, D = f.D, E = f.E, Score = f.Score };

            // all four shapes must produce identical bytes and roundtrip identically
            var df = MessagePackSerializer.Serialize(f);
            foreach (var other in new[]
            {
                MessagePackSerializer.Serialize(vias[i]),
                MessagePackSerializer.Serialize(viaFlags[i]),
                MessagePackSerializer.Serialize(flagFuseds[i]),
            })
            {
                if (!df.AsSpan().SequenceEqual(other)) throw new InvalidOperationException($"bytes mismatch at {i}");
            }
            var fb = MessagePackSerializer.Deserialize<DtFused>(df);
            var vb = MessagePackSerializer.Deserialize<DtVia>(df);
            var qb = MessagePackSerializer.Deserialize<DtViaFlag>(df);
            var hb = MessagePackSerializer.Deserialize<DtFlagFused>(df);
            if (fb.A != f.A || fb.E != f.E || vb.A != f.A || vb.E != f.E || vb.Score != f.Score
                || qb.A != f.A || qb.E != f.E || hb.A != f.A || hb.E != f.E || hb.Score != f.Score)
            {
                throw new InvalidOperationException($"roundtrip mismatch at {i}");
            }
            payloads[i] = df;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int SerializeFused()
    {
        int total = 0;
        var items = fused;
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
    public int SerializeViaFlag()
    {
        int total = 0;
        var items = viaFlags;
        for (int i = 0; i < items.Length; i++)
        {
            total += MessagePackSerializer.Serialize(items[i]).Length;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SerializeFlagFused()
    {
        int total = 0;
        var items = flagFuseds;
        for (int i = 0; i < items.Length; i++)
        {
            total += MessagePackSerializer.Serialize(items[i]).Length;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DeserializeFused()
    {
        long total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<DtFused>(data[i]).E.Ticks;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DeserializeVia()
    {
        long total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<DtVia>(data[i]).E.Ticks;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DeserializeViaFlag()
    {
        long total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<DtViaFlag>(data[i]).E.Ticks;
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DeserializeFlagFused()
    {
        long total = 0;
        var data = payloads;
        for (int i = 0; i < data.Length; i++)
        {
            total += MessagePackSerializer.Deserialize<DtFlagFused>(data[i]).E.Ticks;
        }
        return total;
    }
}

public class DtFused
{
    public int Id;
    public DateTime A, B, C, D, E;
    public int Score;
}

public class DtVia
{
    public int Id;
    public DateTime A, B, C, D, E;
    public int Score;
}

public class DtViaFlag
{
    public int Id;
    public DateTime A, B, C, D, E;
    public int Score;
}

public class DtFlagFused
{
    public int Id;
    public DateTime A, B, C, D, E;
    public int Score;
}

// The shape the generator USED to emit: every member fused into one reservation, the
// timestamps written by an inlined UnsafeWriteTimestamp
public sealed class DtFusedFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DtFused>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DtFused value)
    {
        ref var d = ref buffer.GetReference(1 + MaxInt32Length + (5 * MaxTimestampLength) + MaxInt32Length);
        var w = 0;
        w += UnsafeWriteFixArrayHeader(ref d, 7);
        w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Id);
        w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.A);
        w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.B);
        w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.C);
        w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.D);
        w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.E);
        w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Score);
        buffer.Advance(w);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DtFused value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new DtFused();
        value.Id = buffer.ReadInt32();
        value.A = buffer.ReadTimestamp();
        value.B = buffer.ReadTimestamp();
        value.C = buffer.ReadTimestamp();
        value.D = buffer.ReadTimestamp();
        value.E = buffer.ReadTimestamp();
        value.Score = buffer.ReadInt32();
    }
}

public sealed partial class DtFusedFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new DtFusedFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// The shape the generator emits NOW: the timestamps break the run and dispatch through
// the resolver-supplied DateTime formatter, so the chain (Default vs DotNetOptimized)
// actually decides the encoding
public sealed class DtViaFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DtVia>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime> dt = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        dt = resolver.GetFormatter<TWriteBuffer, TReadBuffer, DateTime>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DtVia value)
    {
        {
            ref var d = ref buffer.GetReference(1 + MaxInt32Length);
            var w = 0;
            w += UnsafeWriteFixArrayHeader(ref d, 7);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Id);
            buffer.Advance(w);
        }
        dt.Serialize(ref buffer, ref state, value.A);
        dt.Serialize(ref buffer, ref state, value.B);
        dt.Serialize(ref buffer, ref state, value.C);
        dt.Serialize(ref buffer, ref state, value.D);
        dt.Serialize(ref buffer, ref state, value.E);
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.Score));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DtVia value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new DtVia();
        value.Id = buffer.ReadInt32();
        dt.Deserialize(ref buffer, ref state, ref value.A);
        dt.Deserialize(ref buffer, ref state, ref value.B);
        dt.Deserialize(ref buffer, ref state, ref value.C);
        dt.Deserialize(ref buffer, ref state, ref value.D);
        dt.Deserialize(ref buffer, ref state, ref value.E);
        value.Score = buffer.ReadInt32();
    }
}

public sealed partial class DtViaFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new DtViaFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// PER-MEMBER escape hatch: Initialize asks whether the resolver handed back the stock
// DateTimeFormatter and, if so, each member site takes the inlined write instead of the
// callvirt. The branch is a loop-invariant field read, so it predicts perfectly — but it
// still cannot restore the FUSED reservation, because a shared GetReference cannot span a
// conditional. This arm isolates "callvirt removed, fusion still lost".
public sealed class DtViaFlagFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DtViaFlag>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime> dt = null!;
    bool isDefaultDateTime;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        dt = resolver.GetFormatter<TWriteBuffer, TReadBuffer, DateTime>();
        isDefaultDateTime = dt is UltraMessagePack.Formatters.DateTimeFormatter<TWriteBuffer, TReadBuffer>;
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DtViaFlag value)
    {
        {
            ref var d = ref buffer.GetReference(1 + MaxInt32Length);
            var w = 0;
            w += UnsafeWriteFixArrayHeader(ref d, 7);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Id);
            buffer.Advance(w);
        }
        Write(ref buffer, ref state, value.A);
        Write(ref buffer, ref state, value.B);
        Write(ref buffer, ref state, value.C);
        Write(ref buffer, ref state, value.D);
        Write(ref buffer, ref state, value.E);
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.Score));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Write(ref TWriteBuffer buffer, ref SerializeState state, DateTime v)
    {
        if (isDefaultDateTime)
        {
            buffer.WriteTimestamp(v);
        }
        else
        {
            dt.Serialize(ref buffer, ref state, v);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DtViaFlag value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new DtViaFlag();
        value.Id = buffer.ReadInt32();
        Read(ref buffer, ref state, ref value.A);
        Read(ref buffer, ref state, ref value.B);
        Read(ref buffer, ref state, ref value.C);
        Read(ref buffer, ref state, ref value.D);
        Read(ref buffer, ref state, ref value.E);
        value.Score = buffer.ReadInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Read(ref TReadBuffer buffer, ref DeserializeState state, ref DateTime v)
    {
        if (isDefaultDateTime)
        {
            v = buffer.ReadTimestamp();
        }
        else
        {
            dt.Deserialize(ref buffer, ref state, ref v);
        }
    }
}

public sealed partial class DtViaFlagFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new DtViaFlagFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// RUN-LEVEL escape hatch: the same flag, but hoisted so the whole write run sits inside
// one arm — which puts the fused reservation back when the flag is true. Costs a duplicated
// Serialize body. This arm isolates "callvirt removed AND fusion restored", i.e. the
// ceiling of what any flag design can buy back.
public sealed class DtFlagFusedFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DtFlagFused>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime> dt = null!;
    bool isDefaultDateTime;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        dt = resolver.GetFormatter<TWriteBuffer, TReadBuffer, DateTime>();
        isDefaultDateTime = dt is UltraMessagePack.Formatters.DateTimeFormatter<TWriteBuffer, TReadBuffer>;
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DtFlagFused value)
    {
        if (isDefaultDateTime)
        {
            ref var d = ref buffer.GetReference(1 + MaxInt32Length + (5 * MaxTimestampLength) + MaxInt32Length);
            var w = 0;
            w += UnsafeWriteFixArrayHeader(ref d, 7);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Id);
            w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.A);
            w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.B);
            w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.C);
            w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.D);
            w += UnsafeWriteTimestamp(ref Unsafe.Add(ref d, w), value.E);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Score);
            buffer.Advance(w);
            return;
        }

        {
            ref var d = ref buffer.GetReference(1 + MaxInt32Length);
            var w = 0;
            w += UnsafeWriteFixArrayHeader(ref d, 7);
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.Id);
            buffer.Advance(w);
        }
        dt.Serialize(ref buffer, ref state, value.A);
        dt.Serialize(ref buffer, ref state, value.B);
        dt.Serialize(ref buffer, ref state, value.C);
        dt.Serialize(ref buffer, ref state, value.D);
        dt.Serialize(ref buffer, ref state, value.E);
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.Score));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DtFlagFused value)
    {
        _ = buffer.ReadArrayHeader();
        value ??= new DtFlagFused();
        value.Id = buffer.ReadInt32();
        if (isDefaultDateTime)
        {
            // reads never fused, so hoisting only removes the other four branches
            value.A = buffer.ReadTimestamp();
            value.B = buffer.ReadTimestamp();
            value.C = buffer.ReadTimestamp();
            value.D = buffer.ReadTimestamp();
            value.E = buffer.ReadTimestamp();
        }
        else
        {
            dt.Deserialize(ref buffer, ref state, ref value.A);
            dt.Deserialize(ref buffer, ref state, ref value.B);
            dt.Deserialize(ref buffer, ref state, ref value.C);
            dt.Deserialize(ref buffer, ref state, ref value.D);
            dt.Deserialize(ref buffer, ref state, ref value.E);
        }
        value.Score = buffer.ReadInt32();
    }
}

public sealed partial class DtFlagFusedFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new DtFlagFusedFormatter<TWriteBuffer, TReadBuffer>();
    }
}

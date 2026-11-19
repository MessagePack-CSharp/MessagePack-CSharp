// MsgPack104: hand-written twins of the generated formatter; the direct primitive calls are the benchmarked subject.
#pragma warning disable MsgPack104

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;
using SerializerFoundation;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// End-to-end companion of FloatRunFuseBenchmark. The blob loop there is latency-bound: object k+1 starts where
// object k ended, with no entry cost in between. These rows put the same two bodies behind the public API as
// formatters in the generated shape (NoInlining Deserialize, nil check, `value ?? new`, state.Enter / Exit, the
// budget-charging header read), so the allocation, the array formatter's per-element interface call and the entry
// sequence dilute the delta as they would for a user.
//   Array  - Deserialize<Float3[]> of 1000 elements: the collection-of-small-objects case the fusion targets
//   Single - Deserialize<Float3> of one object: entry cost dominates
//
// MEASURED (i7-13700KF, .NET 11, ShortRun x2):
//   Array  Loop 8.87 / 9.19 us   Fused 6.66 / 6.68 us   ratio 0.75 / 0.73  (per element 8.9 -> 6.7 ns, 40 B allocated)
//   Single Loop 10.13 / 10.21 ns Fused 8.09 / 8.31 ns   ratio 0.80 / 0.81
// The blob loop's 0.50 dilutes to 0.73-0.81 behind the entry and the allocation, and it survives even on a single
// object. Not in the generator: this is a measurement of the candidate, class shape (populate path) only.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class FloatRunFuseEndToEndBenchmark
{
    const int ElementCount = 1000;

    byte[] arrayPayload = null!;
    byte[] singlePayload = null!;
    MessagePackSerializerOptions loop = null!;
    MessagePackSerializerOptions fused = null!;

    [GlobalSetup]
    public void Setup()
    {
        loop = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new Float3LoopFormatterFactory(),
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
        ]));
        fused = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new Float3FusedFormatterFactory(),
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
        ]));

        var rand = new Random(42);
        var source = new Float3[ElementCount];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = new Float3
            {
                X = (float)(rand.NextDouble() * 2000.0 - 1000.0),
                Y = (float)(rand.NextDouble() * 2000.0 - 1000.0),
                Z = (float)(rand.NextDouble() * 2000.0 - 1000.0),
            };
        }
        arrayPayload = MessagePackSerializer.Serialize(source, loop);
        singlePayload = MessagePackSerializer.Serialize(source[0], loop);
        if (!arrayPayload.AsSpan().SequenceEqual(MessagePackSerializer.Serialize(source, fused)))
        {
            throw new InvalidOperationException("the two formatters serialize differently");
        }

        var a = Array_Loop();
        var b = Array_Fused();
        if (a.Length != ElementCount || b.Length != ElementCount)
        {
            throw new InvalidOperationException("array length mismatch");
        }
        for (int i = 0; i < ElementCount; i++)
        {
            if (!a[i].Bits().AsSpan().SequenceEqual(source[i].Bits()) || !b[i].Bits().AsSpan().SequenceEqual(source[i].Bits()))
            {
                throw new InvalidOperationException($"element {i} mismatch");
            }
        }
        if (!Single_Loop().Bits().AsSpan().SequenceEqual(source[0].Bits()) || !Single_Fused().Bits().AsSpan().SequenceEqual(source[0].Bits()))
        {
            throw new InvalidOperationException("single object mismatch");
        }
    }

    [BenchmarkCategory("Array"), Benchmark(Baseline = true)]
    public Float3[] Array_Loop() => MessagePackSerializer.Deserialize<Float3[]>(arrayPayload, loop)!;

    [BenchmarkCategory("Array"), Benchmark]
    public Float3[] Array_Fused() => MessagePackSerializer.Deserialize<Float3[]>(arrayPayload, fused)!;

    [BenchmarkCategory("Single"), Benchmark(Baseline = true)]
    public Float3 Single_Loop() => MessagePackSerializer.Deserialize<Float3>(singlePayload, loop)!;

    [BenchmarkCategory("Single"), Benchmark]
    public Float3 Single_Fused() => MessagePackSerializer.Deserialize<Float3>(singlePayload, fused)!;
}

// the generated shape for a settable int-key class, member reads through buffer.ReadSingle()
public sealed class Float3LoopFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Float3>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Float3 value)
    {
        buffer.WriteArrayHeader(3);
        buffer.WriteSingle(value.X);
        buffer.WriteSingle(value.Y);
        buffer.WriteSingle(value.Z);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Float3 value)
    {
        if (buffer.TryReadNil())
        {
            value = null!;
            return;
        }

        var result = value ?? new Float3();
        state.Enter();

        var count = buffer.ReadArrayHeader(ref state);
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0:
                    result.X = buffer.ReadSingle();
                    break;
                case 1:
                    result.Y = buffer.ReadSingle();
                    break;
                case 2:
                    result.Z = buffer.ReadSingle();
                    break;
                default:
                    buffer.Skip(); // version tolerance: unknown trailing member
                    break;
            }
        }

        state.Exit();
        value = result;
    }
}

// the same formatter with the fused float run ahead of the loop (FloatRunFuseBenchmark.ReadFusedThenLoop3)
public sealed class Float3FusedFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Float3>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Float3 value)
    {
        buffer.WriteArrayHeader(3);
        buffer.WriteSingle(value.X);
        buffer.WriteSingle(value.Y);
        buffer.WriteSingle(value.Z);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Float3 value)
    {
        if (buffer.TryReadNil())
        {
            value = null!;
            return;
        }

        var result = value ?? new Float3();
        state.Enter();

        var count = buffer.ReadArrayHeader(ref state);
        var i = 0;
        var span = buffer.GetUnreadSpan();
        if (count >= 3 && span.Length >= 15)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            if (((s ^ MessagePackCode.Float32) | (Unsafe.Add(ref s, 5) ^ MessagePackCode.Float32) | (Unsafe.Add(ref s, 10) ^ MessagePackCode.Float32)) == 0)
            {
                result.X = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1))));
                result.Y = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 6))));
                result.Z = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 11))));
                buffer.Advance(15);
                i = 3;
            }
        }
        for (; i < count; i++)
        {
            switch (i)
            {
                case 0:
                    result.X = buffer.ReadSingle();
                    break;
                case 1:
                    result.Y = buffer.ReadSingle();
                    break;
                case 2:
                    result.Z = buffer.ReadSingle();
                    break;
                default:
                    buffer.Skip();
                    break;
            }
        }

        state.Exit();
        value = result;
    }
}

public sealed partial class Float3LoopFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) =>
        type == typeof(Float3) ? new Float3LoopFormatter<TWriteBuffer, TReadBuffer>() : null;
}

public sealed partial class Float3FusedFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) =>
        type == typeof(Float3) ? new Float3FusedFormatter<TWriteBuffer, TReadBuffer>() : null;
}

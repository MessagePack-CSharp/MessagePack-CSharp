using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;

// Round 5: round 4 pinned the trigger to a byref-containing ref struct local combined
// with try/finally (RefStructFinally = NA, GvmFinally = OK). This round asks whether the
// ref struct must be USED INSIDE the funclet (finally/catch body) or merely be live
// across the EH region.
public class DisasmProbe5Benchmark
{
    // ref struct created before try, used only INSIDE try, finally touches nothing of it
    [Benchmark(Baseline = true)]
    public long NotTouchedInFinally()
    {
        var buffer = new ArrayPoolListWriteBuffer(default);
        long result;
        try
        {
            buffer.Advance(UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref buffer.GetReference(UltraMessagePack.MessagePackPrimitives.MaxInt32Length), 42));
            result = buffer.BytesWritten;
        }
        finally
        {
            Blackhole(1);
        }
        buffer.Dispose();
        return result;
    }

    // ref struct read inside the catch funclet (MessagePack-CSharp Deserialize shape:
    // catch { throw new ...($"... at {reader.Consumed}") })
    [Benchmark]
    public long TouchedInCatch()
    {
        var buffer = new ArrayPoolListWriteBuffer(default);
        try
        {
            buffer.Advance(UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref buffer.GetReference(UltraMessagePack.MessagePackPrimitives.MaxInt32Length), 42));
            var result = buffer.BytesWritten;
            buffer.Dispose();
            return result;
        }
        catch (Exception)
        {
            return buffer.BytesWritten;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Blackhole(int x) { }
}

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;

// Where ArrayPoolListWriteBuffer's per-message fixed cost goes, for the tiny-message case the alternating-type
// benchmark exposed (byte[] entry 11 ns per int, SpanWriteBuffer 1.7 ns, formatter alone 1.1 ns). Each row is one
// buffer lifetime around a single 1-byte write: construct, write, (ToArray,) Dispose. The ref struct carries two
// InlineArray fields (16 byte[] references + 17 ints, ~240 bytes with the spans), all zeroed per construction because
// the references are GC-tracked, and Dispose / BytesWritten / WriteTo walk them. SlimPoolListWriteBuffer is a prototype
// of the same contract with the pooled chain moved behind one lazily created reference, so the struct is two spans, an
// int and a reference: the cost of the chain is paid only by messages that outgrow the scratch span.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class WriteBufferSetupBenchmark
{
    // ---- current ArrayPoolListWriteBuffer ----

    [BenchmarkCategory("CtorDispose"), Benchmark(Baseline = true)]
    [SkipLocalsInit]
    public long Current_CtorDispose()
    {
        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[1024]);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("Write"), Benchmark(Baseline = true)]
    [SkipLocalsInit]
    public long Current_Write()
    {
        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("WriteToArray"), Benchmark(Baseline = true)]
    [SkipLocalsInit]
    public int Current_WriteToArray()
    {
        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result.Length;
    }

    // ---- prototype: lazily created pooled chain ----

    [BenchmarkCategory("CtorDispose"), Benchmark]
    [SkipLocalsInit]
    public long Slim_CtorDispose()
    {
        var buffer = new SlimPoolListWriteBuffer(stackalloc byte[1024]);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("Write"), Benchmark]
    [SkipLocalsInit]
    public long Slim_Write()
    {
        var buffer = new SlimPoolListWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("WriteToArray"), Benchmark]
    [SkipLocalsInit]
    public int Slim_WriteToArray()
    {
        var buffer = new SlimPoolListWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result.Length;
    }

    // ---- floor: SpanWriteBuffer, with the same result array ----

    [BenchmarkCategory("Write"), Benchmark]
    [SkipLocalsInit]
    public long Span_Write()
    {
        var buffer = new SpanWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        return buffer.BytesWritten;
    }

    [BenchmarkCategory("WriteToArray"), Benchmark]
    [SkipLocalsInit]
    public int Span_WriteToArray()
    {
        Span<byte> scratch = stackalloc byte[1024];
        var buffer = new SpanWriteBuffer(scratch);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var result = GC.AllocateUninitializedArray<byte>((int)buffer.BytesWritten);
        scratch.Slice(0, result.Length).CopyTo(result);
        return result.Length;
    }

    // the scratch-first chain, with the rented arrays and their lengths behind one reference created on first overflow
    public ref struct SlimPoolListWriteBuffer : IWriteBuffer, IDisposable
    {
        sealed class Chain
        {
            public readonly byte[]?[] Arrays = new byte[]?[16];
            public readonly int[] Lengths = new int[17]; // [0] = scratch, [1..] = pooled
            public int Count;
        }

        Span<byte> scratchBuffer;
        Span<byte> currentBuffer;
        int currentWritten;
        Chain? chain;

        public long BytesWritten
        {
            get
            {
                var c = chain;
                if (c is null)
                {
                    return currentWritten;
                }
                long total = 0;
                for (int i = 0; i < c.Count; i++)
                {
                    total += c.Lengths[i];
                }
                return total + currentWritten;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlimPoolListWriteBuffer(Span<byte> scratchBuffer)
        {
            this.scratchBuffer = scratchBuffer;
            currentBuffer = scratchBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var remaining = currentBuffer.Length - currentWritten;
            if (remaining == 0 || (uint)remaining < (uint)sizeHint)
            {
                return GetSpanSlow(sizeHint);
            }
            return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(currentBuffer), currentWritten), remaining);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        Span<byte> GetSpanSlow(int sizeHint)
        {
            if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
            if (sizeHint == 0) sizeHint = 1;
            if (currentBuffer.Length - currentWritten < sizeHint)
            {
                var c = chain ??= new Chain();
                c.Lengths[c.Count] = currentWritten;
                var minSize = 65_536 << Math.Min(c.Count, 14);
                var newArray = ArrayPool<byte>.Shared.Rent(Math.Max(sizeHint, minSize));
                c.Arrays[c.Count++] = newArray;
                currentBuffer = newArray;
                currentWritten = 0;
            }
            return currentBuffer.Slice(currentWritten);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int bytesWritten)
        {
            if ((uint)bytesWritten > (uint)(currentBuffer.Length - currentWritten))
            {
                throw new InvalidOperationException("advanced too far");
            }
            currentWritten += bytesWritten;
        }

        public byte[] ToArray()
        {
            var totalLength = checked((int)BytesWritten);
            if (totalLength == 0) return [];
            var result = GC.AllocateUninitializedArray<byte>(totalLength);
            WriteTo(result);
            return result;
        }

        public void WriteTo(Span<byte> destination)
        {
            var c = chain;
            if (c is null)
            {
                scratchBuffer.Slice(0, currentWritten).CopyTo(destination);
                return;
            }
            var scratchLen = c.Lengths[0];
            scratchBuffer.Slice(0, scratchLen).CopyTo(destination);
            destination = destination.Slice(scratchLen);
            for (int i = 0; i < c.Count; i++)
            {
                var len = i < c.Count - 1 ? c.Lengths[i + 1] : currentWritten;
                c.Arrays[i]!.AsSpan(0, len).CopyTo(destination);
                destination = destination.Slice(len);
            }
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
            var c = chain;
            if (c is not null)
            {
                for (int i = 0; i < c.Count; i++)
                {
                    if (c.Arrays[i] is { } array)
                    {
                        ArrayPool<byte>.Shared.Return(array);
                        c.Arrays[i] = null;
                    }
                }
                chain = null;
            }
            currentWritten = 0;
            currentBuffer = default;
        }
    }
}

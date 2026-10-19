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
//
// MEASURED 2026-10-19 (i7-13700KF, .NET 11 RC1, 10 warmup / 20 iterations, ns per buffer lifetime):
//   ctor + Dispose:              current 2.90 | slim 1.02
//   + one 1-byte write:          current 3.50 | slim 2.08 | SpanWriteBuffer 0.99
//   + ToArray (32 B result):     current 7.07 | slim 5.49 | SpanWriteBuffer + alloc + copy 2.72
// Zeroing and clearing the ~240-byte struct is ~1.9 ns per message; ToArray adds 3.6 ns of which the allocation and
// the copy themselves are 1.7 (BytesWritten's loop and WriteTo's sliced CopyTo path are the rest). The slim shape saves
// 1.6 ns per message as written, up to ~3.5 with a chain-less fast path in ToArray; the remaining ~4 ns of the 11 ns
// byte[] entry per int are outside the buffer (formatter interface call, resolver lookup, state, processor check).
//
// v2 (PooledChainWriteBuffer: the 16 references and 17 lengths rented from ArrayPool<byte[]?> / ArrayPool<int> on the
// first overflow, returned by Dispose): small path 1.09 / 1.96 / 5.54, the same as the lazily allocated chain, with no
// allocation on either path. The 70 KB overflow path (1 KB scratch, then 64 KB and 128 KB segments, one byte written
// per segment so only the chain management is timed) costs 22.0 ns current vs 33.4 ns pooled: +11 ns for the four
// extra pool operations, on a message whose serialization itself is in the microseconds.
//
// SerializerFoundation 1.2.0 (2026-10-19) ships a lighter ArrayPoolListWriteBuffer; the Current_ rows on it:
//   ctor + Dispose 1.15 | + write 2.30 | + ToArray 4.99 | overflow 26.3 (no allocation). The v1/v2 prototype rows
//   above are kept as the reference points they were measured against (1.1.0: 2.87 / 3.51 / 7.28 / 21.97).
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

    // ---- prototype v2: chain bookkeeping rented from ArrayPool (no allocation on either path) ----

    [BenchmarkCategory("CtorDispose"), Benchmark]
    [SkipLocalsInit]
    public long Pooled_CtorDispose()
    {
        var buffer = new PooledChainWriteBuffer(stackalloc byte[1024]);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("Write"), Benchmark]
    [SkipLocalsInit]
    public long Pooled_Write()
    {
        var buffer = new PooledChainWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var n = buffer.BytesWritten;
        buffer.Dispose();
        return n;
    }

    [BenchmarkCategory("WriteToArray"), Benchmark]
    [SkipLocalsInit]
    public int Pooled_WriteToArray()
    {
        var buffer = new PooledChainWriteBuffer(stackalloc byte[1024]);
        buffer.GetSpan(1)[0] = 0x08;
        buffer.Advance(1);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result.Length;
    }

    // ---- overflow: 70 KB through a 1 KB scratch (two pooled segments: 64 KB, then 128 KB), no ToArray so the chain
    // management is what differs; Allocated must stay zero for both ----

    const int OverflowBytes = 70 * 1024;

    [BenchmarkCategory("Overflow"), Benchmark(Baseline = true)]
    [SkipLocalsInit]
    public long Current_Overflow()
    {
        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[1024]);
        var left = OverflowBytes;
        while (left > 0)
        {
            var span = buffer.GetSpan(1);
            var n = Math.Min(span.Length, left);
            span[0] = 1;
            buffer.Advance(n);
            left -= n;
        }
        var total = buffer.BytesWritten;
        buffer.Dispose();
        return total;
    }

    [BenchmarkCategory("Overflow"), Benchmark]
    [SkipLocalsInit]
    public long Pooled_Overflow()
    {
        var buffer = new PooledChainWriteBuffer(stackalloc byte[1024]);
        var left = OverflowBytes;
        while (left > 0)
        {
            var span = buffer.GetSpan(1);
            var n = Math.Min(span.Length, left);
            span[0] = 1;
            buffer.Advance(n);
            left -= n;
        }
        var total = buffer.BytesWritten;
        buffer.Dispose();
        return total;
    }

    // scratch first; on the first overflow the two bookkeeping arrays (16 segment references, 17 lengths) are rented from
    // the shared pools next to the 64 KB segment and returned by Dispose: nothing is allocated on either path
    public ref struct PooledChainWriteBuffer : IWriteBuffer, IDisposable
    {
        Span<byte> scratchBuffer;
        Span<byte> currentBuffer;
        int currentWritten;
        byte[]?[]? pooledArrays;   // rented, length >= 16
        int[]? completedLengths;   // rented, length >= 17: [0] = scratch, [1..] = pooled
        int pooledCount;

        public long BytesWritten
        {
            get
            {
                var lengths = completedLengths;
                if (lengths is null)
                {
                    return currentWritten;
                }
                long total = 0;
                for (int i = 0; i < pooledCount; i++)
                {
                    total += lengths[i];
                }
                return total + currentWritten;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledChainWriteBuffer(Span<byte> scratchBuffer)
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
                var arrays = pooledArrays ??= ArrayPool<byte[]?>.Shared.Rent(16);
                var lengths = completedLengths ??= ArrayPool<int>.Shared.Rent(17);
                if (pooledCount == 16) throw new InvalidOperationException("insufficient space in buffer");
                lengths[pooledCount] = currentWritten;
                var minSize = 65_536 << Math.Min(pooledCount, 14);
                var newArray = ArrayPool<byte>.Shared.Rent(Math.Max(sizeHint, minSize));
                arrays[pooledCount++] = newArray;
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
            var arrays = pooledArrays;
            if (arrays is null)
            {
                scratchBuffer.Slice(0, currentWritten).CopyTo(destination);
                return;
            }
            var lengths = completedLengths!;
            var scratchLen = lengths[0];
            scratchBuffer.Slice(0, scratchLen).CopyTo(destination);
            destination = destination.Slice(scratchLen);
            for (int i = 0; i < pooledCount; i++)
            {
                var len = i < pooledCount - 1 ? lengths[i + 1] : currentWritten;
                arrays[i]!.AsSpan(0, len).CopyTo(destination);
                destination = destination.Slice(len);
            }
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
            var arrays = pooledArrays;
            if (arrays is not null)
            {
                for (int i = 0; i < pooledCount; i++)
                {
                    if (arrays[i] is { } array)
                    {
                        ArrayPool<byte>.Shared.Return(array);
                        arrays[i] = null; // the reference array goes back clean
                    }
                }
                ArrayPool<byte[]?>.Shared.Return(arrays);
                ArrayPool<int>.Shared.Return(completedLengths!);
                pooledArrays = null;
                completedLengths = null;
                pooledCount = 0;
            }
            currentWritten = 0;
            currentBuffer = default;
        }
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

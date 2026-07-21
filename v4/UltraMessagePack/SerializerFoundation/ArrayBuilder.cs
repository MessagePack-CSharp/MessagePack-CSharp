namespace SerializerFoundation;

// for security usecase
// similar as SegmentedArrayBuilder in System.Linq(use in ToArray/ToList)
public struct ArrayBuilder<T> : IDisposable
{
    internal Arrays segments;
    internal int nextSegmentIndex;

    public Span<T> GetNextSegment()
    {
        var segmentLength = GetSegmentLength(nextSegmentIndex);
        var newArray = ArrayPool<T>.Shared.Rent(segmentLength);
        segments[nextSegmentIndex++] = newArray;
        return newArray.AsSpan(0, segmentLength);
    }

    public T[] ToArray(int lastSegmentCount)
    {
        var length = GetLength(lastSegmentCount);
        if (length == 0)
        {
            return [];
        }

        var array = GC.AllocateUninitializedArray<T>(length);
        WriteTo(array, lastSegmentCount);
        return array;
    }

    public void WriteTo(Span<T> destination, int lastSegmentCount)
    {
        for (int i = 0; i < nextSegmentIndex - 1; i++)
        {
            var segmentLength = GetSegmentLength(i);
            segments[i]!.AsSpan(0, segmentLength).CopyTo(destination);
            destination = destination.Slice(segmentLength);
        }

        if (lastSegmentCount > 0)
        {
            segments[nextSegmentIndex - 1]!.AsSpan(0, lastSegmentCount).CopyTo(destination);
        }
    }

    public int GetLength(int lastSegmentCount)
    {
        return GetPreviousSegmentsTotal(nextSegmentIndex - 1) + lastSegmentCount;
    }

    static int GetSegmentLength(int index) => index switch
    {
        0 => 65_536, // 64K for initial allocation
        1 => 131_072,
        2 => 262_144,
        3 => 524_288,
        4 => 1_048_576,
        5 => 2_097_152,
        6 => 4_194_304,
        7 => 8_388_608,
        8 => 16_777_216,
        9 => 33_554_432,
        10 => 67_108_864,
        11 => 134_217_728,
        12 => 268_435_456,
        13 => 536_870_912,
        14 => 1_073_741_824,
        15 => 65_479, // Array.MaxLength(2,147,483,591) - 2,147,418,112
        _ => Throws.InsufficientSpaceInBuffer<int>(),
    };

    static int GetPreviousSegmentsTotal(int count) => count switch
    {
        0 => 0,
        1 => 65_536,
        2 => 196_608,
        3 => 458_752,
        4 => 983_040,
        5 => 2_031_616,
        6 => 4_128_768,
        7 => 8_323_072,
        8 => 16_711_680,
        9 => 33_488_896,
        10 => 67_043_328,
        11 => 134_152_192,
        12 => 268_369_920,
        13 => 536_805_376,
        14 => 1_073_676_288,
        15 => 2_147_418_112,
        16 => Array.MaxLength, // 2,147,483,591
        _ => 0,
    };

    public void Dispose()
    {
        var clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

        for (int i = 0; i < nextSegmentIndex; i++)
        {
            var segment = segments[i];
            if (segment != null)
            {
                ArrayPool<T>.Shared.Return(segment, clearArray);
                segments[i] = null;
            }
        }

        nextSegmentIndex = 0;
    }

#if NET9_0_OR_GREATER

    [InlineArray(16)]
    internal struct Arrays
    {
        public T[]? values;
    }

#else
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct Arrays
    {
        T[]? _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15;

        public ref T[]? this[int index]
        {
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _0, index);
        }
    }

#endif
}

public static class ArrayBuilderExtensions
{
    extension(ref ArrayBuilder<char> arrayBuilder)
    {
        public string ToString(int lastSegmentCount)
        {
            if (arrayBuilder.nextSegmentIndex == 0) return "";

            var length = arrayBuilder.GetLength(lastSegmentCount);
            if (length == 0) return "";

            // avoid string.Create(Func) struct copy-cost.
            var str = string.FastAllocateString(length);
            unsafe
            {
                fixed (char* destPointer = str.AsSpan())
                {
                    Span<char> dest = new Span<char>(destPointer, length);
                    arrayBuilder.WriteTo(dest, lastSegmentCount);
                }
            }
            return str;
        }
    }
}

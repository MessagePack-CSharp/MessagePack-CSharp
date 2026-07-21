namespace SerializerFoundation;

internal static class Polyfill
{
    extension(String)
    {
#if NET8_0_OR_GREATER
        internal static string FastAllocateString(int length) => string.Create(length, (object?)null, static (_, _) => { });
#else
        internal static string FastAllocateString(int length) => new string('\0', length);
#endif
    }

#if !NET9_0_OR_GREATER

    extension(GC)
    {
        internal static T[] AllocateUninitializedArray<T>(int length)
        {
            return new T[length];
        }
    }

    extension(Array)
    {
        internal static int MaxLength => 0X7FFFFFC7;
    }

#if NETSTANDARD2_0

    extension(RuntimeHelpers)
    {
        internal static bool IsReferenceOrContainsReferences<T>()
        {
            if (typeof(T).IsPrimitive)
            {
                return false;
            }
            else
            {
                // this method is called for ArrayPool.Return to determine whether to clear the array so allows false-negative
                return true;
            }
        }
    }

    extension<T>(ReadOnlySequence<T> sequence)
    {
        internal ReadOnlySpan<T> FirstSpan => sequence.First.Span;
    }

#endif

#endif
}


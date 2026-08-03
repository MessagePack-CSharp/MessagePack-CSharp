#if NETSTANDARD2_0
using System.Text;
#endif

namespace UltraMessagePack
{
    internal static class Polyfill
    {
#if !NET9_0_OR_GREATER

        extension(GC)
        {
            internal static T[] AllocateUninitializedArray<T>(int length)
            {
                return new T[length];
            }
        }

        extension(MemoryMarshal)
        {
            internal static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetReference(array.AsSpan());
        }

        extension(Unsafe)
        {
            internal static TTo BitCast<TFrom, TTo>(TFrom source)
                where TFrom : struct
                where TTo : struct
            {
                return Unsafe.As<TFrom, TTo>(ref source);
            }
        }

        extension(BitConverter)
        {
            internal static float UInt32BitsToSingle(uint value) => Unsafe.As<uint, float>(ref value);
            internal static double UInt64BitsToDouble(ulong value) => Unsafe.As<ulong, double>(ref value);
            internal static uint SingleToUInt32Bits(float value) => Unsafe.As<float, uint>(ref value);
            internal static ulong DoubleToUInt64Bits(double value) => Unsafe.As<double, ulong>(ref value);
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

        extension(Encoding encoding)
        {
            internal unsafe string GetString(ReadOnlySpan<byte> bytes)
            {
                if (bytes.Length == 0)
                {
                    return string.Empty;
                }
                fixed (byte* pointer = &MemoryMarshal.GetReference(bytes))
                {
                    return encoding.GetString(pointer, bytes.Length);
                }
            }
        }

#endif

#endif
    }
}

#if !NET

namespace System.Numerics
{
    internal static class BitOperations
    {
        public static bool IsPow2(int value) => (value & (value - 1)) == 0 && value > 0;

        public static int Log2(uint value)
        {
            int n = 0;
            if (value >= 1u << 16) { n += 16; value >>= 16; }
            if (value >= 1u << 8) { n += 8; value >>= 8; }
            if (value >= 1u << 4) { n += 4; value >>= 4; }
            if (value >= 1u << 2) { n += 2; value >>= 2; }
            if (value >= 2) { n += 1; }
            return n;
        }

        public static int LeadingZeroCount(uint value) => value == 0 ? 32 : 31 - Log2(value);

        public static int LeadingZeroCount(ulong value) =>
            (value >> 32) == 0 ? 32 + LeadingZeroCount((uint)value) : LeadingZeroCount((uint)(value >> 32));
    }
}

namespace System.Text.Unicode
{
    internal static class Utf8
    {
        public static unsafe System.Buffers.OperationStatus FromUtf16(
            ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten,
            bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            if (source.Length == 0)
            {
                charsRead = 0;
                bytesWritten = 0;
                return System.Buffers.OperationStatus.Done;
            }
#if NETSTANDARD2_1
            bytesWritten = System.Text.Encoding.UTF8.GetBytes(source, destination);
#else
            fixed (char* chars = &System.Runtime.InteropServices.MemoryMarshal.GetReference(source))
            fixed (byte* bytes = &System.Runtime.InteropServices.MemoryMarshal.GetReference(destination))
            {
                bytesWritten = System.Text.Encoding.UTF8.GetBytes(chars, source.Length, bytes, destination.Length);
            }
#endif
            charsRead = source.Length;
            return System.Buffers.OperationStatus.Done;
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    // NOT covered by PolySharp 1.16 — inert-but-compiling metadata downlevel (AOT analysis only ever runs on the net10.0 build)

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute(string message) : Attribute
    {
        public string Message => message;
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
    {
        public string Category => category;
        public string CheckId => checkId;
        public string? Scope { get; set; }
        public string? Target { get; set; }
        public string? MessageId { get; set; }
        public string? Justification { get; set; }
    }
}

#endif

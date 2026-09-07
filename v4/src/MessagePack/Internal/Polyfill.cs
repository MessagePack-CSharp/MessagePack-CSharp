using System.Text;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace MessagePack
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

        extension<T>(IEnumerable<T> source)
        {
            internal bool TryGetNonEnumeratedCount(out int count)
            {
                switch (source)
                {
                    case ICollection<T> collection:
                        count = collection.Count;
                        return true;
                    case IReadOnlyCollection<T> readOnlyCollection:
                        count = readOnlyCollection.Count;
                        return true;
                    default:
                        count = 0;
                        return false;
                }
            }
        }

        // ArgumentNullException.ThrowIfNull (net6.0+), callable via static extension
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                {
                    ThrowArgumentNull(paramName);
                }
            }

            [DoesNotReturn]
            static void ThrowArgumentNull(string? paramName) => throw new ArgumentNullException(paramName);
        }

        extension(MethodInfo method)
        {
            public TDelegate CreateDelegate<TDelegate>()
                where TDelegate : Delegate
            {
                return (TDelegate)method.CreateDelegate(typeof(TDelegate));
            }
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
                    // called by ArrayPool.Return to decide whether to clear the array, so a false negative is allowed
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

        // string.Contains(string, StringComparison) (netstandard2.1+)
        public static bool Contains(this string text, string value, StringComparison comparisonType)
        {
            return text.IndexOf(value, comparisonType) >= 0;
        }

        // Dictionary<TKey, TValue>.TryAdd (netstandard2.1+)
        extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            internal bool TryAdd(TKey key, TValue value)
            {
                if (dictionary.ContainsKey(key))
                {
                    return false;
                }
                dictionary.Add(key, value);
                return true;
            }
        }

#endif

#endif
    }
}

#if !NET9_0_OR_GREATER

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

namespace System.Collections.Generic
{
    // ReferenceEqualityComparer (net5.0+), the subset Dictionary needs
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        ReferenceEqualityComparer()
        {
        }

        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object? obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

namespace System.Threading
{
    internal sealed class Lock
    {
#pragma warning disable CS9216
        public void Enter() => Monitor.Enter(this);
        public void Exit() => Monitor.Exit(this);
        public bool TryEnter() => Monitor.TryEnter(this);

        public Scope EnterScope()
        {
            Monitor.Enter(this);
            return new Scope(this);
        }

        public ref struct Scope
        {
            readonly Lock _owner;
            internal Scope(Lock owner) => _owner = owner;
            public void Dispose() => Monitor.Exit(_owner);
        }
#pragma warning restore CS9216
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    // Not covered by PolySharp 1.16. Inert-but-compiling metadata downlevel (AOT analysis only ever runs on the net10.0 build).

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

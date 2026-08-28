using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MessagePack;

/// <summary>
/// Supplies hash-flooding-resistant <see cref="IEqualityComparer{T}"/> instances for
/// hash-based collections (<c>Dictionary&lt;TKey,&gt;</c> / <c>HashSet&lt;T&gt;</c>) built
/// during deserialization, so that adversarial payloads cannot force O(n²) bucket collisions.
/// Hashing is SipHash-1-3 (the Rust/Ruby/Python std HashMap variant) keyed with a per-process random 128-bit key.
/// </summary>
internal static class HashFloodingResistantEqualityComparer
{
    // per-process random 128-bit key
    internal static readonly ulong sipHashKey0;
    internal static readonly ulong sipHashKey1;

    static HashFloodingResistantEqualityComparer()
    {
        var key = new byte[16];
#if NET
        RandomNumberGenerator.Fill(key);
#else
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
#endif
        sipHashKey0 = BinaryPrimitives.ReadUInt64LittleEndian(key);
        sipHashKey1 = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(8));
    }

    /// <summary>
    /// The hash-flooding-resistant comparer for <typeparamref name="T"/>, or
    /// <see langword="null"/> when <typeparamref name="T"/> is outside the supported set
    /// (callers pass null through, keeping the collection's default comparer).
    /// </summary>
    public static IEqualityComparer<T>? Get<T>() => Cache<T>.Instance;

    static class Cache<T>
    {
        internal static readonly IEqualityComparer<T>? Instance = Create();

        static IEqualityComparer<T>? Create()
        {
            var t = typeof(T);

            if (t == typeof(bool)
             || t == typeof(char)
             || t == typeof(sbyte)
             || t == typeof(byte)
             || t == typeof(short)
             || t == typeof(ushort)
             || t == typeof(int)
             || t == typeof(uint)
             || t == typeof(long)
             || t == typeof(ulong)
             || t == typeof(Guid)
             || t.IsEnum)
            {
                // Direct generic instantiation (no MakeGenericType), so enums stay AOT-safe.
                return Unsafe.SizeOf<T>() switch
                {
                    1 => new BitwiseHashComparer1<T>(),
                    2 => new BitwiseHashComparer2<T>(),
                    4 => new BitwiseHashComparer4<T>(),
                    8 => new BitwiseHashComparer8<T>(),
                    16 => new BitwiseHashComparer16<T>(), // Guid
                    _ => throw new InvalidOperationException($"unexpected bitwise key size for {typeof(T)}"),
                };
            }

            if (t == typeof(float)) return (IEqualityComparer<T>)(object)SingleHashComparer.Instance;
            if (t == typeof(double)) return (IEqualityComparer<T>)(object)DoubleHashComparer.Instance;
            if (t == typeof(DateTime)) return (IEqualityComparer<T>)(object)DateTimeHashComparer.Instance;
            if (t == typeof(DateTimeOffset)) return (IEqualityComparer<T>)(object)DateTimeOffsetHashComparer.Instance;

            if (t == typeof(object)) return (IEqualityComparer<T>)(object)ObjectFallbackHashComparer.Instance;

#if !NET
            // Modern .NET's Dictionary<string,V> with a null comparer already defends HashDoS by itself.
            // (non-randomized fast hash, swapped for randomized Marvin once a bucket chain passes the collision threshold)
            if (t == typeof(string)) return (IEqualityComparer<T>)(object)StringHashComparer.Instance;
#endif

            // Only built-in types are covered, not custom user types and so on.
            // Since such types are unlikely to be used as keys, and the difficulty of an attack is different
            // (Hash.Combine uses xxHash32 and is not HashDoS resistant, but it is still different from int and similar types),
            // the serializer probably does not need to take responsibility for that part.
            return null;
        }
    }
}

#region Comparers

sealed class BitwiseHashComparer1<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x!, y!);

    public int GetHashCode(T value)
    {
        ulong bits = Unsafe.As<T, byte>(ref value);
        var h = SipHash.Hash64Fixed(bits, 1, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class BitwiseHashComparer2<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x!, y!);

    public int GetHashCode(T value)
    {
        ulong bits = Unsafe.As<T, ushort>(ref value);
        var h = SipHash.Hash64Fixed(bits, 2, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class BitwiseHashComparer4<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x!, y!);

    public int GetHashCode(T value)
    {
        ulong bits = Unsafe.As<T, uint>(ref value);
        var h = SipHash.Hash64Fixed(bits, 4, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class BitwiseHashComparer8<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x!, y!);

    public int GetHashCode(T value)
    {
        ulong bits = Unsafe.As<T, ulong>(ref value);
        var h = SipHash.Hash64Fixed(bits, 8, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class BitwiseHashComparer16<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x!, y!);

    public int GetHashCode(T value)
    {
        ref var first = ref Unsafe.As<T, byte>(ref value);
        var bits0 = Unsafe.ReadUnaligned<ulong>(ref first);
        var bits1 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref first, 8));
        var h = SipHash.Hash64Fixed16(bits0, bits1, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class SingleHashComparer : IEqualityComparer<float>
{
    internal static readonly SingleHashComparer Instance = new();

    public bool Equals(float x, float y) => x.Equals(y);

    public int GetHashCode(float value)
    {
        // Default equality treats -0 == +0 and any NaN == any NaN; canonicalize both so equal values hash equal.
        if (value == 0f)
        {
            value = 0f;
        }
        else if (float.IsNaN(value))
        {
            value = float.NaN;
        }
        var h = SipHash.Hash64Fixed(BitConverter.SingleToUInt32Bits(value), 4, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class DoubleHashComparer : IEqualityComparer<double>
{
    internal static readonly DoubleHashComparer Instance = new();

    public bool Equals(double x, double y) => x.Equals(y);

    public int GetHashCode(double value)
    {
        if (value == 0d)
        {
            value = 0d;
        }
        else if (double.IsNaN(value))
        {
            value = double.NaN;
        }
        var h = SipHash.Hash64Fixed(BitConverter.DoubleToUInt64Bits(value), 8, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class StringHashComparer : IEqualityComparer<string>
{
    internal static readonly StringHashComparer Instance = new();

    public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

    public int GetHashCode(string value)
    {
        var data = MemoryMarshal.AsBytes(value.AsSpan());
        var h = SipHash.Hash64(data, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32)); // fold, don't truncate
    }
}

sealed class DateTimeHashComparer : IEqualityComparer<DateTime>
{
    internal static readonly DateTimeHashComparer Instance = new();

    public bool Equals(DateTime x, DateTime y) => x == y;

    // Equals compare Ticks only (Kind is ignored), so hash Ticks only
    public int GetHashCode(DateTime value)
    {
        var h = SipHash.Hash64Fixed(unchecked((ulong)value.Ticks), 8, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class DateTimeOffsetHashComparer : IEqualityComparer<DateTimeOffset>
{
    internal static readonly DateTimeOffsetHashComparer Instance = new();

    public bool Equals(DateTimeOffset x, DateTimeOffset y) => x == y;

    // compares the UTC instant (offset is ignored), so hash UtcTicks only
    public int GetHashCode(DateTimeOffset value)
    {
        var h = SipHash.Hash64Fixed(unchecked((ulong)value.UtcTicks), 8, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

sealed class ObjectFallbackHashComparer : IEqualityComparer<object>
{
    internal static readonly ObjectFallbackHashComparer Instance = new();

    public new bool Equals(object? x, object? y) => object.Equals(x, y);

    public int GetHashCode(object value)
    {
        return value switch
        {
            // string cannot use Get<string>() (null = pass-through on NET)
            // object keys must never pass through, the attacker picks the runtime type
            string v => StringHashComparer.Instance.GetHashCode(v),
            int v => HashVia(v),
            long v => HashVia(v),
            ulong v => HashVia(v),
            uint v => HashVia(v),
            byte v => HashVia(v),
            sbyte v => HashVia(v),
            short v => HashVia(v),
            ushort v => HashVia(v),
            bool v => HashVia(v),
            char v => HashVia(v),
            float v => HashVia(v),
            double v => HashVia(v),
            Guid v => HashVia(v),
            DateTime v => HashVia(v),
            DateTimeOffset v => HashVia(v),
            _ => value.GetHashCode(),
        };
    }

    static int HashVia<T>(T value) where T : notnull => HashFloodingResistantEqualityComparer.Get<T>()!.GetHashCode(value);
}

#endregion
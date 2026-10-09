using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MessagePack;

/// <summary>
/// Supplies hash-flooding-resistant <see cref="IEqualityComparer{T}"/> instances for the hash-based collections built during deserialization,
/// so that adversarial payloads cannot force quadratic bucket collisions.
/// Hashing is SipHash-1-3, the variant Rust, Ruby and Python use, keyed with a per-process random 128-bit key.
/// </summary>
internal static class HashFloodingResistantEqualityComparer
{
    // per-process random 128-bit key
    internal static readonly ulong sipHashKey0;
    internal static readonly ulong sipHashKey1;

    static HashFloodingResistantEqualityComparer()
    {
        var key = new byte[16];
#if NET9_0_OR_GREATER
        RandomNumberGenerator.Fill(key);
#else
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
#endif
        sipHashKey0 = BinaryPrimitives.ReadUInt64LittleEndian(key);
        sipHashKey1 = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(8));
    }

    /// <summary>
    /// The hash-flooding-resistant comparer for <typeparamref name="T"/>, or null when the type is outside the supported set.
    /// Callers pass null through, keeping the collection's default comparer.
    /// </summary>
    public static IEqualityComparer<T>? Get<T>() => Cache<T>.Instance;

    /// <summary>
    /// True when a populate-reused hash collection must be replaced by a fresh one: the chain has a resistant comparer
    /// for this key type but the instance runs on the default comparer (a <c>= new()</c> property initializer), so
    /// reusing it would silently drop the protection the resolver promised. A caller-chosen comparer is kept.
    /// </summary>
    public static bool ReplacesDefault<T>(IEqualityComparer<T>? resistant, IEqualityComparer<T> instanceComparer)
        => resistant is not null && ReferenceEquals(instanceComparer, EqualityComparer<T>.Default);

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
             || t == typeof(nint)
             || t == typeof(nuint)
             || t == typeof(TimeSpan) // a single long of ticks; Equals compares ticks
             || t == typeof(Guid)
#if NET9_0_OR_GREATER
             || t == typeof(DateOnly) // an int day number
             || t == typeof(TimeOnly) // a long of ticks
             || t == typeof(Int128)
             || t == typeof(UInt128)
#endif
             || t.IsEnum)
            {
                // Direct generic instantiation (no MakeGenericType), so enums stay AOT-safe.
                return Unsafe.SizeOf<T>() switch
                {
                    1 => new BitwiseHashComparer1<T>(),
                    2 => new BitwiseHashComparer2<T>(),
                    4 => new BitwiseHashComparer4<T>(),
                    8 => new BitwiseHashComparer8<T>(),
                    16 => new BitwiseHashComparer16<T>(), // Guid, Int128, UInt128
                    _ => throw new InvalidOperationException($"unexpected bitwise key size for {typeof(T)}"),
                };
            }

            if (t == typeof(float)) return (IEqualityComparer<T>)(object)SingleHashComparer.Instance;
            if (t == typeof(double)) return (IEqualityComparer<T>)(object)DoubleHashComparer.Instance;
            if (t == typeof(decimal)) return (IEqualityComparer<T>)(object)DecimalHashComparer.Instance;
#if NET9_0_OR_GREATER
            if (t == typeof(Half)) return (IEqualityComparer<T>)(object)HalfHashComparer.Instance;
#endif
            if (t == typeof(DateTime)) return (IEqualityComparer<T>)(object)DateTimeHashComparer.Instance;
            if (t == typeof(DateTimeOffset)) return (IEqualityComparer<T>)(object)DateTimeOffsetHashComparer.Instance;

            if (t == typeof(object)) return (IEqualityComparer<T>)(object)ObjectFallbackHashComparer.Instance;

            // Nullable<U> hashes as U does (null as 0), so a nullable key is exactly as floodable as its underlying type
            if (Nullable.GetUnderlyingType(t) is { } underlying) return CreateNullable(underlying);

#if !NET9_0_OR_GREATER
            // Modern .NET's Dictionary<string,V> with a null comparer already defends HashDoS by itself
            // (a non-randomized fast hash, swapped for randomized Marvin once a bucket chain passes the collision threshold).
            if (t == typeof(string)) return (IEqualityComparer<T>)(object)StringHashComparer.Instance;
#endif

            // Only built-in types are covered, not user types. Such types are unlikely to be used as keys, and the
            // difficulty of an attack is different (HashCode.Combine uses xxHash32 and is not HashDoS resistant, but it is
            // still different from int and similar types), so the serializer does not take responsibility for that part.
            return null;
        }

        // the explicit list keeps every NullableHashComparer<U> instantiation statically visible to Native AOT
        static IEqualityComparer<T>? CreateNullable(Type underlying)
        {
            if (underlying == typeof(bool)) return Wrap<bool>();
            if (underlying == typeof(char)) return Wrap<char>();
            if (underlying == typeof(sbyte)) return Wrap<sbyte>();
            if (underlying == typeof(byte)) return Wrap<byte>();
            if (underlying == typeof(short)) return Wrap<short>();
            if (underlying == typeof(ushort)) return Wrap<ushort>();
            if (underlying == typeof(int)) return Wrap<int>();
            if (underlying == typeof(uint)) return Wrap<uint>();
            if (underlying == typeof(long)) return Wrap<long>();
            if (underlying == typeof(ulong)) return Wrap<ulong>();
            if (underlying == typeof(Guid)) return Wrap<Guid>();
            if (underlying == typeof(float)) return Wrap<float>();
            if (underlying == typeof(double)) return Wrap<double>();
            if (underlying == typeof(decimal)) return Wrap<decimal>();
            if (underlying == typeof(DateTime)) return Wrap<DateTime>();
            if (underlying == typeof(DateTimeOffset)) return Wrap<DateTimeOffset>();
            if (underlying == typeof(TimeSpan)) return Wrap<TimeSpan>();
            if (underlying == typeof(nint)) return Wrap<nint>();
            if (underlying == typeof(nuint)) return Wrap<nuint>();
#if NET9_0_OR_GREATER
            if (underlying == typeof(Half)) return Wrap<Half>();
            if (underlying == typeof(DateOnly)) return Wrap<DateOnly>();
            if (underlying == typeof(TimeOnly)) return Wrap<TimeOnly>();
            if (underlying == typeof(Int128)) return Wrap<Int128>();
            if (underlying == typeof(UInt128)) return Wrap<UInt128>();
#endif
            // Nullable<TEnum> is not covered: reaching TEnum from here needs MakeGenericType (not AOT-safe), and reading
            // the enum out of Nullable<TEnum>'s bytes would rest on a field layout no runtime guarantees. A nullable
            // enum key is a rare shape, so it keeps the default comparer.
            return null;

            static IEqualityComparer<T>? Wrap<TUnderlying>()
                where TUnderlying : struct
            {
                return Cache<TUnderlying>.Instance is null ? null : (IEqualityComparer<T>)(object)new NullableHashComparer<TUnderlying>();
            }
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

// Nullable<TUnderlying> over the underlying type's resistant comparer: null hashes to 0 and equals only null
sealed class NullableHashComparer<TUnderlying> : IEqualityComparer<TUnderlying?>
    where TUnderlying : struct
{
    readonly IEqualityComparer<TUnderlying> inner = HashFloodingResistantEqualityComparer.Get<TUnderlying>()!;

    public bool Equals(TUnderlying? x, TUnderlying? y)
    {
        return x.HasValue ? y.HasValue && inner.Equals(x.GetValueOrDefault(), y.GetValueOrDefault()) : !y.HasValue;
    }

    public int GetHashCode(TUnderlying? value) => value.HasValue ? inner.GetHashCode(value.GetValueOrDefault()) : 0;
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

sealed class DecimalHashComparer : IEqualityComparer<decimal>
{
    internal static readonly DecimalHashComparer Instance = new();

    public bool Equals(decimal x, decimal y) => x == y;

    public int GetHashCode(decimal value)
    {
        // equal decimals can differ in scale (1.0m == 1.00m) and in the sign of zero: dividing by a 1 of maximal
        // scale strips the trailing zeros, so equal values share one bit pattern
        value = value == 0m ? 0m : value / 1.0000000000000000000000000000m;
        Span<int> bits = stackalloc int[4];
#if NET9_0_OR_GREATER
        decimal.GetBits(value, bits);
#else
        decimal.GetBits(value).AsSpan().CopyTo(bits);
#endif
        var bits0 = (ulong)(uint)bits[0] | ((ulong)(uint)bits[1] << 32);
        var bits1 = (ulong)(uint)bits[2] | ((ulong)(uint)bits[3] << 32);
        var h = SipHash.Hash64Fixed16(bits0, bits1, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}

#if NET9_0_OR_GREATER
sealed class HalfHashComparer : IEqualityComparer<Half>
{
    internal static readonly HalfHashComparer Instance = new();

    public bool Equals(Half x, Half y) => x.Equals(y);

    public int GetHashCode(Half value)
    {
        // as SingleHashComparer: -0 == +0 and any NaN == any NaN
        if (value == Half.Zero)
        {
            value = Half.Zero;
        }
        else if (Half.IsNaN(value))
        {
            value = Half.NaN;
        }
        var h = SipHash.Hash64Fixed(BitConverter.HalfToUInt16Bits(value), 2, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
        return (int)(h ^ (h >> 32));
    }
}
#endif

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
            // string cannot use Get<string>(), which is null (pass-through) on modern .NET, and object keys must never
            // pass through, because the attacker picks the runtime type
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
            // a boxed enum (Typeless lets the payload pick one) hashes as its underlying integer would, keyed
            Enum v => v.GetTypeCode() == TypeCode.UInt64 ? HashVia(Convert.ToUInt64(v)) : HashVia(Convert.ToInt64(v)),
            decimal v => HashVia(v),
            TimeSpan v => HashVia(v),
            nint v => HashVia(v),
            nuint v => HashVia(v),
#if NET9_0_OR_GREATER
            Half v => HashVia(v),
            DateOnly v => HashVia(v),
            TimeOnly v => HashVia(v),
            Int128 v => HashVia(v),
            UInt128 v => HashVia(v),
#endif
            _ => value.GetHashCode(),
        };
    }

    static int HashVia<T>(T value) where T : notnull => HashFloodingResistantEqualityComparer.Get<T>()!.GetHashCode(value);
}

#endregion
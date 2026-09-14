namespace MessagePack.Formatters;

/// <summary>Marks the built-in integer-wire enum formatters, so the enum collection formatters can tell
/// "the underlying integer" apart from a resolver-supplied enum wire (enum-as-string, custom formatters).</summary>
internal interface IBuiltInEnumFormatter
{
}

/// <summary>Compile-time dispatch from an underlying integer type to its element codec (the typeof ladder folds per instantiation).</summary>
internal static class EnumElementCodec
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriteBuffer, TUnderlying>(ref TWriteBuffer buffer, ReadOnlySpan<TUnderlying> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TUnderlying : unmanaged
    {
        if (typeof(TUnderlying) == typeof(int)) default(Int32ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, int>(source));
        else if (typeof(TUnderlying) == typeof(byte)) default(ByteElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, byte>(source));
        else if (typeof(TUnderlying) == typeof(sbyte)) default(SByteElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, sbyte>(source));
        else if (typeof(TUnderlying) == typeof(short)) default(Int16ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, short>(source));
        else if (typeof(TUnderlying) == typeof(ushort)) default(UInt16ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, ushort>(source));
        else if (typeof(TUnderlying) == typeof(uint)) default(UInt32ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, uint>(source));
        else if (typeof(TUnderlying) == typeof(long)) default(Int64ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, long>(source));
        else if (typeof(TUnderlying) == typeof(ulong)) default(UInt64ElementCodec).WriteElements(ref buffer, MemoryMarshal.Cast<TUnderlying, ulong>(source));
        else ThrowUnsupportedUnderlyingType();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<TReadBuffer, TUnderlying>(ref TReadBuffer buffer, Span<TUnderlying> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TUnderlying : unmanaged
    {
        if (typeof(TUnderlying) == typeof(int)) default(Int32ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, int>(destination));
        else if (typeof(TUnderlying) == typeof(byte)) default(ByteElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, byte>(destination));
        else if (typeof(TUnderlying) == typeof(sbyte)) default(SByteElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, sbyte>(destination));
        else if (typeof(TUnderlying) == typeof(short)) default(Int16ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, short>(destination));
        else if (typeof(TUnderlying) == typeof(ushort)) default(UInt16ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, ushort>(destination));
        else if (typeof(TUnderlying) == typeof(uint)) default(UInt32ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, uint>(destination));
        else if (typeof(TUnderlying) == typeof(long)) default(Int64ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, long>(destination));
        else if (typeof(TUnderlying) == typeof(ulong)) default(UInt64ElementCodec).ReadElements(ref buffer, MemoryMarshal.Cast<TUnderlying, ulong>(destination));
        else ThrowUnsupportedUnderlyingType();
    }

    /// <summary>The construction-time check both collection formatters share: the pun is only sound at equal width.</summary>
    public static void ValidatePun<TEnum, TUnderlying>()
        where TEnum : struct, Enum
        where TUnderlying : unmanaged
    {
        if (Enum.GetUnderlyingType(typeof(TEnum)) != typeof(TUnderlying))
        {
            ThrowUnderlyingTypeMismatch(typeof(TEnum), typeof(TUnderlying));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowUnsupportedUnderlyingType()
    {
        throw new NotSupportedException("The enum collection formatters support only the eight integer underlying types.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowUnderlyingTypeMismatch(Type enumType, Type underlying)
    {
        throw new InvalidOperationException($"'{underlying}' is not the underlying type of '{enumType}' ({Enum.GetUnderlyingType(enumType)}); the enum collection formatters reinterpret the storage, so the widths must match.");
    }
}

/// <summary><c>TEnum[]</c> as the underlying integer array on the wire, over the primitive element codecs.
/// Same wire and Populate contract as <see cref="ArrayFormatter{TWriteBuffer, TReadBuffer, T}"/> over the built-in enum formatter.</summary>
public sealed class EnumArrayFormatter<TWriteBuffer, TReadBuffer, TEnum, TUnderlying> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TEnum[]?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TEnum : struct, Enum
    where TUnderlying : unmanaged
{
    // non-null only when the resolver serves TEnum with a wire other than the underlying
    // integer; the element formatter owns the wire then and the array goes element by element
    ArrayFormatter<TWriteBuffer, TReadBuffer, TEnum>? fallback;

    public EnumArrayFormatter()
    {
        EnumElementCodec.ValidatePun<TEnum, TUnderlying>();
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        if (resolver.GetFormatter<TWriteBuffer, TReadBuffer, TEnum>() is not IBuiltInEnumFormatter)
        {
            fallback = new ArrayFormatter<TWriteBuffer, TReadBuffer, TEnum>();
            fallback.Initialize(resolver);
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TEnum[]? value)
    {
        if (fallback != null)
        {
            fallback.Serialize(ref buffer, ref state, value);
            return;
        }
        SerializeElements(ref buffer, value);
    }

    // no state.Enter()/Exit(): the codec writes flat elements inline, the same as the
    // primitive array formatters (the depth budget is charged only on descent)
    static void SerializeElements(ref TWriteBuffer buffer, TEnum[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        EnumElementCodec.Write(ref buffer, MemoryMarshal.Cast<TEnum, TUnderlying>(value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TEnum[]? value)
    {
        if (fallback != null)
        {
            fallback.Deserialize(ref buffer, ref state, ref value);
            return;
        }
        DeserializeElements(ref buffer, ref value);
    }

    static void DeserializeElements(ref TReadBuffer buffer, ref TEnum[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        int count = buffer.ReadArrayHeader(); // count is bomb-guarded by ReadArrayHeader
        if (count == 0)
        {
            value = [];
            return;
        }

        // Populate contract: reuse the incoming array only on an exact length match;
        // a fresh array may be uninitialized because the codec writes every element
        var result = (value != null && value.Length == count) ? value : GC.AllocateUninitializedArray<TEnum>(count);
        EnumElementCodec.Read(ref buffer, MemoryMarshal.Cast<TEnum, TUnderlying>(result.AsSpan()));
        value = result;
    }
}

/// <summary>Creates <see cref="EnumArrayFormatter{TWriteBuffer, TReadBuffer, TEnum, TUnderlying}"/> for <c>T[]</c>, selecting the underlying integer type.</summary>
public sealed partial class EnumArrayFormatterFactory<T> : MessagePackFormatterFactory
    where T : struct, Enum
{
    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type != typeof(T[]))
        {
            return null;
        }

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Byte => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, byte>(),
            TypeCode.SByte => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, sbyte>(),
            TypeCode.Int16 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, short>(),
            TypeCode.UInt16 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, ushort>(),
            TypeCode.Int32 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, int>(),
            TypeCode.UInt32 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, uint>(),
            TypeCode.Int64 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, long>(),
            TypeCode.UInt64 => new EnumArrayFormatter<TWriteBuffer, TReadBuffer, T, ulong>(),
            _ => (object?)null,
        };
    }
}

#if NET9_0_OR_GREATER
// modern TFMs only, like PrimitiveListFormatter: the shape exists for CollectionsMarshal
// span access to the List's backing array. Downlevel routes List<TEnum> to ListFormatter.
/// <summary><c>List&lt;TEnum&gt;</c> as the underlying integer array on the wire, over the primitive element codecs.
/// Same wire and Populate contract as <see cref="ListFormatter{TWriteBuffer, TReadBuffer, T}"/> over the built-in enum formatter.</summary>
public sealed class EnumListFormatter<TWriteBuffer, TReadBuffer, TEnum, TUnderlying> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, List<TEnum>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TEnum : struct, Enum
    where TUnderlying : unmanaged
{
    ListFormatter<TWriteBuffer, TReadBuffer, TEnum>? fallback;

    public EnumListFormatter()
    {
        EnumElementCodec.ValidatePun<TEnum, TUnderlying>();
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        if (resolver.GetFormatter<TWriteBuffer, TReadBuffer, TEnum>() is not IBuiltInEnumFormatter)
        {
            fallback = new ListFormatter<TWriteBuffer, TReadBuffer, TEnum>();
            fallback.Initialize(resolver);
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, List<TEnum>? value)
    {
        if (fallback != null)
        {
            fallback.Serialize(ref buffer, ref state, value);
            return;
        }
        SerializeElements(ref buffer, value);
    }

    static void SerializeElements(ref TWriteBuffer buffer, List<TEnum>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        var span = CollectionsMarshal.AsSpan(value);
        buffer.WriteArrayHeader(span.Length);
        EnumElementCodec.Write(ref buffer, MemoryMarshal.Cast<TEnum, TUnderlying>(span));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref List<TEnum>? value)
    {
        if (fallback != null)
        {
            fallback.Deserialize(ref buffer, ref state, ref value);
            return;
        }
        DeserializeElements(ref buffer, ref value);
    }

    static void DeserializeElements(ref TReadBuffer buffer, ref List<TEnum>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // ReadArrayHeader validates the claimed count against BytesRemaining.
        var count = buffer.ReadArrayHeader();
        var result = value ?? new List<TEnum>(count);
        CollectionsMarshal.SetCount(result, count);
        EnumElementCodec.Read(ref buffer, MemoryMarshal.Cast<TEnum, TUnderlying>(CollectionsMarshal.AsSpan(result)));
        value = result;
    }
}

/// <summary>Creates <see cref="EnumListFormatter{TWriteBuffer, TReadBuffer, TEnum, TUnderlying}"/> for <c>List&lt;T&gt;</c>, selecting the underlying integer type.</summary>
public sealed partial class EnumListFormatterFactory<T> : MessagePackFormatterFactory
    where T : struct, Enum
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type != typeof(List<T>))
        {
            return null;
        }

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Byte => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, byte>(),
            TypeCode.SByte => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, sbyte>(),
            TypeCode.Int16 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, short>(),
            TypeCode.UInt16 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, ushort>(),
            TypeCode.Int32 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, int>(),
            TypeCode.UInt32 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, uint>(),
            TypeCode.Int64 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, long>(),
            TypeCode.UInt64 => new EnumListFormatter<TWriteBuffer, TReadBuffer, T, ulong>(),
            _ => (object?)null,
        };
    }
}
#endif

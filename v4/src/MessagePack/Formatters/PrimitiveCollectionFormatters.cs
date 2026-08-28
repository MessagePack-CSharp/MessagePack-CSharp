// primitive optimized collection formatters for array / List / Memory / ReadOnlyMemory / ArraySegment.
// TCodec is a struct(mimic of static abstract members for netstandard compatibility).

// T is sbyte/int/short/ushort/uint/long/ulong/float/double/bool (byte is bin-format, see ByteArrayFormatters.cs)

namespace MessagePack.Formatters;

/// <summary><c>T[]</c> over a <typeparamref name="TCodec"/> element core.
/// Same wire and Populate contract as <see cref="ArrayFormatter{TWriteBuffer, TReadBuffer, T}"/>.</summary>
internal sealed class PrimitiveArrayFormatter<TWriteBuffer, TReadBuffer, T, TCodec> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[]?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : unmanaged
    where TCodec : struct, IElementCodec<T>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter(); // flat elements cannot recurse, but the depth contract must not depend on the element type
        buffer.WriteArrayHeader(value.Length);
        default(TCodec).WriteElements(ref buffer, value);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        int count = buffer.ReadArrayHeader(); // count is bomb-guarded by ReadArrayHeader

        // empty fast path (the shared Array.Empty singleton, same as ArrayFormatter)
        if (count == 0)
        {
            value = [];
            return;
        }

        // Populate contract: reuse the incoming array only on an exact length match;
        // a fresh array may be uninitialized because the codec writes every element
        var result = (value != null && value.Length == count) ? value : GC.AllocateUninitializedArray<T>(count);
        state.Enter();
        default(TCodec).ReadElements(ref buffer, result);
        value = result;
        state.Exit();
    }
}

#if NET
// modern TFMs only: the whole point of this shape is CollectionsMarshal span access to
// the List's backing array. Downlevel TFMs route List<primitive> to the generic
// ListFormatter instead (see BuiltInFormatterFactory) — no perf chase there.
/// <summary><c>List&lt;T&gt;</c> over a <typeparamref name="TCodec"/> element core.
/// Same wire and Populate contract as <see cref="ListFormatter{TWriteBuffer, TReadBuffer, T}"/>.</summary>
internal sealed class PrimitiveListFormatter<TWriteBuffer, TReadBuffer, T, TCodec> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, List<T>?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : unmanaged
    where TCodec : struct, IElementCodec<T>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, List<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter(); // same depth contract as ListFormatter
        var span = CollectionsMarshal.AsSpan(value);
        buffer.WriteArrayHeader(span.Length);
        default(TCodec).WriteElements(ref buffer, span);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref List<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // ReadArrayHeader validates the claimed count against BytesRemaining.
        var count = buffer.ReadArrayHeader();

        state.Enter(); // same depth contract as ListFormatter
        var result = value ?? new List<T>(count);
        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        default(TCodec).ReadElements(ref buffer, span);
        value = result;
        state.Exit();
    }
}
#endif

/// <summary><c>Memory&lt;T&gt;</c> over a <typeparamref name="TCodec"/> element core.
/// Same semantics as <see cref="MemoryFormatter{TWriteBuffer, TReadBuffer, T}"/>
/// (default = empty array, never nil; exact-length populate writes through to the caller's backing store).</summary>
internal sealed class PrimitiveMemoryFormatter<TWriteBuffer, TReadBuffer, T, TCodec> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Memory<T>>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : unmanaged
    where TCodec : struct, IElementCodec<T>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Memory<T> value)
    {
        // Memory has no null: default serializes as an empty array, never nil
        state.Enter(); // same depth contract as MemoryFormatter
        var span = value.Span;
        buffer.WriteArrayHeader(span.Length);
        default(TCodec).WriteElements(ref buffer, span);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Memory<T> value)
    {
        if (buffer.TryReadNil()) // nil is accepted for symmetry with the nullable wrappers
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // exact-length reuse writes through to the caller's backing store (array or
        // MemoryManager) — the ArrayFormatter populate rule applied to a view
        var result = value.Length == count ? value : GC.AllocateUninitializedArray<T>(count);
        state.Enter();
        var span = result.Span;
        default(TCodec).ReadElements(ref buffer, span);
        value = result;
        state.Exit();
    }
}

/// <summary><c>ReadOnlyMemory&lt;T&gt;</c> over a <typeparamref name="TCodec"/> element core.
/// Same semantics as <see cref="ReadOnlyMemoryFormatter{TWriteBuffer, TReadBuffer, T}"/>
/// (read-only view: deserialize always builds a fresh backing array).</summary>
internal sealed class PrimitiveReadOnlyMemoryFormatter<TWriteBuffer, TReadBuffer, T, TCodec> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyMemory<T>>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : unmanaged
    where TCodec : struct, IElementCodec<T>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyMemory<T> value)
    {
        state.Enter(); // same depth contract as ReadOnlyMemoryFormatter
        var span = value.Span;
        buffer.WriteArrayHeader(span.Length);
        default(TCodec).WriteElements(ref buffer, span);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyMemory<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // read-only view: no write-through possible, always a fresh backing array
        var array = GC.AllocateUninitializedArray<T>(count); // the codec writes every element
        state.Enter();
        default(TCodec).ReadElements(ref buffer, array);
        value = array;
        state.Exit();
    }
}

/// <summary><c>ArraySegment&lt;T&gt;</c> over a <typeparamref name="TCodec"/> element core.
/// Same semantics as <see cref="ArraySegmentFormatter{TWriteBuffer, TReadBuffer, T}"/>
/// (default segment = nil; exact-length populate writes through at the segment's offset).
/// Value-typed elements have no array covariance, so spans are safe on both sides.</summary>
internal sealed class PrimitiveArraySegmentFormatter<TWriteBuffer, TReadBuffer, T, TCodec> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ArraySegment<T>>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : unmanaged
    where TCodec : struct, IElementCodec<T>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ArraySegment<T> value)
    {
        if (value.Array == null) // default segment = nil (v3 wire behavior)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter(); // same depth contract as ArraySegmentFormatter
        var span = value.AsSpan();
        buffer.WriteArrayHeader(span.Length);
        default(TCodec).WriteElements(ref buffer, span);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ArraySegment<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // Populate contract: overwrite the incoming view's backing store in place only on
        // an exact length match, otherwise a fresh zero-offset segment
        T[] array;
        int offset;
        if (value.Array != null && value.Count == count)
        {
            array = value.Array;
            offset = value.Offset;
        }
        else
        {
            array = GC.AllocateUninitializedArray<T>(count); // the codec writes every element
            offset = 0;
        }

        state.Enter();
        var span = array.AsSpan(offset, count);
        default(TCodec).ReadElements(ref buffer, span);
        value = new ArraySegment<T>(array, offset, count);
        state.Exit();
    }
}

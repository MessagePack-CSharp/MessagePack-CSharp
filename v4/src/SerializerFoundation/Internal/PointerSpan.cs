namespace SerializerFoundation;

// Span-like structure for a pointer and length.
// An unsafe twin of Span<byte>, mirroring its shape (readonly struct, Slice returns a new value).
// Exists because Span<byte> is a ref struct and cannot be a field of a class or plain struct.

internal readonly unsafe struct PointerSpan
{
    readonly byte* pointer;
    readonly int length;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointerSpan(byte* pointer, int length)
    {
        if (length < 0) Throws.ArgumentOutOfRange();
        this.pointer = pointer;
        this.length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference()
    {
        return ref Unsafe.AsRef<byte>(pointer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        return new Span<byte>(pointer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan(int start)
    {
        if ((uint)start > (uint)length) Throws.ArgumentOutOfRange();
        return new Span<byte>(pointer + start, length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan(int start, int count)
    {
        // same combined start+count validation shape as Span.Slice
        if ((ulong)(uint)start + (uint)count > (uint)length) Throws.ArgumentOutOfRange();
        return new Span<byte>(pointer + start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        return new ReadOnlySpan<byte>(pointer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<byte>(PointerSpan span)
    {
        return span.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(PointerSpan span)
    {
        return span.AsReadOnlySpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointerSpan Slice(int start)
    {
        if ((uint)start > (uint)length) Throws.ArgumentOutOfRange();

        return new PointerSpan(pointer + start, length - start);
    }
}

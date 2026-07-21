namespace SerializerFoundation;

// Span-like structure for a pointer and length — an unsafe twin of Span<byte>, mirroring
// its shape (readonly struct, Slice returns a new value). Exists because Span<byte> is a
// ref struct and cannot be a field of a class or plain struct (the Pointer* buffers and the
// async pipe buffers must hold their window in heap-able types). As a bonus, byte* is
// invisible to the GC (no byref reporting) and GetReference is a plain pointer, skipping
// Span construction entirely.
//
// CONTRACT: the memory behind `pointer` must be native or pinned (MemoryHandle.Pin) for
// this struct's entire lifetime — a GC move silently invalidates the pointer. That pinning
// contract is the ONLY unsafe part: Slice/AsSpan range-check and throw exactly like Span
// (a predicted throw-branch is free — DisasmProbe8 — and a bad slice over a raw pointer
// would corrupt memory, not just throw). GetReference is unchecked, mirroring
// MemoryMarshal.GetReference; on an empty/default instance it returns a null reference.
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

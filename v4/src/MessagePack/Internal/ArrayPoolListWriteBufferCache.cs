using System.Runtime.CompilerServices;
using SerializerFoundation;

namespace MessagePack;

/// <summary>
/// Thread-cached, boxed <see cref="CompatibleArrayPoolListWriteBuffer"/> for places that
/// need an IBufferWriter&lt;byte&gt;-shaped staging buffer (the byte[] entries, the
/// generic TryEncode bridge). Interface calls mutate the box in place, so the one heap
/// object IS the buffer; members not on the interface go through <see cref="AsBuffer"/>.
/// Internal on purpose: a global mutable slot behind Rent/Return is an aliasing footgun
/// as public API, so the pooling policy stays an implementation detail of the entries.
/// </summary>
internal static class ArrayPoolListWriteBufferCache
{
    [ThreadStatic]
    static IBufferWriter<byte>? cached;

    public static IBufferWriter<byte> Rent()
    {
        var writer = cached;
        if (writer == null)
        {
            // SF002 guards against a boxed copy escaping its owner; here the box IS the
            // single owner (freshly constructed, never copied out, mutated only through
            // interface calls and Unbox refs), which is the one sound way to box a buffer
#pragma warning disable SF002
            return new CompatibleArrayPoolListWriteBuffer();
#pragma warning restore SF002
        }
        cached = null; // reentrancy guard: a nested Rent gets a fresh instance
        return writer;
    }

    public static void Return(IBufferWriter<byte> writer)
    {
        AsBuffer(writer).Dispose(); // returns the pooled arrays, resets the box for reuse
        cached = writer;
    }

    /// <summary>A ref into the box, for members not on the interface (ToArray, GetWrittenSegments).</summary>
    public static ref CompatibleArrayPoolListWriteBuffer AsBuffer(IBufferWriter<byte> writer)
        => ref Unsafe.Unbox<CompatibleArrayPoolListWriteBuffer>(writer);
}

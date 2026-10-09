#if NETSTANDARD2_0
namespace MessagePack;

// in-box from netstandard2.1 on; the same shape as the core's Polyfill, which is internal to it
internal static class Polyfill
{
    extension<T>(ReadOnlySequence<T> sequence)
    {
        internal ReadOnlySpan<T> FirstSpan => sequence.First.Span;
    }
}
#endif

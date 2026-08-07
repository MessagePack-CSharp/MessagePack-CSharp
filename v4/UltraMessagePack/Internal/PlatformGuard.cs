namespace UltraMessagePack.Internal;

// The write/read primitives convert between host order and msgpack's big-endian wire
// with unconditional ReverseEndianness (dozens of asm-verified call sites): correct only
// on little-endian hosts. Real big-endian .NET targets are effectively linux-s390x, so
// rather than spread IsLittleEndian folds across every hot path (and re-verify their
// asm), the library declares the assumption ONCE at assembly load — a module
// initializer costs nothing per call, unlike a static ctor on the primitives class.
// If big-endian support is ever wanted: replace the unconditional reverses with the
// `IsLittleEndian ? ReverseEndianness(v) : v` pattern (JIT-identical on little-endian
// hosts); the wire itself is endian-defined, so no format change is involved.
internal static class PlatformGuard
{
    // CA2255 discourages library module initializers because they run eagerly and can
    // entangle initialization order; a branch-and-throw environment assertion has
    // neither problem, and the alternative (a static ctor near the hot primitives)
    // would cost every caller a cctor check
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "pure fail-fast environment assertion with no state, no ordering dependency, and no per-call cost; the intended use of load-time checks")]
    [ModuleInitializer]
    internal static void AssertLittleEndian()
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException(
                "UltraMessagePack requires a little-endian host: its encoding primitives assume little-endian byte order.");
        }
    }
}

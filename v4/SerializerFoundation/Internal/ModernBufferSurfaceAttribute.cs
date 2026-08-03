// Present only on builds whose buffer generics carry `allows ref struct` (net10.0).
// The analyzer detects the absence of this marker in the referenced core assembly.
// This signal is more reliable than checking whether corelib has ByRefLikeGenerics.

#if NET9_0_OR_GREATER

using SerializerFoundation;

[assembly: ModernBufferSurface]

namespace SerializerFoundation;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
internal sealed class ModernBufferSurfaceAttribute : Attribute
{
}

#endif

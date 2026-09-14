// C# union declarations lower against two marker types (IUnion, UnionAttribute) that ship in the BCL from net11.0;
// the core's internal polyfill (src/MessagePack/UnionPolyfill.cs) is visible here through InternalsVisibleTo, so this
// file only carries the marker the core does not need.
namespace System.Runtime.CompilerServices;

// the `closed` modifier lowers to this, enumerating the direct derived types
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
internal sealed class IsClosedTypeAttribute : Attribute
{
    public Type[]? DerivedTypes { get; set; }
}

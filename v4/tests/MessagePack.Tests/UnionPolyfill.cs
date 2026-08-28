// C# union declarations lower against these two marker types; they ship in the BCL from
// net11.0 and polyfill by name on earlier targets (the IsExternalInit pattern — verified
// on .NET 11 preview 7: no runtime API involvement beyond the markers).
namespace System.Runtime.CompilerServices;

internal interface IUnion
{
    object? Value { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class UnionAttribute : Attribute
{
}

// the `closed` modifier lowers to this, enumerating the direct derived types
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
internal sealed class IsClosedTypeAttribute : Attribute
{
    public Type[]? DerivedTypes { get; set; }
}

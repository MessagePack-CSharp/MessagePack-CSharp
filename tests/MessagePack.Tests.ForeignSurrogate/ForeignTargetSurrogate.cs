using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Tests.ForeignSurrogateTarget;

namespace MessagePack.Tests.ForeignSurrogate;

[MessagePackObject]
public readonly record struct ForeignTargetSurrogate([property: Key(0)] int X) : IMessagePackSurrogate<ForeignTarget, ForeignTargetSurrogate>
{
    public ForeignTargetSurrogate ToSurrogate(ForeignTarget value) => new(value.X);

    public ForeignTarget ToTarget() => new() { X = X };
}

// canary observable without touching this module (an AppDomain data slot read by string key): the test checks it is
// still unset right before the first serialization of the target, proving the registration it relies on came from
// the test assembly's own generated factory, not from this module's initializer
internal static class ForeignSurrogateCanary
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Set() => AppDomain.CurrentDomain.SetData("MessagePack.Tests.ForeignSurrogate.Initialized", true);
}

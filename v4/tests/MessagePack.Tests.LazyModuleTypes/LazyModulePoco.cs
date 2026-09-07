using System.Runtime.CompilerServices;
using MessagePack;

namespace MessagePack.Tests.LazyModuleTypes;

[MessagePackObject]
public class LazyModulePoco
{
    [Key(0)]
    public int Value { get; set; }
}

// canary observable WITHOUT touching this module (an AppDomain data slot the test reads by
// string key): proves the module initializer had not run before the deserialize call, and
// had run after it. It shares the module cctor with the generated registration, so "canary
// set" == "registration ran".
internal static class LazyModuleCanary
{
    // CA2255 exempts "advanced source generator scenarios"; this hand-written initializer
    // exists precisely to observe the generated one (they share the module cctor)
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Set() => AppDomain.CurrentDomain.SetData("MessagePack.Tests.LazyModuleTypes.Initialized", true);
}

using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The [ModuleInitializer]-registration blind spot, measured on .NET 10 CoreCLR:
//   - the TYPED path (Deserialize<T>) is safe by itself — a module cctor runs during the
//     JIT/prestub of any method referencing a type from that module, and the caller's
//     method references T as a generic argument;
//   - the TYPE-ONLY path is NOT — Type.GetType (typeless AQN, the non-generic entry,
//     config-driven types) loads the type WITHOUT running its module cctor (verified:
//     even GetProperties does not run it; only executing a method of the module does),
//     so the generated [ModuleInitializer] registration has not happened when the
//     registry is consulted.
// SourceGeneratedFormatterFactory closes the hole by running the requested type's module
// initializers on a miss and looking again. This test drives the type-only path against a
// dedicated fixture assembly nothing else in the suite references — and MUST NOT mention
// the fixture type in source anywhere: a typed mention would run the cctor at this test
// method's own JIT and mask the scenario (the first version of this test failed exactly
// that way).
public class LazyModuleInitializerTests
{
    // string literals on purpose: no compile-time reference to the fixture assembly
    const string CanaryKey = "MessagePack.Tests.LazyModuleTypes.Initialized";
    const string PocoQualifiedName = "MessagePack.Tests.LazyModuleTypes.LazyModulePoco, MessagePack.Tests.LazyModuleTypes";

    [Fact]
    public void TypeOnlyPath_RegistryMiss_RunsTheModuleInitializer()
    {
        var type = Type.GetType(PocoQualifiedName)!;
        Assert.Null(AppDomain.CurrentDomain.GetData(CanaryKey)); // the blind spot: loaded, cctor NOT run

        // no reflection tail in the chain: without the registry's RunModuleConstructor
        // mitigation this resolution would be formatter-not-found (and the JIT Default
        // chain would silently serve the reflection tier instead of the generated formatter)
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [SourceGeneratedFormatterFactory.Instance, BuiltInFormatterFactory.Instance]));

        byte[] payload = [0x91, 0x2A]; // fixarray(1) [42] — hand-built so nothing touches the module first
        var value = V4.Deserialize(type, payload, options)!;
        Assert.Equal(42, (int)type.GetProperty("Value")!.GetValue(value)!);

        // the registry ran the module cctor on its miss: canary (and registration) fired
        Assert.Equal(true, AppDomain.CurrentDomain.GetData(CanaryKey));
    }
}

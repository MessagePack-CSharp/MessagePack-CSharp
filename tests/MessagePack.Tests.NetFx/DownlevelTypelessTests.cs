#pragma warning disable CS0618 // typeless is [Obsolete]-gated by design; these tests are the consenting caller
using MessagePack;

namespace MessagePack.Tests.NetFx;

// Typeless payloads written on .NET name System.Private.CoreLib for the core types; .NET Framework has no assembly of
// that name, so without v3's spelling swap every corelib type (Guid, DateTime, decimal, object[], List`1...) failed with
// "Can't load type". The reverse direction worked all along through .NET's mscorlib facade.
public class DownlevelTypelessTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())));

    const string CoreLib = "System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";

    static byte[] BuildTypelessPayload(string typeName, byte[] inner)
    {
        var nameUtf8 = System.Text.Encoding.UTF8.GetBytes(typeName);
        var body = new List<byte> { 0xD9, (byte)nameUtf8.Length }; // str8
        body.AddRange(nameUtf8);
        body.AddRange(inner);
        var payload = new List<byte> { 0xC7, (byte)body.Count, 0x64 }; // ext8, len, code 100
        payload.AddRange(body);
        return payload.ToArray();
    }

    [Fact]
    public void CoreLibSpelledGuid_Reads()
    {
        var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
        var payload = BuildTypelessPayload("System.Guid, " + CoreLib, MessagePackSerializer.Serialize(guid));
        Assert.Equal(guid, Assert.IsType<Guid>(MessagePackSerializer.Deserialize<object>(payload, Options)));
    }

    [Fact]
    public void CoreLibSpelledNestedGeneric_Reads()
    {
        var name = $"System.Collections.Generic.List`1[[System.Int32, {CoreLib}]], {CoreLib}";
        var payload = BuildTypelessPayload(name, MessagePackSerializer.Serialize(new List<int> { 1, 2 }));
        Assert.Equal(new List<int> { 1, 2 }, Assert.IsType<List<int>>(MessagePackSerializer.Deserialize<object>(payload, Options)));
    }

    [Fact]
    public void CoreLibSpelledShortened_Reads()
    {
        // what an omitAssemblyVersion .NET writer emits
        var payload = BuildTypelessPayload("System.Guid, System.Private.CoreLib", MessagePackSerializer.Serialize(Guid.Empty));
        Assert.Equal(Guid.Empty, Assert.IsType<Guid>(MessagePackSerializer.Deserialize<object>(payload, Options)));
    }

    [Fact]
    public void LoadAnyType_SwapsTheCoreLibrarySpelling()
    {
        Assert.Same(typeof(Guid), TypelessTypeLoader.LoadAnyType().LoadType("System.Guid, " + CoreLib));
        Assert.Same(typeof(Guid), TypelessTypeLoader.LoadAnyType(allowAssemblyVersionMismatch: true).LoadType("System.Guid, " + CoreLib));
        // a disallowed type stays disallowed through the swap (the deny list runs on the result)
        var payload = BuildTypelessPayload("System.Diagnostics.Process, " + CoreLib, [0xC0]);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(payload, Options));
    }
}

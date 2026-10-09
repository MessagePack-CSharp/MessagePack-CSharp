using MessagePack;

// namespace MessagePack.Tests.BridgeOuter_0 next to type MessagePack.Tests.BridgeOuter (FactoryBridgeTest.cs): the
// generators' hint names must tell a containing type from a namespace of the same spelling, or the two Factory
// bridges collide (CS8785). Compiling is the assertion.
namespace MessagePack.Tests.BridgeOuter_0
{
    public sealed partial class Factory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => null;
    }
}

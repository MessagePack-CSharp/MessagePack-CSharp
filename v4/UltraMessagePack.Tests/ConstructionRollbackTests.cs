namespace UltraMessagePack.Tests;

public struct BrokenValue
{
}

// A factory that violates the contract: returns an object that is NOT an
// IMessagePackFormatter for the requested instantiation. The resolver's type check
// must throw — and, crucially, the throw happens at a NESTED node (inside the parent's
// Initialize), outside any per-node try/catch: cleanup relies solely on the root-level
// `catch when (isRoot)` rollback.
#pragma warning disable UMP102
sealed class WrongProductFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
        => valueType == typeof(BrokenValue) ? new object() : null;
}
#pragma warning restore UMP102

public class ConstructionRollbackTests
{
    [Fact]
    public void NestedConstructionFailureRollsBackAndResolverStaysUsable()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new WrongProductFactory(),
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
        ]));

        // the List formatter's Initialize resolves BrokenValue → the factory returns a
        // wrong-typed object → InvalidOperationException from a nested frame
        var ex = Assert.Throws<InvalidOperationException>(
            () => MessagePackSerializer.Serialize(new List<BrokenValue> { default }, options));
        Assert.Contains("not an IMessagePackFormatter", ex.Message);

        // the rollback must leave the construction state empty: if the half-built graph
        // (List formatter + the failed node) leaked, every later resolution would think
        // it is a nested call and never publish. A clean type must resolve and round-trip.
        var payload = MessagePackSerializer.Serialize(42, options);
        Assert.Equal(42, MessagePackSerializer.Deserialize<int>(payload, options));

        // retrying the broken type fails the same way (deterministic, not half-cached)
        Assert.Throws<InvalidOperationException>(
            () => MessagePackSerializer.Serialize(new List<BrokenValue> { default }, options));
    }
}

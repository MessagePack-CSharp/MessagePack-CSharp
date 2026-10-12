using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// Formatter and resolver behavior on failure, from the preview1 review's minor findings (F32, G214, F37).
public partial class FormatterAndResolverRobustnessTests
{
    // ---- F32: a ForceTypeless slot receiving a value of another type is a data error, not an InvalidCastException ----

    public interface IForcedShape
    {
    }

    public sealed class ForcedCircle : IForcedShape
    {
        public int R { get; set; }
    }

    // allowed by the loader, but not an IForcedShape
    public sealed class ForcedSquare
    {
        public int Side { get; set; }
    }

    public class ForcedHolder
    {
        public IForcedShape? Shape { get; set; } // an interface-typed slot: the Typeless tail's ForceTypelessFormatter
    }

    public class LooseHolder
    {
        public object? Shape { get; set; }
    }

    static readonly V4Options Typeless = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(ForcedCircle), typeof(ForcedSquare)))));

    [Fact]
    public void ForceTypelessSlot_RoundTripsAndKeepsNull()
    {
        var back = V4.Deserialize<ForcedHolder>(V4.Serialize(new ForcedHolder { Shape = new ForcedCircle { R = 3 } }, Typeless), Typeless)!;
        Assert.Equal(3, Assert.IsType<ForcedCircle>(back.Shape).R);
        Assert.Null(V4.Deserialize<ForcedHolder>(V4.Serialize(new ForcedHolder(), Typeless), Typeless)!.Shape);
    }

    [Fact]
    public void ForceTypelessSlot_UnassignableValue_IsADataError()
    {
        // a bare integer where an interface is declared: no ext envelope at all
        var primitive = V4.Serialize(new LooseHolder { Shape = 42 }, Typeless);
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ForcedHolder>(primitive, Typeless));
        Assert.Contains(nameof(IForcedShape), ex.Message, StringComparison.Ordinal);
        Assert.Contains("System.Int32", ex.Message, StringComparison.Ordinal);

        // an allowed type that does not implement the slot's interface
        var foreign = V4.Serialize(new LooseHolder { Shape = new ForcedSquare { Side = 2 } }, Typeless);
        ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<ForcedHolder>(foreign, Typeless));
        Assert.Contains(nameof(ForcedSquare), ex.Message, StringComparison.Ordinal);
    }

    // ---- G214: a formatter that cannot be built for a runtime type throws the construction exception, not TargetInvocationException ----

    // the formatter attached to Boom fails to construct, so building any graph that contains Boom throws
    public sealed partial class BoomFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Boom?>
    {
        public BoomFormatter() => throw new MessagePackSerializationException("Boom's formatter cannot be built.");

        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Boom? value) => throw new NotSupportedException();

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Boom? value) => throw new NotSupportedException();
    }

    [MessagePackFormatter(typeof(BoomFormatter<,>))]
    public sealed class Boom
    {
    }

    [Fact]
    public void TypedEntry_UnbuildableFormatter_ThrowsTheConstructionException()
    {
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new Boom()));
        Assert.Contains("cannot be built", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ObjectSlot_UnbuildableRuntimeType_ThrowsTheConstructionException()
    {
        // the object formatter closes a dispatcher over the runtime type through reflection: the same exception, not wrapped
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize<object>(new Boom()));
        Assert.Contains("cannot be built", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TypelessSlot_UnbuildableRuntimeType_ThrowsTheConstructionException()
    {
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize<object>(new Boom(), Typeless));
        Assert.Contains("cannot be built", ex.Message, StringComparison.Ordinal);
    }

    // ---- F37: CompatibilityFallback is raised outside the resolver's lock, so a handler may serialize on another thread ----

    [Fact]
    public void CompatibilityFallback_HandlerMaySerializeOnAnotherThread()
    {
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(
            new CompatiblePairOnlyFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance));
        var options = new V4Options(resolver);
        byte[]? fromHandler = null;
        resolver.CompatibilityFallback += type =>
        {
            // a logger that serializes its entry on a worker thread: with the gate still held, the worker waited for
            // this thread and this thread for the worker
            var worker = Task.Run(() => V4.Serialize(new RoutedValue { X = 1 }, options));
            Assert.True(worker.Wait(TimeSpan.FromSeconds(10)), "the handler's worker thread waited on the resolver's lock");
            fromHandler = worker.Result;
        };

        var payload = V4.Serialize(new RoutedValue { X = 7 }, options);

        Assert.NotNull(fromHandler);
        Assert.Equal(1, V4.Deserialize<RoutedValue>(fromHandler!, options).X);
        Assert.Equal(7, V4.Deserialize<RoutedValue>(payload, options).X);
    }
}

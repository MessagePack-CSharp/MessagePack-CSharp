using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePack.Formatters;

// v3's DynamicObjectTypeFallbackFormatter:
// serializes by the value's runtime type through the resolver, so a POCO sitting in an
// object-typed slot writes its ordinary map/array form, and a boxed collection or
// dictionary writes through its typed formatter.

/// <summary>
/// The non-AOT formatter for <see cref="object"/>.
/// </summary>
public sealed partial class ObjectFallbackFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?>
{
    readonly PrimitiveObjectFormatter<TWriteBuffer, TReadBuffer> primitive = new();
    readonly ConcurrentDictionary<Type, Dispatcher> dispatchers = new();
    MessagePackFormatterResolver resolver = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
        primitive.Initialize(resolver);
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }

        var type = value.GetType();

        // primitive runtime types keep the mini-protocol wire exactly; enums report their
        // underlying integer through GetTypeCode and ride along. Decimal has no arm in the
        // mini-protocol (v3 dispatched it too), so it falls through to the resolver.
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Object:
            case TypeCode.Decimal:
                break;
            default:
                primitive.Serialize(ref buffer, ref state, value);
                return;
        }

        if (type == typeof(byte[]))
        {
            primitive.Serialize(ref buffer, ref state, value); // bin form, not an integer array
            return;
        }

        if (type == typeof(object))
        {
            buffer.WriteMapHeader(0); // v3 rule: a bare object serializes as an empty map
            return;
        }

        GetDispatcher(type).Serialize(ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref object? value)
    {
        primitive.Deserialize(ref buffer, ref state, ref value);
    }

    Dispatcher GetDispatcher(Type type)
    {
        if (dispatchers.TryGetValue(type, out var dispatcher))
        {
            return dispatcher;
        }
        return CreateDispatcher(type);
    }

    static readonly MethodInfo createDispatcherMethod = typeof(ObjectFallbackFormatter<TWriteBuffer, TReadBuffer>).GetMethod(nameof(CreateDispatcherCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind the Default/DotNetOptimized chains' RequiresDynamicCode gate (the AOT chains keep PrimitiveObjectFormatter); the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "CreateDispatcherCore is a private member of this formatter, rooted by the typeof/nameof reference above; its type argument is the value's runtime type, which the caller roots")]
    Dispatcher CreateDispatcher(Type type)
    {
        var dispatcher = (Dispatcher)createDispatcherMethod.MakeGenericMethod(type).Invoke(this, null)!;
        return dispatchers.GetOrAdd(type, dispatcher);
    }

    Dispatcher CreateDispatcherCore<T>() => new Dispatcher<T>(resolver);

    abstract class Dispatcher
    {
        public abstract void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object value);
    }

    sealed class Dispatcher<T> : Dispatcher
    {
        readonly IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter;

        public Dispatcher(MessagePackFormatterResolver resolver)
        {
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
        }

        public override void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object value)
        {
            formatter.Serialize(ref buffer, ref state, (T)value);
        }
    }
}

public sealed partial class ObjectFallbackFormatterFactory : MessagePackFormatterFactory
{
    public static readonly ObjectFallbackFormatterFactory Instance = new();

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
        where TReadBuffer : struct, SerializerFoundation.IReadBuffer
#endif
    {
        return type == typeof(object) ? new ObjectFallbackFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

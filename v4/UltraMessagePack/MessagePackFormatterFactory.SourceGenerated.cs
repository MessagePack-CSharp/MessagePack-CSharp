using System.Collections.Concurrent;

namespace UltraMessagePack;

// TODO: this registry is temporary until the source generator can emit a static chain of factories.
// It is process-global and mutable, so it is not suitable for library code that wants to compose a private chain.

public sealed class SourceGeneratedFormatterFactory : MessagePackFormatterFactory
{
    public static readonly SourceGeneratedFormatterFactory Instance = new SourceGeneratedFormatterFactory();

    ConcurrentDictionary<Type, MessagePackFormatterFactory> factories = new ConcurrentDictionary<Type, MessagePackFormatterFactory>();

    public SourceGeneratedFormatterFactory()
    {
    }

    public void Register(Type type, MessagePackFormatterFactory factory)
    {
        factories[type] = factory;
    }

    public void RegisterFactory<T>(MessagePackFormatterFactory factory)
    {
        factories[typeof(T)] = factory;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return factories.TryGetValue(type, out var factory)
            ? factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type)
            : null;
    }
#endif

    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return factories.TryGetValue(valueType, out var factory)
            ? factory.CreateFormatter(writeBufferType, readBufferType, valueType)
            : null;
    }
}

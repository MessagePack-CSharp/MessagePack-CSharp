using MessagePack.Formatters;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePack;

public sealed partial class ObjectFallbackFormatterFactory : MessagePackFormatterFactory
{
    static ObjectFallbackFormatterFactory? instance;

    /// <summary>Shared instance. The formatter serializes an object slot by its runtime type, closing a formatter over it at run time.</summary>
    public static ObjectFallbackFormatterFactory Instance
    {
        [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
        get => instance ??= new ObjectFallbackFormatterFactory(); // benign race: stateless singleton
    }

    /// <summary>Creates a new instance, for use with <see cref="MessagePackFormatterAttribute"/>. Chains use <see cref="Instance"/>.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public ObjectFallbackFormatterFactory()
    {
    }

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

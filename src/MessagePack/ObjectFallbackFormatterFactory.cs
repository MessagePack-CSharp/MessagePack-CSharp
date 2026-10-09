using MessagePack.Formatters;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePack;

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

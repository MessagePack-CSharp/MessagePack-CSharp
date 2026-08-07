using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

/// <summary>
/// An exception thrown during serializing an object graph or deserializing a messagepack sequence.
/// </summary>
[Serializable]
public class MessagePackSerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
    /// </summary>
    public MessagePackSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public MessagePackSerializationException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The inner exception.</param>
    public MessagePackSerializationException(string? message, Exception? inner)
        : base(message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
    /// </summary>
    /// <param name="info">Serialization info.</param>
    /// <param name="context">Serialization context.</param>
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected MessagePackSerializationException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }

    [DoesNotReturn]
    internal static Exception ThrowUnexpectedNilWhileDeserializing<T>() => throw new MessagePackSerializationException("Unexpected nil encountered while deserializing " + typeof(T).FullName);

    [DoesNotReturn]
    internal static Exception ThrowInvalidEnumName<T>(string name) => throw new MessagePackSerializationException($"'{name}' is not a defined name of {typeof(T).FullName}");

    [DoesNotReturn]
    internal static Exception ThrowImplausibleCollectionHeader(string kind, int count, long bytesRemaining) => throw new MessagePackSerializationException($"The {kind} header claims {(uint)count} elements, which cannot fit in the {bytesRemaining} remaining payload bytes");

    [DoesNotReturn]
    internal static Exception ThrowImplausiblePayloadHeader(string kind, int byteCount, long bytesRemaining) => throw new MessagePackSerializationException($"The {kind} header claims a {(uint)byteCount} byte payload, which cannot fit in the {bytesRemaining} remaining bytes");

    [DoesNotReturn]
    internal static Exception ThrowSerializeDepthExceeded(int maxDepth) => throw new MessagePackSerializationException($"The object graph nests deeper than MaxDepth ({maxDepth}); a cyclic reference in the graph also produces this");

    [DoesNotReturn]
    internal static Exception ThrowDeserializeDepthExceeded(int maxDepth) => throw new MessagePackSerializationException($"The msgpack payload nests deeper than MaxDepth ({maxDepth})");
}
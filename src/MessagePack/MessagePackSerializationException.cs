using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

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
    [Obsolete("This constructor supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
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
    internal static Exception ThrowMalformedCircularReferenceWidth(int dataLength) => throw new MessagePackSerializationException($"A circular-reference back-reference carries a {dataLength}-byte payload; only 1, 2 or 4 byte ids are valid");

    [DoesNotReturn]
    internal static Exception ThrowUnknownCircularReferenceId(uint referenceId) => throw new MessagePackSerializationException($"A circular-reference back-reference points at id {referenceId}, which no definition in the payload has produced (a truncated or hand-reordered payload, or the definition sat inside data skipped by version-tolerant reading)");

    [DoesNotReturn]
    internal static Exception ThrowDuplicateCircularReferenceId(uint referenceId) => throw new MessagePackSerializationException($"The payload defines circular-reference id {referenceId} more than once");

    [DoesNotReturn]
    internal static Exception ThrowCircularReferenceTypeMismatch(uint referenceId, Type actual, Type expected) => throw new MessagePackSerializationException($"The circular-reference back-reference id {referenceId} resolves to an instance of '{actual.FullName}' where '{expected.FullName}' was expected");

    [DoesNotReturn]
    internal static Exception ThrowInvalidEnumName<T>(string name) => throw new MessagePackSerializationException($"'{name}' is not a defined name of {typeof(T).FullName}");

    [DoesNotReturn]
    internal static Exception ThrowImplausibleCollectionHeader(string kind, int count, long bytesRemaining) => throw new MessagePackSerializationException($"The {kind} header claims {(uint)count} elements, which cannot fit in the {bytesRemaining} remaining payload bytes");

    [DoesNotReturn]
    internal static Exception ThrowImplausiblePayloadHeader(string kind, int byteCount, long bytesRemaining) => throw new MessagePackSerializationException($"The {kind} header claims a {(uint)byteCount} byte payload, which cannot fit in the {bytesRemaining} remaining bytes");

    [DoesNotReturn]
    internal static Exception ThrowSerializeDepthExceeded() => throw new MessagePackSerializationException("The object graph nests deeper than MessagePackSerializerOptions.MaxDepth; a cyclic reference in the graph also produces this");

    [DoesNotReturn]
    internal static Exception ThrowDeserializeDepthExceeded() => throw new MessagePackSerializationException("The msgpack payload nests deeper than MessagePackSerializerOptions.MaxDepth");

    [DoesNotReturn]
    internal static Exception ThrowAsyncMessageMissing() => throw new MessagePackSerializationException("The PipeReader completed without a MessagePack value");

    [DoesNotReturn]
    internal static Exception ThrowAsyncMessageTruncated() => throw new MessagePackSerializationException("The PipeReader completed before the end of the MessagePack value");

    [DoesNotReturn]
    internal static Exception ThrowAsyncArrayTruncated(long claimedCount, long deliveredCount) => throw new MessagePackSerializationException($"The array header claims {claimedCount} elements but the PipeReader completed after {deliveredCount}");

    [DoesNotReturn]
    internal static Exception ThrowBufferedMessageSizeExceeded(long requiredBytes, long maxBufferedMessageSize) => throw new MessagePackSerializationException($"The MessagePack value needs at least {requiredBytes} buffered bytes, which exceeds MessagePackSerializerOptions.MaxBufferedMessageSize ({maxBufferedMessageSize})");

    [DoesNotReturn]
    internal static Exception ThrowDuplicateMapKey() => throw new MessagePackSerializationException("The map in the payload defines the same key more than once");

    [DoesNotReturn]
    internal static Exception ThrowNullMapKey() => throw new MessagePackSerializationException("The map in the payload contains a nil key, which the target dictionary cannot store");

    // Guards a dictionary key read from the payload before it reaches the collection.
    // For value-type TKey the null test folds to a constant false and the whole call is
    // elided, so this is free on the common numeric-key path; only reference-type keys
    // (string, records) pay a predicted null-check per entry. A nil key would otherwise
    // surface as a raw ArgumentNullException from the dictionary, breaking the contract
    // that every malformed payload throws MessagePackSerializationException.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNullMapKey<TKey>(TKey key)
    {
        if (key is null)
        {
            ThrowNullMapKey();
        }
    }

    [DoesNotReturn]
    internal static Exception ThrowMissingRequiredMember(string? typeName, string memberName) => throw new MessagePackSerializationException($"Required member '{memberName}' of '{typeName}' is missing from the payload (required member validation, MessagePackFormatterResolver validateRequiredMembers, on by default)");

    [DoesNotReturn]
    internal static Exception ThrowNullValueForNonNullableMember(string? typeName, string memberName) => throw new MessagePackSerializationException($"Member '{memberName}' of '{typeName}' is declared non-nullable but the payload contains nil (nullable annotation validation, MessagePackFormatterResolver validateNullableAnnotations)");

    [DoesNotReturn]
    internal static Exception ThrowAsyncElementsMissing(long declaredCount, long producedCount) => throw new MessagePackSerializationException($"SerializeElementsAsync declared {declaredCount} elements but the source completed after {producedCount}");

    [DoesNotReturn]
    internal static Exception ThrowAsyncElementsExceeded(long declaredCount) => throw new MessagePackSerializationException($"SerializeElementsAsync declared {declaredCount} elements but the source yielded more");
}
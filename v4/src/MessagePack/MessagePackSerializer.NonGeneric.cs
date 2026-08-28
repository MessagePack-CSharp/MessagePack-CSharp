using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace MessagePack;

// This library is generic-first, so the non-generic API is implemented as routing to the generic one.
// Parameter order is Type first.
// typeof(T) stands where <T> stands,
// so the non-generic surface reads as the generic one with the type argument moved into the argument list.

public static partial class MessagePackSerializer
{
    internal const string NonGenericRequiresDynamicCodeMessage =
        "The non-generic entry points close the generic serializer over the runtime Type via MakeGenericType, " +
        "which can require runtime code generation for value types. For Native AOT, use the generic APIs.";

    static readonly ConcurrentDictionary<Type, NonGenericEntry> nonGenericEntries = new();

    /// <summary>
    /// Serializes a value as the given runtime type; the value must be an instance of
    /// that type (the non-generic counterpart of <see cref="Serialize{T}(T)"/>).
    /// </summary>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static byte[] Serialize(Type type, object? value)
    {
        return Serialize(type, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.Serialize(value, options);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize(Type type, IBufferWriter<byte> output, object? value)
    {
        Serialize(type, output, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static void Serialize(Type type, IBufferWriter<byte> output, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        entry.Serialize(output, value, options);
    }

    /// <summary>
    /// Deserializes one value of the given runtime type, returned boxed (the non-generic
    /// counterpart of <see cref="Deserialize{T}(ReadOnlySpan{byte})"/>).
    /// </summary>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, ReadOnlySpan<byte> source)
    {
        return Deserialize(type, source, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static object? Deserialize(Type type, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(source, options);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, in ReadOnlySequence<byte> source)
    {
        return Deserialize(type, in source, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static object? Deserialize(Type type, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(in source, options);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize(Type type, Stream stream, object? value)
    {
        Serialize(type, stream, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static void Serialize(Type type, Stream stream, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        entry.Serialize(stream, value, options);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, Stream stream)
    {
        return Deserialize(type, stream, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static object? Deserialize(Type type, Stream stream, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(stream, options);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync(Type type, Stream stream, object? value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(type, stream, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static Task SerializeAsync(Type type, Stream stream, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.SerializeAsync(stream, value, options, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync(type, stream, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        return GetNonGenericEntry(type).DeserializeAsync(stream, options, cancellationToken);
    }

    /// <summary>
    /// Serializes a value as the given runtime type into the writer and flushes; the
    /// value must be an instance of that type (the non-generic counterpart of
    /// <see cref="SerializeAsync{T}(PipeWriter, T, CancellationToken)"/>).
    /// </summary>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync(Type type, PipeWriter pipeWriter, object? value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(type, pipeWriter, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static Task SerializeAsync(Type type, PipeWriter pipeWriter, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.SerializeAsync(pipeWriter, value, options, cancellationToken);
    }

    /// <summary>
    /// Deserializes one value of the given runtime type from the reader, returned boxed
    /// (the non-generic counterpart of
    /// <see cref="DeserializeAsync{T}(PipeReader, CancellationToken)"/>).
    /// </summary>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, PipeReader pipeReader, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync(type, pipeReader, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, PipeReader pipeReader, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        return GetNonGenericEntry(type).DeserializeAsync(pipeReader, options, cancellationToken);
    }

    // API misuse (a value that is not an instance of the declared type) throws Argument
    // exceptions here, before any output is written; malformed DATA keeps throwing
    // MessagePackSerializationException from the entries, per the exception policy.
    static void ValidateValue(Type type, object? value)
    {
        if (value is null)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            {
                throw new ArgumentException($"null is not a valid value for the non-nullable value type '{type}'.", nameof(value));
            }
        }
        else if (!type.IsInstanceOfType(value))
        {
            throw new ArgumentException($"The value of type '{value.GetType()}' is not an instance of the serialized type '{type}'.", nameof(value));
        }
    }

    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    static NonGenericEntry GetNonGenericEntry(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (nonGenericEntries.TryGetValue(type, out var entry))
        {
            return entry;
        }
        return CreateNonGenericEntry(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [RequiresDynamicCode(NonGenericRequiresDynamicCodeMessage)]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the open bridge definition is rooted by the typeof reference below and its type parameter is unannotated and unconstrained; the argument is the caller-provided serialized type, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "every NonGenericEntry<T> instantiation has the public parameterless constructor of the rooted open definition")]
    static NonGenericEntry CreateNonGenericEntry(Type type)
    {
        if (type.ContainsGenericParameters)
        {
            throw new ArgumentException($"The open generic type '{type}' cannot be serialized; close it over concrete type arguments first.", nameof(type));
        }
        var entry = (NonGenericEntry)Activator.CreateInstance(typeof(NonGenericEntry<>).MakeGenericType(type))!;
        return nonGenericEntries.GetOrAdd(type, entry);
    }

    abstract class NonGenericEntry
    {
        public abstract byte[] Serialize(object? value, MessagePackSerializerOptions options);
        public abstract void Serialize(IBufferWriter<byte> output, object? value, MessagePackSerializerOptions options);
        public abstract void Serialize(Stream stream, object? value, MessagePackSerializerOptions options);
        public abstract object? Deserialize(ReadOnlySpan<byte> source, MessagePackSerializerOptions options);
        public abstract object? Deserialize(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options);
        public abstract object? Deserialize(Stream stream, MessagePackSerializerOptions options);
        public abstract Task SerializeAsync(PipeWriter pipeWriter, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken);
        public abstract Task SerializeAsync(Stream stream, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken);
        public abstract ValueTask<object?> DeserializeAsync(PipeReader pipeReader, MessagePackSerializerOptions options, CancellationToken cancellationToken);
        public abstract ValueTask<object?> DeserializeAsync(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken);
    }

    sealed class NonGenericEntry<T> : NonGenericEntry
    {
        public override byte[] Serialize(object? value, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Serialize<T>((T)value!, options);
        }

        public override void Serialize(IBufferWriter<byte> output, object? value, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize<T>(output, (T)value!, options);
        }

        public override void Serialize(Stream stream, object? value, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize<T>(stream, (T)value!, options);
        }

        public override object? Deserialize(Stream stream, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(stream, options);
        }

        public override Task SerializeAsync(Stream stream, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken)
        {
            return MessagePackSerializer.SerializeAsync<T>(stream, (T)value!, options, cancellationToken);
        }

        public override async ValueTask<object?> DeserializeAsync(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken)
        {
            return await MessagePackSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
        }

        public override object? Deserialize(ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(source, options);
        }

        public override object? Deserialize(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(in source, options);
        }

        public override Task SerializeAsync(PipeWriter pipeWriter, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken)
        {
            return MessagePackSerializer.SerializeAsync<T>(pipeWriter, (T)value!, options, cancellationToken);
        }

        public override async ValueTask<object?> DeserializeAsync(PipeReader pipeReader, MessagePackSerializerOptions options, CancellationToken cancellationToken)
        {
            return await MessagePackSerializer.DeserializeAsync<T>(pipeReader, options, cancellationToken).ConfigureAwait(false);
        }
    }
}

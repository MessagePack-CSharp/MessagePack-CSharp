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
    // Type-based entries close NonGenericEntry<T> over the runtime type. Three sources, in order: the entries generated
    // module initializers register alongside their factories (every generated type, harvested closure and
    // [MessagePackSerializable] root), the built-in scalar set in CreateBuiltInEntry, and MakeGenericType for anything
    // else. Only the last needs runtime code generation, so Native AOT stops there with an actionable exception instead
    // of a missing-artifact crash, and the entries carry no RequiresDynamicCode of their own.
    // RuntimeTypeHandle key: a value-type key takes the containers' comparer-null fast path.
    static readonly TypeKeyHashTable<NonGenericEntry> nonGenericEntries = new();

    // reached through SourceGeneratedFormatterFactory's generic Register/RegisterHarvested, which generated module
    // initializers call for every closed type they serve
    internal static void RegisterNonGeneric<T>()
    {
        nonGenericEntries.TryAdd(typeof(T), new NonGenericEntry<T>());
    }

    /// <summary>
    /// Serializes <paramref name="value"/> as <paramref name="type"/>, the non-generic form of <see cref="Serialize{T}(T)"/>.
    /// The value must be an instance of that type.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static byte[] Serialize(Type type, object? value)
    {
        return Serialize(type, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.Serialize(value, options);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize(Type type, IBufferWriter<byte> output, object? value)
    {
        Serialize(type, output, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    public static void Serialize(Type type, IBufferWriter<byte> output, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        entry.Serialize(output, value, options);
    }

    /// <summary>Deserializes one value of <paramref name="type"/> and returns it boxed, the non-generic form of <see cref="Deserialize{T}(ReadOnlySpan{byte})"/>.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, ReadOnlySpan<byte> source)
    {
        return Deserialize(type, source, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    public static object? Deserialize(Type type, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(source, options);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, in ReadOnlySequence<byte> source)
    {
        return Deserialize(type, in source, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    public static object? Deserialize(Type type, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(in source, options);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize(Type type, Stream stream, object? value)
    {
        Serialize(type, stream, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    public static void Serialize(Type type, Stream stream, object? value, MessagePackSerializerOptions options)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        entry.Serialize(stream, value, options);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize(Type type, Stream stream)
    {
        return Deserialize(type, stream, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type, ReadOnlySpan{byte})"/>
    public static object? Deserialize(Type type, Stream stream, MessagePackSerializerOptions options)
    {
        return GetNonGenericEntry(type).Deserialize(stream, options);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync(Type type, Stream stream, object? value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(type, stream, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    public static Task SerializeAsync(Type type, Stream stream, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.SerializeAsync(stream, value, options, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync(type, stream, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    public static ValueTask<object?> DeserializeAsync(Type type, Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        return GetNonGenericEntry(type).DeserializeAsync(stream, options, cancellationToken);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> as <paramref name="type"/> into the writer and flushes, the non-generic form of <see cref="SerializeAsync{T}(PipeWriter, T, CancellationToken)"/>.
    /// The value must be an instance of that type.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync(Type type, PipeWriter pipeWriter, object? value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(type, pipeWriter, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync(Type, PipeWriter, object?, CancellationToken)"/>
    public static Task SerializeAsync(Type type, PipeWriter pipeWriter, object? value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        return entry.SerializeAsync(pipeWriter, value, options, cancellationToken);
    }

    /// <summary>Deserializes one value of <paramref name="type"/> from the reader and returns it boxed, the non-generic form of <see cref="DeserializeAsync{T}(PipeReader, CancellationToken)"/>.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<object?> DeserializeAsync(Type type, PipeReader pipeReader, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync(type, pipeReader, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync(Type, PipeReader, CancellationToken)"/>
    public static ValueTask<object?> DeserializeAsync(Type type, PipeReader pipeReader, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        return GetNonGenericEntry(type).DeserializeAsync(pipeReader, options, cancellationToken);
    }

#if NET9_0_OR_GREATER

    /// <summary>
    /// Serializes <paramref name="value"/> as <paramref name="type"/> straight into a write buffer without flushing, the non-generic form of
    /// <see cref="Serialize{TWriteBuffer, T}(ref TWriteBuffer, T, MessagePackSerializerOptions)"/> for embedding MessagePack inside another protocol.
    /// Options with a MessageProcessor are rejected, since the processor needs the complete message and this entry never sees it.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize<TWriteBuffer>(Type type, ref TWriteBuffer buffer, object? value)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        Serialize(type, ref buffer, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{TWriteBuffer}(Type, ref TWriteBuffer, object?)"/>
    public static void Serialize<TWriteBuffer>(Type type, ref TWriteBuffer buffer, object? value, MessagePackSerializerOptions options)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        var entry = GetNonGenericEntry(type);
        ValidateValue(type, value);
        entry.Serialize(ref buffer, value, options);
    }

    /// <summary>
    /// Deserializes one value of <paramref name="type"/> straight from a read buffer and leaves the following bytes unconsumed, the non-generic form of
    /// <see cref="Deserialize{TReadBuffer, T}(ref TReadBuffer, MessagePackSerializerOptions)"/> for reading MessagePack embedded inside another protocol.
    /// Options with a MessageProcessor are rejected, since the processor needs the complete message and this entry never sees it.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static object? Deserialize<TReadBuffer>(Type type, ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return Deserialize(type, ref buffer, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize{TReadBuffer}(Type, ref TReadBuffer)"/>
    public static object? Deserialize<TReadBuffer>(Type type, ref TReadBuffer buffer, MessagePackSerializerOptions options)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return GetNonGenericEntry(type).Deserialize(ref buffer, options);
    }

#endif

    // API misuse (a value that is not an instance of the declared type) throws ArgumentException here, before any
    // output is written. Malformed data keeps throwing MessagePackSerializationException from the entries, per the
    // exception policy.
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

    static NonGenericEntry GetNonGenericEntry(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (nonGenericEntries.TryGetValue(type, out var entry))
        {
            return entry;
        }
        return GetNonGenericEntrySlow(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static NonGenericEntry GetNonGenericEntrySlow(Type type)
    {
        // the registry's module-initializer blind spot applies here too: a Type-only first contact with an assembly
        // has not run its registrations yet
        SourceGeneratedFormatterFactory.RunModuleInitializers(type);
        if (nonGenericEntries.TryGetValue(type, out var entry))
        {
            return entry;
        }
        if (type.ContainsGenericParameters)
        {
            throw new ArgumentException($"The open generic type '{type}' cannot be serialized; close it over concrete type arguments first.", nameof(type));
        }
        entry = CreateBuiltInEntry(type) ?? CreateNonGenericEntryDynamically(type);
        return nonGenericEntries.GetOrAdd(type, entry);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "the open bridge definition is rooted by the typeof reference below and its type parameter is unannotated and unconstrained; the argument is the caller-provided serialized type, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "every NonGenericEntry<T> instantiation has the public parameterless constructor of the rooted open definition")]
    static NonGenericEntry CreateNonGenericEntryDynamically(Type type)
    {
#if NET9_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new NotSupportedException(
                $"No Type-based entry exists for '{type}' and this runtime cannot generate one: the type is not a generated type, a harvested closure, a [MessagePackSerializable] root or a built-in scalar. " +
                "Use the generic entries, or declare the type as a [MessagePackSerializable<T>] root on a partial factory so the source generator registers it.");
        }
#endif
        return (NonGenericEntry)Activator.CreateInstance(typeof(NonGenericEntry<>).MakeGenericType(type))!;
    }

    // the built-in scalar set (MessagePackFormatterFactory.BuiltIn) and the Nullable twins of its value types, closed
    // statically so Type-first callers such as hub protocols and MVC formatters reach them on Native AOT without a
    // registration. Only reachable from the Type-based entries, so applications on the generic entries carry none of it.
    static NonGenericEntry? CreateBuiltInEntry(Type type)
    {
        if (type == typeof(int)) return new NonGenericEntry<int>();
        if (type == typeof(long)) return new NonGenericEntry<long>();
        if (type == typeof(short)) return new NonGenericEntry<short>();
        if (type == typeof(byte)) return new NonGenericEntry<byte>();
        if (type == typeof(sbyte)) return new NonGenericEntry<sbyte>();
        if (type == typeof(uint)) return new NonGenericEntry<uint>();
        if (type == typeof(ulong)) return new NonGenericEntry<ulong>();
        if (type == typeof(ushort)) return new NonGenericEntry<ushort>();
        if (type == typeof(char)) return new NonGenericEntry<char>();
        if (type == typeof(bool)) return new NonGenericEntry<bool>();
        if (type == typeof(float)) return new NonGenericEntry<float>();
        if (type == typeof(double)) return new NonGenericEntry<double>();
        if (type == typeof(decimal)) return new NonGenericEntry<decimal>();
        if (type == typeof(string)) return new NonGenericEntry<string>();
        if (type == typeof(DateTime)) return new NonGenericEntry<DateTime>();
        if (type == typeof(DateTimeOffset)) return new NonGenericEntry<DateTimeOffset>();
        if (type == typeof(TimeSpan)) return new NonGenericEntry<TimeSpan>();
        if (type == typeof(Guid)) return new NonGenericEntry<Guid>();
        if (type == typeof(Nil)) return new NonGenericEntry<Nil>();
        if (type == typeof(Uri)) return new NonGenericEntry<Uri>();
        if (type == typeof(Version)) return new NonGenericEntry<Version>();
        if (type == typeof(System.Text.StringBuilder)) return new NonGenericEntry<System.Text.StringBuilder>();
        if (type == typeof(System.Collections.BitArray)) return new NonGenericEntry<System.Collections.BitArray>();
        if (type == typeof(System.Globalization.CultureInfo)) return new NonGenericEntry<System.Globalization.CultureInfo>();
        if (type == typeof(TimeZoneInfo)) return new NonGenericEntry<TimeZoneInfo>();
        if (type == typeof(System.Numerics.BigInteger)) return new NonGenericEntry<System.Numerics.BigInteger>();
        if (type == typeof(System.Numerics.Complex)) return new NonGenericEntry<System.Numerics.Complex>();
        if (type == typeof(System.Numerics.Vector2)) return new NonGenericEntry<System.Numerics.Vector2>();
        if (type == typeof(System.Numerics.Vector3)) return new NonGenericEntry<System.Numerics.Vector3>();
        if (type == typeof(System.Numerics.Vector4)) return new NonGenericEntry<System.Numerics.Vector4>();
        if (type == typeof(System.Numerics.Quaternion)) return new NonGenericEntry<System.Numerics.Quaternion>();
        if (type == typeof(System.Numerics.Matrix3x2)) return new NonGenericEntry<System.Numerics.Matrix3x2>();
        if (type == typeof(System.Numerics.Matrix4x4)) return new NonGenericEntry<System.Numerics.Matrix4x4>();
        if (type == typeof(System.Numerics.Plane)) return new NonGenericEntry<System.Numerics.Plane>();
        if (type == typeof(int?)) return new NonGenericEntry<int?>();
        if (type == typeof(long?)) return new NonGenericEntry<long?>();
        if (type == typeof(short?)) return new NonGenericEntry<short?>();
        if (type == typeof(byte?)) return new NonGenericEntry<byte?>();
        if (type == typeof(sbyte?)) return new NonGenericEntry<sbyte?>();
        if (type == typeof(uint?)) return new NonGenericEntry<uint?>();
        if (type == typeof(ulong?)) return new NonGenericEntry<ulong?>();
        if (type == typeof(ushort?)) return new NonGenericEntry<ushort?>();
        if (type == typeof(char?)) return new NonGenericEntry<char?>();
        if (type == typeof(bool?)) return new NonGenericEntry<bool?>();
        if (type == typeof(float?)) return new NonGenericEntry<float?>();
        if (type == typeof(double?)) return new NonGenericEntry<double?>();
        if (type == typeof(decimal?)) return new NonGenericEntry<decimal?>();
        if (type == typeof(DateTime?)) return new NonGenericEntry<DateTime?>();
        if (type == typeof(DateTimeOffset?)) return new NonGenericEntry<DateTimeOffset?>();
        if (type == typeof(TimeSpan?)) return new NonGenericEntry<TimeSpan?>();
        if (type == typeof(Guid?)) return new NonGenericEntry<Guid?>();
        if (type == typeof(Nil?)) return new NonGenericEntry<Nil?>();
        return null;
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
#if NET9_0_OR_GREATER
        public abstract void Serialize<TWriteBuffer>(ref TWriteBuffer buffer, object? value, MessagePackSerializerOptions options)
            where TWriteBuffer : struct, IWriteBuffer, allows ref struct;
        public abstract object? Deserialize<TReadBuffer>(ref TReadBuffer buffer, MessagePackSerializerOptions options)
            where TReadBuffer : struct, IReadBuffer, allows ref struct;
#endif
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

#if NET9_0_OR_GREATER
        public override void Serialize<TWriteBuffer>(ref TWriteBuffer buffer, object? value, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize<TWriteBuffer, T>(ref buffer, (T)value!, options);
        }

        public override object? Deserialize<TReadBuffer>(ref TReadBuffer buffer, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<TReadBuffer, T>(ref buffer, options);
        }
#endif
    }
}

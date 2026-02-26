// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using MessagePack.Formatters;

namespace MessagePack.Internal;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
internal static class FormatterDispatchByValue<T>
{
    internal static void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        formatter.Serialize(ref writer, value, options);
    }

    internal static T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        return formatter.Deserialize(ref reader, options);
    }
}

internal static class FormatterDispatch<T>
    where T : struct
{
    private delegate void SerializeDelegate(IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options);

    private delegate void DeserializeIntoDelegate(IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);

    private static readonly SerializeDelegate SerializeByValue = static (IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options) => formatter.Serialize(ref writer, value, options);
    private static readonly SerializeDelegate SerializeByRef = static (IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options) => ((IMessagePackFormatterSerializeIn<T>)formatter).Serialize(ref writer, in value, options);
    private static readonly DeserializeIntoDelegate DeserializeByValue = static (IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options) => value = formatter.Deserialize(ref reader, options);
    private static readonly DeserializeIntoDelegate DeserializeByRef = static (IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options) => ((IMessagePackFormatterDeserializeRef<T>)formatter).Deserialize(ref reader, ref value, options);

    private static SerializeCache? serializeCache;
    private static DeserializeCache? deserializeCache;

    private sealed class SerializeCache
    {
        internal SerializeCache(Type formatterType, SerializeDelegate dispatch)
        {
            this.FormatterType = formatterType;
            this.Dispatch = dispatch;
        }

        internal Type FormatterType { get; }

        internal SerializeDelegate Dispatch { get; }
    }

    private sealed class DeserializeCache
    {
        internal DeserializeCache(Type formatterType, DeserializeIntoDelegate dispatch)
        {
            this.FormatterType = formatterType;
            this.Dispatch = dispatch;
        }

        internal Type FormatterType { get; }

        internal DeserializeIntoDelegate Dispatch { get; }
    }

    internal static void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        Type formatterType = formatter.GetType();
        SerializeCache? cache = Volatile.Read(ref serializeCache);
        if (cache is null || !ReferenceEquals(cache.FormatterType, formatterType))
        {
            SerializeDelegate dispatch = formatter is IMessagePackFormatterSerializeIn<T> ? SerializeByRef : SerializeByValue;
            cache = new SerializeCache(formatterType, dispatch);
            Volatile.Write(ref serializeCache, cache);
        }

        cache.Dispatch(formatter, ref writer, in value, options);
    }

    internal static T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        T value = default;
        Deserialize(ref reader, ref value, options);
        return value;
    }

    internal static void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        Type formatterType = formatter.GetType();
        DeserializeCache? cache = Volatile.Read(ref deserializeCache);
        if (cache is null || !ReferenceEquals(cache.FormatterType, formatterType))
        {
            DeserializeIntoDelegate dispatch = formatter is IMessagePackFormatterDeserializeRef<T> ? DeserializeByRef : DeserializeByValue;
            cache = new DeserializeCache(formatterType, dispatch);
            Volatile.Write(ref deserializeCache, cache);
        }

        cache.Dispatch(formatter, ref reader, ref value, options);
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type

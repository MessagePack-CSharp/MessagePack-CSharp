// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
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

internal static class FormatterDispatchInto<T>
    where T : class
{
    private delegate T? DeserializeDelegate(IMessagePackFormatter<T?> formatter, ref MessagePackReader reader, T? value, MessagePackSerializerOptions options);

    private static readonly DeserializeDelegate DeserializeByValue = static (IMessagePackFormatter<T?> formatter, ref MessagePackReader reader, T? value, MessagePackSerializerOptions options) => formatter.Deserialize(ref reader, options);
    private static readonly DeserializeDelegate DeserializeInto = static (IMessagePackFormatter<T?> formatter, ref MessagePackReader reader, T? value, MessagePackSerializerOptions options) =>
    {
        if (value is null)
        {
            return formatter.Deserialize(ref reader, options);
        }

        if (reader.TryReadNil())
        {
            return null;
        }

        ((IMessagePackFormatterDeserializeInto<T>)formatter).Deserialize(ref reader, value, options);
        return value;
    };

    private static DeserializeCache? deserializeCache;

    private sealed class DeserializeCache
    {
        internal DeserializeCache(Type formatterType, DeserializeDelegate dispatch)
        {
            this.FormatterType = formatterType;
            this.Dispatch = dispatch;
        }

        internal Type FormatterType { get; }

        internal DeserializeDelegate Dispatch { get; }
    }

    internal static T? Deserialize(ref MessagePackReader reader, T? value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T?> formatter = options.Resolver.GetFormatterWithVerify<T?>();
        return Deserialize(formatter, ref reader, value, options);
    }

    internal static T? Deserialize(IMessagePackFormatter<T?> formatter, ref MessagePackReader reader, T? value, MessagePackSerializerOptions options)
    {
        Type formatterType = formatter.GetType();
        DeserializeCache? cache = Volatile.Read(ref deserializeCache);
        if (cache is null || !ReferenceEquals(cache.FormatterType, formatterType))
        {
            DeserializeDelegate dispatch = formatter is IMessagePackFormatterDeserializeInto<T> ? DeserializeInto : DeserializeByValue;
            cache = new DeserializeCache(formatterType, dispatch);
            Volatile.Write(ref deserializeCache, cache);
        }

        return cache.Dispatch(formatter, ref reader, value, options);
    }
}

internal static class FormatterDispatchReuse<T>
{
    private delegate T DeserializeDelegate(ref MessagePackReader reader, T value, MessagePackSerializerOptions options);

    private static readonly DeserializeDelegate DeserializeCore = CreateDeserialize();

    internal static T Deserialize(ref MessagePackReader reader, T value, MessagePackSerializerOptions options) => DeserializeCore(ref reader, value, options);

    private static DeserializeDelegate CreateDeserialize()
    {
        Type type = typeof(T);
        if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
        {
            MethodInfo method = typeof(FormatterDispatchReuse<T>).GetMethod(nameof(DeserializeStructCore), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return (DeserializeDelegate)method.CreateDelegate(typeof(DeserializeDelegate));
        }

        if (!type.IsValueType)
        {
            MethodInfo method = typeof(FormatterDispatchReuse<T>).GetMethod(nameof(DeserializeClassCore), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return (DeserializeDelegate)method.CreateDelegate(typeof(DeserializeDelegate));
        }

        return static (ref MessagePackReader reader, T value, MessagePackSerializerOptions options) => FormatterDispatchByValue<T>.Deserialize(ref reader, options);
    }

    private static TStruct DeserializeStructCore<TStruct>(ref MessagePackReader reader, TStruct value, MessagePackSerializerOptions options)
        where TStruct : struct
    {
        FormatterDispatch<TStruct>.Deserialize(ref reader, ref value, options);
        return value;
    }

    private static TClass? DeserializeClassCore<TClass>(ref MessagePackReader reader, TClass? value, MessagePackSerializerOptions options)
        where TClass : class
    {
        return FormatterDispatchInto<TClass>.Deserialize(ref reader, value, options);
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type

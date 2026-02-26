// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Formatters;

namespace MessagePack.Internal;

#pragma warning disable SA1649 // File name should match first type name
internal static class FormatterDispatch<T>
    where T : struct
{
    private delegate void SerializeDelegate(IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options);

    private delegate void DeserializeIntoDelegate(IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);

    private static readonly SerializeDelegate SerializeByValue = static (IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options) => formatter.Serialize(ref writer, value, options);
    private static readonly SerializeDelegate SerializeByRef = static (IMessagePackFormatter<T> formatter, ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options) => ((IMessagePackFormatterSerializeIn<T>)formatter).Serialize(ref writer, in value, options);
    private static readonly DeserializeIntoDelegate DeserializeByValue = static (IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options) => value = formatter.Deserialize(ref reader, options);
    private static readonly DeserializeIntoDelegate DeserializeByRef = static (IMessagePackFormatter<T> formatter, ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options) => ((IMessagePackFormatterDeserializeRef<T>)formatter).Deserialize(ref reader, ref value, options);

    private static Type? serializeFormatterType;
    private static SerializeDelegate? serializeDispatch;
    private static Type? deserializeFormatterType;
    private static DeserializeIntoDelegate? deserializeDispatch;

    internal static void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        Type formatterType = formatter.GetType();
        SerializeDelegate? dispatch = serializeDispatch;
        if (dispatch is null || !ReferenceEquals(serializeFormatterType, formatterType))
        {
            dispatch = formatter is IMessagePackFormatterSerializeIn<T> ? SerializeByRef : SerializeByValue;
            serializeFormatterType = formatterType;
            serializeDispatch = dispatch;
        }

        dispatch(formatter, ref writer, in value, options);
    }

    internal static T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
    }

    internal static void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        Type formatterType = formatter.GetType();
        DeserializeIntoDelegate? dispatch = deserializeDispatch;
        if (dispatch is null || !ReferenceEquals(deserializeFormatterType, formatterType))
        {
            dispatch = formatter is IMessagePackFormatterDeserializeRef<T> ? DeserializeByRef : DeserializeByValue;
            deserializeFormatterType = formatterType;
            deserializeDispatch = dispatch;
        }

        dispatch(formatter, ref reader, ref value, options);
    }
}
#pragma warning restore SA1649 // File name should match first type name

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

namespace MessagePack.Internal;

#pragma warning disable SA1649 // File name should match first type name
internal static class FormatterDispatch<T>
{
    internal static void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        if (formatter is IMessagePackFormatterSerializeIn<T> byRefFormatter)
        {
            byRefFormatter.Serialize(ref writer, in value, options);
            return;
        }

        formatter.Serialize(ref writer, value, options);
    }

    internal static T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
    }

    internal static void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        if (formatter is IMessagePackFormatterDeserializeRef<T> byRefFormatter)
        {
            byRefFormatter.Deserialize(ref reader, ref value, options);
            return;
        }

        value = formatter.Deserialize(ref reader, options);
    }
}
#pragma warning restore SA1649 // File name should match first type name

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

internal class CustomFormatterRecordFormatter : IMessagePackFormatter<CustomFormatterRecord>
{
    public void Serialize(ref MessagePackWriter writer, CustomFormatterRecord value, MessagePackSerializerOptions options)
    {
        writer.WriteInt32(value.Value);
    }

    public CustomFormatterRecord Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new CustomFormatterRecord { Value = reader.ReadInt32() };
    }
}

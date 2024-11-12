﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

internal class CustomFormatterRecordFormatter : IMessagePackFormatter<CustomFormatterRecord?>
{
    // Deliberately test the singleton pattern.
    public static readonly IMessagePackFormatter<CustomFormatterRecord?> Instance = new CustomFormatterRecordFormatter();

    private CustomFormatterRecordFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, CustomFormatterRecord? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteInt32(value.Value);
    }

    public CustomFormatterRecord? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        return new CustomFormatterRecord { Value = reader.ReadInt32() };
    }
}

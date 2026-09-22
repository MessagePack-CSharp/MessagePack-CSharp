// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

internal class UnserializableRecordFormatter : IMessagePackFormatter<UnserializableRecord?>
{
    public void Serialize(ref MessagePackWriter writer, UnserializableRecord? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteInt32(value.Value);
    }

    public UnserializableRecord? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        return new UnserializableRecord { Value = reader.ReadInt32() };
    }
}

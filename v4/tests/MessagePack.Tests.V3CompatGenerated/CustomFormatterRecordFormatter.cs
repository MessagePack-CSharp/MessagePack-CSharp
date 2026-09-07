// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// V3COMPAT-EDIT: v4 formatters are buffer-generic; the attribute names the unbound
// generic (typeof(CustomFormatterRecordFormatter<,>)) and v3's singleton pattern gives
// way to per-buffer-pair instances
using SerializerFoundation;

internal class CustomFormatterRecordFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, CustomFormatterRecord?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, CustomFormatterRecord? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteInt32(value.Value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref CustomFormatterRecord? value)
    {
        value = buffer.TryReadNil() ? null : new CustomFormatterRecord { Value = buffer.ReadInt32() };
    }
}

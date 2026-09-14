// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// V3COMPAT-EDIT: v4 formatters are buffer-generic; the member-level attribute names the
// unbound generic (typeof(UnserializableRecordFormatter<,>))
using SerializerFoundation;

internal class UnserializableRecordFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, UnserializableRecord?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, UnserializableRecord? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteInt32(value.Value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref UnserializableRecord? value)
    {
        value = buffer.TryReadNil() ? null : new UnserializableRecord { Value = buffer.ReadInt32() };
    }
}

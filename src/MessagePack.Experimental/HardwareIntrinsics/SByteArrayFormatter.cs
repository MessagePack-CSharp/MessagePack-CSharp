// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace MessagePack.Formatters;

public sealed partial class SByteArrayFormatter : IMessagePackFormatter<sbyte[]?>
{
    public void Serialize(ref MessagePackWriter writer, sbyte[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }
}

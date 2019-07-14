// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class IgnoreFormatter<T> : IMessagePackFormatter<T>
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            reader.Skip();
            return default(T);
        }
    }
}

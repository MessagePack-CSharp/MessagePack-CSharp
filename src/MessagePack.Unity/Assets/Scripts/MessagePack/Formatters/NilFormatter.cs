// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Formatters
{
    public class NilFormatter : IMessagePackFormatter<Nil>
    {
        public static readonly IMessagePackFormatter<Nil> Instance = new NilFormatter();

        private NilFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Nil value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Nil Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadNil();
        }
    }

    // NullableNil is same as Nil.
    public class NullableNilFormatter : IMessagePackFormatter<Nil?>
    {
        public static readonly IMessagePackFormatter<Nil?> Instance = new NullableNilFormatter();

        private NullableNilFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Nil? value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Nil? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadNil();
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Formatters
{
    public sealed class CircularFormatter<T> : IMessagePackFormatter<T?>
        where T : class
    {
#pragma warning disable SA1401 // Fields should be private
        public readonly IMessagePackFormatter<T?> Formatter;
#pragma warning restore SA1401 // Fields should be private

        public CircularFormatter(IMessagePackFormatter<T?> formatter)
        {
            Formatter = formatter;
        }

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            if (options is CircularReferenceMessagePackSerializerOptions circularOptions)
            {
                if (reader.TryReadArrayHeader(out var count))
                {
                    if (count != 1)
                    {
                        throw new MessagePackSerializationException("The array length must be 1. actual: " + count);
                    }

                    return DeserializeInternal(ref reader, circularOptions);
                }
                else
                {
                    var index = (int)reader.ReadUInt32();
                    return (T)circularOptions.ReferenceSpan[index];
                }
            }

            using (circularOptions = CircularReferenceMessagePackSerializerOptions.Rent(options))
            {
                if (!reader.TryReadArrayHeader(out var count))
                {
                    throw new MessagePackSerializationException("The circular reference deserialization must start with length 1 array.");
                }

                if (count != 1)
                {
                    throw new MessagePackSerializationException("The array length must be 1. actual: " + count);
                }

                return DeserializeInternal(ref reader, circularOptions);
            }
        }

        private T DeserializeInternal(ref MessagePackReader reader, CircularReferenceMessagePackSerializerOptions circularOptions)
        {
            var answer = Formatter.Deserialize(ref reader, circularOptions)
#if DEBUG
                ?? throw new MessagePackSerializationException("Deserialized object should not be null!");
#else
                !;
#endif

            circularOptions.Add(answer);
            return answer;
        }

        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            if (options is CircularReferenceMessagePackSerializerOptions circularOptions)
            {
                var index = circularOptions.FindIndex(value);
                if (index != -1)
                {
                    writer.Write((uint)index);
                    return;
                }

                SerializeInternal(ref writer, value, circularOptions);
            }
            else
            {
                using (circularOptions = CircularReferenceMessagePackSerializerOptions.Rent(options))
                {
                    SerializeInternal(ref writer, value, circularOptions);
                }
            }
        }

        private void SerializeInternal(ref MessagePackWriter writer, T value, CircularReferenceMessagePackSerializerOptions circularOptions)
        {
            writer.WriteArrayHeader(1);
            circularOptions.Add(value);

            Formatter.Serialize(ref writer, value, circularOptions);
        }
    }
}

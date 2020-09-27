// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Formatters
{
    public sealed class CircularFormatter<T> : IMessagePackFormatter<T?>
        where T : class, new()
    {
#pragma warning disable SA1401 // Fields should be private
        public readonly IMessagePackFormatter<T?> Formatter;
        public readonly IMessagePackDeserializeOverwriter<T> Overwriter;
#pragma warning restore SA1401 // Fields should be private

        public CircularFormatter(IMessagePackFormatter<T?> formatter, IMessagePackDeserializeOverwriter<T> overwriter)
        {
            Formatter = formatter;
            Overwriter = overwriter;
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

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            if (options is CircularReferenceMessagePackSerializerOptions circularOptions)
            {
                if (TryReadUInt32(ref reader, out var index))
                {
                    return (T)circularOptions.ReferenceSpan[(int)index];
                }

                var count = reader.ReadArrayHeader();
                if (count != 1)
                {
                    throw new MessagePackSerializationException("The array length must be 1. actual: " + count);
                }

                return DeserializeInternal(ref reader, circularOptions);
            }

            using (circularOptions = CircularReferenceMessagePackSerializerOptions.Rent(options))
            {
                var count = reader.ReadArrayHeader();
                if (count != 1)
                {
                    throw new MessagePackSerializationException("The array length must be 1. actual: " + count);
                }

                return DeserializeInternal(ref reader, circularOptions);
            }
        }

        private static bool TryReadUInt32(ref MessagePackReader reader, out uint value)
        {
            if (reader.NextMessagePackType == MessagePackType.Integer)
            {
                value = reader.ReadUInt32();
                return true;
            }

            value = default;
            return false;
        }

        private T DeserializeInternal(ref MessagePackReader reader, CircularReferenceMessagePackSerializerOptions circularOptions)
        {
            var answer = new T();
            circularOptions.Add(answer);
            Overwriter.DeserializeOverwrite(ref reader, circularOptions, answer);
            return answer;
        }
    }
}

using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class NullableFormatter<T> : IMessagePackFormatter<T?>
        where T : struct
    {
        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                reader.ReadNil();
                return null;
            }
            else
            {
                return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
            }
        }
    }

    public sealed class StaticNullableFormatter<T> : IMessagePackFormatter<T?>
        where T : struct
    {
        readonly IMessagePackFormatter<T> underlyingFormatter;

        public StaticNullableFormatter(IMessagePackFormatter<T> underlyingFormatter)
        {
            this.underlyingFormatter = underlyingFormatter;
        }

        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                underlyingFormatter.Serialize(ref writer, value.Value, options);
            }
        }

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return underlyingFormatter.Deserialize(ref reader, options);
            }
        }
    }
}
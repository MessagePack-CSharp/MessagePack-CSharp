using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class NullableFormatter<T> : IMessagePackFormatter<T?>
        where T : struct
    {
        public void Serialize(ref MessagePackWriter writer, T? value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                formatterResolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, formatterResolver);
            }
        }

        public T? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.IsNil)
            {
                reader.ReadNil();
                return null;
            }
            else
            {
                return resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
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

        public void Serialize(ref MessagePackWriter writer, T? value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                underlyingFormatter.Serialize(ref writer, value.Value, formatterResolver);
            }
        }

        public T? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return underlyingFormatter.Deserialize(ref reader, resolver);
            }
        }
    }
}
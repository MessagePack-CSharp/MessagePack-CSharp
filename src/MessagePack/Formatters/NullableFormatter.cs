using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class NullableFormatter<T> : IMessagePackFormatter<T?>
        where T : struct
    {
        public int Serialize(ref byte[] bytes, int offset, T? value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);
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

        public int Serialize(ref byte[] bytes, int offset, T? value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return underlyingFormatter.Serialize(ref bytes, offset, value.Value, formatterResolver);
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
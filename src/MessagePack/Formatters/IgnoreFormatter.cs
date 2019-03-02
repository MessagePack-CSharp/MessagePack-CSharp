using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class IgnoreFormatter<T> : IMessagePackFormatter<T>
    {
        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public T Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            reader.Skip();
            return default(T);
        }
    }
}
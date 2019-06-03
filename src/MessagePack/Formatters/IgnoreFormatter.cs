using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class IgnoreFormatter<T> : IMessagePackFormatter<T>
    {
        public void Serialize(ref MessagePackWriter writer, T value, IFormatterResolver resolver)
        {
            writer.WriteNil();
        }

        public T Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            reader.Skip();
            return default(T);
        }
    }
}
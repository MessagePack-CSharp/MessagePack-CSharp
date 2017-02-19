
namespace MessagePack.Formatters
{
    public interface IMessagePackFormatter<T>
    {
        int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
        T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
    }
}

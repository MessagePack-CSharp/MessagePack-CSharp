
namespace MessagePack.Formatters
{
    public interface IMessagePackFormatter<T>
    {
        int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver typeResolver);
        T Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize);
    }
}

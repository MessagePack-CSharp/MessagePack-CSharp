
namespace MessagePack.Formatters
{
    public interface IMessagePackFormatter<T>
    {
        int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
        T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
    }

    public interface IOverwriteMessagePackFormatter<T> : IMessagePackFormatter<T>
    {
        void DeserializeTo(ref T to, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
    }

    public static class IMessagePackFormatterExtensions
    {
        public static void DeserializeTo<T>(this IMessagePackFormatter<T> formatter, ref T to, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var overwriteFormatter = formatter as IOverwriteMessagePackFormatter<T>;
            if (overwriteFormatter != null)
            {
                overwriteFormatter.DeserializeTo(ref to, bytes, offset, formatterResolver, out readSize);
            }
            else
            {
                to = formatter.Deserialize(bytes, offset, formatterResolver, out readSize);
            }
        }
    }
}

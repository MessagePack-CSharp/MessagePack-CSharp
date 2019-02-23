using System.Buffers;

namespace MessagePack.Formatters
{
    /// <summary>
    /// The contract for serialization of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
    public interface IMessagePackFormatter<T>
    {
        int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="resolver">The resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        T Deserialize(ref MessagePackReader reader, IFormatterResolver resolver);
    }
}

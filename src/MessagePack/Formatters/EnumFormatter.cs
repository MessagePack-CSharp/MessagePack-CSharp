using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    public class ByteEnumFormatter<T> : IMessagePackFormatter<T>
    {
        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotImplementedException();
        }
    }
}

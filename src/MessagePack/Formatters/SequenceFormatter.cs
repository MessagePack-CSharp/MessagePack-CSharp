using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    // TODO:ArraySegment, other collections

    public class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public int Serialize(ref byte[] bytes, int offset, byte[] value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBytes(ref bytes, offset, value);
        }

        public byte[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadBytes(bytes, offset, out readSize);
        }
    }

    public class ArrayFormatter<T> : IMessagePackFormatter<T[]>
    {
        public int Serialize(ref byte[] bytes, int offset, T[] value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            var formatter = formatterResolver.GetFormatterWithVerify<T>();

            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);

            foreach (var item in value)
            {
                offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
            }

            return offset - startOffset;
        }

        public T[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var formatter = formatterResolver.GetFormatterWithVerify<T>();

            var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            var array = new T[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = formatter.Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
            }
            readSize = offset - startOffset;
            return array;
        }
    }
}
